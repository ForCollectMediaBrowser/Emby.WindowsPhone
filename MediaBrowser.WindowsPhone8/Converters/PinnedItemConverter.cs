﻿using System;
using System.Windows.Data;
using Emby.WindowsPhone.Localisation;

namespace MediaBrowser.WindowsPhone.Converters
{
    public class PinnedItemConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var type = "icon";
            if (parameter != null) type = (string) parameter;

            var isPinned = (bool) value;

            if (type == "icon")
            {
                return !isPinned ?  new Uri("/Icons/appbar.pin.png", UriKind.Relative) : new Uri("/Icons/appbar.pin.remove.png", UriKind.Relative);
            }
            return !isPinned ? AppResources.PinToStart.ToLower() : AppResources.Unpin.ToLower();
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
