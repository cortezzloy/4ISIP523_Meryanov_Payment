using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using MeryanovPayment.Services;

namespace MeryanovPayment.Pages
{
    public partial class UserPage : Page
    {
        public UserPage()
        {
            InitializeComponent();
            Loaded += UserPage_Loaded;
        }

        private void UserPage_Loaded(object sender, RoutedEventArgs e)
        {
            MainWindow.Instance?.SetPageTitle("Мои платежи");
            MainWindow.Instance?.ShowBackButton(false);

            var categories = DatabaseService.GetAllCategories();
            categories.Insert(0, new Models.Category { ID = 0, Name = "Все категории" });
            CmbCategory.ItemsSource = categories;
            CmbCategory.SelectedIndex = 0;

            LoadData();
        }

        private void LoadData()
        {
            if (!SessionService.IsAuthenticated) return;

            string search = TxtSearch.Text;
            int? catId = null;
            if (CmbCategory.SelectedValue is int id && id > 0)
                catId = id;

            DateTime? from = DpFrom.SelectedDate;
            DateTime? to = DpTo.SelectedDate;

            var data = DatabaseService.GetPayments(
                userId: SessionService.CurrentUser.ID,
                search: search,
                categoryId: catId,
                dateFrom: from,
                dateTo: to,
                sortColumn: "Date",
                sortAsc: false);

            DgPayments.ItemsSource = data;
            decimal total = data.Sum(p => p.Total);
            TxtTotal.Text = $"Итого: {total:N2} ₽  (записей: {data.Count})";
        }

        private void Filter_Changed(object sender, EventArgs e) => LoadData();

        private void BtnReset_Click(object sender, RoutedEventArgs e)
        {
            TxtSearch.Text = "";
            CmbCategory.SelectedIndex = 0;
            DpFrom.SelectedDate = null;
            DpTo.SelectedDate = null;
            LoadData();
        }

        private void BtnChangePassword_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.Instance.NavigateTo(new ChangePasswordPage(), "Смена пароля", true);
        }

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Вы действительно хотите выйти из учётной записи?",
                "Выход", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                SessionService.Logout();
                MainWindow.Instance.SetCurrentUser("", "");
                MainWindow.Instance.NavigateTo(new AuthPage(), "Авторизация", false);
            }
        }

        private void DgPayments_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Пользователь только просматривает — детали можно показать в MessageBox
            if (DgPayments.SelectedItem is Models.PaymentView p)
            {
                MessageBox.Show(
                    $"Название: {p.Name}\nКатегория: {p.CategoryName}\nДата: {p.Date:dd.MM.yyyy}\n" +
                    $"Количество: {p.Num}\nЦена: {p.Price:N2}\nСумма: {p.Total:N2}",
                    "Детали платежа", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
}
