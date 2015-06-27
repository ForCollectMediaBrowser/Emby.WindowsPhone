﻿using System;
using System.Windows.Data;

namespace Emby.WindowsPhone.Converters
{
    public class TicksToTimespanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value != null)
            {
                var runtimeTicks = (long)value;
                var runtime = TimeSpan.FromTicks(runtimeTicks);
                return runtime.ToString(@"mm\:ss");
            }
            return new TimeSpan();
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
