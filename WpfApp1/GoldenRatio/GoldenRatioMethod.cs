using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using NCalc;

namespace WpfApp1
{
    public class GoldenRatioMethod
    {
        private readonly Expression _expression;
        public int IterationsCount { get; private set; }
        private const double GoldenRatio = 1.618033988749895;

        public GoldenRatioMethod(string function)
        {
            string processedFunction = ProcessFunctionForNCalc(function);
            Console.WriteLine($"Преобразованная функция: {processedFunction}");

            _expression = new Expression(processedFunction, EvaluateOptions.IgnoreCase);

            _expression.Parameters["pi"] = Math.PI;
            _expression.Parameters["e"] = Math.E;

            _expression.EvaluateFunction += EvaluateFunction;
            _expression.EvaluateParameter += EvaluateParameter;
        }

        private string ProcessFunctionForNCalc(string function)
        {
            if (string.IsNullOrWhiteSpace(function))
                return "x";

            string result = function.Trim();

            Console.WriteLine($"Исходная функция: {result}");

            result = result.Replace(",", ".");

            result = SimpleExponentialConversion(result);

            result = SimpleMultiplication(result);

            result = SimplePowerConversion(result);

            result = result.Replace(" ", "");

            Console.WriteLine($"После обработки: {result}");

            return result;
        }

        private string SimpleExponentialConversion(string expression)
        {
            string result = expression;

            result = result.Replace("e^(", "exp(");
            result = result.Replace("e ^ (", "exp(");
            result = result.Replace("e ^(", "exp(");
            result = result.Replace("e^ (", "exp(");

            int eIndex = result.IndexOf("e^", StringComparison.OrdinalIgnoreCase);
            while (eIndex >= 0)
            {
                int start = eIndex + 2;
                if (start < result.Length)
                {
                    int end = start;
                    int bracketCount = 0;

                    if (start < result.Length && result[start] == '(')
                    {
                        bracketCount = 1;
                        end = start + 1;

                        while (end < result.Length && bracketCount > 0)
                        {
                            if (result[end] == '(') bracketCount++;
                            if (result[end] == ')') bracketCount--;
                            end++;
                        }
                    }
                    else
                    {
                        while (end < result.Length &&
                               (char.IsLetterOrDigit(result[end]) ||
                                result[end] == '.' || result[end] == '+' ||
                                result[end] == '-' || result[end] == '*' ||
                                result[end] == '/'))
                        {
                            end++;
                        }
                    }

                    string innerExpr = result.Substring(start, end - start);
                    string replacement = $"exp({innerExpr})";

                    result = result.Remove(eIndex, (end - eIndex)).Insert(eIndex, replacement);
                }

                eIndex = result.IndexOf("e^", eIndex + 1, StringComparison.OrdinalIgnoreCase);
            }

            return result;
        }

        private string SimpleMultiplication(string expression)
        {
            string result = expression;

            result = Regex.Replace(result, @"(\d)(\()", "$1*$2");
            result = Regex.Replace(result, @"(\))(\()", "$1*$2");

            return result;
        }

        private string SimplePowerConversion(string expression)
        {
            string result = expression;

            int caretIndex = result.IndexOf('^');
            while (caretIndex >= 0)
            {
                int leftStart = caretIndex - 1;
                while (leftStart >= 0 &&
                       (char.IsLetterOrDigit(result[leftStart]) ||
                        result[leftStart] == 'x' || result[leftStart] == 'y' ||
                        result[leftStart] == ')' || result[leftStart] == '.'))
                {
                    leftStart--;
                }
                leftStart++;

                int rightEnd = caretIndex + 1;
                while (rightEnd < result.Length &&
                       (char.IsLetterOrDigit(result[rightEnd]) ||
                        result[rightEnd] == 'x' || result[rightEnd] == 'y' ||
                        result[rightEnd] == '(' || result[rightEnd] == '.' ||
                        result[rightEnd] == '+' || result[rightEnd] == '-' ||
                        result[rightEnd] == '*' || result[rightEnd] == '/'))
                {
                    rightEnd++;
                }

                string left = result.Substring(leftStart, caretIndex - leftStart);
                string right = result.Substring(caretIndex + 1, rightEnd - caretIndex - 1);

                left = left.Trim();
                right = right.Trim();

                string replacement = $"pow({left},{right})";
                result = result.Remove(leftStart, rightEnd - leftStart).Insert(leftStart, replacement);

                caretIndex = result.IndexOf('^');
            }

            return result;
        }

        private void EvaluateParameter(string name, ParameterArgs args)
        {
            switch (name.ToLower())
            {
                case "pi":
                    args.Result = Math.PI;
                    break;
                case "e":
                    args.Result = Math.E;
                    break;
            }
        }

        private void EvaluateFunction(string name, FunctionArgs args)
        {
            try
            {
                switch (name.ToLower())
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
                        args.Result = Math.Atan(Convert.ToDouble(args.Parameters[0].Evaluate()));
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
                        {
                            args.Result = Math.Log(Convert.ToDouble(args.Parameters[0].Evaluate()));
                        }
                        else if (args.Parameters.Length == 2)
                        {
                            args.Result = Math.Log(Convert.ToDouble(args.Parameters[0].Evaluate()),
                                                 Convert.ToDouble(args.Parameters[1].Evaluate()));
                        }
                        else
                        {
                            throw new ArgumentException("Функция log требует 1 или 2 аргумента");
                        }
                        break;
                    case "log10":
                        if (args.Parameters.Length == 1)
                        {
                            args.Result = Math.Log10(Convert.ToDouble(args.Parameters[0].Evaluate()));
                        }
                        else
                        {
                            throw new ArgumentException("Функция log10 требует 1 аргумент");
                        }
                        break;
                    case "pow":
                        if (args.Parameters.Length == 2)
                        {
                            double baseVal = Convert.ToDouble(args.Parameters[0].Evaluate());
                            double exponent = Convert.ToDouble(args.Parameters[1].Evaluate());

                            if (Math.Abs(baseVal) < 1e-15 && exponent < 0)
                                throw new ArgumentException("Деление на ноль");

                            args.Result = Math.Pow(baseVal, exponent);
                        }
                        else
                        {
                            throw new ArgumentException("Функция pow требует 2 аргумента");
                        }
                        break;
                    default:
                        throw new ArgumentException($"Неизвестная функция: {name}");
                }
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Ошибка вычисления функции {name}: {ex.Message}");
            }
        }

        public double CalculateFunction(double x)
        {
            try
            {
                if (Math.Abs(x) > 1e10)
                {
                    return x > 0 ? double.MaxValue / 1000 : double.MinValue / 1000;
                }

                _expression.Parameters["x"] = x;
                var result = _expression.Evaluate();

                if (result is double doubleResult)
                {
                    if (double.IsInfinity(doubleResult) || double.IsNaN(doubleResult))
                    {
                        return double.MaxValue / 1000;
                    }
                    return doubleResult;
                }

                if (result is int intResult)
                {
                    return intResult;
                }

                if (result is decimal decimalResult)
                {
                    return (double)decimalResult;
                }

                return Convert.ToDouble(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка в CalculateFunction(x={x}): {ex.Message}");
                throw new ArgumentException($"Ошибка вычисления функции: {ex.Message}");
            }
        }

        public GoldenRatioResult FindMinimum(double a, double b, double epsilon)
        {
            ValidateInterval(a, b, epsilon);
            IterationsCount = 0;

            double x1 = b - (b - a) / GoldenRatio;
            double x2 = a + (b - a) / GoldenRatio;

            double f1 = CalculateFunction(x1);
            double f2 = CalculateFunction(x2);

            while (Math.Abs(b - a) > epsilon)
            {
                IterationsCount++;

                if (f1 >= f2)
                {
                    a = x1;
                    x1 = x2;
                    f1 = f2;
                    x2 = a + (b - a) / GoldenRatio;
                    f2 = CalculateFunction(x2);
                }
                else
                {
                    b = x2;
                    x2 = x1;
                    f2 = f1;
                    x1 = b - (b - a) / GoldenRatio;
                    f1 = CalculateFunction(x1);
                }

                if (IterationsCount > 10000)
                {
                    break;
                }
            }

            double extremumPoint = (a + b) / 2;
            double extremumValue = CalculateFunction(extremumPoint);

            int decimalPlaces = GetDecimalPlaces(epsilon);

            return new GoldenRatioResult
            {
                ExtremumPoint = Math.Round(extremumPoint, decimalPlaces),
                ExtremumValue = Math.Round(extremumValue, decimalPlaces),
                Iterations = IterationsCount,
                FinalInterval = (Math.Round(a, decimalPlaces), Math.Round(b, decimalPlaces))
            };
        }

        public GoldenRatioResult FindMaximum(double a, double b, double epsilon)
        {
            ValidateInterval(a, b, epsilon);
            IterationsCount = 0;

            double x1 = b - (b - a) / GoldenRatio;
            double x2 = a + (b - a) / GoldenRatio;

            double f1 = CalculateFunction(x1);
            double f2 = CalculateFunction(x2);

            while (Math.Abs(b - a) > epsilon)
            {
                IterationsCount++;

                if (f1 <= f2)
                {
                    a = x1;
                    x1 = x2;
                    f1 = f2;
                    x2 = a + (b - a) / GoldenRatio;
                    f2 = CalculateFunction(x2);
                }
                else
                {
                    b = x2;
                    x2 = x1;
                    f2 = f1;
                    x1 = b - (b - a) / GoldenRatio;
                    f1 = CalculateFunction(x1);
                }

                if (IterationsCount > 10000)
                {
                    break;
                }
            }

            double extremumPoint = (a + b) / 2;
            double extremumValue = CalculateFunction(extremumPoint);

            int decimalPlaces = GetDecimalPlaces(epsilon);

            return new GoldenRatioResult
            {
                ExtremumPoint = Math.Round(extremumPoint, decimalPlaces),
                ExtremumValue = Math.Round(extremumValue, decimalPlaces),
                Iterations = IterationsCount,
                FinalInterval = (Math.Round(a, decimalPlaces), Math.Round(b, decimalPlaces))
            };
        }

        public GoldenRatioResult FindRoot(double a, double b, double epsilon)
        {
            ValidateInterval(a, b, epsilon);

            double fa = CalculateFunction(a);
            double fb = CalculateFunction(b);

            int decimalPlaces = GetDecimalPlaces(epsilon);

            if (Math.Abs(fa) < epsilon)
            {
                return new GoldenRatioResult
                {
                    ExtremumPoint = Math.Round(a, decimalPlaces),
                    ExtremumValue = Math.Round(fa, decimalPlaces),
                    Iterations = 0,
                    FinalInterval = (Math.Round(a, decimalPlaces), Math.Round(b, decimalPlaces))
                };
            }

            if (Math.Abs(fb) < epsilon)
            {
                return new GoldenRatioResult
                {
                    ExtremumPoint = Math.Round(b, decimalPlaces),
                    ExtremumValue = Math.Round(fb, decimalPlaces),
                    Iterations = 0,
                    FinalInterval = (Math.Round(a, decimalPlaces), Math.Round(b, decimalPlaces))
                };
            }

            if (fa * fb > 0)
            {
                throw new ArgumentException($"Функция не меняет знак на интервале [{a}, {b}]. f(a)={fa:F6}, f(b)={fb:F6}");
            }

            IterationsCount = 0;

            while (Math.Abs(b - a) > epsilon)
            {
                IterationsCount++;

                double mid = (a + b) / 2;
                double fmid = CalculateFunction(mid);

                if (Math.Abs(fmid) < epsilon)
                {
                    a = b = mid;
                    break;
                }

                if (fa * fmid < 0)
                {
                    b = mid;
                    fb = fmid;
                }
                else
                {
                    a = mid;
                    fa = fmid;
                }

                if (IterationsCount > 10000)
                {
                    break;
                }
            }

            double root = (a + b) / 2;
            double fRoot = CalculateFunction(root);

            return new GoldenRatioResult
            {
                ExtremumPoint = Math.Round(root, decimalPlaces),
                ExtremumValue = Math.Round(fRoot, decimalPlaces),
                Iterations = IterationsCount,
                FinalInterval = (Math.Round(a, decimalPlaces), Math.Round(b, decimalPlaces))
            };
        }

        private void ValidateInterval(double a, double b, double epsilon)
        {
            if (a >= b)
            {
                throw new ArgumentException("Начало интервала a должно быть меньше конца интервала b");
            }

            if (epsilon <= 0)
            {
                throw new ArgumentException("Точность epsilon должна быть положительной");
            }

            if (epsilon > Math.Abs(b - a))
            {
                throw new ArgumentException("Точность epsilon не должна превышать длину интервала");
            }
        }

        private int GetDecimalPlaces(double epsilon)
        {
            if (epsilon <= 0) return 6;

            string epsilonStr = epsilon.ToString(CultureInfo.InvariantCulture);

            if (epsilonStr.Contains('.'))
            {
                int decimalPlaces = epsilonStr.Split('.')[1].Length;
                return Math.Min(Math.Max(decimalPlaces, 3), 12);
            }
            else if (epsilonStr.Contains(','))
            {
                int decimalPlaces = epsilonStr.Split(',')[1].Length;
                return Math.Min(Math.Max(decimalPlaces, 3), 12);
            }

            return 6;
        }

        public List<GoldenRatioResult> FindAllExtremums(double a, double b, double epsilon, bool findMinimum = true)
        {
            List<GoldenRatioResult> results = new List<GoldenRatioResult>();

            int subdivisions = 10;
            double step = (b - a) / subdivisions;

            for (int i = 0; i < subdivisions; i++)
            {
                double start = a + i * step;
                double end = Math.Min(b, start + step);

                if (end - start < epsilon) continue;

                try
                {
                    GoldenRatioResult result = findMinimum ?
                        FindMinimum(start, end, epsilon) :
                        FindMaximum(start, end, epsilon);

                    if (result.ExtremumPoint > start + epsilon && result.ExtremumPoint < end - epsilon)
                    {
                        bool isDuplicate = false;
                        foreach (var existing in results)
                        {
                            if (Math.Abs(existing.ExtremumPoint - result.ExtremumPoint) < epsilon * 10)
                            {
                                isDuplicate = true;
                                break;
                            }
                        }

                        if (!isDuplicate)
                        {
                            results.Add(result);
                        }
                    }
                }
                catch
                {
                    // Пропускаем проблемные интервалы
                }
            }

            return results;
        }

        public GoldenRatioResult FindGlobalExtremum(double a, double b, double epsilon, bool findMinimum = true)
        {
            try
            {
                var allExtremums = FindAllExtremums(a, b, epsilon, findMinimum);

                if (allExtremums.Count > 0)
                {
                    GoldenRatioResult global = allExtremums[0];
                    foreach (var extremum in allExtremums)
                    {
                        if (findMinimum && extremum.ExtremumValue < global.ExtremumValue)
                        {
                            global = extremum;
                        }
                        else if (!findMinimum && extremum.ExtremumValue > global.ExtremumValue)
                        {
                            global = extremum;
                        }
                    }
                    return global;
                }
            }
            catch
            {
            }

            return findMinimum ? FindMinimum(a, b, epsilon) : FindMaximum(a, b, epsilon);
        }

        public bool TestFunctionOnInterval(double a, double b)
        {
            try
            {
                double[] testPoints = { a, (a + b) / 2, b };

                foreach (double x in testPoints)
                {
                    double value = CalculateFunction(x);
                    if (double.IsNaN(value) || double.IsInfinity(value))
                    {
                        return false;
                    }
                }
                return true;
            }
            catch
            {
                return false;
            }
        }
    }

    public class GoldenRatioResult
    {
        public double ExtremumPoint { get; set; }
        public double ExtremumValue { get; set; }
        public int Iterations { get; set; }
        public (double a, double b) FinalInterval { get; set; }
    }
}