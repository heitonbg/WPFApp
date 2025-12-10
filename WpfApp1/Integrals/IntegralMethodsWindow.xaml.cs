using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Wpf;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using FontWeights = System.Windows.FontWeights;

namespace WpfApp1
{
    public partial class IntegralMethodsWindow : Window
    {
        public PlotModel PlotModel { get; set; }
        private LineSeries functionSeries;
        private List<LineSeries> historySeries = new List<LineSeries>();

        private IntegralCalculator _calculator;
        private bool _isStepByStepMode = false;
        private int _currentStep = 0;
        private int _stepN = 0;
        private List<IntegrationResult> _iterationHistory = new List<IntegrationResult>();
        private Dictionary<IntegrationMethod, IntegrationResult> _lastResults;
        private double _lastA, _lastB;
        private Random _random = new Random();

        public IntegralMethodsWindow()
        {
            InitializeComponent();

            InitializePlotModel();
            DataContext = this;

            btnNextStep.IsEnabled = false;
            miNextStep.IsEnabled = false;
            lblIntegralBounds.Text = "∫[0, 3.14] sin(x)dx";

            UpdateAutoNControls();
        }

        private void InitializePlotModel()
        {
            PlotModel = new PlotModel
            {
                Title = "График функции и численное интегрирование",
                TitleColor = OxyColors.DarkBlue,
                TextColor = OxyColors.DarkBlue,
                PlotAreaBorderColor = OxyColors.Gray,
                PlotAreaBorderThickness = new OxyThickness(1)
            };

            PlotModel.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Bottom,
                Title = "x",
                TitleColor = OxyColors.DarkBlue,
                TextColor = OxyColors.DarkBlue,
                AxislineColor = OxyColors.Gray,
                MajorGridlineColor = OxyColors.LightGray,
                MajorGridlineStyle = LineStyle.Dash,
                MinorGridlineColor = OxyColors.LightGray,
                MinorGridlineStyle = LineStyle.Dot
            });

            PlotModel.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Left,
                Title = "f(x)",
                TitleColor = OxyColors.DarkBlue,
                TextColor = OxyColors.DarkBlue,
                AxislineColor = OxyColors.Gray,
                MajorGridlineColor = OxyColors.LightGray,
                MajorGridlineStyle = LineStyle.Dash,
                MinorGridlineColor = OxyColors.LightGray,
                MinorGridlineStyle = LineStyle.Dot
            });

            plotView.Model = PlotModel;
        }

        private void Calculate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!ValidateInput())
                    return;

                ResetStepMode();

                var stopwatch = Stopwatch.StartNew();

                double a = double.Parse(txtA.Text.Replace(",", "."), CultureInfo.InvariantCulture);
                double b = double.Parse(txtB.Text.Replace(",", "."), CultureInfo.InvariantCulture);
                double epsilon = double.Parse(txtEpsilon.Text.Replace(",", "."), CultureInfo.InvariantCulture);
                int n = int.Parse(txtN.Text);
                string function = PreprocessFunction(txtFunction.Text);

                _lastA = a;
                _lastB = b;

                lblIntegralBounds.Text = $"∫[{FormatNumber(a)}, {FormatNumber(b)}] f(x)dx";

                _calculator = new IntegralCalculator(function);

                List<IntegrationMethod> methods = GetSelectedMethods();
                if (methods.Count == 0)
                {
                    MessageBox.Show("Выберите хотя бы один метод интегрирования!", "Внимание",
                                  MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                bool autoN = cbAutoN.IsChecked == true;

                Dictionary<IntegrationMethod, IntegrationResult> results;

                if (autoN)
                {
                    results = _calculator.CalculateWithAutoN(a, b, epsilon, n, methods);
                }
                else
                {
                    ValidateSimpsonN(n, methods.Contains(IntegrationMethod.Simpson));
                    results = _calculator.CalculateWithFixedN(a, b, n, methods);
                }

                stopwatch.Stop();
                lblTime.Text = $"Время: {stopwatch.ElapsedMilliseconds} мс";

                _lastResults = results;

                DisplayResults(results, epsilon, autoN);

                PlotIntegration(a, b, results);

                lblStatus.Text = "Вычисление завершено";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка вычисления: {ex.Message}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
                lblStatus.Text = "Ошибка вычисления";
            }
        }

        private List<IntegrationMethod> GetSelectedMethods()
        {
            List<IntegrationMethod> methods = new List<IntegrationMethod>();
            if (cbRectLeft.IsChecked == true) methods.Add(IntegrationMethod.RectangleLeft);
            if (cbRectRight.IsChecked == true) methods.Add(IntegrationMethod.RectangleRight);
            if (cbRectMid.IsChecked == true) methods.Add(IntegrationMethod.RectangleMidpoint);
            if (cbTrapezoid.IsChecked == true) methods.Add(IntegrationMethod.Trapezoidal);
            if (cbSimpson.IsChecked == true) methods.Add(IntegrationMethod.Simpson);
            return methods;
        }

        private void ValidateSimpsonN(int n, bool simpsonSelected)
        {
            if (simpsonSelected && !cbAutoN.IsChecked == true)
            {
                if (n < 2)
                {
                    MessageBox.Show("Для метода Симпсона N должно быть не менее 2!", "Ошибка",
                                  MessageBoxButton.OK, MessageBoxImage.Warning);
                    throw new ArgumentException("N < 2 для метода Симпсона");
                }
                if (n % 2 != 0)
                {
                    var result = MessageBox.Show("Для метода Симпсона N должно быть четным. Исправить на четное?",
                                               "Внимание", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (result == MessageBoxResult.Yes)
                    {
                        n = (n % 2 == 0) ? n : n + 1;
                        txtN.Text = n.ToString();
                    }
                    else
                    {
                        throw new ArgumentException("N нечетное для метода Симпсона");
                    }
                }
            }
        }

        private void DisplayResults(Dictionary<IntegrationMethod, IntegrationResult> results, double epsilon, bool autoN)
        {
            spResults.Children.Clear();

            int decimalPlaces = PrecisionFormatConverter.GetDecimalPlacesFromEpsilon(epsilon);
            string format = $"F{decimalPlaces}";

            tbRectLeft.Text = results.ContainsKey(IntegrationMethod.RectangleLeft) ?
                results[IntegrationMethod.RectangleLeft].Value.ToString(format, CultureInfo.InvariantCulture) : "-";
            tbRectRight.Text = results.ContainsKey(IntegrationMethod.RectangleRight) ?
                results[IntegrationMethod.RectangleRight].Value.ToString(format, CultureInfo.InvariantCulture) : "-";
            tbRectMid.Text = results.ContainsKey(IntegrationMethod.RectangleMidpoint) ?
                results[IntegrationMethod.RectangleMidpoint].Value.ToString(format, CultureInfo.InvariantCulture) : "-";
            tbTrapezoid.Text = results.ContainsKey(IntegrationMethod.Trapezoidal) ?
                results[IntegrationMethod.Trapezoidal].Value.ToString(format, CultureInfo.InvariantCulture) : "-";
            tbSimpson.Text = results.ContainsKey(IntegrationMethod.Simpson) ?
                results[IntegrationMethod.Simpson].Value.ToString(format, CultureInfo.InvariantCulture) : "-";

            IntegrationResult optimalResult = null;

            if (results.Count > 0)
            {
                if (autoN)
                {
                    var validResults = results.Values
                        .Where(r => r.ErrorEstimate >= 0 && !double.IsInfinity(r.ErrorEstimate) && !double.IsNaN(r.ErrorEstimate))
                        .ToList();

                    if (validResults.Count > 0)
                    {
                        optimalResult = validResults
                            .OrderBy(r => r.ErrorEstimate)
                            .FirstOrDefault();
                    }
                    else
                    {
                        optimalResult = GetMostAccurateMethodByHierarchy(results);
                    }
                }
                else
                {
                    optimalResult = GetMostAccurateMethodByHierarchy(results);
                }

                if (optimalResult != null)
                {
                    tbOptimalMethod.Text = $"Метод: {GetMethodName(optimalResult.Method)}";
                    tbOptimalValue.Text = $"Значение: {optimalResult.Value.ToString(format, CultureInfo.InvariantCulture)}";
                    tbOptimalN.Text = $"Разбиений: {optimalResult.Iterations}";

                }

                foreach (var result in results.Values)
                {
                    var panel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 2, 0, 2) };
                    panel.Children.Add(new TextBlock
                    {
                        Text = $"{GetMethodName(result.Method)}: ",
                        FontWeight = FontWeights.Bold,
                        Width = 150
                    });

                    string infoText;
                    if (autoN)
                    {
                        infoText = result.ErrorEstimate > 0 ?
                            $"(оптимальное N={result.Iterations})" :
                            $"(оптимальное N={result.Iterations})";
                    }
                    else
                    {
                        infoText = $"(фикс. N={result.Iterations})";
                    }

                    panel.Children.Add(new TextBlock
                    {
                        Text = $"{result.Value.ToString(format, CultureInfo.InvariantCulture)} {infoText}"
                    });
                    spResults.Children.Add(panel);
                }

                if (autoN)
                {
                    string nInfo = "Найденные оптимальные N: ";
                    foreach (var result in results.Values.OrderBy(r => GetMethodOrder(r.Method)))
                    {
                        nInfo += $"\n{GetMethodName(result.Method)}: N={result.Iterations}";
                    }
                    tbModeInfo.Text = nInfo;
                    tbPrecisionInfo.Text = $"Точность ε={epsilon} достигнута для всех методов";
                }
                else
                {
                    tbModeInfo.Text = $"Фиксированное N={txtN.Text} для всех методов";
                    tbPrecisionInfo.Text = "Точность не оценивается (режим фикс. N)";
                }
            }
            else
            {
                tbOptimalMethod.Text = "Метод: -";
                tbOptimalValue.Text = "Значение: -";
                tbOptimalN.Text = "Разбиений: -";
                tbModeInfo.Text = "Режим: -";
                tbPrecisionInfo.Text = "Точность: -";
            }
        }

        private double GetMethodOrder(IntegrationMethod method)
        {
            return method switch
            {
                IntegrationMethod.Simpson => 4.0,
                IntegrationMethod.Trapezoidal => 2.0,
                IntegrationMethod.RectangleMidpoint => 2.0,
                IntegrationMethod.RectangleLeft => 1.0,
                IntegrationMethod.RectangleRight => 1.0,
                _ => 0.0
            };
        }

        private IntegrationResult GetMostAccurateMethodByHierarchy(Dictionary<IntegrationMethod, IntegrationResult> results)
        {
            IntegrationMethod[] hierarchy = {
                IntegrationMethod.Simpson,        
                IntegrationMethod.Trapezoidal,    
                IntegrationMethod.RectangleMidpoint, 
                IntegrationMethod.RectangleLeft,  
                IntegrationMethod.RectangleRight  
            };

            foreach (var method in hierarchy)
            {
                if (results.ContainsKey(method))
                {
                    return results[method];
                }
            }

            return results.Values.FirstOrDefault();
        }

        private string FormatNumber(double value)
        {
            return Math.Abs(value) < 0.001 || Math.Abs(value) > 1000 ?
                value.ToString("G4", CultureInfo.InvariantCulture) :
                value.ToString("F2", CultureInfo.InvariantCulture);
        }

        private string GetMethodName(IntegrationMethod method)
        {
            return method switch
            {
                IntegrationMethod.RectangleLeft => "Прямоуг. (лев.)",
                IntegrationMethod.RectangleRight => "Прямоуг. (прав.)",
                IntegrationMethod.RectangleMidpoint => "Прямоуг. (сред.)",
                IntegrationMethod.Trapezoidal => "Трапеций",
                IntegrationMethod.Simpson => "Симпсона",
                _ => "Неизвестный"
            };
        }

        private void PlotIntegration(double a, double b, Dictionary<IntegrationMethod, IntegrationResult> results)
        {
            PlotModel.Series.Clear();
            historySeries.Clear();

            PlotFunction(a, b);

            IntegrationResult mostAccurate = GetMostAccurateMethodByHierarchy(results);
            int nToPlot = mostAccurate?.Iterations ?? int.Parse(txtN.Text);

            foreach (var method in results.Keys)
            {
                PlotMethodPartitions(a, b, nToPlot, method, results[method]);
            }

            if (miShowHistory.IsChecked == true && results.Count > 0 && mostAccurate != null)
            {
                ShowHistoryPartitions(a, b, mostAccurate);
            }

            PlotModel.InvalidatePlot(true);
        }

        private void ShowHistoryPartitions(double a, double b, IntegrationResult result)
        {
            if (result.HistoryN == null || result.HistoryN.Count <= 1) return;

            cmbHistoryN.Items.Clear();
            for (int i = 0; i < result.HistoryN.Count; i++)
            {
                cmbHistoryN.Items.Add($"N={result.HistoryN[i]} (шаг {i + 1})");
            }
            cmbHistoryN.SelectedIndex = result.HistoryN.Count - 1;

            historyPanel.Visibility = Visibility.Visible;
        }

        private void PlotFunction(double a, double b)
        {
            functionSeries = new LineSeries
            {
                Title = $"f(x) = {txtFunction.Text}",
                Color = OxyColors.Blue,
                StrokeThickness = 2
            };

            int pointsCount = 500;
            double step = (b - a) / pointsCount;

            for (int i = 0; i <= pointsCount; i++)
            {
                double x = a + i * step;
                try
                {
                    double y = _calculator.CalculateFunction(x);
                    if (!double.IsNaN(y) && !double.IsInfinity(y))
                    {
                        functionSeries.Points.Add(new DataPoint(x, y));
                    }
                }
                catch { }
            }

            PlotModel.Series.Add(functionSeries);
        }

        private void PlotMethodPartitions(double a, double b, int n, IntegrationMethod method, IntegrationResult result)
        {
            if (n <= 0) return;

            if (method == IntegrationMethod.Simpson && n % 2 != 0)
            {
                n++;
            }

            double h = (b - a) / n;
            OxyColor color;
            string title;

            switch (method)
            {
                case IntegrationMethod.RectangleLeft:
                    color = OxyColors.Red;
                    title = "Прямоуг. (лев.)";
                    break;
                case IntegrationMethod.RectangleRight:
                    color = OxyColors.DarkOrange;
                    title = "Прямоуг. (прав.)";
                    break;
                case IntegrationMethod.RectangleMidpoint:
                    color = OxyColors.Purple;
                    title = "Прямоуг. (сред.)";
                    break;
                case IntegrationMethod.Trapezoidal:
                    color = OxyColors.Green;
                    title = "Трапеций";
                    break;
                case IntegrationMethod.Simpson:
                    color = OxyColors.DarkCyan;
                    title = "Симпсона";
                    break;
                default:
                    return;
            }

            if (method == IntegrationMethod.Simpson)
            {
                PlotSimpsonPartitions(a, b, n, color, title);
            }
            else
            {
                PlotSimplePartitions(a, b, n, h, method, color, title);
            }
        }

        private void PlotSimplePartitions(double a, double b, int n, double h, IntegrationMethod method, OxyColor color, string title)
        {
            if (n < 1) return;

            var partitionSeries = new LineSeries
            {
                Title = title,
                Color = color,
                StrokeThickness = 1,
                LineStyle = LineStyle.Solid
            };

            for (int i = 0; i < n; i++)
            {
                double x1 = a + i * h;
                double x2 = a + (i + 1) * h;

                double y1, y2;

                switch (method)
                {
                    case IntegrationMethod.RectangleLeft:
                        y1 = _calculator.CalculateFunction(x1);
                        y2 = y1;
                        break;
                    case IntegrationMethod.RectangleRight:
                        y2 = _calculator.CalculateFunction(x2);
                        y1 = y2;
                        break;
                    case IntegrationMethod.RectangleMidpoint:
                        double xMid = (x1 + x2) / 2;
                        y1 = _calculator.CalculateFunction(xMid);
                        y2 = y1;
                        break;
                    case IntegrationMethod.Trapezoidal:
                        y1 = _calculator.CalculateFunction(x1);
                        y2 = _calculator.CalculateFunction(x2);
                        break;
                    default:
                        y1 = y2 = 0;
                        break;
                }

                partitionSeries.Points.Add(new DataPoint(x1, 0));
                partitionSeries.Points.Add(new DataPoint(x1, y1));
                partitionSeries.Points.Add(new DataPoint(x2, y2));
                partitionSeries.Points.Add(new DataPoint(x2, 0));
                partitionSeries.Points.Add(new DataPoint(x1, 0));

                partitionSeries.Points.Add(new DataPoint(double.NaN, double.NaN));
            }

            PlotModel.Series.Add(partitionSeries);
        }

        private void PlotSimpsonPartitions(double a, double b, int n, OxyColor color, string title)
        {
            if (n < 2) return;

            var simpsonSeries = new LineSeries
            {
                Title = title,
                Color = color,
                StrokeThickness = 1.5,
                LineStyle = LineStyle.Solid
            };

            double h = (b - a) / n;

            for (int i = 0; i < n; i += 2)
            {
                if (i + 2 > n) break;

                double x0 = a + i * h;
                double x1 = a + (i + 1) * h;
                double x2 = a + (i + 2) * h;

                try
                {
                    double y0 = _calculator.CalculateFunction(x0);
                    double y1 = _calculator.CalculateFunction(x1);
                    double y2 = _calculator.CalculateFunction(x2);

                    if (double.IsNaN(y0) || double.IsInfinity(y0) ||
                        double.IsNaN(y1) || double.IsInfinity(y1) ||
                        double.IsNaN(y2) || double.IsInfinity(y2))
                    {
                        continue; 
                    }

                    for (int j = 0; j <= 20; j++)
                    {
                        double t = j / 20.0;
                        double x = x0 + t * 2 * h;

                        double denominator0 = (x0 - x1) * (x0 - x2);
                        double denominator1 = (x1 - x0) * (x1 - x2);
                        double denominator2 = (x2 - x0) * (x2 - x1);

                        if (Math.Abs(denominator0) < 1e-15 ||
                            Math.Abs(denominator1) < 1e-15 ||
                            Math.Abs(denominator2) < 1e-15)
                            continue;

                        double term0 = y0 * ((x - x1) * (x - x2)) / denominator0;
                        double term1 = y1 * ((x - x0) * (x - x2)) / denominator1;
                        double term2 = y2 * ((x - x0) * (x - x1)) / denominator2;

                        double y = term0 + term1 + term2;

                        if (!double.IsNaN(y) && !double.IsInfinity(y))
                        {
                            simpsonSeries.Points.Add(new DataPoint(x, y));
                        }
                    }

                    simpsonSeries.Points.Add(new DataPoint(double.NaN, double.NaN));

                    var verticalSeries = new LineSeries
                    {
                        Color = color,
                        StrokeThickness = 0.7,
                        LineStyle = LineStyle.Dash
                    };

                    verticalSeries.Points.Add(new DataPoint(x0, 0));
                    verticalSeries.Points.Add(new DataPoint(x0, y0));
                    verticalSeries.Points.Add(new DataPoint(double.NaN, double.NaN));

                    verticalSeries.Points.Add(new DataPoint(x1, 0));
                    verticalSeries.Points.Add(new DataPoint(x1, y1));
                    verticalSeries.Points.Add(new DataPoint(double.NaN, double.NaN));

                    verticalSeries.Points.Add(new DataPoint(x2, 0));
                    verticalSeries.Points.Add(new DataPoint(x2, y2));

                    PlotModel.Series.Add(verticalSeries);
                }
                catch (Exception)
                {
                    continue;
                }
            }

            if (simpsonSeries.Points.Count > 0)
            {
                PlotModel.Series.Add(simpsonSeries);
            }
        }

        private void StartStepByStep_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!ValidateInput())
                    return;

                ResetStepMode();

                double a = double.Parse(txtA.Text.Replace(",", "."), CultureInfo.InvariantCulture);
                double b = double.Parse(txtB.Text.Replace(",", "."), CultureInfo.InvariantCulture);
                string function = PreprocessFunction(txtFunction.Text);

                _calculator = new IntegralCalculator(function);
                _iterationHistory.Clear();
                _currentStep = 0;
                _stepN = 2; 

                _isStepByStepMode = true;
                btnNextStep.IsEnabled = true;
                miNextStep.IsEnabled = true;
                btnStepByStep.IsEnabled = false;
                btnCalculate.IsEnabled = false;

                lblStepInfo.Text = $"Шаг 1: Начало интервала [{FormatNumber(a)}, {FormatNumber(b)}], N={_stepN}";
                PlotFunction(a, b);
                lblStatus.Text = "Пошаговый режим: готов к первому шагу";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void NextStep_Click(object sender, RoutedEventArgs e)
        {
            if (!_isStepByStepMode || _calculator == null) return;

            try
            {
                double a = double.Parse(txtA.Text.Replace(",", "."), CultureInfo.InvariantCulture);
                double b = double.Parse(txtB.Text.Replace(",", "."), CultureInfo.InvariantCulture);
                int maxN = int.Parse(txtN.Text);

                _currentStep++;

                _stepN += 2; 

                if (_stepN > maxN && !cbAutoN.IsChecked == true)
                {
                    _stepN = maxN;
                }

                List<IntegrationMethod> methods = GetSelectedMethods();
                if (methods.Count == 0) return;

                Dictionary<IntegrationMethod, IntegrationResult> results;

                results = _calculator.CalculateWithFixedN(a, b, _stepN, methods);

                foreach (var result in results.Values)
                {
                    _iterationHistory.Add(result);
                }

                if (results.Count > 0)
                {
                    var firstResult = results.Values.First();
                    lblStepInfo.Text = $"Шаг {_currentStep}: N={_stepN}, значение={firstResult.Value:F6}";

                    PlotIntegration(a, b, results);

                    if (_stepN >= maxN && !cbAutoN.IsChecked == true)
                    {
                        FinishStepByStep(results);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка на шаге: {ex.Message}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
                FinishStepByStep(new Dictionary<IntegrationMethod, IntegrationResult>());
            }
        }

        private void FinishStepByStep(Dictionary<IntegrationMethod, IntegrationResult> results)
        {
            _isStepByStepMode = false;
            btnNextStep.IsEnabled = false;
            miNextStep.IsEnabled = false;
            btnStepByStep.IsEnabled = true;
            btnCalculate.IsEnabled = true;

            if (results.Count > 0)
            {
                double epsilon = double.Parse(txtEpsilon.Text.Replace(",", "."), CultureInfo.InvariantCulture);
                bool autoN = cbAutoN.IsChecked == true;
                DisplayResults(results, epsilon, autoN);
                lblStatus.Text = $"Пошаговый режим завершен. Выполнено шагов: {_currentStep}";
            }
            else
            {
                lblStatus.Text = "Пошаговый режим прерван";
            }
        }

        private void ResetStepMode()
        {
            _isStepByStepMode = false;
            btnNextStep.IsEnabled = false;
            miNextStep.IsEnabled = false;
            btnStepByStep.IsEnabled = true;
            btnCalculate.IsEnabled = true;
            _iterationHistory.Clear();
            _currentStep = 0;
            _stepN = 0;
            lblStepInfo.Text = "";
            lblStatus.Text = "Готов к работе";
            historyPanel.Visibility = Visibility.Collapsed;
        }

        private void ResetSteps_Click(object sender, RoutedEventArgs e)
        {
            ResetStepMode();
        }

        private bool ValidateInput()
        {
            if (string.IsNullOrWhiteSpace(txtA.Text) ||
                string.IsNullOrWhiteSpace(txtB.Text) ||
                string.IsNullOrWhiteSpace(txtEpsilon.Text) ||
                string.IsNullOrWhiteSpace(txtN.Text) ||
                string.IsNullOrWhiteSpace(txtFunction.Text))
            {
                MessageBox.Show("Все поля должны быть заполнены!", "Ошибка ввода",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (!double.TryParse(txtA.Text.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out double a) ||
                !double.TryParse(txtB.Text.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out double b) ||
                !double.TryParse(txtEpsilon.Text.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out double epsilon) ||
                !int.TryParse(txtN.Text, out int n))
            {
                MessageBox.Show("Некорректные числовые значения!", "Ошибка ввода",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (a >= b)
            {
                MessageBox.Show("Должно быть a < b!", "Ошибка ввода",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (epsilon <= 0)
            {
                MessageBox.Show("Точность epsilon должна быть положительным числом!", "Ошибка ввода",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (n <= 0 || n > 1000000)
            {
                MessageBox.Show("Количество разбиений N должно быть от 1 до 1 000 000!", "Ошибка ввода",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        private void ClearAll_Click(object sender, RoutedEventArgs e)
        {
            txtA.Text = "0";
            txtB.Text = "3.14159";
            txtEpsilon.Text = "0.0001";
            txtN.Text = "10";
            txtFunction.Text = "sin(x)";

            cbRectLeft.IsChecked = true;
            cbRectRight.IsChecked = false;
            cbRectMid.IsChecked = false;
            cbTrapezoid.IsChecked = true;
            cbSimpson.IsChecked = true;

            spResults.Children.Clear();
            tbRectLeft.Text = tbRectRight.Text = tbRectMid.Text = tbTrapezoid.Text = tbSimpson.Text = "-";
            tbOptimalMethod.Text = "Метод: -";
            tbOptimalValue.Text = "Значение: -";
            tbOptimalN.Text = "Разбиений: -";
            tbModeInfo.Text = "Режим: -";
            tbPrecisionInfo.Text = "Точность: -";

            lblStepInfo.Text = "";
            lblStatus.Text = "Готов к работе";
            lblTime.Text = "Время: 0 мс";
            lblIntegralBounds.Text = "∫[a,b] f(x)dx";

            PlotModel.Series.Clear();
            InitializePlotModel();
            ResetStepMode();
            UpdateAutoNControls();
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (plotView != null)
            {
                plotView.Model = null;
            }

            PlotModel?.Series.Clear();
        }

        private void Help_Click(object sender, RoutedEventArgs e)
        {
            ShowSyntaxHelp();
        }

        private void ShowSyntaxHelp()
        {
            string helpText = @"Поддерживаемые математические функции:

Базовые операции: + - * / ^
Степень: x^2 или pow(x,2)
Тригонометрические: sin(x), cos(x), tan(x)
Обратные тригонометрические: asin(x), acos(x), atan(x)
Экспонента и логарифмы: exp(x), ln(x), log10(x), log(x,основание)
Корни: sqrt(x), x^(1/2)
Модуль: abs(x)
Гиперболические: sinh(x), cosh(x), tanh(x)

Константы: pi, e

Примеры функций для интегрирования:
• sin(x)
• x^2 + 2*x + 1
• exp(-x^2)
• 1/(1+x^2)
• sqrt(4-x^2)";

            MessageBox.Show(helpText, "Справка по синтаксису функций",
                          MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void AboutMethods_Click(object sender, RoutedEventArgs e)
        {
            string aboutText = @"Методы численного интегрирования:

1. Метод прямоугольников:
   - Левые: ∫f(x)dx ≈ h·Σf(xᵢ)
   - Правые: ∫f(x)dx ≈ h·Σf(xᵢ₊₁)
   - Средние: ∫f(x)dx ≈ h·Σf((xᵢ+xᵢ₊₁)/2)
   Порядок точности: 1

2. Метод трапеций:
   ∫f(x)dx ≈ h/2·[f(a)+2Σf(xᵢ)+f(b)]
   Порядок точности: 2

3. Метод Симпсона (парабол):
   ∫f(x)dx ≈ h/3·[f(a)+4Σf(x_нечет)+2Σf(x_чет)+f(b)]
   Порядок точности: 4
   Требует четного N

АВТО ПОДБОР N:
Каждый метод находит СВОЕ оптимальное количество разбиений,
необходимое для достижения заданной точности ε.
Более точные методы (Симпсон) требуют меньше разбиений,
менее точные (прямоугольники) - больше.";

            MessageBox.Show(aboutText, "О методах интегрирования",
                          MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void AutoNCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            UpdateAutoNControls();
        }

        private void AutoNCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            UpdateAutoNControls();
        }

        private void UpdateAutoNControls()
        {
            bool autoN = cbAutoN.IsChecked == true;

            txtEpsilon.IsEnabled = true;
            txtEpsilon.Background = Brushes.White;

            txtN.IsEnabled = !autoN;
            txtN.Background = !autoN ? Brushes.White : Brushes.LightGray;

            if (autoN)
            {
                tbModeInfo.Text = "Автоматический выбор N (каждый метод находит свое оптимальное N)";
                lblCurrentMode.Text = "Режим: авто N для каждого метода";
                lblStatus.Text = "Включен авто подбор N: каждый метод найдет свое оптимальное количество разбиений";
            }
            else
            {
                tbModeInfo.Text = $"Фиксированное N: {txtN.Text} для всех методов";
                lblCurrentMode.Text = "Режим: фиксированное N";
                lblStatus.Text = $"Используется указанное количество разбиений: {txtN.Text} для всех методов";
            }

            tbPrecisionInfo.Text = $"Точность: {txtEpsilon.Text}";
        }

        private string PreprocessFunction(string function)
        {
            if (string.IsNullOrWhiteSpace(function))
                return function;

            string result = function.Trim();

            result = result.Replace("ln", "log");
            result = result.Replace(",", ".");

            result = ProcessPowerOperator(result);

            return result;
        }

        private string ProcessPowerOperator(string expression)
        {
            string pattern = @"([a-zA-Z0-9\(\)\.]+)\^([a-zA-Z0-9\(\)\.\/]+)";

            while (Regex.IsMatch(expression, pattern))
            {
                var match = Regex.Match(expression, pattern);
                string basePart = match.Groups[1].Value;
                string exponentPart = match.Groups[2].Value;

                if (exponentPart == "1/2" || exponentPart == "0.5")
                {
                    expression = expression.Replace(match.Value, $"sqrt({basePart})");
                }
                else if (exponentPart == "2")
                {
                    expression = expression.Replace(match.Value, $"({basePart})*({basePart})");
                }
                else
                {
                    expression = expression.Replace(match.Value, $"pow({basePart},{exponentPart})");
                }
            }

            return expression;
        }

        private void GenerateRandomData_Click(object sender, RoutedEventArgs e)
        {
            string[] functions = {
                "sin(x)", "cos(x)", "x^2", "exp(-x^2)", "1/(1+x^2)",
                "sqrt(4-x^2)", "x^3 - 2*x + 1", "tan(x)", "log(x+1)", "abs(sin(x))"
            };

            txtFunction.Text = functions[_random.Next(functions.Length)];

            double a = Math.Round(_random.NextDouble() * 4 - 2, 2);
            double b = Math.Round(a + _random.NextDouble() * 3 + 0.5, 2);
            txtA.Text = a.ToString(CultureInfo.InvariantCulture);
            txtB.Text = b.ToString(CultureInfo.InvariantCulture);

            double[] precisions = { 0.1, 0.01, 0.001, 0.0001, 0.00001 };
            txtEpsilon.Text = precisions[_random.Next(precisions.Length)].ToString(CultureInfo.InvariantCulture);

            txtN.Text = (_random.Next(5) * 2 + 2).ToString();

            MessageBox.Show("Случайные данные сгенерированы!\nНажмите 'Рассчитать' для вычисления.",
                          "Генерация данных", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ShowHistory_Checked(object sender, RoutedEventArgs e)
        {
            if (_lastResults != null && _lastResults.Count > 0)
            {
                IntegrationResult mostAccurate = GetMostAccurateMethodByHierarchy(_lastResults);
                ShowHistoryPartitions(_lastA, _lastB, mostAccurate);
            }
        }

        private void ShowHistory_Unchecked(object sender, RoutedEventArgs e)
        {
            historyPanel.Visibility = Visibility.Collapsed;
            if (_lastResults != null)
            {
                PlotIntegration(_lastA, _lastB, _lastResults);
            }
        }

        private void ToggleGrid_Click(object sender, RoutedEventArgs e)
        {
            foreach (var axis in PlotModel.Axes)
            {
                if (miShowGrid.IsChecked == true)
                {
                    axis.MajorGridlineStyle = LineStyle.Dash;
                    axis.MinorGridlineStyle = LineStyle.Dot;
                }
                else
                {
                    axis.MajorGridlineStyle = LineStyle.None;
                    axis.MinorGridlineStyle = LineStyle.None;
                }
            }
            PlotModel.InvalidatePlot(true);
        }

        private void HistoryN_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbHistoryN.SelectedIndex >= 0 && _lastResults != null && _lastResults.Count > 0)
            {
                IntegrationResult mostAccurate = GetMostAccurateMethodByHierarchy(_lastResults);
                if (cmbHistoryN.SelectedIndex < mostAccurate.HistoryN.Count)
                {
                    int selectedN = mostAccurate.HistoryN[cmbHistoryN.SelectedIndex];
                    double value = mostAccurate.History[cmbHistoryN.SelectedIndex];
                    lblHistoryValue.Text = $"Значение: {value:F6}";

                    PlotModel.Series.Clear();
                    PlotFunction(_lastA, _lastB);
                    PlotMethodPartitions(_lastA, _lastB, selectedN, mostAccurate.Method, mostAccurate);
                    PlotModel.InvalidatePlot(true);
                }
            }
        }
    }
}