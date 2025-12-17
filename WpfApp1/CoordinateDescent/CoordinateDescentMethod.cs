using System;
using NCalc;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Windows.Media.Media3D;
using System.Text.RegularExpressions;

namespace WpfApp1
{
    public class CoordinateDescentMethod
    {
        private readonly Expression _expression;
        private int _functionEvaluations = 0;
        private Stopwatch _stopwatch = new Stopwatch();

        public CoordinateDescentMethod(string function)
        {
            string processedFunction = PreprocessFunctionString(function);
            Console.WriteLine($"Исходная функция: {function}");
            Console.WriteLine($"Обработанная функция: {processedFunction}");

            try
            {
                _expression = new Expression(processedFunction.ToLower(), EvaluateOptions.IgnoreCase);
                _expression.Parameters["pi"] = Math.PI;
                _expression.Parameters["e"] = Math.E;
                _expression.EvaluateFunction += EvaluateFunction;
                _expression.EvaluateParameter += EvaluateParameter;

                _expression.Parameters["x"] = 1.0;
                _expression.Parameters["y"] = 1.0;
                var testResult = _expression.Evaluate();
                Console.WriteLine($"Тест вычисления в (1,1): {testResult}");
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Ошибка в функции '{function}': {ex.Message}\n" +
                                          $"Используйте синтаксис: pow(x,2), sin(x), exp(y), etc.");
            }
        }

        private string PreprocessFunctionString(string function)
        {
            if (string.IsNullOrWhiteSpace(function))
                return "0";

            function = function.Trim();
            function = function.Replace(',', '.');
            function = Regex.Replace(function, @"\s+", " ");
            function = function.Replace("**", "^");
            function = ProcessPowerOperator(function);

            return function;
        }

        private string ProcessPowerOperator(string input)
        {
            int caretIndex;
            while ((caretIndex = input.IndexOf('^')) >= 0)
            {
                string rightPart = ExtractRightPart(input, caretIndex + 1);
                string leftPart = ExtractLeftPart(input, caretIndex - 1);

                string powExpression = $"pow({leftPart},{rightPart})";

                int leftStart = caretIndex - leftPart.Length;
                int rightEnd = caretIndex + 1 + rightPart.Length;

                input = input.Substring(0, leftStart) + powExpression +
                        (rightEnd < input.Length ? input.Substring(rightEnd) : "");
            }

            return input;
        }

        private string ExtractRightPart(string input, int startIndex)
        {
            if (startIndex >= input.Length)
                return "";

            if (char.IsDigit(input[startIndex]) || input[startIndex] == '.')
            {
                int end = startIndex + 1;
                while (end < input.Length && (char.IsDigit(input[end]) || input[end] == '.'))
                {
                    end++;
                }
                return input.Substring(startIndex, end - startIndex);
            }
            else if (char.IsLetter(input[startIndex]))
            {
                int end = startIndex + 1;
                while (end < input.Length && (char.IsLetterOrDigit(input[end]) || input[end] == '_'))
                {
                    end++;
                }
                return input.Substring(startIndex, end - startIndex);
            }
            else if (input[startIndex] == '(')
            {
                int parenCount = 1;
                int end = startIndex + 1;

                while (end < input.Length && parenCount > 0)
                {
                    if (input[end] == '(') parenCount++;
                    if (input[end] == ')') parenCount--;
                    end++;
                }

                return input.Substring(startIndex, end - startIndex);
            }

            return input[startIndex].ToString();
        }

        private string ExtractLeftPart(string input, int endIndex)
        {
            if (endIndex < 0)
                return "";

            if (char.IsDigit(input[endIndex]) || input[endIndex] == '.')
            {
                int start = endIndex - 1;
                while (start >= 0 && (char.IsDigit(input[start]) || input[start] == '.'))
                {
                    start--;
                }
                start++;
                return input.Substring(start, endIndex - start + 1);
            }
            else if (char.IsLetter(input[endIndex]))
            {
                int start = endIndex - 1;
                while (start >= 0 && (char.IsLetterOrDigit(input[start]) || input[start] == '_'))
                {
                    start--;
                }
                start++;
                return input.Substring(start, endIndex - start + 1);
            }
            else if (input[endIndex] == ')')
            {
                int parenCount = 1;
                int start = endIndex - 1;

                while (start >= 0 && parenCount > 0)
                {
                    if (input[start] == ')') parenCount++;
                    if (input[start] == '(') parenCount--;
                    start--;
                }

                start++;
                return input.Substring(start, endIndex - start + 1);
            }

            return input[endIndex].ToString();
        }

        private void EvaluateParameter(string name, ParameterArgs args)
        {
            name = name.ToLower();

            if (name == "pi")
            {
                args.Result = Math.PI;
            }
            else if (name == "e")
            {
                args.Result = Math.E;
            }
        }

        private void EvaluateFunction(string name, FunctionArgs args)
        {
            try
            {
                name = name.ToLower();

                switch (name)
                {
                    case "sin":
                        args.Result = Math.Sin(Convert.ToDouble(args.Parameters[0].Evaluate()));
                        break;
                    case "cos":
                        args.Result = Math.Cos(Convert.ToDouble(args.Parameters[0].Evaluate()));
                        break;
                    case "tan":
                        args.Result = Math.Tan(Convert.ToDouble(args.Parameters[0].Evaluate()));
                        break;
                    case "atan":
                    case "arctan":
                        args.Result = Math.Atan(Convert.ToDouble(args.Parameters[0].Evaluate()));
                        break;
                    case "asin":
                    case "arcsin":
                        args.Result = Math.Asin(Convert.ToDouble(args.Parameters[0].Evaluate()));
                        break;
                    case "acos":
                    case "arccos":
                        args.Result = Math.Acos(Convert.ToDouble(args.Parameters[0].Evaluate()));
                        break;
                    case "exp":
                        args.Result = Math.Exp(Convert.ToDouble(args.Parameters[0].Evaluate()));
                        break;
                    case "sqrt":
                        args.Result = Math.Sqrt(Convert.ToDouble(args.Parameters[0].Evaluate()));
                        break;
                    case "abs":
                        args.Result = Math.Abs(Convert.ToDouble(args.Parameters[0].Evaluate()));
                        break;
                    case "log":
                        if (args.Parameters.Length == 1)
                            args.Result = Math.Log(Convert.ToDouble(args.Parameters[0].Evaluate()));
                        else if (args.Parameters.Length == 2)
                            args.Result = Math.Log(Convert.ToDouble(args.Parameters[0].Evaluate()),
                                                 Convert.ToDouble(args.Parameters[1].Evaluate()));
                        else throw new ArgumentException("Функция log требует 1 или 2 аргумента");
                        break;
                    case "log10":
                        if (args.Parameters.Length == 1)
                            args.Result = Math.Log10(Convert.ToDouble(args.Parameters[0].Evaluate()));
                        else throw new ArgumentException("Функция log10 требует 1 аргумент");
                        break;
                    case "pow":
                        if (args.Parameters.Length == 2)
                            args.Result = Math.Pow(Convert.ToDouble(args.Parameters[0].Evaluate()),
                                                 Convert.ToDouble(args.Parameters[1].Evaluate()));
                        else throw new ArgumentException("Функция pow требует 2 аргумента");
                        break;
                    case "floor":
                        args.Result = Math.Floor(Convert.ToDouble(args.Parameters[0].Evaluate()));
                        break;
                    case "ceil":
                        args.Result = Math.Ceiling(Convert.ToDouble(args.Parameters[0].Evaluate()));
                        break;
                    case "round":
                        if (args.Parameters.Length == 1)
                            args.Result = Math.Round(Convert.ToDouble(args.Parameters[0].Evaluate()));
                        else if (args.Parameters.Length == 2)
                            args.Result = Math.Round(Convert.ToDouble(args.Parameters[0].Evaluate()),
                                                   Convert.ToInt32(args.Parameters[1].Evaluate()));
                        else throw new ArgumentException("Функция round требует 1 или 2 аргумента");
                        break;
                    case "min":
                        if (args.Parameters.Length == 2)
                            args.Result = Math.Min(Convert.ToDouble(args.Parameters[0].Evaluate()),
                                                 Convert.ToDouble(args.Parameters[1].Evaluate()));
                        else throw new ArgumentException("Функция min требует 2 аргумента");
                        break;
                    case "max":
                        if (args.Parameters.Length == 2)
                            args.Result = Math.Max(Convert.ToDouble(args.Parameters[0].Evaluate()),
                                                 Convert.ToDouble(args.Parameters[1].Evaluate()));
                        else throw new ArgumentException("Функция max требует 2 аргумента");
                        break;
                    default:
                        throw new ArgumentException($"Неизвестная функция: {name}");
                }
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Ошибка в функции {name}: {ex.Message}");
            }
        }

        public double CalculateFunction(double x, double y)
        {
            _functionEvaluations++;
            try
            {
                if (double.IsInfinity(x) || double.IsInfinity(y) ||
                    Math.Abs(x) > 1e100 || Math.Abs(y) > 1e100)
                    return double.MaxValue;

                _expression.Parameters["x"] = x;
                _expression.Parameters["y"] = y;
                var result = _expression.Evaluate();

                if (result is double doubleResult)
                {
                    if (double.IsInfinity(doubleResult) || double.IsNaN(doubleResult))
                        return double.MaxValue;
                    return doubleResult;
                }

                return Convert.ToDouble(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка вычисления в точке ({x}, {y}): {ex.Message}");
                return double.MaxValue;
            }
        }

        private double OneDimensionalSearch(double a, double b, Func<double, double> function,
                                          bool findMinimum, double epsilon)
        {
            double left = a;
            double right = b;

            while (Math.Abs(right - left) > epsilon)
            {
                double mid = (left + right) / 2;
                double fMid = function(mid);
                double fMidPlus = function(mid + epsilon / 10);

                if ((findMinimum && fMidPlus < fMid) || (!findMinimum && fMidPlus > fMid))
                {
                    left = mid;
                }
                else
                {
                    right = mid;
                }
            }

            return (left + right) / 2;
        }

        public CoordinateDescentResult FindExtremum(double xStart, double xEnd, double yStart, double yEnd,
                                                   double epsilon, bool findMinimum,
                                                   double? startX = null, double? startY = null,
                                                   int maxIterations = 1000,
                                                   double lambda = 1.0)
        {
            _functionEvaluations = 0;
            _stopwatch.Restart();

            if (xStart >= xEnd || yStart >= yEnd)
                throw new ArgumentException("Некорректные интервалы");

            if (epsilon <= 0) throw new ArgumentException("Точность должна быть положительной");
            if (lambda <= 0) throw new ArgumentException("Длина шага λ должна быть положительной");

            double currentX = startX ?? (xStart + xEnd) / 2;
            double currentY = startY ?? (yStart + yEnd) / 2;

            currentX = Math.Max(xStart, Math.Min(currentX, xEnd));
            currentY = Math.Max(yStart, Math.Min(currentY, yEnd));

            double currentValue = CalculateFunction(currentX, currentY);

            var iterations = new List<CoordinateDescentIteration>
            {
                new CoordinateDescentIteration
                {
                    Iteration = 0,
                    X = currentX,
                    Y = currentY,
                    Value = currentValue,
                    Direction = "Начальная точка"
                }
            };

            var convergenceHistory = new List<double> { currentValue };
            bool converged = false;
            int iterationCount = 0;
            bool boundaryWarning = false;
            double bestValue = currentValue;
            double bestX = currentX;
            double bestY = currentY;

            while (iterationCount < maxIterations && !converged)
            {
                iterationCount++;

                double prevX = currentX;
                double prevY = currentY;
                double prevValue = currentValue;

                bool searchInX = (iterationCount % 2 == 1);
                string direction = searchInX ? "По X" : "По Y";

                if (searchInX)
                {
                    double xLeft = currentX - epsilon;
                    double xRight = currentX + epsilon;

                    xLeft = Math.Max(xStart, xLeft);
                    xRight = Math.Min(xEnd, xRight);

                    double fLeft = CalculateFunction(xLeft, currentY);
                    double fRight = CalculateFunction(xRight, currentY);

                    double searchStart, searchEnd;

                    if ((findMinimum && fRight < fLeft) || (!findMinimum && fRight > fLeft))
                    {
                        searchStart = currentX;
                        searchEnd = Math.Min(currentX + lambda, xEnd);
                    }
                    else
                    {
                        searchStart = Math.Max(currentX - lambda, xStart);
                        searchEnd = currentX;
                    }

                    if (Math.Abs(searchEnd - searchStart) > epsilon)
                    {
                        double xOpt = OneDimensionalSearch(searchStart, searchEnd,
                            x => CalculateFunction(x, currentY), findMinimum, epsilon);

                        double fOpt = CalculateFunction(xOpt, currentY);

                        if ((findMinimum && fOpt < currentValue) || (!findMinimum && fOpt > currentValue))
                        {
                            currentX = xOpt;
                            currentValue = fOpt;
                        }
                    }
                }
                else
                {
                    double yBottom = currentY - epsilon;
                    double yTop = currentY + epsilon;

                    yBottom = Math.Max(yStart, yBottom);
                    yTop = Math.Min(yEnd, yTop);

                    double fBottom = CalculateFunction(currentX, yBottom);
                    double fTop = CalculateFunction(currentX, yTop);

                    double searchStart, searchEnd;

                    if ((findMinimum && fTop < fBottom) || (!findMinimum && fTop > fBottom))
                    {
                        searchStart = currentY;
                        searchEnd = Math.Min(currentY + lambda, yEnd);
                    }
                    else
                    {
                        searchStart = Math.Max(currentY - lambda, yStart);
                        searchEnd = currentY;
                    }

                    if (Math.Abs(searchEnd - searchStart) > epsilon)
                    {
                        double yOpt = OneDimensionalSearch(searchStart, searchEnd,
                            y => CalculateFunction(currentX, y), findMinimum, epsilon);

                        double fOpt = CalculateFunction(currentX, yOpt);

                        if ((findMinimum && fOpt < currentValue) || (!findMinimum && fOpt > currentValue))
                        {
                            currentY = yOpt;
                            currentValue = fOpt;
                        }
                    }
                }

                if ((findMinimum && currentValue < bestValue) || (!findMinimum && currentValue > bestValue))
                {
                    bestValue = currentValue;
                    bestX = currentX;
                    bestY = currentY;
                }

                double deltaX = Math.Abs(currentX - prevX);
                double deltaY = Math.Abs(currentY - prevY);
                double deltaValue = Math.Abs(currentValue - prevValue);

                double distance = Math.Sqrt(deltaX * deltaX + deltaY * deltaY);

                iterations.Add(new CoordinateDescentIteration
                {
                    Iteration = iterationCount,
                    X = currentX,
                    Y = currentY,
                    Value = currentValue,
                    DeltaX = deltaX,
                    DeltaY = deltaY,
                    DeltaValue = deltaValue,
                    Direction = direction
                });

                convergenceHistory.Add(currentValue);

                converged = distance < 2 * epsilon;

                bool isOnXBoundary = Math.Abs(currentX - xStart) < epsilon || Math.Abs(currentX - xEnd) < epsilon;
                bool isOnYBoundary = Math.Abs(currentY - yStart) < epsilon || Math.Abs(currentY - yEnd) < epsilon;
                boundaryWarning = isOnXBoundary || isOnYBoundary;

                if (double.IsNaN(currentValue) || double.IsInfinity(currentValue))
                {
                    converged = true;
                    currentX = bestX;
                    currentY = bestY;
                    currentValue = bestValue;
                    break;
                }
            }

            _stopwatch.Stop();

            return new CoordinateDescentResult
            {
                X = bestX,
                Y = bestY,
                Value = bestValue,
                Iterations = iterationCount,
                Epsilon = epsilon,
                Converged = converged || iterationCount >= maxIterations,
                BoundaryWarning = boundaryWarning && iterationCount < maxIterations,
                IterationHistory = iterations,
                ConvergenceHistory = convergenceHistory,
                TotalFunctionEvaluations = _functionEvaluations,
                ElapsedTime = _stopwatch.Elapsed.TotalSeconds
            };
        }

        public List<Point3D> GenerateSurfaceData(double xStart, double xEnd, double yStart, double yEnd, int resolution = 30)
        {
            var points = new List<Point3D>();

            if (resolution < 2) resolution = 2;

            double xStep = (xEnd - xStart) / resolution;
            double yStep = (yEnd - yStart) / resolution;

            for (int i = 0; i <= resolution; i++)
            {
                double y = yStart + i * yStep;
                for (int j = 0; j <= resolution; j++)
                {
                    double x = xStart + j * xStep;
                    try
                    {
                        double z = CalculateFunction(x, y);
                        if (!double.IsNaN(z) && !double.IsInfinity(z) && Math.Abs(z) < 1e50)
                        {
                            points.Add(new Point3D(x, y, z));
                        }
                        else
                        {
                            points.Add(new Point3D(x, y, 0));
                        }
                    }
                    catch
                    {
                        points.Add(new Point3D(x, y, 0));
                    }
                }
            }

            return points;
        }

        public List<List<Point3D>> GenerateContourLines(double xStart, double xEnd, double yStart, double yEnd,
                                                       int numContours = 5, int resolution = 20)
        {
            var contours = new List<List<Point3D>>();

            try
            {
                if (numContours < 1) numContours = 1;
                if (resolution < 2) resolution = 2;

                double minZ = double.MaxValue;
                double maxZ = double.MinValue;
                List<double> validValues = new List<double>();

                for (int i = 0; i <= resolution; i++)
                {
                    double x = xStart + (xEnd - xStart) * i / resolution;
                    for (int j = 0; j <= resolution; j++)
                    {
                        double y = yStart + (yEnd - yStart) * j / resolution;
                        try
                        {
                            double z = CalculateFunction(x, y);
                            if (!double.IsNaN(z) && !double.IsInfinity(z) && Math.Abs(z) < 1e50)
                            {
                                validValues.Add(z);
                                minZ = Math.Min(minZ, z);
                                maxZ = Math.Max(maxZ, z);
                            }
                        }
                        catch { }
                    }
                }

                if (validValues.Count < 2 || maxZ - minZ < 1e-10)
                    return contours;

                double step = (maxZ - minZ) / (numContours + 1);

                for (int level = 0; level < numContours; level++)
                {
                    double contourValue = minZ + (level + 1) * step;
                    var contour = new List<Point3D>();

                    for (int i = 0; i < resolution; i++)
                    {
                        double x1 = xStart + (xEnd - xStart) * i / resolution;
                        double x2 = xStart + (xEnd - xStart) * (i + 1) / resolution;

                        for (int j = 0; j < resolution; j++)
                        {
                            double y1 = yStart + (yEnd - yStart) * j / resolution;
                            double y2 = yStart + (yEnd - yStart) * (j + 1) / resolution;

                            double z1 = CalculateFunction(x1, y1);
                            double z2 = CalculateFunction(x2, y1);
                            double z3 = CalculateFunction(x1, y2);
                            double z4 = CalculateFunction(x2, y2);

                            if (!double.IsNaN(z1) && !double.IsNaN(z2) &&
                                (z1 - contourValue) * (z2 - contourValue) <= 0)
                            {
                                double t = (contourValue - z1) / (z2 - z1);
                                double x = x1 + t * (x2 - x1);
                                contour.Add(new Point3D(x, y1, contourValue));
                            }

                            if (!double.IsNaN(z1) && !double.IsNaN(z3) &&
                                (z1 - contourValue) * (z3 - contourValue) <= 0)
                            {
                                double t = (contourValue - z1) / (z3 - z1);
                                double y = y1 + t * (y2 - y1);
                                contour.Add(new Point3D(x1, y, contourValue));
                            }
                        }
                    }

                    if (contour.Count > 1)
                    {
                        contours.Add(contour);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка создания линий уровня: {ex.Message}");
            }

            return contours;
        }

        public (double gradX, double gradY) CalculateGradient(double x, double y, double step = 1e-6)
        {
            double f_xplus = CalculateFunction(x + step, y);
            double f_xminus = CalculateFunction(x - step, y);
            double f_yplus = CalculateFunction(x, y + step);
            double f_yminus = CalculateFunction(x, y - step);

            double gradX = (f_xplus - f_xminus) / (2 * step);
            double gradY = (f_yplus - f_yminus) / (2 * step);

            return (gradX, gradY);
        }

        public (double value, double gradX, double gradY) CalculateFunctionWithGradient(double x, double y, double step = 1e-6)
        {
            double value = CalculateFunction(x, y);
            var (gradX, gradY) = CalculateGradient(x, y, step);

            return (value, gradX, gradY);
        }
    }

    public class CoordinateDescentResult
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Value { get; set; }
        public int Iterations { get; set; }
        public double Epsilon { get; set; }
        public bool Converged { get; set; }
        public bool BoundaryWarning { get; set; }
        public List<CoordinateDescentIteration> IterationHistory { get; set; }
        public List<double> ConvergenceHistory { get; set; }
        public int TotalFunctionEvaluations { get; set; }
        public double ElapsedTime { get; set; }

        public int GetPrecisionDigits()
        {
            if (Epsilon <= 0) return 6;
            int order = (int)Math.Ceiling(-Math.Log10(Epsilon));
            return Math.Max(1, Math.Min(order, 15));
        }

        public string GetFormattedX() => X.ToString($"F{GetPrecisionDigits()}");
        public string GetFormattedY() => Y.ToString($"F{GetPrecisionDigits()}");
        public string GetFormattedValue() => Value.ToString($"F{GetPrecisionDigits()}");
        public string GetStatus()
        {
            string status = Converged ? "Сошелся" : "Достигнут лимит итераций";
            if (BoundaryWarning)
                status += " (возможно на границе)";
            return status;
        }
    }

    public class CoordinateDescentIteration
    {
        public int Iteration { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double Value { get; set; }
        public double DeltaX { get; set; }
        public double DeltaY { get; set; }
        public double DeltaValue { get; set; }
        public string Direction { get; set; }
    }
}