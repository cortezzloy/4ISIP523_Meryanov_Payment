using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using MeryanovPayment.Models;
using MeryanovPayment.Services;

namespace MeryanovPayment.Pages
{
    public partial class AdminPage : Page
    {
        public AdminPage()
        {
            InitializeComponent();
            Loaded += AdminPage_Loaded;
        }

        private void AdminPage_Loaded(object sender, RoutedEventArgs e)
        {
            MainWindow.Instance?.SetPageTitle("Панель администратора");
            MainWindow.Instance?.ShowBackButton(false);

            var categories = DatabaseService.GetAllCategories();
            categories.Insert(0, new Category { ID = 0, Name = "Все категории" });
            CmbCategory.ItemsSource = categories;
            CmbCategory.SelectedIndex = 0;

            LoadData();
        }

        private void LoadData()
        {
            string search = TxtSearch.Text;
            int? catId = null;
            if (CmbCategory.SelectedValue is int id && id > 0)
                catId = id;

            var data = DatabaseService.GetPayments(
                search: search,
                categoryId: catId,
                dateFrom: DpFrom.SelectedDate,
                dateTo: DpTo.SelectedDate,
                sortColumn: "Date",
                sortAsc: false);

            DgPayments.ItemsSource = data;
            TxtTotal.Text = $"Итого: {data.Sum(p => p.Total):N2} ₽  (записей: {data.Count})";
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

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new PaymentEditWindow(null);
            if (dlg.ShowDialog() == true)
                LoadData();
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (DgPayments.SelectedItem is PaymentView p)
            {
                var dlg = new PaymentEditWindow(p);
                if (dlg.ShowDialog() == true)
                    LoadData();
            }
            else
            {
                MessageBox.Show("Выберите запись для редактирования", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (!(DgPayments.SelectedItem is PaymentView p))
            {
                MessageBox.Show("Выберите запись для удаления", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show(
                $"Вы действительно хотите удалить платёж «{p.Name}»?\nЭто действие необратимо.",
                "Подтверждение удаления",
                MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    DatabaseService.DeletePayment(p.ID);
                    LoadData();
                    MessageBox.Show("Запись удалена", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка удаления: " + ex.Message, "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnChart_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.Instance.NavigateTo(new ChartPage(), "Диаграмма по категориям", true);
        }

        private void BtnExportDocx_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var payments = DatabaseService.GetPayments(sortColumn: "Date", sortAsc: false);
                var categories = DatabaseService.GetAllCategories();
                ExportService.ExportToWord(payments, categories, null);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка: " + ex.Message, "Экспорт в Word",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnExportPdf_Click(object sender, RoutedEventArgs e)
        {
            // PDF сохраняется вместе с DOCX
            BtnExportDocx_Click(sender, e);
        }

        private void BtnExportXlsx_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var payments = DatabaseService.GetPayments(sortColumn: "Date", sortAsc: false);
                var categories = DatabaseService.GetAllCategories();
                ExportService.ExportToExcel(payments, categories);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка: " + ex.Message, "Экспорт в Excel",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
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
    }
}
