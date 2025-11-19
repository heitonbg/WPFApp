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

        public BisectionMethodWindow()
        {
            InitializeComponent();

            // Инициализация модели графика
            PlotModel = new PlotModel
            {
                Title = "График функции и поиск корней",
                TitleColor = OxyColor.FromRgb(0x2C, 0x5F, 0x9E),
                TextColor = OxyColor.FromRgb(0x2C, 0x5F, 0x9E),
                PlotAreaBorderColor = OxyColor.FromRgb(0x2C, 0x5F, 0x9E)
            };

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

            _functionPoints = new List<OxyPlot.DataPoint>();
            _rootPoints = new List<OxyPlot.DataPoint>();

            _stepByStepIterations = new List<BisectionIteration>();
            _currentStepIndex = -1;

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

                if (function.Contains("^") || function.Contains("**"))
                {
                    MessageBox.Show("Пожалуйста, используйте функцию pow(x,y) вместо операторов ^ или **.\n\nПример: x^2 -> pow(x,2)",
                                  "Неподдерживаемый оператор",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Warning);
                    return;
                }

                _dihotomyMethod = new DihotomyMethod(function);

                // Проверка функции на интервале
                bool functionValid = _dihotomyMethod.TestFunctionOnInterval(a, b);
                if (!functionValid)
                {
                    MessageBox.Show("Функция не определена или имеет разрывы на заданном интервале.\nПопробуйте изменить интервал [a, b].",
                                  "Ошибка функции", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                List<double> roots;
                try
                {
                    roots = _dihotomyMethod.FindRoots(a, b, epsilon);
                }
                catch (InvalidOperationException ex) when (ex.Message.Contains("одинаковый знак"))
                {
                    // Обработка случая с одинаковыми знаками
                    lblResult.Text = "Корни не найдены";
                    lblFunctionValue.Text = "Функция имеет одинаковый знак на концах интервала";

                    // Показываем график даже если корней нет
                    PlotGraphWithRoots(a, b, new List<double>(), _dihotomyMethod);

                    MessageBox.Show(ex.Message + "\n\nРекомендации:\n" +
                                  "- Измените интервал [a, b]\n" +
                                  "- Убедитесь, что функция пересекает ось X на этом интервале\n" +
                                  "- Проверьте правильность ввода функции",
                                  "Корни не найдены", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                if (roots.Count == 0)
                {
                    lblResult.Text = "Корни не найдены на заданном интервале";
                    lblFunctionValue.Text = "Функция не пересекает ось X";

                    MessageBox.Show("Корни не найдены на заданном интервале.\n\nВозможные причины:\n" +
                                  "- Функция не меняет знак на интервале\n" +
                                  "- Функция не имеет корней на этом интервале\n" +
                                  "- Функция имеет разрывы\n" +
                                  "- Точность слишком высокая",
                                  "Корни не найдены", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    lblResult.Text = $"Найдено корней: {roots.Count}";
                    lblFunctionValue.Text = "Значения функции в корнях:";

                    string rootsInfo = string.Join("\n", roots.Select((r, i) => $"Корень {i + 1}: x = {r:F6}, f(x) = {_dihotomyMethod.CalculateFunction(r):E2}"));

                    MessageBox.Show($"Найдено корней: {roots.Count}\n\n{rootsInfo}",
                                  "Результат поиска корней", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                PlotGraphWithRoots(a, b, roots, _dihotomyMethod);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка вычисления", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PlotGraphWithRoots(double a, double b, List<double> roots, DihotomyMethod method)
        {
            PlotModel.Series.Clear();
            _functionPoints.Clear();
            _rootPoints.Clear();

            int pointsCount = 1000;
            double step = (b - a) / pointsCount;

            // Создаем сегменты для обработки разрывов
            List<List<OxyPlot.DataPoint>> segments = new List<List<OxyPlot.DataPoint>>();
            List<OxyPlot.DataPoint> currentSegment = new List<OxyPlot.DataPoint>();

            for (int i = 0; i <= pointsCount; i++)
            {
                double x = a + i * step;
                try
                {
                    double y = method.CalculateFunction(x);

                    // Проверяем на разрыв
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

            // Добавляем последний сегмент
            if (currentSegment.Count > 1)
            {
                segments.Add(new List<OxyPlot.DataPoint>(currentSegment));
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

            // Добавляем найденные корни
            if (roots.Any())
            {
                ScatterSeries rootsSeries = new ScatterSeries
                {
                    Title = "Корни",
                    MarkerType = MarkerType.Circle,
                    MarkerSize = 8,
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
                    }
                    catch
                    {
                        // Игнорируем корни с ошибками вычисления
                    }
                }

                PlotModel.Series.Add(rootsSeries);
            }

            // Добавляем ось X для визуализации корней
            LineSeries xAxisSeries = new LineSeries
            {
                Color = OxyColor.FromRgb(0x80, 0x80, 0x80),
                StrokeThickness = 1,
                LineStyle = LineStyle.Dash,
                Title = "y = 0"
            };
            xAxisSeries.Points.Add(new OxyPlot.DataPoint(a, 0));
            xAxisSeries.Points.Add(new OxyPlot.DataPoint(b, 0));
            PlotModel.Series.Add(xAxisSeries);

            // Добавляем информацию о знаках на концах интервала
            try
            {
                double fa = method.CalculateFunction(a);
                double fb = method.CalculateFunction(b);

                string signA = fa >= 0 ? "+" : "-";
                string signB = fb >= 0 ? "+" : "-";

                PlotModel.Subtitle = $"f(a) = {fa:E2} ({signA}), f(b) = {fb:E2} ({signB})";
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
            txtA.Text = "-1";
            txtB.Text = "2";
            txtEpsilon.Text = "0,0001";
            txtFunction.Text = "sin(x)";
            lblResult.Text = "Результат: ";
            lblFunctionValue.Text = "f(x) = ";

            PlotModel.Series.Clear();
            PlotModel.InvalidatePlot(true);

            _stepByStepIterations.Clear();
            _currentStepIndex = -1;
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

            string result = function;

            // заменяем запятые в функциях на специальные маркеры
            result = Regex.Replace(result, @"pow\(([^,]+),([^)]+)\)", "pow($1|SEPARATOR|$2)");
            result = Regex.Replace(result, @"log\(([^,]+),([^)]+)\)", "log($1|SEPARATOR|$2)");

            result = result.Replace(",", ".");

            // возвращаем запятые в функциях обратно
            result = result.Replace("|SEPARATOR|", ",");

            return result;
        }

        private void UpdateStepControls()
        {
            // Реализация для пошагового режима (если потребуется)
        }
    }

    // Класс для хранения информации о итерациях (для будущего расширения)
    public class BisectionIteration
    {
        public int Iteration { get; set; }
        public double A { get; set; }
        public double B { get; set; }
        public double Mid { get; set; }
        public double Fmid { get; set; }
    }
}