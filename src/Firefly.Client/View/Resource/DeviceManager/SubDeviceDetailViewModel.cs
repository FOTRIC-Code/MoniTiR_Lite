// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System.Windows;
using System.Collections.Generic;

using Firefly.Client.Common;
using Firefly.Client.Control;
using Firefly.Client.Service;

namespace Firefly.Client
{
    public class SubDeviceDetailViewModel : BaseViewModel
    {
        #region Fields

        private Visibility _usernameVisibility = Visibility.Visible;
        private Visibility _passwordVisibility = Visibility.Visible;
        private Visibility _queryButtonVisibility = Visibility.Collapsed;
        private Visibility _channelListVisibility = Visibility.Collapsed;
        private bool _isSaveEnabled = true;
        private string _title;

        #endregion

        #region Properties

        public SubDeviceViewModel SubDeviceVM { get; set; }

        public AgentViewModel AgentVM { get; set; }

        public List<string> ProtocolList { get; set; }

        public Visibility UserNameVisibility
        {
            get => _usernameVisibility;
            set
            {
                _usernameVisibility = value;
                OnPropertyChanged();
            }
        }

        public Visibility PasswordVisibility
        {
            get => _passwordVisibility;
            set
            {
                _passwordVisibility = value;
                OnPropertyChanged();
            }
        }

        public Visibility QueryButtonVisibility
        {
            get => _queryButtonVisibility;
            set
            {
                _queryButtonVisibility = value;
                OnPropertyChanged();
            }
        }

        public Visibility ChannelListVisibility
        {
            get => _channelListVisibility;
            set
            {
                _channelListVisibility = value;
                OnPropertyChanged();
            }
        }

        public bool IsSaveEnabled
        {
            get => _isSaveEnabled;
            set
            {
                _isSaveEnabled = value;
                OnPropertyChanged();
            }
        }

        public string Title
        {
            get => _title;
            set
            {
                _title = value;
                OnPropertyChanged();
            }
        }

        #endregion

        #region Constructors

        public SubDeviceDetailViewModel(SubDeviceViewModel subDeviceVM)
        {
            SubDeviceVM = new SubDeviceViewModel();
            ProtocolList = new List<string>();
            if (subDeviceVM != null)
            {
                SubDeviceVM.CopyFrom(subDeviceVM);
                subDeviceVM.SupportedProtocolTypeList?.ForEach(it => { ProtocolList.Add(it); });
                if (string.IsNullOrWhiteSpace(SubDeviceVM.Protocol) && ProtocolList.Count > 0)
                {
                    SubDeviceVM.Protocol = ProtocolList[0];
                }

                if (string.Equals(SubDeviceVM.Protocol, SensorType.SRCSDK_NET.ToString(), System.StringComparison.InvariantCultureIgnoreCase)
                    || string.Equals(SubDeviceVM.Protocol, SensorType.MODBUS_TCP_MASTER_NET.ToString(), System.StringComparison.InvariantCultureIgnoreCase))
                {
                    UserNameVisibility = Visibility.Collapsed;
                    PasswordVisibility = Visibility.Collapsed;
                    QueryButtonVisibility = Visibility.Visible;
                    ChannelListVisibility = Visibility.Visible;
                }

                if (string.Equals(SubDeviceVM.Protocol, DCCameraType.HIKSDK_NET.ToString(), System.StringComparison.InvariantCultureIgnoreCase)
                    || string.Equals(SubDeviceVM.Protocol, DCCameraType.DHSDK_NET.ToString(), System.StringComparison.InvariantCultureIgnoreCase)
                    || string.Equals(SubDeviceVM.Protocol, DCCameraType.XMSDK_NET.ToString(), System.StringComparison.InvariantCultureIgnoreCase)
                    || string.Equals(SubDeviceVM.Protocol, DCCameraType.ONVIF_DC_NET.ToString(), System.StringComparison.InvariantCultureIgnoreCase)
                    || string.Equals(SubDeviceVM.Protocol, DCCameraType.TWSDK_NET.ToString(), System.StringComparison.InvariantCultureIgnoreCase)
                    || string.Equals(SubDeviceVM.Protocol, ACCameraType.IRTEKACSDK_NET.ToString(), System.StringComparison.InvariantCultureIgnoreCase))
                {
                    ChannelListVisibility = Visibility.Visible;
                }
            }
        }

        #endregion

        #region Methods

        public void LoadSensorChannel()
        {
            if (AgentVM != null)
            {
                DeviceAddressDataObject deviceAddressDO = new DeviceAddressDataObject
                {
                    IP = SubDeviceVM.IP,
                    Port = SubDeviceVM.Port,
                    ProtocolType = SubDeviceVM.Protocol
                };
            }
        }

        #endregion
    }
}
