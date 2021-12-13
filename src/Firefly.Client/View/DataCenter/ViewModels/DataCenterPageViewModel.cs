// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using Firefly.Client.Common;
using Firefly.Client.Control;
using Firefly.Client.Service;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Firefly.Client
{
    public class DataCenterPageViewModel : BaseViewModel
    {
        private string _searchText;
        private TreeItem _selectionTree;
        private List<DeviceDO> _deviceDOs;
        private readonly List<DeviceDataStatisticsViewModel> _allDeviceDataList;

        public ObservableCollection<DeviceDataStatisticsViewModel> DeviceDataList { get; set; }

        public DeviceDataStatisticsViewModel SelectedDeviceDataRecord { get; set; }

        public TreeItem SelectionTree
        {
            get => _selectionTree;
            set
            {
                _selectionTree = value;
                OnPropertyChanged(nameof(SelectionTree));
            }
        }

        public DateTime BeginTime { get; set; } = DateTime.Today;

        public DateTime EndTime { get; set; } = DateTime.Now;

        public bool HasItem => SelectionTree.Children.Any();

        public bool HasRecord => DeviceDataList.Any();

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText == value)
                {
                    return;
                }
                _searchText = value;
                OnPropertyChanged();
                FilterList();
            }
        }

        public DataCenterPageViewModel()
        {
            _allDeviceDataList = new List<DeviceDataStatisticsViewModel>();
            DeviceDataList = new ObservableCollection<DeviceDataStatisticsViewModel>();
            SelectionTree = new TreeItem()
            {
                ID = Constants.DEFAULT_ID,
                Name = string.Empty,
                DataObject = new object(),
                Children = new ObservableCollection<TreeItem>()
            };
            DeviceDataList.CollectionChanged += DataList_CollectionChanged;
        }

        private void DataList_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged(nameof(HasRecord));
        }

        public void LoadDevice(List<DeviceDO> deviceDOs)
        {
            DeviceDataList.Clear();
            _deviceDOs = deviceDOs ?? new List<DeviceDO>();

            List<TreeItem> allItems = new List<TreeItem>();
            allItems.AddRange(_deviceDOs.Select(x => new TreeItem() { ID = x.ID, ParentID = Constants.DEFAULT_ID, Name = x.Name, DataObject = x, ItemType = ItemType.Device, Children = new ObservableCollection<TreeItem>() }));
            SelectionTree?.Children.Clear();
            SelectionTree.AddRange(allItems);

            OnPropertyChanged(nameof(HasItem));
        }

        public void LoadDeviceStatistics(List<DeviceDataStatisticsDO> deviceDataStatistics)
        {
            _allDeviceDataList.Clear();
            foreach (DeviceDataStatisticsDO fireDataStatisticsDO in deviceDataStatistics)
            {
                _allDeviceDataList.Add(new DeviceDataStatisticsViewModel(GetFullRegionName(fireDataStatisticsDO.DeviceID), _deviceDOs.FirstOrDefault(x => x.ID == fireDataStatisticsDO.DeviceID), fireDataStatisticsDO));
            }
            FilterList();
        }

        public List<string> ConvertAllToCSV()
        {
            List<string> csvList = new List<string>();
            csvList.Add($"{I18n.Properties.Resources.GroupName}," +
                    $"{I18n.Properties.Resources.Device}," +
                    $"{I18n.Properties.Resources.ChannelName}," +
                    $"{I18n.Properties.Resources.Preset}," +
                    $"{I18n.Properties.Resources.DataRecord}," +
                    $"{I18n.Properties.Resources.AlarmRecord}");
            foreach (DeviceDataStatisticsViewModel deviceData in DeviceDataList)
            {
                csvList.Add(string.Format("{0},{1},{2},{3},{4},{5}",
                    deviceData.FullRegionName,
                    deviceData.DeviceName,
                    deviceData.ChannelName,
                    deviceData.PresetName,
                    deviceData.DataCount,
                    deviceData.AlarmStatistics));
            }

            return csvList;
        }

        private string GetFullRegionName(string id)
        {
            TreeItem treeItem = _selectionTree.GetItem(x => x.DataObject is DeviceDO deviceDO && deviceDO.ID == id);
            return treeItem?.Name ?? string.Empty;
        }

        private void FilterList()
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                DeviceDataList.Clear();
                foreach (DeviceDataStatisticsViewModel deviceData in _allDeviceDataList)
                {
                    DeviceDataList.Add(deviceData);
                }
                return;
            }
            List<DeviceDataStatisticsViewModel> deviceDataList = _allDeviceDataList.Where(x => x.DeviceName.Contains(SearchText) || (!string.IsNullOrWhiteSpace(x.PresetName) && x.PresetName.Contains(SearchText))).ToList();
            DeviceDataList.Clear();
            foreach (DeviceDataStatisticsViewModel deviceData in deviceDataList)
            {
                DeviceDataList.Add(deviceData);
            }
        }
    }

    public class DeviceDataStatisticsViewModel
    {
        public string FullRegionName { get; set; }

        public string DeviceID { get; set; }

        public string DeviceName { get; set; }

        public int ChannelID { get; set; }

        public string ChannelName { get; set; }

        public int PresetID { get; set; }

        public string PresetName { get; set; }

        public int DataCount { get; set; }

        public string AlarmStatistics { get; set; }

        public DeviceDO DeviceDO { get; set; }

        public DeviceDataStatisticsViewModel(string fullRegionName, DeviceDO deviceDO, DeviceDataStatisticsDO fireDataStatisticsDO)
        {
            FullRegionName = fullRegionName;
            DeviceDO = deviceDO;
            DeviceID = fireDataStatisticsDO.DeviceID;
            DeviceName = fireDataStatisticsDO.DeviceName;
            PresetID = fireDataStatisticsDO.PresetID;
            PresetName = fireDataStatisticsDO.PresetName;
            ChannelID = fireDataStatisticsDO.ChannelID;
            ChannelName = fireDataStatisticsDO.ChannelName;
            DataCount = fireDataStatisticsDO.Statistics.DataStatistics;
            AlarmStatistics = fireDataStatisticsDO.Statistics.AlarmStatistics.Aggregate("", (cur, x) => cur + x.AlarmLevel + ":" + x.Count + ",").TrimEnd(',');
        }
    }

    public class TreeItem : BaseViewModel
    {
        private bool? _isChecked = false;
        private bool _isSelected = false;
        private bool _isExpanded = false;
        private bool _isEnabled = true;
        private bool _isOnline = true;

        private string _name;

        public TreeItem Parent { get; set; }

        public string ID { get; set; }

        public string ParentID { get; set; }

        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                OnPropertyChanged();
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

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                OnPropertyChanged();
            }
        }

        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                _isExpanded = value;
                OnPropertyChanged();
            }
        }

        public bool? IsChecked
        {
            get => _isChecked;
            set
            {
                if (IsNotLeafNode)
                {
                    switch (_isChecked)
                    {
                        case true:
                            _isChecked = false;
                            break;

                        case false:
                            _isChecked = true;
                            break;

                        case null:
                            _isChecked = false;
                            break;
                    }

                    Broadcast();
                }
                else
                {
                    _isChecked = value;
                }

                BubbleBroadcast();
                OnPropertyChanged(nameof(IsChecked));
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

        public object DataObject { get; set; }

        public bool IsNotLeafNode => Children != null && Children.Count > 0;

        public ObservableCollection<TreeItem> Children { get; set; }

        public ItemType ItemType { get; set; }

        private void BubbleBroadcast()
        {
            TreeItem parent = Parent;
            do
            {
                if (parent == null)
                {
                    break;
                }
                parent.FlushChecked();
                parent = parent.Parent;
            } while (parent != null);
        }

        private void Broadcast()
        {
            Queue<TreeItem> queue = new Queue<TreeItem>();
            queue.Enqueue(this);
            while (queue.Count > 0)
            {
                TreeItem item = queue.Dequeue();
                foreach (TreeItem deviceTreeChild in item.Children)
                {
                    queue.Enqueue(deviceTreeChild);
                    deviceTreeChild.SetChecked(_isChecked);
                }
            }
        }

        public void FlushChecked()
        {
            if (Children.All(x => x.IsChecked == true))
            {
                _isChecked = true;
            }
            else if (Children.All(x => x.IsChecked == false))
            {
                _isChecked = false;
            }
            else
            {
                _isChecked = null;
            }
            OnPropertyChanged(nameof(IsChecked));
        }

        public void SetChecked(bool? isChecked)
        {
            _isChecked = isChecked;
            OnPropertyChanged(nameof(IsChecked));
        }

        public TreeItem GetItem(Func<TreeItem, bool> func)
        {
            Queue<TreeItem> queue = new Queue<TreeItem>();
            queue.Enqueue(this);
            while (queue.Count > 0)
            {
                TreeItem curItem = queue.Dequeue();
                if (func(curItem))
                {
                    return curItem;
                }

                foreach (TreeItem child in curItem.Children)
                {
                    queue.Enqueue(child);
                }
            }

            return null;
        }

        public void Dispose()
        {
            if (Children != null)
            {
                foreach (var child in Children)
                {
                    child.Dispose(); // 递归清理
                }
                Children.Clear();
            }

            Parent = null;
            DataObject = null;
        }

        public TreeItem DeepClone()
        {
            TreeItem treeItem = new TreeItem();

            treeItem.ParentID = ParentID;
            treeItem.Parent = Parent?.DeepClone();
            treeItem.DataObject = DataObject;
            treeItem.ID = ID;
            treeItem.Name = Name;
            treeItem.ItemType = ItemType;

            return treeItem;
        }
    }

    public class TreeItemEqualityComparer : IEqualityComparer<TreeItem>
    {
        public bool Equals(TreeItem x, TreeItem y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (ReferenceEquals(x, null)) return false;
            if (ReferenceEquals(y, null)) return false;
            if (x.GetType() != y.GetType()) return false;
            return x.ID == y.ID && x.ParentID == y.ParentID && x.ItemType == y.ItemType;
        }

        public int GetHashCode(TreeItem obj)
        {
            unchecked
            {
                var hashCode = obj.ID.GetHashCode();
                hashCode = (hashCode * 397) ^ (string.IsNullOrWhiteSpace(obj.ParentID) ? string.Empty : obj.ParentID).GetHashCode();
                hashCode = (hashCode * 397) ^ (int)obj.ItemType;
                return hashCode;
            }
        }
    }

    public class TreeItemComparer : IComparer<TreeItem>
    {
        public int Compare(TreeItem x, TreeItem y)
        {
            if (x != null && y != null)
            {
                if (x.Name.Length > y.Name.Length)
                {
                    return 1;
                }
                if (x.Name.Length < y.Name.Length)
                {
                    return -1;
                }

                for (int i = 0; i < x.Name.Length; i++)
                {
                    if (x.Name[i] > y.Name[i])
                    {
                        return 1;
                    }
                    if (x.Name[i] < y.Name[i])
                    {
                        return -1;
                    }
                }
            }

            return 0;
        }
    }
}
