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
    public class PollingGroupViewModel : BaseViewModel
    {
        private string _name;
        private bool _isAdd = true;
        private string _splitModeDesc;
        private int _second;
        private int _cycleTimeIndex;

        private SplitModeViewModel _splitModeViewModel;
        private List<SplitModeViewModel> _splitList;

        private bool _isRunning = false;
        private bool _isDefault = false;

        public PollingGroupViewModel()
        {
            PollingItems = new ObservableCollection<PollingGroupItemViewModel>();
            AllDeviceItems = new ObservableCollection<PollingGroupItemViewModel>();
            CycleTimeIndex = 0;
            _splitList = SplitVideoViewModel.GetSplitModelList();
            SelectedSplitMode = _splitList[0];
        }

        public ObservableCollection<PollingGroupItemViewModel> PollingItems { get; private set; }

        public ObservableCollection<PollingGroupItemViewModel> AllDeviceItems { get; set; }

        public LivePreviewCruisePlan PollingGroupInfo { get; set; }

        public bool IsAdd
        {
            get => _isAdd;
            set
            {
                _isAdd = value;
                OnPropertyChanged();
            }
        }

        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                OnPropertyChanged();
            }
        }

        public string SplitModeDesc
        {
            get => _splitModeDesc;
            set
            {
                _splitModeDesc = value;
                OnPropertyChanged();
            }
        }

        public SplitModeViewModel SelectedSplitMode
        {
            get => _splitModeViewModel;
            set
            {
                _splitModeViewModel = value;
                SplitModeDesc = $"{_splitModeViewModel.Rows} row X {_splitModeViewModel.Columns} col";
                OnPropertyChanged();
            }
        }

        public List<SplitModeViewModel> SplitList
        {
            get => _splitList;
            set
            {
                _splitList = value;
                OnPropertyChanged();
            }
        }

        public int CycleTimeIndex
        {
            get => _cycleTimeIndex;
            set
            {
                _cycleTimeIndex = value;
                Second = TranslateToSecond(value);
                OnPropertyChanged();
            }
        }

        public int Second
        {
            get => _second;
            set
            {
                _second = value;
                OnPropertyChanged();
            }
        }

        public bool IsRunning
        {
            get => _isRunning;
            set
            {
                _isRunning = value;
                OnPropertyChanged();
            }
        }

        public bool IsDefault
        {
            get => _isDefault;
            set
            {
                _isDefault = value;
                OnPropertyChanged();
            }
        }

        public async Task LoadData()
        {
            await Task.Delay(20);

            List<DeviceDO> deviceList = DeviceManager.Instance.GetCachedDeviceList();
            foreach (DeviceDO device in deviceList)
            {
                if (string.Equals(device.DefaultAgent().Type, ServerType.EnvironmentMonitorAgent.ToString(), StringComparison.InvariantCultureIgnoreCase))
                {
                    continue;
                }
                device.DeviceAddress?.ForEach(item =>
                {
                    bool isEnable = !PollingItems.Any(it => string.Equals(it.DeviceID, device.ID) && string.Equals(it.ChannelID, item.ChannelID.ToString()));

                    AllDeviceItems.Add(new PollingGroupItemViewModel
                    {
                        ChannelID = item.ChannelID.ToString(),
                        DeviceID = device.ID,
                        GroupName = device.Name,
                        DeviceName = device.Name,
                        ChannelName = $"{I18n.Properties.Resources.Channel}{item.ChannelID}",
                        Order = 0,
                        IsEnabled = true,
                    });
                });
            }

            if (PollingItems.Count > 0)
            {
                foreach (var pollingItem in PollingItems)
                {
                    var item = AllDeviceItems.FirstOrDefault(it => string.Equals(it.DeviceID, pollingItem.DeviceID) && string.Equals(it.ChannelID, pollingItem.ChannelID));
                    if (item != null)
                    {
                        item.IsEnabled = false;
                        item.IsSelected = true;
                    }
                }
            }
        }

        public List<PollingGroupItemViewModel> GetDataGridSelection()
        {
            return PollingItems.Where(it => it.IsSelected).ToList(); ;
        }

        public void LoadFromPollingGroupInfo(LivePreviewCruisePlan info)
        {
            PollingGroupInfo = info;
            Name = info.Name;
            Second = info.Second;
            if(info.SplitMode == 0)
            {
                SelectedSplitMode = SplitList[0];
            }
            else
            {
                SelectedSplitMode = SplitList?.FirstOrDefault(it => it.Mode == info.SplitMode);
            }
            CycleTimeIndex = TranslateToCycleIndex(info.Second);
            PollingItems.Clear();
            info.GroupItems?.OrderBy(it => it.Order).ToList().ForEach(it =>
            {
                PollingGroupItemViewModel itemViewModel = new PollingGroupItemViewModel();
                itemViewModel.LoadData(it);
                PollingItems.Add(itemViewModel);
            });
        }

        public LivePreviewCruisePlan ToLivePreviewPollingGroupInfo()
        {
            return new LivePreviewCruisePlan
            {
                Name = Name,
                Second = Second,
                SplitMode = SelectedSplitMode.Mode,
                GroupItems = PollingItems.Select(it => it.ToPollingGroupItemInfo()).ToList(),
            };
        }

        public int TranslateToSecond(int index)
        {
            if (index == 0)
            {
                return 30;
            }
            if (index == 1)
            {
                return 60;
            }
            if (index == 2)
            {
                return 300;
            }
            if (index == 3)
            {
                return 600;
            }
            return 30;
        }

        public int TranslateToCycleIndex(int second)
        {
            if (second == 30)
            {
                return 0;
            }
            if (second == 60)
            {
                return 1;
            }
            if (second == 300)
            {
                return 2;
            }
            if (second == 600)
            {
                return 3;
            }
            return 0;
        }

        public PollingGroupViewModel DeepClone()
        {
            PollingGroupViewModel clone = new PollingGroupViewModel();
            clone.LoadFromPollingGroupInfo(ToLivePreviewPollingGroupInfo());
            clone.IsRunning = IsRunning;
            return clone;
        }
    }

    public class PollingGroupItemViewModel : BaseViewModel
    {
        private bool _isSelected;
        private bool _isEnabled;
        private int _order;
        private string _groupName;

        public PollingGroupItemViewModel()
        {
        }

        public string DeviceID { get; set; }
        public string DeviceName { get; set; }
        public string ChannelID { get; set; }
        public string ChannelName { get; set; }

        public int Order
        {
            get => _order;
            set
            {
                _order = value;
                OnPropertyChanged();
            }
        }

        public string GroupName
        {
            get => _groupName;
            set
            {
                _groupName = value;
                OnPropertyChanged();
            }
        }

        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                _isEnabled = value;
                OnPropertyChanged();
            }
        }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                OnPropertyChanged();
            }
        }

        public void LoadData(TreeItem treeItem, int order)
        {
            DeviceID = treeItem.ParentID;
            DeviceName = treeItem.Parent.Name;
            ChannelID = treeItem.ID;
            ChannelName = treeItem.Name;
            Order = order;
        }

        public PollingGroupItemViewModel DeepClone()
        {
            PollingGroupItemViewModel clone = new PollingGroupItemViewModel();
            clone.DeviceID = DeviceID;
            clone.DeviceName = DeviceName;
            clone.ChannelID = ChannelID;
            clone.ChannelName = ChannelName;
            clone.Order = Order;
            clone.IsEnabled = true;
            clone.IsSelected = false;
            return clone;
        }

        public void LoadData(LivePreviewCruiseItem itemInfo)
        {
            if (!string.IsNullOrWhiteSpace(itemInfo.DeviceID))
            {
                DeviceID = itemInfo.DeviceID;
                ChannelID = itemInfo.ChannelID;
                ChannelName = $"{I18n.Properties.Resources.Channel}{ChannelID}";
                if (string.IsNullOrWhiteSpace(itemInfo.DeviceName))
                {
                    DeviceName = DeviceManager.Instance.GetCached(DeviceID)?.Name ?? "";
                }
                else
                {
                    DeviceName = itemInfo.DeviceName;
                }
            }
            Order = itemInfo.Order;
        }

        public LivePreviewCruiseItem ToPollingGroupItemInfo()
        {
            return new LivePreviewCruiseItem
            {
                DeviceName = DeviceName,
                DeviceID = DeviceID,
                ChannelID = ChannelID,
                Order = Order,
            };
        }
    }
}