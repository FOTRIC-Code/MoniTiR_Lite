// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Linq;
using System.Windows;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using Firefly.Client.Control;
using Firefly.Client.Service;

namespace Firefly.Client
{
    public delegate void SplitVideoItemSelectionChanged(DisplayItemViewModel selectedDisplayItem);

    public class SplitVideoViewModel : BaseViewModel
    {
        private Dictionary<string, DisplayItemViewModel> _cachedItemDictionary;
        private ObservableCollection<DisplayItemViewModel> _displayItems;
        private ObservableCollection<DisplayItemViewModel> _oldDisplayItems;
        private DisplayItemViewModel _selectedDisplayItem;
        private SplitModeViewModel _selectedMode;
        private SplitModeViewModel _oldMode;
        private bool _isSingleView;
        private bool _isAddPollingMode;

        private int _selectedIndex;

        public SplitVideoViewModel()
        {
            _cachedItemDictionary = new Dictionary<string, DisplayItemViewModel>();
            _displayItems = new ObservableCollection<DisplayItemViewModel>();
            _oldDisplayItems = new ObservableCollection<DisplayItemViewModel>();
            _isSingleView = false;
            SplitModeViewModelList = GetSplitModelList();

            InitMode();
        }

        #region Properties

        public bool IsAddPollingMode
        {
            get => _isAddPollingMode;
            set
            {
                _isAddPollingMode = value;
                OnPropertyChanged();
            }
        }

        internal List<SplitModeViewModel> SplitModeViewModelList { get; set; }

        public DisplayItemViewModel SelectedDisplayItem
        {
            get => _selectedDisplayItem;
            set
            {
                _selectedDisplayItem = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<DisplayItemViewModel> DisplayItems
        {
            get => _displayItems;
            set
            {
                _displayItems = value;
                OnPropertyChanged();
            }
        }

        public SplitModeViewModel SelectedMode
        {
            get => _selectedMode;
            set
            {
                if (_selectedMode != value)
                {
                    _selectedMode = value;
                }

                OnPropertyChanged();
            }
        }

        public bool IsSingleView
        { get => _isSingleView; set { _isSingleView = value; } }

        public int ItemCount { get; set; }

        #endregion Properties

        #region Public Methods

        public void AddPollingDisplayData(List<LivePreviewCruiseItem> cruiseItems)
        {
            for (int i = 0; i < _displayItems.Count; i++)
            {
                _displayItems[i].StopPreview();
                LivePreviewCruiseItem item = cruiseItems.FirstOrDefault(it => it.Order == i);
                if (item != null)
                {
                    DeviceDO deviceDO = DeviceManager.Instance.GetCached(item.DeviceID);
                    if (deviceDO != null)
                    {
                        DeviceAddressDataObject deviceAddressDO = deviceDO.DeviceAddress.FirstOrDefault(it => string.Equals(it.ChannelID.ToString(), item.ChannelID));
                        if (deviceAddressDO != null)
                        {
                            if (string.Equals(deviceAddressDO.Type, DeviceType.IRCamera.ToString(), StringComparison.InvariantCultureIgnoreCase))
                            {
                                _displayItems[i].Description = $"{deviceDO.Name}\n{I18n.Properties.Resources.IRCamera}{deviceAddressDO.ChannelID}";
                            }
                            else if (string.Equals(deviceAddressDO.Type, DeviceType.DCCamera.ToString(), StringComparison.InvariantCultureIgnoreCase))
                            {
                                _displayItems[i].Description = $"{deviceDO.Name}\n{I18n.Properties.Resources.DCCamera}{deviceAddressDO.ChannelID}";
                            }
                            _displayItems[i].DeviceDO = deviceDO;
                            _displayItems[i].ChannelID = deviceAddressDO.ChannelID;
                        }
                    }
                }
            }
            SelectedDisplayItem = null;
        }

        public void AddPollingDisplayDataByDrop(int startIndex, List<TreeItem> deviceItems)
        {
            for (int i = 0; i < deviceItems.Count; i++)
            {
                if (startIndex + i >= DisplayItems.Count)
                {
                    break;
                }
                TreeItem treeItem = deviceItems[i];

                int replaceIndex = startIndex + i;
                DeviceDO deviceDO = treeItem.Parent.DataObject as DeviceDO;
                DeviceAddressDataObject deviceAddressDO = treeItem.DataObject as DeviceAddressDataObject;

                if (_displayItems[replaceIndex].DeviceDO != null)
                {
                    _displayItems[replaceIndex].StopPreview();
                }
                if (deviceDO != null)
                {
                    if (deviceAddressDO != null)
                    {
                        if (string.Equals(deviceAddressDO.Type, DeviceType.IRCamera.ToString(), StringComparison.InvariantCultureIgnoreCase))
                        {
                            _displayItems[replaceIndex].Description = $"{deviceDO.Name}\n{I18n.Properties.Resources.IRCamera}{deviceAddressDO.ChannelID}";
                        }
                        else if (string.Equals(deviceAddressDO.Type, DeviceType.DCCamera.ToString(), StringComparison.InvariantCultureIgnoreCase))
                        {
                            _displayItems[replaceIndex].Description = $"{deviceDO.Name}\n{I18n.Properties.Resources.DCCamera}{deviceAddressDO.ChannelID}";
                        }
                        _displayItems[replaceIndex].ChannelID = deviceAddressDO.ChannelID;
                    }
                    _displayItems[replaceIndex].DeviceDO = deviceDO;
                }
            }
            SelectedDisplayItem = null;
        }

        public void ChangeDisplayDataByDrop(int startIndex, List<TreeItem> deviceItems)
        {
            for (int i = 0; i < deviceItems.Count; i++)
            {
                if (startIndex + i >= DisplayItems.Count)
                {
                    break;
                }
                TreeItem treeItem = deviceItems[i];
                ChangeDisplayItem(startIndex + i, treeItem.Parent.DataObject as DeviceDO, treeItem.DataObject as DeviceAddressDataObject);
            }

            //SavePreference();

            UpdateDisplayItemStatus();

            SelectedDisplayItem = null;
        }

        public void SwitchToFullView()
        {
            _oldMode = SelectedMode;

            _selectedIndex = DisplayItems.IndexOf(SelectedDisplayItem);
            for (int i = 0; i < DisplayItems.Count; i++)
            {
                if (_selectedIndex == i)
                {
                    continue;
                }
                DisplayItemViewModel itemViewModel = DisplayItems[i];
                itemViewModel.PausePreview();
            }

            SplitModeViewModel _singleSplitMode = new SplitModeViewModel
            {
                Mode = 1,
                Columns = 1,
                Rows = 1,
                ItemCount = 1,
                StreamType = StreamType.MainStream,
                SplitGridItems = new List<ISplitGridItem> { new DisplayItemViewModel { Column = 0, ColumnSpan = 1, Row = 0, RowSpan = 1 } }
            };

            DisplayItems.Move(_selectedIndex, 0);
            while (DisplayItems.Count > 1)
            {
                _oldDisplayItems.Add(DisplayItems[1]);
                DisplayItems.RemoveAt(1);
            }
            SelectedMode = _singleSplitMode;

            for (int i = 0; i < DisplayItems.Count; i++)
            {
                DisplayItemViewModel itemViewModel = DisplayItems[i];
                itemViewModel.ResumePreview(_selectedMode.StreamType);
            }

            IsSingleView = true;
        }

        public void SwitchToNormalView()
        {
            while (_oldDisplayItems.Count > 0)
            {
                DisplayItems.Add(_oldDisplayItems[0]);
                _oldDisplayItems.RemoveAt(0);
            }
            DisplayItems.Move(0, _selectedIndex);
            SelectedMode = _oldMode;

            for (int i = 0; i < DisplayItems.Count; i++)
            {
                DisplayItemViewModel itemViewModel = DisplayItems[i];
                itemViewModel.ResumePreview(_selectedMode.StreamType);
            }

            IsSingleView = false;
        }

        public void Release()
        {
            _cachedItemDictionary.Clear();
        }

        #endregion Public Methods

        #region Private Methods

        private void UpdateDisplayItem()
        {
            for (int i = 0; i < _displayItems.Count; i++)
            {
                DisplayItemViewModel itemViewModel = _displayItems[i];
                if (itemViewModel.IsPaused && !string.IsNullOrEmpty(itemViewModel.DeviceID))
                {
                    string key = $"{itemViewModel.DeviceID}_{itemViewModel.ChannelID}";
                    DisplayItemViewModel cachedItemViewModel;
                    if (_cachedItemDictionary.TryGetValue(key, out cachedItemViewModel))
                    {
                        itemViewModel.Init(cachedItemViewModel.DeviceDO, cachedItemViewModel.ChannelID, cachedItemViewModel.DeviceType);
                    }
                }
            }

            UpdateDisplayItemStatus();
        }

        private void UpdateDisplayItemStatus()
        {
            if (DisplayItems != null)
            {
                int count = DisplayItems.Count;
                for (int i = 0; i < count; i++)
                {
                    if (DisplayItems.Count <= i)
                    {
                        break;
                    }

                    DisplayItemViewModel itemViewModel = DisplayItems[i];
                    if (itemViewModel.DeviceDO == null)
                    {
                        itemViewModel.IsOnline = false;
                        itemViewModel.ErrorText = string.Empty;
                        continue;
                    }
                    bool isOnline = DeviceManager.Instance.IsOnline(itemViewModel.DeviceDO.ID);

                    if (itemViewModel.IsOnline != isOnline)
                    {
                        itemViewModel.IsOnline = isOnline;

                        if (isOnline)
                        {
                            itemViewModel.ErrorText = string.Empty;
                            itemViewModel.ResumePreview(_selectedMode.StreamType);
                        }
                        else
                        {
                            itemViewModel.ErrorText = I18n.Properties.Resources.ConnectFailed;
                            itemViewModel.PausePreview();
                        }
                    }
                }
            }
        }

        internal void UpdateDevice(List<DeviceDO> deviceDOList)
        {
            if (deviceDOList == null)
            {
                return;
            }

            List<string> oldIDList = _cachedItemDictionary.Select(it => it.Key).ToList();

            List<string> idsList = new List<string>();
            for (int i = 0; i < deviceDOList.Count; i++)
            {
                if (deviceDOList[i]?.DeviceAddress == null)
                {
                    continue;
                }

                for (int j = 0; j < deviceDOList[i].DeviceAddress.Count; j++)
                {
                    idsList.Add($"{deviceDOList[i].ID}_{deviceDOList[i].DeviceAddress[j].ChannelID}");
                }
            }
            List<string> exceptList = idsList.Except(oldIDList).ToList();
            if (exceptList?.Count > 0)
            {
                // 新增的监控点
                for (int i = 0; i < exceptList.Count; i++)
                {
                    // Device IDs may contain '_'; take channel id after the last separator.
                    if (!TryParseCacheKey(exceptList[i], out string deviceId, out string channelIdText))
                    {
                        continue;
                    }

                    DeviceDO deviceDO = deviceDOList.FirstOrDefault(it => string.Equals(deviceId, it.ID));
                    if (deviceDO?.DeviceAddress == null)
                    {
                        continue;
                    }

                    DeviceAddressDataObject deviceAddressDO = deviceDO.DeviceAddress.FirstOrDefault(it => string.Equals(channelIdText, $"{it.ChannelID}"));
                    VideoDisplayItemViewModel itemViewModel = new VideoDisplayItemViewModel();
                    if (deviceAddressDO != null)
                    {
                        itemViewModel.Init(deviceDO, int.Parse(channelIdText), deviceAddressDO.Type);
                        _cachedItemDictionary.Add(exceptList[i], itemViewModel);
                    }
                }
            }
            exceptList = oldIDList.Except(idsList).ToList();
            if (exceptList?.Count > 0)
            {
                // 需要移除的监控点
                for (int i = 0; i < exceptList.Count; i++)
                {
                    DisplayItemViewModel itemViewModel;
                    if (_cachedItemDictionary.TryGetValue(exceptList[i], out itemViewModel))
                    {
                        itemViewModel.StopPreview();
                        _cachedItemDictionary.Remove(exceptList[i]);
                    }
                }
            }
            UpdateDisplayItem();
        }

        private static bool TryParseCacheKey(string key, out string deviceId, out string channelIdText)
        {
            deviceId = null;
            channelIdText = null;
            if (string.IsNullOrEmpty(key))
            {
                return false;
            }

            int separator = key.LastIndexOf('_');
            if (separator <= 0 || separator >= key.Length - 1)
            {
                return false;
            }

            deviceId = key.Substring(0, separator);
            channelIdText = key.Substring(separator + 1);
            return true;
        }

        private void ChangeDisplayItem(int replaceIndex, DeviceDO deviceDO, DeviceAddressDataObject deviceAddressDO)
        {
            if (_displayItems[replaceIndex].DeviceDO != null)
            {
                _displayItems[replaceIndex].StopPreview();
            }
            if (deviceDO != null)
            {
                if (deviceAddressDO != null)
                {
                    if (string.Equals(deviceAddressDO.Type, DeviceType.IRCamera.ToString(), StringComparison.InvariantCultureIgnoreCase))
                    {
                        VideoDisplayItemViewModel videoDisplayItemViewModel = new VideoDisplayItemViewModel();
                        videoDisplayItemViewModel.Init(deviceDO, deviceAddressDO.ChannelID, DeviceType.IRCamera.ToString());
                        videoDisplayItemViewModel.CopyFromISplitGridItem(_displayItems[replaceIndex]);
                        _displayItems.RemoveAt(replaceIndex);
                        _displayItems.Insert(replaceIndex, videoDisplayItemViewModel);
                    }
                    else if (string.Equals(deviceAddressDO.Type, DeviceType.DCCamera.ToString(), StringComparison.InvariantCultureIgnoreCase))
                    {
                        VideoDisplayItemViewModel videoDisplayItemViewModel = new VideoDisplayItemViewModel();
                        videoDisplayItemViewModel.Init(deviceDO, deviceAddressDO.ChannelID, DeviceType.DCCamera.ToString());
                        videoDisplayItemViewModel.CopyFromISplitGridItem(_displayItems[replaceIndex]);
                        _displayItems.RemoveAt(replaceIndex);
                        _displayItems.Insert(replaceIndex, videoDisplayItemViewModel);
                    }

                    _displayItems[replaceIndex].ChannelID = deviceAddressDO.ChannelID;
                }
            }
            _displayItems[replaceIndex].DeviceDO = deviceDO;
        }

        internal void LoadPreference()
        {
            //int selectedModel = LivePreviewPreference.Intance.SelectedMode ?? 2;

            //SelectedMode = SplitModeViewModelList.FirstOrDefault(it => it.Mode == selectedModel);
            //UpdateView();
            if (LivePreviewPreference.Intance.DisplayItems?.Count > 0)
            {
                for (int i = 0; i < _selectedMode.ItemCount; i++)
                {
                    ISplitGridItem splitGridItem = SelectedMode.SplitGridItems[i];
                    LivePreviewDisplayItem cachedDisplayItem = LivePreviewPreference.Intance.DisplayItems.FirstOrDefault(it => it.Index == i);
                    if (cachedDisplayItem != null)
                    {
                        VideoDisplayItemViewModel itemViewModel = new VideoDisplayItemViewModel();
                        itemViewModel.CopyFromISplitGridItem(splitGridItem);
                        itemViewModel.CopyFromLivePreviewDisplayItem(cachedDisplayItem);
                        if (_displayItems.Count > i)
                        {
                            _displayItems.RemoveAt(i);
                            _displayItems.Insert(i, itemViewModel);
                        }
                        else
                        {
                            _displayItems.Add(itemViewModel);
                        }
                    }
                    else
                    {
                        DisplayItemViewModel itemViewModel = new DisplayItemViewModel();
                        itemViewModel.CopyFromISplitGridItem(splitGridItem);
                        if (_displayItems.Count > i)
                        {
                            _displayItems.RemoveAt(i);
                            _displayItems.Insert(i, itemViewModel);
                        }
                        else
                        {
                            _displayItems.Add(itemViewModel);
                        }
                    }
                }
            }
        }

        private void InitMode()
        {
            foreach (SplitModeViewModel modeItem in SplitModeViewModelList)
            {
                modeItem.SplitGridItems = new List<ISplitGridItem>();
                switch (modeItem.Mode)
                {
                    case 1:
                    case 2:
                    case 3:
                        for (int i = 0; i < modeItem.ItemCount; i++)
                        {
                            modeItem.SplitGridItems.Add(new DisplayItemViewModel { Column = i % 2, ColumnSpan = 1, Row = i / 2, RowSpan = 1 });
                        }

                        break;

                    case 4:
                        modeItem.SplitGridItems.Add(new DisplayItemViewModel { Column = 0, ColumnSpan = 2, Row = 0, RowSpan = 2 });
                        modeItem.SplitGridItems.Add(new DisplayItemViewModel { Column = 2, ColumnSpan = 1, Row = 0, RowSpan = 1 });
                        modeItem.SplitGridItems.Add(new DisplayItemViewModel { Column = 2, ColumnSpan = 1, Row = 1, RowSpan = 1 });
                        modeItem.SplitGridItems.Add(new DisplayItemViewModel { Column = 0, ColumnSpan = 1, Row = 2, RowSpan = 1 });
                        modeItem.SplitGridItems.Add(new DisplayItemViewModel { Column = 1, ColumnSpan = 1, Row = 2, RowSpan = 1 });
                        modeItem.SplitGridItems.Add(new DisplayItemViewModel { Column = 2, ColumnSpan = 1, Row = 2, RowSpan = 1 });
                        break;

                    case 5:

                        for (int i = 0; i < modeItem.ItemCount; i++)
                        {
                            modeItem.SplitGridItems.Add(new DisplayItemViewModel { Column = i % 3, ColumnSpan = 1, Row = i / 3, RowSpan = 1 });
                        }

                        break;

                    case 6:
                        modeItem.SplitGridItems.Add(new DisplayItemViewModel { Column = 0, ColumnSpan = 3, Row = 0, RowSpan = 3 });
                        modeItem.SplitGridItems.Add(new DisplayItemViewModel { Column = 3, ColumnSpan = 1, Row = 0, RowSpan = 1 });
                        modeItem.SplitGridItems.Add(new DisplayItemViewModel { Column = 3, ColumnSpan = 1, Row = 1, RowSpan = 1 });
                        modeItem.SplitGridItems.Add(new DisplayItemViewModel { Column = 3, ColumnSpan = 1, Row = 2, RowSpan = 1 });
                        modeItem.SplitGridItems.Add(new DisplayItemViewModel { Column = 0, ColumnSpan = 1, Row = 3, RowSpan = 1 });
                        modeItem.SplitGridItems.Add(new DisplayItemViewModel { Column = 1, ColumnSpan = 1, Row = 3, RowSpan = 1 });
                        modeItem.SplitGridItems.Add(new DisplayItemViewModel { Column = 2, ColumnSpan = 1, Row = 3, RowSpan = 1 });
                        modeItem.SplitGridItems.Add(new DisplayItemViewModel { Column = 3, ColumnSpan = 1, Row = 3, RowSpan = 1 });
                        break;

                    case 7:
                        for (int i = 0; i < modeItem.ItemCount; i++)
                        {
                            modeItem.SplitGridItems.Add(new DisplayItemViewModel { Column = i % 3, ColumnSpan = 1, Row = i / 3, RowSpan = 1 });
                        }

                        break;

                    case 8:
                        modeItem.SplitGridItems.Add(new DisplayItemViewModel { Column = 0, ColumnSpan = 1, Row = 0, RowSpan = 1 });
                        modeItem.SplitGridItems.Add(new DisplayItemViewModel { Column = 1, ColumnSpan = 1, Row = 0, RowSpan = 1 });
                        modeItem.SplitGridItems.Add(new DisplayItemViewModel { Column = 2, ColumnSpan = 1, Row = 0, RowSpan = 1 });
                        modeItem.SplitGridItems.Add(new DisplayItemViewModel { Column = 3, ColumnSpan = 1, Row = 0, RowSpan = 1 });
                        modeItem.SplitGridItems.Add(new DisplayItemViewModel { Column = 0, ColumnSpan = 1, Row = 1, RowSpan = 1 });
                        modeItem.SplitGridItems.Add(new DisplayItemViewModel { Column = 1, ColumnSpan = 2, Row = 1, RowSpan = 2 });
                        modeItem.SplitGridItems.Add(new DisplayItemViewModel { Column = 3, ColumnSpan = 1, Row = 1, RowSpan = 1 });
                        modeItem.SplitGridItems.Add(new DisplayItemViewModel { Column = 0, ColumnSpan = 1, Row = 2, RowSpan = 1 });
                        modeItem.SplitGridItems.Add(new DisplayItemViewModel { Column = 3, ColumnSpan = 1, Row = 2, RowSpan = 1 });
                        modeItem.SplitGridItems.Add(new DisplayItemViewModel { Column = 0, ColumnSpan = 1, Row = 3, RowSpan = 1 });
                        modeItem.SplitGridItems.Add(new DisplayItemViewModel { Column = 1, ColumnSpan = 1, Row = 3, RowSpan = 1 });
                        modeItem.SplitGridItems.Add(new DisplayItemViewModel { Column = 2, ColumnSpan = 1, Row = 3, RowSpan = 1 });
                        modeItem.SplitGridItems.Add(new DisplayItemViewModel { Column = 3, ColumnSpan = 1, Row = 3, RowSpan = 1 });
                        break;

                    case 9:
                        modeItem.SplitGridItems.Add(new DisplayItemViewModel { Column = 0, ColumnSpan = 1, Row = 0, RowSpan = 1 });
                        modeItem.SplitGridItems.Add(new DisplayItemViewModel { Column = 1, ColumnSpan = 1, Row = 0, RowSpan = 1 });
                        modeItem.SplitGridItems.Add(new DisplayItemViewModel { Column = 2, ColumnSpan = 1, Row = 0, RowSpan = 1 });
                        modeItem.SplitGridItems.Add(new DisplayItemViewModel { Column = 3, ColumnSpan = 1, Row = 0, RowSpan = 1 });
                        modeItem.SplitGridItems.Add(new DisplayItemViewModel { Column = 0, ColumnSpan = 1, Row = 1, RowSpan = 1 });
                        modeItem.SplitGridItems.Add(new DisplayItemViewModel { Column = 1, ColumnSpan = 1, Row = 1, RowSpan = 1 });
                        modeItem.SplitGridItems.Add(new DisplayItemViewModel { Column = 2, ColumnSpan = 1, Row = 1, RowSpan = 1 });

                        modeItem.SplitGridItems.Add(new DisplayItemViewModel { Column = 0, ColumnSpan = 1, Row = 2, RowSpan = 1 });
                        modeItem.SplitGridItems.Add(new DisplayItemViewModel { Column = 1, ColumnSpan = 1, Row = 2, RowSpan = 1 });
                        modeItem.SplitGridItems.Add(new DisplayItemViewModel { Column = 2, ColumnSpan = 1, Row = 2, RowSpan = 1 });
                        modeItem.SplitGridItems.Add(new DisplayItemViewModel { Column = 3, ColumnSpan = 1, Row = 2, RowSpan = 1 });
                        modeItem.SplitGridItems.Add(new DisplayItemViewModel { Column = 0, ColumnSpan = 1, Row = 3, RowSpan = 1 });
                        modeItem.SplitGridItems.Add(new DisplayItemViewModel { Column = 1, ColumnSpan = 1, Row = 3, RowSpan = 1 });
                        modeItem.SplitGridItems.Add(new DisplayItemViewModel { Column = 2, ColumnSpan = 1, Row = 3, RowSpan = 1 });
                        break;

                    case 10:
                        for (int i = 0; i < modeItem.ItemCount; i++)
                        {
                            modeItem.SplitGridItems.Add(new DisplayItemViewModel { Column = i % 4, ColumnSpan = 1, Row = i / 4, RowSpan = 1 });
                        }

                        break;
                    //case 11:
                    //    modeItem.SplitGridItems.Add(new DisplayItemViewModel { Row = 0, ColumnSpan = 1, Column = 0, RowSpan = 1 });
                    //    modeItem.SplitGridItems.Add(new DisplayItemViewModel { Row = 1, ColumnSpan = 1, Column = 0, RowSpan = 1 });
                    //    modeItem.SplitGridItems.Add(new DisplayItemViewModel { Row = 2, ColumnSpan = 1, Column = 0, RowSpan = 1 });
                    //    modeItem.SplitGridItems.Add(new DisplayItemViewModel { Row = 3, ColumnSpan = 1, Column = 0, RowSpan = 1 });
                    //    modeItem.SplitGridItems.Add(new DisplayItemViewModel { Row = 4, ColumnSpan = 1, Column = 0, RowSpan = 1 });
                    //    modeItem.SplitGridItems.Add(new DisplayItemViewModel { Row = 5, ColumnSpan = 1, Column = 0, RowSpan = 1 });
                    //    modeItem.SplitGridItems.Add(new DisplayItemViewModel { Row = 6, ColumnSpan = 1, Column = 0, RowSpan = 1 });

                    //    modeItem.SplitGridItems.Add(new DisplayItemViewModel { Row = 0, ColumnSpan = 1, Column = 1, RowSpan = 1 });
                    //    modeItem.SplitGridItems.Add(new DisplayItemViewModel { Row = 1, ColumnSpan = 1, Column = 1, RowSpan = 1 });
                    //    modeItem.SplitGridItems.Add(new DisplayItemViewModel { Row = 2, ColumnSpan = 1, Column = 1, RowSpan = 1 });
                    //    modeItem.SplitGridItems.Add(new DisplayItemViewModel { Row = 3, ColumnSpan = 1, Column = 1, RowSpan = 1 });
                    //    modeItem.SplitGridItems.Add(new DisplayItemViewModel { Row = 4, ColumnSpan = 1, Column = 1, RowSpan = 1 });
                    //    modeItem.SplitGridItems.Add(new DisplayItemViewModel { Row = 5, ColumnSpan = 1, Column = 1, RowSpan = 1 });
                    //    modeItem.SplitGridItems.Add(new DisplayItemViewModel { Row = 6, ColumnSpan = 1, Column = 1, RowSpan = 1 });

                    //    modeItem.SplitGridItems.Add(new DisplayItemViewModel { Row = 0, ColumnSpan = 1, Column = 2, RowSpan = 1 });
                    //    modeItem.SplitGridItems.Add(new DisplayItemViewModel { Row = 1, ColumnSpan = 1, Column = 2, RowSpan = 1 });
                    //    modeItem.SplitGridItems.Add(new DisplayItemViewModel { Row = 2, ColumnSpan = 1, Column = 2, RowSpan = 1 });
                    //    modeItem.SplitGridItems.Add(new DisplayItemViewModel { Row = 3, ColumnSpan = 1, Column = 2, RowSpan = 1 });
                    //    modeItem.SplitGridItems.Add(new DisplayItemViewModel { Row = 4, ColumnSpan = 1, Column = 2, RowSpan = 1 });
                    //    modeItem.SplitGridItems.Add(new DisplayItemViewModel { Row = 5, ColumnSpan = 1, Column = 2, RowSpan = 1 });
                    //    modeItem.SplitGridItems.Add(new DisplayItemViewModel { Row = 6, ColumnSpan = 1, Column = 2, RowSpan = 1 });
                    //    break;
                    case 12:
                        for (int i = 0; i < modeItem.ItemCount; i++)
                        {
                            modeItem.SplitGridItems.Add(new DisplayItemViewModel { Column = i % 5, ColumnSpan = 1, Row = i / 5, RowSpan = 1 });
                        }

                        break;

                    case 13:
                        for (int i = 0; i < modeItem.ItemCount; i++)
                        {
                            modeItem.SplitGridItems.Add(new DisplayItemViewModel { Column = i % 6, ColumnSpan = 1, Row = i / 6, RowSpan = 1 });
                        }

                        break;

                    case 14:
                        for (int i = 0; i < modeItem.ItemCount; i++)
                        {
                            modeItem.SplitGridItems.Add(new DisplayItemViewModel { Column = i % 6, ColumnSpan = 1, Row = i / 6, RowSpan = 1 });
                        }

                        break;

                    case 15:
                        for (int i = 0; i < modeItem.ItemCount; i++)
                        {
                            modeItem.SplitGridItems.Add(new DisplayItemViewModel { Column = i % 8, ColumnSpan = 1, Row = i / 8, RowSpan = 1 });
                        }

                        break;
                }
            }
        }

        internal void UpdateView()
        {
            if (_displayItems.Count > _selectedMode.ItemCount)
            {
                while (_displayItems.Count > _selectedMode.ItemCount)
                {
                    _displayItems[_displayItems.Count - 1].StopPreview();
                    _displayItems.RemoveAt(_displayItems.Count - 1);
                }
            }
            for (int i = 0; i < _selectedMode.ItemCount; i++)
            {
                ISplitGridItem splitGridItem = _selectedMode.SplitGridItems[i];

                if (_displayItems.Count > i)
                {
                    _displayItems[i].CopyFromISplitGridItem(_selectedMode.SplitGridItems[i]);
                }
                else
                {
                    DisplayItemViewModel itemViewModel = new DisplayItemViewModel();
                    itemViewModel.CopyFromISplitGridItem(splitGridItem);
                    _displayItems.Add(itemViewModel);
                }
            }
        }

        internal void ReplaceItems(List<TreeItem> itemList)
        {
            int count = DisplayItems.Count;
            for (int i = 0; i < count; i++)
            {
                if (itemList.Count <= i)
                {
                    StopPlay(i);
                    continue;
                }
                if (itemList[i] == null)
                {
                    StopPlay(i);
                    continue;
                }

                if (string.Equals(DisplayItems[i].DeviceID, itemList[i].ParentID) && string.Equals($"{DisplayItems[i].ChannelID}", itemList[i].ID))
                {
                    continue;
                }
                ChangeDisplayItem(i, itemList[i].Parent.DataObject as DeviceDO, itemList[i].DataObject as DeviceAddressDataObject);
            }

            UpdateDisplayItemStatus();
        }

        internal void StopPlay()
        {
            for (int i = 0; i < DisplayItems.Count; i++)
            {
                StopPlay(i);
            }
        }

        internal void StopPlay(int index)
        {
            DisplayItems[index].StopPreview();

            DisplayItemViewModel itemViewModel = new DisplayItemViewModel();
            itemViewModel.CopyFromISplitGridItem(DisplayItems[index]);
            _displayItems.RemoveAt(index);
            _displayItems.Insert(index, itemViewModel);
        }

        #endregion Private Methods

        public static List<SplitModeViewModel> GetSplitModelList()
        {
            return new List<SplitModeViewModel>
            {
                new SplitModeViewModel { Mode = 1, Columns = 1, Rows = 1, ItemCount = 1, StreamType = StreamType.MainStream },
                new SplitModeViewModel { Mode = 2, Columns = 2, Rows = 1, ItemCount = 2, StreamType = StreamType.MainStream },
                new SplitModeViewModel { Mode = 3, Columns = 2, Rows = 2, ItemCount = 4, StreamType = StreamType.MainStream },
                new SplitModeViewModel { Mode = 4, Columns = 3, Rows = 3, ItemCount = 6, StreamType = StreamType.MainStream },
                new SplitModeViewModel { Mode = 5, Columns = 3, Rows = 3, ItemCount = 7, StreamType = StreamType.MainStream },
                new SplitModeViewModel { Mode = 6, Columns = 4, Rows = 4, ItemCount = 8, StreamType = StreamType.MainStream },
                new SplitModeViewModel { Mode = 7, Columns = 3, Rows = 3, ItemCount = 9, StreamType = StreamType.MainStream },
                new SplitModeViewModel { Mode = 8, Columns = 4, Rows = 4, ItemCount = 13, StreamType = StreamType.MainStream },
                new SplitModeViewModel { Mode = 9, Columns = 4, Rows = 4, ItemCount = 14, StreamType = StreamType.MainStream },
                new SplitModeViewModel { Mode = 10, Columns = 4, Rows = 4, ItemCount = 16, StreamType = StreamType.MainStream },
                //new SplitModeViewModel { Mode = 11, Columns = 3, Rows = 7, ItemCount = 21, StreamType = StreamType.SubStream },
                new SplitModeViewModel { Mode = 12, Columns = 5, Rows = 5, ItemCount = 25, StreamType = StreamType.SubStream },
                new SplitModeViewModel { Mode = 13, Columns = 6, Rows = 6, ItemCount = 36, StreamType = StreamType.SubStream },
                new SplitModeViewModel { Mode = 14, Columns = 6, Rows = 7, ItemCount = 42, StreamType = StreamType.SubStream },
                new SplitModeViewModel { Mode = 15, Columns = 8, Rows = 8, ItemCount = 64, StreamType = StreamType.SubStream }
            };
        }
    }

    public class DisplayItemViewModel : BaseViewModel, ISplitGridItem
    {
        #region Fields

        private bool _isAddPollingMode;

        private string _address;

        private string _rtspUrl;
        private int _column;
        private int _columnSpan;
        private int _row;
        private int _rowSpan;
        private bool _connectStatus;
        private bool _isOnline;
        private bool _hasDevice;
        private bool _isHiddenBar;
        private bool _isLockingBar;
        private string _description;
        private DeviceDO _deviceDO;
        private Size _deviceArea;
        private string _errorText;
        private bool _recordStatus;
        private string _deviceType;
        private bool _isPaused = true;
        private bool _enableDrawMask;

        #endregion Fields

        public DisplayItemViewModel()
        {
            _isHiddenBar = true;
        }

        #region Properties

        public bool IsAddPollingMode
        {
            get => _isAddPollingMode;
            set
            {
                _isAddPollingMode = value;
                OnPropertyChanged();
            }
        }

        public string Address
        {
            get => _address;
            set
            {
                _address = value;
                OnPropertyChanged();
            }
        }

        public string ErrorText
        {
            get => _errorText;
            set
            {
                _errorText = value;
                OnPropertyChanged();
            }
        }

        public int ChannelID { get; set; }

        public string DeviceID { get; set; }

        public StreamType StreamType { get; set; }

        public string RTSPUrl
        {
            get => _rtspUrl;
            set
            {
                _rtspUrl = value;
                OnPropertyChanged();
            }
        }

        public Size DeviceArea
        {
            get => _deviceArea;
            set
            {
                _deviceArea = value;
                OnPropertyChanged();
            }
        }

        public string DeviceType
        {
            get => _deviceType;
            set
            {
                _deviceType = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsShowMask));
            }
        }

        public int Row
        {
            get => _row;
            set
            {
                _row = value;
                OnPropertyChanged();
            }
        }

        public int RowSpan
        {
            get => _rowSpan;
            set
            {
                _rowSpan = value;
                OnPropertyChanged();
            }
        }

        public int Column
        {
            get => _column;
            set
            {
                _column = value;
                OnPropertyChanged();
            }
        }

        public int ColumnSpan
        {
            get => _columnSpan;
            set
            {
                _columnSpan = value;
                OnPropertyChanged();
            }
        }

        public bool HasDevice
        {
            get => _hasDevice;
            set
            {
                _hasDevice = value;
                OnPropertyChanged();
            }
        }

        public bool IsHiddenBar
        {
            get => _isHiddenBar;
            set
            {
                _isHiddenBar = value;
                OnPropertyChanged();
            }
        }

        public DeviceDO DeviceDO
        {
            get => _deviceDO;
            set
            {
                _deviceDO = value;
                HasDevice = _deviceDO != null;
                DeviceID = _deviceDO?.ID ?? string.Empty;
            }
        }

        public bool ConnectStatus
        {
            get => _connectStatus;
            set
            {
                _connectStatus = value;
                OnPropertyChanged();
            }
        }

        public bool RecordStatus
        {
            get => _recordStatus;
            set
            {
                _recordStatus = value;
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

        public string Description
        {
            get => _description;
            set
            {
                _description = value;
                OnPropertyChanged();
            }
        }

        public bool EnableDrawMask
        {
            get => _enableDrawMask;
            set
            {
                _enableDrawMask = value;
                OnPropertyChanged();
            }
        }

        public bool LockingBar
        {
            get => _isLockingBar;
            set
            {
                _isLockingBar = value;
                OnPropertyChanged();
                if (value)
                {
                    IsHiddenBar = false;
                }
            }
        }

        public bool IsShowMask => DeviceType == Service.DeviceType.IRCamera.ToString();

        public bool IsPaused
        {
            get => _isPaused;
            set
            {
                _isPaused = value;
                OnPropertyChanged();
            }
        }

        #endregion Properties

        #region Methods

        internal virtual void Init(DeviceDO deviceDO, int channelID, string deviceType)
        {
            DeviceDO = deviceDO;
            ChannelID = channelID;
            DeviceType = deviceType;
        }

        public void StopPreview()
        {
            DeviceDO = null;
            RTSPUrl = string.Empty;
            DeviceType = string.Empty;
            Description = string.Empty;
            IsOnline = false;
            StreamType = StreamType.Unknown;
            IsPaused = true;
        }

        public void PausePreview()
        {
            IsPaused = true;
        }

        public async void ResumePreview(StreamType streamType)
        {
            if (DeviceDO != null)
            {
                StreamType = streamType;
                await InitRTSPUrl();

                if (RTSPUrl != string.Empty)
                {
                    IsPaused = false;
                }
            }
        }

        private async Task InitRTSPUrl()
        {
            if (!IsOnline || DeviceDO == null)
            {
                return;
            }

            DeviceStreamDO streamDO = await DeviceManager.Instance.GetStream(DeviceDO, StreamType);

            DeviceStreamItemDO deviceStreamItemDO = streamDO?.StreamItems?.FirstOrDefault(it => it.ChannelID == ChannelID);

            if (deviceStreamItemDO != null)
            {
                DeviceParamDataObject deviceParamDO = await DeviceManager.Instance.GetDeviceParam(_deviceDO);
                int width = 384, height = 288;
                if (deviceParamDO?.DeviceModel?.Count > ChannelID)
                {
                    DeviceModelDataObject deviceModelDO = deviceParamDO.DeviceModel[ChannelID];
                    if (deviceModelDO != null && deviceModelDO.Width != 0)
                    {
                        width = deviceModelDO.Width;
                    }
                    if (deviceModelDO != null && deviceModelDO.Height != 0)
                    {
                        height = deviceModelDO.Height;
                    }
                }
                DeviceArea = new Size(width, height);
                RTSPUrl = deviceStreamItemDO.RTSPUrl;
            }
        }

        #endregion Methods

        #region Methods

        internal void CopyFromISplitGridItem(ISplitGridItem splitGridItem)
        {
            Row = splitGridItem.Row;
            RowSpan = splitGridItem.RowSpan;
            Column = splitGridItem.Column;
            ColumnSpan = splitGridItem.ColumnSpan;
        }

        internal void CopyFromLivePreviewDisplayItem(LivePreviewDisplayItem item)
        {
            DeviceID = item.DeviceID;
            ChannelID = item.ChannelID;
            DeviceType = item.DeviceType;
        }

        #endregion Methods
    }

    public class VideoDisplayItemViewModel : DisplayItemViewModel
    {
        internal override void Init(DeviceDO deviceDO, int channelID, string deviceType)
        {
            base.Init(deviceDO, channelID, deviceType);
            if (string.Equals(deviceType, Service.DeviceType.IRCamera.ToString(), StringComparison.InvariantCultureIgnoreCase))
            {
                Description = $"{deviceDO.Name}/{I18n.Properties.Resources.IRCamera}{ChannelID}";
            }
            else if (string.Equals(deviceType, Service.DeviceType.DCCamera.ToString(), StringComparison.InvariantCultureIgnoreCase))
            {
                Description = $"{deviceDO.Name}/{I18n.Properties.Resources.DCCamera}{ChannelID}";
            }
        }
    }

    public class SplitModeViewModel : BaseViewModel
    {
        #region Fields

        private int _mode;
        private int _columns;
        private int _rows;
        private int _itemCount;
        private string _desc;

        #endregion Fields

        #region Properties

        public int Mode
        {
            get => _mode;
            set
            {
                _mode = value;
                OnPropertyChanged();
            }
        }

        public int Columns
        {
            get => _columns;
            set
            {
                _columns = value;
                OnPropertyChanged();
            }
        }

        public int Rows
        {
            get => _rows;
            set
            {
                _rows = value;
                OnPropertyChanged();
            }
        }

        public int ItemCount
        {
            get => _itemCount;
            set
            {
                _itemCount = value;
                OnPropertyChanged();
            }
        }

        public string Desc
        {
            get => _desc;
            set
            {
                _desc = value;
                OnPropertyChanged();
            }
        }

        public StreamType StreamType { get; set; }

        public List<ISplitGridItem> SplitGridItems { get; set; }

        #endregion Properties
    }
}