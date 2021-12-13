// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Reflection;
using System.Windows.Data;
using System.Globalization;

namespace Firefly.Client.Control.Fire
{
    public class EnumToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            PropertyInfo property = typeof(I18n.Properties.Resources).GetProperty(value.ToString());
            if (property == null)
            {
                return string.Empty;
            }
            var propertyInfo = typeof(I18n.Properties.Resources).GetProperty(value.ToString());
            return propertyInfo.GetValue(null);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return string.Empty;
        }
    }
}