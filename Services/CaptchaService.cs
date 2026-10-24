using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Media.Imaging;

namespace MeryanovPayment.Services
{
    /// <summary>
    /// Генерация и проверка капчи
    /// </summary>
    public static class CaptchaService
    {
        private static readonly Random _rnd = new Random();
        private static string _currentCode = "";

        public static string CurrentCode => _currentCode;

        /// <summary>
        /// Генерирует новый код капчи (4–5 символов)
        /// </summary>
        public static string GenerateCode(int length = 5)
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // без похожих символов
            char[] code = new char[length];
            for (int i = 0; i < length; i++)
                code[i] = chars[_rnd.Next(chars.Length)];
            _currentCode = new string(code);
            return _currentCode;
        }

        public static bool Validate(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return false;
            return string.Equals(input.Trim(), _currentCode, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Создаёт изображение капчи
        /// </summary>
        public static BitmapImage GenerateImage(int width = 180, int height = 50)
        {
            if (string.IsNullOrEmpty(_currentCode))
                GenerateCode();

            using (var bmp = new Bitmap(width, height))
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.FromArgb(243, 243, 243));

                // Шум — линии
                using (var pen = new Pen(Color.FromArgb(150, 150, 150), 1))
                {
                    for (int i = 0; i < 8; i++)
                    {
                        g.DrawLine(pen,
                            _rnd.Next(width), _rnd.Next(height),
                            _rnd.Next(width), _rnd.Next(height));
                    }
                }

                // Точки шума
                for (int i = 0; i < 80; i++)
                {
                    bmp.SetPixel(_rnd.Next(width), _rnd.Next(height),
                        Color.FromArgb(_rnd.Next(100, 200), _rnd.Next(100, 200), _rnd.Next(100, 200)));
                }

                // Текст
                using (var font = new Font("Arial", 22, FontStyle.Bold | FontStyle.Italic))
                using (var brush = new SolidBrush(Color.FromArgb(35, 62, 118)))
                {
                    var format = new StringFormat
                    {
                        Alignment = StringAlignment.Center,
                        LineAlignment = StringAlignment.Center
                    };
                    g.DrawString(_currentCode, font, brush,
                        new RectangleF(0, 0, width, height), format);
                }

                // Конвертация в BitmapImage для WPF
                using (var ms = new MemoryStream())
                {
                    bmp.Save(ms, ImageFormat.Png);
                    ms.Position = 0;
                    var image = new BitmapImage();
                    image.BeginInit();
                    image.CacheOption = BitmapCacheOption.OnLoad;
                    image.StreamSource = ms;
                    image.EndInit();
                    image.Freeze();
                    return image;
                }
            }
        }
    }
}
