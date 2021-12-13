// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using Firefly.Client.Control;
using Firefly.Client.Service;

namespace Firefly.Client
{
    public class StorePlanningViewModel : BaseViewModel
    {
        private StorePlanningNodeViewModel _selectedDevice;
        private NVRItemViewModel _selectedNVR;
        private NVRChannelItemViewModel _selectedNVRChannel;

        public StorePlanningViewModel()
        {
            Items = new ObservableCollection<StorePlanningNodeViewModel>();
            NVRItems = new ObservableCollection<NVRItemViewModel>();
            NVRChannelItems = new ObservableCollection<NVRChannelItemViewModel>();
        }

        public ObservableCollection<NVRItemViewModel> NVRItems { get; set; }

        public ObservableCollection<NVRChannelItemViewModel> NVRChannelItems { get; set; }

        public ObservableCollection<StorePlanningNodeViewModel> Items { get; set; }

        public StorePlanningNodeViewModel SelectedDevice
        {
            get => _selectedDevice;
            set
            {
                _selectedDevice = value;
                OnPropertyChanged(nameof(HasSelectedDevice));
                OnPropertyChanged(nameof(SelectedDevice));
            }
        }

        public bool HasSelectedDevice => SelectedDevice != null;

        public NVRItemViewModel SelectedNVR
        {
            get => _selectedNVR;
            set
            {
                _selectedNVR = value;
                UpdateCurrentNVRChannel();
                OnPropertyChanged();
            }
        }

        public NVRChannelItemViewModel SelectedNVRChannel
        {
            get => _selectedNVRChannel;
            set
            {
                _selectedNVRChannel = value;
                OnPropertyChanged();
            }
        }

        public async Task LoadDeviceDOs(List<DeviceDO> deviceDOs)
        {
            Items.Clear();

            await Task.Delay(20);

            SelectedDevice = null;
            if (deviceDOs != null)
            {
                foreach (DeviceDO deviceDO in deviceDOs)
                {
                    if(Items.Any(it=>string.Equals(it.DeviceID, deviceDO.ID)))
                    {
                        continue;
                    }
                    StorePlanningNodeViewModel viewModel = new StorePlanningNodeViewModel
                    {
                        Name = deviceDO.Name,
                        DeviceID = deviceDO.ID,
                        DeviceDO = deviceDO
                    };

                    if (string.Equals(deviceDO.DefaultAgent().Type, ServerType.EnvironmentMonitorAgent.ToString(), StringComparison.InvariantCultureIgnoreCase))
                    {
                        continue;
                    }

                    ServerDO serverDataObject = deviceDO.AgentAddress.FirstOrDefault(it => it.Type.Equals(ServerType.StoreAgent.ToString()));
                    if (serverDataObject != null)
                    {
                        viewModel.StoreServerAddress = serverDataObject.GetServerAddress();
                        viewModel.StoreServerID = serverDataObject.ID;
                    }

                    deviceDO.DeviceAddress?.ForEach(child =>
                    {
                        viewModel.Children.Add(new StorePlanningNodeViewModel
                        {
                            DeviceID = deviceDO.ID,
                            Name = $"{I18n.Properties.Resources.Channel}{child.ChannelID}",
                            DeviceChannelID = child.ChannelID,
                            IsDeviceChannel = true,
                            StoreServerAddress = viewModel.StoreServerAddress,
                            StoreServerID = viewModel.StoreServerID,
                            DeviceDO = deviceDO
                        });
                    });
                    Items.Add(viewModel);
                }
            }
        }

        public async void LoadNVRs()
        {
            NVRItems.Clear();
            NVRItemViewModel defaultStoreNVRItemVM = NVRItemViewModel.CreateDefault();
            NVRItems.Add(defaultStoreNVRItemVM);
            if (!string.IsNullOrWhiteSpace(SelectedDevice?.StoreServerAddress))
            {
                List<NVRDataObject> nvrDOs = await StoreManager.Instance.GetNVRList(SelectedDevice.StoreServerAddress);

                foreach (NVRDataObject nvrDO in nvrDOs)
                {
                    NVRItemViewModel nvrItem = new NVRItemViewModel(nvrDO);
                    NVRItems.Add(nvrItem);
                }

                if (SelectedDevice.IsDeviceChannel)
                {
                    List<NVRChannelDataObject> channelDOs = await StoreManager.Instance.GetChannelsByDeviceID(SelectedDevice.StoreServerAddress, SelectedDevice.DeviceID);

                    NVRChannelDataObject currentNVRChannelDO = channelDOs.FirstOrDefault(it => it.ChannelID == SelectedDevice.DeviceChannelID);

                    if (currentNVRChannelDO != null)
                    {
                        SelectedDevice.NVRChannelDO = currentNVRChannelDO;

                        NVRItemViewModel firstOrDefault = NVRItems.FirstOrDefault(it => it.NVRID == currentNVRChannelDO.NVRID);
                        if (firstOrDefault != null)
                        {
                            SelectedNVR = firstOrDefault;
                        }
                        else
                        {
                            SelectedNVR = NVRItems.First();
                        }
                    }
                    else
                    {
                        SelectedNVR = NVRItems.First();
                    }
                }
            }
        }

        private async void UpdateCurrentNVRChannel()
        {
            NVRChannelItems.Clear();
            NVRChannelItems.Add(NVRChannelItemViewModel.CreatedDefault());

            if (SelectedNVR != null && SelectedNVR.NVRID != StorePlanningControl.INVALID_NVR_ID)
            {
                List<NVRChannelInfoDataObject> nvrChannelInfoDOList = await StoreManager.Instance.GetChannelsByNVRID(SelectedDevice.StoreServerAddress, SelectedNVR.NVRID);

                nvrChannelInfoDOList?.ForEach(item =>
                {
                    if (SelectedNVR != null)
                    {
                        NVRChannelItems.Add(new NVRChannelItemViewModel(SelectedNVR.NVRID, item));
                    }
                });

                if (SelectedDevice.NVRChannelDO != null)
                {
                    NVRChannelItemViewModel nvrChannelItemViewModel = NVRChannelItems.FirstOrDefault(it =>
                    {
                        if (it.NVRID == SelectedDevice.NVRChannelDO.NVRID
                            && it.ChannelInfo != null
                            && SelectedDevice.NVRChannelDO.ChannelInfo != null
                            && it.ChannelInfo.NVRChannelID == SelectedDevice.NVRChannelDO.ChannelInfo.NVRChannelID)
                        {
                            return true;
                        }
                        return false;
                    });

                    SelectedNVRChannel = nvrChannelItemViewModel ?? NVRChannelItems.First();
                }
                else
                {
                    SelectedNVRChannel = NVRChannelItems.First();
                }
            }
            else if (SelectedNVR != null && SelectedNVR.NVRID == StorePlanningControl.INVALID_NVR_ID)
            {
                SelectedNVRChannel = NVRChannelItems.First();
            }
        }

        public void Clear()
        {
            Items.Clear();
        }
    }

    public class StorePlanningNodeViewModel : BaseViewModel
    {
        #region Fields

        private string _name;

        #endregion Fields

        #region Constructors

        public StorePlanningNodeViewModel()
        {
            Children = new ObservableCollection<StorePlanningNodeViewModel>();
        }

        #endregion Constructors

        #region Properties

        public ObservableCollection<StorePlanningNodeViewModel> Children { get; set; }

        public string DeviceID { get; set; }

        public int DeviceChannelID { get; set; }

        public bool IsDeviceChannel { get; set; }

        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                OnPropertyChanged();
            }
        }

        public int StoreServerID { get; set; }

        public string StoreServerAddress { get; set; }

        public DeviceDO DeviceDO { get; set; }

        public NVRChannelDataObject NVRChannelDO { get; set; }

        #endregion Properties
    }

    public class NVRItemViewModel : BaseViewModel
    {
        #region Constructors

        public NVRItemViewModel(NVRDataObject dataObject)
        {
            NVRID = dataObject.ID;
            Name = dataObject.Name;
            ProtocolType = dataObject.ProtocolType;
        }

        #endregion Constructors

        #region Properties

        public int ID { get; set; }
        public int NVRID { get; set; }
        public string Name { get; set; }
        public string ProtocolType { get; set; }

        #endregion Properties

        #region method

        public static NVRItemViewModel CreateDefault()
        {
            return new NVRItemViewModel(new NVRDataObject
            {
                ID = StorePlanningControl.INVALID_NVR_ID,
                Name = I18n.Properties.Resources.None,
                ProtocolType = string.Empty
            });
        }

        #endregion method
    }

    public class NVRChannelItemViewModel : BaseViewModel
    {
        #region Constructors

        public NVRChannelItemViewModel(int nvrID, NVRChannelInfoDataObject nvrChannelInfoDO)
        {
            NVRID = nvrID;
            ChannelInfo = nvrChannelInfoDO;
            if (nvrChannelInfoDO != null)
            {
                Name = $"{I18n.Properties.Resources.Channel}{nvrChannelInfoDO.NVRChannelID + 1}";
            }
        }

        #endregion Constructors

        #region Properties

        public int ID { get; set; }
        public int NVRID { get; set; }
        public string DeviceID { get; set; }
        public int ChannelID { get; set; }
        public string Name { get; set; }
        public NVRChannelInfoDataObject ChannelInfo { get; set; }

        #endregion Properties

        #region method

        public NVRChannelDataObject ToNVRChannelDataObject()
        {
            return new NVRChannelDataObject(ID, NVRID, DeviceID, ChannelID, ChannelInfo);
        }

        public static NVRChannelItemViewModel CreatedDefault()
        {
            return new NVRChannelItemViewModel(StorePlanningControl.INVALID_NVR_CHANNEL_ID, null)
            {
                Name = I18n.Properties.Resources.None
            };
        }

        #endregion method
    }
}