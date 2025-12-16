using System;
using System.Collections.Generic;
using System.Linq;
using NCalc;

namespace WpfApp1
{
    public class NewtonMethod
    {
        private readonly Expression _expression;
        private readonly DihotomyMethod _dihotomyHelper;

        public NewtonMethod(string function)
        {
            string processedFunction = function.ToLower();
            _expression = new Expression(processedFunction, EvaluateOptions.IgnoreCase);
            _expression.Parameters["pi"] = Math.PI;
            _expression.Parameters["e"] = Math.E;
            _expression.EvaluateFunction += EvaluateFunction;
            _expression.EvaluateParameter += EvaluateParameter;

            _dihotomyHelper = new DihotomyMethod(function);
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
                    case "sin": args.Result = Math.Sin(Convert.ToDouble(args.Parameters[0].Evaluate())); break;
                    case "cos": args.Result = Math.Cos(Convert.ToDouble(args.Parameters[0].Evaluate())); break;
                    case "tan": args.Result = Math.Tan(Convert.ToDouble(args.Parameters[0].Evaluate())); break;
                    case "atan": args.Result = Math.Atan(Convert.ToDouble(args.Parameters[0].Evaluate())); break;
                    case "exp": args.Result = Math.Exp(Convert.ToDouble(args.Parameters[0].Evaluate())); break;
                    case "sqrt": args.Result = Math.Sqrt(Convert.ToDouble(args.Parameters[0].Evaluate())); break;
                    case "abs": args.Result = Math.Abs(Convert.ToDouble(args.Parameters[0].Evaluate())); break;
                    case "ln":
                    case "log":
                        if (args.Parameters.Length == 1)
                            args.Result = Math.Log(Convert.ToDouble(args.Parameters[0].Evaluate()));
                        else
                            args.Result = Math.Log(Convert.ToDouble(args.Parameters[0].Evaluate()),
                                                 Convert.ToDouble(args.Parameters[1].Evaluate()));
                        break;
                    case "log10": args.Result = Math.Log10(Convert.ToDouble(args.Parameters[0].Evaluate())); break;
                    case "pow":
                        args.Result = Math.Pow(Convert.ToDouble(args.Parameters[0].Evaluate()),
                                             Convert.ToDouble(args.Parameters[1].Evaluate()));
                        break;
                    default: throw new ArgumentException($"Неизвестная функция: {name}");
                }
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Ошибка в функции {name}: {ex.Message}");
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

                if (result is int intResult) return intResult;
                if (result is decimal decimalResult) return (double)decimalResult;
                return Convert.ToDouble(result);
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Ошибка вычисления функции в точке x={x}: {ex.Message}");
            }
        }

        public bool TestFunctionOnInterval(double a, double b)
        {
            return _dihotomyHelper.TestFunctionOnInterval(a, b);
        }

        public double CalculateFirstDerivative(double x, double h = 1e-6)
        {
            try
            {
                double f_plus = CalculateFunction(x + h);
                double f_minus = CalculateFunction(x - h);

                if (f_plus >= double.MaxValue - 1 || f_minus >= double.MaxValue - 1)
                    return double.MaxValue;

                return (f_plus - f_minus) / (2 * h);
            }
            catch
            {
                return double.MaxValue;
            }
        }

        public double CalculateSecondDerivative(double x, double h = 1e-4)
        {
            try
            {
                double f_x = CalculateFunction(x);
                double f_plus = CalculateFunction(x + h);
                double f_minus = CalculateFunction(x - h);

                if (f_x >= double.MaxValue - 1 || f_plus >= double.MaxValue - 1 || f_minus >= double.MaxValue - 1)
                    return 1.0;

                return (f_plus - 2 * f_x + f_minus) / (h * h);
            }
            catch
            {
                return 1.0;
            }
        }

        public TangentLine CalculateTangentLine(double x)
        {
            try
            {
                double y = CalculateFunction(x);
                double derivative = CalculateFirstDerivative(x);
                double slope = derivative;
                double intercept = y - slope * x;

                return new TangentLine
                {
                    Slope = slope,
                    Intercept = intercept,
                    PointX = x,
                    PointY = y
                };
            }
            catch
            {
                return null;
            }
        }

        public NewtonResult FindMinimum(double x0, double epsilon, int maxIterations, double a, double b, bool trackSteps = false)
        {
            if (epsilon <= 0)
                throw new ArgumentException("Точность epsilon должна быть положительной");

            if (a >= b)
                throw new ArgumentException("Начало интервала a должно быть меньше конца b");

            List<NewtonIteration> stepByStepIterations = new List<NewtonIteration>();
            List<TangentLine> tangentLines = new List<TangentLine>();

            double x = Math.Max(a, Math.Min(x0, b));
            bool converged = false;
            bool foundMinimum = false;
            int iterations = 0;
            string convergenceMessage = "Метод не сошелся";

            double fa = CalculateFunction(a);
            double fb = CalculateFunction(b);

            for (int i = 0; i < maxIterations; i++)
            {
                iterations = i + 1;

                double functionValue = CalculateFunction(x);
                double firstDerivative = CalculateFirstDerivative(x);
                double secondDerivative = CalculateSecondDerivative(x);

                if (double.IsInfinity(firstDerivative) || double.IsNaN(firstDerivative))
                    firstDerivative = 1.0;
                if (double.IsInfinity(secondDerivative) || double.IsNaN(secondDerivative))
                    secondDerivative = 1.0;

                var tangent = CalculateTangentLine(x);
                if (tangent != null && trackSteps)
                {
                    tangentLines.Add(tangent);
                }

                if (trackSteps)
                {
                    stepByStepIterations.Add(new NewtonIteration
                    {
                        Iteration = i,
                        X = x,
                        FunctionValue = functionValue,
                        FirstDerivative = firstDerivative,
                        SecondDerivative = secondDerivative,
                        TangentLine = tangent
                    });
                }

                bool isMinimum = Math.Abs(firstDerivative) < epsilon && secondDerivative > 0;
                bool isLeftBoundary = Math.Abs(x - a) < epsilon && firstDerivative > 0;
                bool isRightBoundary = Math.Abs(x - b) < epsilon && firstDerivative < 0;

                if (isMinimum || isLeftBoundary || isRightBoundary)
                {
                    converged = true;
                    foundMinimum = true;
                    if (isLeftBoundary) convergenceMessage = "Найден минимум на левой границе";
                    else if (isRightBoundary) convergenceMessage = "Найден минимум на правой границе";
                    else convergenceMessage = "Найден локальный минимум";
                    break;
                }

                double xNew;
                if (Math.Abs(secondDerivative) > epsilon)
                {
                    double newtonStep = -firstDerivative / secondDerivative;

                    double maxStep = (b - a) / 10;
                    if (Math.Abs(newtonStep) > maxStep)
                    {
                        newtonStep = Math.Sign(newtonStep) * maxStep;
                    }

                    xNew = x + newtonStep;
                }
                else
                {
                    double alpha = 0.1;
                    xNew = x - alpha * firstDerivative;
                }

                xNew = Math.Max(a, Math.Min(xNew, b));

                if (Math.Abs(xNew - x) < epsilon)
                {
                    converged = true;
                    x = xNew;

                    double finalFirstDeriv = CalculateFirstDerivative(x);
                    double finalSecondDeriv = CalculateSecondDerivative(x);

                    bool finalIsMinimum = Math.Abs(finalFirstDeriv) < epsilon && finalSecondDeriv > 0;
                    bool finalIsLeftBoundary = Math.Abs(x - a) < epsilon && finalFirstDeriv > 0;
                    bool finalIsRightBoundary = Math.Abs(x - b) < epsilon && finalFirstDeriv < 0;

                    if (finalIsMinimum || finalIsLeftBoundary || finalIsRightBoundary)
                    {
                        foundMinimum = true;
                        if (finalIsLeftBoundary) convergenceMessage = "Сходимость - минимум на левой границе";
                        else if (finalIsRightBoundary) convergenceMessage = "Сходимость - минимум на правой границе";
                        else convergenceMessage = "Сходимость - найден минимум";
                    }
                    else
                    {
                        foundMinimum = false;
                        convergenceMessage = "Сходимость достигнута, но точка не является минимумом";
                    }
                    break;
                }

                x = xNew;
            }

            if (!foundMinimum)
            {
                double scanPoint = FindMinimumByScan(a, b, 50);
                double scanValue = CalculateFunction(scanPoint);

                if (fa < scanValue && fa < fb)
                {
                    x = a;
                    foundMinimum = Math.Abs(CalculateFirstDerivative(a)) < epsilon && CalculateSecondDerivative(a) > 0;
                    convergenceMessage = foundMinimum ? "Минимум на левой границе" : "Лучшая точка на левой границе";
                }
                else if (fb < scanValue && fb < fa)
                {
                    x = b;
                    foundMinimum = Math.Abs(CalculateFirstDerivative(b)) < epsilon && CalculateSecondDerivative(b) > 0;
                    convergenceMessage = foundMinimum ? "Минимум на правой границе" : "Лучшая точка на правой границе";
                }
                else
                {
                    x = scanPoint;
                    foundMinimum = Math.Abs(CalculateFirstDerivative(scanPoint)) < epsilon &&
                                  CalculateSecondDerivative(scanPoint) > 0;
                    convergenceMessage = foundMinimum ? "Локальный минимум (сканирование)" : "Лучшая точка (сканирование)";
                }
                converged = true;
            }

            double minimumValue = CalculateFunction(x);
            double finalFirstDerivative = CalculateFirstDerivative(x);
            double finalSecondDerivative = CalculateSecondDerivative(x);

            return new NewtonResult
            {
                MinimumPoint = x,
                MinimumValue = minimumValue,
                Iterations = iterations,
                FinalDerivative = finalFirstDerivative,
                FinalSecondDerivative = finalSecondDerivative,
                Converged = converged,
                IsMinimum = foundMinimum,
                StepByStepIterations = stepByStepIterations,
                TangentLines = tangentLines,
                ConvergenceMessage = convergenceMessage
            };
        }

        private double FindMinimumByScan(double a, double b, int points)
        {
            double bestX = a;
            double bestValue = CalculateFunction(a);
            double step = (b - a) / points;

            for (int i = 0; i <= points; i++)
            {
                double x = a + i * step;
                double value = CalculateFunction(x);

                if (value < bestValue && value < double.MaxValue - 1)
                {
                    bestX = x;
                    bestValue = value;
                }
            }

            return bestX;
        }

        public double FindGoodStartingPoint(double a, double b, int samplePoints = 50)
        {
            return FindMinimumByScan(a, b, samplePoints);
        }
    }

    public class NewtonResult
    {
        public double MinimumPoint { get; set; }
        public double MinimumValue { get; set; }
        public int Iterations { get; set; }
        public double FinalDerivative { get; set; }
        public double FinalSecondDerivative { get; set; }
        public bool Converged { get; set; }
        public bool IsMinimum { get; set; }
        public List<NewtonIteration> StepByStepIterations { get; set; } = new List<NewtonIteration>();
        public List<TangentLine> TangentLines { get; set; } = new List<TangentLine>();
        public string ConvergenceMessage { get; set; }
    }

    public class NewtonIteration
    {
        public int Iteration { get; set; }
        public double X { get; set; }
        public double FunctionValue { get; set; }
        public double FirstDerivative { get; set; }
        public double SecondDerivative { get; set; }
        public TangentLine TangentLine { get; set; }
    }

    public class TangentLine
    {
        public double Slope { get; set; }
        public double Intercept { get; set; }
        public double PointX { get; set; }
        public double PointY { get; set; }

        public double GetY(double x)
        {
            return Slope * x + Intercept;
        }
    }
}