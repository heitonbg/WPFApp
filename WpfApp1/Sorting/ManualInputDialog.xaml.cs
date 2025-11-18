using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace WpfApp1
{
    public partial class ManualInputDialog : Window
    {
        public List<int> Data { get; private set; }

        public ManualInputDialog(List<int> currentData)
        {
            InitializeComponent();
            Data = new List<int>(currentData);
            txtInput.Text = string.Join(Environment.NewLine, currentData);
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var lines = txtInput.Text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                var newData = new List<int>();

                foreach (var line in lines)
                {
                    if (int.TryParse(line.Trim(), out int value))
                    {
                        newData.Add(value);
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