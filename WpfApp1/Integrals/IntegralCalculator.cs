using System;
using System.Collections.Generic;
using System.Linq;
using NCalc;

namespace WpfApp1
{
    public enum IntegrationMethod
    {
        RectangleLeft,
        RectangleRight,
        RectangleMidpoint,
        Trapezoidal,
        Simpson
    }

    public class IntegrationResult
    {
        public IntegrationMethod Method { get; set; }
        public double Value { get; set; }
        public int Iterations { get; set; }
        public double ErrorEstimate { get; set; }
        public List<double> History { get; set; } = new List<double>();
        public List<int> HistoryN { get; set; } = new List<int>();
    }

    public class IntegralCalculator
    {
        private readonly Expression _expression;

        public IntegralCalculator(string function)
        {
            _expression = new Expression(function.ToLower(), EvaluateOptions.IgnoreCase);
            _expression.Parameters["pi"] = Math.PI;
            _expression.Parameters["e"] = Math.E;
            _expression.EvaluateFunction += EvaluateFunction;
        }

        public double CalculateFunction(double x)
        {
            try
            {
                _expression.Parameters["x"] = x;
                var result = _expression.Evaluate();

                if (result is double d)
                {
                    if (double.IsNaN(d) || double.IsInfinity(d))
                        throw new ArgumentException($"Неопределенное значение функции в точке x={x}");
                    return d;
                }
                if (result is int i) return i;
                if (result is decimal m) return (double)m;
                return Convert.ToDouble(result);
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Ошибка вычисления функции в точке x={x}: {ex.Message}");
            }
        }

        public Dictionary<IntegrationMethod, IntegrationResult> CalculateWithFixedN(
            double a, double b, int n, List<IntegrationMethod> methods)
        {
            var results = new Dictionary<IntegrationMethod, IntegrationResult>();

            foreach (var method in methods)
            {
                double value;
                int actualN = n;

                if (method == IntegrationMethod.Simpson && n % 2 != 0)
                {
                    actualN = n + 1;
                }

                value = CalculateWithExactN(a, b, actualN, method);

                results[method] = new IntegrationResult
                {
                    Method = method,
                    Value = value,
                    Iterations = actualN,
                    ErrorEstimate = 0 
                };
            }

            return results;
        }

        public Dictionary<IntegrationMethod, IntegrationResult> CalculateWithAutoN(
            double a, double b, double epsilon, int initialN, List<IntegrationMethod> methods)
        {
            var results = new Dictionary<IntegrationMethod, IntegrationResult>();

            foreach (var method in methods)
            {
                var result = FindOptimalNForMethod(a, b, epsilon, initialN, method);
                results[method] = result;
            }

            return results;
        }

        private IntegrationResult FindOptimalNForMethod(double a, double b, double epsilon, int startN, IntegrationMethod method)
        {
            int n = GetMinimumNForMethod(method, startN);

            var result = new IntegrationResult
            {
                Method = method,
                History = new List<double>(),
                HistoryN = new List<int>()
            };

            int maxIterations = 500; 
            double currentValue = 0;
            double previousValue = 0;
            bool precisionAchieved = false;
            int stabilizationCount = 0;
            const int maxStabilizations = 3; 

            for (int iteration = 0; iteration < maxIterations; iteration++)
            {
                if (method == IntegrationMethod.Simpson && n % 2 != 0)
                {
                    n++;
                }

                try
                {
                    currentValue = CalculateWithExactN(a, b, n, method);
                }
                catch (Exception)
                {
                    n = method == IntegrationMethod.Simpson ? n + 2 : n + 1;
                    continue;
                }

                result.History.Add(currentValue);
                result.HistoryN.Add(n);

                if (iteration > 0)
                {
                    double change = Math.Abs(currentValue - previousValue);

                    if (change <= epsilon)
                    {
                        stabilizationCount++;

                        if (stabilizationCount >= maxStabilizations)
                        {
                            precisionAchieved = true;
                        }
                    }
                    else
                    {
                        stabilizationCount = 0;
                    }

                    int nextN = method == IntegrationMethod.Simpson ? n + 2 : n + 1;
                    double nextValue;

                    try
                    {
                        nextValue = CalculateWithExactN(a, b, nextN, method);
                    }
                    catch (Exception)
                    {
                        nextValue = currentValue;
                    }

                    double nextChange = Math.Abs(nextValue - currentValue);

                    if (nextChange <= epsilon / 10)
                    {
                        stabilizationCount++;
                        if (stabilizationCount >= maxStabilizations)
                        {
                            precisionAchieved = true;
                        }
                    }
                }

                previousValue = currentValue;

                if (precisionAchieved || n >= 1000000)
                {
                    result.Value = currentValue;
                    result.Iterations = n;

                    if (result.History.Count >= 2)
                    {
                        double order = GetMethodOrder(method);
                        double lastChange = Math.Abs(currentValue - result.History[result.History.Count - 2]);
                        result.ErrorEstimate = lastChange / (Math.Pow(2, order) - 1);
                    }
                    else
                    {
                        result.ErrorEstimate = 0;
                    }

                    return result;
                }

                if (method == IntegrationMethod.Simpson)
                {
                    n += 2;
                }
                else
                {
                    if (method == IntegrationMethod.RectangleLeft || method == IntegrationMethod.RectangleRight)
                    {
                        n += Math.Max(1, n / 10); 
                    }
                    else
                    {
                        n += 1;
                    }
                }
            }

            result.Value = currentValue;
            result.Iterations = n;

            if (result.History.Count >= 2)
            {
                double order = GetMethodOrder(method);
                double lastChange = Math.Abs(currentValue - result.History[result.History.Count - 2]);
                result.ErrorEstimate = lastChange / (Math.Pow(2, order) - 1);
            }
            else
            {
                result.ErrorEstimate = double.MaxValue;
            }

            return result;
        }

        private int GetMinimumNForMethod(IntegrationMethod method, int initialN)
        {
            int minN = Math.Max(1, initialN);

            switch (method)
            {
                case IntegrationMethod.Simpson:
                    minN = Math.Max(2, minN);
                    if (minN % 2 != 0) minN++;
                    break;
                case IntegrationMethod.Trapezoidal:
                    minN = Math.Max(2, minN);
                    break;
                default: 
                    minN = Math.Max(1, minN);
                    break;
            }

            return minN;
        }

        private double GetMethodOrder(IntegrationMethod method)
        {
            return method switch
            {
                IntegrationMethod.RectangleLeft => 1.0,
                IntegrationMethod.RectangleRight => 1.0,
                IntegrationMethod.RectangleMidpoint => 2.0,
                IntegrationMethod.Trapezoidal => 2.0,
                IntegrationMethod.Simpson => 4.0,
                _ => 1.0
            };
        }

        private double CalculateWithExactN(double a, double b, int n, IntegrationMethod method)
        {
            if (n <= 0) throw new ArgumentException("Количество разбиений должно быть положительным");

            double h = (b - a) / n;

            return method switch
            {
                IntegrationMethod.RectangleLeft => RectangleLeft(a, h, n),
                IntegrationMethod.RectangleRight => RectangleRight(a, h, n),
                IntegrationMethod.RectangleMidpoint => RectangleMidpoint(a, h, n),
                IntegrationMethod.Trapezoidal => Trapezoidal(a, b, h, n),
                IntegrationMethod.Simpson => Simpson(a, b, h, n),
                _ => throw new ArgumentException("Неизвестный метод интегрирования")
            };
        }

        private double RectangleLeft(double a, double h, int n)
        {
            double sum = 0;
            for (int i = 0; i < n; i++)
            {
                double x = a + i * h;
                sum += CalculateFunction(x);
            }
            return h * sum;
        }

        private double RectangleRight(double a, double h, int n)
        {
            double sum = 0;
            for (int i = 1; i <= n; i++)
            {
                double x = a + i * h;
                sum += CalculateFunction(x);
            }
            return h * sum;
        }

        private double RectangleMidpoint(double a, double h, int n)
        {
            double sum = 0;
            for (int i = 0; i < n; i++)
            {
                double x = a + (i + 0.5) * h;
                sum += CalculateFunction(x);
            }
            return h * sum;
        }

        private double Trapezoidal(double a, double b, double h, int n)
        {
            double sum = 0.5 * (CalculateFunction(a) + CalculateFunction(b));

            for (int i = 1; i < n; i++)
            {
                double x = a + i * h;
                sum += CalculateFunction(x);
            }

            return h * sum;
        }

        private double Simpson(double a, double b, double h, int n)
        {
            if (n < 2)
            {
                throw new ArgumentException("Для метода Симпсона N должно быть не менее 2");
            }

            if (n % 2 != 0)
            {
                throw new ArgumentException("Для метода Симпсона N должно быть четным");
            }

            double sum = CalculateFunction(a); 

            for (int i = 1; i < n; i += 2)
            {
                double x = a + i * h;
                sum += 4 * CalculateFunction(x);
            }

            for (int i = 2; i < n; i += 2)
            {
                double x = a + i * h;
                sum += 2 * CalculateFunction(x);
            }

            sum += CalculateFunction(b); 

            return (h / 3.0) * sum;
        }

        private void EvaluateFunction(string name, FunctionArgs args)
        {
            switch (name.ToLower())
            {
                case "sin": args.Result = Math.Sin(Convert.ToDouble(args.Parameters[0].Evaluate())); break;
                case "cos": args.Result = Math.Cos(Convert.ToDouble(args.Parameters[0].Evaluate())); break;
                case "tan": args.Result = Math.Tan(Convert.ToDouble(args.Parameters[0].Evaluate())); break;
                case "asin": args.Result = Math.Asin(Convert.ToDouble(args.Parameters[0].Evaluate())); break;
                case "acos": args.Result = Math.Acos(Convert.ToDouble(args.Parameters[0].Evaluate())); break;
                case "atan": args.Result = Math.Atan(Convert.ToDouble(args.Parameters[0].Evaluate())); break;
                case "sinh": args.Result = Math.Sinh(Convert.ToDouble(args.Parameters[0].Evaluate())); break;
                case "cosh": args.Result = Math.Cosh(Convert.ToDouble(args.Parameters[0].Evaluate())); break;
                case "tanh": args.Result = Math.Tanh(Convert.ToDouble(args.Parameters[0].Evaluate())); break;
                case "exp": args.Result = Math.Exp(Convert.ToDouble(args.Parameters[0].Evaluate())); break;
                case "sqrt":
                    {
                        double v = Convert.ToDouble(args.Parameters[0].Evaluate());
                        if (v < 0) throw new ArgumentException("Квадратный корень из отрицательного числа");
                        args.Result = Math.Sqrt(v);
                        break;
                    }
                case "abs": args.Result = Math.Abs(Convert.ToDouble(args.Parameters[0].Evaluate())); break;
                case "log":
                    if (args.Parameters.Length == 1)
                    {
                        double v = Convert.ToDouble(args.Parameters[0].Evaluate());
                        if (v <= 0) throw new ArgumentException("Логарифм определен только для положительных чисел");
                        args.Result = Math.Log(v);
                    }
                    else if (args.Parameters.Length == 2)
                    {
                        double v = Convert.ToDouble(args.Parameters[0].Evaluate());
                        double b = Convert.ToDouble(args.Parameters[1].Evaluate());
                        if (v <= 0 || b <= 0 || b == 1) throw new ArgumentException("Некорректные аргументы log");
                        args.Result = Math.Log(v, b);
                    }
                    else throw new ArgumentException("Функция log требует 1 или 2 аргумента");
                    break;
                case "log10":
                    if (args.Parameters.Length != 1) throw new ArgumentException("Функция log10 требует 1 аргумент");
                    {
                        double v = Convert.ToDouble(args.Parameters[0].Evaluate());
                        if (v <= 0) throw new ArgumentException("Логарифм определен только для положительных чисел");
                        args.Result = Math.Log10(v);
                        break;
                    }
                case "pow":
                    if (args.Parameters.Length != 2) throw new ArgumentException("Функция pow требует 2 аргумента");
                    {
                        double b = Convert.ToDouble(args.Parameters[0].Evaluate());
                        double p = Convert.ToDouble(args.Parameters[1].Evaluate());
                        args.Result = Math.Pow(b, p);
                        break;
                    }
                default:
                    throw new ArgumentException($"Неизвестная функция: {name}");
            }
        }
    }
}