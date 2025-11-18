using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Microsoft.Win32;
using System.IO;
using System.Text;
using OfficeOpenXml;

namespace WpfApp1
{
    public partial class SortingWindow : Window
    {
        private SortingAlgorithms sorter = new SortingAlgorithms();
        private List<int> originalData = new List<int>();

        public SortingWindow()
        {
            InitializeComponent();
            GenerateRandomData();
        }

        private void GenerateRandomData()
        {
            try
            {
                if (!int.TryParse(txtMinValue.Text, out int min) ||
                    !int.TryParse(txtMaxValue.Text, out int max) ||
                    !int.TryParse(txtElementCount.Text, out int count))
                {
                    MessageBox.Show("Введите корректные числовые значения для генерации!", "Ошибка",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (min >= max)
                {
                    MessageBox.Show("Минимальное значение должно быть меньше максимального!", "Ошибка",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (count <= 0 || count > 1000) 
                {
                    MessageBox.Show("Количество элементов должно быть от 1 до 1000!", "Ошибка",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                Random rand = new Random();
                originalData.Clear();

                for (int i = 0; i < count; i++) 
                {
                    originalData.Add(rand.Next(min, max + 1));
                }

                UpdateDataGrid();
                txtResults.Text = $"Данные сгенерированы: {count} элементов в диапазоне {min} - {max}\n\n";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при генерации данных: {ex.Message}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateDataGrid()
        {
            dataGrid.ItemsSource = originalData.Select((value, index) =>
                new { Индекс = index + 1, Значение = value }).ToList();
        }

        private void DataGrid_BeginningEdit(object sender, DataGridBeginningEditEventArgs e)
        {
        }

        private void GenerateData_Click(object sender, RoutedEventArgs e)
        {
            GenerateRandomData();
            visualizationCanvas.Children.Clear();
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            originalData.Clear();
            UpdateDataGrid();
            txtResults.Clear();
            visualizationCanvas.Children.Clear();
        }

        private void ManualInput_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dataItems = dataGrid.ItemsSource as IEnumerable<dynamic>;
                if (dataItems != null && dataItems.Any())
                {
                    originalData = dataItems.Select(item => (int)item.Значение).ToList();
                }

                var inputDialog = new ManualInputDialog(originalData);
                if (inputDialog.ShowDialog() == true)
                {
                    originalData = inputDialog.Data;
                    UpdateDataGrid();
                    txtResults.Text = "Данные обновлены вручную\n\n";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при ручном вводе: {ex.Message}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadExcel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OpenFileDialog openFileDialog = new OpenFileDialog
                {
                    Filter = "Excel файлы (*.xlsx)|*.xlsx",
                    Title = "Выберите файл с данными"
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    LoadDataFromFile(openFileDialog.FileName);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке файла: {ex.Message}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadDataFromFile(string filePath)
        {
            try
            {
                List<int> data = new List<int>();
                string extension = System.IO.Path.GetExtension(filePath).ToLower();
                string fileName = System.IO.Path.GetFileName(filePath);

                if (extension == ".xlsx")
                {
                    data = LoadFromExcel(filePath);
                }
                else
                {
                    data = LoadFromTextFile(filePath);
                }

                if (data.Any())
                {
                    originalData = data;
                    UpdateDataGrid();
                    txtResults.Text = $"УСПЕШНО ЗАГРУЖЕНО ИЗ ФАЙЛА:\n";
                    txtResults.Text += $"Файл: {fileName}\n";
                    txtResults.Text += $"Формат: {extension.ToUpper()}\n";
                    txtResults.Text += $"Количество элементов: {data.Count}\n";
                    txtResults.Text += $"Диапазон данных: {data.Min()} - {data.Max()}\n";
                    txtResults.Text += $"Первые 10 элементов: {string.Join(", ", data.Take(10))}" + (data.Count > 10 ? "..." : "") + "\n\n";
                }
                else
                {
                    MessageBox.Show("Не удалось найти числовые данные в файле.\n\n" +
                                  "Поддерживаемые форматы:\n" +
                                  "- XLSX: числа в первом столбце первого листа\n",
                                  "Информация",
                                  MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при чтении файла:\n{ex.Message}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private List<int> LoadFromExcel(string filePath)
        {
            List<int> data = new List<int>();

            try
            {
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                using (var package = new ExcelPackage(new FileInfo(filePath)))
                {
                    if (package.Workbook.Worksheets.Count == 0)
                    {
                        MessageBox.Show("Файл Excel не содержит листов", "Ошибка",
                                      MessageBoxButton.OK, MessageBoxImage.Error);
                        return data;
                    }

                    var worksheet = package.Workbook.Worksheets[0]; 
                    var start = worksheet.Dimension.Start;
                    var end = worksheet.Dimension.End;

                    for (int row = start.Row; row <= end.Row; row++)
                    {
                        var cellValue = worksheet.Cells[row, 1].Value;
                        if (cellValue != null)
                        {
                            string stringValue = cellValue.ToString();
                            if (int.TryParse(stringValue.Trim(), out int number))
                            {
                                data.Add(number);
                            }
                            else if (double.TryParse(stringValue.Trim(),
                                System.Globalization.NumberStyles.Any,
                                System.Globalization.CultureInfo.InvariantCulture,
                                out double doubleValue))
                            {
                                data.Add((int)Math.Round(doubleValue));
                            }
                        }
                    }
                }

                if (data.Count == 0)
                {
                    MessageBox.Show("Не найдено числовых данных в первом столбце Excel файла", "Информация",
                                  MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при чтении Excel файла:\n{ex.Message}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return data;
        }

        private List<int> LoadFromTextFile(string filePath)
        {
            List<int> data = new List<int>();

            Encoding encoding = DetectEncoding(filePath);

            string[] lines = File.ReadAllLines(filePath, encoding);

            foreach (string line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                string[] values = line.Split(new[] { ',', ';', ' ', '\t', '|', ':', '~' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (string value in values)
                {
                    string cleanedValue = value.Trim()
                        .Replace("\"", "")
                        .Replace("'", "")
                        .Replace("(", "")
                        .Replace(")", "")
                        .Replace("[", "")
                        .Replace("]", "")
                        .Replace("{", "")
                        .Replace("}", "");

                    if (int.TryParse(cleanedValue, out int number))
                    {
                        data.Add(number);
                    }
                    else
                    {
                        if (double.TryParse(cleanedValue,
                            System.Globalization.NumberStyles.Any,
                            System.Globalization.CultureInfo.InvariantCulture,
                            out double doubleValue))
                        {
                            data.Add((int)Math.Round(doubleValue));
                        }
                    }
                }
            }

            return data;
        }

        private Encoding DetectEncoding(string filePath)
        {
            try
            {
                byte[] buffer = new byte[4];
                using (FileStream file = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    file.Read(buffer, 0, 4);
                }

                if (buffer[0] == 0xef && buffer[1] == 0xbb && buffer[2] == 0xbf)
                    return Encoding.UTF8;
                else if (buffer[0] == 0xff && buffer[1] == 0xfe)
                    return Encoding.Unicode;
                else if (buffer[0] == 0xfe && buffer[1] == 0xff)
                    return Encoding.BigEndianUnicode;
                else
                    return Encoding.Default;
            }
            catch
            {
                return Encoding.Default;
            }
        }

        private void Sort_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!IsAnyAlgorithmSelected())
                {
                    MessageBox.Show("Выберите хотя бы один алгоритм сортировки!", "Внимание",
                                  MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var dataItems = dataGrid.ItemsSource as IEnumerable<dynamic>;
                if (dataItems == null || !dataItems.Any())
                {
                    MessageBox.Show("Нет данных для сортировки!", "Ошибка",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                List<int> data = dataItems.Select(item => (int)item.Значение).ToList();
                originalData = new List<int>(data);

                bool ascending = comboOrder.SelectedIndex == 0;
                txtResults.Clear();
                visualizationCanvas.Children.Clear();

                PerformSorting(data, ascending);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при выполнении сортировки:\n{ex.Message}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool IsAnyAlgorithmSelected()
        {
            return cbBubble.IsChecked == true ||
                   cbInsertion.IsChecked == true ||
                   cbShaker.IsChecked == true ||
                   cbQuick.IsChecked == true ||
                   cbBogo.IsChecked == true;
        }

        private void PerformSorting(List<int> data, bool ascending)
        {
            Dictionary<string, (TimeSpan Time, int Iterations)> results = new Dictionary<string, (TimeSpan, int)>();
            Dictionary<string, List<int>> sortedData = new Dictionary<string, List<int>>();

            txtResults.Text += "ВЫПОЛНЕНИЕ СОРТИРОВКИ:\n\n";

            if (cbBogo.IsChecked == true && data.Count > 12)
            {
                MessageBox.Show($"Bogo сортировка отключена для {data.Count} элементов (максимум 12 элементов)\n" +
                               "Используйте меньшее количество данных для Bogo сортировки.",
                               "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                cbBogo.IsChecked = false;
            }

            if (cbBubble.IsChecked == true)
            {
                var dataCopy = new List<int>(data);
                var result = sorter.BubbleSort(dataCopy, ascending);
                results.Add("Пузырьковая", (result.Time, result.Iterations));
                sortedData.Add("Пузырьковая", dataCopy);
                txtResults.Text += $"Пузырьковая: {result.Time.TotalMilliseconds:F3} мс, {result.Iterations} итераций\n";
            }

            if (cbInsertion.IsChecked == true)
            {
                var dataCopy = new List<int>(data);
                var result = sorter.InsertionSort(dataCopy, ascending);
                results.Add("Вставками", (result.Time, result.Iterations));
                sortedData.Add("Вставками", dataCopy);
                txtResults.Text += $"Вставками: {result.Time.TotalMilliseconds:F3} мс, {result.Iterations} итераций\n";
            }

            if (cbShaker.IsChecked == true)
            {
                var dataCopy = new List<int>(data);
                var result = sorter.ShakerSort(dataCopy, ascending);
                results.Add("Шейкерная", (result.Time, result.Iterations));
                sortedData.Add("Шейкерная", dataCopy);
                txtResults.Text += $"Шейкерная: {result.Time.TotalMilliseconds:F3} мс, {result.Iterations} итераций\n";
            }

            if (cbQuick.IsChecked == true)
            {
                var dataCopy = new List<int>(data);
                var result = sorter.QuickSort(dataCopy, ascending);
                results.Add("Быстрая", (result.Time, result.Iterations));
                sortedData.Add("Быстрая", dataCopy);
                txtResults.Text += $"Быстрая: {result.Time.TotalMilliseconds:F3} мс, {result.Iterations} итераций\n";
            }

            if (cbBogo.IsChecked == true)
            {
                if (data.Count <= 12) 
                {
                    var dataCopy = new List<int>(data);
                    var result = sorter.BogoSort(dataCopy, ascending);
                    results.Add("BOGO", (result.Time, result.Iterations));
                    sortedData.Add("BOGO", dataCopy);
                    txtResults.Text += $"BOGO: {result.Time.TotalMilliseconds:F3} мс, {result.Iterations} итераций\n";
                }
                else
                {
                    txtResults.Text += $"BOGO: ПРОПУЩЕНА (слишком много данных: {data.Count} > 12)\n";
                }
            }

            VisualizeAllResults(sortedData);
            DisplayFinalResults(results);
        }

        private void DisplayFinalResults(Dictionary<string, (TimeSpan Time, int Iterations)> results)
        {
            if (results.Any())
            {
                var fastest = results.OrderBy(r => r.Value.Time).First();
                var slowest = results.OrderBy(r => r.Value.Time).Last();
                var leastIterations = results.OrderBy(r => r.Value.Iterations).First();

                txtResults.Text += $"\nРЕЗУЛЬТАТЫ:\n";
                txtResults.Text += $"Всего алгоритмов: {results.Count}\n\n";

                foreach (var result in results.OrderBy(r => r.Value.Time))
                {
                    string timeMarker = result.Key == fastest.Key ? "[БЫСТРЕЙШИЙ] " : "";
                    string iterMarker = result.Key == leastIterations.Key ? "[МЕНЬШЕ ИТЕРАЦИЙ] " : "";
                    txtResults.Text += $"{timeMarker}{result.Key}:\n";
                    txtResults.Text += $"   Время: {result.Value.Time.TotalMilliseconds:F3} мс\n";
                    txtResults.Text += $"   {iterMarker}Итерации: {result.Value.Iterations}\n\n";
                }

                txtResults.Text += $"САМЫЙ БЫСТРЫЙ: {fastest.Key} ({fastest.Value.Time.TotalMilliseconds:F3} мс)\n";
                txtResults.Text += $"САМЫЙ МЕДЛЕННЫЙ: {slowest.Key} ({slowest.Value.Time.TotalMilliseconds:F3} мс)\n";
                txtResults.Text += $"МЕНЬШЕ ВСЕХ ИТЕРАЦИЙ: {leastIterations.Key} ({leastIterations.Value.Iterations} итераций)\n";
            }
        }

        private void VisualizeAllResults(Dictionary<string, List<int>> sortedData)
        {
            visualizationCanvas.Children.Clear();

            if (!sortedData.Any()) return;

            double canvasWidth = visualizationCanvas.ActualWidth;
            double canvasHeight = visualizationCanvas.ActualHeight;

            if (canvasWidth <= 0 || canvasHeight <= 0)
            {
                canvasWidth = 800;
                canvasHeight = 200;
            }

            int algorithmCount = sortedData.Count;
            int dataCount = sortedData.First().Value.Count;

            if (dataCount > 100)
            {
                TextBlock warningText = new TextBlock
                {
                    Text = $"Визуализация отключена для {dataCount} элементов (максимум 100)\n" +
                           "Сортировка выполнена, но графическое отображение доступно только для небольших наборов данных.",
                    TextWrapping = TextWrapping.Wrap,
                    Foreground = Brushes.OrangeRed,
                    FontSize = 12,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };

                Canvas.SetLeft(warningText, 10);
                Canvas.SetTop(warningText, canvasHeight / 2 - 20);
                visualizationCanvas.Children.Add(warningText);
                return;
            }

            double sectionWidth = canvasWidth / algorithmCount;

            Color[] colors = { Colors.Blue, Colors.Green, Colors.Orange, Colors.Red, Colors.Purple };
            int colorIndex = 0;

            int globalMin = sortedData.Min(algorithm => algorithm.Value.Min());
            int globalMax = sortedData.Max(algorithm => algorithm.Value.Max());
            double dataRange = globalMax - globalMin;

            if (dataRange == 0) dataRange = 1;

            double scale = (canvasHeight - 60) / dataRange;

            double minBarWidth = 2.0; 
            double barWidth = Math.Max(minBarWidth, (sectionWidth - 30) / dataCount);

            foreach (var algorithm in sortedData)
            {
                var data = algorithm.Value;
                double startX = colorIndex * sectionWidth + 15;

                TextBlock label = new TextBlock
                {
                    Text = algorithm.Key,
                    Foreground = new SolidColorBrush(colors[colorIndex % colors.Length]),
                    FontSize = 11,
                    FontWeight = FontWeights.SemiBold
                };
                Canvas.SetTop(label, 5);
                Canvas.SetLeft(label, startX);
                visualizationCanvas.Children.Add(label);

                for (int i = 0; i < data.Count; i++)
                {
                    double barHeight = (data[i] - globalMin) * scale;

                    if (barHeight < 2) barHeight = 2;

                    Rectangle bar = new Rectangle
                    {
                        Width = barWidth - 1,
                        Height = barHeight,
                        Fill = new SolidColorBrush(colors[colorIndex % colors.Length]),
                        Stroke = Brushes.White,
                        StrokeThickness = 0.5
                    };

                    double left = startX + i * barWidth;
                    double top = canvasHeight - barHeight - 40;

                    if (left >= 0 && left + barWidth <= canvasWidth && top >= 0)
                    {
                        Canvas.SetLeft(bar, left);
                        Canvas.SetTop(bar, top);
                        visualizationCanvas.Children.Add(bar);

                        if (barWidth >= 15)
                        {
                            TextBlock valueLabel = new TextBlock
                            {
                                Text = data[i].ToString(),
                                FontSize = 8,
                                Foreground = Brushes.Black,
                                HorizontalAlignment = HorizontalAlignment.Center
                            };
                            Canvas.SetLeft(valueLabel, left);
                            Canvas.SetTop(valueLabel, top - 15);
                            visualizationCanvas.Children.Add(valueLabel);
                        }
                    }
                }

                colorIndex++;
            }

            if (globalMin < 0)
            {
                double zeroLineY = canvasHeight - 40 - (0 - globalMin) * scale;

                Line zeroLine = new Line
                {
                    X1 = 0,
                    X2 = canvasWidth,
                    Y1 = zeroLineY,
                    Y2 = zeroLineY,
                    Stroke = Brushes.Gray,
                    StrokeThickness = 1,
                    StrokeDashArray = new DoubleCollection { 4, 2 }
                };
                visualizationCanvas.Children.Add(zeroLine);

                TextBlock zeroLabel = new TextBlock
                {
                    Text = "0",
                    FontSize = 8,
                    Foreground = Brushes.Gray
                };
                Canvas.SetLeft(zeroLabel, 5);
                Canvas.SetTop(zeroLabel, zeroLineY - 10);
                visualizationCanvas.Children.Add(zeroLabel);
            }
        }
    }
}