using System;
using System.Collections.Generic;
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
            _expression = new Expression(function.ToLower(), EvaluateOptions.IgnoreCase);

            _expression.Parameters["pi"] = Math.PI;
            _expression.Parameters["e"] = Math.E;

            _expression.EvaluateFunction += EvaluateFunction;
        }

        private void EvaluateFunction(string name, FunctionArgs args)
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
                    args.Result = Math.Pow(Convert.ToDouble(args.Parameters[0].Evaluate()), Convert.ToDouble(args.Parameters[1].Evaluate()));
                    break;
                default:
                    throw new ArgumentException($"Неизвестная функция: {name}");
            }
        }

        public double CalculateFunction(double x)
        {
            try
            {
                if (Math.Abs(x) > 1e10)
                {
                    return double.MaxValue / 1000;
                }

                _expression.Parameters["x"] = x;
                var result = _expression.Evaluate();

                if (result is double doubleResult)
                {
                    if (double.IsInfinity(doubleResult) || double.IsNaN(doubleResult))
                    {
                        return double.MaxValue;
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
                throw new ArgumentException($"Ошибка вычисления функции в точке x={x}: {ex.Message}");
            }
        }

        public GoldenRatioResult FindMinimum(double a, double b, double epsilon)
        {
            if (a >= b)
            {
                throw new ArgumentException("Интервал [a, b] задан неверно");
            }

            if (epsilon <= 0)
            {
                throw new ArgumentException("Точность epsilon должна быть положительной");
            }

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

                if (IterationsCount > 1000)
                {
                    break;
                }
            }

            double extremumPoint = (a + b) / 2;
            double extremumValue = CalculateFunction(extremumPoint);

            return new GoldenRatioResult
            {
                ExtremumPoint = extremumPoint,
                ExtremumValue = extremumValue,
                Iterations = IterationsCount,
                FinalInterval = (a, b)
            };
        }

        public GoldenRatioResult FindMaximum(double a, double b, double epsilon)
        {
            if (a >= b)
            {
                throw new ArgumentException("Интервал [a, b] задан неверно");
            }

            if (epsilon <= 0)
            {
                throw new ArgumentException("Точность epsilon должна быть положительной");
            }

            IterationsCount = 0;

            double x1 = b - (b - a) / GoldenRatio;
            double x2 = a + (b - a) / GoldenRatio;

            double f1 = CalculateFunction(x1);
            double f2 = CalculateFunction(x2);

            while (Math.Abs(b - a) > epsilon)
            {
                IterationsCount++;

                if (f1 <= f2)  // Изменено условие для поиска максимума
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

                if (IterationsCount > 1000)
                {
                    break;
                }
            }

            double extremumPoint = (a + b) / 2;
            double extremumValue = CalculateFunction(extremumPoint);

            return new GoldenRatioResult
            {
                ExtremumPoint = extremumPoint,
                ExtremumValue = extremumValue,
                Iterations = IterationsCount,
                FinalInterval = (a, b)
            };
        }

        public GoldenRatioResult FindGlobalExtremum(double a, double b, double epsilon, bool findMinimum = true)
        {
            int gridPoints = 10;
            double step = (b - a) / gridPoints;

            GoldenRatioResult bestResult = null;

            for (int i = 0; i < gridPoints; i++)
            {
                double startA = a + i * step;
                double startB = Math.Min(b, startA + step * 2);

                try
                {
                    var localResult = findMinimum ?
                        FindMinimum(startA, startB, epsilon) :
                        FindMaximum(startA, startB, epsilon);

                    if (bestResult == null ||
                        (findMinimum && localResult.ExtremumValue < bestResult.ExtremumValue) ||
                        (!findMinimum && localResult.ExtremumValue > bestResult.ExtremumValue))
                    {
                        bestResult = localResult;
                    }
                }
                catch
                {
                    // Игнорируем интервалы, где функция не определена
                }
            }

            return bestResult ?? (findMinimum ? FindMinimum(a, b, epsilon) : FindMaximum(a, b, epsilon));
        }

        public bool IsConstantFunction(double a, double b)
        {
            double[] testPoints = { a, (a + b) / 2, b, a + (b - a) / 4, a + 3 * (b - a) / 4 };
            double firstValue = CalculateFunction(testPoints[0]);

            foreach (double point in testPoints)
            {
                if (Math.Abs(CalculateFunction(point) - firstValue) > 1e-15)
                {
                    return false;
                }
            }
            return true;
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