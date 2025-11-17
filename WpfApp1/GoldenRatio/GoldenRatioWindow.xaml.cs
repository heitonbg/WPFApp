using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using OxyPlot;
using OxyPlot.Series;
using OxyPlot.Axes;

namespace WpfApp1
{
    public partial class GoldenRatioWindow : Window
    {
        public PlotModel PlotModel { get; set; }
        private List<DataPoint> _functionPoints;
        private List<DataPoint> _extremumPoints;

        private MainWindow _mainWindow;
        private bool _findMinimum = true;

        public GoldenRatioWindow()
        {
            InitializeComponent();
            PlotModel = new PlotModel
            {
                Title = "График функции",
                TitleColor = OxyColor.FromRgb(0x2C, 0x5F, 0x9E),
                TextColor = OxyColor.FromRgb(0x2C, 0x5F, 0x9E),
                PlotAreaBorderColor = OxyColor.FromRgb(0x2C, 0x5F, 0x9E)
            };
            _functionPoints = new List<DataPoint>();
            _extremumPoints = new List<DataPoint>();

            // Настройка осей
            PlotModel.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Bottom,
                Title = "x",
                TitleColor = OxyColor.FromRgb(0x2C, 0x5F, 0x9E),
                TextColor = OxyColor.FromRgb(0x2C, 0x5F, 0x9E),
                AxislineColor = OxyColor.FromRgb(0x2C, 0x5F, 0x9E),
                TicklineColor = OxyColor.FromRgb(0x2C, 0x5F, 0x9E)
            });
            PlotModel.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Left,
                Title = "f(x)",
                TitleColor = OxyColor.FromRgb(0x2C, 0x5F, 0x9E),
                TextColor = OxyColor.FromRgb(0x2C, 0x5F, 0x9E),
                AxislineColor = OxyColor.FromRgb(0x2C, 0x5F, 0x9E),
                TicklineColor = OxyColor.FromRgb(0x2C, 0x5F, 0x9E)
            });

            DataContext = this;
            this.Closing += Window_Closing;
        }

        public GoldenRatioWindow(MainWindow mainWindow) : this()
        {
            _mainWindow = mainWindow;
        }

        private void Calculate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!ValidateInput())
                {
                    return;
                }

                double a = double.Parse(txtA.Text.Replace(",", "."), CultureInfo.InvariantCulture);
                double b = double.Parse(txtB.Text.Replace(",", "."), CultureInfo.InvariantCulture);
                double epsilon = double.Parse(txtEpsilon.Text.Replace(",", "."), CultureInfo.InvariantCulture);
                string function = txtFunction.Text;
                _findMinimum = cmbExtremumType.SelectedIndex == 0;

                function = PreprocessFunction(function);

                if (function.ToLower().Contains("sin") || function.ToLower().Contains("cos"))
                {
                    MessageBox.Show("При больших интервалах на графике может быть несколько экстремумов, выведется только один экстремум, если вам необходим определенный экстремум попробуйте уменьшить интервал",
                        "Попробуйте уменьшить интервал", MessageBoxButton.OK, MessageBoxImage.Warning);
                }

                if (function.ToLower().Contains("tan"))
                {
                    MessageBox.Show("У тангенса происходит разрыв графика, график будет изображен с учетом разрывов",
                        "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                }

                if (function.ToLower().Contains("/x"))
                {
                    MessageBox.Show("Происходит разрыв графика при */x, график отображается с учетом разрывов",
                        "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                }

                if (function.ToLower().Contains("log") || function.ToLower().Contains("log10"))
                {
                    if (a <= 0)
                    {
                        MessageBox.Show("Внимание: логарифм не определен для x ≤ 0.\n" +
                                      "Автоматически корректирую начало интервала на 0.001",
                                      "Корректировка интервала", MessageBoxButton.OK, MessageBoxImage.Warning);
                        a = 0.001;
                        txtA.Text = "0.001";
                    }
                }

                if (function.Contains("^"))
                {
                    MessageBox.Show("Пожалуйста, используйте функцию pow(x,y) вместо оператора ^.\n\nПример: x^2 -> pow(x,2)",
                                  "Неподдерживаемый оператор",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Warning);
                    return;
                }
                else if (function.Contains("**"))
                {
                    MessageBox.Show("Пожалуйста, используйте функцию pow(x,y) вместо оператора **. \n\nПример: x**2 -> pow(x,2)",
                        "Неподдерживаемый оператор",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                GoldenRatioMethod method = new GoldenRatioMethod(function);

                if (method.IsConstantFunction(a, b))
                {
                    MessageBox.Show("Функция является постоянной на заданном интервале!",
                                  "Постоянная функция",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Information);
                }

                GoldenRatioResult result = _findMinimum ?
                    method.FindMinimum(a, b, epsilon) :
                    method.FindMaximum(a, b, epsilon);

                string extremumType = _findMinimum ? "минимума" : "максимума";
                lblResult.Text = $"Точка {extremumType}: x = {result.ExtremumPoint:F6}";

                PlotGraphWithExtremum(a, b, result, method);

            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка вычисления", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PlotGraphWithExtremum(double a, double b, GoldenRatioResult result, GoldenRatioMethod method)
        {
            PlotModel.Series.Clear();
            _functionPoints.Clear();
            _extremumPoints.Clear();

            int pointsCount = 1000;
            double step = (b - a) / pointsCount;

            // Для обработки разрывов создаем отдельные сегменты
            List<List<DataPoint>> segments = new List<List<DataPoint>>();
            List<DataPoint> currentSegment = new List<DataPoint>();

            for (int i = 0; i <= pointsCount; i++)
            {
                double x = a + i * step;
                try
                {
                    double y = method.CalculateFunction(x);

                    // Проверяем на разрыв (большие скачки значений или NaN/Infinity)
                    if (currentSegment.Count > 0)
                    {
                        double lastY = currentSegment.Last().Y;
                        double diff = Math.Abs(y - lastY);

                        // Если разрыв слишком большой или значение некорректное - начинаем новый сегмент
                        if (double.IsNaN(y) || double.IsInfinity(y) ||
                            (diff > Math.Abs(lastY) * 100 && diff > 1000))
                        {
                            // Завершаем текущий сегмент если в нем достаточно точек
                            if (currentSegment.Count > 1)
                            {
                                segments.Add(new List<DataPoint>(currentSegment));
                            }
                            currentSegment.Clear();
                            continue; // Пропускаем точку с разрывом
                        }
                    }

                    currentSegment.Add(new DataPoint(x, y));
                }
                catch
                {
                    // При ошибке вычисления завершаем текущий сегмент
                    if (currentSegment.Count > 1)
                    {
                        segments.Add(new List<DataPoint>(currentSegment));
                    }
                    currentSegment.Clear();
                }
            }

            // Добавляем последний сегмент
            if (currentSegment.Count > 1)
            {
                segments.Add(new List<DataPoint>(currentSegment));
            }

            // Добавляем все сегменты на график
            int segmentNumber = 0;
            foreach (var segment in segments)
            {
                LineSeries segmentSeries = new LineSeries
                {
                    Color = OxyColor.FromRgb(0x2C, 0x5F, 0x9E),
                    StrokeThickness = 2,
                    Title = segmentNumber == 0 ? "Функция" : null // Название только у первого сегмента
                };

                foreach (var point in segment)
                {
                    segmentSeries.Points.Add(point);
                }

                PlotModel.Series.Add(segmentSeries);
                segmentNumber++;
            }

            // Добавляем точку экстремума
            ScatterSeries extremumSeries = new ScatterSeries
            {
                Title = _findMinimum ? "Минимум" : "Максимум",
                MarkerType = MarkerType.Circle,
                MarkerSize = 8,
                MarkerFill = OxyColor.FromRgb(0xFF, 0x6B, 0x8E),
                MarkerStroke = OxyColor.FromRgb(0x2C, 0x5F, 0x9E),
                MarkerStrokeThickness = 2
            };
            extremumSeries.Points.Add(new ScatterPoint(result.ExtremumPoint, result.ExtremumValue));
            PlotModel.Series.Add(extremumSeries);

            PlotModel.InvalidatePlot(true);
        }

        private bool ValidateInput()
        {
            if (string.IsNullOrWhiteSpace(txtA.Text) || string.IsNullOrWhiteSpace(txtB.Text) ||
                string.IsNullOrWhiteSpace(txtEpsilon.Text) || string.IsNullOrWhiteSpace(txtFunction.Text))
            {
                MessageBox.Show("Все поля должны быть заполнены!", "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (!double.TryParse(txtA.Text.Replace(",", "."), NumberStyles.Any,
                    CultureInfo.InvariantCulture, out double a) ||
                !double.TryParse(txtB.Text.Replace(",", "."), NumberStyles.Any,
                    CultureInfo.InvariantCulture, out double b) ||
                !double.TryParse(txtEpsilon.Text.Replace(",", "."), NumberStyles.Any,
                    CultureInfo.InvariantCulture, out double epsilon))
            {
                MessageBox.Show("Параметры a, b и epsilon должны быть числами!", "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (Math.Abs(a) > 1e15 || Math.Abs(b) > 1e15)
            {
                MessageBox.Show("Значения a и b не должны превышать 10^15 по модулю!", "Ошибка ввода",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (Math.Abs(b - a) > 1e10)
            {
                MessageBox.Show("Интервал [a, b] слишком большой! Максимальная длина: 10^10",
                    "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (epsilon < 1e-15)
            {
                MessageBox.Show("Точность epsilon не должна быть меньше 10^-15!", "Ошибка ввода",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (a >= b)
            {
                MessageBox.Show("Значение a должно быть меньше b!", "Ошибка ввода",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (epsilon <= 0)
            {
                MessageBox.Show("Точность epsilon должна быть положительным числом!", "Ошибка ввода",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            txtA.Text = "-2";
            txtB.Text = "2";
            txtEpsilon.Text = "0,001";
            txtFunction.Text = "pow(x,2)";
            cmbExtremumType.SelectedIndex = 0;
            lblResult.Text = "Точка экстремума: ";
            PlotModel.Series.Clear();
            PlotModel.InvalidatePlot(true);
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            PlotModel?.Series.Clear();
        }

        private string PreprocessFunction(string function)
        {
            if (string.IsNullOrWhiteSpace(function))
            {
                return function;
            }

            string result = function;

            result = Regex.Replace(result, @"pow\(([^,]+),([^)]+)\)", "pow($1|SEPARATOR|$2)");
            result = Regex.Replace(result, @"log\(([^,]+),([^)]+)\)", "log($1|SEPARATOR|$2)");

            result = result.Replace(",", ".");

            result = result.Replace("|SEPARATOR|", ",");

            return result;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            cmbExtremumType.SelectedIndex = 0;
        }
    }
}