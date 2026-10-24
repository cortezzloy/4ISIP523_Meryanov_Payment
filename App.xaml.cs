using System;
using System.Configuration;
using System.Windows;
using MeryanovPayment.Services;

namespace MeryanovPayment
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Загрузка сохранённой темы
            string theme = ConfigurationManager.AppSettings["Theme"] ?? "Light";
            ThemeService.ApplyTheme(theme);
        }
    }
}
