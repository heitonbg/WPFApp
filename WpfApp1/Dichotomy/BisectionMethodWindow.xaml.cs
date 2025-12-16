using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Globalization;
using System.Text.RegularExpressions;
using OxyPlot;
using OxyPlot.Series;
using OxyPlot.Axes;

namespace WpfApp1
{
    public partial class BisectionMethodWindow : Window
    {
        public PlotModel PlotModel { get; set; }
        private List<OxyPlot.DataPoint> _functionPoints;
        private List<OxyPlot.DataPoint> _rootPoints;

        private DihotomyMethod _dihotomyMethod;
        private List<BisectionIteration> _stepByStepIterations;
        private int _currentStepIndex;
        private bool _calculationPerformed = false;

        public BisectionMethodWindow()
        {
            InitializeComponent();

            PlotModel = new PlotModel
            {
                Title = "График функции и поиск корня",
                TitleColor = OxyColor.FromRgb(0x2C, 0x5F, 0x9E),
                TextColor = OxyColor.FromRgb(0x2C, 0x5F, 0x9E),
                PlotAreaBorderColor = OxyColor.FromRgb(0x2C, 0x5F, 0x9E)
            };

            PlotModel.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Bottom,
                Title = "x",
                TitleColor = OxyColor.FromRgb(0x2C, 0x5F, 0x9E),
                TextColor = OxyColor.FromRgb(0x2C, 0x5F, 0x9E),
                AxislineColor = OxyColor.FromRgb(0x2C, 0x5F, 0x9E),
                TicklineColor = OxyColor.FromRgb(0x2C, 0x5F, 0x9E),
                AxislineStyle = LineStyle.Solid,
                AxislineThickness = 1
            });
            PlotModel.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Left,
                Title = "f(x)",
                TitleColor = OxyColor.FromRgb(0x2C, 0x5F, 0x9E),
                TextColor = OxyColor.FromRgb(0x2C, 0x5F, 0x9E),
                AxislineColor = OxyColor.FromRgb(0x2C, 0x5F, 0x9E),
                TicklineColor = OxyColor.FromRgb(0x2C, 0x5F, 0x9E),
                AxislineStyle = LineStyle.Solid,
                AxislineThickness = 1
            });

            _functionPoints = new List<OxyPlot.DataPoint>();
            _rootPoints = new List<OxyPlot.DataPoint>();

            _stepByStepIterations = new List<BisectionIteration>();
            _currentStepIndex = -1;

            ResetResultFields();

            DataContext = this;
            UpdateStepControls();
        }

        public BisectionMethodWindow(MainWindow mainWindow) : this()
        {
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

                function = PreprocessFunction(function);

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

                _dihotomyMethod = new DihotomyMethod(function);

                bool functionValid = _dihotomyMethod.TestFunctionOnInterval(a, b);
                if (!functionValid)
                {
                    MessageBox.Show("Функция не определена или имеет разрывы на заданном интервале.\nПопробуйте изменить интервал [a, b].",
                                  "Ошибка функции", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                double root;
                try
                {
                    root = _dihotomyMethod.FindSingleRoot(a, b, epsilon);
                }
                catch (InvalidOperationException ex) when (ex.Message.Contains("одинаковый знак"))
                {
                    lblResult.Text = "Корень не найден";
                    lblXValue.Text = "-";
                    lblFXValue.Text = "-";

                    PlotGraphWithRoots(a, b, new List<double>(), _dihotomyMethod);

                    _calculationPerformed = true;

                    MessageBox.Show(ex.Message + "\n\nРекомендации:\n" +
                                  "- Измените интервал [a, b]\n" +
                                  "- Убедитесь, что функция пересекает ось X на этом интервале\n" +
                                  "- Проверьте правильность ввода функции",
                                  "Корень не найден", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                catch (ArgumentException ex) when (ex.Message.Contains("нет смены знака"))
                {
                    lblResult.Text = "Корень не найден";
                    lblXValue.Text = "-";
                    lblFXValue.Text = " -";

                    PlotGraphWithRoots(a, b, new List<double>(), _dihotomyMethod);

                    _calculationPerformed = true;

                    MessageBox.Show(ex.Message + "\n\nРекомендации:\n" +
                                  "- Измените интервал [a, b]\n" +
                                  "- Убедитесь, что функция пересекает ось X на этом интервале",
                                  "Корень не найден", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                int decimalPlaces = PrecisionFormatConverter.GetDecimalPlacesFromEpsilon(epsilon);

                lblResult.Text = "Корень найден!";
                lblXValue.Text = $"{root.ToString($"F{decimalPlaces}", CultureInfo.InvariantCulture)}";

                double fRoot = _dihotomyMethod.CalculateFunction(root);
                lblFXValue.Text = $"{fRoot.ToString($"E2", CultureInfo.InvariantCulture)}";

                MessageBox.Show($"Найден корень уравнения f(x) = 0\n\n" +
                              $"Корень: x = {root.ToString($"F{decimalPlaces}", CultureInfo.InvariantCulture)}\n" +
                              $"Значение функции: f(x) = {fRoot.ToString($"E2", CultureInfo.InvariantCulture)}\n" +
                              $"Количество итераций: {_dihotomyMethod.IterationsCount}\n" +
                              $"Точность ε = {epsilon}\n" +
                              $"Ответ выводится с точностью до {decimalPlaces} знаков после запятой",
                              "Результат поиска корня", MessageBoxButton.OK, MessageBoxImage.Information);

                PlotGraphWithRoots(a, b, new List<double> { root }, _dihotomyMethod);

                _calculationPerformed = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка вычисления", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PlotGraphWithRoots(double a, double b, List<double> roots, DihotomyMethod method)
        {
            PlotModel.Series.Clear();
            PlotModel.Annotations.Clear();
            _functionPoints.Clear();
            _rootPoints.Clear();

            int pointsCount = 1000;
            double step = (b - a) / pointsCount;

            List<List<OxyPlot.DataPoint>> segments = new List<List<OxyPlot.DataPoint>>();
            List<OxyPlot.DataPoint> currentSegment = new List<OxyPlot.DataPoint>();

            double minY = double.MaxValue;
            double maxY = double.MinValue;

            for (int i = 0; i <= pointsCount; i++)
            {
                double x = a + i * step;
                try
                {
                    double y = method.CalculateFunction(x);

                    if (!double.IsNaN(y) && !double.IsInfinity(y))
                    {
                        if (y < minY) minY = y;
                        if (y > maxY) maxY = y;
                    }

                    if (currentSegment.Count > 0)
                    {
                        double lastY = currentSegment.Last().Y;
                        double diff = Math.Abs(y - lastY);

                        if (double.IsNaN(y) || double.IsInfinity(y) ||
                            (diff > Math.Abs(lastY) * 100 && diff > 1000))
                        {
                            if (currentSegment.Count > 1)
                            {
                                segments.Add(new List<OxyPlot.DataPoint>(currentSegment));
                            }
                            currentSegment.Clear();
                            continue;
                        }
                    }

                    currentSegment.Add(new OxyPlot.DataPoint(x, y));
                }
                catch
                {
                    if (currentSegment.Count > 1)
                    {
                        segments.Add(new List<OxyPlot.DataPoint>(currentSegment));
                    }
                    currentSegment.Clear();
                }
            }

            if (currentSegment.Count > 1)
            {
                segments.Add(new List<OxyPlot.DataPoint>(currentSegment));
            }

            if (minY == double.MaxValue) minY = -10;
            if (maxY == double.MinValue) maxY = 10;

            double yPadding = Math.Max(Math.Abs(maxY - minY) * 0.1, 1.0);
            minY -= yPadding;
            maxY += yPadding;

            double xPadding = Math.Abs(b - a) * 0.1;
            double visibleA = a - xPadding;
            double visibleB = b + xPadding;

            if (visibleA <= 0 && visibleB >= 0)
            {
                LineSeries yAxisSeries = new LineSeries
                {
                    Color = OxyColor.FromRgb(0x80, 0x80, 0x80), 
                    StrokeThickness = 1.5,
                    LineStyle = LineStyle.Solid,
                    Title = "Ось Y (x = 0)"
                };

                yAxisSeries.Points.Add(new OxyPlot.DataPoint(0, minY));
                yAxisSeries.Points.Add(new OxyPlot.DataPoint(0, maxY));
                PlotModel.Series.Add(yAxisSeries);

                var yAxisAnnotation = new OxyPlot.Annotations.TextAnnotation
                {
                    Text = "y",
                    TextPosition = new DataPoint(0 - xPadding * 0.05, maxY - yPadding * 0.5),
                    TextColor = OxyColor.FromRgb(0x80, 0x80, 0x80),
                    FontSize = 12
                };
                PlotModel.Annotations.Add(yAxisAnnotation);
            }

            if (minY <= 0 && maxY >= 0)
            {
                LineSeries xAxisSeries = new LineSeries
                {
                    Color = OxyColor.FromRgb(0x80, 0x80, 0x80), 
                    StrokeThickness = 1.5,
                    LineStyle = LineStyle.Solid,
                    Title = "Ось X (y = 0)"
                };

                xAxisSeries.Points.Add(new OxyPlot.DataPoint(visibleA, 0));
                xAxisSeries.Points.Add(new OxyPlot.DataPoint(visibleB, 0));
                PlotModel.Series.Add(xAxisSeries);

                var xAxisAnnotation = new OxyPlot.Annotations.TextAnnotation
                {
                    Text = "x",
                    TextPosition = new DataPoint(visibleB - xPadding * 0.25, 0 - yPadding * 0.15),
                    TextColor = OxyColor.FromRgb(0x80, 0x80, 0x80),
                    FontSize = 12
                };
                PlotModel.Annotations.Add(xAxisAnnotation);
            }

            PlotModel.Axes[0].MajorGridlineColor = OxyColor.FromArgb(30, 0x80, 0x80, 0x80);
            PlotModel.Axes[0].MajorGridlineStyle = LineStyle.Dot;
            PlotModel.Axes[0].MajorGridlineThickness = 0.5;

            PlotModel.Axes[1].MajorGridlineColor = OxyColor.FromArgb(30, 0x80, 0x80, 0x80);
            PlotModel.Axes[1].MajorGridlineStyle = LineStyle.Dot;
            PlotModel.Axes[1].MajorGridlineThickness = 0.5;

            int segmentNumber = 0;
            foreach (var segment in segments)
            {
                LineSeries segmentSeries = new LineSeries
                {
                    Color = OxyColor.FromRgb(0x2C, 0x5F, 0x9E),
                    StrokeThickness = 2.5,
                    Title = segmentNumber == 0 ? "Функция f(x)" : null
                };

                foreach (var point in segment)
                {
                    segmentSeries.Points.Add(point);
                }

                PlotModel.Series.Add(segmentSeries);
                segmentNumber++;
            }

            if (roots.Any())
            {
                ScatterSeries rootsSeries = new ScatterSeries
                {
                    Title = "Найденный корень",
                    MarkerType = MarkerType.Circle,
                    MarkerSize = 10,
                    MarkerFill = OxyColor.FromRgb(0xFF, 0x6B, 0x8E), 
                    MarkerStroke = OxyColor.FromRgb(0x2C, 0x5F, 0x9E),
                    MarkerStrokeThickness = 2
                };

                foreach (double root in roots)
                {
                    try
                    {
                        double y = method.CalculateFunction(root);
                        rootsSeries.Points.Add(new ScatterPoint(root, y));

                        if (minY <= 0 && maxY >= 0)
                        {
                            LineSeries rootToXAxisSeries = new LineSeries
                            {
                                Color = OxyColor.FromRgb(0xFF, 0x6B, 0x8E), 
                                StrokeThickness = 1,
                                LineStyle = LineStyle.Dash,
                                Title = null
                            };
                            rootToXAxisSeries.Points.Add(new OxyPlot.DataPoint(root, y));
                            rootToXAxisSeries.Points.Add(new OxyPlot.DataPoint(root, 0));
                            PlotModel.Series.Add(rootToXAxisSeries);
                        }

                        if (visibleA <= 0 && visibleB >= 0)
                        {
                            LineSeries rootToYAxisSeries = new LineSeries
                            {
                                Color = OxyColor.FromRgb(0xFF, 0x6B, 0x8E), 
                                StrokeThickness = 1,
                                LineStyle = LineStyle.Dash,
                                Title = null
                            };
                            rootToYAxisSeries.Points.Add(new OxyPlot.DataPoint(root, y));
                            rootToYAxisSeries.Points.Add(new OxyPlot.DataPoint(0, y));
                            PlotModel.Series.Add(rootToYAxisSeries);
                        }

                        double epsilon = double.Parse(txtEpsilon.Text.Replace(",", "."), CultureInfo.InvariantCulture);
                        int decimalPlaces = PrecisionFormatConverter.GetDecimalPlacesFromEpsilon(epsilon);

                        var rootAnnotation = new OxyPlot.Annotations.TextAnnotation
                        {
                            Text = $"({root.ToString($"F{decimalPlaces}")}, {y.ToString($"F{decimalPlaces}")})",
                            TextPosition = new DataPoint(root + xPadding * 0.05, y + yPadding * 0.05),
                            TextColor = OxyColor.FromRgb(0xFF, 0x6B, 0x8E),
                            FontSize = 10,
                            Background = OxyColor.FromArgb(200, 255, 255, 255)
                        };
                        PlotModel.Annotations.Add(rootAnnotation);
                    }
                    catch
                    {
                        // Игнорируем корни с ошибками вычисления
                    }
                }

                PlotModel.Series.Add(rootsSeries);
            }

            if (visibleA <= 0 && visibleB >= 0 && minY <= 0 && maxY >= 0)
            {
                ScatterSeries originSeries = new ScatterSeries
                {
                    MarkerType = MarkerType.Circle,
                    MarkerSize = 4,
                    MarkerFill = OxyColor.FromRgb(0x80, 0x80, 0x80),
                    MarkerStroke = OxyColor.FromRgb(0x80, 0x80, 0x80),
                    MarkerStrokeThickness = 1,
                    Title = "Начало координат"
                };
                originSeries.Points.Add(new ScatterPoint(0, 0));
                PlotModel.Series.Add(originSeries);

                var originAnnotation = new OxyPlot.Annotations.TextAnnotation
                {
                    Text = "(0,0)",
                    TextPosition = new DataPoint(0 - xPadding * 0.1, 0 - yPadding * 0.1),
                    TextColor = OxyColor.FromRgb(0x80, 0x80, 0x80),
                    FontSize = 9
                };
                PlotModel.Annotations.Add(originAnnotation);
            }

            PlotModel.Axes[0].Minimum = visibleA;
            PlotModel.Axes[0].Maximum = visibleB;
            PlotModel.Axes[1].Minimum = minY;
            PlotModel.Axes[1].Maximum = maxY;

            try
            {
                double fa = method.CalculateFunction(a);
                double fb = method.CalculateFunction(b);

                string signA = fa >= 0 ? "+" : "-";
                string signB = fb >= 0 ? "+" : "-";

                PlotModel.Subtitle = $"f(a) = {fa.ToString("E2", CultureInfo.InvariantCulture)} ({signA}), f(b) = {fb.ToString("E2", CultureInfo.InvariantCulture)} ({signB})";
                PlotModel.SubtitleColor = OxyColor.FromRgb(0x2C, 0x5F, 0x9E);
            }
            catch
            {
                // Игнорируем ошибки при вычислении знаков
            }

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
            txtA.Text = "1";
            txtB.Text = "3";
            txtEpsilon.Text = "0,001";
            txtFunction.Text = "x^3 - 2*x^2 + x - 5";

            ResetResultFields();

            PlotModel.Series.Clear();
            PlotModel.Annotations.Clear(); 
            PlotModel.InvalidatePlot(true);

            _stepByStepIterations.Clear();
            _currentStepIndex = -1;
            _calculationPerformed = false;
            UpdateStepControls();
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

            string result = function.Trim();

            result = Regex.Replace(result, @"(\d),(\d)", "$1.$2");

            result = ConvertPowerOperator(result);

            return result;
        }

        private string ConvertPowerOperator(string expression)
        {
            string result = expression;
            int maxIterations = 10; 

            for (int i = 0; i < maxIterations; i++)
            {
                Match match = Regex.Match(result, @"([a-zA-Z0-9\.]+|\([^)]+\))\s*\^\s*([a-zA-Z0-9\.]+|\([^)]+\))");

                if (!match.Success)
                    break;

                string left = match.Groups[1].Value;
                string right = match.Groups[2].Value;

                if (left.StartsWith("(") && left.EndsWith(")"))
                    left = left.Substring(1, left.Length - 2);
                if (right.StartsWith("(") && right.EndsWith(")"))
                    right = right.Substring(1, right.Length - 2);

                string replacement = $"pow({left},{right})";
                result = result.Replace(match.Value, replacement);
            }

            return result;
        }

        private void ShowHelp_Click(object sender, RoutedEventArgs e)
        {
            ShowFunctionHelp();
        }

        private void ShowFunctionHelp()
        {
            string helpText = @"СПРАВКА ПО ФУНКЦИЯМ

Доступные математические функции:
────────────────────────────────
• Основные операции:
  +    сложение           (x + 2)
  -    вычитание          (x - 3)
  *    умножение          (x * 4)
  /    деление            (x / 5)
  ^    возведение         (x^2, (x+1)^3)

• Тригонометрические функции:
  sin(x)    - синус
  cos(x)    - косинус
  tan(x)    - тангенс
  atan(x)   - арктангенс

• Экспоненциальные и логарифмические:
  exp(x)    - экспонента (e^x)
  sqrt(x)   - квадратный корень
  log(x)    - натуральный логарифм
  log(x, b) - логарифм по основанию b
  log10(x)  - десятичный логарифм

• Другие функции:
  abs(x)    - абсолютное значение
  pow(x, y) - x в степени y

• Константы:
  pi    - число π (3.14159...)
  e     - число Эйлера (2.71828...)

Примеры использования:
─────────────────────
1. Полином: x^3 - 2*x^2 + x - 5
2. Тригонометрическая: sin(x) + cos(2*x)
3. Экспоненциальная: exp(-x) - 0.5
4. Сложная функция: log(x+1) * sin(pi*x)
5. Дробно-рациональная: (x^2 - 1)/(x + 2)

Примечания:
───────────
• Для степеней используйте оператор ^ или pow()
• Десятичные числа вводите с точкой: 0.001
• Логарифм определён только для x > 0
• Избегайте деления на ноль

Требования к интервалу [a, b]:
─────────────────────────────
• a < b
• Функция должна быть определена на интервале
• Функция должна менять знак на концах интервала";

            MessageBox.Show(helpText, "Справка по функциям", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void UpdateStepControls()
        {
        }

        private void ResetResultFields()
        {
            lblResult.Text = "";
            lblXValue.Text = "";
            lblFXValue.Text = "";
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (!_calculationPerformed)
            {
                ResetResultFields();
            }
        }
    }

    public class BisectionIteration
    {
        public int Iteration { get; set; }
        public double A { get; set; }
        public double B { get; set; }
        public double Mid { get; set; }
        public double Fmid { get; set; }
    }
}   