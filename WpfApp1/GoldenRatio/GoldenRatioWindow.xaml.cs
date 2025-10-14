using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using LiveCharts;
using LiveCharts.Wpf;
using LiveCharts.Defaults;

namespace WpfApp1
{
    public partial class GoldenRatioWindow : Window
    {
        public SeriesCollection SeriesCollection { get; set; }
        public ChartValues<ObservablePoint> FunctionValues { get; set; }
        public ChartValues<ObservablePoint> MinimumPoints { get; set; }

        private MainWindow _mainWindow;

        public GoldenRatioWindow()
        {
            InitializeComponent();
            DataContext = this;

            FunctionValues = new ChartValues<ObservablePoint>();
            MinimumPoints = new ChartValues<ObservablePoint>();

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

                function = PreprocessFunction(function);

                if (function.ToLower().Contains("sin") || function.ToLower().Contains("cos"))
                {
                    MessageBox.Show("При больших интервалах на графике может быть несколько минимумов, выведется только один минимум, если вам необходим определенный минимум попробуйте уменьшить интервал",
                        "Попробуйте уменьшить интервал", MessageBoxButton.OK, MessageBoxImage.Warning);
                }

                if (function.ToLower().Contains("tan"))
                {
                    MessageBox.Show("У тангенса происходит разрыв графика, график будет изображен неправильно",
                        "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                }

                if (function.ToLower().Contains("/x"))
                {
                    MessageBox.Show("Происходит разрыва графика при */x, график отображается некорректно",
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

                GoldenRatioResult result = method.FindMinimum(a, b, epsilon);

                lblResult.Text = $"Точка минимума: x = {result.MinimumPoint:F6}";
                lblIterations.Text = $"Количество итераций: {result.Iterations}";

                PlotGraphWithMinimum(a, b, result, method);

            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка вычисления", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PlotGraphWithMinimum(double a, double b, GoldenRatioResult result, GoldenRatioMethod method)
        {
            FunctionValues.Clear();
            MinimumPoints.Clear();

            int pointsCount = 300;
            double step = (b - a) / pointsCount;

            for (double x = a; x <= b; x += step)
            {
                try
                {
                    double y = method.CalculateFunction(x);
                    FunctionValues.Add(new ObservablePoint(x, y));
                }
                catch
                {
                }
            }

            try
            {
                MinimumPoints.Add(new ObservablePoint(result.MinimumPoint, result.MinimumValue));
            }
            catch
            {
            }
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
            lblResult.Text = "Точка минимума: ";
            lblIterations.Text = "Количество итераций: ";
            FunctionValues.Clear();
            MinimumPoints.Clear();
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (chart != null)
            {
                chart.Series.Clear();
                chart = null;
            }

            FunctionValues?.Clear();
            MinimumPoints?.Clear();
            SeriesCollection?.Clear();
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
    }
}