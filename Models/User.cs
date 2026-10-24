using System;

namespace MeryanovPayment.Models
{
    public class User
    {
        public int ID { get; set; }
        public string Login { get; set; }
        public string PasswordHash { get; set; }
        public string Role { get; set; }
        public string FIO { get; set; }
        public byte[] Photo { get; set; }
        public int FailedLoginAttempts { get; set; }
        public bool IsBlocked { get; set; }
        public DateTime? BlockUntil { get; set; }

        public bool IsAdmin => Role == "Администратор";
    }
}
