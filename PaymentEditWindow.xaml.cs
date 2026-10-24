using System;
using System.Globalization;
using System.Windows;
using MeryanovPayment.Models;
using MeryanovPayment.Services;

namespace MeryanovPayment
{
    public partial class PaymentEditWindow : Window
    {
        private readonly PaymentView _existing;

        public PaymentEditWindow(PaymentView existing)
        {
            InitializeComponent();
            _existing = existing;

            CmbCategory.ItemsSource = DatabaseService.GetAllCategories();

            if (existing != null)
            {
                TxtTitle.Text = "Редактирование платежа";
                CmbCategory.SelectedValue = existing.CategoryID;
                TxtName.Text = existing.Name;
                DpDate.SelectedDate = existing.Date;
                TxtNum.Text = existing.Num.ToString();
                TxtPrice.Text = existing.Price.ToString(CultureInfo.InvariantCulture);
            }
            else
            {
                DpDate.SelectedDate = DateTime.Today;
                if (CmbCategory.Items.Count > 0)
                    CmbCategory.SelectedIndex = 0;
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            TxtError.Visibility = Visibility.Collapsed;

            if (!(CmbCategory.SelectedValue is int catId))
            {
                ShowError("Выберите категорию");
                return;
            }
            string name = TxtName.Text?.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                ShowError("Введите название");
                return;
            }
            if (!DpDate.SelectedDate.HasValue)
            {
                ShowError("Укажите дату");
                return;
            }
            if (!int.TryParse(TxtNum.Text, out int num) || num <= 0)
            {
                ShowError("Количество должно быть целым числом больше 0");
                return;
            }
            if (!decimal.TryParse(TxtPrice.Text.Replace(',', '.'), NumberStyles.Any,
                    CultureInfo.InvariantCulture, out decimal price) || price < 0)
            {
                ShowError("Цена должна быть числом ≥ 0");
                return;
            }

            try
            {
                if (_existing == null)
                {
                    // Добавление — привязываем к текущему пользователю (админ может добавлять за себя)
                    DatabaseService.AddPayment(catId, SessionService.CurrentUser.ID,
                        DpDate.SelectedDate.Value, name, num, price);
                }
                else
                {
                    DatabaseService.UpdatePayment(_existing.ID, catId,
                        DpDate.SelectedDate.Value, name, num, price);
                }
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                ShowError("Ошибка сохранения: " + ex.Message);
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void ShowError(string msg)
        {
            TxtError.Text = msg;
            TxtError.Visibility = Visibility.Visible;
        }
    }
}
