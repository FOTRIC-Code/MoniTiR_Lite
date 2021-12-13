// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Firefly.Client.I18n.Properties;
using Firefly.Client.Service;

namespace Firefly.Client.Control.Fire
{
    public class DeviceOverrideViewModel : BaseViewModel
    {
        #region Fields

        private int _selectedIndex;

        #endregion

        #region Properties

        public DeviceItemViewModel SelectedItem { get; private set; }

        public bool IsSelectedAll
        {
            get
            {
                return DeviceItems.Count > 0 && DeviceItems.All(item => item.IsDeviceChecked);
            }
            set
            {
                foreach (DeviceItemViewModel deviceItemVM in DeviceItems)
                {
                    deviceItemVM.IsDeviceChecked = value;
                }
            }
        }

        public bool HasSelectedItem
        {
            get
            {
                return DeviceItems.Any(item => item.IsDeviceChecked);
            }
        }

        public ObservableCollection<DeviceItemViewModel> DeviceItems { get; set; }

        public int SelectedIndex
        {
            get => _selectedIndex;
            set
            {
                _selectedIndex = value;

                if (_selectedIndex >= 0 && DeviceItems.Count > _selectedIndex)
                {
                    SelectedItem = DeviceItems[_selectedIndex];
                }
            }
        }

        public List<ConfigItemViewModel> ConfigItems { get; set; }

        public void RefreshSelectedProperty()
        {
            OnPropertyChanged(nameof(IsSelectedAll));
            OnPropertyChanged(nameof(HasSelectedItem));
        }
        #endregion

        #region Constructors

        public DeviceOverrideViewModel()
        {
            DeviceItems = new ObservableCollection<DeviceItemViewModel>();
            ConfigItems = new List<ConfigItemViewModel>
            {
                new ConfigItemViewModel()
                {
                    IsChecked = false,
                    ConfigName = Resources.General,
                    PropertiesName = new List<string>() { "GeneralConfig" },
                },
                new ConfigItemViewModel()
                {
                    IsChecked = false,
                    ConfigName = Resources.EventTrigger,
                    PropertiesName = new List<string>() { "EventTrigger" }

                },
                new ConfigItemViewModel()
                {
                    IsChecked = false,
                    ConfigName = Resources.CharacterOverlay,
                    PropertiesName = new List<string>()
                        {"OSDConfigEnabled", "ChannelConfig", "PTZOSDConfig", "MarkerOSDConfig"}
                },
                new ConfigItemViewModel()
                {
                    IsChecked = false,
                    ConfigName = Resources.Preset,
                    PropertiesName = new List<string>() { "PresetConfig", "AlarmRuleConfig" }
                },
                new ConfigItemViewModel()
                {
                    IsChecked = false,
                    ConfigName = Resources.CruiseRoute,
                    PropertiesName = new List<string>() { "CruiseRouteConfig" }
                },
                new ConfigItemViewModel()
                {
                    IsChecked = false,
                    ConfigName = Resources.TaskSchedule,
                    PropertiesName = new List<string>() { "TaskSchedule" }
                },
                new ConfigItemViewModel()
                {
                    IsChecked = false,
                    ConfigName = Resources.AlarmSchedule,
                    PropertiesName = new List<string>() { "AlarmSchedule" }
                }
            };
        }

        #endregion

        #region Methods

        public async void LoadData()
        {
            DeviceDO deviceDO = FireAgentDeviceManager.Instance.GetDeviceDO();
            List<DeviceDO> deviceDOList = await DeviceManager.Instance.GetDevicesConfigByAgentID(deviceDO.DefaultAgent().ID);

            deviceDOList = deviceDOList.Where(item => item.ID != deviceDO.ID).ToList();
            DeviceItems.Clear();
            deviceDOList.ToList().ForEach(deviceDOItem => { DeviceItems.Add(new DeviceItemViewModel(deviceDOItem)); });
        }

        #endregion
    }

    public class DeviceItemViewModel : BaseViewModel
    {
        #region Fields

        public bool _isDeviceChecked;

        #endregion

        #region Constructors

        public DeviceItemViewModel(DeviceDO deviceDO)
        {
            DeviceID = deviceDO.ID;
            DeviceName = deviceDO.Name;
        }

        #endregion

        #region Properties

        public string DeviceName { get; set; }

        public bool IsDeviceChecked
        {
            get => _isDeviceChecked;
            set
            {
                _isDeviceChecked = value;
                OnPropertyChanged();
            }
        }

        public string DeviceID { get; set; }

        #endregion
    }

    public class ConfigItemViewModel : BaseViewModel
    {
        private bool _isChecked;

        public string ConfigName { get; set; }

        public bool IsChecked
        {
            get => _isChecked;
            set
            {
                _isChecked = value;
                OnPropertyChanged();
            }
        }

        public List<string> PropertiesName { get; set; }
    }
}
