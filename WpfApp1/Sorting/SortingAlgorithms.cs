using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace WpfApp1
{
    public class SortingResult
    {
        public TimeSpan Time { get; set; }
        public int Iterations { get; set; }
    }

    public class SortingAlgorithms
    {
        private Random random = new Random();

        public SortingResult BubbleSort(List<int> array, bool ascending = true)
        {
            var stopwatch = Stopwatch.StartNew();
            int iterations = 0;
            int n = array.Count;

            for (int i = 0; i < n - 1; i++)
            {
                for (int j = 0; j < n - i - 1; j++)
                {
                    iterations++;
                    bool shouldSwap = ascending ?
                        array[j] > array[j + 1] :
                        array[j] < array[j + 1];

                    if (shouldSwap)
                    {
                        int temp = array[j];
                        array[j] = array[j + 1];
                        array[j + 1] = temp;
                    }
                }
            }

            stopwatch.Stop();
            return new SortingResult { Time = stopwatch.Elapsed, Iterations = iterations };
        }

        public SortingResult InsertionSort(List<int> array, bool ascending = true)
        {
            var stopwatch = Stopwatch.StartNew();
            int iterations = 0;
            int n = array.Count;

            for (int i = 1; i < n; i++)
            {
                int key = array[i];
                int j = i - 1;

                while (j >= 0 && (
                    (ascending && array[j] > key) ||
                    (!ascending && array[j] < key)))
                {
                    iterations++;
                    array[j + 1] = array[j];
                    j--;
                }
                iterations++; 
                array[j + 1] = key;
            }

            stopwatch.Stop();
            return new SortingResult { Time = stopwatch.Elapsed, Iterations = iterations };
        }

        public SortingResult ShakerSort(List<int> array, bool ascending = true)
        {
            var stopwatch = Stopwatch.StartNew();
            int iterations = 0;
            int left = 0;
            int right = array.Count - 1;

            while (left <= right)
            {
                for (int i = left; i < right; i++)
                {
                    iterations++;
                    bool shouldSwap = ascending ?
                        array[i] > array[i + 1] :
                        array[i] < array[i + 1];

                    if (shouldSwap)
                    {
                        int temp = array[i];
                        array[i] = array[i + 1];
                        array[i + 1] = temp;
                    }
                }
                right--;

                for (int i = right; i > left; i--)
                {
                    iterations++;
                    bool shouldSwap = ascending ?
                        array[i - 1] > array[i] :
                        array[i - 1] < array[i];

                    if (shouldSwap)
                    {
                        int temp = array[i];
                        array[i] = array[i - 1];
                        array[i - 1] = temp;
                    }
                }
                left++;
            }

            stopwatch.Stop();
            return new SortingResult { Time = stopwatch.Elapsed, Iterations = iterations };
        }

        public SortingResult QuickSort(List<int> array, bool ascending = true)
        {
            var stopwatch = Stopwatch.StartNew();
            int iterations = 0;
            QuickSortRecursive(array, 0, array.Count - 1, ascending, ref iterations);
            stopwatch.Stop();
            return new SortingResult { Time = stopwatch.Elapsed, Iterations = iterations };
        }

        private void QuickSortRecursive(List<int> array, int low, int high, bool ascending, ref int iterations)
        {
            if (low < high)
            {
                iterations++;
                int pi = Partition(array, low, high, ascending, ref iterations);
                QuickSortRecursive(array, low, pi - 1, ascending, ref iterations);
                QuickSortRecursive(array, pi + 1, high, ascending, ref iterations);
            }
        }

        private int Partition(List<int> array, int low, int high, bool ascending, ref int iterations)
        {
            int pivot = array[high];
            int i = low - 1;

            for (int j = low; j < high; j++)
            {
                iterations++;
                bool condition = ascending ?
                    array[j] <= pivot :
                    array[j] >= pivot;

                if (condition)
                {
                    i++;
                    int temp = array[i];
                    array[i] = array[j];
                    array[j] = temp;
                }
            }

            int temp1 = array[i + 1];
            array[i + 1] = array[high];
            array[high] = temp1;

            return i + 1;
        }

        public SortingResult BogoSort(List<int> array, bool ascending = true)
        {
            var stopwatch = Stopwatch.StartNew();
            int iterations = 0;

            while (!IsSorted(array, ascending))
            {
                iterations++;
                Shuffle(array);
            }

            stopwatch.Stop();
            return new SortingResult { Time = stopwatch.Elapsed, Iterations = iterations };
        }

        private bool IsSorted(List<int> array, bool ascending)
        {
            for (int i = 0; i < array.Count - 1; i++)
            {
                if (ascending && array[i] > array[i + 1])
                    return false;
                if (!ascending && array[i] < array[i + 1])
                    return false;
            }
            return true;
        }

        private void Shuffle(List<int> array)
        {
            int n = array.Count;
            for (int i = 0; i < n; i++)
            {
                int j = random.Next(i, n);
                int temp = array[i];
                array[i] = array[j];
                array[j] = temp;
            }
        }
    }
}