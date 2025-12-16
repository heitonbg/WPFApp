using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace WpfApp1
{
    public partial class ManualInputDialog : Window
    {
        public List<double> Data { get; private set; }

        public ManualInputDialog(List<double> currentData)
        {
            InitializeComponent();
            Data = new List<double>(currentData);
            txtInput.Text = string.Join(Environment.NewLine, currentData.Select(x => x.ToString("F3")));
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var lines = txtInput.Text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                var newData = new List<double>();

                foreach (var line in lines)
                {
                    if (double.TryParse(line.Trim(),
                        System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture,
                        out double value))
                    {
                        newData.Add(Math.Round(value, 3)); 
                    }
                    else
                    {
                        MessageBox.Show($"Некорректное значение: {line}", "Ошибка",
                                      MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }

                if (newData.Any())
                {
                    Data = newData;
                    DialogResult = true;
                    Close();
                }
                else
                {
                    MessageBox.Show("Введите хотя бы одно числовое значение!", "Ошибка",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при обработке данных: {ex.Message}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}