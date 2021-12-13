// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0
using Firefly.Client.Common;
using Firefly.Client.Control;
using Firefly.Client.Service;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace Firefly.Client
{
    public class LivePreviewPageViewModel : BaseViewModel
    {
        #region Fields

        private bool _isSplitModePopOpen = false;
        private TreeItem _rootDeviceTreeItem;
        private ObservableCollection<RealTimePresetItemViewModel> _presetItems;
        private ObservableCollection<RealTimeCruiseItemViewModel> _cruiseItems;
        private RealTimePresetItemViewModel _selectedPreset;
        private RealTimeCruiseItemViewModel _selectedCruise;
        private Visibility _ptzControlVisibility = Visibility.Collapsed;
        private Visibility _cruiseControlVisibility = Visibility.Visible;
        private int _pollingStartPos = 0;
        private int _pollingCount = 0;
        private List<string> _pollingTimeList;
        private bool _isPollingStarted = false;
        private List<TreeItem> _pollingItems;
        private DisplayItemViewModel _selectedDisplayItem;
        private List<TreeItem> _searchingDisplayItemList;
        private string _searchText;
        private bool _isSearching;
        private List<TreeItem> _allDeviceItems;
        private SimpleCommand _searchTextCommand;
        private bool _isAddPollingMode;

        #endregion Fields

        #region Constructors

        public LivePreviewPageViewModel()
        {
            CruiseItemCacheDict = new Dictionary<int, List<LivePreviewCruiseItem>>();
            CruisePageList = new ObservableCollection<int>();
            PollingGroups = new ObservableCollection<PollingGroupViewModel>();
            _allDeviceItems = new List<TreeItem>();
            _rootDeviceTreeItem = new TreeItem
            {
                ID = Constants.DEFAULT_PARENT_ID,
                Name = string.Empty,
                DataObject = new object(),
                Children = new ObservableCollection<TreeItem>()
            };
            PTZControlVisibility = Visibility.Visible;

            _presetItems = new ObservableCollection<RealTimePresetItemViewModel>();
            _cruiseItems = new ObservableCollection<RealTimeCruiseItemViewModel>();
            _pollingTimeList = new List<string>
            {
                $"20 {I18n.Properties.Resources.Second}",
                $"40 {I18n.Properties.Resources.Second}",
                $"1 {I18n.Properties.Resources.Minute}",
                $"5 {I18n.Properties.Resources.Minute}"
            };

            InitDeviceTree();
            _searchTextCommand = new SimpleCommand(CanSearch, DoSearchText);
        }

        #endregion Constructors

        #region Properties

        public TreeItem RootDeviceTreeItem
        {
            get => _rootDeviceTreeItem;
            set
            {
                _rootDeviceTreeItem = value;
                OnPropertyChanged();
            }
        }

        public List<Tuple<int, int>> SplitModeList { get; set; }

        public Dictionary<int, List<LivePreviewCruiseItem>> CruiseItemCacheDict { get; set; }

        public SplitModeViewModel SelectedSplitModeVM { get; set; }

        public int SelectedSplitMode { get; set; }

        public bool IsSplitModePopOpen
        {
            get => _isSplitModePopOpen;
            set
            {
                _isSplitModePopOpen = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<int> CruisePageList { get; set; }

        public ObservableCollection<PollingGroupViewModel> PollingGroups { get; set; }

        public ObservableCollection<RealTimePresetItemViewModel> PresetItems
        {
            get => _presetItems;
            set
            {
                _presetItems = value;
                OnPropertyChanged();
            }
        }

        public RealTimePresetItemViewModel SelectedPreset
        {
            get => _selectedPreset;
            set
            {
                _selectedPreset = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<RealTimeCruiseItemViewModel> CruiseItems
        {
            get => _cruiseItems;
            set
            {
                _cruiseItems = value;
                OnPropertyChanged();
            }
        }

        public RealTimeCruiseItemViewModel SelectedCruise
        {
            get => _selectedCruise;
            set
            {
                _selectedCruise = value;
                OnPropertyChanged();
            }
        }

        public Visibility CruiseControlVisibility
        {
            get => _cruiseControlVisibility;
            set
            {
                _cruiseControlVisibility = value;
                OnPropertyChanged();
            }
        }

        public Visibility PTZControlVisibility
        {
            get => _ptzControlVisibility;
            set
            {
                _ptzControlVisibility = value;

                OnPropertyChanged();
            }
        }

        public DisplayItemViewModel SelectedDisplayItem
        {
            get => _selectedDisplayItem;
            set
            {
                _selectedDisplayItem = value;

                GetDeviceConfig();

                OnPropertyChanged();
                OnPropertyChanged(nameof(IsIRCameraPTZLayoutEnable));
                OnPropertyChanged(nameof(IsDCCameraPTZLayoutEnable));
            }
        }

        public bool IsIRCameraPTZLayoutEnable => string.Equals(_selectedDisplayItem?.DeviceType, DeviceType.IRCamera.ToString(), StringComparison.InvariantCultureIgnoreCase) && (_selectedDisplayItem?.HasDevice ?? false) && (_selectedDisplayItem?.ConnectStatus ?? false);

        public bool IsDCCameraPTZLayoutEnable => string.Equals(_selectedDisplayItem?.DeviceType, DeviceType.DCCamera.ToString(), StringComparison.InvariantCultureIgnoreCase) && (_selectedDisplayItem?.HasDevice ?? false) && (_selectedDisplayItem?.ConnectStatus ?? false);

        public List<string> PollingTimeList
        {
            get => _pollingTimeList;
            set => _pollingTimeList = value;
        }

        public bool IsPollingStarted
        {
            get => _isPollingStarted;
            set
            {
                _isPollingStarted = value;
                OnPropertyChanged();
            }
        }

        public List<TreeItem> SearchingDisplayItems
        {
            get => _searchingDisplayItemList;
            set
            {
                _searchingDisplayItemList = value;
                IsSearching = _searchingDisplayItemList != null && _searchingDisplayItemList.Count > 0;
                OnPropertyChanged();
            }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged();
            }
        }

        public SimpleCommand SearchTextCommand
        {
            get => _searchTextCommand;
            set
            {
                _searchTextCommand = value;
                OnPropertyChanged();
            }
        }

        public bool IsSearching
        {
            get => _isSearching;
            set
            {
                _isSearching = value;
                OnPropertyChanged();
            }
        }

        public bool IsAddCruisePlan
        {
            get => _isAddPollingMode;
            set
            {
                _isAddPollingMode = value;
                OnPropertyChanged();
            }
        }

        public string CurrentCruiseDeviceID { get; set; }

        public int CurrentCruiseId { get; set; }

        #endregion Properties

        #region Methods

        private void InitDeviceTree()
        {
            List<DeviceDO> deviceDOs = DeviceManager.Instance.GetCachedDeviceList();
            List<TreeItem> allItems = BuildDeviceTreeItems(deviceDOs);
            RootDeviceTreeItem.AddRange(allItems);
        }

        public void LoadDevicesForTreeData(List<DeviceDO> deviceDOs)
        {
            List<TreeItem> allItems = BuildDeviceTreeItems(deviceDOs);

            allItems?.ForEach(item =>
            {
                TreeItem treeItem = RootDeviceTreeItem.GetItem(it => it.ID == item.ID && it.ParentID == item.ParentID && it.ItemType == item.ItemType);
                item.IsExpanded = treeItem?.IsExpanded ?? false;
            });

            _allDeviceItems.Clear();
            List<TreeItem> channelItems = allItems.Where(it => it.ItemType == ItemType.Channel).ToList();
            channelItems.ForEach(item =>
            {
                TreeItem deviceItem = allItems.FirstOrDefault(it => it.ID == item.ParentID);
                if(deviceItem != null)
                {
                    TreeItem tempItem = new TreeItem
                    {
                        ID = item.ID,
                        Name = $"{deviceItem.Name} - {item.Name}",
                        DataObject = item.DataObject,
                        ParentID = item.ParentID,
                        ItemType = ItemType.Channel,
                        Parent = deviceItem,
                        Children = new ObservableCollection<TreeItem>()
                    };
                    _allDeviceItems.Add(tempItem);
                }
            });

            allItems.Sort(new TreeItemComparer());

            RootDeviceTreeItem.Dispose();
            RootDeviceTreeItem.AddRange(allItems);

            LoadPollingGroups();
        }

        private List<TreeItem> BuildDeviceTreeItems(List<DeviceDO> deviceDOs)
        {
            List<TreeItem> allItems = new List<TreeItem>();
            Dictionary<string, TreeItem> createdGroupNodes = new Dictionary<string, TreeItem>();
            var validDevices = deviceDOs?
                .Where(d => !string.Equals(d.DefaultAgent()?.Type, ServerType.EnvironmentMonitorAgent.ToString(), StringComparison.InvariantCultureIgnoreCase))
                .ToList();

            if (validDevices == null)
            {
                return allItems;
            }

            foreach (DeviceDO device in validDevices)
            {
                string parentID = Constants.DEFAULT_PARENT_ID;
                if (AppConfig.Instance.SupportLivePreviewGroup)
                {
                    string groupName = GetDeviceGroupName(device.Name);
                    if (!string.IsNullOrWhiteSpace(groupName))
                    {
                        string groupKey = $"Group_{groupName}";
                        if (!createdGroupNodes.TryGetValue(groupKey, out TreeItem groupItem))
                        {
                            groupItem = new TreeItem
                            {
                                ID = groupKey,
                                Name = groupName,
                                DataObject = new object(),
                                ParentID = Constants.DEFAULT_PARENT_ID,
                                ItemType = ItemType.Equipment,
                                Children = new ObservableCollection<TreeItem>()
                            };
                            allItems.Add(groupItem);
                            createdGroupNodes.Add(groupKey, groupItem);
                        }

                        parentID = groupItem.ID;
                    }
                }

                TreeItem deviceItem = new TreeItem
                {
                    ID = device.ID,
                    Name = device.Name,
                    DataObject = device,
                    ParentID = parentID,
                    ItemType = ItemType.Device,
                    IsOnline = device.IsOnline(),
                    Children = new ObservableCollection<TreeItem>()
                };
                allItems.Add(deviceItem);

                device.DeviceAddress?.ForEach(channel =>
                {
                    TreeItem channelItem = new TreeItem
                    {
                        ID = channel.ChannelID.ToString(),
                        Name = TranslateChannelType(channel.Type),
                        ParentID = device.ID,
                        Parent = deviceItem,
                        ItemType = ItemType.Channel,
                        DataObject = channel,
                        Children = new ObservableCollection<TreeItem>()
                    };
                    allItems.Add(channelItem);
                });
            }

            return allItems;
        }

        private string GetDeviceGroupName(string deviceName)
        {
            string[] nameParts = deviceName?.Split(new[] { '#' }, StringSplitOptions.RemoveEmptyEntries);
            return nameParts != null && nameParts.Length > 1 ? nameParts[0] : string.Empty;
        }

        public void LoadPreference()
        {
            if (LivePreviewPreference.Intance.SelectedMode != null && LivePreviewPreference.Intance.SelectedMode >= 0)
            {
                SelectedSplitMode = (int)LivePreviewPreference.Intance.SelectedMode;
            }
            else
            {
                SelectedSplitMode = 3;
            }
            if (LivePreviewPreference.Intance.IsPtzVisibe)
            {
                PTZControlVisibility = Visibility.Visible;
            }
            else
            {
                PTZControlVisibility = Visibility.Collapsed;
            }
        }

        private string TranslateChannelType(string channelType)
        {
            if (string.Equals(channelType, DeviceType.IRCamera.ToString(), StringComparison.InvariantCultureIgnoreCase))
            {
                return I18n.Properties.Resources.IRCamera;
            }
            else if (string.Equals(channelType, DeviceType.DCCamera.ToString(), StringComparison.InvariantCultureIgnoreCase))
            {
                return I18n.Properties.Resources.DCCamera;
            }
            else
            {
                return channelType;
            }
        }

        private async void GetDeviceConfig()
        {
            try
            {
                Mouse.SetCursor(Cursors.Wait);
                _presetItems?.Clear();
                _cruiseItems?.Clear();
                if (_selectedDisplayItem == null || !_selectedDisplayItem.HasDevice)
                {
                    return;
                }

                if (string.Equals(_selectedDisplayItem.DeviceType, DeviceType.DCCamera.ToString()))
                {
                    return;
                }

                if (_selectedDisplayItem.DeviceDO != null)
                {
                    DeviceConfigDataObject deviceConfigDO = await DeviceManager.Instance.GetDeviceConfig<DeviceConfigDataObject>(_selectedDisplayItem.DeviceDO);
                    deviceConfigDO?.PresetConfig?.PresetList?.ForEach(it =>
                    {
                        _presetItems.Add(new RealTimePresetItemViewModel
                        {
                            ID = it.ID
                        });
                    });
                    deviceConfigDO?.CruiseRouteConfig?.CruiseRouteList?.ForEach(it =>
                    {
                        _cruiseItems.Add(new RealTimeCruiseItemViewModel
                        {
                            ID = it.ID
                        });
                    });

                    if (string.Equals(CurrentCruiseDeviceID, _selectedDisplayItem.DeviceDO.ID))
                    {
                        var cruiseItem = _cruiseItems?.FirstOrDefault(it => it.ID == CurrentCruiseId);
                        if (cruiseItem != null)
                        {
                            cruiseItem.CruiseStatus = true;
                            SelectedCruise = cruiseItem;
                        }
                    }
                }
            }
            finally
            {
                Mouse.SetCursor(Cursors.Arrow);
            }
        }

        public void InitPollingGroup(PollingGroupViewModel pollingGroupViewModel)
        {
            List<PollingGroupItemViewModel> pollingGroupItemList = pollingGroupViewModel.PollingItems.OrderBy(it => it.Order).ToList();
            _pollingItems?.ForEach(it =>
            {
                it?.Parent?.Dispose();
                it?.Dispose();
            });
            _pollingItems = new List<TreeItem>();

            int totalCount = ((pollingGroupItemList.Last().Order / pollingGroupViewModel.SelectedSplitMode.ItemCount) + 1) * pollingGroupViewModel.SelectedSplitMode.ItemCount;

            for (int i = 0; i < totalCount; i++)
            {
                PollingGroupItemViewModel item = pollingGroupItemList.FirstOrDefault(it => it.Order == i);
                if (item != null)
                {
                    TreeItem channelItem = RootDeviceTreeItem.GetItem(channel => channel.ItemType == ItemType.Channel && string.Equals(channel.ParentID, item.DeviceID) && string.Equals(channel.ID, item.ChannelID));
                    if (channelItem != null)
                    {
                        _pollingItems.Add(channelItem.DeepClone());
                    }
                }
                else
                {
                    _pollingItems.Add(null);
                }
            }
            _pollingStartPos = 0;
            _pollingCount = SelectedSplitModeVM.ItemCount;
        }

        public List<TreeItem> PrevPollingGroup()
        {
            if (_pollingItems == null)
            {
                return null;
            }

            int startPos = _pollingStartPos - _pollingCount * 2;
            if (startPos < 0)
            {
                startPos = 0;
            }
            _pollingStartPos = startPos;
            int pollingCount = _pollingCount;
            if (_pollingItems.Count < _pollingStartPos + pollingCount)
            {
                pollingCount = _pollingItems.Count - _pollingStartPos;
            }

            return ChangePollingGroup(pollingCount);
        }

        public List<TreeItem> NextPollingGroup()
        {
            if (_pollingItems == null)
            {
                return null;
            }

            int pollingCount = _pollingCount;

            if (_pollingStartPos >= _pollingItems.Count)
            {
                _pollingStartPos = 0;
            }

            if (_pollingItems.Count < _pollingStartPos + pollingCount)
            {
                pollingCount = _pollingItems.Count - _pollingStartPos;
            }

            return ChangePollingGroup(pollingCount);
        }

        private List<TreeItem> ChangePollingGroup(int count)
        {
            List<TreeItem> currentPollingGroup = _pollingItems.GetRange(_pollingStartPos, count);

            List<TreeItem> result = new List<TreeItem>();
            for (int i = 0; i < SelectedSplitModeVM.ItemCount; i++)
            {
                if (i < count)
                {
                    result.Add(currentPollingGroup[i]);
                }
            }
            _pollingStartPos += _pollingCount;
            return result;
        }

        public async void LoadPollingGroups()
        {
            if (IsPollingStarted)
            {
                return;
            }
            PollingGroups.Clear();
            PollingGroupViewModel defaultPollingGroupVM = new PollingGroupViewModel();
            defaultPollingGroupVM.IsDefault = true;
            List<TreeItem> deviceTreeItemList = RootDeviceTreeItem.GetItems(it => it.ItemType == ItemType.Device);
            List<LivePreviewCruiseItem> pollingGroupItemList = new List<LivePreviewCruiseItem>();
            deviceTreeItemList?.ForEach(item =>
            {
                if (item.DataObject is DeviceDO deviceDO)
                {
                    deviceDO?.DeviceAddress?.ForEach(channel =>
                    {
                        pollingGroupItemList.Add(new LivePreviewCruiseItem
                        {
                            ChannelID = channel.ChannelID.ToString(),
                            DeviceID = deviceDO.ID,
                            Order = pollingGroupItemList.Count,
                        });
                    });
                }
            });
            LivePreviewCruisePlan pollingGroupInfo = new LivePreviewCruisePlan
            {
                Name = I18n.Properties.Resources.DefaultPolling,
                Second = 60,
                GroupItems = pollingGroupItemList
            };
            defaultPollingGroupVM.LoadFromPollingGroupInfo(pollingGroupInfo);
            PollingGroups.Add(defaultPollingGroupVM);

            LivePreviewCruisePlanConfigDO pollingGroupConfigDO = await SystemConfigManager.Instance.GetLivePreviewPollingGroup();
            pollingGroupConfigDO?.LivePreviewPollingGroupInfos?.ForEach(it =>
            {
                PollingGroupViewModel viewModel = new PollingGroupViewModel();
                viewModel.LoadFromPollingGroupInfo(it);
                PollingGroups.Add(viewModel);
            });
        }

        private bool CanSearch(object parameter)
        {
            return !string.IsNullOrWhiteSpace(SearchText);
        }

        private void DoSearchText(object parameter)
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                SearchingDisplayItems = null;
            }
            else
            {
                SearchingDisplayItems = _allDeviceItems.Where(it => it.Name.Contains(SearchText)).ToList();
            }
        }

        #endregion Methods
    }
}