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
        private List<double> originalData = new List<double>();

        public SortingWindow()
        {
            InitializeComponent();
            GenerateRandomData();
        }

        private void GenerateRandomData()
        {
            try
            {
                if (!double.TryParse(txtMinValue.Text, out double min) ||
                    !double.TryParse(txtMaxValue.Text, out double max) ||
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

                // Убрано ограничение на количество элементов
                if (count <= 0)
                {
                    MessageBox.Show("Количество элементов должно быть больше 0!", "Ошибка",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Предупреждение для большого количества элементов
                if (count > 10000)
                {
                    var result = MessageBox.Show($"Вы собираетесь сгенерировать {count} элементов. Это может занять значительное время и память. Продолжить?",
                                               "Предупреждение", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    if (result == MessageBoxResult.No)
                        return;
                }

                Random rand = new Random();
                originalData.Clear();

                for (int i = 0; i < count; i++)
                {
                    // Генерация double чисел с точностью до тысячных
                    double randomValue = min + (rand.NextDouble() * (max - min));
                    randomValue = Math.Round(randomValue, 3); // Округление до 3 знаков после запятой
                    originalData.Add(randomValue);
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
                    originalData = dataItems.Select(item => (double)item.Значение).ToList();
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
                List<double> data = new List<double>();
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
                    // Предупреждение для большого количества элементов
                    if (data.Count > 10000)
                    {
                        var result = MessageBox.Show($"Файл содержит {data.Count} элементов. Это может занять значительное время и память. Продолжить?",
                                                   "Предупреждение", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                        if (result == MessageBoxResult.No)
                            return;
                    }

                    originalData = data;
                    UpdateDataGrid();
                    txtResults.Text = $"УСПЕШНО ЗАГРУЖЕНО ИЗ ФАЙЛА:\n";
                    txtResults.Text += $"Файл: {fileName}\n";
                    txtResults.Text += $"Формат: {extension.ToUpper()}\n";
                    txtResults.Text += $"Количество элементов: {data.Count}\n";
                    txtResults.Text += $"Диапазон данных: {data.Min():F3} - {data.Max():F3}\n";

                    // Показываем только первые 10 элементов для больших наборов
                    if (data.Count > 10)
                    {
                        txtResults.Text += $"Первые 10 элементов: {string.Join(", ", data.Take(10).Select(x => x.ToString("F3")))}...\n\n";
                    }
                    else
                    {
                        txtResults.Text += $"Элементы: {string.Join(", ", data.Select(x => x.ToString("F3")))}\n\n";
                    }
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

        private List<double> LoadFromExcel(string filePath)
        {
            List<double> data = new List<double>();

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
                            if (double.TryParse(stringValue.Trim(),
                                System.Globalization.NumberStyles.Any,
                                System.Globalization.CultureInfo.InvariantCulture,
                                out double number))
                            {
                                data.Add(Math.Round(number, 3)); // Округление до 3 знаков
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

        private List<double> LoadFromTextFile(string filePath)
        {
            List<double> data = new List<double>();

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

                    if (double.TryParse(cleanedValue,
                        System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture,
                        out double number))
                    {
                        data.Add(Math.Round(number, 3)); // Округление до 3 знаков
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

                List<double> data = dataItems.Select(item => (double)item.Значение).ToList();
                originalData = new List<double>(data);

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

        private void PerformSorting(List<double> data, bool ascending)
        {
            Dictionary<string, (TimeSpan Time, int Iterations, bool IsCompleted)> results = new Dictionary<string, (TimeSpan, int, bool)>();
            Dictionary<string, List<double>> sortedData = new Dictionary<string, List<double>>();

            // Получаем максимальное количество итераций для Bogo
            int maxBogoIterations = 0;
            if (!int.TryParse(txtMaxBogoIterations.Text, out maxBogoIterations) || maxBogoIterations < 0)
            {
                maxBogoIterations = 0; // 0 = без ограничений
            }

            txtResults.Text += "ВЫПОЛНЕНИЕ СОРТИРОВКИ:\n\n";

            // Предупреждение для Bogo сортировки с большим количеством элементов
            if (cbBogo.IsChecked == true && data.Count > 12)
            {
                var result = MessageBox.Show($"Bogo сортировка для {data.Count} элементов может занять очень много времени.\n" +
                                           $"Максимальное количество итераций: {(maxBogoIterations == 0 ? "без ограничений" : maxBogoIterations.ToString())}\n" +
                                           "Продолжить?",
                                           "Предупреждение", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.No)
                {
                    cbBogo.IsChecked = false;
                }
            }

            if (cbBubble.IsChecked == true)
            {
                var dataCopy = new List<double>(data);
                var result = sorter.BubbleSort(dataCopy, ascending);
                results.Add("Пузырьковая", (result.Time, result.Iterations, result.IsCompleted));
                sortedData.Add("Пузырьковая", dataCopy);
                txtResults.Text += $"Пузырьковая: {result.Time.TotalMilliseconds:F3} мс, {result.Iterations} итераций\n";
            }

            if (cbInsertion.IsChecked == true)
            {
                var dataCopy = new List<double>(data);
                var result = sorter.InsertionSort(dataCopy, ascending);
                results.Add("Вставками", (result.Time, result.Iterations, result.IsCompleted));
                sortedData.Add("Вставками", dataCopy);
                txtResults.Text += $"Вставками: {result.Time.TotalMilliseconds:F3} мс, {result.Iterations} итераций\n";
            }

            if (cbShaker.IsChecked == true)
            {
                var dataCopy = new List<double>(data);
                var result = sorter.ShakerSort(dataCopy, ascending);
                results.Add("Шейкерная", (result.Time, result.Iterations, result.IsCompleted));
                sortedData.Add("Шейкерная", dataCopy);
                txtResults.Text += $"Шейкерная: {result.Time.TotalMilliseconds:F3} мс, {result.Iterations} итераций\n";
            }

            if (cbQuick.IsChecked == true)
            {
                var dataCopy = new List<double>(data);
                var result = sorter.QuickSort(dataCopy, ascending);
                results.Add("Быстрая", (result.Time, result.Iterations, result.IsCompleted));
                sortedData.Add("Быстрая", dataCopy);
                txtResults.Text += $"Быстрая: {result.Time.TotalMilliseconds:F3} мс, {result.Iterations} итераций\n";
            }

            if (cbBogo.IsChecked == true)
            {
                var dataCopy = new List<double>(data);
                var result = sorter.BogoSort(dataCopy, ascending, maxBogoIterations);
                results.Add("BOGO", (result.Time, result.Iterations, result.IsCompleted));
                sortedData.Add("BOGO", dataCopy);
                string status = result.IsCompleted ? "" : " [ПРЕРВАНА]";
                txtResults.Text += $"BOGO: {result.Time.TotalMilliseconds:F3} мс, {result.Iterations} итераций{status}\n";
            }

            VisualizeAllResults(sortedData);
            DisplayFinalResults(results);
        }

        private void DisplayFinalResults(Dictionary<string, (TimeSpan Time, int Iterations, bool IsCompleted)> results)
        {
            if (results.Any())
            {
                var completedResults = results.Where(r => r.Value.IsCompleted);

                txtResults.Text += $"\nРЕЗУЛЬТАТЫ:\n";
                txtResults.Text += $"Всего алгоритмов: {results.Count}\n";
                txtResults.Text += $"Завершено: {completedResults.Count()}\n";
                if (results.Any(r => !r.Value.IsCompleted))
                {
                    txtResults.Text += $"Прервано: {results.Count(r => !r.Value.IsCompleted)}\n";
                }
                txtResults.Text += "\n";

                foreach (var result in results.OrderBy(r => r.Value.Time))
                {
                    string status = result.Value.IsCompleted ? "" : " [ПРЕРВАНА]";
                    string timeMarker = completedResults.Any() && result.Key == completedResults.OrderBy(r => r.Value.Time).First().Key ? "[БЫСТРЕЙШИЙ] " : "";

                    txtResults.Text += $"{timeMarker}{result.Key}{status}:\n";
                    txtResults.Text += $"   Время: {result.Value.Time.TotalMilliseconds:F3} мс\n";
                    txtResults.Text += $"   Итерации: {result.Value.Iterations}\n\n";
                }

                if (completedResults.Any())
                {
                    var fastest = completedResults.OrderBy(r => r.Value.Time).First();
                    var slowest = completedResults.OrderBy(r => r.Value.Time).Last();
                    var leastIterations = completedResults.OrderBy(r => r.Value.Iterations).First();

                    txtResults.Text += $"САМЫЙ БЫСТРЫЙ: {fastest.Key} ({fastest.Value.Time.TotalMilliseconds:F3} мс)\n";
                    txtResults.Text += $"САМЫЙ МЕДЛЕННЫЙ: {slowest.Key} ({slowest.Value.Time.TotalMilliseconds:F3} мс)\n";
                    txtResults.Text += $"МЕНЬШЕ ВСЕХ ИТЕРАЦИЙ: {leastIterations.Key} ({leastIterations.Value.Iterations} итераций)\n";
                }
            }
        }

        private void VisualizeAllResults(Dictionary<string, List<double>> sortedData)
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

            double globalMin = sortedData.Min(algorithm => algorithm.Value.Min());
            double globalMax = sortedData.Max(algorithm => algorithm.Value.Max());
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
                                Text = data[i].ToString("F2"), // Отображение с 2 знаками после запятой
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