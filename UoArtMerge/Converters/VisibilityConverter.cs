/* 
* Copyright (C) 2013 Ian Karlinsey
* 
* 
* UOArtMerge is free software: you can redistribute it and/or modify
* it under the terms of the GNU General Public License as published by
* the Free Software Foundation, either version 3 of the License, or
* (at your option) any later version.
* 
* UOArtMerge is distributed in the hope that it will be useful,
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
* GNU General Public License for more details.
* 
* You should have received a copy of the GNU General Public License
* along with UltimaLive.  If not, see <http://www.gnu.org/licenses/>. 
*/

using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace UoArtMerge.Converters
{
    public class VisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Visibility rv = Visibility.Visible;
            try
            {
                var x = value != null && bool.Parse(value.ToString());

                rv = x ? Visibility.Visible : Visibility.Collapsed;
            }
            catch
            {
                // ignored
            }

            return rv;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}

