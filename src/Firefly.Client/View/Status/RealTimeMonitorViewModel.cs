// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System.Linq;
using System.Windows.Input;
using System.Collections.Generic;

using Firefly.Client.Common;
using Firefly.Client.Control;
using Firefly.Client.Service;

namespace Firefly.Client
{
    public class RealTimeMonitorViewModel : BaseViewModel
    {
        #region Fields

        private const int DEFAULT_SPEED = 1;
        private DeviceDO _deviceDO;
        private List<RealTimePresetItemViewModel> _presetItems;
        private List<RealTimeCruiseItemViewModel> _cruiseItems;
        private RealTimePresetItemViewModel _selectedPreset;
        private string _irRTSPUrl;
        private string _dcRTSPUrl;

        #endregion

        #region Constructors

        public RealTimeMonitorViewModel(DeviceDO deviceDO)
        {
            _deviceDO = deviceDO;
            _presetItems = new List<RealTimePresetItemViewModel>();
            _cruiseItems = new List<RealTimeCruiseItemViewModel>();

            for (int i = 1; i < 256; i++)
            {
                _presetItems.Add(new RealTimePresetItemViewModel { ID = i });
            }

            for (int i = 1; i <= 10; i++)
            {
                _cruiseItems.Add(new RealTimeCruiseItemViewModel { ID = i });
            }
        }

        public void Load(DeviceStreamDO streamDO)
        {
            if (streamDO?.IRDeviceSteamItem != null)
            {
                IRRTSPUrl = streamDO.IRDeviceSteamItem.RTSPUrl;
            }

            if (streamDO?.DCDeviceSteamItem != null)
            {
                DCRTSPUrl = streamDO.DCDeviceSteamItem.RTSPUrl;
            }
        }

        #endregion

        #region Properties

        public List<RealTimePresetItemViewModel> PresetItems
        {
            get => _presetItems;
            set
            {
                _presetItems = value;
                OnPropertyChanged(nameof(PresetItems));
            }
        }

        public RealTimePresetItemViewModel SelectedPreset
        {
            get => _selectedPreset;
            set
            {
                _selectedPreset = value;
                if (_selectedPreset != null)
                {
                    SetPreset();
                }
                OnPropertyChanged(nameof(SelectedPreset));
            }
        }

        public List<RealTimeCruiseItemViewModel> CruiseItems
        {
            get => _cruiseItems;
            set
            {
                _cruiseItems = value;
                OnPropertyChanged(nameof(CruiseItems));
            }
        }

        public bool HasVisualVideo
        {
            get { return _deviceDO?.DeviceAddress?.Any(item => item.ProtocolType == DCCameraType.HIKSDK_NET.ToString() || item.ProtocolType == DCCameraType.DHSDK_NET.ToString()) == true; }
        }

        public string IRRTSPUrl
        {
            get => _irRTSPUrl;
            set
            {
                _irRTSPUrl = value;
                OnPropertyChanged();
            }
        }

        public string DCRTSPUrl
        {
            get => _dcRTSPUrl;
            set
            {
                _dcRTSPUrl = value;
                OnPropertyChanged();
            }
        }

        #endregion

        private async void SetPreset()
        {
            try
            {
                Mouse.SetCursor(Cursors.Wait);
                ApiResult<bool> apiResult = await DeviceManager.Instance.SetPreset(_deviceDO, new PTZPresetDataObject
                {
                    Command = PTZPresetCommand.GOTO,
                    PresetID = SelectedPreset.ID,
                    Speed = DEFAULT_SPEED,
                });
                if (apiResult.Code == ResponseCode.EQUIPMENT_DEVICE_INVALID_PTZ || apiResult.Code == ResponseCode.FIRE_DEVICE_INVALID_PTZ)
                {
                    IRtekMessageBox.ShowDialog(null, I18n.Properties.Resources.InvalidPTZ, I18n.Properties.Resources.Tip, Control.MessageBoxButton.OK);
                }
            }
            finally
            {
                Mouse.SetCursor(Cursors.Arrow);
            }
        }
    }

    public class RealTimePresetItemViewModel
    {
        private string _name;
        public int ID { get; set; }

        public string Name
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_name))
                {
                    return string.Format(I18n.Properties.Resources.PresetIndex, ID);
                }

                return _name;
            }
            set
            {
                _name = value;
            }
        }
    }

    public class RealTimeCruiseItemViewModel : BaseViewModel
    {
        private string _name;
        private bool _cruiseStatus = false;
        public int ID { get; set; }

        public string Name
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_name))
                {
                    return $"{I18n.Properties.Resources.CruiseRoute}{ID}";
                }

                return _name;
            }
            set
            {
                _name = value;
            }
        }

        public bool CruiseStatus
        {
            get => _cruiseStatus;
            set
            {
                _cruiseStatus = value;
                OnPropertyChanged();
            }
        }
    }
}