using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using MeryanovPayment.Services;

namespace MeryanovPayment.Pages
{
    public partial class RegisterPage : Page
    {
        public RegisterPage()
        {
            InitializeComponent();
            MainWindow.Instance?.SetPageTitle("Регистрация");
            MainWindow.Instance?.ShowBackButton(true);
        }

        private void BtnRegister_Click(object sender, RoutedEventArgs e)
        {
            TxtError.Visibility = Visibility.Collapsed;

            string fio = TxtFIO.Text?.Trim();
            string login = TxtLogin.Text?.Trim();
            string password = TxtPassword.Password;
            string confirm = TxtPasswordConfirm.Password;

            // Валидация
            if (string.IsNullOrWhiteSpace(fio) || fio.Length < 5)
            {
                ShowError("Введите корректное ФИО (минимум 5 символов)");
                return;
            }
            if (!Regex.IsMatch(fio, @"^[А-Яа-яЁёA-Za-z\s\-]+$"))
            {
                ShowError("ФИО может содержать только буквы, пробелы и дефис");
                return;
            }
            if (string.IsNullOrWhiteSpace(login) || login.Length < 3)
            {
                ShowError("Логин должен содержать минимум 3 символа");
                return;
            }
            if (!Regex.IsMatch(login, @"^[a-zA-Z0-9_]+$"))
            {
                ShowError("Логин может содержать только латинские буквы, цифры и _");
                return;
            }
            if (string.IsNullOrEmpty(password) || password.Length < 6)
            {
                ShowError("Пароль должен содержать минимум 6 символов");
                return;
            }
            if (password != confirm)
            {
                ShowError("Пароли не совпадают");
                return;
            }

            try
            {
                if (DatabaseService.UserExists(login))
                {
                    ShowError("Пользователь с таким логином уже существует");
                    return;
                }

                bool ok = DatabaseService.Register(login, password, fio, "Пользователь");
                if (ok)
                {
                    MessageBox.Show("Регистрация прошла успешно!\nТеперь вы можете войти.",
                        "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    MainWindow.Instance.NavigateTo(new AuthPage(), "Авторизация", false);
                }
                else
                {
                    ShowError("Не удалось зарегистрировать пользователя");
                }
            }
            catch (Exception ex)
            {
                ShowError("Ошибка: " + ex.Message);
            }
        }

        private void BtnBackToLogin_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.Instance.NavigateTo(new AuthPage(), "Авторизация", false);
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

                string tempPassword = "123456";
                bool ok = DatabaseService.ResetPassword(login, tempPassword);

                if (ok)
                {
                    MessageBox.Show(
                        $"Пароль успешно сброшен.\n\n" +
                        $"Логин: {login}\n" +
                        $"Временный пароль: {tempPassword}\n\n" +
                        "Теперь вы можете войти с этим паролем.",
                        "Восстановление пароля",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    MainWindow.Instance.NavigateTo(new AuthPage(), "Авторизация", false);
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

        private void ShowError(string message)
        {
            TxtError.Text = message;
            TxtError.Visibility = Visibility.Visible;
        }
    }
}
