using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using OxyPlot;
using OxyPlot.Series;
using OxyPlot.Axes;
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

        public GoldenRatioWindow()
        {
            InitializeComponent();
            PlotModel = new PlotModel
            {
                Title = "График функции",
                TitleColor = OxyColor.FromRgb(0x2C, 0x5F, 0x9E),
                TextColor = OxyColor.FromRgb(0x2C, 0x5F, 0x9E),
                PlotAreaBorderColor = OxyColor.FromRgb(0x2C, 0x5F, 0x9E),
                Background = OxyColors.White
            };
            _functionPoints = new List<DataPoint>();
            _extremumPoints = new List<DataPoint>();
            _rootPoints = new List<DataPoint>();

            // Настройка осей
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

            // Добавляем линии осей координат
            AddAxisLines();

            DataContext = this;
            this.Closing += Window_Closing;
        }

        public GoldenRatioWindow(MainWindow mainWindow) : this()
        {
            _mainWindow = mainWindow;
        }

        private void AddAxisLines()
        {
            // Линия оси X (y = 0)
            var xAxisLine = new LineSeries
            {
                Title = "Ось X (y = 0)",
                Color = OxyColor.FromArgb(128, 0x2C, 0x5F, 0x9E),
                StrokeThickness = 1,
                LineStyle = LineStyle.Dash
            };

            // Линия оси Y (x = 0)
            var yAxisLine = new LineSeries
            {
                Title = "Ось Y (x = 0)",
                Color = OxyColor.FromArgb(128, 0x2C, 0x5F, 0x9E),
                StrokeThickness = 1,
                LineStyle = LineStyle.Dash
            };

            // Эти линии будут добавляться динамически при построении графика
            PlotModel.Series.Add(xAxisLine);
            PlotModel.Series.Add(yAxisLine);
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

                // Для отладки
                Console.WriteLine($"=== НОВЫЙ РАСЧЕТ ===");
                Console.WriteLine($"Исходная функция: {function}");

                // Предобработка функции - пользователь может вводить e^x или exp(x)
                function = PreprocessFunction(function);

                Console.WriteLine($"После предобработки: {function}");
                Console.WriteLine($"Интервал: [{a}, {b}]");
                Console.WriteLine($"Точность: {epsilon}");

                // Создаем метод
                GoldenRatioMethod method = new GoldenRatioMethod(function);

                // Тестируем функцию в нескольких точках
                Console.WriteLine($"Тест функции:");
                try
                {
                    double testX = (a + b) / 2;
                    double testY = method.CalculateFunction(testX);
                    Console.WriteLine($"  f({testX:F3}) = {testY:F6}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"  Тест не удался: {ex.Message}");
                }

                // Ищем экстремум
                GoldenRatioResult result = method.FindGlobalExtremum(a, b, epsilon, _findMinimum);

                // Формируем результат
                int decimalPlaces = CalculateDecimalPlaces(epsilon);
                string extremumType = _findMinimum ? "минимума" : "максимума";

                lblResult.Text = $"Точка {extremumType}: x = {result.ExtremumPoint.ToString($"F{decimalPlaces}")}\n" +
                               $"Значение функции: f(x) = {result.ExtremumValue.ToString($"F{decimalPlaces}")}\n" +
                               $"Количество итераций: {result.Iterations}\n" +
                               $"Финальный интервал: [{result.FinalInterval.a.ToString($"F{decimalPlaces}")}, " +
                               $"{result.FinalInterval.b.ToString($"F{decimalPlaces}")}]";

                lblRootResult.Text = "";

                // Строим график
                PlotGraphWithExtremum(a, b, result, method);

                Console.WriteLine($"=== РАСЧЕТ ЗАВЕРШЕН ===");

            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}\n\n" +
                               $"Совет: Попробуйте ввести функцию в одном из следующих форматов:\n" +
                               $"1. (27-18*x+2*x^2)*exp(-x/3)\n" +
                               $"2. (27-18*x+2*x^2)*e^(-x/3)\n" +
                               $"3. sin(x)*exp(-x^2)",
                    "Ошибка вычисления", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private int CalculateDecimalPlaces(double epsilon)
        {
            if (epsilon <= 0) return 3;

            // Определяем количество знаков после запятой на основе точности
            string epsilonStr = epsilon.ToString(CultureInfo.InvariantCulture);

            if (epsilonStr.Contains('.'))
            {
                int decimalPlaces = epsilonStr.Split('.')[1].Length;
                return Math.Min(decimalPlaces, 15); // Ограничиваем максимальное количество знаков
            }

            // Если epsilon целое число, используем 3 знака по умолчанию
            return 3;
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

                // НЕ форматируем точность - оставляем как ввел пользователь

                function = PreprocessFunction(function);

                GoldenRatioMethod method = new GoldenRatioMethod(function);

                try
                {
                    GoldenRatioResult result = method.FindRoot(a, b, epsilon);

                    // Определяем количество знаков после запятой для форматирования
                    int decimalPlaces = CalculateDecimalPlaces(epsilon);

                    lblResult.Text = $"Корень уравнения f(x) = 0:\n" +
                                   $"x = {result.ExtremumPoint.ToString($"F{decimalPlaces}")}\n" +
                                   $"f(x) = {result.ExtremumValue.ToString($"F{decimalPlaces}")}\n" +
                                   $"Количество итераций: {result.Iterations}\n" +
                                   $"Финальный интервал: [{result.FinalInterval.a.ToString($"F{decimalPlaces}")}, {result.FinalInterval.b.ToString($"F{decimalPlaces}")}]";

                    PlotGraphWithRoot(a, b, result, method);
                }
                catch (ArgumentException ex)
                {
                    if (ex.Message.Contains("не меняет знак"))
                    {
                        MessageBox.Show("Функция не меняет знак на заданном интервале [a, b].\n" +
                                      "Попробуйте другой интервал или проверьте функцию.",
                                      "Корень не найден", MessageBoxButton.OK, MessageBoxImage.Warning);
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
            AddAxisLines(); // Добавляем линии осей заново
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

            // Находим серию оси X (y = 0) и добавляем точки
            var xAxisSeries = PlotModel.Series.FirstOrDefault(s => s.Title == "Ось X (y = 0)") as LineSeries;
            if (xAxisSeries != null)
            {
                xAxisSeries.Points.Clear();
                xAxisSeries.Points.Add(new DataPoint(a - Math.Abs(a * 0.1), 0));
                xAxisSeries.Points.Add(new DataPoint(b + Math.Abs(b * 0.1), 0));
            }

            // Добавляем линию оси Y (x = 0) если она попадает в интервал
            var yAxisSeries = PlotModel.Series.FirstOrDefault(s => s.Title == "Ось Y (x = 0)") as LineSeries;
            if (yAxisSeries != null)
            {
                yAxisSeries.Points.Clear();
                // Находим минимальное и максимальное значение функции
                double minY = double.MaxValue;
                double maxY = double.MinValue;
                foreach (var segment in segments)
                {
                    foreach (var point in segment)
                    {
                        if (point.Y < minY) minY = point.Y;
                        if (point.Y > maxY) maxY = point.Y;
                    }
                }
                yAxisSeries.Points.Add(new DataPoint(0, minY - Math.Abs(minY * 0.1)));
                yAxisSeries.Points.Add(new DataPoint(0, maxY + Math.Abs(maxY * 0.1)));
            }

            PlotModel.InvalidatePlot(true);
        }

        private void PlotGraphWithRoot(double a, double b, GoldenRatioResult result, GoldenRatioMethod method)
        {
            PlotModel.Series.Clear();
            AddAxisLines(); // Добавляем линии осей заново
            _functionPoints.Clear();
            _rootPoints.Clear();

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
                    Title = segmentNumber == 0 ? "Функция" : null
                };

                foreach (var point in segment)
                {
                    segmentSeries.Points.Add(point);
                }

                PlotModel.Series.Add(segmentSeries);
                segmentNumber++;
            }

            // Добавляем точку корня
            ScatterSeries rootSeries = new ScatterSeries
            {
                Title = "Корень",
                MarkerType = MarkerType.Circle,
                MarkerSize = 8,
                MarkerFill = OxyColor.FromRgb(0x4C, 0xAF, 0x50),
                MarkerStroke = OxyColor.FromRgb(0x2C, 0x5F, 0x9E),
                MarkerStrokeThickness = 2
            };
            rootSeries.Points.Add(new ScatterPoint(result.ExtremumPoint, result.ExtremumValue));
            PlotModel.Series.Add(rootSeries);

            // Добавляем горизонтальную линию y = 0
            var zeroLineSeries = new LineSeries
            {
                Title = "y = 0",
                Color = OxyColor.FromArgb(128, 0x2C, 0x5F, 0x9E),
                StrokeThickness = 1,
                LineStyle = LineStyle.Dash
            };
            zeroLineSeries.Points.Add(new DataPoint(a - Math.Abs(a * 0.1), 0));
            zeroLineSeries.Points.Add(new DataPoint(b + Math.Abs(b * 0.1), 0));
            PlotModel.Series.Add(zeroLineSeries);

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
                return "x";

            string result = function.Trim();

            // 1. Заменяем запятые на точки для десятичных чисел
            result = result.Replace(",", ".");

            // 2. Заменяем e^ на exp (пользовательский ввод)
            result = Regex.Replace(result, @"e\s*\^\s*", "exp(", RegexOptions.IgnoreCase);

            // 3. Если мы добавили exp(, нужно закрыть скобку
            if (result.Contains("exp(") && !result.Contains("exp()"))
            {
                // Находим позицию начала exp
                int expIndex = result.IndexOf("exp(", StringComparison.OrdinalIgnoreCase);
                if (expIndex >= 0)
                {
                    // Ищем закрывающую скобку для этого exp
                    int balance = 0;
                    for (int i = expIndex + 4; i < result.Length; i++)
                    {
                        if (result[i] == '(') balance++;
                        if (result[i] == ')')
                        {
                            if (balance == 0)
                            {
                                // Нашли закрывающую скобку
                                break;
                            }
                            balance--;
                        }

                        // Если дошли до конца и нет закрывающей скобки
                        if (i == result.Length - 1 && balance >= 0)
                        {
                            result += ")";
                        }
                    }
                }
            }

            return result;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            cmbExtremumType.SelectedIndex = 0;
        }
    }
}
