using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;
using HelixToolkit.Wpf;

namespace WpfApp1
{
    public partial class CoordinateDescentWindow : Window
    {
        private CoordinateDescentResult _currentResult;
        private CoordinateDescentMethod _currentMethod;
        private double _currentXStart, _currentXEnd, _currentYStart, _currentYEnd;

        // 3D объекты
        private GridLinesVisual3D _gridLines;
        private LinesVisual3D _trajectoryLine;
        private PointsVisual3D _surfacePoints;
        private List<SphereVisual3D> _iterationSpheres;
        private List<LinesVisual3D> _contourLines;
        private List<BillboardTextVisual3D> _contourLabels;

        // Состояние визуализации
        private bool _showGrid = true;
        private bool _showSurface = true;
        private bool _showTrajectory = true;
        private bool _showContours = true;

        // Режим камеры
        private bool _isInspectMode = true;

        public CoordinateDescentWindow()
        {
            InitializeComponent();
            Initialize3DViewport();
            this.Loaded += Window_Loaded;
            this.SizeChanged += Window_SizeChanged;

            this.KeyDown += Window_KeyDown;
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F5)
            {
                Calculate_Click(sender, e);
            }
            else if (e.Key == Key.Escape)
            {
                Close();
            }
            else if (e.Key == Key.F1)
            {
                Show3DControlsHelp_Click(sender, e);
            }
        }

        private void Initialize3DViewport()
        {
            viewport3D.Camera = new PerspectiveCamera
            {
                Position = new Point3D(10, 10, 10),
                LookDirection = new Vector3D(-10, -10, -10),
                UpDirection = new Vector3D(0, 0, 1),
                FieldOfView = 60
            };

            _iterationSpheres = new List<SphereVisual3D>();
            _contourLines = new List<LinesVisual3D>();
            _contourLabels = new List<BillboardTextVisual3D>();

            viewport3D.MouseDown += Viewport3D_MouseDown;
            viewport3D.MouseDoubleClick += Viewport3D_MouseDoubleClick;

            viewport3D.Height = 500;
        }

        private void Calculate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!ValidateInput())
                {
                    return;
                }

                // Парсинг входных данных
                _currentXStart = double.Parse(txtXStart.Text.Replace(",", "."), CultureInfo.InvariantCulture);
                _currentXEnd = double.Parse(txtXEnd.Text.Replace(",", "."), CultureInfo.InvariantCulture);
                _currentYStart = double.Parse(txtYStart.Text.Replace(",", "."), CultureInfo.InvariantCulture);
                _currentYEnd = double.Parse(txtYEnd.Text.Replace(",", "."), CultureInfo.InvariantCulture);
                double epsilon = double.Parse(txtEpsilon.Text.Replace(",", "."), CultureInfo.InvariantCulture);
                double lambda = double.Parse(txtLambda.Text.Replace(",", "."), CultureInfo.InvariantCulture); 
                string function = txtFunction.Text;
                bool findMinimum = cmbExtremumType.SelectedIndex == 0;

                double? startX = null;
                double? startY = null;

                if (!string.IsNullOrWhiteSpace(txtStartX.Text))
                {
                    startX = double.Parse(txtStartX.Text.Replace(",", "."), CultureInfo.InvariantCulture);
                    startX = Math.Max(_currentXStart, Math.Min(startX.Value, _currentXEnd));
                }

                if (!string.IsNullOrWhiteSpace(txtStartY.Text))
                {
                    startY = double.Parse(txtStartY.Text.Replace(",", "."), CultureInfo.InvariantCulture);
                    startY = Math.Max(_currentYStart, Math.Min(startY.Value, _currentYEnd));
                }

                function = PreprocessFunction(function);

                _currentMethod = new CoordinateDescentMethod(function);

                _currentResult = _currentMethod.FindExtremum(
                    _currentXStart, _currentXEnd, _currentYStart, _currentYEnd,
                    epsilon, findMinimum, startX, startY,
                    int.Parse(txtMaxIterations.Text),
                    lambda); 

                UpdateResultsUI(findMinimum, epsilon);

                if (_currentResult?.IterationHistory != null)
                {
                    dgIterations.ItemsSource = _currentResult.IterationHistory;
                }

                Create3DVisualization();

                DrawConvergenceGraph();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка вычисления",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Create3DVisualization()
        {
            if (_currentResult == null || _currentMethod == null)
            {
                MessageBox.Show("Нет данных для визуализации. Сначала выполните расчет функции.",
                    "Нет данных", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                Clear3DScene();

                if (_showGrid)
                {
                    AddGridLines();
                }

                if (_showContours && _currentMethod != null)
                {
                    AddContourLines();
                }

                if (_showSurface && _currentMethod != null)
                {
                    AddFunctionSurface();
                }

                if (_showTrajectory && _currentResult?.IterationHistory != null)
                {
                    AddTrajectory(_currentResult.IterationHistory);
                }

                if (viewport3D != null)
                {
                    viewport3D.ZoomExtents();
                }

                UpdateMenuStates();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка создания 3D визуализации: {ex.Message}");
                MessageBox.Show($"Ошибка создания 3D визуализации: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateMenuStates()
        {
            miShowGrid.IsChecked = _showGrid;
            miShowSurface.IsChecked = _showSurface;
            miShowTrajectory.IsChecked = _showTrajectory;
            miShowContours.IsChecked = _showContours;
        }

        private void AddGridLines()
        {
            double xSize = _currentXEnd - _currentXStart;
            double ySize = _currentYEnd - _currentYStart;

            _gridLines = new GridLinesVisual3D
            {
                Width = xSize,
                Length = ySize,
                MinorDistance = Math.Min(xSize, ySize) / 10,
                MajorDistance = Math.Min(xSize, ySize) / 5,
                Center = new Point3D((_currentXStart + _currentXEnd) / 2,
                                    (_currentYStart + _currentYEnd) / 2, 0),
                Fill = Brushes.Transparent,
                Thickness = 0.5
            };

            viewport3D.Children.Add(_gridLines);
        }

        private void AddFunctionSurface()
        {
            try
            {
                var surfacePoints = _currentMethod.GenerateSurfaceData(
                    _currentXStart, _currentXEnd,
                    _currentYStart, _currentYEnd,
                    40); 

                if (!surfacePoints.Any())
                    return;

                var pointCollection = new Point3DCollection();

                foreach (var point in surfacePoints)
                {
                    pointCollection.Add(new Point3D(point.X, point.Y, point.Z));
                }

                _surfacePoints = new PointsVisual3D
                {
                    Points = pointCollection,
                    Size = 4,
                    Color = Colors.Blue
                };

                viewport3D.Children.Add(_surfacePoints);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка создания поверхности: {ex.Message}");
                AddSimpleSurface();
            }
        }

        private void AddSimpleSurface()
        {
            var points = _currentMethod.GenerateSurfaceData(
                _currentXStart, _currentXEnd,
                _currentYStart, _currentYEnd,
                20); 

            var pointCollection = new Point3DCollection();
            foreach (var point in points)
            {
                pointCollection.Add(new Point3D(point.X, point.Y, point.Z));
            }

            _surfacePoints = new PointsVisual3D
            {
                Points = pointCollection,
                Size = 3,
                Color = Colors.Blue
            };

            viewport3D.Children.Add(_surfacePoints);
        }

        private void AddContourLines()
        {
            try
            {
                var contours = _currentMethod.GenerateContourLines(
                    _currentXStart, _currentXEnd,
                    _currentYStart, _currentYEnd,
                    7, 40); 

                double minZ = double.MaxValue;
                double maxZ = double.MinValue;

                foreach (var contour in contours)
                {
                    if (contour.Any())
                    {
                        double z = contour[0].Z;
                        minZ = Math.Min(minZ, z);
                        maxZ = Math.Max(maxZ, z);
                    }
                }

                if (Math.Abs(maxZ - minZ) < 1e-10)
                {
                    minZ -= 1;
                    maxZ += 1;
                }

                int contourIndex = 0;
                foreach (var contour in contours)
                {
                    if (contour.Count > 1)
                    {
                        var pointCollection = new Point3DCollection();
                        foreach (var point in contour)
                        {
                            pointCollection.Add(new Point3D(point.X, point.Y, point.Z));
                        }

                        double normalizedValue = (contour[0].Z - minZ) / (maxZ - minZ);
                        Color contourColor = GetContourColor(normalizedValue);

                        var contourLine = new LinesVisual3D
                        {
                            Points = pointCollection,
                            Color = contourColor,
                            Thickness = 1.5
                        };

                        _contourLines.Add(contourLine);
                        viewport3D.Children.Add(contourLine);

                        if (contourIndex % 3 == 0 && contour.Count > 20)
                        {
                            int midPoint = contour.Count / 2;
                            if (midPoint < contour.Count)
                            {
                                AddContourLabel(contour[midPoint], contour[0].Z);
                            }
                        }

                        contourIndex++;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка создания линий уровня: {ex.Message}");
            }
        }

        private Color GetContourColor(double normalizedValue)
        {
            normalizedValue = Math.Max(0, Math.Min(1, normalizedValue));
            byte r = (byte)(255 * normalizedValue);
            byte g = 0;
            byte b = (byte)(255 * (1 - normalizedValue));
            return Color.FromRgb(r, g, b);
        }

        private void AddContourLabel(Point3D position, double value)
        {
            var text = new BillboardTextVisual3D
            {
                Text = value.ToString("F2"),
                Position = new Point3D(position.X, position.Y, position.Z + 0.1),
                Foreground = Brushes.DarkGreen,
                Background = Brushes.White,
                Padding = new Thickness(2),
                FontSize = 8,
                FontWeight = FontWeights.Bold
            };

            _contourLabels.Add(text);
            viewport3D.Children.Add(text);
        }

        private void AddTrajectory(List<CoordinateDescentIteration> iterations)
        {
            if (iterations == null || iterations.Count < 2)
                return;

            foreach (var sphere in _iterationSpheres)
            {
                viewport3D.Children.Remove(sphere);
            }
            _iterationSpheres.Clear();

            if (_trajectoryLine != null)
            {
                viewport3D.Children.Remove(_trajectoryLine);
                _trajectoryLine = null;
            }

            var points = new Point3DCollection();

            for (int i = 0; i < iterations.Count; i++)
            {
                var iteration = iterations[i];
                double z = iteration.Value;

                if (double.IsInfinity(z) || double.IsNaN(z) || Math.Abs(z) > 1000)
                {
                    z = 0;
                }

                var point = new Point3D(iteration.X, iteration.Y, z);
                points.Add(point);

                double xSize = _currentXEnd - _currentXStart;
                double ySize = _currentYEnd - _currentYStart;
                double sphereRadius = Math.Max(xSize, ySize) * 0.015;

                var sphere = new SphereVisual3D
                {
                    Center = point,
                    Radius = sphereRadius,
                    Material = MaterialHelper.CreateMaterial(GetIterationColor(i, iterations.Count))
                };

                _iterationSpheres.Add(sphere);
                viewport3D.Children.Add(sphere);
            }

            _trajectoryLine = new LinesVisual3D
            {
                Points = points,
                Color = Colors.Red,
                Thickness = 2
            };

            viewport3D.Children.Add(_trajectoryLine);

            if (_iterationSpheres.Count > 0)
            {
                _iterationSpheres[0].Material = MaterialHelper.CreateMaterial(Colors.Green);
                _iterationSpheres.Last().Material = MaterialHelper.CreateMaterial(Colors.Yellow);
            }
        }

        private Color GetIterationColor(int index, int total)
        {
            double ratio = total > 1 ? (double)index / (total - 1) : 0;
            byte r = (byte)(255 * ratio);
            byte b = (byte)(255 * (1 - ratio));
            return Color.FromRgb(r, 0, b);
        }

        private void Clear3DScene()
        {
            try
            {
                if (viewport3D == null) return;

                if (_gridLines != null)
                {
                    viewport3D.Children.Remove(_gridLines);
                    _gridLines = null;
                }

                if (_surfacePoints != null)
                {
                    viewport3D.Children.Remove(_surfacePoints);
                    _surfacePoints = null;
                }

                if (_trajectoryLine != null)
                {
                    viewport3D.Children.Remove(_trajectoryLine);
                    _trajectoryLine = null;
                }

                foreach (var sphere in _iterationSpheres)
                {
                    viewport3D.Children.Remove(sphere);
                }
                _iterationSpheres.Clear();

                foreach (var line in _contourLines)
                {
                    viewport3D.Children.Remove(line);
                }
                _contourLines.Clear();

                foreach (var label in _contourLabels)
                {
                    viewport3D.Children.Remove(label);
                }
                _contourLabels.Clear();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при очистке 3D сцены: {ex.Message}");
            }
        }

        private void Viewport3D_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            try
            {
                var point = viewport3D.FindNearestPoint(e.GetPosition(viewport3D));
                if (point.HasValue)
                {
                    double x = point.Value.X;
                    double y = point.Value.Y;

                    if (x >= _currentXStart && x <= _currentXEnd &&
                        y >= _currentYStart && y <= _currentYEnd)
                    {
                        txtStartX.Text = x.ToString("F3");
                        txtStartY.Text = y.ToString("F3");

                        try
                        {
                            double z = _currentMethod?.CalculateFunction(x, y) ?? 0;
                        }
                        catch
                        {
                        }
                    }
                }
            }
            catch
            {
            }
        }

        private void Viewport3D_MouseDown(object sender, MouseButtonEventArgs e)
        {
        }

        private void UpdateResultsUI(bool findMinimum, double epsilon)
        {
            if (_currentResult == null) return;

            lblExtremumType.Text = findMinimum ? "Минимум" : "Максимум";
            lblPoint.Text = $"({_currentResult.GetFormattedX()}, {_currentResult.GetFormattedY()})";
            lblValue.Text = _currentResult.GetFormattedValue();
            lblIterations.Text = $"{_currentResult.Iterations}";
            lblStatus.Text = _currentResult.GetStatus();

            if (_currentResult.BoundaryWarning)
            {
                lblWarning.Text = "Внимание: Возможно, алгоритм остановился на границе области. Попробуйте другую начальную точку или увеличьте интервал поиска.";
                lblWarning.Visibility = Visibility.Visible;
            }
            else
            {
                lblWarning.Visibility = Visibility.Collapsed;
            }

            double evalPerSec = _currentResult.ElapsedTime > 0 ?
                _currentResult.TotalFunctionEvaluations / _currentResult.ElapsedTime : 0;

            double avgStepX = 0, avgStepY = 0;
            if (_currentResult.IterationHistory != null && _currentResult.IterationHistory.Count > 1)
            {
                avgStepX = _currentResult.IterationHistory.Average(it => Math.Abs(it.DeltaX));
                avgStepY = _currentResult.IterationHistory.Average(it => Math.Abs(it.DeltaY));
            }
        }

        private void DrawConvergenceGraph()
        {
            if (_currentResult?.ConvergenceHistory == null || _currentResult.ConvergenceHistory.Count < 2)
                return;

            canvasConvergence.Children.Clear();

            double width = canvasConvergence.ActualWidth;
            double height = canvasConvergence.ActualHeight;

            if (width < 100 || height < 100) return;

            var history = _currentResult.ConvergenceHistory;
            double minValue = history.Min();
            double maxValue = history.Max();
            double range = maxValue - minValue;

            if (range < 1e-10) range = 1;

            double leftMargin = 40;
            double rightMargin = 20;
            double topMargin = 20;
            double bottomMargin = 30;

            double plotWidth = width - leftMargin - rightMargin;
            double plotHeight = height - topMargin - bottomMargin;

            Polyline polyline = new Polyline
            {
                Stroke = Brushes.Red,
                StrokeThickness = 2,
                StrokeLineJoin = PenLineJoin.Round
            };

            for (int i = 0; i < history.Count; i++)
            {
                double x = leftMargin + (double)i / (history.Count - 1) * plotWidth;
                double y = topMargin + plotHeight - (history[i] - minValue) / range * plotHeight;

                polyline.Points.Add(new Point(x, y));
            }

            canvasConvergence.Children.Add(polyline);

            DrawAxes(width, height, leftMargin, rightMargin, topMargin, bottomMargin, minValue, maxValue, history.Count);
        }

        private void DrawAxes(double width, double height, double leftMargin, double rightMargin,
                            double topMargin, double bottomMargin, double minY, double maxY, int pointCount)
        {
            double plotWidth = width - leftMargin - rightMargin;
            double plotHeight = height - topMargin - bottomMargin;

            Line xAxis = new Line
            {
                X1 = leftMargin,
                Y1 = topMargin + plotHeight,
                X2 = leftMargin + plotWidth,
                Y2 = topMargin + plotHeight,
                Stroke = Brushes.Black,
                StrokeThickness = 1
            };

            Line yAxis = new Line
            {
                X1 = leftMargin,
                Y1 = topMargin,
                X2 = leftMargin,
                Y2 = topMargin + plotHeight,
                Stroke = Brushes.Black,
                StrokeThickness = 1
            };

            canvasConvergence.Children.Add(xAxis);
            canvasConvergence.Children.Add(yAxis);

            for (int i = 0; i <= 5; i++)
            {
                double x = leftMargin + i * plotWidth / 5;
                int iteration = (int)(i * (pointCount - 1) / 5.0);

                Line tick = new Line
                {
                    X1 = x,
                    Y1 = topMargin + plotHeight,
                    X2 = x,
                    Y2 = topMargin + plotHeight + 5,
                    Stroke = Brushes.Black,
                    StrokeThickness = 1
                };

                canvasConvergence.Children.Add(tick);

                TextBlock label = new TextBlock
                {
                    Text = iteration.ToString(),
                    FontSize = 10,
                    Foreground = Brushes.Black
                };

                Canvas.SetLeft(label, x - 8);
                Canvas.SetTop(label, topMargin + plotHeight + 8);

                canvasConvergence.Children.Add(label);
            }

            TextBlock xLabel = new TextBlock
            {
                Text = "Итерация",
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.Black
            };

            Canvas.SetLeft(xLabel, leftMargin + plotWidth / 2 - 25);
            Canvas.SetTop(xLabel, topMargin + plotHeight + 25);
            canvasConvergence.Children.Add(xLabel);

            for (int i = 0; i <= 5; i++)
            {
                double y = topMargin + plotHeight - i * plotHeight / 5;
                double value = minY + i * (maxY - minY) / 5;

                Line tick = new Line
                {
                    X1 = leftMargin,
                    Y1 = y,
                    X2 = leftMargin - 5,
                    Y2 = y,
                    Stroke = Brushes.Black,
                    StrokeThickness = 1
                };

                canvasConvergence.Children.Add(tick);

                TextBlock label = new TextBlock
                {
                    Text = value.ToString("F2"),
                    FontSize = 10,
                    Foreground = Brushes.Black
                };

                Canvas.SetLeft(label, leftMargin - 35);
                Canvas.SetTop(label, y - 8);

                canvasConvergence.Children.Add(label);
            }

            TextBlock yLabel = new TextBlock
            {
                Text = "f(x,y)",
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.Black
            };

            Canvas.SetLeft(yLabel, 5);
            Canvas.SetTop(yLabel, topMargin + plotHeight / 2 - 20);
            canvasConvergence.Children.Add(yLabel);
        }

        private bool ValidateInput()
        {
            if (string.IsNullOrWhiteSpace(txtFunction.Text) ||
                string.IsNullOrWhiteSpace(txtXStart.Text) ||
                string.IsNullOrWhiteSpace(txtXEnd.Text) ||
                string.IsNullOrWhiteSpace(txtYStart.Text) ||
                string.IsNullOrWhiteSpace(txtYEnd.Text) ||
                string.IsNullOrWhiteSpace(txtEpsilon.Text) ||
                string.IsNullOrWhiteSpace(txtLambda.Text) || 
                string.IsNullOrWhiteSpace(txtMaxIterations.Text))
            {
                MessageBox.Show("Все обязательные поля должны быть заполнены!", "Ошибка ввода",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (!double.TryParse(txtXStart.Text.Replace(",", "."), NumberStyles.Any,
                    CultureInfo.InvariantCulture, out double xStart) ||
                !double.TryParse(txtXEnd.Text.Replace(",", "."), NumberStyles.Any,
                    CultureInfo.InvariantCulture, out double xEnd) ||
                !double.TryParse(txtYStart.Text.Replace(",", "."), NumberStyles.Any,
                    CultureInfo.InvariantCulture, out double yStart) ||
                !double.TryParse(txtYEnd.Text.Replace(",", "."), NumberStyles.Any,
                    CultureInfo.InvariantCulture, out double yEnd) ||
                !double.TryParse(txtEpsilon.Text.Replace(",", "."), NumberStyles.Any,
                    CultureInfo.InvariantCulture, out double epsilon) ||
                !double.TryParse(txtLambda.Text.Replace(",", "."), NumberStyles.Any, 
                    CultureInfo.InvariantCulture, out double lambda) ||
                !int.TryParse(txtMaxIterations.Text, out int maxIterations))
            {
                MessageBox.Show("Все числовые параметры должны быть корректными числами!", "Ошибка ввода",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (!string.IsNullOrWhiteSpace(txtStartX.Text) &&
                !double.TryParse(txtStartX.Text.Replace(",", "."), NumberStyles.Any,
                    CultureInfo.InvariantCulture, out _))
            {
                MessageBox.Show("Начальная координата X должна быть числом!", "Ошибка ввода",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (!string.IsNullOrWhiteSpace(txtStartY.Text) &&
                !double.TryParse(txtStartY.Text.Replace(",", "."), NumberStyles.Any,
                    CultureInfo.InvariantCulture, out _))
            {
                MessageBox.Show("Начальная координата Y должна быть числом!", "Ошибка ввода",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (xStart >= xEnd)
            {
                MessageBox.Show("Начало интервала по X должно быть меньше конца!", "Ошибка ввода",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (yStart >= yEnd)
            {
                MessageBox.Show("Начало интервала по Y должно быть меньше конца!", "Ошибка ввода",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (epsilon <= 0)
            {
                MessageBox.Show("Точность epsilon должна быть положительным числом!", "Ошибка ввода",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (lambda <= 0) 
            {
                MessageBox.Show("Длина шага λ должна быть положительным числом!", "Ошибка ввода",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (maxIterations <= 0 || maxIterations > 10000)
            {
                MessageBox.Show("Максимальное число итераций должно быть от 1 до 10000!", "Ошибка ввода",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        private string PreprocessFunction(string function)
        {
            if (string.IsNullOrWhiteSpace(function))
                return function;

            return function.Replace(",", ".");
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            txtFunction.Text = "x^2 + y^2";
            cmbExtremumType.SelectedIndex = 0;
            txtXStart.Text = "-2";
            txtXEnd.Text = "2";
            txtYStart.Text = "-2";
            txtYEnd.Text = "2";
            txtStartX.Text = "";
            txtStartY.Text = "";
            txtEpsilon.Text = "0.001";
            txtLambda.Text = "1.0"; 
            txtMaxIterations.Text = "100";

            ClearResults();
            Clear3DScene();
            canvasConvergence.Children.Clear();
            lblWarning.Visibility = Visibility.Collapsed;
        }

        private void ClearResults()
        {
            lblExtremumType.Text = "-";
            lblPoint.Text = "(-, -)";
            lblValue.Text = "-";
            lblIterations.Text = "-";
            lblStatus.Text = "-";

            dgIterations.ItemsSource = null;
            _currentResult = null;
            _currentMethod = null;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            cmbExtremumType.SelectedIndex = 0;
            UpdateMenuStates();
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.NewSize.Width < 1200)
            {
                viewport3D.Height = 400;
                canvasConvergence.Height = 150;
            }
            else if (e.NewSize.Width < 1400)
            {
                viewport3D.Height = 450;
                canvasConvergence.Height = 160;
            }
            else
            {
                viewport3D.Height = 500;
                canvasConvergence.Height = 180;
            }

            if (_currentResult?.ConvergenceHistory != null)
            {
                DrawConvergenceGraph();
            }
        }

        private void btnResetView_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (viewport3D != null)
                {
                    viewport3D.ZoomExtents();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при сбросе вида: {ex.Message}");
            }
        }

        private void btnToggleCamera_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (viewport3D == null) return;

                _isInspectMode = !_isInspectMode;

                if (_isInspectMode)
                {
                    viewport3D.CameraMode = CameraMode.Inspect;
                    viewport3D.CameraRotationMode = CameraRotationMode.Turntable;
                    btnToggleCamera.Content = "Режим: Inspect";
                }
                else
                {
                    viewport3D.CameraMode = CameraMode.FixedPosition;
                    viewport3D.CameraRotationMode = CameraRotationMode.Trackball;
                    btnToggleCamera.Content = "Режим: Trackball";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при переключении камеры: {ex.Message}");
            }
        }

        private void btnToggleGrid_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _showGrid = !_showGrid;
                miShowGrid.IsChecked = _showGrid;

                if (_currentResult == null)
                {
                    MessageBox.Show("Сначала выполните расчет функции", "Нет данных",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                Create3DVisualization();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при переключении сетки: {ex.Message}");
                MessageBox.Show($"Ошибка при переключении сетки: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnToggleSurface_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _showSurface = !_showSurface;
                miShowSurface.IsChecked = _showSurface;

                if (_currentResult == null)
                {
                    MessageBox.Show("Сначала выполните расчет функции", "Нет данных",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                Create3DVisualization();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при переключении поверхности: {ex.Message}");
                MessageBox.Show($"Ошибка при переключении поверхности: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnToggleTrajectory_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _showTrajectory = !_showTrajectory;
                miShowTrajectory.IsChecked = _showTrajectory;

                if (_currentResult == null)
                {
                    MessageBox.Show("Сначала выполните расчет функции", "Нет данных",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                Create3DVisualization();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при переключении траектории: {ex.Message}");
                MessageBox.Show($"Ошибка при переключении траектории: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnToggleContours_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _showContours = !_showContours;
                miShowContours.IsChecked = _showContours;

                if (_currentResult == null)
                {
                    MessageBox.Show("Сначала выполните расчет функции", "Нет данных",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                Create3DVisualization();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при переключении изолиний: {ex.Message}");
                MessageBox.Show($"Ошибка при переключении изолиний: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Show3DControlsHelp_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Window
            {
                Title = "Управление 3D графиком",
                Width = 500,
                Height = 350,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                ResizeMode = ResizeMode.NoResize,
                Background = new SolidColorBrush(Color.FromRgb(240, 245, 252)),
                WindowStyle = WindowStyle.ToolWindow
            };

            var scrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };

            var stackPanel = new StackPanel
            {
                Margin = new Thickness(20)
            };

            var title = new TextBlock
            {
                Text = "Управление 3D графиком",
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.Navy,
                Margin = new Thickness(0, 0, 0, 15),
                HorizontalAlignment = HorizontalAlignment.Center
            };

            var content = new TextBlock
            {
                Text = @"Управление мышью:

• ЛЕВАЯ КНОПКА + перемещение = вращение сцены
• СРЕДНЯЯ КНОПКА + перемещение = панорамирование
• ПРАВАЯ КНОПКА + перемещение = масштабирование
• КОЛЕСО МЫШИ = приближение/отдаление

Дополнительные возможности:

• Двойной клик на графике = установить начальную точку
• Кнопка 'Сбросить вид' = вернуться к начальному виду
• Кнопка 'Переключить камеру' = изменить режим камеры

Горячие клавиши:

• F5 = выполнить вычисление
• ESC = выйти из полноэкранного режима/закрыть программу
• F1 = показать эту справку

Настройки визуализации:

• Сетка = показать/скрыть координатную сетку
• Поверхность = показать/скрыть поверхность функции
• Траектория = показать/скрыть путь спуска
• Изолинии = показать/скрыть линии уровня

Советы по использованию:

• Для точного выбора начальной точки используйте двойной клик
• При работе с большими функциями уменьшите разрешение поверхности
• Используйте изолинии для лучшего понимания формы функции
• Если алгоритм останавливается на границе, попробуйте другую начальную точку
• Увеличьте интервал поиска для сложных функций с удаленными экстремумами",
                TextWrapping = TextWrapping.Wrap,
                FontSize = 12,
                Margin = new Thickness(0, 0, 0, 20)
            };

            var closeButton = new Button
            {
                Content = "Закрыть",
                Width = 100,
                Height = 30,
                HorizontalAlignment = HorizontalAlignment.Center,
                Background = Brushes.Navy,
                Foreground = Brushes.White,
                Cursor = Cursors.Hand,
                Margin = new Thickness(0, 10, 0, 0)
            };
            closeButton.Click += (s, args) => dialog.Close();

            stackPanel.Children.Add(title);
            stackPanel.Children.Add(content);
            stackPanel.Children.Add(closeButton);

            scrollViewer.Content = stackPanel;
            dialog.Content = scrollViewer;
            dialog.ShowDialog();
        }

        private void btnExamples_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Window
            {
                Title = "Выберите пример функции",
                Width = 450,
                Height = 400, 
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                ResizeMode = ResizeMode.NoResize,
                Background = new SolidColorBrush(Color.FromRgb(240, 245, 252)),
                WindowStyle = WindowStyle.ToolWindow
            };

            var stackPanel = new StackPanel
            {
                Margin = new Thickness(10)
            };

            var title = new TextBlock
            {
                Text = "Примеры функций для тестирования:",
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.Navy,
                Margin = new Thickness(0, 0, 0, 10)
            };

            stackPanel.Children.Add(title);

            var examples = new[]
            {
                new {
                    Name = "Парабола (простая)",
                    Function = "x^2 + y^2",
                    XStart = "-2", XEnd = "2",
                    YStart = "-2", YEnd = "2",
                    StartX = "1.5", StartY = "1.5",
                    Lambda = "1.0", 
                    Description = "Минимум в (0,0), f=0"
                },
                new {
                    Name = "Смещенная парабола",
                    Function = "(x-1)^2 + (y+2)^2",
                    XStart = "-5", XEnd = "5",
                    YStart = "-5", YEnd = "5",
                    StartX = "0", StartY = "0",
                    Lambda = "2.0",
                    Description = "Минимум в (1,-2), f=0"
                },
                new {
                    Name = "Функция Розенброка",
                    Function = "100*(y - x^2)^2 + (1 - x)^2",
                    XStart = "-2", XEnd = "2",
                    YStart = "-1", YEnd = "3",
                    StartX = "-1", StartY = "1",
                    Lambda = "0.5",  
                    Description = "Минимум в (1,1), f=0 (сложная)"
                },
                new {
                    Name = "Синусоида",
                    Function = "sin(x) * cos(y)",
                    XStart = "0", XEnd = "6.28",
                    YStart = "0", YEnd = "6.28",
                    StartX = "1", StartY = "1",
                    Lambda = "1.0",
                    Description = "Много локальных экстремумов"
                },
                new {
                    Name = "Мексиканская шляпа",
                    Function = "(1 - x^2 - y^2) * exp(-(x^2 + y^2)/2)",
                    XStart = "-3", XEnd = "3",
                    YStart = "-3", YEnd = "3",
                    StartX = "0.5", StartY = "0.5",
                    Lambda = "0.5",
                    Description = "Максимум в центре"
                },
            };

            foreach (var example in examples)
            {
                var button = new Button
                {
                    Content = example.Name,
                    Tag = example,
                    Height = 40,
                    Margin = new Thickness(0, 0, 0, 5),
                    Background = Brushes.Navy,
                    Foreground = Brushes.White,
                    BorderThickness = new Thickness(0),
                    Cursor = Cursors.Hand
                };

                button.Click += (s, args) =>
                {
                    dynamic ex = ((Button)s).Tag;
                    txtFunction.Text = ex.Function;
                    txtXStart.Text = ex.XStart;
                    txtXEnd.Text = ex.XEnd;
                    txtYStart.Text = ex.YStart;
                    txtYEnd.Text = ex.YEnd;
                    txtStartX.Text = ex.StartX;
                    txtStartY.Text = ex.StartY;
                    txtLambda.Text = ex.Lambda; 
                    dialog.Close();
                };

                var toolTip = new ToolTip();
                var toolTipContent = new StackPanel();
                toolTipContent.Children.Add(new TextBlock
                {
                    Text = $"f(x,y) = {example.Function}",
                    FontWeight = FontWeights.Bold
                });
                toolTipContent.Children.Add(new TextBlock
                {
                    Text = $"λ = {example.Lambda}", 
                    FontWeight = FontWeights.Bold
                });
                toolTipContent.Children.Add(new TextBlock
                {
                    Text = example.Description,
                    FontStyle = FontStyles.Italic,
                    Margin = new Thickness(0, 5, 0, 0)
                });
                toolTip.Content = toolTipContent;
                button.ToolTip = toolTip;

                stackPanel.Children.Add(button);
            }

            var closeButton = new Button
            {
                Content = "Закрыть",
                Height = 30,
                Width = 100,
                HorizontalAlignment = HorizontalAlignment.Center,
                Background = Brushes.Gray,
                Foreground = Brushes.White,
                Margin = new Thickness(0, 10, 0, 0),
                Cursor = Cursors.Hand
            };
            closeButton.Click += (s, args) => dialog.Close();

            stackPanel.Children.Add(closeButton);
            dialog.Content = stackPanel;
            dialog.ShowDialog();
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void AboutMethod_Click(object sender, RoutedEventArgs e)
        {
            ShowAboutMethodDialog();
        }

        private void HelpSyntax_Click(object sender, RoutedEventArgs e)
        {
            ShowSyntaxHelpDialog();
        }

        private void ShowAboutMethodDialog()
        {
            var dialog = new Window
            {
                Title = "О методе покоординатного спуска",
                Width = 600,
                Height = 500,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                WindowStyle = WindowStyle.ToolWindow,
                Background = new SolidColorBrush(Color.FromRgb(240, 245, 252))
            };

            var scrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };

            var stackPanel = new StackPanel
            {
                Margin = new Thickness(20)
            };

            var title = new TextBlock
            {
                Text = "Метод покоординатного спуска",
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.Navy,
                Margin = new Thickness(0, 0, 0, 20),
                HorizontalAlignment = HorizontalAlignment.Center
            };

            var content = new TextBlock
            {
                Text = @"Метод покоординатного спуска — это итерационный метод оптимизации, 
используемый для нахождения локального минимума или максимума функции.

Основные принципы:
1. Начинаем с начальной точки (x₀, y₀)
2. На каждой итерации фиксируем все координаты, кроме одной
3. Оптимизируем функцию по одной координате за раз
4. Чередуем оптимизацию по осям X и Y
5. Процесс повторяется до достижения заданной точности

Алгоритм покоординатного спуска:

1. Инициализация:
   • Выбрать начальную точку x⁰ = (x₁⁰, x₂⁰, ..., xₙ⁰)
   • Задать точность ε > 0 и длину шага λ > 0
   • Установить k = 0

2. Для каждой координаты i = 1, 2, ..., n:
   • Исследовать монотонность в ε-окрестности
   • Определить направление поиска (знак λ)
   • Выполнить одномерный поиск на отрезке [xᵢᵏ, xᵢᵏ ± λ]
   • Обновить i-ю координату

3. Проверка условия остановки:
   • Если ||xᵏ⁺¹ - xᵏ|| < 2ε, то остановиться
   • Иначе: k = k + 1 и перейти к шагу 2

Параметры алгоритма в данной реализации:
• ε (эпсилон) - точность поиска
• λ (лямбда) - длина шага поиска
• Максимальное число итераций

Преимущества метода:
• Простота реализации — не требует вычисления градиента
• Эффективен для функций с разделяющимися переменными
• Может использоваться для негладких функций

Ограничения:
• Медленная сходимость для некоторых функций
• Чувствителен к выбору начальной точки и шага λ
• Может застревать в локальных экстремумах

В данной программе реализован классический метод покоординатного спуска с параметром λ для управления длиной шага поиска.",
                TextWrapping = TextWrapping.Wrap,
                FontSize = 13,
                Margin = new Thickness(0, 0, 0, 20)
            };

            var closeButton = new Button
            {
                Content = "Закрыть",
                Width = 100,
                Height = 30,
                HorizontalAlignment = HorizontalAlignment.Center,
                Background = Brushes.Navy,
                Foreground = Brushes.White,
                Cursor = Cursors.Hand,
                Margin = new Thickness(0, 10, 0, 0)
            };
            closeButton.Click += (s, args) => dialog.Close();

            stackPanel.Children.Add(title);
            stackPanel.Children.Add(content);
            stackPanel.Children.Add(closeButton);

            scrollViewer.Content = stackPanel;
            dialog.Content = scrollViewer;
            dialog.ShowDialog();
        }

        private void ShowSyntaxHelpDialog()
        {
            var dialog = new Window
            {
                Title = "Синтаксис математических функций",
                Width = 500,
                Height = 400,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                WindowStyle = WindowStyle.ToolWindow,
                Background = new SolidColorBrush(Color.FromRgb(240, 245, 252))
            };

            var scrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };

            var stackPanel = new StackPanel
            {
                Margin = new Thickness(15)
            };

            var title = new TextBlock
            {
                Text = "Поддерживаемые математические операции",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.Navy,
                Margin = new Thickness(0, 0, 0, 15)
            };

            var content = new TextBlock
            {
                Text = @"Поддерживаемые операторы и функции:

Базовые арифметические операции:
• Сложение: x + y
• Вычитание: x - y
• Умножение: x * y
• Деление: x / y
• Возведение в степень: x^y или pow(x, y)
• Остаток от деления: x % y

Математические функции:
• Тригонометрические:
  - Синус: sin(x)
  - Косинус: cos(x)
  - Тангенс: tan(x)
  - Арксинус: asin(x)
  - Арккосинус: acos(x)
  - Арктангенс: atan(x)

• Экспоненциальные и логарифмические:
  - Экспонента: exp(x)
  - Натуральный логарифм: log(x) или ln(x)
  - Десятичный логарифм: log10(x)
  - Произвольный логарифм: log(x, base)

• Степенные и корни:
  - Квадратный корень: sqrt(x)
  - Степень: pow(x, y)
  - Абсолютное значение: abs(x)

• Округление:
  - Округление вниз: floor(x)
  - Округление вверх: ceil(x)
  - Округление до ближайшего: round(x)

Математические константы:
• pi или π (3.141592653589793)
• e (2.718281828459045)

Примеры корректных функций:
1. Простые: x^2 + y^2
2. Тригонометрические: sin(x) * cos(y)
3. Экспоненциальные: exp(-(x^2 + y^2)/2)
4. Сложные: 100*(y - x^2)^2 + (1 - x)^2
5. С параметрами: (x-1)^2 + (y+2)^2
6. Комбинированные: sin(x^2 + y^2) / (1 + x^2 + y^2)

Ограничения:
• Используйте точку (.) как разделитель десятичных дробей
• Все скобки должны быть правильно закрыты
• Функции чувствительны к регистру (sin, а не Sin)
• Избегайте деления на нero",
                TextWrapping = TextWrapping.Wrap,
                FontSize = 12,
                Margin = new Thickness(0, 0, 0, 20)
            };

            var closeButton = new Button
            {
                Content = "Закрыть",
                Width = 100,
                Height = 30,
                HorizontalAlignment = HorizontalAlignment.Center,
                Background = Brushes.Navy,
                Foreground = Brushes.White,
                Cursor = Cursors.Hand,
                Margin = new Thickness(0, 10, 0, 0)
            };
            closeButton.Click += (s, args) => dialog.Close();

            stackPanel.Children.Add(title);
            stackPanel.Children.Add(content);
            stackPanel.Children.Add(closeButton);

            scrollViewer.Content = stackPanel;
            dialog.Content = scrollViewer;
            dialog.ShowDialog();
        }
    }
}