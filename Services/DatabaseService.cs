using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using MeryanovPayment.Models;

namespace MeryanovPayment.Services
{
    public static class DatabaseService
    {
        private static string ConnectionString =>
            ConfigurationManager.ConnectionStrings["PaymentDb"]?.ConnectionString
            ?? "Data Source=.;Initial Catalog=DB_Meryanov_Payment;Integrated Security=True;TrustServerCertificate=True;";

        #region Пользователи / Авторизация

        public static User Authenticate(string login, string password)
        {
            string hash = HashService.HashPassword(password);

            using (var conn = new SqlConnection(ConnectionString))
            {
                conn.Open();
                string sql = @"
                    SELECT ID, Login, Password, Role, FIO, Photo, FailedLoginAttempts, IsBlocked, BlockUntil
                    FROM [User]
                    WHERE Login = @login";

                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@login", login);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (!reader.Read())
                            return null;

                        var user = MapUser(reader);

                        // Проверка блокировки
                        if (user.IsBlocked)
                        {
                            if (user.BlockUntil.HasValue && user.BlockUntil > DateTime.Now)
                                return user; // всё ещё заблокирован
                            // Срок блокировки истёк — разблокируем
                            UnlockUser(user.ID);
                            user.IsBlocked = false;
                            user.FailedLoginAttempts = 0;
                        }

                        if (HashService.VerifyPassword(password, user.PasswordHash))
                        {
                            ResetFailedAttempts(user.ID);
                            return user;
                        }

                        // Неверный пароль
                        IncrementFailedAttempts(user.ID);
                        user.FailedLoginAttempts++;
                        return null;
                    }
                }
            }
        }

        public static bool Register(string login, string password, string fio, string role = "Пользователь")
        {
            if (UserExists(login))
                return false;

            string hash = HashService.HashPassword(password);

            using (var conn = new SqlConnection(ConnectionString))
            {
                conn.Open();
                string sql = @"
                    INSERT INTO [User] (Login, Password, Role, FIO, FailedLoginAttempts, IsBlocked)
                    VALUES (@login, @hash, @role, @fio, 0, 0)";

                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@login", login);
                    cmd.Parameters.AddWithValue("@hash", hash);
                    cmd.Parameters.AddWithValue("@role", role);
                    cmd.Parameters.AddWithValue("@fio", fio);
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }

        public static bool UserExists(string login)
        {
            using (var conn = new SqlConnection(ConnectionString))
            {
                conn.Open();
                using (var cmd = new SqlCommand("SELECT COUNT(1) FROM [User] WHERE Login = @login", conn))
                {
                    cmd.Parameters.AddWithValue("@login", login);
                    return (int)cmd.ExecuteScalar() > 0;
                }
            }
        }

        /// <summary>
        /// Сброс пароля (для функции «Забыли пароль?»)
        /// </summary>
        public static bool ResetPassword(string login, string newPassword)
        {
            string hash = HashService.HashPassword(newPassword);
            using (var conn = new SqlConnection(ConnectionString))
            {
                conn.Open();
                using (var cmd = new SqlCommand(
                    @"UPDATE [User] 
                      SET Password = @hash, FailedLoginAttempts = 0, IsBlocked = 0, BlockUntil = NULL 
                      WHERE Login = @login", conn))
                {
                    cmd.Parameters.AddWithValue("@hash", hash);
                    cmd.Parameters.AddWithValue("@login", login);
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }

        public static void IncrementFailedAttempts(int userId)
        {
            using (var conn = new SqlConnection(ConnectionString))
            {
                conn.Open();
                string sql = @"
                    UPDATE [User]
                    SET FailedLoginAttempts = FailedLoginAttempts + 1,
                        IsBlocked = CASE WHEN FailedLoginAttempts + 1 >= 3 THEN 1 ELSE IsBlocked END,
                        BlockUntil = CASE WHEN FailedLoginAttempts + 1 >= 3 THEN DATEADD(MINUTE, 15, GETDATE()) ELSE BlockUntil END
                    WHERE ID = @id";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@id", userId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public static void ResetFailedAttempts(int userId)
        {
            using (var conn = new SqlConnection(ConnectionString))
            {
                conn.Open();
                using (var cmd = new SqlCommand(
                    "UPDATE [User] SET FailedLoginAttempts = 0, IsBlocked = 0, BlockUntil = NULL WHERE ID = @id", conn))
                {
                    cmd.Parameters.AddWithValue("@id", userId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public static void UnlockUser(int userId)
        {
            ResetFailedAttempts(userId);
        }

        public static bool ChangePassword(int userId, string oldPassword, string newPassword)
        {
            using (var conn = new SqlConnection(ConnectionString))
            {
                conn.Open();
                // Проверяем старый пароль
                using (var cmd = new SqlCommand("SELECT Password FROM [User] WHERE ID = @id", conn))
                {
                    cmd.Parameters.AddWithValue("@id", userId);
                    string currentHash = cmd.ExecuteScalar()?.ToString();
                    if (!HashService.VerifyPassword(oldPassword, currentHash))
                        return false;
                }

                string newHash = HashService.HashPassword(newPassword);
                using (var cmd = new SqlCommand("UPDATE [User] SET Password = @hash WHERE ID = @id", conn))
                {
                    cmd.Parameters.AddWithValue("@hash", newHash);
                    cmd.Parameters.AddWithValue("@id", userId);
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }

        private static User MapUser(SqlDataReader reader)
        {
            return new User
            {
                ID = reader.GetInt32(0),
                Login = reader.GetString(1),
                PasswordHash = reader.GetString(2),
                Role = reader.GetString(3),
                FIO = reader.GetString(4),
                Photo = reader.IsDBNull(5) ? null : (byte[])reader[5],
                FailedLoginAttempts = reader.GetInt32(6),
                IsBlocked = reader.GetBoolean(7),
                BlockUntil = reader.IsDBNull(8) ? (DateTime?)null : reader.GetDateTime(8)
            };
        }

        public static List<User> GetAllUsers()
        {
            var list = new List<User>();
            using (var conn = new SqlConnection(ConnectionString))
            {
                conn.Open();
                using (var cmd = new SqlCommand(
                    "SELECT ID, Login, Password, Role, FIO, Photo, FailedLoginAttempts, IsBlocked, BlockUntil FROM [User] ORDER BY FIO", conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                        list.Add(MapUser(reader));
                }
            }
            return list;
        }

        #endregion

        #region Категории

        public static List<Category> GetAllCategories()
        {
            var list = new List<Category>();
            using (var conn = new SqlConnection(ConnectionString))
            {
                conn.Open();
                using (var cmd = new SqlCommand("SELECT ID, Name FROM Category ORDER BY Name", conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new Category
                        {
                            ID = reader.GetInt32(0),
                            Name = reader.GetString(1)
                        });
                    }
                }
            }
            return list;
        }

        public static int AddCategory(string name)
        {
            using (var conn = new SqlConnection(ConnectionString))
            {
                conn.Open();
                using (var cmd = new SqlCommand(
                    "INSERT INTO Category (Name) OUTPUT INSERTED.ID VALUES (@name)", conn))
                {
                    cmd.Parameters.AddWithValue("@name", name);
                    return (int)cmd.ExecuteScalar();
                }
            }
        }

        public static bool UpdateCategory(int id, string name)
        {
            using (var conn = new SqlConnection(ConnectionString))
            {
                conn.Open();
                using (var cmd = new SqlCommand("UPDATE Category SET Name = @name WHERE ID = @id", conn))
                {
                    cmd.Parameters.AddWithValue("@name", name);
                    cmd.Parameters.AddWithValue("@id", id);
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }

        public static bool DeleteCategory(int id)
        {
            using (var conn = new SqlConnection(ConnectionString))
            {
                conn.Open();
                using (var cmd = new SqlCommand("DELETE FROM Category WHERE ID = @id", conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }

        #endregion

        #region Платежи

        public static List<PaymentView> GetPayments(int? userId = null, string search = null,
            int? categoryId = null, DateTime? dateFrom = null, DateTime? dateTo = null,
            string sortColumn = "Date", bool sortAsc = false)
        {
            var list = new List<PaymentView>();
            using (var conn = new SqlConnection(ConnectionString))
            {
                conn.Open();
                var sql = @"
                    SELECT p.ID, p.CategoryID, c.Name AS CategoryName, p.UserID, u.FIO AS UserFIO,
                           p.Date, p.Name, p.Num, p.Price, (p.Num * p.Price) AS Total
                    FROM Payment p
                    INNER JOIN Category c ON p.CategoryID = c.ID
                    INNER JOIN [User] u ON p.UserID = u.ID
                    WHERE 1=1";

                if (userId.HasValue)
                    sql += " AND p.UserID = @userId";
                if (!string.IsNullOrWhiteSpace(search))
                    sql += " AND (p.Name LIKE @search OR c.Name LIKE @search OR u.FIO LIKE @search)";
                if (categoryId.HasValue)
                    sql += " AND p.CategoryID = @categoryId";
                if (dateFrom.HasValue)
                    sql += " AND p.Date >= @dateFrom";
                if (dateTo.HasValue)
                    sql += " AND p.Date < @dateTo";

                // Сортировка (белый список колонок)
                var allowedSort = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                    { "Date", "Name", "Num", "Price", "Total", "CategoryName", "UserFIO" };
                if (!allowedSort.Contains(sortColumn))
                    sortColumn = "Date";
                sql += $" ORDER BY {sortColumn} {(sortAsc ? "ASC" : "DESC")}";

                using (var cmd = new SqlCommand(sql, conn))
                {
                    if (userId.HasValue)
                        cmd.Parameters.AddWithValue("@userId", userId.Value);
                    if (!string.IsNullOrWhiteSpace(search))
                        cmd.Parameters.AddWithValue("@search", "%" + search.Trim() + "%");
                    if (categoryId.HasValue)
                        cmd.Parameters.AddWithValue("@categoryId", categoryId.Value);
                    if (dateFrom.HasValue)
                        cmd.Parameters.AddWithValue("@dateFrom", dateFrom.Value.Date);
                    if (dateTo.HasValue)
                        cmd.Parameters.AddWithValue("@dateTo", dateTo.Value.Date.AddDays(1));

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new PaymentView
                            {
                                ID = reader.GetInt32(0),
                                CategoryID = reader.GetInt32(1),
                                CategoryName = reader.GetString(2),
                                UserID = reader.GetInt32(3),
                                UserFIO = reader.GetString(4),
                                Date = reader.GetDateTime(5),
                                Name = reader.GetString(6),
                                Num = reader.GetInt32(7),
                                Price = reader.GetDecimal(8),
                                Total = reader.GetDecimal(9)
                            });
                        }
                    }
                }
            }
            return list;
        }

        public static int AddPayment(int categoryId, int userId, DateTime date, string name, int num, decimal price)
        {
            using (var conn = new SqlConnection(ConnectionString))
            {
                conn.Open();
                using (var cmd = new SqlCommand(@"
                    INSERT INTO Payment (CategoryID, UserID, Date, Name, Num, Price)
                    OUTPUT INSERTED.ID
                    VALUES (@cat, @user, @date, @name, @num, @price)", conn))
                {
                    cmd.Parameters.AddWithValue("@cat", categoryId);
                    cmd.Parameters.AddWithValue("@user", userId);
                    cmd.Parameters.AddWithValue("@date", date);
                    cmd.Parameters.AddWithValue("@name", name);
                    cmd.Parameters.AddWithValue("@num", num);
                    cmd.Parameters.AddWithValue("@price", price);
                    return (int)cmd.ExecuteScalar();
                }
            }
        }

        public static bool UpdatePayment(int id, int categoryId, DateTime date, string name, int num, decimal price)
        {
            using (var conn = new SqlConnection(ConnectionString))
            {
                conn.Open();
                using (var cmd = new SqlCommand(@"
                    UPDATE Payment
                    SET CategoryID = @cat, Date = @date, Name = @name, Num = @num, Price = @price
                    WHERE ID = @id", conn))
                {
                    cmd.Parameters.AddWithValue("@cat", categoryId);
                    cmd.Parameters.AddWithValue("@date", date);
                    cmd.Parameters.AddWithValue("@name", name);
                    cmd.Parameters.AddWithValue("@num", num);
                    cmd.Parameters.AddWithValue("@price", price);
                    cmd.Parameters.AddWithValue("@id", id);
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }

        public static bool DeletePayment(int id)
        {
            using (var conn = new SqlConnection(ConnectionString))
            {
                conn.Open();
                using (var cmd = new SqlCommand("DELETE FROM Payment WHERE ID = @id", conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }

        /// <summary>
        /// Данные для графика: сумма по категориям
        /// </summary>
        public static List<(string Category, decimal Total)> GetPaymentsByCategory(int? userId = null)
        {
            var list = new List<(string, decimal)>();
            using (var conn = new SqlConnection(ConnectionString))
            {
                conn.Open();
                string sql = @"
                    SELECT c.Name, SUM(p.Num * p.Price) AS Total
                    FROM Payment p
                    INNER JOIN Category c ON p.CategoryID = c.ID
                    WHERE 1=1";
                if (userId.HasValue)
                    sql += " AND p.UserID = @userId";
                sql += " GROUP BY c.Name ORDER BY Total DESC";

                using (var cmd = new SqlCommand(sql, conn))
                {
                    if (userId.HasValue)
                        cmd.Parameters.AddWithValue("@userId", userId.Value);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                            list.Add((reader.GetString(0), reader.GetDecimal(1)));
                    }
                }
            }
            return list;
        }

        #endregion
    }
}
