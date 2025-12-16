using System;
using System.Collections.Generic;
using System.Linq;

namespace WpfApp1
{
    public class LeastSquaresResult
    {
        public double[] Coefficients { get; set; }
        public double R2 { get; set; } 
        public TimeSpan Time { get; set; }
    }

    public class LeastSquaresMethod
    {
        public LeastSquaresResult LinearRegression(List<(double x, double y)> points)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            int n = points.Count;
            double sumX = 0, sumY = 0, sumXY = 0, sumX2 = 0;

            foreach (var point in points)
            {
                sumX += point.x;
                sumY += point.y;
                sumXY += point.x * point.y;
                sumX2 += point.x * point.x;
            }

            double denominator = n * sumX2 - sumX * sumX;
            double a = (n * sumXY - sumX * sumY) / denominator;
            double b = (sumY * sumX2 - sumX * sumXY) / denominator;

            double yMean = sumY / n;
            double ssTotal = 0;
            double ssResidual = 0;

            foreach (var point in points)
            {
                double yPred = a * point.x + b;
                ssTotal += Math.Pow(point.y - yMean, 2);
                ssResidual += Math.Pow(point.y - yPred, 2);
            }

            double r2 = 1 - (ssResidual / ssTotal);

            stopwatch.Stop();

            return new LeastSquaresResult
            {
                Coefficients = new double[] { b, a }, 
                R2 = r2,
                Time = stopwatch.Elapsed
            };
        }

        public LeastSquaresResult QuadraticRegression(List<(double x, double y)> points)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            int n = points.Count;

            double sumX = 0, sumY = 0;
            double sumX2 = 0, sumX3 = 0, sumX4 = 0;
            double sumXY = 0, sumX2Y = 0;

            foreach (var point in points)
            {
                double x = point.x;
                double y = point.y;
                double x2 = x * x;
                double x3 = x2 * x;
                double x4 = x3 * x;

                sumX += x;
                sumY += y;
                sumX2 += x2;
                sumX3 += x3;
                sumX4 += x4;
                sumXY += x * y;
                sumX2Y += x2 * y;
            }

            double[,] matrix = {
                { n, sumX, sumX2 },
                { sumX, sumX2, sumX3 },
                { sumX2, sumX3, sumX4 }
            };

            double[] vector = { sumY, sumXY, sumX2Y };

            double detMain = Determinant(matrix);

            if (Math.Abs(detMain) < 1e-12)
                throw new Exception("Определитель матрицы равен нулю. Невозможно решить систему.");

            double[,] matrixB = (double[,])matrix.Clone();
            double[,] matrixA = (double[,])matrix.Clone();
            double[,] matrixC = (double[,])matrix.Clone();

            for (int i = 0; i < 3; i++)
            {
                matrixB[i, 0] = vector[i]; 
                matrixA[i, 1] = vector[i]; 
                matrixC[i, 2] = vector[i]; 
            }

            double b = Determinant(matrixB) / detMain; 
            double a = Determinant(matrixA) / detMain; 
            double c = Determinant(matrixC) / detMain; 

            double yMean = sumY / n;
            double ssTotal = 0;
            double ssResidual = 0;

            foreach (var point in points)
            {
                double yPred = c * point.x * point.x + a * point.x + b;
                ssTotal += Math.Pow(point.y - yMean, 2);
                ssResidual += Math.Pow(point.y - yPred, 2);
            }

            double r2 = 1 - (ssResidual / ssTotal);

            stopwatch.Stop();

            return new LeastSquaresResult
            {
                Coefficients = new double[] { b, a, c },
                R2 = r2,
                Time = stopwatch.Elapsed
            };
        }

        private double Determinant(double[,] matrix)
        {
            return matrix[0, 0] * (matrix[1, 1] * matrix[2, 2] - matrix[1, 2] * matrix[2, 1])
                 - matrix[0, 1] * (matrix[1, 0] * matrix[2, 2] - matrix[1, 2] * matrix[2, 0])
                 + matrix[0, 2] * (matrix[1, 0] * matrix[2, 1] - matrix[1, 1] * matrix[2, 0]);
        }

        public double CalculateY(double x, double[] coefficients, int degree)
        {
            double result = 0;
            for (int i = 0; i <= degree; i++)
            {
                result += coefficients[i] * Math.Pow(x, i);
            }
            return result;
        }
    }
}