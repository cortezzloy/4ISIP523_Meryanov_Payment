using System.Configuration;
using System.Windows;

namespace MeryanovPayment.Services
{
    public static class ThemeService
    {
        private static string _currentTheme = "Light";

        public static string CurrentTheme => _currentTheme;

        public static void ApplyTheme(string theme)
        {
            _currentTheme = theme == "Dark" ? "Dark" : "Light";

            var dict = new ResourceDictionary();
            if (_currentTheme == "Dark")
                dict.Source = new System.Uri("Styles/DarkTheme.xaml", System.UriKind.Relative);
            else
                dict.Source = new System.Uri("Styles/LightTheme.xaml", System.UriKind.Relative);

            // Удаляем старую тему и добавляем новую
            var appResources = Application.Current.Resources.MergedDictionaries;
            // Ищем и заменяем тему (последний словарь обычно тема)
            for (int i = appResources.Count - 1; i >= 0; i--)
            {
                var src = appResources[i].Source?.OriginalString ?? "";
                if (src.Contains("LightTheme") || src.Contains("DarkTheme"))
                {
                    appResources.RemoveAt(i);
                    break;
                }
            }
            appResources.Add(dict);

            // Сохраняем выбор
            SaveThemeSetting(_currentTheme);
        }

        public static void ToggleTheme()
        {
            ApplyTheme(_currentTheme == "Light" ? "Dark" : "Light");
        }

        private static void SaveThemeSetting(string theme)
        {
            try
            {
                var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                if (config.AppSettings.Settings["Theme"] != null)
                    config.AppSettings.Settings["Theme"].Value = theme;
                else
                    config.AppSettings.Settings.Add("Theme", theme);
                config.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection("appSettings");
            }
            catch
            {
                // Игнорируем ошибки записи конфига (в режиме отладки иногда нет прав)
            }
        }
    }
}
