using System;
using System.Text.RegularExpressions;
using System.Windows;

namespace WpfApp1
{
    public partial class GoogleSheetsDialogLeast : Window
    {
        public string Url { get; private set; }

        public GoogleSheetsDialogLeast()
        {
            InitializeComponent();
            txtUrl.Focus();
        }

        private void Load_Click(object sender, RoutedEventArgs e)
        {
            string url = txtUrl.Text.Trim();

            if (string.IsNullOrWhiteSpace(url))
            {
                MessageBox.Show("Введите ссылку на Google Sheets таблицу", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Проверяем, что это ссылка Google Sheets
            if (!IsValidGoogleSheetsUrl(url))
            {
                var result = MessageBox.Show(
                    "Ссылка не похожа на Google Sheets таблицу. Продолжить?",
                    "Предупреждение",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question
                );

                if (result == MessageBoxResult.No)
                    return;
            }

            Url = url;
            DialogResult = true;
            Close();
        }

        private bool IsValidGoogleSheetsUrl(string url)
        {
            try
            {
                // Проверяем основные паттерны Google Sheets URL
                return Regex.IsMatch(url, @"docs\.google\.com\/spreadsheets", RegexOptions.IgnoreCase) ||
                       Regex.IsMatch(url, @"\/d\/[a-zA-Z0-9-_]+", RegexOptions.IgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}