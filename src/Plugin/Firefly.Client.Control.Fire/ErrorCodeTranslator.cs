// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using Firefly.Client.Service;

namespace Firefly.Client.Control.Fire
{
    public class ErrorCodeTranslator
    {
        public static string Translate(int errorCode)
        {
            string failedInfo = I18n.Properties.Resources.Failed;

            switch (errorCode)
            {
                case ResponseCode.FIRE_DEVICE_ENGINE_SET_PRESET_FAILED:
                    failedInfo = I18n.Properties.Resources.SetPresetFailed;
                    break;
                case ResponseCode.FIRE_DEVICE_INVALID_PRESET:
                    failedInfo = I18n.Properties.Resources.FixMonitoringOnlyPreset1;
                    break;
                case ResponseCode.FIRE_DEVICE_INVALID_SPOT:
                    failedInfo = I18n.Properties.Resources.NoSupportMarkerSpot;
                    break;
                case ResponseCode.FIRE_DEVICE_INVALID_LINE:
                    failedInfo = I18n.Properties.Resources.NoSupportMarkerLine;
                    break;
                case ResponseCode.FIRE_DEVICE_INVALID_REGION:
                    failedInfo = I18n.Properties.Resources.NoSupportMarkerRegion;
                    break;
                case ResponseCode.FIRE_DEVICE_LIMITED_SPOT:
                    failedInfo = I18n.Properties.Resources.LimitMarkerSpot;
                    break;
                case ResponseCode.FIRE_DEVICE_LIMITED_LINE:
                    failedInfo = I18n.Properties.Resources.LimitMarkerLine;
                    break;
                case ResponseCode.FIRE_DEVICE_LIMITED_REGION:
                    failedInfo = I18n.Properties.Resources.LimitMarkerRegion;
                    break;
                case ResponseCode.FIRE_DEVICE_INVALID_OFFSET_TEMP:
                    failedInfo = I18n.Properties.Resources.NoSupportMarkerOffset;
                    break;
                case ResponseCode.FIRE_DEVICE_INVALID_REF_TEMP:
                    failedInfo = I18n.Properties.Resources.NoSupportMarkerRef;
                    break;
                case ResponseCode.FIRE_DEVICE_NOT_EXISTED:
                    failedInfo = I18n.Properties.Resources.DeviceNotExisted;
                    break;
                case ResponseCode.FIRE_DEVICE_OVERRIDE_CONFIG_FAILED:
                    failedInfo = I18n.Properties.Resources.OverriedFailed;
                    break;
                case ResponseCode.FIRE_DEVICE_INVALID_SHIELD_MARKER:
                    failedInfo = I18n.Properties.Resources.InvalidShieldMarker;
                    break;
                case ResponseCode.FIRE_DEVICE_INVALID_MARKER:
                    failedInfo = I18n.Properties.Resources.InvalidMarker;
                    break;
                case ResponseCode.FIRE_DEVICE_INVALID_ALARM_OUTPUT:
                    failedInfo = I18n.Properties.Resources.InvalidAlarmOutput;
                    break;
                case ResponseCode.FIRE_DEVICE_ABILITY_NOT_MATCH:
                    failedInfo = I18n.Properties.Resources.DeviceAbilityNotMatch;
                    break;
                case ResponseCode.FIRE_DEVICE_INVALID_SENSITIVITY:
                    failedInfo = I18n.Properties.Resources.InvalidSensitivity;
                    break;
                case ResponseCode.FIRE_DEVICE_INVALID_PTZ:
                    failedInfo = I18n.Properties.Resources.InvalidPTZ;
                    break;
                case ResponseCode.ADMIN_CONFIG_ACCOUNT_FORBIDDEN:
                    failedInfo = I18n.Properties.Resources.AccountForbidden;
                    break;
            }

            return failedInfo;
        }
    }
}