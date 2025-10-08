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

namespace WpfApp1
{
    public partial class SLAEWindow : Window
    {
        private int matrixSize = 2;
        private const int MAX_MATRIX_SIZE = 50;
        private List<double[]> matrixAData = new List<double[]>();
        private List<double[]> vectorBData = new List<double[]>();

        public SLAEWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
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
            if (MatrixADataGrid == null) return;

            MatrixADataGrid.Columns.Clear();
            MatrixADataGrid.Items.Clear();
            matrixAData.Clear();

            try
            {
                for (int i = 0; i < matrixSize; i++)
                {
                    var column = new DataGridTextColumn()
                    {
                        Header = $"x{i + 1}",
                        Binding = new System.Windows.Data.Binding($"[{i}]"),
                        Width = new DataGridLength(60, DataGridLengthUnitType.Pixel)
                    };
                    MatrixADataGrid.Columns.Add(column);
                }

                for (int i = 0; i < matrixSize; i++)
                {
                    var row = new double[matrixSize];
                    for (int j = 0; j < matrixSize; j++)
                    {
                        row[j] = 0;
                    }
                    matrixAData.Add(row);
                    MatrixADataGrid.Items.Add(row);
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
            if (VectorBDataGrid == null) return;

            VectorBDataGrid.Columns.Clear();
            VectorBDataGrid.Items.Clear();
            vectorBData.Clear();

            VectorBDataGrid.Columns.Add(new DataGridTextColumn()
            {
                Header = "Значение",
                Binding = new System.Windows.Data.Binding("[0]"),
                Width = new DataGridLength(100, DataGridLengthUnitType.Pixel)
            });

            for (int i = 0; i < matrixSize; i++)
            {
                var row = new double[1] { 0 };
                vectorBData.Add(row);
                VectorBDataGrid.Items.Add(row);
            }
        }

        private void CreateVectorX()
        {
            if (VectorXDataGrid == null) return;

            VectorXDataGrid.Columns.Clear();
            VectorXDataGrid.Items.Clear();

            VectorXDataGrid.Columns.Add(new DataGridTextColumn()
            {
                Header = "Переменная",
                Binding = new System.Windows.Data.Binding("Variable"),
                Width = new DataGridLength(80, DataGridLengthUnitType.Pixel)
            });

            VectorXDataGrid.Columns.Add(new DataGridTextColumn()
            {
                Header = "Значение",
                Binding = new System.Windows.Data.Binding("Value"),
                Width = new DataGridLength(100, DataGridLengthUnitType.Pixel)
            });
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
                for (int j = 0; j < matrixSize && j < row.Length; j++)
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
                var row = vectorBData[i];
                vector[i] = row[0];
            }
            return vector;
        }

        private bool ValidateInputs()
        {
            for (int i = 0; i < matrixSize && i < matrixAData.Count; i++)
            {
                var row = matrixAData[i];
                for (int j = 0; j < matrixSize && j < row.Length; j++)
                {
                    if (!double.TryParse(row[j].ToString(), out _))
                    {
                        ShowStatus($"Ошибка: Некорректное значение в матрице A[{i + 1},{j + 1}]", true);
                        return false;
                    }
                }
            }

            for (int i = 0; i < matrixSize && i < vectorBData.Count; i++)
            {
                var row = vectorBData[i];
                if (!double.TryParse(row[0].ToString(), out _))
                {
                    ShowStatus($"Ошибка: Некорректное значение в векторе B[{i + 1}]", true);
                    return false;
                }
            }

            return true;
        }

        private void ImportFromExcel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OpenFileDialog openFileDialog = new OpenFileDialog
                {
                    Filter = "CSV files (*.csv)|*.csv|Text files (*.txt)|*.txt|All files (*.*)|*.*",
                    Title = "Импорт данных из Excel - выберите CSV файл"
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    string fileName = Path.GetFileName(openFileDialog.FileName);
                    ShowStatus($"Импорт данных из {fileName}...", false);

                    Task.Run(() => ImportDataFromCSV(openFileDialog.FileName));
                }
            }
            catch (Exception ex)
            {
                ShowStatus($"Ошибка при импорте из Excel: {ex.Message}", true);
            }
        }

        private void ImportFromGoogleTables_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OpenFileDialog openFileDialog = new OpenFileDialog
                {
                    Filter = "CSV files (*.csv)|*.csv|Text files (*.txt)|*.txt|All files (*.*)|*.*",
                    Title = "Импорт данных из Google Tables - выберите CSV файл"
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    string fileName = Path.GetFileName(openFileDialog.FileName);
                    ShowStatus($"Импорт данных из {fileName}...", false);

                    Task.Run(() => ImportDataFromCSV(openFileDialog.FileName));
                }
            }
            catch (Exception ex)
            {
                ShowStatus($"Ошибка при импорте из Google Tables: {ex.Message}", true);
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
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
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
                            bool hasDataB = vectorBData.Any(row => row[0] != 0);

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
                                    System.Globalization.NumberStyles.Any,
                                    System.Globalization.CultureInfo.InvariantCulture,
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
                            if (double.TryParse(line.Trim(), out double value))
                            {
                                vectorBData[rowIndex][0] = value;
                            }
                            else
                            {
                                vectorBData[rowIndex][0] = 0;
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
                        if (double.TryParse(line.Trim(), out double value))
                        {
                            vectorBData[i][0] = value;
                        }
                        else
                        {
                            vectorBData[i][0] = 0;
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
                                    System.Globalization.NumberStyles.Any,
                                    System.Globalization.CultureInfo.InvariantCulture,
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
                            System.Globalization.NumberStyles.Any,
                            System.Globalization.CultureInfo.InvariantCulture,
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

        private bool ValidateCSVFormat(string[] lines)
        {
            try
            {
                bool hasMatrixA = false;
                bool hasVectorB = false;

                foreach (var line in lines)
                {
                    if (line.ToLower().Contains("матрица a") || line.ToLower().Contains("matrix a"))
                        hasMatrixA = true;

                    if (line.ToLower().Contains("вектор b") || line.ToLower().Contains("vector b"))
                        hasVectorB = true;
                }

                if (!hasMatrixA && !hasVectorB)
                {
                    if (lines.Length >= 2)
                    {
                        var firstLine = lines[0].Split(',');
                        if (firstLine.Length >= 2 && double.TryParse(firstLine[0].Trim(), out _))
                        {
                            return true;
                        }
                    }
                    return false;
                }

                return hasMatrixA || hasVectorB;
            }
            catch
            {
                return false;
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

            VectorXDataGrid.Items.Clear();

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
                VectorXDataGrid.Items.Add(new
                {
                    Variable = $"x{i + 1}",
                    Value = Math.Round(solution[i], 6)
                });
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
                    for (int j = 0; j < row.Length; j++)
                    {
                        row[j] = Math.Round(random.NextDouble() * 20 - 10, 2);
                    }
                }
                MatrixADataGrid.Items.Refresh();

                for (int i = 0; i < vectorBData.Count; i++)
                {
                    vectorBData[i][0] = Math.Round(random.NextDouble() * 20 - 10, 2);
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
                    for (int j = 0; j < row.Length; j++)
                    {
                        row[j] = 0;
                    }
                }
                MatrixADataGrid.Items.Refresh();

                for (int i = 0; i < vectorBData.Count; i++)
                {
                    vectorBData[i][0] = 0;
                }
                VectorBDataGrid.Items.Refresh();

                VectorXDataGrid?.Items.Clear();
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
                    using (var writer = new StreamWriter(saveFileDialog.FileName, false, System.Text.Encoding.UTF8))
                    {
                        var invariantCulture = System.Globalization.CultureInfo.InvariantCulture;

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
                            var row = vectorBData[i];
                            writer.WriteLine(row[0].ToString());
                        }

                        writer.WriteLine();

                        if (VectorXDataGrid != null && VectorXDataGrid.Items.Count > 0)
                        {
                            writer.WriteLine("Вектор X");
                            foreach (var item in VectorXDataGrid.Items)
                            {
                                dynamic solution = item;
                                string value = solution.Value.ToString("0.######", invariantCulture);
                                writer.WriteLine($"{solution.Variable},{value}");
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
            var helpWindow = new HelpWindow();
            helpWindow.Owner = this;
            helpWindow.ShowDialog();
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}