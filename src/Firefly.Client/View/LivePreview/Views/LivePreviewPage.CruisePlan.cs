
using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Collections.Generic;

using Firefly.Client.Control;
using Firefly.Client.Service;

namespace Firefly.Client
{
    public partial class LivePreviewPage
    {
        private async void btnRunCruise_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            if (button?.DataContext is RealTimeCruiseItemViewModel itemViewModel)
            {
                if(ViewModel.SelectedCruise != itemViewModel)
                {
                    if (ViewModel.SelectedCruise != null)
                    {
                        ViewModel.SelectedCruise.CruiseStatus = false;
                    }
                }

                ViewModel.SelectedCruise = itemViewModel;

                try
                {
                    if (ViewModel.SelectedDisplayItem == null)
                    {
                        return;
                    }

                    DeviceDO deviceDO = ViewModel.SelectedDisplayItem.DeviceDO;
                    if (deviceDO == null)
                    {
                        return;
                    }

                    Mouse.SetCursor(Cursors.Wait);

                    if (!itemViewModel.CruiseStatus)
                    {
                        ApiResult<bool> apiResult = await DeviceManager.Instance.SetPTZCruise(deviceDO,
                            new PTZCruiseDataObject()
                            {
                                Command = PTZCruiseCommand.RUN,
                                CruiseID = itemViewModel.ID
                            });
                        if (apiResult.Code == ResponseCode.EQUIPMENT_DEVICE_INVALID_PTZ || apiResult.Code == ResponseCode.FIRE_DEVICE_INVALID_PTZ)
                        {
                            IRtekMessageBox.ShowDialog(Window.GetWindow(this), I18n.Properties.Resources.InvalidPTZ, I18n.Properties.Resources.Tip, Control.MessageBoxButton.OK);
                        }
                        itemViewModel.CruiseStatus = true;
                        ViewModel.CurrentCruiseId = itemViewModel.ID;
                        ViewModel.CurrentCruiseDeviceID = ViewModel.SelectedDisplayItem.DeviceID;
                    }
                    else
                    {
                        ApiResult<bool> apiResult = await DeviceManager.Instance.SetPTZCruise(deviceDO,
                            new PTZCruiseDataObject()
                            {
                                Command = PTZCruiseCommand.STOP,
                                CruiseID = itemViewModel.ID
                            });
                        if (apiResult.Code == ResponseCode.EQUIPMENT_DEVICE_INVALID_PTZ || apiResult.Code == ResponseCode.FIRE_DEVICE_INVALID_PTZ)
                        {
                            IRtekMessageBox.ShowDialog(Window.GetWindow(this), I18n.Properties.Resources.InvalidPTZ, I18n.Properties.Resources.Tip, Control.MessageBoxButton.OK);
                        }
                        itemViewModel.CruiseStatus = false;
                        ViewModel.CurrentCruiseId = 0;
                        ViewModel.CurrentCruiseDeviceID = string.Empty;
                    }

                }
                finally
                {
                    Mouse.SetCursor(Cursors.Arrow);
                }
            }
        }

        private void btnPrevPollingGroup_Click(object sender, RoutedEventArgs e)
        {
            if (!ViewModel.IsPollingStarted)
            {
                IRtekMessageBox.ShowDialog(Window.GetWindow(this), I18n.Properties.Resources.NotStartedPolling, I18n.Properties.Resources.Tip, Control.MessageBoxButton.None);
                return;
            }

            _pollingTimer.Stop();
            _pollingTimer.Start();
            List<TreeItem> result = ViewModel.PrevPollingGroup();
            if (result != null)
            {
                _splitVideoControl.ChangeDisplayDataByPolling(result);
            }
        }

        private void StopPolling()
        {
            _pollingTimer.Stop();
            ViewModel.IsPollingStarted = false;
            _pollingTimer = null;
        }

        private async void btnPolling_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Amur.Controls.Button button && button.DataContext is PollingGroupViewModel pollingGroupViewModel)
            {
                if (!pollingGroupViewModel.IsRunning && ViewModel.IsPollingStarted)
                {
                    IRtekMessageBox.ShowDialog(Window.GetWindow(this), I18n.Properties.Resources.IsPollingStarted, I18n.Properties.Resources.Tip, Control.MessageBoxButton.None);
                    return;
                }
                if (_pollingTimer != null)
                {
                    pollingGroupViewModel.IsRunning = false;
                    StopPolling();
                    return;
                }

                if (!pollingGroupViewModel.IsDefault)
                {
                    var selectedMode = ViewModel.SplitModeList.FirstOrDefault(it => it.Item1 == pollingGroupViewModel.SelectedSplitMode.Mode);
                    if (selectedMode != null)
                    {
                        lbSplitList.SelectedItem = selectedMode;
                    }
                }

                pollingGroupViewModel.IsRunning = true;
                ViewModel.IsPollingStarted = true;

                // 这里搞延迟，是为了lbSplitList.SelectedItem = selectedMode;切换分屏模式时，切换的顺畅
                await Task.Delay(500);
                int time = pollingGroupViewModel.Second * 1000;

                if (_pollingTimer == null)
                {
                    _pollingTimer = new DispatcherTimer();
                    _pollingTimer.Interval = TimeSpan.FromMilliseconds(time);
                    _pollingTimer.Tick += PollingTimer_Tick;
                }
                ViewModel.InitPollingGroup(pollingGroupViewModel);
                List<TreeItem> result = ViewModel.NextPollingGroup();
                if (result != null)
                {
                    _splitVideoControl.ChangeDisplayDataByPolling(result);
                }
                _pollingTimer.Start();
            }
        }

        private void PollingTimer_Tick(object sender, EventArgs e)
        {
            List<TreeItem> result = ViewModel.NextPollingGroup();
            if (result != null)
            {
                _splitVideoControl.ChangeDisplayDataByPolling(result);
            }
        }

        private void btnNextPollingGroup_Click(object sender, RoutedEventArgs e)
        {
            if (!ViewModel.IsPollingStarted)
            {
                IRtekMessageBox.ShowDialog(Window.GetWindow(this), I18n.Properties.Resources.NotStartedPolling, I18n.Properties.Resources.Tip, Control.MessageBoxButton.None);
                return;
            }
            _pollingTimer.Stop();
            _pollingTimer.Start();
            List<TreeItem> result = ViewModel.NextPollingGroup();
            if (result != null)
            {
                _splitVideoControl.ChangeDisplayDataByPolling(result);
            }
        }

        private void btnCancelAddPolling_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.IsAddCruisePlan = false;

            ViewModel.CruisePageList.Clear();
            ViewModel.CruiseItemCacheDict.Clear();

            _splitVideoControl.CloseAddPollingMode();
            _latestSelectedCruisePage = 1;
        }

        private void btnBeginAddPolling_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.CruisePageList.Add(1);
            ViewModel.CruiseItemCacheDict.Add(1, new List<LivePreviewCruiseItem>());

            ViewModel.IsAddCruisePlan = true;
            _splitVideoControl.OpenAddPollingMode();
        }

        private async void btnSavePolling_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.CruiseItemCacheDict[_latestSelectedCruisePage] = _splitVideoControl.GetDisplayCruiseItems();

            PollingGroupViewModel clonedPollingGroupVM = null;
            if (_currentpollingGroupViewModel != null)
            {
                clonedPollingGroupVM = _currentpollingGroupViewModel.DeepClone();
            }

            PollingGroupWindow window = new PollingGroupWindow(clonedPollingGroupVM);
            window.ViewModel.SelectedSplitMode = ViewModel.SelectedSplitModeVM;
            window.Owner = Window.GetWindow(this);
            if (window.ShowDialog() == true)
            {
                var groupInfo = window.ViewModel.ToLivePreviewPollingGroupInfo();
                List<LivePreviewCruiseItem> groupItems = new List<LivePreviewCruiseItem>();
                foreach (var keyValuePair in ViewModel.CruiseItemCacheDict)
                {
                    foreach (var groupItem in keyValuePair.Value)
                    {
                        groupItem.Order = groupItem.Order + (keyValuePair.Key - 1) * ViewModel.SelectedSplitModeVM.ItemCount;
                        groupItems.Add(groupItem);
                    }
                }
                groupInfo.GroupItems = groupItems;

                LivePreviewCruisePlanConfigDO pollingGroupConfigDO = await SystemConfigManager.Instance.GetLivePreviewPollingGroup();
                if (pollingGroupConfigDO == null)
                {
                    pollingGroupConfigDO = new LivePreviewCruisePlanConfigDO();
                    pollingGroupConfigDO.LivePreviewPollingGroupInfos = new List<LivePreviewCruisePlan>();
                }
                else if (pollingGroupConfigDO.LivePreviewPollingGroupInfos == null)
                {
                    pollingGroupConfigDO.LivePreviewPollingGroupInfos = new List<LivePreviewCruisePlan>();
                }
                var oldItem = pollingGroupConfigDO.LivePreviewPollingGroupInfos.FirstOrDefault(it => string.Equals(it.Name, groupInfo.Name));
                if (oldItem != null)
                {
                    pollingGroupConfigDO.LivePreviewPollingGroupInfos.Remove(oldItem);
                }
                pollingGroupConfigDO.LivePreviewPollingGroupInfos.Add(groupInfo);
                bool result = await SystemConfigManager.Instance.UpdateLivePreviewPollingGroup(pollingGroupConfigDO);
                if (result)
                {
                    IRtekMessageBox.ShowDialog(Window.GetWindow(this), I18n.Properties.Resources.AddSuccessfully, I18n.Properties.Resources.Tip, Control.MessageBoxButton.OK);
                    ViewModel.LoadPollingGroups();
                }
                else
                {
                    IRtekMessageBox.ShowDialog(Window.GetWindow(this), I18n.Properties.Resources.AddFail, I18n.Properties.Resources.Tip, Control.MessageBoxButton.OK);
                    ViewModel.LoadPollingGroups();
                }
            }

            _currentpollingGroupViewModel = null;
            ViewModel.IsAddCruisePlan = false;

            ViewModel.CruisePageList.Clear();
            ViewModel.CruiseItemCacheDict.Clear();

            _splitVideoControl.CloseAddPollingMode();
            _latestSelectedCruisePage = 1;
        }

        private void btnEditPollingGroup_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is PollingGroupViewModel pollingGroupViewModel)
            {
                _currentpollingGroupViewModel = pollingGroupViewModel;
                foreach (var item in pollingGroupViewModel.PollingItems)
                {
                    if (!ViewModel.CruiseItemCacheDict.TryGetValue((item.Order / pollingGroupViewModel.SelectedSplitMode.ItemCount) + 1, out List<LivePreviewCruiseItem> values))
                    {
                        values = new List<LivePreviewCruiseItem>();
                        ViewModel.CruiseItemCacheDict.Add((item.Order / pollingGroupViewModel.SelectedSplitMode.ItemCount) + 1, values);
                    }
                    values.Add(new LivePreviewCruiseItem
                    {
                        DeviceName = item.DeviceName,
                        DeviceID = item.DeviceID,
                        ChannelID = item.ChannelID,
                        Order = item.Order % pollingGroupViewModel.SelectedSplitMode.ItemCount,
                    });
                }

                List<int> keys = ViewModel.CruiseItemCacheDict.Keys.ToList();
                foreach (int key in keys)
                {
                    if (!ViewModel.CruisePageList.Contains(key))
                    {
                        ViewModel.CruisePageList.Add(key);
                    }
                }
                ViewModel.IsAddCruisePlan = true;
                _splitVideoControl.OpenAddPollingMode();
                _splitVideoControl.SetDisplayCruiseItems(ViewModel.CruiseItemCacheDict[1]);
            }
        }

        private async void btnDeletePollingGroup_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is PollingGroupViewModel pollingGroupViewModel)
            {
                if (IRtekMessageBox.ShowDialog(Window.GetWindow(this), I18n.Properties.Resources.IsDelete, I18n.Properties.Resources.Tip, Control.MessageBoxButton.OKCancel) == true)
                {
                    ViewModel.PollingGroups.Remove(pollingGroupViewModel);
                    LivePreviewCruisePlanConfigDO pollingGroupConfigDO = new LivePreviewCruisePlanConfigDO();
                    pollingGroupConfigDO.LivePreviewPollingGroupInfos = ViewModel.PollingGroups.Where(it => !it.IsDefault).Select(it => it.ToLivePreviewPollingGroupInfo()).ToList();
                    bool result = await SystemConfigManager.Instance.UpdateLivePreviewPollingGroup(pollingGroupConfigDO);
                    if (result)
                    {
                        IRtekMessageBox.ShowDialog(Window.GetWindow(this), I18n.Properties.Resources.DeleteSuccess, I18n.Properties.Resources.Tip, Control.MessageBoxButton.OK);
                    }
                    else
                    {
                        IRtekMessageBox.ShowDialog(Window.GetWindow(this), I18n.Properties.Resources.DeleteFail, I18n.Properties.Resources.Tip, Control.MessageBoxButton.OK);
                        ViewModel.PollingGroups.Add(pollingGroupViewModel);
                    }
                }
            }
        }

        private void btnExpandCruise_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.CruiseControlVisibility == Visibility.Visible)
            {
                ViewModel.CruiseControlVisibility = Visibility.Collapsed;
            }
            else
            {
                ViewModel.PTZControlVisibility = Visibility.Collapsed;
                ViewModel.CruiseControlVisibility = Visibility.Visible;
            }
        }

        private void btnAddCruisePage_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.CruiseItemCacheDict.Add(ViewModel.CruiseItemCacheDict.Count + 1, new List<LivePreviewCruiseItem>());
            List<int> keys = ViewModel.CruiseItemCacheDict.Keys.ToList();
            foreach (int key in keys)
            {
                if (!ViewModel.CruisePageList.Contains(key))
                {
                    ViewModel.CruisePageList.Add(key);
                }
            }
            SwitchToPage(ViewModel.CruisePageList.Count);
            lvCruisePlanPage.SelectedIndex = ViewModel.CruisePageList.Count - 1;
        }

        private void lvCruisePage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is int currentValue)
            {
                SwitchToPage(currentValue);
            }
        }

        private void SwitchToPage(int toPage)
        {
            ViewModel.CruiseItemCacheDict[_latestSelectedCruisePage] = _splitVideoControl.GetDisplayCruiseItems();
            List<LivePreviewCruiseItem> pollingGroupList = ViewModel.CruiseItemCacheDict[toPage];
            _splitVideoControl.SetDisplayCruiseItems(pollingGroupList);
            _latestSelectedCruisePage = toPage;
        }

        private void cmCruisePageDelete_Click(object sender, RoutedEventArgs e)
        {
            int selectedIndex = lvCruisePlanPage.SelectedIndex;
            if (selectedIndex >= 0 && ViewModel.CruisePageList.Count > 1)
            {
                int selectPage = ViewModel.CruisePageList[selectedIndex];
                ViewModel.CruiseItemCacheDict.Remove(selectPage);

                List<List<LivePreviewCruiseItem>> cacheCruiseList = ViewModel.CruiseItemCacheDict.OrderBy(it => it.Key).Select(it => it.Value).ToList();
                int count = ViewModel.CruiseItemCacheDict.Count;

                ViewModel.CruiseItemCacheDict.Clear();
                ViewModel.CruisePageList.Clear();
                for (int i = 1; i <= count; i++)
                {
                    ViewModel.CruiseItemCacheDict.Add(i, cacheCruiseList[i - 1]);
                    ViewModel.CruisePageList.Add(i);
                }

                selectedIndex = (selectedIndex - 1) < 0 ? 0 : selectedIndex - 1;

                List<LivePreviewCruiseItem> pollingGroupList = ViewModel.CruiseItemCacheDict[selectedIndex + 1];
                _splitVideoControl.SetDisplayCruiseItems(pollingGroupList);
                _latestSelectedCruisePage = selectedIndex + 1;

                lvCruisePlanPage.SelectedIndex = selectedIndex;
            }
        }
    }
}