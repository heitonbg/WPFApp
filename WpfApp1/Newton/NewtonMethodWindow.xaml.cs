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

namespace WpfApp1
{
    public partial class NewtonMethodWindow : Window
    {
        public PlotModel PlotModel { get; set; }
        private List<DataPoint> _functionPoints;
        private List<DataPoint> _minimumPoints;
        private List<DataPoint> _iterationPoints;
        private List<TangentLine> _tangentLines;

        private NewtonMethod _newtonMethod;
        private List<NewtonIteration> _stepByStepIterations;
        private int _currentStepIndex;
        private bool _calculationPerformed = false;

        public NewtonMethodWindow()
        {
            InitializeComponent();

            PlotModel = new PlotModel
            {
                Title = "График функции и поиск минимума",
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

            _functionPoints = new List<DataPoint>();
            _minimumPoints = new List<DataPoint>();
            _iterationPoints = new List<DataPoint>();
            _tangentLines = new List<TangentLine>();

            _stepByStepIterations = new List<NewtonIteration>();
            _currentStepIndex = -1;

            DataContext = this;
            UpdateStepControls();
            ResetResultFields();
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

                int decimalPlaces = GetDecimalPlacesFromEpsilon(epsilon);

                if (result.IsMinimum)
                {
                    lblResult.Text = "Минимум найден!";
                    lblIterations.Text = $"Количество итераций: {result.Iterations}";
                    lblFunctionValue.Text = $"Значение функции: f(x) = {result.MinimumValue.ToString($"F{decimalPlaces}")}";
                    lblDerivative.Text = $"Производные: f'(x) = {result.FinalDerivative.ToString("E2")}, f''(x) = {result.FinalSecondDerivative.ToString("E2")}";

                    MessageBox.Show($"Минимум успешно найден!\n\n" +
                                  $"x = {result.MinimumPoint.ToString($"F{decimalPlaces}")}\n" +
                                  $"f(x) = {result.MinimumValue.ToString($"F{decimalPlaces}")}\n" +
                                  $"Итераций: {result.Iterations}\n" +
                                  $"{result.ConvergenceMessage}",
                                  "Результат", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else if (result.Converged)
                {
                    lblResult.Text = "Критическая точка (не минимум)";
                    lblIterations.Text = $"Количество итераций: {result.Iterations}";
                    lblFunctionValue.Text = $"Значение функции: f(x) = {result.MinimumValue.ToString($"F{decimalPlaces}")}";
                    lblDerivative.Text = $"Производные: f'(x) = {result.FinalDerivative.ToString("E2")}, f''(x) = {result.FinalSecondDerivative.ToString("E2")}";

                    MessageBox.Show("Метод нашел критическую точку, но это не минимум! f''(x) ≤ 0\n\n" +
                                  $"Точка: x = {result.MinimumPoint.ToString($"F{decimalPlaces}")}\n" +
                                  $"f(x) = {result.MinimumValue.ToString($"F{decimalPlaces}")}\n" +
                                  $"f''(x) = {result.FinalSecondDerivative.ToString("E2")}\n\n" +
                                  "Попробуйте другую начальную точку.",
                                  "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else
                {
                    lblResult.Text = "Метод не сошелся";
                    lblIterations.Text = $"Количество итераций: {result.Iterations}";
                    lblFunctionValue.Text = $"Значение функции: f(x) = {result.MinimumValue.ToString($"F{decimalPlaces}")}";
                    lblDerivative.Text = $"Производные: f'(x) = {result.FinalDerivative.ToString("E2")}, f''(x) = {result.FinalSecondDerivative.ToString("E2")}";

                    MessageBox.Show("Метод не сошелся к минимуму за указанное количество итераций.\n\n" +
                                  $"Текущая точка: x = {result.MinimumPoint.ToString($"F{decimalPlaces}")}\n" +
                                  $"f(x) = {result.MinimumValue.ToString($"F{decimalPlaces}")}\n\n" +
                                  "Попробуйте:\n" +
                                  "- Увеличить максимальное количество итераций\n" +
                                  "- Изменить начальную точку\n" +
                                  "- Проверить корректность функции\n" +
                                  "- Использовать автоподбор начальной точки",
                                  "Сходимость не достигнута", MessageBoxButton.OK, MessageBoxImage.Warning);
                }

                _stepByStepIterations = result.StepByStepIterations;
                _tangentLines = result.TangentLines;
                _currentStepIndex = _stepByStepIterations.Count - 1;

                PlotGraphWithMinimum(a, b, result);
                UpdateStepControls();
                UpdateStepsList();

                _calculationPerformed = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка вычисления", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private int GetDecimalPlacesFromEpsilon(double epsilon)
        {
            if (epsilon >= 1) return 0;
            if (epsilon >= 0.1) return 1;
            if (epsilon >= 0.01) return 2;
            if (epsilon >= 0.001) return 3;
            if (epsilon >= 0.0001) return 4;
            if (epsilon >= 0.00001) return 5;
            if (epsilon >= 0.000001) return 6;
            return 7;
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

        private void PlotGraphWithMinimum(double a, double b, NewtonResult result)
        {
            PlotModel.Series.Clear();
            PlotModel.Annotations.Clear();
            _functionPoints.Clear();
            _minimumPoints.Clear();
            _iterationPoints.Clear();

            double minY = double.MaxValue;
            double maxY = double.MinValue;

            int pointsCount = 1000;
            double step = (b - a) / pointsCount;

            List<List<DataPoint>> segments = new List<List<DataPoint>>();
            List<DataPoint> currentSegment = new List<DataPoint>();

            for (int i = 0; i <= pointsCount; i++)
            {
                double x = a + i * step;
                try
                {
                    double y = _newtonMethod.CalculateFunction(x);

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

            if (_tangentLines.Any())
            {
                foreach (var tangent in _tangentLines)
                {
                    if (tangent != null)
                    {
                        LineSeries tangentSeries = new LineSeries
                        {
                            Color = OxyColor.FromRgb(0xFF, 0x8C, 0x00), 
                            StrokeThickness = 1.5,
                            LineStyle = LineStyle.Dash,
                            Title = "Касательная"
                        };

                        double tangentY1 = tangent.GetY(visibleA);
                        double tangentY2 = tangent.GetY(visibleB);

                        tangentSeries.Points.Add(new DataPoint(visibleA, tangentY1));
                        tangentSeries.Points.Add(new DataPoint(visibleB, tangentY2));

                        PlotModel.Series.Add(tangentSeries);

                        ScatterSeries tangentPointSeries = new ScatterSeries
                        {
                            MarkerType = MarkerType.Circle,
                            MarkerSize = 5,
                            MarkerFill = OxyColor.FromRgb(0xFF, 0x8C, 0x00),
                            MarkerStroke = OxyColor.FromRgb(0x2C, 0x5F, 0x9E),
                            MarkerStrokeThickness = 1
                        };
                        tangentPointSeries.Points.Add(new ScatterPoint(tangent.PointX, tangent.PointY));
                        PlotModel.Series.Add(tangentPointSeries);
                    }
                }
            }

            if (result.IsMinimum)
            {
                ScatterSeries minimumSeries = new ScatterSeries
                {
                    Title = "Минимум",
                    MarkerType = MarkerType.Circle,
                    MarkerSize = 10,
                    MarkerFill = OxyColor.FromRgb(0xFF, 0x6B, 0x8E),
                    MarkerStroke = OxyColor.FromRgb(0x2C, 0x5F, 0x9E),
                    MarkerStrokeThickness = 2
                };
                minimumSeries.Points.Add(new ScatterPoint(result.MinimumPoint, result.MinimumValue));
                PlotModel.Series.Add(minimumSeries);

                int decimalPlaces = GetDecimalPlacesFromEpsilon(double.Parse(txtEpsilon.Text.Replace(",", "."), CultureInfo.InvariantCulture));
                var minimumAnnotation = new TextAnnotation
                {
                    Text = $"({result.MinimumPoint.ToString($"F{decimalPlaces}")}, {result.MinimumValue.ToString($"F{decimalPlaces}")})",
                    TextPosition = new DataPoint(result.MinimumPoint + xPadding * 0.05, result.MinimumValue + yPadding * 0.05),
                    TextColor = OxyColor.FromRgb(0xFF, 0x6B, 0x8E),
                    FontSize = 10,
                    Background = OxyColor.FromArgb(200, 255, 255, 255)
                };
                PlotModel.Annotations.Add(minimumAnnotation);
            }

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

            PlotModel.InvalidatePlot(true);
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
                _tangentLines = result.TangentLines;
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
            int decimalPlaces = GetDecimalPlacesFromEpsilon(double.Parse(txtEpsilon.Text.Replace(",", "."), CultureInfo.InvariantCulture));

            lblResult.Text = $"Текущая точка: x = {iteration.X.ToString($"F{decimalPlaces}")}";
            lblIterations.Text = $"Итерация: {iteration.Iteration + 1}";
            lblFunctionValue.Text = $"f(x) = {iteration.FunctionValue.ToString($"F{decimalPlaces}")}";
            lblDerivative.Text = $"f'(x) = {iteration.FirstDerivative.ToString("E2")}, f''(x) = {iteration.SecondDerivative.ToString("E2")}";

            UpdateStepsList();
            PlotCurrentStep();
        }

        private void PlotCurrentStep()
        {
            if (_currentStepIndex < 0 || _currentStepIndex >= _stepByStepIterations.Count)
                return;

            var iteration = _stepByStepIterations[_currentStepIndex];

            var seriesToRemove = PlotModel.Series.Where(s =>
                s.Title == "Текущая итерация" ||
                s.Title == "Касательная" ||
                s.Title == "Точка касания").ToList();

            foreach (var series in seriesToRemove)
            {
                PlotModel.Series.Remove(series);
            }

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

            if (iteration.TangentLine != null)
            {
                double a = double.Parse(txtA.Text.Replace(",", "."), CultureInfo.InvariantCulture);
                double b = double.Parse(txtB.Text.Replace(",", "."), CultureInfo.InvariantCulture);
                double xPadding = Math.Abs(b - a) * 0.1;
                double visibleA = a - xPadding;
                double visibleB = b + xPadding;

                LineSeries tangentSeries = new LineSeries
                {
                    Title = "Касательная",
                    Color = OxyColor.FromRgb(0xFF, 0x8C, 0x00),
                    StrokeThickness = 1.5,
                    LineStyle = LineStyle.Dash
                };

                double tangentY1 = iteration.TangentLine.GetY(visibleA);
                double tangentY2 = iteration.TangentLine.GetY(visibleB);

                tangentSeries.Points.Add(new DataPoint(visibleA, tangentY1));
                tangentSeries.Points.Add(new DataPoint(visibleB, tangentY2));
                PlotModel.Series.Add(tangentSeries);

                ScatterSeries tangentPointSeries = new ScatterSeries
                {
                    Title = "Точка касания",
                    MarkerType = MarkerType.Circle,
                    MarkerSize = 6,
                    MarkerFill = OxyColor.FromRgb(0xFF, 0x8C, 0x00),
                    MarkerStroke = OxyColor.FromRgb(0x2C, 0x5F, 0x9E),
                    MarkerStrokeThickness = 1
                };
                tangentPointSeries.Points.Add(new ScatterPoint(iteration.TangentLine.PointX, iteration.TangentLine.PointY));
                PlotModel.Series.Add(tangentPointSeries);
            }

            PlotModel.InvalidatePlot(true);
        }

        private void UpdateStepsList()
        {
            lstSteps.Items.Clear();
            double epsilon = double.Parse(txtEpsilon.Text.Replace(",", "."), CultureInfo.InvariantCulture);

            for (int i = 0; i <= _currentStepIndex; i++)
            {
                var step = _stepByStepIterations[i];
                string stepType = "шаг";

                if (i == _stepByStepIterations.Count - 1 && Math.Abs(step.FirstDerivative) < epsilon && step.SecondDerivative > 0)
                    stepType = "МИНИМУМ";
                else if (Math.Abs(step.FirstDerivative) < epsilon && step.SecondDerivative > 0)
                    stepType = "минимум";
                else if (Math.Abs(step.FirstDerivative) < epsilon && step.SecondDerivative < 0)
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

        private void PlotInitialGraph(double a, double b)
        {
            PlotModel.Series.Clear();
            PlotModel.Annotations.Clear();
            _functionPoints.Clear();
            _minimumPoints.Clear();
            _iterationPoints.Clear();

            int pointsCount = 1000;
            double step = (b - a) / pointsCount;

            List<List<DataPoint>> segments = new List<List<DataPoint>>();
            List<DataPoint> currentSegment = new List<DataPoint>();

            for (int i = 0; i <= pointsCount; i++)
            {
                double x = a + i * step;
                try
                {
                    double y = _newtonMethod.CalculateFunction(x);

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

            ResetResultFields();

            PlotModel.Series.Clear();
            PlotModel.Annotations.Clear();
            PlotModel.InvalidatePlot(true);

            _stepByStepIterations.Clear();
            _tangentLines.Clear();
            _currentStepIndex = -1;
            lstSteps.Items.Clear();
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
            PlotModel?.Annotations.Clear();
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
            string helpText = @"СПРАВКА ПО МЕТОДУ НЬЮТОНА

Принцип работы:
───────────────
Метод Ньютона для поиска минимума функции использует
итерационную формулу:
xₙ₊₁ = xₙ - f'(xₙ)/f''(xₙ)

где:
• f'(x) - первая производная (наклон касательной)
• f''(x) - вторая производная (кривизна)

Доступные математические функции:
────────────────────────────────
• Основные операции: +, -, *, /, ^
• Тригонометрические: sin(x), cos(x), tan(x), atan(x)
• Экспоненциальные: exp(x), sqrt(x)
• Логарифмические: log(x), log10(x), log(x, b)
• Другие: abs(x), pow(x, y)
• Константы: pi, e

Примеры функций:
───────────────
1. Квадратичная: x^2 - 4*x + 4
2. Тригонометрическая: sin(x) + 0.5*x^2
3. Экспоненциальная: exp(-x^2)
4. Сложная: log(x^2 + 1) + sin(x)

Особенности метода:
─────────────────
• Требуется дважды дифференцируемая функция
• Чувствителен к выбору начальной точки
• Быстрая сходимость при удачном приближении
• Может расходиться при f''(x) ≈ 0

Рекомендации:
────────────
1. Убедитесь, что функция определена на интервале
2. Проверьте наличие минимума на интервале
3. Используйте автоподбор начальной точки
4. Начните с умеренной точности (0.001)
5. Увеличьте число итераций при медленной сходимости";

            MessageBox.Show(helpText, "Справка по методу Ньютона", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ResetResultFields()
        {
            lblResult.Text = "";
            lblIterations.Text = "";
            lblFunctionValue.Text = "";
            lblDerivative.Text = "";
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (!_calculationPerformed)
            {
                ResetResultFields();
            }
        }
    }
}