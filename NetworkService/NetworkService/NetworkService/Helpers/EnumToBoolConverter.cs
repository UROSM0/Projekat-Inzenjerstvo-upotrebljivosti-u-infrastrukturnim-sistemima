using System;
using System.Globalization;
using System.Windows.Data;
using NetworkService.ViewModel;

namespace NetworkService.Helpers
{

    public class EnumToBoolConverter : IValueConverter
    {

        public static readonly EnumToBoolConverter Instance = new EnumToBoolConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null) return false;
            var current = value.ToString();
            var target = parameter.ToString();
            return string.Equals(current, target, StringComparison.OrdinalIgnoreCase);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isChecked = (bool)value;
            if (!isChecked) return Binding.DoNothing;
            if (parameter == null) return IdCompareOp.None;
            var name = parameter.ToString();
            return Enum.Parse(typeof(IdCompareOp), name);
        }
    }
}
