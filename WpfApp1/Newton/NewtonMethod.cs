using System;
using System.Collections.Generic;
using NCalc;

namespace WpfApp1
{
    public class NewtonMethod
    {
        private readonly Expression _expression;

        public NewtonMethod(string function)
        {
            _expression = new Expression(function.ToLower(), EvaluateOptions.IgnoreCase);
            _expression.Parameters["pi"] = Math.PI;
            _expression.Parameters["e"] = Math.E;
            _expression.EvaluateFunction += EvaluateFunction;
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
                _expression.Parameters["x"] = x;
                var result = _expression.Evaluate();

                if (result == null)
                    return double.MaxValue;

                double value = Convert.ToDouble(result);

                if (double.IsNaN(value) || double.IsInfinity(value))
                    return double.MaxValue;

                return value;
            }
            catch
            {
                return double.MaxValue;
            }
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

        public NewtonResult FindMinimum(double x0, double epsilon, int maxIterations, double a, double b, bool trackSteps = false)
        {
            if (epsilon <= 0)
                throw new ArgumentException("Точность epsilon должна быть положительной");

            if (a >= b)
                throw new ArgumentException("Начало интервала a должно быть меньше конца b");

            List<NewtonIteration> stepByStepIterations = new List<NewtonIteration>();
            double x = Math.Max(a, Math.Min(x0, b));
            bool converged = false;
            bool foundMinimum = false;
            int iterations = 0;
            string convergenceMessage = "Метод не сошелся";

            double fa = CalculateFunction(a);
            double fb = CalculateFunction(b);
            double f_x0 = CalculateFunction(x);

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

                if (trackSteps)
                {
                    stepByStepIterations.Add(new NewtonIteration
                    {
                        Iteration = i,
                        X = x,
                        FunctionValue = functionValue,
                        FirstDerivative = firstDerivative,
                        SecondDerivative = secondDerivative
                    });
                }

                bool isMinimum = Math.Abs(firstDerivative) < epsilon && secondDerivative > 0;
                bool isLeftBoundary = Math.Abs(x - a) < 1e-10 && firstDerivative > 0; 
                bool isRightBoundary = Math.Abs(x - b) < 1e-10 && firstDerivative < 0; 

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
                if (Math.Abs(secondDerivative) > 1e-10)
                {
                    double newtonStep = -firstDerivative / secondDerivative;

                    double maxStep = 1.0;
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
                    bool finalIsLeftBoundary = Math.Abs(x - a) < 1e-10 && finalFirstDeriv > 0;
                    bool finalIsRightBoundary = Math.Abs(x - b) < 1e-10 && finalFirstDeriv < 0;

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
                List<(double x, double value)> candidates = new List<(double, double)>();

                candidates.Add((a, fa));
                candidates.Add((b, fb));

                candidates.Add((x, CalculateFunction(x)));
                double scanPoint = FindMinimumByScan(a, b, 50);
                candidates.Add((scanPoint, CalculateFunction(scanPoint)));

                var bestCandidate = candidates.OrderBy(c => c.value).First();

                x = bestCandidate.x;
                double bestValue = bestCandidate.value;

                double deriv = CalculateFirstDerivative(x);
                double secondDeriv = CalculateSecondDerivative(x);

                bool isMin = Math.Abs(deriv) < epsilon && secondDeriv > 0;
                bool isLeftMin = Math.Abs(x - a) < 1e-10 && deriv > 0;
                bool isRightMin = Math.Abs(x - b) < 1e-10 && deriv < 0;

                foundMinimum = isMin || isLeftMin || isRightMin;
                converged = true;

                if (isLeftMin) convergenceMessage = "Глобальный минимум на левой границе";
                else if (isRightMin) convergenceMessage = "Глобальный минимум на правой границе";
                else if (isMin) convergenceMessage = "Локальный минимум (поиск по сетке)";
                else convergenceMessage = "Наилучшая найденная точка (возможный минимум)";
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

        private double FindBestPoint(double a, double b, int points)
        {
            return FindMinimumByScan(a, b, points);
        }

        public double FindGoodStartingPoint(double a, double b, int samplePoints = 50)
        {
            return FindBestPoint(a, b, samplePoints);
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
        public string ConvergenceMessage { get; set; }
    }

    public class NewtonIteration
    {
        public int Iteration { get; set; }
        public double X { get; set; }
        public double FunctionValue { get; set; }
        public double FirstDerivative { get; set; }
        public double SecondDerivative { get; set; }
    }
}