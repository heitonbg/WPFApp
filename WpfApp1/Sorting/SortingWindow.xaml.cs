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
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

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

        private void LoadExcelFromMenu_Click(object sender, RoutedEventArgs e)
        {
            LoadExcel_Click(sender, e);
        }

        private void LoadGoogleSheetsFromMenu_Click(object sender, RoutedEventArgs e)
        {
            LoadGoogleSheets_Click(sender, e);
        }

        private void SaveResultsFromMenu_Click(object sender, RoutedEventArgs e)
        {
            SaveResults_Click(sender, e);
        }

        private void ExitFromMenu_Click(object sender, RoutedEventArgs e)
        {
            Exit_Click(sender, e);
        }

        private void GenerateDataFromMenu_Click(object sender, RoutedEventArgs e)
        {
            GenerateData_Click(sender, e);
        }

        private void ManualInputFromMenu_Click(object sender, RoutedEventArgs e)
        {
            ManualInput_Click(sender, e);
        }

        private void ClearFromMenu_Click(object sender, RoutedEventArgs e)
        {
            Clear_Click(sender, e);
        }

        private void SortFromMenu_Click(object sender, RoutedEventArgs e)
        {
            Sort_Click(sender, e);
        }

        private void SelectAllAlgorithmsFromMenu_Click(object sender, RoutedEventArgs e)
        {
            SelectAllAlgorithms_Click(sender, e);
        }

        private void DeselectAllAlgorithmsFromMenu_Click(object sender, RoutedEventArgs e)
        {
            DeselectAllAlgorithms_Click(sender, e);
        }

        private void ClearVisualizationFromMenu_Click(object sender, RoutedEventArgs e)
        {
            ClearVisualization_Click(sender, e);
        }

        private void AboutFromMenu_Click(object sender, RoutedEventArgs e)
        {
            About_Click(sender, e);
        }

        private void HelpFromMenu_Click(object sender, RoutedEventArgs e)
        {
            Help_Click(sender, e);
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void SelectAllAlgorithms_Click(object sender, RoutedEventArgs e)
        {
            cbBubble.IsChecked = true;
            cbInsertion.IsChecked = true;
            cbShaker.IsChecked = true;
            cbQuick.IsChecked = true;
            cbBogo.IsChecked = true;
        }

        private void DeselectAllAlgorithms_Click(object sender, RoutedEventArgs e)
        {
            cbBubble.IsChecked = false;
            cbInsertion.IsChecked = false;
            cbShaker.IsChecked = false;
            cbQuick.IsChecked = false;
            cbBogo.IsChecked = false;
        }

        private async void LoadGoogleSheets_Click(object sender, RoutedEventArgs e)
        {
            await ShowGoogleSheetsDialog();
        }

        private void SaveResults_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveDialog = new SaveFileDialog
            {
                Filter = "Текстовые файлы (*.txt)|*.txt|Все файлы (*.*)|*.*",
                Title = "Сохранить результаты",
                DefaultExt = ".txt"
            };

            if (saveDialog.ShowDialog() == true)
            {
                try
                {
                    File.WriteAllText(saveDialog.FileName, txtResults.Text);
                    MessageBox.Show("Результаты успешно сохранены!", "Сохранение",
                                  MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при сохранении: {ex.Message}", "Ошибка",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                "Олимпиадные сортировки v1.0\n\n" +
                "Программа для сравнения различных алгоритмов сортировки.\n" +
                "Поддерживает загрузку данных из различных источников:\n" +
                "- Файлы Excel (.xlsx)\n" +
                "- Google Sheets (по ссылке)\n" +
                "- Ручной ввод\n\n" +
                "Реализованные алгоритмы:\n" +
                "- Пузырьковая сортировка\n" +
                "- Сортировка вставками\n" +
                "- Шейкерная сортировка\n" +
                "- Быстрая сортировка\n" +
                "- BOGO сортировка\n\n" +
                "Визуализация",
                "О программе",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
        }

        private void Help_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                "Руководство пользователя:\n\n" +
                "1. Сгенерируйте или загрузите данные\n" +
                "   - Генерация: укажите диапазон и количество элементов\n" +
                "   - Загрузка из файла: поддерживается Excel (.xlsx)\n" +
                "   - Загрузка из Google Sheets: вставьте ссылку на таблицу\n" +
                "   - Ручной ввод: введите значения через Enter\n\n" +
                "2. Выберите алгоритмы сортировки\n" +
                "3. Укажите порядок сортировки (по возрастанию/убыванию)\n" +
                "4. Для Bogo сортировки задайте ограничение итераций (если нужно)\n" +
                "5. Нажмите 'Выполнить сортировку'\n\n" +
                "Советы:\n" +
                "- Bogo сортировка работает очень медленно на больших данных\n" +
                "- Для больших наборов данных используйте Быструю сортировку\n" +
                "- Визуализация отображается только для ≤100 элементов",
                "Руководство пользователя",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
        }

        private void ClearVisualization_Click(object sender, RoutedEventArgs e)
        {
            visualizationCanvas.Children.Clear();
        }

        private async Task ShowGoogleSheetsDialog()
        {
            try
            {
                var dialog = new GoogleSheetsDialog();
                if (dialog.ShowDialog() == true)
                {
                    string url = dialog.Url;
                    await LoadDataFromGoogleSheets(url);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при открытии диалога Google Sheets: {ex.Message}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadDataFromGoogleSheets(string url)
        {
            try
            {
                txtResults.Text = "Загрузка данных из Google Sheets...\n";

                string sheetId = ExtractSheetId(url);
                if (string.IsNullOrEmpty(sheetId))
                {
                    MessageBox.Show("Не удалось извлечь ID таблицы из ссылки. Убедитесь, что ссылка корректна.", "Ошибка",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                string csvUrl = $"https://docs.google.com/spreadsheets/d/{sheetId}/export?format=csv";

                using (HttpClient client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(30);

                    HttpResponseMessage response = await client.GetAsync(csvUrl);
                    if (response.IsSuccessStatusCode)
                    {
                        string csvContent = await response.Content.ReadAsStringAsync();
                        List<double> data = ParseGoogleSheetsCsv(csvContent);

                        if (data.Any())
                        {
                            if (data.Count > 10000)
                            {
                                var result = MessageBox.Show($"Таблица содержит {data.Count} элементов. Это может занять значительное время и память. Продолжить?",
                                                           "Предупреждение", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                                if (result == MessageBoxResult.No)
                                    return;
                            }

                            originalData = data;
                            UpdateDataGrid();
                            txtResults.Text = $"УСПЕШНО ЗАГРУЖЕНО ИЗ GOOGLE SHEETS:\n";
                            txtResults.Text += $"Ссылка: {url}\n";
                            txtResults.Text += $"Количество элементов: {data.Count}\n";
                            txtResults.Text += $"Диапазон данных: {data.Min():F3} - {data.Max():F3}\n";

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
                            MessageBox.Show("Не удалось найти числовые данные в таблице Google Sheets.\n\n" +
                                          "Убедитесь, что:\n" +
                                          "1. Таблица имеет публичный доступ\n" +
                                          "2. Данные находятся в первом столбце\n" +
                                          "3. Ячейки содержат числовые значения",
                                          "Информация",
                                          MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                    else
                    {
                        MessageBox.Show($"Не удалось загрузить данные. Проверьте:\n" +
                                      "1. Доступность интернета\n" +
                                      "2. Публичный доступ к таблице\n" +
                                      "3. Корректность ссылки\n\n" +
                                      $"HTTP код: {response.StatusCode}",
                                      "Ошибка загрузки",
                                      MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (HttpRequestException hrex)
            {
                MessageBox.Show($"Ошибка сети: {hrex.Message}\n\nПроверьте подключение к интернету и доступность таблицы.",
                              "Ошибка сети",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (TaskCanceledException)
            {
                MessageBox.Show("Превышено время ожидания при загрузке данных. Проверьте скорость интернета или размер таблицы.",
                              "Таймаут",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке из Google Sheets:\n{ex.Message}",
                              "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string ExtractSheetId(string url)
        {
            try
            {
                var patterns = new[]
                {
                    @"\/d\/([a-zA-Z0-9-_]+)", 
                    @"\/spreadsheets\/d\/([a-zA-Z0-9-_]+)", 
                    @"key=([a-zA-Z0-9-_]+)", 
                    @"id=([a-zA-Z0-9-_]+)" 
                };

                foreach (var pattern in patterns)
                {
                    var match = Regex.Match(url, pattern);
                    if (match.Success && match.Groups.Count > 1)
                    {
                        return match.Groups[1].Value;
                    }
                }

                var parts = url.Split('/');
                foreach (var part in parts)
                {
                    if (part.Length > 20 && !part.Contains("?") && !part.Contains("&"))
                    {
                        return part;
                    }
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        private List<double> ParseGoogleSheetsCsv(string csvContent)
        {
            List<double> data = new List<double>();

            try
            {
                var lines = csvContent.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    var values = ParseCsvLine(line);

                    foreach (var value in values)
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
                            data.Add(Math.Round(number, 3)); 
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при обработке данных CSV: {ex.Message}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return data;
        }

        private List<string> ParseCsvLine(string line)
        {
            var values = new List<string>();
            bool inQuotes = false;
            StringBuilder currentValue = new StringBuilder();

            for (int i = 0; i < line.Length; i++)
            {
                char currentChar = line[i];

                if (currentChar == '"')
                {
                    if (inQuotes && i < line.Length - 1 && line[i + 1] == '"')
                    {
                        currentValue.Append('"');
                        i++; 
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (currentChar == ',' && !inQuotes)
                {
                    values.Add(currentValue.ToString());
                    currentValue.Clear();
                }
                else
                {
                    currentValue.Append(currentChar);
                }
            }

            values.Add(currentValue.ToString());
            return values;
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

                if (count <= 0)
                {
                    MessageBox.Show("Количество элементов должно быть больше 0!", "Ошибка",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

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
                    double randomValue = min + (rand.NextDouble() * (max - min));
                    randomValue = Math.Round(randomValue, 3); 
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
                                data.Add(Math.Round(number, 3)); 
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
                        data.Add(Math.Round(number, 3));
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

            int maxBogoIterations = 0;
            if (!int.TryParse(txtMaxBogoIterations.Text, out maxBogoIterations) || maxBogoIterations < 0)
            {
                maxBogoIterations = 0; 
            }

            txtResults.Text += "ВЫПОЛНЕНИЕ СОРТИРОВКИ:\n\n";

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
                                Text = data[i].ToString("F2"), 
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

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Освобождаем ресурсы, если нужно
        }
    }
}