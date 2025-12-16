using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using OxyPlot;
using OxyPlot.Series;
using OxyPlot.Axes;
using OxyPlot.Annotations;
using System.Windows.Media;

namespace WpfApp1
{
    public partial class GoldenRatioWindow : Window
    {
        public PlotModel PlotModel { get; set; }
        private List<DataPoint> _functionPoints;
        private List<DataPoint> _extremumPoints;
        private List<DataPoint> _rootPoints;

        private MainWindow _mainWindow;
        private bool _findMinimum = true;
        private bool _calculationPerformed = false;

        public GoldenRatioWindow()
        {
            InitializeComponent();

            PlotModel = new PlotModel
            {
                Title = "График функции и поиск экстремума",
                TitleColor = OxyColor.FromRgb(0x2C, 0x5F, 0x9E),
                TextColor = OxyColor.FromRgb(0x2C, 0x5F, 0x9E),
                PlotAreaBorderColor = OxyColor.FromRgb(0x2C, 0x5F, 0x9E),
                Background = OxyColors.White
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

            _functionPoints = new List<DataPoint>();
            _extremumPoints = new List<DataPoint>();
            _rootPoints = new List<DataPoint>();

            DataContext = this;
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

                GoldenRatioMethod method = new GoldenRatioMethod(function);

                bool functionValid = method.TestFunctionOnInterval(a, b);
                if (!functionValid)
                {
                    MessageBox.Show("Функция не определена или имеет разрывы на заданном интервале.\nПопробуйте изменить интервал [a, b].",
                                  "Ошибка функции", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                GoldenRatioResult result = method.FindGlobalExtremum(a, b, epsilon, _findMinimum);

                int decimalPlaces = PrecisionFormatConverter.GetDecimalPlacesFromEpsilon(epsilon);
                string extremumType = _findMinimum ? "минимума" : "максимума";

                lblResult.Text = $"Точка {extremumType} найдена!\n" +
                               $"x = {result.ExtremumPoint.ToString($"F{decimalPlaces}", CultureInfo.InvariantCulture)}\n" +
                               $"f(x) = {result.ExtremumValue.ToString($"F{decimalPlaces}", CultureInfo.InvariantCulture)}\n" +
                               $"Количество итераций: {result.Iterations}\n" +
                               $"Финальный интервал: [{result.FinalInterval.a.ToString($"F{decimalPlaces}")}, " +
                               $"{result.FinalInterval.b.ToString($"F{decimalPlaces}")}]";

                lblRootResult.Text = "";

                MessageBox.Show($"Найден {extremumType} функции f(x)\n\n" +
                              $"Точка {extremumType}: x = {result.ExtremumPoint.ToString($"F{decimalPlaces}", CultureInfo.InvariantCulture)}\n" +
                              $"Значение функции: f(x) = {result.ExtremumValue.ToString($"F{decimalPlaces}", CultureInfo.InvariantCulture)}\n" +
                              $"Количество итераций: {result.Iterations}\n" +
                              $"Точность ε = {epsilon}\n" +
                              $"Ответ выводится с точностью до {decimalPlaces} знаков после запятой",
                              $"Результат поиска {extremumType}", MessageBoxButton.OK, MessageBoxImage.Information);

                PlotGraphWithExtremum(a, b, result, method);

                _calculationPerformed = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка вычисления", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private int CalculateDecimalPlaces(double epsilon)
        {
            return PrecisionFormatConverter.GetDecimalPlacesFromEpsilon(epsilon);
        }

        private void FindRoot_Click(object sender, RoutedEventArgs e)
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

                GoldenRatioMethod method = new GoldenRatioMethod(function);

                bool functionValid = method.TestFunctionOnInterval(a, b);
                if (!functionValid)
                {
                    MessageBox.Show("Функция не определена или имеет разрывы на заданном интервале.\nПопробуйте изменить интервал [a, b].",
                                  "Ошибка функции", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                try
                {
                    GoldenRatioResult result = method.FindRoot(a, b, epsilon);

                    int decimalPlaces = PrecisionFormatConverter.GetDecimalPlacesFromEpsilon(epsilon);

                    lblResult.Text = $"Корень уравнения f(x) = 0 найден!\n" +
                                   $"x = {result.ExtremumPoint.ToString($"F{decimalPlaces}", CultureInfo.InvariantCulture)}\n" +
                                   $"f(x) = {result.ExtremumValue.ToString($"F{decimalPlaces}", CultureInfo.InvariantCulture)}\n" +
                                   $"Количество итераций: {result.Iterations}\n" +
                                   $"Финальный интервал: [{result.FinalInterval.a.ToString($"F{decimalPlaces}")}, " +
                                   $"{result.FinalInterval.b.ToString($"F{decimalPlaces}")}]";

                    lblRootResult.Text = "";

                    MessageBox.Show($"Найден корень уравнения f(x) = 0\n\n" +
                                  $"Корень: x = {result.ExtremumPoint.ToString($"F{decimalPlaces}", CultureInfo.InvariantCulture)}\n" +
                                  $"Значение функции: f(x) = {result.ExtremumValue.ToString($"F{decimalPlaces}", CultureInfo.InvariantCulture)}\n" +
                                  $"Количество итераций: {result.Iterations}\n" +
                                  $"Точность ε = {epsilon}\n" +
                                  $"Ответ выводится с точностью до {decimalPlaces} знаков после запятой",
                                  "Результат поиска корня", MessageBoxButton.OK, MessageBoxImage.Information);

                    PlotGraphWithRoot(a, b, result, method);

                    _calculationPerformed = true;
                }
                catch (ArgumentException ex)
                {
                    if (ex.Message.Contains("не меняет знак"))
                    {
                        lblResult.Text = "Корень не найден\n" +
                                       "Функция не меняет знак на заданном интервале [a, b].\n" +
                                       "Попробуйте другой интервал или проверьте функцию.";
                        lblRootResult.Text = "";

                        PlotGraphWithRoot(a, b, null, method);

                        MessageBox.Show("Функция не меняет знак на заданном интервале [a, b].\n" +
                                      "Попробуйте другой интервал или проверьте функцию.\n\n" +
                                      "Рекомендации:\n" +
                                      "- Измените интервал [a, b]\n" +
                                      "- Убедитесь, что функция пересекает ось X на этом интервале",
                                      "Корень не найден", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка вычисления корня", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PlotGraphWithExtremum(double a, double b, GoldenRatioResult result, GoldenRatioMethod method)
        {
            PlotModel.Series.Clear();
            PlotModel.Annotations.Clear();
            _functionPoints.Clear();
            _extremumPoints.Clear();

            int pointsCount = 1000;
            double step = (b - a) / pointsCount;

            List<List<DataPoint>> segments = new List<List<DataPoint>>();
            List<DataPoint> currentSegment = new List<DataPoint>();

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
                                segments.Add(new List<DataPoint>(currentSegment));
                            }
                            currentSegment.Clear();
                            continue;
                        }
                    }

                    currentSegment.Add(new DataPoint(x, y));
                }
                catch
                {
                    if (currentSegment.Count > 1)
                    {
                        segments.Add(new List<DataPoint>(currentSegment));
                    }
                    currentSegment.Clear();
                }
            }

            if (currentSegment.Count > 1)
            {
                segments.Add(new List<DataPoint>(currentSegment));
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

                yAxisSeries.Points.Add(new DataPoint(0, minY));
                yAxisSeries.Points.Add(new DataPoint(0, maxY));
                PlotModel.Series.Add(yAxisSeries);

                var yAxisAnnotation = new TextAnnotation
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

                xAxisSeries.Points.Add(new DataPoint(visibleA, 0));
                xAxisSeries.Points.Add(new DataPoint(visibleB, 0));
                PlotModel.Series.Add(xAxisSeries);

                var xAxisAnnotation = new TextAnnotation
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

            if (result != null)
            {
                ScatterSeries extremumSeries = new ScatterSeries
                {
                    Title = _findMinimum ? "Минимум" : "Максимум",
                    MarkerType = MarkerType.Circle,
                    MarkerSize = 10,
                    MarkerFill = OxyColor.FromRgb(0xFF, 0x6B, 0x8E), 
                    MarkerStroke = OxyColor.FromRgb(0x2C, 0x5F, 0x9E),
                    MarkerStrokeThickness = 2
                };

                extremumSeries.Points.Add(new ScatterPoint(result.ExtremumPoint, result.ExtremumValue));

                if (minY <= 0 && maxY >= 0)
                {
                    LineSeries extremumToXAxisSeries = new LineSeries
                    {
                        Color = OxyColor.FromRgb(0xFF, 0x6B, 0x8E), 
                        StrokeThickness = 1,
                        LineStyle = LineStyle.Dash,
                        Title = null
                    };
                    extremumToXAxisSeries.Points.Add(new DataPoint(result.ExtremumPoint, result.ExtremumValue));
                    extremumToXAxisSeries.Points.Add(new DataPoint(result.ExtremumPoint, 0));
                    PlotModel.Series.Add(extremumToXAxisSeries);
                }

                if (visibleA <= 0 && visibleB >= 0)
                {
                    LineSeries extremumToYAxisSeries = new LineSeries
                    {
                        Color = OxyColor.FromRgb(0xFF, 0x6B, 0x8E), 
                        StrokeThickness = 1,
                        LineStyle = LineStyle.Dash,
                        Title = null
                    };
                    extremumToYAxisSeries.Points.Add(new DataPoint(result.ExtremumPoint, result.ExtremumValue));
                    extremumToYAxisSeries.Points.Add(new DataPoint(0, result.ExtremumValue));
                    PlotModel.Series.Add(extremumToYAxisSeries);
                }

                double epsilon = double.Parse(txtEpsilon.Text.Replace(",", "."), CultureInfo.InvariantCulture);
                int decimalPlaces = PrecisionFormatConverter.GetDecimalPlacesFromEpsilon(epsilon);

                var extremumAnnotation = new TextAnnotation
                {
                    Text = $"({result.ExtremumPoint.ToString($"F{decimalPlaces}")}, {result.ExtremumValue.ToString($"F{decimalPlaces}")})",
                    TextPosition = new DataPoint(result.ExtremumPoint + xPadding * 0.05, result.ExtremumValue + yPadding * 0.05),
                    TextColor = OxyColor.FromRgb(0xFF, 0x6B, 0x8E),
                    FontSize = 10,
                    Background = OxyColor.FromArgb(200, 255, 255, 255)
                };
                PlotModel.Annotations.Add(extremumAnnotation);

                PlotModel.Series.Add(extremumSeries);
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

                var originAnnotation = new TextAnnotation
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

                string extremumType = _findMinimum ? "минимума" : "максимума";
                PlotModel.Subtitle = $"Поиск {extremumType}: f({a:F2}) = {fa.ToString("E2", CultureInfo.InvariantCulture)}, f({b:F2}) = {fb.ToString("E2", CultureInfo.InvariantCulture)}";
                PlotModel.SubtitleColor = OxyColor.FromRgb(0x2C, 0x5F, 0x9E);
            }
            catch
            {
                // Игнорируем ошибки при вычислении
            }

            PlotModel.InvalidatePlot(true);
        }

        private void PlotGraphWithRoot(double a, double b, GoldenRatioResult result, GoldenRatioMethod method)
        {
            PlotModel.Series.Clear();
            PlotModel.Annotations.Clear();
            _functionPoints.Clear();
            _rootPoints.Clear();

            int pointsCount = 1000;
            double step = (b - a) / pointsCount;

            List<List<DataPoint>> segments = new List<List<DataPoint>>();
            List<DataPoint> currentSegment = new List<DataPoint>();

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
                                segments.Add(new List<DataPoint>(currentSegment));
                            }
                            currentSegment.Clear();
                            continue;
                        }
                    }

                    currentSegment.Add(new DataPoint(x, y));
                }
                catch
                {
                    if (currentSegment.Count > 1)
                    {
                        segments.Add(new List<DataPoint>(currentSegment));
                    }
                    currentSegment.Clear();
                }
            }

            if (currentSegment.Count > 1)
            {
                segments.Add(new List<DataPoint>(currentSegment));
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

                yAxisSeries.Points.Add(new DataPoint(0, minY));
                yAxisSeries.Points.Add(new DataPoint(0, maxY));
                PlotModel.Series.Add(yAxisSeries);

                var yAxisAnnotation = new TextAnnotation
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

                xAxisSeries.Points.Add(new DataPoint(visibleA, 0));
                xAxisSeries.Points.Add(new DataPoint(visibleB, 0));
                PlotModel.Series.Add(xAxisSeries);

                var xAxisAnnotation = new TextAnnotation
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

            if (result != null)
            {
                ScatterSeries rootSeries = new ScatterSeries
                {
                    Title = "Корень",
                    MarkerType = MarkerType.Circle,
                    MarkerSize = 10,
                    MarkerFill = OxyColor.FromRgb(0x4C, 0xAF, 0x50), 
                    MarkerStroke = OxyColor.FromRgb(0x2C, 0x5F, 0x9E),
                    MarkerStrokeThickness = 2
                };

                rootSeries.Points.Add(new ScatterPoint(result.ExtremumPoint, result.ExtremumValue));

                if (minY <= 0 && maxY >= 0)
                {
                    LineSeries rootToXAxisSeries = new LineSeries
                    {
                        Color = OxyColor.FromRgb(0x4C, 0xAF, 0x50), 
                        StrokeThickness = 1,
                        LineStyle = LineStyle.Dash,
                        Title = null
                    };
                    rootToXAxisSeries.Points.Add(new DataPoint(result.ExtremumPoint, result.ExtremumValue));
                    rootToXAxisSeries.Points.Add(new DataPoint(result.ExtremumPoint, 0));
                    PlotModel.Series.Add(rootToXAxisSeries);
                }

                if (visibleA <= 0 && visibleB >= 0)
                {
                    LineSeries rootToYAxisSeries = new LineSeries
                    {
                        Color = OxyColor.FromRgb(0x4C, 0xAF, 0x50), 
                        StrokeThickness = 1,
                        LineStyle = LineStyle.Dash,
                        Title = null
                    };
                    rootToYAxisSeries.Points.Add(new DataPoint(result.ExtremumPoint, result.ExtremumValue));
                    rootToYAxisSeries.Points.Add(new DataPoint(0, result.ExtremumValue));
                    PlotModel.Series.Add(rootToYAxisSeries);
                }

                double epsilon = double.Parse(txtEpsilon.Text.Replace(",", "."), CultureInfo.InvariantCulture);
                int decimalPlaces = PrecisionFormatConverter.GetDecimalPlacesFromEpsilon(epsilon);

                var rootAnnotation = new TextAnnotation
                {
                    Text = $"({result.ExtremumPoint.ToString($"F{decimalPlaces}")}, {result.ExtremumValue.ToString($"F{decimalPlaces}")})",
                    TextPosition = new DataPoint(result.ExtremumPoint + xPadding * 0.05, result.ExtremumValue + yPadding * 0.05),
                    TextColor = OxyColor.FromRgb(0x4C, 0xAF, 0x50),
                    FontSize = 10,
                    Background = OxyColor.FromArgb(200, 255, 255, 255)
                };
                PlotModel.Annotations.Add(rootAnnotation);

                PlotModel.Series.Add(rootSeries);
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

                var originAnnotation = new TextAnnotation
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
            txtA.Text = "-2";
            txtB.Text = "2";
            txtEpsilon.Text = "0,001";
            txtFunction.Text = "x^2";
            cmbExtremumType.SelectedIndex = 0;

            lblResult.Text = "Результаты:";
            lblRootResult.Text = "";

            PlotModel.Series.Clear();
            PlotModel.Annotations.Clear();
            PlotModel.InvalidatePlot(true);

            _calculationPerformed = false;
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
                return "x";

            string result = function.Trim();

            result = result.Replace(",", ".");

            result = Regex.Replace(result, @"e\s*\^\s*", "exp(", RegexOptions.IgnoreCase);

            if (result.Contains("exp(") && !result.Contains("exp()"))
            {
                int expIndex = result.IndexOf("exp(", StringComparison.OrdinalIgnoreCase);
                if (expIndex >= 0)
                {
                    int balance = 0;
                    for (int i = expIndex + 4; i < result.Length; i++)
                    {
                        if (result[i] == '(') balance++;
                        if (result[i] == ')')
                        {
                            if (balance == 0)
                            {
                                break;
                            }
                            balance--;
                        }

                        if (i == result.Length - 1 && balance >= 0)
                        {
                            result += ")";
                        }
                    }
                }
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
1. Квадратичная: x^2 - 4*x + 4
2. Тригонометрическая: sin(x) + cos(2*x)
3. Экспоненциальная: (27-18*x+2*x^2)*exp(-x/3)
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
• Функция должна быть определена на интервале";

            MessageBox.Show(helpText, "Справка по функциям", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            cmbExtremumType.SelectedIndex = 0;
        }
    }
}