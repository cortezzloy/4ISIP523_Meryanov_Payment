using System.Security.Cryptography;
using System.Text;

namespace MeryanovPayment.Services
{
    /// <summary>
    /// Хэширование паролей (SHA-256)
    /// </summary>
    public static class HashService
    {
        public static string HashPassword(string password)
        {
            if (string.IsNullOrEmpty(password))
                return string.Empty;

            using (var sha256 = SHA256.Create())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(password);
                byte[] hash = sha256.ComputeHash(bytes);
                var sb = new StringBuilder();
                foreach (byte b in hash)
                    sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }

        public static bool VerifyPassword(string password, string hash)
        {
            string computed = HashPassword(password);
            return string.Equals(computed, hash, System.StringComparison.OrdinalIgnoreCase);
        }
    }
}
