// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Windows.Data;
using System.Globalization;
using Firefly.Client.Service;

namespace Firefly.Client
{
    internal class DeviceTypeConverter : IValueConverter
    {
        #region Methods

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DeviceType type)
            {
                switch (type)
                {
                    case DeviceType.IRCamera:
                        return I18n.Properties.Resources.IRCamera;
                    case DeviceType.DCCamera:
                        return I18n.Properties.Resources.DCCamera;
                    default:
                        break;
                }
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return string.Empty;
        }
        #endregion
    }
}
