using System.Windows;
using System.Windows.Controls;
using MeryanovPayment.Services;

namespace MeryanovPayment.Pages
{
    public partial class ChangePasswordPage : Page
    {
        public ChangePasswordPage()
        {
            InitializeComponent();
            MainWindow.Instance?.SetPageTitle("Смена пароля");
            MainWindow.Instance?.ShowBackButton(true);
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            TxtError.Visibility = Visibility.Collapsed;

            string oldPass = TxtOld.Password;
            string newPass = TxtNew.Password;
            string confirm = TxtConfirm.Password;

            if (string.IsNullOrEmpty(oldPass) || string.IsNullOrEmpty(newPass))
            {
                ShowError("Заполните все поля");
                return;
            }
            if (newPass.Length < 6)
            {
                ShowError("Новый пароль должен содержать минимум 6 символов");
                return;
            }
            if (newPass != confirm)
            {
                ShowError("Пароли не совпадают");
                return;
            }

            try
            {
                bool ok = DatabaseService.ChangePassword(SessionService.CurrentUser.ID, oldPass, newPass);
                if (ok)
                {
                    MessageBox.Show("Пароль успешно изменён", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);

                    // Возврат на предыдущую страницу
                    if (SessionService.CurrentUser.IsAdmin)
                        MainWindow.Instance.NavigateTo(new AdminPage(), "Панель администратора", false);
                    else
                        MainWindow.Instance.NavigateTo(new UserPage(), "Мои платежи", false);
                }
                else
                {
                    ShowError("Неверный текущий пароль");
                }
            }
            catch (System.Exception ex)
            {
                ShowError("Ошибка: " + ex.Message);
            }
        }

        private void ShowError(string msg)
        {
            TxtError.Text = msg;
            TxtError.Visibility = Visibility.Visible;
        }
    }
}
