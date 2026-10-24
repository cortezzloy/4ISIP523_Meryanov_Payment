using System.Linq;
using System.Windows.Controls;
using MeryanovPayment.Services;

namespace MeryanovPayment.Pages
{
    public partial class ChartPage : Page
    {
        public ChartPage()
        {
            InitializeComponent();
            MainWindow.Instance?.SetPageTitle("Диаграмма по категориям");
            MainWindow.Instance?.ShowBackButton(true);
            LoadChart();
        }

        private void LoadChart()
        {
            var data = DatabaseService.GetPaymentsByCategory();
            if (data.Count == 0)
            {
                ChartItems.ItemsSource = null;
                return;
            }

            decimal max = data.Max(x => x.Total);
            const double maxBarWidth = 400;

            var items = data.Select(x => new
            {
                Category = x.Category,
                Total = x.Total,
                BarWidth = max > 0 ? (double)(x.Total / max) * maxBarWidth : 0
            }).ToList();

            ChartItems.ItemsSource = items;
        }
    }
}
