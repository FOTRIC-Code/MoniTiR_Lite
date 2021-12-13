// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Windows.Data;
using System.Globalization;

using Firefly.Client.Service;

namespace Firefly.Client
{
    internal class ServerTypeConverter : IValueConverter
    {
        #region Methods

        public static bool IsBaseAgent(string type)
        {
            Enum.TryParse(type, out ServerType serverType);
            return serverType == ServerType.StoreAgent || serverType == ServerType.InspectionTaskAgent;
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ServerType type)
            {
                switch (type)
                {
                    case ServerType.FireMonitorAgent:
                        return I18n.Properties.Resources.FireMonitorAgent;

                    case ServerType.StoreAgent:
                        return I18n.Properties.Resources.StoreAgent;

                    case ServerType.EquipmentMonitorAgent:
                        return I18n.Properties.Resources.EquipmentMonitorAgent;

                    case ServerType.EnvironmentMonitorAgent:
                        return I18n.Properties.Resources.EnvironmentMonitorAgent;

                    case ServerType.None:
                        return I18n.Properties.Resources.None;

                    case ServerType.SecurityMonitorTransferAgent:
                        return I18n.Properties.Resources.SecurityMonitorTransferAgent;

                    case ServerType.InspectionTaskAgent:
                        return I18n.Properties.Resources.InspectionTaskAgent;

                    case ServerType.AcousticMonitorAgent:
                        return I18n.Properties.Resources.AcousticMonitorAgent;

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

        #endregion Methods
    }
}