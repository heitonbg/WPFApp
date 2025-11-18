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
    public partial class NewtonMethodWindow : Window
    {
        public PlotModel PlotModel { get; set; }
        private List<DataPoint> _functionPoints;
        private List<DataPoint> _minimumPoints;
        private List<DataPoint> _iterationPoints;

        private NewtonMethod _newtonMethod;
        private List<NewtonIteration> _stepByStepIterations;
        private int _currentStepIndex;

        public NewtonMethodWindow()
        {
            InitializeComponent();

            // Инициализация модели графика
            PlotModel = new PlotModel
            {
                Title = "График функции и поиск минимума",
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

            _functionPoints = new List<DataPoint>();
            _minimumPoints = new List<DataPoint>();
            _iterationPoints = new List<DataPoint>();

            _stepByStepIterations = new List<NewtonIteration>();
            _currentStepIndex = -1;

            DataContext = this;
            UpdateStepControls();
        }

        private void Calculate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!ValidateInput())
                    return;

                double x0 = double.Parse(txtX0.Text.Replace(",", "."), CultureInfo.InvariantCulture);
                double epsilon = double.Parse(txtEpsilon.Text.Replace(",", "."), CultureInfo.InvariantCulture);
                int maxIterations = int.Parse(txtMaxIterations.Text);
                string function = txtFunction.Text;
                double a = double.Parse(txtA.Text.Replace(",", "."), CultureInfo.InvariantCulture);
                double b = double.Parse(txtB.Text.Replace(",", "."), CultureInfo.InvariantCulture);

                function = PreprocessFunction(function);

                if (function.Contains("^") || function.Contains("**"))
                {
                    MessageBox.Show("Пожалуйста, используйте функцию pow(x,y) вместо операторов ^ или **.\n\nПример: x^2 -> pow(x,2)",
                                  "Неподдерживаемый оператор",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Warning);
                    return;
                }

                bool functionValid = TestFunctionOnInterval(function, a, b);
                if (!functionValid)
                {
                    MessageBox.Show("Функция не определена или имеет разрывы на заданном интервале.\nПопробуйте изменить интервал [a, b].",
                                  "Ошибка функции", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                _newtonMethod = new NewtonMethod(function);

                NewtonResult result = _newtonMethod.FindMinimum(x0, epsilon, maxIterations, a, b, true);

                if (result == null)
                {
                    MessageBox.Show("Не удалось найти минимум методом Ньютона. Попробуйте изменить начальную точку.",
                                  "Ошибка поиска",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Warning);
                    return;
                }

                if (result.IsMinimum)
                {
                    lblResult.Text = $"Точка минимума: x = {result.MinimumPoint:F6}";
                    lblIterations.Text = $"Количество итераций: {result.Iterations}";
                    lblFunctionValue.Text = $"Значение функции: f(x) = {result.MinimumValue:F6}";
                    lblDerivative.Text = $"Производные: f'(x) = {result.FinalDerivative:E2}, f''(x) = {result.FinalSecondDerivative:E2}";

                    MessageBox.Show($"Минимум успешно найден!\n\n" +
                                  $"x = {result.MinimumPoint:F6}\n" +
                                  $"f(x) = {result.MinimumValue:F6}\n" +
                                  $"Итераций: {result.Iterations}\n" +
                                  $"{result.ConvergenceMessage}",
                                  "Результат", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else if (result.Converged)
                {
                    lblResult.Text = $"Найдена критическая точка: x = {result.MinimumPoint:F6} (не минимум)";
                    lblIterations.Text = $"Количество итераций: {result.Iterations}";
                    lblFunctionValue.Text = $"Значение функции: f(x) = {result.MinimumValue:F6}";
                    lblDerivative.Text = $"Производные: f'(x) = {result.FinalDerivative:E2}, f''(x) = {result.FinalSecondDerivative:E2}";

                    MessageBox.Show("Метод нашел критическую точку, но это не минимум! f''(x) ≤ 0\n\n" +
                                  $"Точка: x = {result.MinimumPoint:F6}\n" +
                                  $"f(x) = {result.MinimumValue:F6}\n" +
                                  $"f''(x) = {result.FinalSecondDerivative:E2}\n\n" +
                                  "Попробуйте другую начальную точку.",
                                  "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else
                {
                    lblResult.Text = $"Последняя точка: x = {result.MinimumPoint:F6} (не сошлось)";
                    lblIterations.Text = $"Количество итераций: {result.Iterations}";
                    lblFunctionValue.Text = $"Значение функции: f(x) = {result.MinimumValue:F6}";
                    lblDerivative.Text = $"Производные: f'(x) = {result.FinalDerivative:E2}, f''(x) = {result.FinalSecondDerivative:E2}";

                    MessageBox.Show("Метод не сошелся к минимуму за указанное количество итераций.\n\n" +
                                  $"Текущая точка: x = {result.MinimumPoint:F6}\n" +
                                  $"f(x) = {result.MinimumValue:F6}\n\n" +
                                  "Попробуйте:\n" +
                                  "- Увеличить максимальное количество итераций\n" +
                                  "- Изменить начальную точку\n" +
                                  "- Проверить корректность функции\n" +
                                  "- Использовать автоподбор начальной точки",
                                  "Сходимость не достигнута", MessageBoxButton.OK, MessageBoxImage.Warning);
                }

                _stepByStepIterations = result.StepByStepIterations;
                _currentStepIndex = _stepByStepIterations.Count - 1;

                PlotGraphWithMinimum(a, b, result);
                UpdateStepControls();
                UpdateStepsList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка вычисления", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool TestFunctionOnInterval(string function, double a, double b)
        {
            try
            {
                var testMethod = new NewtonMethod(function);
                int testPoints = 10;
                double step = (b - a) / testPoints;
                int validPoints = 0;

                for (int i = 0; i <= testPoints; i++)
                {
                    double x = a + i * step;
                    double value = testMethod.CalculateFunction(x);
                    if (value < double.MaxValue - 1 && !double.IsNaN(value) && !double.IsInfinity(value))
                        validPoints++;
                }

                return validPoints >= testPoints * 0.7;
            }
            catch
            {
                return false;
            }
        }

        private void StepByStep_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!ValidateInput())
                    return;

                double x0 = double.Parse(txtX0.Text.Replace(",", "."), CultureInfo.InvariantCulture);
                double epsilon = double.Parse(txtEpsilon.Text.Replace(",", "."), CultureInfo.InvariantCulture);
                int maxIterations = int.Parse(txtMaxIterations.Text);
                string function = txtFunction.Text;
                double a = double.Parse(txtA.Text.Replace(",", "."), CultureInfo.InvariantCulture);
                double b = double.Parse(txtB.Text.Replace(",", "."), CultureInfo.InvariantCulture);

                function = PreprocessFunction(function);

                if (function.Contains("^") || function.Contains("**"))
                {
                    MessageBox.Show("Пожалуйста, используйте функцию pow(x,y) вместо операторов ^ или **.",
                                  "Неподдерживаемый оператор",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Warning);
                    return;
                }

                _newtonMethod = new NewtonMethod(function);

                NewtonResult result = _newtonMethod.FindMinimum(x0, epsilon, maxIterations, a, b, true);

                if (result == null || !result.StepByStepIterations.Any())
                {
                    MessageBox.Show("Не удалось выполнить пошаговый просмотр.",
                                  "Ошибка",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Warning);
                    return;
                }

                _stepByStepIterations = result.StepByStepIterations;
                _currentStepIndex = 0;

                PlotInitialGraph(a, b);
                ShowCurrentStep();
                UpdateStepControls();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка вычисления", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PrevStep_Click(object sender, RoutedEventArgs e)
        {
            if (_currentStepIndex > 0)
            {
                _currentStepIndex--;
                ShowCurrentStep();
                UpdateStepControls();
            }
        }

        private void NextStep_Click(object sender, RoutedEventArgs e)
        {
            if (_currentStepIndex < _stepByStepIterations.Count - 1)
            {
                _currentStepIndex++;
                ShowCurrentStep();
                UpdateStepControls();
            }
        }

        private void ShowCurrentStep()
        {
            if (_currentStepIndex < 0 || _currentStepIndex >= _stepByStepIterations.Count)
                return;

            var iteration = _stepByStepIterations[_currentStepIndex];

            lblResult.Text = $"Текущая точка: x = {iteration.X:F6}";
            lblIterations.Text = $"Итерация: {iteration.Iteration + 1}";
            lblFunctionValue.Text = $"f(x) = {iteration.FunctionValue:F6}";
            lblDerivative.Text = $"f'(x) = {iteration.FirstDerivative:E2}, f''(x) = {iteration.SecondDerivative:E2}";

            UpdateStepsList();
            PlotCurrentStep();
        }

        private void PlotCurrentStep()
        {
            if (_currentStepIndex < 0 || _currentStepIndex >= _stepByStepIterations.Count)
                return;

            var iteration = _stepByStepIterations[_currentStepIndex];

            // Очищаем предыдущие точки итераций
            var existingIterationSeries = PlotModel.Series.OfType<ScatterSeries>()
                .FirstOrDefault(s => s.Title == "Текущая итерация");
            if (existingIterationSeries != null)
            {
                PlotModel.Series.Remove(existingIterationSeries);
            }

            // Добавляем текущую точку итерации
            var iterationSeries = new ScatterSeries
            {
                Title = "Текущая итерация",
                MarkerType = MarkerType.Triangle,
                MarkerSize = 8,
                MarkerFill = OxyColor.FromRgb(0x32, 0xCD, 0x32),
                MarkerStroke = OxyColor.FromRgb(0x2C, 0x5F, 0x9E),
                MarkerStrokeThickness = 2
            };
            iterationSeries.Points.Add(new ScatterPoint(iteration.X, iteration.FunctionValue));
            PlotModel.Series.Add(iterationSeries);

            PlotModel.InvalidatePlot(true);
        }

        private void UpdateStepsList()
        {
            lstSteps.Items.Clear();
            for (int i = 0; i <= _currentStepIndex; i++)
            {
                var step = _stepByStepIterations[i];
                string stepType = "шаг";
                if (i == _stepByStepIterations.Count - 1 && Math.Abs(step.FirstDerivative) < double.Parse(txtEpsilon.Text.Replace(",", "."), CultureInfo.InvariantCulture) && step.SecondDerivative > 0)
                    stepType = "МИНИМУМ";
                else if (Math.Abs(step.FirstDerivative) < double.Parse(txtEpsilon.Text.Replace(",", "."), CultureInfo.InvariantCulture) && step.SecondDerivative > 0)
                    stepType = "минимум";
                else if (Math.Abs(step.FirstDerivative) < double.Parse(txtEpsilon.Text.Replace(",", "."), CultureInfo.InvariantCulture) && step.SecondDerivative < 0)
                    stepType = "максимум";

                lstSteps.Items.Add($"{stepType} {i + 1}: x = {step.X:F4}, f(x) = {step.FunctionValue:F4}, f' = {step.FirstDerivative:E2}");
            }

            if (lstSteps.Items.Count > 0)
                lstSteps.ScrollIntoView(lstSteps.Items[lstSteps.Items.Count - 1]);
        }

        private void UpdateStepControls()
        {
            btnPrevStep.IsEnabled = _currentStepIndex > 0;
            btnNextStep.IsEnabled = _currentStepIndex < _stepByStepIterations.Count - 1;
            lblStepInfo.Text = _stepByStepIterations.Any()
                ? $"Пошаговый просмотр: {_currentStepIndex + 1} из {_stepByStepIterations.Count}"
                : "Пошаговый просмотр:";
        }

        private void PlotGraphWithMinimum(double a, double b, NewtonResult result)
        {
            PlotModel.Series.Clear();
            _functionPoints.Clear();
            _minimumPoints.Clear();
            _iterationPoints.Clear();

            int pointsCount = 1000;
            double step = (b - a) / pointsCount;

            // Создаем сегменты для обработки разрывов
            List<List<DataPoint>> segments = new List<List<DataPoint>>();
            List<DataPoint> currentSegment = new List<DataPoint>();

            for (int i = 0; i <= pointsCount; i++)
            {
                double x = a + i * step;
                try
                {
                    double y = _newtonMethod.CalculateFunction(x);

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

            // Добавляем точку минимума
            if (result.IsMinimum)
            {
                ScatterSeries minimumSeries = new ScatterSeries
                {
                    Title = "Минимум",
                    MarkerType = MarkerType.Circle,
                    MarkerSize = 8,
                    MarkerFill = OxyColor.FromRgb(0xFF, 0x6B, 0x8E),
                    MarkerStroke = OxyColor.FromRgb(0x2C, 0x5F, 0x9E),
                    MarkerStrokeThickness = 2
                };
                minimumSeries.Points.Add(new ScatterPoint(result.MinimumPoint, result.MinimumValue));
                PlotModel.Series.Add(minimumSeries);
            }

            // Добавляем точки итераций
            ScatterSeries iterationsSeries = new ScatterSeries
            {
                Title = "Итерации",
                MarkerType = MarkerType.Triangle,
                MarkerSize = 6,
                MarkerFill = OxyColor.FromRgb(0x32, 0xCD, 0x32),
                MarkerStroke = OxyColor.FromRgb(0x2C, 0x5F, 0x9E),
                MarkerStrokeThickness = 1
            };

            foreach (var iteration in result.StepByStepIterations)
            {
                iterationsSeries.Points.Add(new ScatterPoint(iteration.X, iteration.FunctionValue));
            }

            PlotModel.Series.Add(iterationsSeries);
            PlotModel.InvalidatePlot(true);
        }

        private void PlotInitialGraph(double a, double b)
        {
            PlotModel.Series.Clear();
            _functionPoints.Clear();
            _minimumPoints.Clear();
            _iterationPoints.Clear();

            int pointsCount = 1000;
            double step = (b - a) / pointsCount;

            // Создаем сегменты для обработки разрывов
            List<List<DataPoint>> segments = new List<List<DataPoint>>();
            List<DataPoint> currentSegment = new List<DataPoint>();

            for (int i = 0; i <= pointsCount; i++)
            {
                double x = a + i * step;
                try
                {
                    double y = _newtonMethod.CalculateFunction(x);

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

            PlotModel.InvalidatePlot(true);
        }

        private bool ValidateInput()
        {
            if (string.IsNullOrWhiteSpace(txtX0.Text) || string.IsNullOrWhiteSpace(txtEpsilon.Text) ||
                string.IsNullOrWhiteSpace(txtMaxIterations.Text) || string.IsNullOrWhiteSpace(txtFunction.Text) ||
                string.IsNullOrWhiteSpace(txtA.Text) || string.IsNullOrWhiteSpace(txtB.Text))
            {
                MessageBox.Show("Все поля должны быть заполнены!", "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (!double.TryParse(txtX0.Text.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out double x0) ||
                !double.TryParse(txtEpsilon.Text.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out double epsilon) ||
                !double.TryParse(txtA.Text.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out double a) ||
                !double.TryParse(txtB.Text.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out double b))
            {
                MessageBox.Show("Параметры x0, epsilon, a и b должны быть числами!", "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (!int.TryParse(txtMaxIterations.Text, out int maxIterations) || maxIterations <= 0)
            {
                MessageBox.Show("Максимальное количество итераций должно быть положительным целым числом!", "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (epsilon <= 0)
            {
                MessageBox.Show("Точность epsilon должна быть положительным числом!", "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (a >= b)
            {
                MessageBox.Show("Начало интервала a должно быть меньше конца b!", "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (x0 < a || x0 > b)
            {
                MessageBox.Show("Начальная точка x0 должна находиться в интервале [a, b]!", "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        private void AutoFindStartingPoint_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!ValidateInput()) return;

                double a = double.Parse(txtA.Text.Replace(",", "."), CultureInfo.InvariantCulture);
                double b = double.Parse(txtB.Text.Replace(",", "."), CultureInfo.InvariantCulture);
                string function = PreprocessFunction(txtFunction.Text);

                _newtonMethod = new NewtonMethod(function);
                double goodStart = _newtonMethod.FindGoodStartingPoint(a, b);

                txtX0.Text = goodStart.ToString("F3");
                MessageBox.Show($"Автоматически подобрана начальная точка: x0 = {goodStart:F3}",
                              "Начальная точка", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            txtX0.Text = "1";
            txtEpsilon.Text = "0,001";
            txtMaxIterations.Text = "100";
            txtFunction.Text = "x^2";
            txtA.Text = "-2";
            txtB.Text = "2";

            lblResult.Text = "Точка минимума: ";
            lblIterations.Text = "Количество итераций: ";
            lblFunctionValue.Text = "Значение функции: ";
            lblDerivative.Text = "Производная в точке: ";

            PlotModel.Series.Clear();
            PlotModel.InvalidatePlot(true);

            _stepByStepIterations.Clear();
            _currentStepIndex = -1;
            lstSteps.Items.Clear();
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

            result = Regex.Replace(result, @"(\d),(\d)", "$1.$2");

            result = Regex.Replace(result, @"([a-zA-Z0-9.]+)\^([a-zA-Z0-9.]+)", "pow($1,$2)");

            return result;
        }
    }
}