using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MeryanovPayment.Services;

namespace MeryanovPayment.Pages
{
    public partial class AuthPage : Page
    {
        private int _failedAttempts = 0;
        private const int MaxAttemptsBeforeCaptcha = 2;

        public AuthPage()
        {
            InitializeComponent();
            MainWindow.Instance?.ShowBackButton(false);
            MainWindow.Instance?.SetCurrentUser("", "");
            MainWindow.Instance?.SetPageTitle("Авторизация");
        }

        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            TxtError.Visibility = Visibility.Collapsed;

            string login = TxtLogin.Text?.Trim();
            string password = TxtPassword.Password;

            if (string.IsNullOrWhiteSpace(login))
            {
                ShowError("Введите логин");
                return;
            }
            if (string.IsNullOrWhiteSpace(password))
            {
                ShowError("Введите пароль");
                return;
            }

            // Проверка капчи, если нужна
            if (CaptchaPanel.Visibility == Visibility.Visible)
            {
                if (!CaptchaService.Validate(TxtCaptcha.Text))
                {
                    ShowError("Неверный код капчи");
                    RefreshCaptcha();
                    return;
                }
            }

            try
            {
                var user = DatabaseService.Authenticate(login, password);

                if (user == null)
                {
                    _failedAttempts++;
                    if (_failedAttempts >= MaxAttemptsBeforeCaptcha)
                    {
                        CaptchaPanel.Visibility = Visibility.Visible;
                        RefreshCaptcha();
                    }
                    ShowError("Неверный логин или пароль");
                    TxtPassword.Password = "";
                    return;
                }

                if (user.IsBlocked)
                {
                    string until = user.BlockUntil?.ToString("HH:mm:ss") ?? "неизвестно";
                    ShowError($"Учётная запись заблокирована до {until}. Попробуйте позже.");
                    CaptchaPanel.Visibility = Visibility.Visible;
                    RefreshCaptcha();
                    return;
                }

                // Успешный вход
                SessionService.Login(user);
                MainWindow.Instance.SetCurrentUser(user.FIO, user.Role);

                if (user.IsAdmin)
                    MainWindow.Instance.NavigateTo(new AdminPage(), "Панель администратора", false);
                else
                    MainWindow.Instance.NavigateTo(new UserPage(), "Мои платежи", false);
            }
            catch (Exception ex)
            {
                ShowError("Ошибка подключения к базе данных:\n" + ex.Message);
            }
        }

        private void BtnRegister_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.Instance.NavigateTo(new RegisterPage(), "Регистрация", true);
        }

        private void BtnForgotPassword_Click(object sender, RoutedEventArgs e)
        {
            string login = TxtLogin.Text?.Trim();

            if (string.IsNullOrWhiteSpace(login))
            {
                MessageBox.Show("Сначала введите логин в поле выше, затем нажмите «Забыли пароль?».",
                    "Восстановление пароля", MessageBoxButton.OK, MessageBoxImage.Information);
                TxtLogin.Focus();
                return;
            }

            var confirm = MessageBox.Show(
                $"Сбросить пароль для пользователя «{login}»?\n\nБудет установлен временный пароль.",
                "Восстановление пароля",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes)
                return;

            try
            {
                if (!DatabaseService.UserExists(login))
                {
                    MessageBox.Show("Пользователь с таким логином не найден.",
                        "Восстановление пароля", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Сбрасываем пароль на временный
                string tempPassword = "123456";
                bool ok = DatabaseService.ResetPassword(login, tempPassword);

                if (ok)
                {
                    MessageBox.Show(
                        $"Пароль успешно сброшен.\n\n" +
                        $"Логин: {login}\n" +
                        $"Временный пароль: {tempPassword}\n\n" +
                        "После входа рекомендуется сразу сменить пароль в разделе «Сменить пароль».",
                        "Восстановление пароля",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    TxtPassword.Password = "";
                }
                else
                {
                    MessageBox.Show("Не удалось сбросить пароль. Попробуйте позже.",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка: " + ex.Message,
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ImgCaptcha_Click(object sender, MouseButtonEventArgs e)
        {
            RefreshCaptcha();
        }

        private void RefreshCaptcha()
        {
            CaptchaService.GenerateCode();
            ImgCaptcha.Source = CaptchaService.GenerateImage();
            TxtCaptcha.Text = "";
        }

        private void ShowError(string message)
        {
            TxtError.Text = message;
            TxtError.Visibility = Visibility.Visible;
        }
    }
}
