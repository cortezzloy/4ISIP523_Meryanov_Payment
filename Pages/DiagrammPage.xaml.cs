using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms.DataVisualization.Charting;
using MeryanovPayment.Models;
using MeryanovPayment.Services;

namespace MeryanovPayment.Pages
{
    /// <summary>
    /// Страница диаграмм — Шаг 12 методических указаний
    /// </summary>
    public partial class DiagrammPage : Page
    {
        public DiagrammPage()
        {
            InitializeComponent();
            MainWindow.Instance?.SetPageTitle("Диаграмма платежей");
            MainWindow.Instance?.ShowBackButton(true);

            // Область построения (как в методичке)
            ChartPayments.ChartAreas.Clear();
            ChartPayments.ChartAreas.Add(new ChartArea("Main"));

            // Серия данных
            ChartPayments.Series.Clear();
            var currentSeries = new Series("Платежи")
            {
                IsValueShownAsLabel = true
            };
            ChartPayments.Series.Add(currentSeries);

            // Пользователи (аналог _context.User.ToList())
            CmbUser.ItemsSource = DatabaseService.GetAllUsers();
            if (CmbUser.Items.Count > 0)
                CmbUser.SelectedIndex = 0;

            // Типы диаграммы: Column, Pie, Doughnut, Line и т.д.
            CmbDiagram.ItemsSource = Enum.GetValues(typeof(SeriesChartType));
            CmbDiagram.SelectedItem = SeriesChartType.Column;

            UpdateChart(null, null);
        }

        /// <summary>
        /// Обновление диаграммы — метод UpdateChart из методички
        /// </summary>
        private void UpdateChart(object sender, SelectionChangedEventArgs e)
        {
            if (!(CmbUser.SelectedItem is User currentUser))
                return;
            if (!(CmbDiagram.SelectedItem is SeriesChartType currentType))
                return;

            Series currentSeries = ChartPayments.Series.FirstOrDefault();
            if (currentSeries == null) return;

            currentSeries.ChartType = currentType;
            currentSeries.Points.Clear();

            var categoriesList = DatabaseService.GetAllCategories();
            var payments = DatabaseService.GetPayments(userId: currentUser.ID);

            foreach (var category in categoriesList)
            {
                double sum = (double)payments
                    .Where(p => p.CategoryID == category.ID)
                    .Sum(p => p.Price * p.Num);

                currentSeries.Points.AddXY(category.Name, sum);
            }
        }

        private void BtnExportWord_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var payments = DatabaseService.GetPayments();
                var categories = DatabaseService.GetAllCategories();
                ExportService.ExportToWord(payments, categories, null);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка экспорта в Word:\n" + ex.Message);
            }
        }

        private void BtnExportExcel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var payments = DatabaseService.GetPayments();
                var categories = DatabaseService.GetAllCategories();
                ExportService.ExportToExcel(payments, categories);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка экспорта в Excel:\n" + ex.Message);
            }
        }
    }
}
