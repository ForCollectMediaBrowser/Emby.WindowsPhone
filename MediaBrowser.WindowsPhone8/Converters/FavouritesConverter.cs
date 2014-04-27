﻿using System;
using System.Windows.Data;
using MediaBrowser.WindowsPhone.Resources;

namespace MediaBrowser.WindowsPhone.Converters
{
    public class FavouritesConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var favouriteType = (string) parameter;
            var textOrIcon = favouriteType.Contains("text");
            if (value == null)
            {
                if(textOrIcon)return favouriteType.Contains("full") ? AppResources.LabelAddToFavourites.ToLower() : AppResources.LabelAdd.ToLower();
                return new Uri("/Icons/appbar.star.add.png", UriKind.Relative);
            }
            var isFavourite = (bool) value;
            if (textOrIcon)
            {
                if (favouriteType.Contains("full"))
                {
                    return isFavourite ? AppResources.RemoveFromFavouritesFull.ToLower() : AppResources.LabelAddToFavourites.ToLower();
                }

                return isFavourite ? AppResources.LabelRemove.ToLower() : AppResources.LabelAdd.ToLower();
            }
            return isFavourite ? new Uri("/Icons/appbar.star.minus.png", UriKind.Relative) : new Uri("/Icons/appbar.star.add.png", UriKind.Relative);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
