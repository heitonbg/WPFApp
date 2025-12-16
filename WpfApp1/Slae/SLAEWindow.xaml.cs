using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using System.IO;
using System.Diagnostics;
using System.Windows.Threading;
using System.Collections;
using System.Net.Http;
using System.Text;
using System.Globalization;
using System.Threading;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace WpfApp1
{
    // Класс для представления строки матрицы
    public class MatrixRow : ObservableCollection<double>, INotifyPropertyChanged
    {
        private int _rowIndex;

        public int RowIndex
        {
            get { return _rowIndex; }
            set
            {
                _rowIndex = value;
                OnPropertyChanged(nameof(RowIndex));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public MatrixRow(int size) : base()
        {
            for (int i = 0; i < size; i++)
            {
                this.Add(0.0);
            }
        }
    }

    // Класс для представления вектора B
    public class VectorBItem : INotifyPropertyChanged
    {
        private double _value;

        public int Index { get; set; }

        public double Value
        {
            get { return _value; }
            set
            {
                if (_value != value)
                {
                    _value = value;
                    OnPropertyChanged(nameof(Value));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public VectorBItem(int index, double value = 0)
        {
            Index = index;
            Value = value;
        }
    }

    // Класс для представления решения X
    public class SolutionItem : INotifyPropertyChanged
    {
        private string _variable;
        private double _value;

        public string Variable
        {
            get { return _variable; }
            set
            {
                if (_variable != value)
                {
                    _variable = value;
                    OnPropertyChanged(nameof(Variable));
                }
            }
        }

        public double Value
        {
            get { return _value; }
            set
            {
                if (_value != value)
                {
                    _value = value;
                    OnPropertyChanged(nameof(Value));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public SolutionItem(string variable, double value)
        {
            Variable = variable;
            Value = value;
        }
    }

    public partial class SLAEWindow : Window
    {
        private int matrixSize = 2;
        private const int MAX_MATRIX_SIZE = 50;
        private ObservableCollection<MatrixRow> matrixAData = new ObservableCollection<MatrixRow>();
        private ObservableCollection<VectorBItem> vectorBData = new ObservableCollection<VectorBItem>();
        private ObservableCollection<SolutionItem> solutionData = new ObservableCollection<SolutionItem>();
        private HttpClient httpClient = new HttpClient();

        // Enum для определения формата данных
        private enum DataFormatType
        {
            Unknown,
            MatrixOnly,
            MatrixWithVector,
            LabeledSections
        }

        // Класс для хранения результатов анализа данных
        private class DataAnalysisResult
        {
            public DataFormatType DataFormat { get; set; }
            public int MatrixSize { get; set; }
        }

        public SLAEWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Устанавливаем контекст данных для DataGrid
            MatrixADataGrid.ItemsSource = matrixAData;
            VectorBDataGrid.ItemsSource = vectorBData;
            VectorXDataGrid.ItemsSource = solutionData;

            InitializeDataGrids();
        }

        private void InitializeDataGrids()
        {
            CreateMatrixA();
            CreateVectorB();
            CreateVectorX();
        }

        private void CreateMatrixA()
        {
            try
            {
                matrixAData.Clear();

                // Создаем столбцы для DataGrid
                MatrixADataGrid.Columns.Clear();

                for (int i = 0; i < matrixSize; i++)
                {
                    var column = new DataGridTextColumn()
                    {
                        Header = $"x{i + 1}",
                        Binding = new System.Windows.Data.Binding($"[{i}]") { Mode = System.Windows.Data.BindingMode.TwoWay },
                        Width = new DataGridLength(60, DataGridLengthUnitType.Pixel)
                    };
                    MatrixADataGrid.Columns.Add(column);
                }

                // Создаем строки матрицы
                for (int i = 0; i < matrixSize; i++)
                {
                    var row = new MatrixRow(matrixSize);
                    row.RowIndex = i + 1;
                    matrixAData.Add(row);
                }

                MatrixADataGrid.UpdateLayout();
            }
            catch (Exception ex)
            {
                ShowStatus($"Ошибка при создании матрицы: {ex.Message}", true);
            }
        }

        private void CreateVectorB()
        {
            try
            {
                vectorBData.Clear();

                VectorBDataGrid.Columns.Clear();

                // Добавляем номер строки
                VectorBDataGrid.Columns.Add(new DataGridTextColumn()
                {
                    Header = "№",
                    Binding = new System.Windows.Data.Binding("Index") { Mode = System.Windows.Data.BindingMode.OneWay },
                    Width = new DataGridLength(40, DataGridLengthUnitType.Pixel),
                    IsReadOnly = true
                });

                // Добавляем значение
                VectorBDataGrid.Columns.Add(new DataGridTextColumn()
                {
                    Header = "Значение",
                    Binding = new System.Windows.Data.Binding("Value") { Mode = System.Windows.Data.BindingMode.TwoWay },
                    Width = new DataGridLength(100, DataGridLengthUnitType.Pixel)
                });

                // Создаем элементы вектора B
                for (int i = 0; i < matrixSize; i++)
                {
                    vectorBData.Add(new VectorBItem(i + 1, 0));
                }
            }
            catch (Exception ex)
            {
                ShowStatus($"Ошибка при создании вектора B: {ex.Message}", true);
            }
        }

        private void CreateVectorX()
        {
            try
            {
                solutionData.Clear();

                VectorXDataGrid.Columns.Clear();

                VectorXDataGrid.Columns.Add(new DataGridTextColumn()
                {
                    Header = "Переменная",
                    Binding = new System.Windows.Data.Binding("Variable") { Mode = System.Windows.Data.BindingMode.OneWay },
                    Width = new DataGridLength(80, DataGridLengthUnitType.Pixel),
                    IsReadOnly = true
                });

                VectorXDataGrid.Columns.Add(new DataGridTextColumn()
                {
                    Header = "Значение",
                    Binding = new System.Windows.Data.Binding("Value") { Mode = System.Windows.Data.BindingMode.OneWay },
                    Width = new DataGridLength(100, DataGridLengthUnitType.Pixel),
                    IsReadOnly = true
                });
            }
            catch (Exception ex)
            {
                ShowStatus($"Ошибка при создании вектора X: {ex.Message}", true);
            }
        }

        private void MatrixSizeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MatrixSizeComboBox.SelectedItem is ComboBoxItem item)
            {
                int newSize = int.Parse(item.Content.ToString());

                if (newSize != matrixSize)
                {
                    matrixSize = newSize;

                    if (matrixSize > 15)
                    {
                        ShowStatus($"Создание матрицы {matrixSize}x{matrixSize}...", false);
                    }

                    InitializeDataGrids();

                    if (matrixSize > 15)
                    {
                        ShowStatus($"Матрица {matrixSize}x{matrixSize} создана", false);
                    }
                }
            }
        }

        private void DataGrid_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            e.Row.Header = (e.Row.GetIndex() + 1).ToString();
        }

        private double[,] GetMatrixA()
        {
            var matrix = new double[matrixSize, matrixSize];

            for (int i = 0; i < matrixSize && i < matrixAData.Count; i++)
            {
                var row = matrixAData[i];
                for (int j = 0; j < matrixSize && j < row.Count; j++)
                {
                    matrix[i, j] = row[j];
                }
            }
            return matrix;
        }

        private double[] GetVectorB()
        {
            var vector = new double[matrixSize];

            for (int i = 0; i < matrixSize && i < vectorBData.Count; i++)
            {
                vector[i] = vectorBData[i].Value;
            }
            return vector;
        }

        private bool ValidateInputs()
        {
            // Проверка матрицы A
            for (int i = 0; i < matrixSize && i < matrixAData.Count; i++)
            {
                var row = matrixAData[i];
                for (int j = 0; j < matrixSize && j < row.Count; j++)
                {
                    if (double.IsNaN(row[j]) || double.IsInfinity(row[j]))
                    {
                        ShowStatus($"Ошибка: Некорректное значение в матрице A[{i + 1},{j + 1}]", true);
                        return false;
                    }
                }
            }

            // Проверка вектора B
            for (int i = 0; i < matrixSize && i < vectorBData.Count; i++)
            {
                if (double.IsNaN(vectorBData[i].Value) || double.IsInfinity(vectorBData[i].Value))
                {
                    ShowStatus($"Ошибка: Некорректное значение в векторе B[{i + 1}]", true);
                    return false;
                }
            }

            return true;
        }

        private async void ImportFromExcel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OpenFileDialog openFileDialog = new OpenFileDialog
                {
                    Filter = "CSV files (*.csv)|*.csv|Excel files (*.xlsx)|*.xlsx|Text files (*.txt)|*.txt|All files (*.*)|*.*",
                    Title = "Импорт данных из Excel"
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    string fileName = Path.GetFileName(openFileDialog.FileName);
                    string extension = Path.GetExtension(openFileDialog.FileName).ToLower();

                    ShowStatus($"Импорт данных из {fileName}...", false);

                    if (extension == ".csv" || extension == ".txt")
                    {
                        await Task.Run(() => ImportDataFromCSV(openFileDialog.FileName));
                    }
                    else if (extension == ".xlsx")
                    {
                        MessageBox.Show("Для импорта из .xlsx файлов необходимо сначала экспортировать данные в CSV формат или использовать специализированные библиотеки.",
                            "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                ShowStatus($"Ошибка при импорте из Excel: {ex.Message}", true);
            }
        }

        private async void ImportFromGoogleTables_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Показываем диалог для ввода URL Google Таблицы
                GoogleSheetsDialogWindow dialogWindow = new GoogleSheetsDialogWindow();
                dialogWindow.Owner = this;

                if (dialogWindow.ShowDialog() == true && !string.IsNullOrEmpty(dialogWindow.SheetsUrl))
                {
                    ShowStatus("Импорт данных из Google Таблиц...", false);
                    await ImportFromGoogleSheetsAsync(dialogWindow.SheetsUrl);
                }
            }
            catch (Exception ex)
            {
                ShowStatus($"Ошибка при импорте из Google Tables: {ex.Message}", true);
            }
        }

        private async Task ImportFromGoogleSheetsAsync(string url)
        {
            try
            {
                ShowStatus("Подключение к Google Таблицам...", false);

                // Конвертируем URL Google Таблицы в CSV URL
                string csvUrl = ConvertGoogleSheetsUrlToCsv(url);

                // Загружаем данные с таймаутом
                using (var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(30)))
                {
                    var response = await httpClient.GetAsync(csvUrl, timeoutCts.Token);
                    response.EnsureSuccessStatusCode();

                    string csvContent = await response.Content.ReadAsStringAsync();

                    if (string.IsNullOrWhiteSpace(csvContent))
                    {
                        ShowStatus("Таблица пустая", true);
                        return;
                    }

                    ShowStatus("Анализ данных...", false);

                    // Анализируем и импортируем данные
                    await AnalyzeAndImportGoogleSheetsData(csvContent);
                }
            }
            catch (OperationCanceledException)
            {
                ShowStatus("Таймаут при загрузке данных", true);
            }
            catch (HttpRequestException ex)
            {
                ShowStatus($"Ошибка сети: {ex.Message}", true);
            }
            catch (Exception ex)
            {
                ShowStatus($"Ошибка: {ex.Message}", true);
            }
        }

        private async Task AnalyzeAndImportGoogleSheetsData(string csvContent)
        {
            await Dispatcher.InvokeAsync(() =>
            {
                try
                {
                    // Разбиваем на строки и очищаем
                    var lines = csvContent.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(line => line.Trim())
                        .Where(line => !string.IsNullOrWhiteSpace(line))
                        .ToArray();

                    if (lines.Length == 0)
                    {
                        ShowStatus("Нет данных для импорта", true);
                        return;
                    }

                    // Анализируем структуру данных
                    var analysisResult = AnalyzeDataStructure(lines);

                    switch (analysisResult.DataFormat)
                    {
                        case DataFormatType.MatrixWithVector:
                            ImportMatrixWithVector(lines, analysisResult.MatrixSize);
                            break;

                        case DataFormatType.MatrixOnly:
                            ImportMatrixOnly(lines, analysisResult.MatrixSize);
                            break;

                        case DataFormatType.LabeledSections:
                            ImportLabeledSections(lines);
                            break;

                        case DataFormatType.Unknown:
                            ShowStatus("Не удалось распознать формат данных", true);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    ShowStatus($"Ошибка при анализе данных: {ex.Message}", true);
                }
            });
        }

        private DataAnalysisResult AnalyzeDataStructure(string[] lines)
        {
            // Проверяем, есть ли метки
            bool hasMatrixLabel = lines.Any(l => l.ToLower().Contains("матрица") || l.ToLower().Contains("matrix"));
            bool hasVectorLabel = lines.Any(l => l.ToLower().Contains("вектор") || l.ToLower().Contains("vector"));

            if (hasMatrixLabel || hasVectorLabel)
            {
                return new DataAnalysisResult
                {
                    DataFormat = DataFormatType.LabeledSections,
                    MatrixSize = DetectSizeFromLabeledSections(lines)
                };
            }

            // Анализируем первую строку для определения формата
            var firstLineParts = lines[0].Split(',');
            int cols = firstLineParts.Length;
            int rows = lines.Length;

            // Проверяем, является ли последний столбец вектором B
            bool lastColumnIsVector = DetectIfLastColumnIsVector(lines);

            if (lastColumnIsVector && rows == cols - 1)
            {
                return new DataAnalysisResult
                {
                    DataFormat = DataFormatType.MatrixWithVector,
                    MatrixSize = rows
                };
            }

            if (rows == cols)
            {
                return new DataAnalysisResult
                {
                    DataFormat = DataFormatType.MatrixOnly,
                    MatrixSize = rows
                };
            }

            return new DataAnalysisResult
            {
                DataFormat = DataFormatType.Unknown,
                MatrixSize = Math.Min(rows, cols)
            };
        }

        private bool DetectIfLastColumnIsVector(string[] lines)
        {
            try
            {
                int sampleRows = Math.Min(10, lines.Length);

                for (int i = 0; i < sampleRows; i++)
                {
                    var parts = lines[i].Split(',');
                    if (parts.Length < 2) return false;

                    // Проверяем, что все строки имеют одинаковое количество столбцов
                    if (i > 0)
                    {
                        var prevParts = lines[i - 1].Split(',');
                        if (parts.Length != prevParts.Length) return false;
                    }

                    // Пытаемся распарсить все значения
                    for (int j = 0; j < parts.Length; j++)
                    {
                        string value = parts[j].Trim();
                        if (!double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out _))
                        {
                            // Если не число, проверяем специальные случаи
                            if (!string.IsNullOrWhiteSpace(value) &&
                                !value.Equals("-") &&
                                !value.Equals(".") &&
                                !value.Equals(","))
                            {
                                return false;
                            }
                        }
                    }
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        private int DetectSizeFromLabeledSections(string[] lines)
        {
            try
            {
                bool inMatrixSection = false;
                int matrixRows = 0;

                foreach (var line in lines)
                {
                    string lowerLine = line.ToLower();

                    if (lowerLine.Contains("матрица") || lowerLine.Contains("matrix"))
                    {
                        inMatrixSection = true;
                        continue;
                    }

                    if (lowerLine.Contains("вектор") || lowerLine.Contains("vector"))
                    {
                        break;
                    }

                    if (inMatrixSection)
                    {
                        // Проверяем, является ли строка данными матрицы
                        var parts = line.Split(',');
                        bool isDataRow = parts.All(p =>
                            double.TryParse(p.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out _));

                        if (isDataRow)
                        {
                            matrixRows++;
                        }
                    }
                }

                return matrixRows;
            }
            catch
            {
                return 0;
            }
        }

        private void ImportMatrixWithVector(string[] lines, int size)
        {
            if (size <= 0 || size > MAX_MATRIX_SIZE)
            {
                ShowStatus($"Некорректный размер матрицы: {size}", true);
                return;
            }

            // Обновляем размер
            matrixSize = size;
            UpdateMatrixSizeComboBox();
            InitializeDataGrids();

            // Импортируем данные
            for (int i = 0; i < size && i < lines.Length; i++)
            {
                var parts = lines[i].Split(',');

                if (i < matrixAData.Count)
                {
                    var row = matrixAData[i];
                    for (int j = 0; j < size && j < parts.Length - 1; j++)
                    {
                        ParseAndSetValue(parts[j], out double value);
                        row[j] = value;
                    }
                }

                if (i < vectorBData.Count && parts.Length > size)
                {
                    ParseAndSetValue(parts[parts.Length - 1], out double value);
                    vectorBData[i].Value = value;
                }
            }

            MatrixADataGrid.Items.Refresh();
            VectorBDataGrid.Items.Refresh();
            ShowStatus($"Импортировано: матрица {size}×{size} с вектором B", false);
        }

        private void ImportMatrixOnly(string[] lines, int size)
        {
            if (size <= 0 || size > MAX_MATRIX_SIZE)
            {
                ShowStatus($"Некорректный размер матрицы: {size}", true);
                return;
            }

            matrixSize = size;
            UpdateMatrixSizeComboBox();
            InitializeDataGrids();

            for (int i = 0; i < size && i < lines.Length; i++)
            {
                var parts = lines[i].Split(',');

                if (i < matrixAData.Count)
                {
                    var row = matrixAData[i];
                    for (int j = 0; j < size && j < parts.Length; j++)
                    {
                        ParseAndSetValue(parts[j], out double value);
                        row[j] = value;
                    }
                }
            }

            MatrixADataGrid.Items.Refresh();
            VectorBDataGrid.Items.Refresh();
            ShowStatus($"Импортирована матрица {size}×{size}", false);
        }

        private void ImportLabeledSections(string[] lines)
        {
            try
            {
                bool inMatrixSection = false;
                bool inVectorSection = false;
                int matrixRowIndex = 0;
                int vectorRowIndex = 0;
                List<double[]> tempMatrixData = new List<double[]>();
                List<double[]> tempVectorData = new List<double[]>();

                foreach (var line in lines)
                {
                    string lowerLine = line.ToLower();

                    if (lowerLine.Contains("матрица") || lowerLine.Contains("matrix"))
                    {
                        inMatrixSection = true;
                        inVectorSection = false;
                        continue;
                    }

                    if (lowerLine.Contains("вектор") || lowerLine.Contains("vector"))
                    {
                        inMatrixSection = false;
                        inVectorSection = true;
                        continue;
                    }

                    if (inMatrixSection)
                    {
                        var parts = line.Split(',');
                        var row = new double[parts.Length];

                        for (int j = 0; j < parts.Length; j++)
                        {
                            ParseAndSetValue(parts[j], out row[j]);
                        }

                        tempMatrixData.Add(row);
                        matrixRowIndex++;
                    }
                    else if (inVectorSection)
                    {
                        var parts = line.Split(',');
                        foreach (var part in parts)
                        {
                            if (double.TryParse(part.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double value))
                            {
                                tempVectorData.Add(new double[] { value });
                                vectorRowIndex++;
                            }
                        }
                    }
                }

                // Определяем размер матрицы
                int detectedSize = tempMatrixData.Count;

                if (detectedSize > 0 && detectedSize <= MAX_MATRIX_SIZE)
                {
                    matrixSize = detectedSize;
                    UpdateMatrixSizeComboBox();
                    InitializeDataGrids();

                    // Заполняем матрицу A
                    for (int i = 0; i < detectedSize && i < tempMatrixData.Count; i++)
                    {
                        if (i < matrixAData.Count)
                        {
                            var targetRow = matrixAData[i];
                            var sourceRow = tempMatrixData[i];

                            for (int j = 0; j < detectedSize && j < sourceRow.Length; j++)
                            {
                                targetRow[j] = sourceRow[j];
                            }
                        }
                    }

                    // Заполняем вектор B если есть данные
                    if (tempVectorData.Count > 0)
                    {
                        for (int i = 0; i < detectedSize && i < tempVectorData.Count; i++)
                        {
                            if (i < vectorBData.Count)
                            {
                                vectorBData[i].Value = tempVectorData[i][0];
                            }
                        }
                    }

                    MatrixADataGrid.Items.Refresh();
                    VectorBDataGrid.Items.Refresh();

                    string status = tempVectorData.Count > 0
                        ? $"Импортировано: матрица {detectedSize}×{detectedSize} с вектором B"
                        : $"Импортирована матрица {detectedSize}×{detectedSize}";

                    ShowStatus(status, false);
                }
                else
                {
                    ShowStatus($"Не удалось определить размер матрицы", true);
                }
            }
            catch (Exception ex)
            {
                ShowStatus($"Ошибка при импорте секций: {ex.Message}", true);
            }
        }

        private bool ParseAndSetValue(string input, out double result)
        {
            if (double.TryParse(input.Trim().Replace(',', '.'),
                NumberStyles.Any, CultureInfo.InvariantCulture, out result))
            {
                return true;
            }
            result = 0;
            return false;
        }

        private string ConvertGoogleSheetsUrlToCsv(string url)
        {
            try
            {
                // Нормализуем URL
                if (!url.StartsWith("http"))
                {
                    url = "https://" + url;
                }

                Uri uri = new Uri(url);

                // Извлекаем ID таблицы
                var segments = uri.Segments;
                string spreadsheetId = "";

                for (int i = 0; i < segments.Length; i++)
                {
                    if (segments[i].Equals("d/", StringComparison.OrdinalIgnoreCase) &&
                        i + 1 < segments.Length)
                    {
                        spreadsheetId = segments[i + 1].TrimEnd('/');
                        break;
                    }
                }

                if (string.IsNullOrEmpty(spreadsheetId))
                {
                    // Альтернативный способ извлечения ID
                    var match = System.Text.RegularExpressions.Regex.Match(
                        url, @"/spreadsheets/d/([a-zA-Z0-9-_]+)");
                    if (match.Success)
                    {
                        spreadsheetId = match.Groups[1].Value;
                    }
                }

                if (string.IsNullOrEmpty(spreadsheetId))
                {
                    throw new ArgumentException("Не удалось извлечь ID таблицы из URL");
                }

                // Извлекаем GID (ID листа)
                string gid = "0";
                if (uri.Fragment.Contains("gid="))
                {
                    gid = uri.Fragment.Split('=')[1];
                }
                else
                {
                    // Пробуем извлечь из query параметров
                    var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
                    if (query["gid"] != null)
                    {
                        gid = query["gid"];
                    }
                }

                // Формируем CSV URL
                return $"https://docs.google.com/spreadsheets/d/{spreadsheetId}/export?format=csv&gid={gid}";
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Ошибка конвертации URL: {ex.Message}");
            }
        }

        private void CreateTemplate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    Filter = "CSV files (*.csv)|*.csv",
                    Title = "Создать шаблон CSV",
                    FileName = "шаблон_матрицы.csv"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    using (var writer = new StreamWriter(saveFileDialog.FileName, false, System.Text.Encoding.UTF8))
                    {
                        writer.WriteLine("Матрица A");
                        writer.WriteLine("1,0,0");
                        writer.WriteLine("0,1,0");
                        writer.WriteLine("0,0,1");
                        writer.WriteLine("");
                        writer.WriteLine("Вектор B");
                        writer.WriteLine("1");
                        writer.WriteLine("2");
                        writer.WriteLine("3");
                        writer.WriteLine("");
                        writer.WriteLine("// Инструкция:");
                        writer.WriteLine("// 1. Замените числа в матрице A и векторе B своими значениями");
                        writer.WriteLine("// 2. Сохраните файл");
                        writer.WriteLine("// 3. Импортируйте через меню 'Файл → Импорт данных'");
                    }

                    ShowStatus($"Шаблон создан: {saveFileDialog.FileName}", false);

                    var result = MessageBox.Show(
                        "Шаблон CSV файла создан. Хотите открыть его для редактирования?",
                        "Шаблон создан",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = saveFileDialog.FileName,
                            UseShellExecute = true
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                ShowStatus($"Ошибка при создании шаблона: {ex.Message}", true);
            }
        }

        private void ImportDataFromCSV(string filePath)
        {
            try
            {
                var lines = File.ReadAllLines(filePath);
                if (lines.Length < 2)
                {
                    Dispatcher.Invoke(() => ShowStatus("Ошибка: файл слишком короткий", true));
                    return;
                }

                int detectedSize = DetectMatrixSizeFromCSV(lines);

                if (detectedSize >= 2 && detectedSize <= MAX_MATRIX_SIZE)
                {
                    Dispatcher.Invoke(() =>
                    {
                        int oldSize = matrixSize;
                        matrixSize = detectedSize;

                        if (oldSize != matrixSize)
                        {
                            UpdateMatrixSizeComboBox();
                            InitializeDataGrids();
                        }

                        ShowStatus($"Импорт матрицы {matrixSize}x{matrixSize}...", false);
                    });

                    Dispatcher.Invoke(() =>
                    {
                        try
                        {
                            ImportMatrixAFromCSV(lines);
                            ImportVectorBFromCSV(lines);

                            bool hasDataA = matrixAData.Any(row => row.Any(val => val != 0));
                            bool hasDataB = vectorBData.Any(item => item.Value != 0);

                            if (hasDataA || hasDataB)
                            {
                                ShowStatus($"Данные успешно импортированы. Размер: {matrixSize}x{matrixSize}", false);
                            }
                            else
                            {
                                ShowStatus("Предупреждение: импортированы нулевые значения. Проверьте формат файла.", true);
                            }
                        }
                        catch (Exception ex)
                        {
                            ShowStatus($"Ошибка при импорте данных: {ex.Message}", true);
                        }
                    });
                }
                else if (detectedSize > MAX_MATRIX_SIZE)
                {
                    Dispatcher.Invoke(() =>
                        ShowStatus($"Ошибка: размер матрицы {detectedSize} превышает максимальный ({MAX_MATRIX_SIZE})", true));
                }
                else
                {
                    Dispatcher.Invoke(() =>
                        ShowStatus("Ошибка: не удалось определить матрицу в файле", true));
                }
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                    ShowStatus($"Ошибка при чтении файла: {ex.Message}", true));
            }
        }

        private void ImportMatrixAFromCSV(string[] lines)
        {
            bool inMatrixSection = false;
            int rowIndex = 0;

            foreach (var line in lines)
            {
                if (line.ToLower().Contains("матрица a") || line.ToLower().Contains("matrix a"))
                {
                    inMatrixSection = true;
                    continue;
                }

                if (inMatrixSection)
                {
                    if (string.IsNullOrEmpty(line.Trim()) || line.Trim().StartsWith("//"))
                    {
                        inMatrixSection = false;
                        continue;
                    }

                    if (rowIndex < matrixSize)
                    {
                        var values = line.Split(',');

                        if (rowIndex < matrixAData.Count)
                        {
                            var row = matrixAData[rowIndex];
                            for (int j = 0; j < matrixSize && j < values.Length; j++)
                            {
                                string valueStr = values[j].Trim().Replace(',', '.');
                                if (double.TryParse(valueStr,
                                    NumberStyles.Any,
                                    CultureInfo.InvariantCulture,
                                    out double value))
                                {
                                    row[j] = value;
                                }
                                else
                                {
                                    row[j] = 0;
                                }
                            }
                        }
                        rowIndex++;
                    }
                }
            }
            MatrixADataGrid.Items.Refresh();
        }

        private void ImportVectorBFromCSV(string[] lines)
        {
            bool inVectorSection = false;
            int rowIndex = 0;

            foreach (var line in lines)
            {
                if (line.ToLower().Contains("вектор b") || line.ToLower().Contains("vector b"))
                {
                    inVectorSection = true;
                    continue;
                }

                if (inVectorSection)
                {
                    if (string.IsNullOrEmpty(line.Trim()) || line.Trim().StartsWith("//"))
                    {
                        inVectorSection = false;
                        continue;
                    }

                    if (rowIndex < matrixSize)
                    {
                        if (rowIndex < vectorBData.Count)
                        {
                            string valueStr = line.Trim().Replace(',', '.');
                            if (double.TryParse(valueStr,
                                NumberStyles.Any,
                                CultureInfo.InvariantCulture,
                                out double value))
                            {
                                vectorBData[rowIndex].Value = value;
                            }
                            else
                            {
                                vectorBData[rowIndex].Value = 0;
                            }
                        }
                        rowIndex++;
                    }
                }
            }

            if (rowIndex == 0)
            {
                int vectorStart = FindMatrixAEnd(lines);
                if (vectorStart == -1) vectorStart = matrixSize;

                for (int i = 0; i < matrixSize && (vectorStart + i) < lines.Length; i++)
                {
                    var line = lines[vectorStart + i];
                    if (string.IsNullOrEmpty(line.Trim()) || line.Trim().StartsWith("//"))
                        continue;

                    if (i < vectorBData.Count)
                    {
                        string valueStr = line.Trim().Replace(',', '.');
                        if (double.TryParse(valueStr,
                            NumberStyles.Any,
                            CultureInfo.InvariantCulture,
                            out double value))
                        {
                            vectorBData[i].Value = value;
                        }
                        else
                        {
                            vectorBData[i].Value = 0;
                        }
                    }
                }
            }
            VectorBDataGrid.Items.Refresh();
        }

        private int FindMatrixAEnd(string[] lines)
        {
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].ToLower().Contains("матрица a") || lines[i].ToLower().Contains("matrix a"))
                {
                    for (int j = i + 1; j < lines.Length; j++)
                    {
                        if (string.IsNullOrEmpty(lines[j].Trim()) || lines[j].Trim().StartsWith("//"))
                        {
                            return j + 1;
                        }
                    }
                    return i + matrixSize + 1;
                }
            }
            return -1;
        }

        private int DetectMatrixSizeFromCSV(string[] lines)
        {
            try
            {
                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].ToLower().Contains("матрица a") || lines[i].ToLower().Contains("matrix a"))
                    {
                        int matrixStart = i + 1;
                        int size = 0;

                        for (int j = matrixStart; j < lines.Length; j++)
                        {
                            var line = lines[j].Trim();

                            if (string.IsNullOrEmpty(line) || line.StartsWith("//"))
                                break;

                            var values = line.Split(',');

                            bool isMatrixRow = true;
                            int numbersCount = 0;

                            foreach (var value in values)
                            {
                                string cleanValue = value.Trim().Replace(',', '.');
                                if (double.TryParse(cleanValue,
                                    NumberStyles.Any,
                                    CultureInfo.InvariantCulture,
                                    out _))
                                {
                                    numbersCount++;
                                }
                                else
                                {
                                    isMatrixRow = false;
                                    break;
                                }
                            }

                            if (isMatrixRow && numbersCount >= 2)
                            {
                                size++;
                            }
                            else
                            {
                                break;
                            }

                            if (size >= MAX_MATRIX_SIZE)
                                break;
                        }
                        return size > 0 ? size : 2;
                    }
                }

                int dataRows = 0;
                int maxColumns = 0;

                for (int i = 0; i < lines.Length; i++)
                {
                    var line = lines[i].Trim();
                    if (string.IsNullOrEmpty(line) || line.StartsWith("//"))
                        continue;

                    var values = line.Split(',');
                    int validNumbers = 0;

                    foreach (var value in values)
                    {
                        string cleanValue = value.Trim().Replace(',', '.');
                        if (double.TryParse(cleanValue,
                            NumberStyles.Any,
                            CultureInfo.InvariantCulture,
                            out _))
                        {
                            validNumbers++;
                        }
                    }

                    if (validNumbers >= 2)
                    {
                        dataRows++;
                        maxColumns = Math.Max(maxColumns, validNumbers);
                    }
                    else
                    {
                        break;
                    }

                    if (dataRows >= MAX_MATRIX_SIZE)
                        break;
                }

                int detectedSize = Math.Min(dataRows, maxColumns);
                return detectedSize > 0 ? detectedSize : 2;
            }
            catch
            {
                return 2;
            }
        }

        private void UpdateMatrixSizeComboBox()
        {
            foreach (ComboBoxItem item in MatrixSizeComboBox.Items)
            {
                if (item.Content.ToString() == matrixSize.ToString())
                {
                    item.IsSelected = true;
                    break;
                }
            }
        }

        private async void GaussMethod_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInputs()) return;

            ShowStatus("Вычисление методом Гаусса...", false);
            var stopwatch = Stopwatch.StartNew();

            try
            {
                var A = GetMatrixA();
                var B = GetVectorB();

                var result = await Task.Run(() => SolveByGauss(A, B));
                stopwatch.Stop();

                DisplaySolution(result, stopwatch.Elapsed);
                ShowStatus("Решение найдено методом Гаусса", false);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                ShowStatus($"Ошибка: {ex.Message}", true);
            }
        }

        private async void JordanGaussMethod_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInputs()) return;

            ShowStatus("Вычисление методом Жордана-Гаусса...", false);
            var stopwatch = Stopwatch.StartNew();

            try
            {
                var A = GetMatrixA();
                var B = GetVectorB();

                var result = await Task.Run(() => SolveByJordanGauss(A, B));
                stopwatch.Stop();

                DisplaySolution(result, stopwatch.Elapsed);
                ShowStatus("Решение найдено методом Жордана-Гаусса", false);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                ShowStatus($"Ошибка: {ex.Message}", true);
            }
        }

        private async void CramerMethod_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInputs()) return;

            if (matrixSize > 10)
            {
                var result = MessageBox.Show(
                    $"Метод Крамера очень медленный для матриц размером {matrixSize}x{matrixSize}. " +
                    "Выполнение может занять длительное время.\n\n" +
                    "Продолжить?",
                    "Предупреждение",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result != MessageBoxResult.Yes)
                    return;
            }

            ShowStatus("Вычисление методом Крамера...", false);
            var stopwatch = Stopwatch.StartNew();

            try
            {
                var A = GetMatrixA();
                var B = GetVectorB();

                var result = await Task.Run(() => SolveByCramer(A, B));
                stopwatch.Stop();

                DisplaySolution(result, stopwatch.Elapsed);
                ShowStatus("Решение найдено методом Крамера", false);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                ShowStatus($"Ошибка: {ex.Message}", true);
            }
        }

        private double[] SolveByGauss(double[,] A, double[] B)
        {
            int n = B.Length;
            double[] x = new double[n];
            double[,] matrix = new double[n, n + 1];

            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    matrix[i, j] = A[i, j];
                }
                matrix[i, n] = B[i];
            }

            for (int k = 0; k < n; k++)
            {
                int maxRow = k;
                double maxVal = Math.Abs(matrix[k, k]);
                for (int i = k + 1; i < n; i++)
                {
                    if (Math.Abs(matrix[i, k]) > maxVal)
                    {
                        maxVal = Math.Abs(matrix[i, k]);
                        maxRow = i;
                    }
                }

                if (maxRow != k)
                {
                    for (int j = k; j < n + 1; j++)
                    {
                        (matrix[k, j], matrix[maxRow, j]) = (matrix[maxRow, j], matrix[k, j]);
                    }
                }

                for (int i = k + 1; i < n; i++)
                {
                    double factor = matrix[i, k] / matrix[k, k];
                    for (int j = k; j < n + 1; j++)
                    {
                        matrix[i, j] -= factor * matrix[k, j];
                    }
                }
            }

            for (int i = n - 1; i >= 0; i--)
            {
                x[i] = matrix[i, n];
                for (int j = i + 1; j < n; j++)
                {
                    x[i] -= matrix[i, j] * x[j];
                }
                x[i] /= matrix[i, i];
            }

            return x;
        }

        private double[] SolveByJordanGauss(double[,] A, double[] B)
        {
            int n = B.Length;
            double[,] matrix = new double[n, n + 1];

            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    matrix[i, j] = A[i, j];
                }
                matrix[i, n] = B[i];
            }

            for (int k = 0; k < n; k++)
            {
                double divisor = matrix[k, k];
                for (int j = k; j < n + 1; j++)
                {
                    matrix[k, j] /= divisor;
                }

                for (int i = 0; i < n; i++)
                {
                    if (i != k)
                    {
                        double factor = matrix[i, k];
                        for (int j = k; j < n + 1; j++)
                        {
                            matrix[i, j] -= factor * matrix[k, j];
                        }
                    }
                }
            }

            double[] x = new double[n];
            for (int i = 0; i < n; i++)
            {
                x[i] = matrix[i, n];
            }

            return x;
        }

        private double[] SolveByCramer(double[,] A, double[] B)
        {
            int n = B.Length;

            double[] x = new double[n];
            double mainDet = Determinant(A);

            if (Math.Abs(mainDet) < 1e-12)
                throw new Exception("Определитель матрицы A равен нулю. Метод Крамера не применим.");

            for (int i = 0; i < n; i++)
            {
                double[,] tempMatrix = (double[,])A.Clone();
                for (int j = 0; j < n; j++)
                {
                    tempMatrix[j, i] = B[j];
                }
                x[i] = Determinant(tempMatrix) / mainDet;
            }

            return x;
        }

        private double Determinant(double[,] matrix)
        {
            int n = (int)Math.Sqrt(matrix.Length);
            if (n == 1) return matrix[0, 0];
            if (n == 2) return matrix[0, 0] * matrix[1, 1] - matrix[0, 1] * matrix[1, 0];

            double det = 0;
            for (int j = 0; j < n; j++)
            {
                det += (j % 2 == 0 ? 1 : -1) * matrix[0, j] * Determinant(GetMinor(matrix, 0, j));
            }
            return det;
        }

        private double[,] GetMinor(double[,] matrix, int row, int col)
        {
            int n = (int)Math.Sqrt(matrix.Length);
            double[,] minor = new double[n - 1, n - 1];

            for (int i = 0, mi = 0; i < n; i++)
            {
                if (i == row) continue;
                for (int j = 0, mj = 0; j < n; j++)
                {
                    if (j == col) continue;
                    minor[mi, mj] = matrix[i, j];
                    mj++;
                }
                mi++;
            }
            return minor;
        }

        private void DisplaySolution(double[] solution, TimeSpan time)
        {
            if (VectorXDataGrid == null) return;

            solutionData.Clear();

            if (solution.Length > 20)
            {
                VectorXDataGrid.Height = 300;
            }
            else if (solution.Length > 10)
            {
                VectorXDataGrid.Height = 200;
            }
            else
            {
                VectorXDataGrid.Height = 120;
            }

            for (int i = 0; i < solution.Length; i++)
            {
                solutionData.Add(new SolutionItem($"x{i + 1}", Math.Round(solution[i], 6)));
            }

            if (ExecutionTimeTextBox != null)
                ExecutionTimeTextBox.Text = $"{time.TotalMilliseconds:F4} мс";
        }

        private void ShowStatus(string message, bool isError)
        {
            if (StatusBorder == null || StatusTextBlock == null) return;

            Dispatcher.Invoke(() =>
            {
                StatusBorder.Visibility = Visibility.Visible;
                StatusTextBlock.Text = message;
                StatusBorder.Background = isError ?
                    new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 230, 230)) :
                    new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(230, 245, 230));
                StatusBorder.BorderBrush = isError ?
                    System.Windows.Media.Brushes.Red :
                    System.Windows.Media.Brushes.Green;
            });
        }

        private void GenerateData_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var random = new Random();

                for (int i = 0; i < matrixAData.Count; i++)
                {
                    var row = matrixAData[i];
                    for (int j = 0; j < row.Count; j++)
                    {
                        row[j] = Math.Round(random.NextDouble() * 20 - 10, 2);
                    }
                }
                MatrixADataGrid.Items.Refresh();

                for (int i = 0; i < vectorBData.Count; i++)
                {
                    vectorBData[i].Value = Math.Round(random.NextDouble() * 20 - 10, 2);
                }
                VectorBDataGrid.Items.Refresh();

                ShowStatus("Данные сгенерированы случайным образом", false);
            }
            catch (Exception ex)
            {
                ShowStatus($"Ошибка при генерации данных: {ex.Message}", true);
            }
        }

        private void ClearData_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                for (int i = 0; i < matrixAData.Count; i++)
                {
                    var row = matrixAData[i];
                    for (int j = 0; j < row.Count; j++)
                    {
                        row[j] = 0;
                    }
                }
                MatrixADataGrid.Items.Refresh();

                for (int i = 0; i < vectorBData.Count; i++)
                {
                    vectorBData[i].Value = 0;
                }
                VectorBDataGrid.Items.Refresh();

                solutionData.Clear();
                ExecutionTimeTextBox.Text = "";
                StatusBorder.Visibility = Visibility.Collapsed;

                ShowStatus("Данные очищены", false);
            }
            catch (Exception ex)
            {
                ShowStatus($"Ошибка при очистке данных: {ex.Message}", true);
            }
        }

        private void ExportToExcel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    Filter = "CSV files (*.csv)|*.csv|Text files (*.txt)|*.txt",
                    Title = "Экспорт данных"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    using (var writer = new StreamWriter(saveFileDialog.FileName, false, Encoding.UTF8))
                    {
                        var invariantCulture = CultureInfo.InvariantCulture;

                        writer.WriteLine("Матрица A");
                        for (int i = 0; i < matrixAData.Count; i++)
                        {
                            var row = matrixAData[i];
                            var formattedRow = row.Select(val => val.ToString("0.######", invariantCulture));
                            writer.WriteLine(string.Join(",", formattedRow));
                        }

                        writer.WriteLine();

                        writer.WriteLine("Вектор B");
                        for (int i = 0; i < vectorBData.Count; i++)
                        {
                            var item = vectorBData[i];
                            writer.WriteLine(item.Value.ToString("0.######", invariantCulture));
                        }

                        writer.WriteLine();

                        if (solutionData.Count > 0)
                        {
                            writer.WriteLine("Вектор X");
                            foreach (var item in solutionData)
                            {
                                string value = item.Value.ToString("0.######", invariantCulture);
                                writer.WriteLine($"{item.Variable},{value}");
                            }
                        }
                    }

                    ShowStatus($"Данные экспортированы в {saveFileDialog.FileName}", false);
                }
            }
            catch (Exception ex)
            {
                ShowStatus($"Ошибка при экспорте: {ex.Message}", true);
            }
        }

        private void HelpImport_Click(object sender, RoutedEventArgs e)
        {
            string helpText = @"ИНСТРУКЦИЯ ПО ИМПОРТУ ДАННЫХ

1. ФОРМАТ CSV ФАЙЛА:
   - Матрица A и вектор B должны быть разделены пустой строкой
   - Можно использовать заголовки 'Матрица A' и 'Вектор B'
   - Разделитель - запятая (,)
   - Десятичный разделитель - точка (.)

2. ФОРМАТ GOOGLE ТАБЛИЦ:
   - Матрица A: первые N столбцов, N строк
   - Вектор B: последний столбец или отдельный блок
   - Поддерживаются форматы:
     * Матрица и вектор в одном блоке
     * Отдельные секции с метками
     * Только матрица

3. ПРИМЕР ФАЙЛА:
   Матрица A
   1,2,3
   4,5,6
   7,8,9

   Вектор B
   10
   11
   12

4. ИМПОРТ ИЗ GOOGLE ТАБЛИЦ:
   - Таблица должна быть публично доступной
   - Используйте меню 'Файл → Импорт данных → Из Google Tables'
   - Вставьте ссылку на таблицу

5. СОЗДАНИЕ ШАБЛОНА:
   - Используйте меню 'Файл → Импорт данных → Создать шаблон CSV'
   - Отредактируйте созданный файл
   - Импортируйте через меню 'Файл → Импорт данных'";

            MessageBox.Show(helpText, "Инструкция по импорту", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            string aboutText = @"ПРОГРАММА ДЛЯ РЕШЕНИЯ СЛАУ

Версия: 1.0
Разработчик: Студент КемГУ

ФУНКЦИОНАЛЬНОСТЬ:
- Решение систем линейных алгебраических уравнений
- Три метода решения: Гаусса, Жордана-Гаусса, Крамера
- Импорт данных из CSV файлов и Google Таблиц
- Экспорт результатов
- Генерация случайных данных

ТРЕБОВАНИЯ К СИСТЕМЕ:
- .NET Framework 4.7.2 или выше
- 100 МБ свободного места на диске
- Интернет-соединение для импорта из Google Таблиц

Лабораторная работа №6
Кемеровский государственный университет
2024 год";

            MessageBox.Show(aboutText, "О программе", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}