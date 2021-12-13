// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using Firefly.Client.Common;
using Firefly.Client.Control;
using Firefly.Client.Service;

namespace Firefly.Client
{
    public class StoreServerConfigViewModel : BaseViewModel
    {
        private StoreServerConfigItemViewModel _selectedItem;
        public ObservableCollection<StoreServerConfigItemViewModel> Items { get; set; }
        public string StoreAddress { get; set; }

        public StoreServerConfigItemViewModel SelectedItem
        {
            get => _selectedItem;
            set
            {
                _selectedItem = value;
                OnPropertyChanged(nameof(SelectedItem));
            }
        }

        public StoreServerConfigViewModel()
        {
            Items = new ObservableCollection<StoreServerConfigItemViewModel>();
        }

        public void LoadDataObject(List<NVRDataObject> dataObjects, List<NVRChannelDataObject> nvrChannelDataObjects)
        {
            Items.Clear();
            dataObjects?.ForEach(it => { Items.Add(new StoreServerConfigItemViewModel(it, nvrChannelDataObjects.Count(x => x.NVRID == it.ID))); });
        }
    }

    public class StoreServerConfigItemViewModel : BaseViewModel
    {
        #region Fields

        private string _name;
        private string _protocolType;
        private string _userName;
        private string _password;
        private string _ip;
        private int _port;

        private Dictionary<string, int> _protocolToPortMap = new Dictionary<string, int>()
            {
                { NVRType.HIKSDK_NET.ToString(), Constants.HIK_NVR_DEFAULT_PORT },
                { NVRType.DHSDK_NET.ToString(), Constants.DH_NVR_DEFAULT_PORT },
                { NVRType.XMSDK_NET.ToString(), Constants.XM_NVR_DEFAULT_PORT },
                { NVRType.TWSDK_NET.ToString(), Constants.TW_NVR_DEFAULT_PORT }
            };

        #endregion

        #region Constructors

        public StoreServerConfigItemViewModel()
        {
            ProtocolType = DCCameraType.HIKSDK_NET.ToString();
            IP = Constants.DEFAULT_IP;
        }

        public StoreServerConfigItemViewModel(NVRDataObject dataObject, int channelCount)
        {
            ID = dataObject.ID;
            Name = dataObject.Name;
            ProtocolType = dataObject.ProtocolType;
            CanDelete = channelCount == 0;

            if (dataObject.Address != null)
            {
                ProtocolType = dataObject.ProtocolType;
                IP = dataObject.Address.IP;
                Port = dataObject.Address.Port;
                UserName = dataObject.Address.UserName;
                Password = dataObject.Address.Password;
            }
            else
            {
                IP = Constants.DEFAULT_IP;
                Port = Constants.DEFAULT_PORT;
            }
            OnPropertyChanged(nameof(Title));
        }

        #endregion

        #region Properties

        public string Title => ID == 0 ? I18n.Properties.Resources.Add : I18n.Properties.Resources.Edit;

        public int ID { get; set; }

        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                OnPropertyChanged(nameof(Name));
            }
        }

        public string ProtocolType
        {
            get => _protocolType;
            set
            {
                _protocolType = value;
                Port = _protocolToPortMap[_protocolType];
                OnPropertyChanged(nameof(ProtocolType));
            }
        }

        public string Address { get; set; }

        public string UserName
        {
            get => _userName;
            set
            {
                _userName = value;
                OnPropertyChanged(nameof(UserName));
            }
        }

        public string IP
        {
            get => _ip;
            set
            {
                _ip = value;
                Address = $"{_ip}:{_port}";
                OnPropertyChanged(nameof(IP));
                OnPropertyChanged(nameof(Address));
            }
        }

        public int Port
        {
            get => _port;
            set
            {
                if (value > 65535 || value < 0)
                {
                    return;
                }

                _port = value;
                Address = $"{_ip}:{_port}";
                OnPropertyChanged(nameof(Port));
                OnPropertyChanged(nameof(Address));
            }
        }

        public string Password
        {
            get => _password;
            set
            {
                _password = value;
                OnPropertyChanged(nameof(Password));
            }
        }

        public List<string> ProtocolTypeList => new List<string> { NVRType.HIKSDK_NET.ToString(), NVRType.DHSDK_NET.ToString(), NVRType.XMSDK_NET.ToString(), NVRType.TWSDK_NET.ToString() };

        public bool CanDelete { get; private set; }

        #endregion

        #region Methods

        public NVRDataObject GetDataObject()
        {
            NVRDataObject dataObject = new NVRDataObject
            {
                ID = ID,
                Name = Name,
                ProtocolType = ProtocolType
            };
            dataObject.Address = new NVRAddressDataObject
            {
                IP = IP,
                Password = Password,
                Port = Port,
                UserName = UserName
            };

            return dataObject;
        }

        public StoreServerConfigItemViewModel Clone()
        {
            return new StoreServerConfigItemViewModel
            {
                ID = ID,
                Name = Name,
                ProtocolType = ProtocolType,
                IP = IP,
                Port = Port,
                Address = Address,
                UserName = UserName,
                Password = Password
            };
        }

        #endregion
    }
}