using System;
using System.Windows;
using System.Windows.Threading;
using MeryanovPayment.Pages;
using MeryanovPayment.Services;

namespace MeryanovPayment
{
    public partial class MainWindow : Window
    {
        public static MainWindow Instance { get; private set; }

        public MainWindow()
        {
            InitializeComponent();
            Instance = this;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Таймер даты и времени
            var timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            timer.Tick += (o, t) =>
            {
                DateTimeNow.Text = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss");
            };
            timer.Start();
            DateTimeNow.Text = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss");

            // Стартовая страница — Авторизация
            MainFrame.Navigate(new AuthPage());
            SetPageTitle("Авторизация");
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            var result = MessageBox.Show(
                "Вы действительно хотите выйти из приложения?",
                "Подтверждение выхода",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.No)
                e.Cancel = true;
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            if (MainFrame.CanGoBack)
                MainFrame.GoBack();
        }

        private void BtnTheme_Click(object sender, RoutedEventArgs e)
        {
            ThemeService.ToggleTheme();
        }

        public void SetPageTitle(string title)
        {
            TxtPageTitle.Text = title;
            Title = $"Система учёта платежей — {title}";
        }

        public void ShowBackButton(bool show)
        {
            BtnBack.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        }

        public void SetCurrentUser(string fio, string role)
        {
            TxtCurrentUser.Text = string.IsNullOrEmpty(fio) ? "" : $"{fio} ({role})";
        }

        public void NavigateTo(System.Windows.Controls.Page page, string title, bool showBack = true)
        {
            MainFrame.Navigate(page);
            SetPageTitle(title);
            ShowBackButton(showBack);
        }
    }
}
