using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Microsoft.Win32;
using OfficeOpenXml;
using System.Net.Http;
using System.Text.RegularExpressions;
using OxyPlot;
using OxyPlot.Series;
using OxyPlot.Wpf;
using OxyPlot.Axes;

namespace WpfApp1
{
    public class PointData : INotifyPropertyChanged
    {
        private double _x;
        private double _y;

        public int Index { get; set; }

        public double X
        {
            get { return _x; }
            set
            {
                if (_x != value)
                {
                    _x = value;
                    OnPropertyChanged(nameof(X));
                }
            }
        }

        public double Y
        {
            get { return _y; }
            set
            {
                if (_y != value)
                {
                    _y = value;
                    OnPropertyChanged(nameof(Y));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public PointData(int index, double x = 0, double y = 0)
        {
            Index = index;
            X = x;
            Y = y;
        }
    }

    public partial class LeastSquaresWindow : Window
    {
        private ObservableCollection<PointData> pointsData = new ObservableCollection<PointData>();
        private ObservableCollection<PointData> historyPoints = new ObservableCollection<PointData>();
        private LeastSquaresMethod lsqMethod = new LeastSquaresMethod();

        private double generateMinX = -10;
        private double generateMaxX = 10;
        private double generateMinY = -50;
        private double generateMaxY = 50;

        private bool hasLinearResult = false;
        private bool hasQuadraticResult = false;
        private double[] linearCoefficients;
        private double[] quadraticCoefficients;

        private PlotModel plotModel;

        public LeastSquaresWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;

            OfficeOpenXml.ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
            InitializePlotModel();
        }

        private void InitializePlotModel()
        {
            plotModel = new PlotModel
            {
                Title = "Аппроксимация данных",
                TitleFontSize = 14,
                TitleFontWeight = OxyPlot.FontWeights.Bold
            };

            plotModel.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Bottom,
                Title = "X",
                Key = "X",
                PositionAtZeroCrossing = true,
                MajorGridlineStyle = LineStyle.Dot,
                MinorGridlineStyle = LineStyle.Dot
            });

            plotModel.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Left,
                Title = "Y",
                Key = "Y",
                PositionAtZeroCrossing = true,
                MajorGridlineStyle = LineStyle.Dot,
                MinorGridlineStyle = LineStyle.Dot
            });

            plotModel.PlotMargins = new OxyThickness(60, 40, 40, 60);
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            PointsDataGrid.ItemsSource = pointsData;
            HistoryDataGrid.ItemsSource = historyPoints;
            InitializePointsGrid();
            LoadSettings();
            GenerateSampleData();
            UpdatePlot();

            // Добавляем обработчик клавиш
            PointsDataGrid.PreviewKeyDown += PointsDataGrid_PreviewKeyDown;
        }

        private void PointsDataGrid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete && PointsDataGrid.SelectedItem != null)
            {
                RemoveSelectedPoint_Click(sender, e);
                e.Handled = true;
            }
        }

        private void InitializePointsGrid()
        {
            PointsDataGrid.Columns.Clear();
            HistoryDataGrid.Columns.Clear();

            PointsDataGrid.CanUserAddRows = false; 
            PointsDataGrid.CanUserDeleteRows = false; 

            PointsDataGrid.Columns.Add(new DataGridTextColumn()
            {
                Header = "№",
                Binding = new System.Windows.Data.Binding("Index") { Mode = System.Windows.Data.BindingMode.OneWay },
                Width = new DataGridLength(40, DataGridLengthUnitType.Pixel),
                IsReadOnly = true
            });

            PointsDataGrid.Columns.Add(new DataGridTextColumn()
            {
                Header = "X",
                Binding = new System.Windows.Data.Binding("X") { Mode = System.Windows.Data.BindingMode.TwoWay },
                Width = new DataGridLength(80, DataGridLengthUnitType.Pixel)
            });

            PointsDataGrid.Columns.Add(new DataGridTextColumn()
            {
                Header = "Y",
                Binding = new System.Windows.Data.Binding("Y") { Mode = System.Windows.Data.BindingMode.TwoWay },
                Width = new DataGridLength(80, DataGridLengthUnitType.Pixel)
            });

            HistoryDataGrid.Columns.Add(new DataGridTextColumn()
            {
                Header = "№",
                Binding = new System.Windows.Data.Binding("Index") { Mode = System.Windows.Data.BindingMode.OneWay },
                Width = new DataGridLength(40, DataGridLengthUnitType.Pixel),
                IsReadOnly = true
            });

            HistoryDataGrid.Columns.Add(new DataGridTextColumn()
            {
                Header = "X",
                Binding = new System.Windows.Data.Binding("X") { Mode = System.Windows.Data.BindingMode.OneWay },
                Width = new DataGridLength(80, DataGridLengthUnitType.Pixel),
                IsReadOnly = true
            });

            HistoryDataGrid.Columns.Add(new DataGridTextColumn()
            {
                Header = "Y",
                Binding = new System.Windows.Data.Binding("Y") { Mode = System.Windows.Data.BindingMode.OneWay },
                Width = new DataGridLength(80, DataGridLengthUnitType.Pixel),
                IsReadOnly = true
            });
        }

        private void PointsDataGrid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            if (e.PropertyName == "Index")
            {
                e.Column.IsReadOnly = true;
            }
        }

        private void LoadSettings()
        {
            try
            {
                string settingsFile = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "LSQ_Settings.txt");

                if (File.Exists(settingsFile))
                {
                    var lines = File.ReadAllLines(settingsFile);
                    if (lines.Length >= 4)
                    {
                        if (double.TryParse(lines[0], out double minX)) generateMinX = minX;
                        if (double.TryParse(lines[1], out double maxX)) generateMaxX = maxX;
                        if (double.TryParse(lines[2], out double minY)) generateMinY = minY;
                        if (double.TryParse(lines[3], out double maxY)) generateMaxY = maxY;
                    }
                }

                txtMinX.Text = generateMinX.ToString();
                txtMaxX.Text = generateMaxX.ToString();
                txtMinY.Text = generateMinY.ToString();
                txtMaxY.Text = generateMaxY.ToString();
            }
            catch { }
        }

        private void SaveSettings()
        {
            try
            {
                if (double.TryParse(txtMinX.Text, out double minX) &&
                    double.TryParse(txtMaxX.Text, out double maxX) &&
                    double.TryParse(txtMinY.Text, out double minY) &&
                    double.TryParse(txtMaxY.Text, out double maxY))
                {
                    generateMinX = minX;
                    generateMaxX = maxX;
                    generateMinY = minY;
                    generateMaxY = maxY;

                    string settingsPath = System.IO.Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "LSQ_Settings.txt");

                    File.WriteAllLines(settingsPath, new[] {
                        minX.ToString(),
                        maxX.ToString(),
                        minY.ToString(),
                        maxY.ToString()
                    });
                }
            }
            catch { }
        }

        private void SaveGenerationIntervals_Click(object sender, RoutedEventArgs e)
        {
            SaveSettings();
            ShowStatus("Интервалы генерации сохранены", false);
        }

        private void GenerateSampleData()
        {
            pointsData.Clear();
            historyPoints.Clear();

            var samplePoints = new[]
            {
                new PointData(1, -5, 25),
                new PointData(2, -3, 9),
                new PointData(3, -1, 1),
                new PointData(4, 0, 0),
                new PointData(5, 1, 1),
                new PointData(6, 3, 9),
                new PointData(7, 5, 25)
            };

            foreach (var point in samplePoints)
            {
                pointsData.Add(point);
                historyPoints.Add(new PointData(historyPoints.Count + 1, point.X, point.Y));
            }

            PointsDataGrid.Items.Refresh();
            HistoryDataGrid.Items.Refresh();
            UpdatePlot();
            ShowStatus("Загружены демонстрационные данные", false);

            if (pointsData.Count >= 2)
            {
                CalculateAllSelected_Click(null, null);
            }
        }

        private List<(double x, double y)> GetPointsFromData()
        {
            var points = new List<(double x, double y)>();
            foreach (var point in pointsData)
            {
                points.Add((point.X, point.Y));
            }
            return points;
        }

        private bool ValidatePoints()
        {
            if (pointsData.Count < 2)
            {
                ShowStatus("Ошибка: требуется минимум 2 точки для аппроксимации", true);
                return false;
            }

            if (chkQuadratic.IsChecked == true && pointsData.Count < 3)
            {
                ShowStatus("Ошибка: для квадратичной аппроксимации требуется минимум 3 точки", true);
                return false;
            }

            foreach (var point in pointsData)
            {
                if (double.IsNaN(point.X) || double.IsInfinity(point.X) ||
                    double.IsNaN(point.Y) || double.IsInfinity(point.Y))
                {
                    ShowStatus($"Ошибка: недопустимое значение в точке {point.Index}", true);
                    return false;
                }
            }

            return true;
        }

        private async void CalculateAllSelected_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidatePoints()) return;

            if (!IsAnyMethodSelected())
            {
                ShowStatus("Ошибка: выберите хотя бы один метод аппроксимации", true);
                return;
            }

            txtLinearResults.Clear();
            txtQuadraticResults.Clear();
            txtComparison.Clear();

            var points = GetPointsFromData();

            ShowStatus("Выполняются вычисления...", false);

            try
            {
                if (chkLinear.IsChecked == true)
                {
                    await CalculateLinearApproximation(points);
                }

                if (chkQuadratic.IsChecked == true)
                {
                    await CalculateQuadraticApproximation(points);
                }

                DisplayComparison();
                UpdatePlot();

                ShowStatus("Вычисления успешно завершены", false);
            }
            catch (Exception ex)
            {
                ShowStatus($"Ошибка вычислений: {ex.Message}", true);
            }
        }

        private async Task CalculateLinearApproximation(List<(double x, double y)> points)
        {
            await Task.Run(() =>
            {
                try
                {
                    var result = lsqMethod.LinearRegression(points);
                    hasLinearResult = true;
                    linearCoefficients = result.Coefficients;

                    Dispatcher.Invoke(() =>
                    {
                        StringBuilder sb = new StringBuilder();
                        sb.AppendLine($"y = {result.Coefficients[1]:F4}x + {result.Coefficients[0]:F4}");
                        sb.AppendLine($"R² = {result.R2:F6}");
                        sb.AppendLine($"Время: {result.Time.TotalMilliseconds:F3} мс");
                        sb.AppendLine();
                        sb.AppendLine("Качество:");
                        sb.AppendLine($"  R² > 0.9: отличное");
                        sb.AppendLine($"  R² > 0.7: хорошее");
                        sb.AppendLine($"  R² > 0.5: удовлетворительное");
                        sb.AppendLine($"  R² ≤ 0.5: плохое");

                        txtLinearResults.Text = sb.ToString();
                    });
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() =>
                    {
                        txtLinearResults.Text = $"Ошибка: {ex.Message}";
                        hasLinearResult = false;
                    });
                }
            });
        }

        private async Task CalculateQuadraticApproximation(List<(double x, double y)> points)
        {
            await Task.Run(() =>
            {
                try
                {
                    var result = lsqMethod.QuadraticRegression(points);
                    hasQuadraticResult = true;
                    quadraticCoefficients = result.Coefficients;

                    Dispatcher.Invoke(() =>
                    {
                        StringBuilder sb = new StringBuilder();
                        sb.AppendLine($"y = {result.Coefficients[2]:F4}x² + {result.Coefficients[1]:F4}x + {result.Coefficients[0]:F4}");
                        sb.AppendLine($"R² = {result.R2:F6}");
                        sb.AppendLine($"Время: {result.Time.TotalMilliseconds:F3} мс");
                        sb.AppendLine();
                        sb.AppendLine("Качество:");
                        sb.AppendLine($"  R² > 0.9: отличное");
                        sb.AppendLine($"  R² > 0.7: хорошее");
                        sb.AppendLine($"  R² > 0.5: удовлетворительное");
                        sb.AppendLine($"  R² ≤ 0.5: плохое");

                        txtQuadraticResults.Text = sb.ToString();
                    });
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() =>
                    {
                        txtQuadraticResults.Text = $"Ошибка: {ex.Message}";
                        hasQuadraticResult = false;
                    });
                }
            });
        }

        private void DisplayComparison()
        {
            if (!hasLinearResult && !hasQuadraticResult) return;

            StringBuilder sb = new StringBuilder();

            if (hasLinearResult && hasQuadraticResult)
            {
                sb.AppendLine("Рекомендация:");
                sb.AppendLine("Линейная модель проще,");
                sb.AppendLine("квадратичная точнее для");
                sb.AppendLine("нелинейных данных.");
            }
            else if (hasLinearResult)
            {
                sb.AppendLine("Доступна только");
                sb.AppendLine("линейная модель.");
            }
            else if (hasQuadraticResult)
            {
                sb.AppendLine("Доступна только");
                sb.AppendLine("квадратичная модель.");
            }

            txtComparison.Text = sb.ToString();
        }

        private void UpdatePlot()
        {
            if (plotModel == null) return;

            plotModel.Series.Clear();
            plotModel.Annotations.Clear();

            var points = GetPointsFromData();
            if (points.Count == 0) return;

            var scatterSeries = new ScatterSeries
            {
                Title = "Точки данных",
                MarkerType = MarkerType.Circle,
                MarkerSize = 5,
                MarkerFill = OxyColors.Blue,
                MarkerStroke = OxyColors.DarkBlue,
                MarkerStrokeThickness = 1
            };

            foreach (var point in points)
            {
                scatterSeries.Points.Add(new ScatterPoint(point.x, point.y));
            }
            plotModel.Series.Add(scatterSeries);

            if (hasLinearResult)
            {
                double minX = points.Min(p => p.x);
                double maxX = points.Max(p => p.x);
                double padding = (maxX - minX) * 0.2;

                var lineSeries = new LineSeries
                {
                    Title = $"Линейная: y = {linearCoefficients[1]:F2}x + {linearCoefficients[0]:F2}",
                    Color = OxyColors.Red,
                    StrokeThickness = 2
                };

                int steps = 100;
                for (int i = 0; i <= steps; i++)
                {
                    double x = minX - padding + (maxX - minX + 2 * padding) * i / steps;
                    double y = linearCoefficients[1] * x + linearCoefficients[0];
                    lineSeries.Points.Add(new DataPoint(x, y));
                }
                plotModel.Series.Add(lineSeries);
            }

            if (hasQuadraticResult)
            {
                double minX = points.Min(p => p.x);
                double maxX = points.Max(p => p.x);
                double padding = (maxX - minX) * 0.2;

                var quadSeries = new LineSeries
                {
                    Title = $"Квадратичная: y = {quadraticCoefficients[2]:F2}x² + {quadraticCoefficients[1]:F2}x + {quadraticCoefficients[0]:F2}",
                    Color = OxyColors.Green,
                    StrokeThickness = 2
                };

                int steps = 100;
                for (int i = 0; i <= steps; i++)
                {
                    double x = minX - padding + (maxX - minX + 2 * padding) * i / steps;
                    double y = quadraticCoefficients[2] * x * x + quadraticCoefficients[1] * x + quadraticCoefficients[0];
                    quadSeries.Points.Add(new DataPoint(x, y));
                }
                plotModel.Series.Add(quadSeries);
            }

            plotModel.InvalidatePlot(true);
            PlotView.Model = plotModel;
        }

        private void GenerateData_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!int.TryParse(txtPointCount.Text, out int pointCount) || pointCount < 2)
                {
                    ShowStatus("Ошибка: количество точек должно быть ≥ 2", true);
                    return;
                }

                if (!double.TryParse(txtMinX.Text, out double minX) ||
                    !double.TryParse(txtMaxX.Text, out double maxX) ||
                    !double.TryParse(txtMinY.Text, out double minY) ||
                    !double.TryParse(txtMaxY.Text, out double maxY))
                {
                    ShowStatus("Ошибка: недопустимые значения", true);
                    return;
                }

                if (minX >= maxX || minY >= maxY)
                {
                    ShowStatus("Ошибка: минимум должен быть меньше максимума", true);
                    return;
                }

                SaveSettings();

                Random random = new Random();
                pointsData.Clear();

                for (int i = 0; i < pointCount; i++)
                {
                    double x = minX + random.NextDouble() * (maxX - minX);
                    double y = minY + random.NextDouble() * (maxY - minY);
                    pointsData.Add(new PointData(i + 1, Math.Round(x, 2), Math.Round(y, 2)));

                    historyPoints.Add(new PointData(historyPoints.Count + 1, Math.Round(x, 2), Math.Round(y, 2)));
                }

                PointsDataGrid.Items.Refresh();
                HistoryDataGrid.Items.Refresh();
                ClearResults();
                UpdatePlot();
                ShowStatus($"Сгенерировано {pointCount} точек", false);
            }
            catch (Exception ex)
            {
                ShowStatus($"Ошибка: {ex.Message}", true);
            }
        }

        private void AddPoint_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int newIndex = pointsData.Count + 1;
                var newPoint = new PointData(newIndex, 0, 0);
                pointsData.Add(newPoint);

                historyPoints.Add(new PointData(historyPoints.Count + 1, 0, 0));

                PointsDataGrid.Items.Refresh();
                HistoryDataGrid.Items.Refresh();

                PointsDataGrid.SelectedItem = newPoint;
                PointsDataGrid.ScrollIntoView(newPoint);

                ShowStatus($"Добавлена новая точка", false);
            }
            catch (Exception ex)
            {
                ShowStatus($"Ошибка: {ex.Message}", true);
            }
        }

        private void RemoveSelectedPoint_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (PointsDataGrid.SelectedItem is PointData selectedPoint)
                {
                    pointsData.Remove(selectedPoint);

                    for (int i = 0; i < pointsData.Count; i++)
                    {
                        pointsData[i].Index = i + 1;
                    }

                    PointsDataGrid.Items.Refresh();
                    UpdatePlot();
                    ShowStatus($"Точка удалена", false);
                }
                else
                {
                    ShowStatus("Выберите точку для удаления", true);
                }
            }
            catch (Exception ex)
            {
                ShowStatus($"Ошибка: {ex.Message}", true);
            }
        }

        private void RemoveLastPoint_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (pointsData.Count > 0)
                {
                    var lastPoint = pointsData[pointsData.Count - 1];
                    pointsData.RemoveAt(pointsData.Count - 1);

                    for (int i = 0; i < pointsData.Count; i++)
                    {
                        pointsData[i].Index = i + 1;
                    }

                    PointsDataGrid.Items.Refresh();
                    UpdatePlot();
                    ShowStatus("Последняя точка удалена", false);
                }
                else
                {
                    ShowStatus("Нет точек для удаления", true);
                }
            }
            catch (Exception ex)
            {
                ShowStatus($"Ошибка: {ex.Message}", true);
            }
        }

        private void ClearData_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                pointsData.Clear();
                historyPoints.Clear();
                PointsDataGrid.Items.Refresh();
                HistoryDataGrid.Items.Refresh();
                ClearResults();
                UpdatePlot();
                ShowStatus("Данные очищены", false);
            }
            catch (Exception ex)
            {
                ShowStatus($"Ошибка: {ex.Message}", true);
            }
        }

        private void ClearResults()
        {
            txtLinearResults.Clear();
            txtQuadraticResults.Clear();
            txtComparison.Clear();
            plotModel?.Series.Clear();
            plotModel?.Annotations.Clear();
            hasLinearResult = false;
            hasQuadraticResult = false;
            StatusBorder.Visibility = Visibility.Collapsed;
            if (plotModel != null)
                plotModel.InvalidatePlot(true);
        }

        private void LinearApproximation_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidatePoints()) return;
            chkLinear.IsChecked = true;
            chkQuadratic.IsChecked = false;
            CalculateAllSelected_Click(sender, e);
        }

        private void QuadraticApproximation_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidatePoints()) return;
            chkLinear.IsChecked = false;
            chkQuadratic.IsChecked = true;
            CalculateAllSelected_Click(sender, e);
        }

        private void SelectAllMethods_Click(object sender, RoutedEventArgs e)
        {
            chkLinear.IsChecked = true;
            chkQuadratic.IsChecked = true;
        }

        private void DeselectAllMethods_Click(object sender, RoutedEventArgs e)
        {
            chkLinear.IsChecked = false;
            chkQuadratic.IsChecked = false;
        }

        private bool IsAnyMethodSelected()
        {
            return chkLinear.IsChecked == true || chkQuadratic.IsChecked == true;
        }

        private void ShowGraphs_Click(object sender, RoutedEventArgs e)
        {
            if (pointsData.Count > 0)
            {
                UpdatePlot();
                ShowStatus("График показан", false);
            }
        }

        private void HideGraphs_Click(object sender, RoutedEventArgs e)
        {
            plotModel?.Series.Clear();
            plotModel?.Annotations.Clear();
            if (plotModel != null)
                plotModel.InvalidatePlot(true);
            ShowStatus("График скрыт", false);
        }

        private void ShowStatus(string message, bool isError)
        {
            if (StatusBorder == null || StatusTextBlock == null) return;

            Dispatcher.Invoke(() =>
            {
                StatusBorder.Visibility = Visibility.Visible;
                StatusTextBlock.Text = message;
                StatusBorder.Background = isError ?
                    new SolidColorBrush(Color.FromRgb(255, 230, 230)) :
                    new SolidColorBrush(Color.FromRgb(230, 245, 230));
                StatusBorder.BorderBrush = isError ?
                    Brushes.Red :
                    Brushes.Green;
            });
        }

        private async void ImportFromExcel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OpenFileDialog openFileDialog = new OpenFileDialog
                {
                    Filter = "Excel (*.xlsx)|*.xlsx|CSV (*.csv)|*.csv",
                    Title = "Импорт из Excel"
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    await ImportDataFromExcel(openFileDialog.FileName);
                }
            }
            catch (Exception ex)
            {
                ShowStatus($"Ошибка: {ex.Message}", true);
            }
        }

        private async Task ImportDataFromExcel(string filePath)
        {
            try
            {
                ShowStatus("Импорт...", false);

                await Task.Run(() =>
                {
                    using (var package = new ExcelPackage(new FileInfo(filePath)))
                    {
                        var worksheet = package.Workbook.Worksheets[0];
                        var start = worksheet.Dimension.Start;
                        var end = worksheet.Dimension.End;

                        List<(double x, double y)> importedPoints = new List<(double x, double y)>();

                        for (int row = start.Row; row <= end.Row; row++)
                        {
                            var cellX = worksheet.Cells[row, 1].Value;
                            var cellY = worksheet.Cells[row, 2].Value;

                            if (cellX != null && cellY != null)
                            {
                                if (double.TryParse(cellX.ToString().Replace(',', '.'),
                                    NumberStyles.Any, CultureInfo.InvariantCulture, out double x) &&
                                    double.TryParse(cellY.ToString().Replace(',', '.'),
                                    NumberStyles.Any, CultureInfo.InvariantCulture, out double y))
                                {
                                    importedPoints.Add((x, y));
                                }
                            }
                        }

                        Dispatcher.Invoke(() =>
                        {
                            pointsData.Clear();
                            historyPoints.Clear();

                            for (int i = 0; i < importedPoints.Count; i++)
                            {
                                pointsData.Add(new PointData(i + 1, importedPoints[i].x, importedPoints[i].y));
                                historyPoints.Add(new PointData(i + 1, importedPoints[i].x, importedPoints[i].y));
                            }

                            PointsDataGrid.Items.Refresh();
                            HistoryDataGrid.Items.Refresh();

                            ShowStatus($"Импортировано {importedPoints.Count} точек", false);
                            UpdatePlot();

                            if (importedPoints.Count >= 2)
                            {
                                CalculateAllSelected_Click(null, null);
                            }
                        });
                    }
                });
            }
            catch (Exception ex)
            {
                ShowStatus($"Ошибка: {ex.Message}", true);
            }
        }

        private async void ImportFromGoogleTables_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await ShowGoogleSheetsDialog();
            }
            catch (Exception ex)
            {
                ShowStatus($"Ошибка: {ex.Message}", true);
            }
        }

        private async Task ShowGoogleSheetsDialog()
        {
            try
            {
                var dialog = new GoogleSheetsDialogLeast();
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
                ShowStatus("Загрузка данных из Google Sheets...", false);

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
                        List<(double x, double y)> data = ParseGoogleSheetsCsvForLSQ(csvContent);

                        if (data.Any())
                        {
                            Dispatcher.Invoke(() =>
                            {
                                pointsData.Clear();
                                historyPoints.Clear();

                                for (int i = 0; i < data.Count; i++)
                                {
                                    pointsData.Add(new PointData(i + 1, data[i].x, data[i].y));
                                    historyPoints.Add(new PointData(i + 1, data[i].x, data[i].y));
                                }

                                PointsDataGrid.Items.Refresh();
                                HistoryDataGrid.Items.Refresh();
                                UpdatePlot();

                                ShowStatus($"Загружено {data.Count} точек из Google Sheets", false);

                                if (data.Count >= 2)
                                {
                                    CalculateAllSelected_Click(null, null);
                                }
                            });
                        }
                        else
                        {
                            MessageBox.Show("Не удалось найти числовые данные в таблице Google Sheets.\n\n" +
                                          "Убедитесь, что:\n" +
                                          "1. Таблица имеет публичный доступ\n" +
                                          "2. Данные находятся в первых двух столбцах (X и Y)\n" +
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

        private List<(double x, double y)> ParseGoogleSheetsCsvForLSQ(string csvContent)
        {
            List<(double x, double y)> data = new List<(double x, double y)>();

            try
            {
                var lines = csvContent.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    var values = ParseCsvLine(line);

                    if (values.Count >= 2)
                    {
                        string cleanedValueX = values[0].Trim()
                            .Replace("\"", "")
                            .Replace("'", "")
                            .Replace("(", "")
                            .Replace(")", "")
                            .Replace("[", "")
                            .Replace("]", "")
                            .Replace("{", "")
                            .Replace("}", "");

                        string cleanedValueY = values[1].Trim()
                            .Replace("\"", "")
                            .Replace("'", "")
                            .Replace("(", "")
                            .Replace(")", "")
                            .Replace("[", "")
                            .Replace("]", "")
                            .Replace("{", "")
                            .Replace("}", "");

                        if (double.TryParse(cleanedValueX,
                            System.Globalization.NumberStyles.Any,
                            System.Globalization.CultureInfo.InvariantCulture,
                            out double x) &&
                            double.TryParse(cleanedValueY,
                            System.Globalization.NumberStyles.Any,
                            System.Globalization.CultureInfo.InvariantCulture,
                            out double y))
                        {
                            data.Add((Math.Round(x, 3), Math.Round(y, 3)));
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

        private void ExportToExcel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    Filter = "Excel (*.xlsx)|*.xlsx",
                    Title = "Экспорт",
                    DefaultExt = ".xlsx",
                    FileName = "данные_мнк.xlsx"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    ExportToExcelFile(saveFileDialog.FileName);
                }
            }
            catch (Exception ex)
            {
                ShowStatus($"Ошибка: {ex.Message}", true);
            }
        }

        private void ExportToExcelFile(string filePath)
        {
            try
            {
                using (var package = new ExcelPackage())
                {
                    var worksheet = package.Workbook.Worksheets.Add("Данные");

                    worksheet.Cells[1, 1].Value = "X";
                    worksheet.Cells[1, 2].Value = "Y";
                    worksheet.Cells[1, 1].Style.Font.Bold = true;
                    worksheet.Cells[1, 2].Style.Font.Bold = true;

                    for (int i = 0; i < pointsData.Count; i++)
                    {
                        worksheet.Cells[i + 2, 1].Value = pointsData[i].X;
                        worksheet.Cells[i + 2, 2].Value = pointsData[i].Y;
                    }

                    int row = pointsData.Count + 4;

                    if (hasLinearResult)
                    {
                        worksheet.Cells[row, 1].Value = "Линейная аппроксимация:";
                        worksheet.Cells[row, 2].Value = $"y = {linearCoefficients[1]:F4}x + {linearCoefficients[0]:F4}";
                        row++;
                    }

                    if (hasQuadraticResult)
                    {
                        worksheet.Cells[row, 1].Value = "Квадратичная аппроксимация:";
                        worksheet.Cells[row, 2].Value = $"y = {quadraticCoefficients[2]:F4}x² + {quadraticCoefficients[1]:F4}x + {quadraticCoefficients[0]:F4}";
                    }

                    worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
                    package.SaveAs(new FileInfo(filePath));

                    ShowStatus("Данные экспортированы", false);
                }
            }
            catch (Exception ex)
            {
                ShowStatus($"Ошибка: {ex.Message}", true);
            }
        }

        private void CreateTemplate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    Filter = "Excel (*.xlsx)|*.xlsx",
                    Title = "Создать шаблон",
                    FileName = "шаблон.xlsx",
                    DefaultExt = ".xlsx"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    CreateExcelTemplate(saveFileDialog.FileName);
                }
            }
            catch (Exception ex)
            {
                ShowStatus($"Ошибка: {ex.Message}", true);
            }
        }

        private void CreateExcelTemplate(string filePath)
        {
            try
            {
                using (var package = new ExcelPackage())
                {
                    var worksheet = package.Workbook.Worksheets.Add("Шаблон");

                    worksheet.Cells[1, 1].Value = "ШАБЛОН ДЛЯ МНК";
                    worksheet.Cells[1, 1].Style.Font.Bold = true;
                    worksheet.Cells[1, 1].Style.Font.Size = 14;

                    worksheet.Cells[3, 1].Value = "Заполните столбцы X и Y:";
                    worksheet.Cells[3, 1].Style.Font.Bold = true;

                    worksheet.Cells[5, 1].Value = "X";
                    worksheet.Cells[5, 2].Value = "Y";
                    worksheet.Cells[5, 1].Style.Font.Bold = true;
                    worksheet.Cells[5, 2].Style.Font.Bold = true;

                    for (int i = 0; i < 5; i++)
                    {
                        worksheet.Cells[6 + i, 1].Value = i + 1;
                        worksheet.Cells[6 + i, 2].Value = (i + 1) * 2;
                    }

                    worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
                    package.SaveAs(new FileInfo(filePath));

                    ShowStatus("Шаблон создан", false);
                }
            }
            catch (Exception ex)
            {
                ShowStatus($"Ошибка: {ex.Message}", true);
            }
        }

        private void HelpImport_Click(object sender, RoutedEventArgs e)
        {
            string helpText = @"ИНСТРУКЦИЯ:

1. ВВОД ДАННЫХ:
   - Вводите точки в таблицу
   - Или используйте 'Сгенерировать данные'
   - История точек сохраняется

2. ИМПОРТ:
   - Из Excel (.xlsx) - 2 столбца X и Y
   - Из Google Sheets - публичная ссылка

3. УПРАВЛЕНИЕ ТОЧКАМИ:
   - Добавить точку - добавляет новую строку
   - Удалить выбранную - удаляет выделенную точку
   - Удалить последнюю - удаляет последнюю точку
   - Также можно удалять клавишей Delete

4. ВЫЧИСЛЕНИЯ:
   - Выберите метод(ы) аппроксимации
   - Нажмите 'ВЫПОЛНИТЬ РАСЧЁТ'

5. ГРАФИК:
   - Масштабирование колесом мыши
   - Панорамирование перетаскиванием
   - Подписи точек при наведении";

            MessageBox.Show(helpText, "Помощь", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            SaveSettings();
            this.Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            SaveSettings();
        }
    }
}