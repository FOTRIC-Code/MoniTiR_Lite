// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using Firefly.Client.Control;
using Firefly.Client.Service;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Firefly.Client
{
    public class DeviceManagerViewModel : BaseViewModel
    {
        private string _searchText;
        public ObservableCollection<DeviceViewModel> DeviceList { get; set; }

        public DeviceViewModel SelectedItem { get; set; }

        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged();
            }
        }

        public DeviceManagerViewModel()
        {
            DeviceList = new ObservableCollection<DeviceViewModel>();
        }

        public async Task Load(List<DeviceDO> deviceDOs)
        {
            await Task.Delay(20);

            if(deviceDOs == null || deviceDOs.Count == 0)
            {
                DeviceList.Clear();
                return;
            }

            List<DeviceViewModel> removedList = new List<DeviceViewModel>();
            for (int i = 0; i < DeviceList.Count; i++)
            {
                DeviceViewModel deviceVM = DeviceList[i];
                if(deviceDOs.All(it=>!string.Equals(it.ID, deviceVM.ID)))
                {
                    removedList.Add(deviceVM);
                }
            }
            removedList.ForEach(it => DeviceList.Remove(it));

            for(int i = 0; i < deviceDOs.Count; i++)
            {
                DeviceDO deviceDO = deviceDOs[i];
                DeviceViewModel deviceVM = DeviceList.FirstOrDefault(it => string.Equals(it.ID, deviceDO.ID));
                if (deviceVM == null)
                {
                    deviceVM = new DeviceViewModel(deviceDO);
                    DeviceList.Add(deviceVM);
                    continue;
                }

                deviceVM.Update(deviceDO);
            }
        }

        public void Clear()
        {
            DeviceList.Clear();
        }
    }

    public class DeviceViewModel : BaseViewModel
    {
        #region Fields

        private DeviceDO deviceDO;
        private string id;
        private string name;
        private string _deviceType;
        private string ip;
        private string _serverName;
        private bool _isOnline;
        private bool _enableConfig = true;
        private string _statusDesc;

        #endregion Fields

        #region Constructors

        public DeviceViewModel()
        {
        }

        public DeviceViewModel(DeviceDO deviceDO)
        {
            DeviceDO = deviceDO;
            ID = deviceDO.ID;
            Name = deviceDO.Name;
            if (deviceDO.DefaultAgent() != null)
            {
                DeviceType = deviceDO.ProtocolType;
                IP = string.Join(Environment.NewLine, deviceDO.DeviceAddress.Select(item => item.IP));
                _serverName = deviceDO.DefaultAgent().Name;
            }
            IsOnline = deviceDO.IsOnline();
            StatusDesc = deviceDO.GetDeviceStatusDesc();
        }

        #endregion Constructors

        #region Properties

        public DeviceDO DeviceDO
        { get => deviceDO; private set { deviceDO = value; } }

        public string ProtocolType { get; set; }

        public string ID
        {
            get => id;
            set
            {
                id = value;
                OnPropertyChanged(nameof(ID));
            }
        }

        public string Name
        {
            get => name;
            set
            {
                name = value;
                OnPropertyChanged(nameof(Name));
            }
        }

        public string DeviceType
        {
            get => _deviceType;
            set
            {
                _deviceType = value;
                OnPropertyChanged(nameof(DeviceType));
            }
        }

        public string IP
        {
            get => ip;
            set
            {
                ip = value;
                OnPropertyChanged(nameof(IP));
            }
        }

        public string ServerName
        {
            get => _serverName;
            set
            {
                _serverName = value;
                OnPropertyChanged(nameof(ServerName));
            }
        }

        public bool IsOnline
        {
            get => _isOnline;
            set
            {
                _isOnline = value;
                OnPropertyChanged();
            }
        }

        public string StatusDesc
        {
            get => _statusDesc;
            set
            {
                _statusDesc = value;
                OnPropertyChanged(nameof(StatusDesc));
            }
        }

        public bool EnableConfig
        {
            get => _enableConfig;
            set
            {
                _enableConfig = value;
                OnPropertyChanged();
            }
        }

        #endregion Properties

        #region Methods

        internal void FormCopy(DeviceViewModel model)
        {
            ID = model.id;
            Name = model.name;
            DeviceType = model._deviceType;
            IP = model.ip;
            ServerName = model._serverName;
            IsOnline = model.IsOnline;
        }

        internal void Update(DeviceDO deviceDO)
        {
            DeviceDO = deviceDO;
            ID = deviceDO.ID;
            Name = deviceDO.Name;
            if (deviceDO.DefaultAgent() != null)
            {
                DeviceType = deviceDO.ProtocolType;
                IP = string.Join(Environment.NewLine, deviceDO.DeviceAddress.Select(item => item.IP));
                _serverName = deviceDO.DefaultAgent().Name;
            }
            IsOnline = deviceDO.IsOnline();
            StatusDesc = deviceDO.GetDeviceStatusDesc();
        }

        #endregion Methods
    }
}