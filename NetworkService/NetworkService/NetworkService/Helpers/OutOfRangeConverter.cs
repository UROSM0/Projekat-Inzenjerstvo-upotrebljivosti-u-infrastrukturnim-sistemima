using System;
using System.Globalization;
using System.Windows.Data;

namespace NetworkService.Helpers
{

    public class OutOfRangeConverter : IValueConverter
    {
        public double Low { get; set; } = 250.0;
        public double High { get; set; } = 350.0;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return false;
            if (value is double d) return d < Low || d > High;
            if (double.TryParse(value.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var v))
                return v < Low || v > High;
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotImplementedException();
    }
}
