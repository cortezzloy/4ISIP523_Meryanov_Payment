using MeryanovPayment.Models;

namespace MeryanovPayment.Services
{
    /// <summary>
    /// Текущая сессия пользователя
    /// </summary>
    public static class SessionService
    {
        public static User CurrentUser { get; private set; }

        public static bool IsAuthenticated => CurrentUser != null;

        public static void Login(User user)
        {
            CurrentUser = user;
        }

        public static void Logout()
        {
            CurrentUser = null;
        }
    }
}
