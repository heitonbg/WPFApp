using System;
using System.Globalization;
using System.Windows.Data;

namespace WpfApp1
{
    public class PrecisionFormatConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double doubleValue)
            {
                double epsilon = 0.0001;
                if (parameter is string epsilonStr && double.TryParse(epsilonStr, out double paramEpsilon))
                {
                    epsilon = paramEpsilon;
                }

                int decimalPlaces = GetDecimalPlacesFromEpsilon(epsilon);

                return doubleValue.ToString($"F{decimalPlaces}", CultureInfo.InvariantCulture);
            }

            return value?.ToString() ?? "-";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public static int GetDecimalPlacesFromEpsilon(double epsilon)
        {
            if (epsilon <= 0) return 6;

            int order = (int)Math.Ceiling(-Math.Log10(epsilon));

            return Math.Max(1, Math.Min(order, 15));
        }
    }
}