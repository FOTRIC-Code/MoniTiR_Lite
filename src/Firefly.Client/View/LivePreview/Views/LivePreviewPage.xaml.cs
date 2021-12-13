// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Linq;
using System.Windows;
using System.Threading;
using System.Windows.Input;
using System.Windows.Media;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Collections.Generic;

using log4net;

using Firefly.Client.Control;
using Firefly.Client.Service;

namespace Firefly.Client
{
    public partial class LivePreviewPage : Page, IPage
    {
        #region Fields

        private const int DEFAULT_SPEED = 1;

        private CancellationTokenSource _deviceThreadCancellationTokenSource;
        private DispatcherTimer _pollingTimer;
        private SplitVideoControl _splitVideoControl;
        private int _latestSelectedCruisePage = 1;
        private PollingGroupViewModel _currentpollingGroupViewModel;

        private ILog _logger = LogManager.GetLogger(typeof(LivePreviewPage));

        #endregion Fields

        #region Constructors

        public LivePreviewPage()
        {
            ViewModel = new LivePreviewPageViewModel();
            _splitVideoControl = new SplitVideoControl();
            _splitVideoControl.SelectedSelectionChanged = SelectedSelectionChanged;
            DataContext = this;
            ViewModel.LoadPreference();
            ViewModel.SplitModeList = _splitVideoControl.GetModeList();
            InitializeComponent();
        }

        #endregion Constructors

        public LivePreviewPageViewModel ViewModel { get; }

        #region Methods

        private void SelectedSelectionChanged(DisplayItemViewModel selectedDisplayItem)
        {
            if (ViewModel != null)
            {
                ViewModel.SelectedDisplayItem = selectedDisplayItem;
            }
        }

        private void LivePreviewPage_Loaded(object sender, RoutedEventArgs e)
        {
            TrackingService.Instance.In(typeof(LivePreviewPage));
            StartTask();
            ViewModel.SelectedSplitModeVM = _splitVideoControl.ChangeSplitMode(ViewModel.SelectedSplitMode);
            _splitVideoControl.SplitVideoControl_Loaded();
            if (!dockVideo.Children.Contains(_splitVideoControl))
            {
                if (_splitVideoControl.Parent != null && _splitVideoControl.Parent is Panel parent)
                {
                    parent.Children.Remove(_splitVideoControl);
                }
                dockVideo.Children.Add(_splitVideoControl);
            }
            List<DeviceDO> deviceDOs = DeviceManager.Instance.GetCachedDeviceList();
            ViewModel.LoadDevicesForTreeData(deviceDOs);
            _splitVideoControl.UpdateDevice(deviceDOs);
            DockPanel.SetDock(_splitVideoControl, Dock.Top);
        }

        private void LivePreviewPage_Unloaded(object sender, RoutedEventArgs e)
        {
            if (ViewModel.IsAddCruisePlan)
            {
                ViewModel.IsAddCruisePlan = false;
                ViewModel.CruisePageList.Clear();
                ViewModel.CruiseItemCacheDict.Clear();
                _splitVideoControl.CloseAddPollingMode();

                LivePreviewPreference.Intance.Save();

                _splitVideoControl.SplitVideoControl_Unloaded();
            }
            else
            {
                LivePreviewPreference.Intance.Save();

                _splitVideoControl.SplitVideoControl_Unloaded();
                _splitVideoControl.StopPlayer();
            }
            _currentpollingGroupViewModel = null;

            _deviceThreadCancellationTokenSource?.Cancel();
            dockVideo.Children.Remove(_splitVideoControl);

            if (_pollingTimer != null)
            {
                StopPolling();
            }

            ViewModel.SelectedDisplayItem = null;
        }

        private void StartTask()
        {
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            _deviceThreadCancellationTokenSource = cancellationTokenSource;
            Task.Factory.StartNew(() =>
            {
                while (!cancellationTokenSource.IsCancellationRequested)
                {
                    Thread.Sleep(10 * 1000);
                    if (cancellationTokenSource.IsCancellationRequested)
                    {
                        return;
                    }
                    this.Dispatcher?.Invoke(() =>
                    {
                        List<DeviceDO> deviceDOs = DeviceManager.Instance.GetCachedDeviceList();
                        ViewModel?.LoadDevicesForTreeData(deviceDOs);
                    });

                    _logger.Info($"Video Online State: {_splitVideoControl.ToString()}");
                }
            }, cancellationTokenSource.Token);
        }

        private void btnSplitMode_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.IsSplitModePopOpen = !ViewModel.IsSplitModePopOpen;
        }

        private void btnFullScreen_Click(object sender, RoutedEventArgs e)
        {
            dockVideo.Children.Remove(_splitVideoControl);
            LivePreviewFullScreenWindow window = new LivePreviewFullScreenWindow();
            window.Owner = Window.GetWindow(this);
            window.DataContext = ViewModel;
            window.VideoControl = _splitVideoControl;
            window.ShowDialog();
            if (!dockVideo.Children.Contains(_splitVideoControl))
            {
                if (_splitVideoControl.Parent != null && _splitVideoControl.Parent is Panel parent)
                {
                    parent.Children.Remove(_splitVideoControl);
                }
                dockVideo.Children.Add(_splitVideoControl);
            }
            DockPanel.SetDock(_splitVideoControl, Dock.Top);
        }

        private void btnStopPreview_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.IsAddCruisePlan)
            {
                _splitVideoControl.OpenAddPollingMode();
            }
            else
            {

                _splitVideoControl.StopPlayer();
            }
        }

        private void TreeViewItem_MouseDown(object sender, MouseButtonEventArgs e)
        {
            DockPanel dockPanel = sender as DockPanel;
            if (dockPanel != null)
            {
                DataObject data = new DataObject();
                data.SetData("DataContext", dockPanel.DataContext);
                DragDrop.DoDragDrop(dockPanel, data, DragDropEffects.Copy);
            }
        }

        private void btnExpand_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.PTZControlVisibility == Visibility.Visible)
            {
                ViewModel.PTZControlVisibility = Visibility.Collapsed;
            }
            else
            {
                ViewModel.CruiseControlVisibility = Visibility.Collapsed;
                ViewModel.PTZControlVisibility = Visibility.Visible;
            }
        }

        private void btnIRCameraPresetControl_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (ViewModel.SelectedDisplayItem == null)
            {
                return;
            }

            DeviceDO deviceDO = ViewModel.SelectedDisplayItem.DeviceDO;

            if (sender is Button controlButton)
            {
                DeviceAddressDataObject deviceAddressDO = deviceDO.DeviceAddress.FirstOrDefault(x => string.Equals(x.Type, DeviceType.IRCamera.ToString()));
                PTZControlDataObject PTZControlDO = new PTZControlDataObject
                {
                    ChannelID = deviceAddressDO?.ChannelID ?? 0,
                    Command = (PTZCommand)controlButton.Tag,
                    Speed = (int)sldSpeed.Value,
                    Stop = PTZControlAction.Start //开始
                };
                _ = DeviceManager.Instance.PresetControl(deviceDO, PTZControlDO);
            }
        }

        private async void btnIRCameraPresetControl_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (ViewModel.SelectedDisplayItem == null)
            {
                return;
            }

            DeviceDO deviceDO = ViewModel.SelectedDisplayItem.DeviceDO;

            if (sender is Button controlButton)
            {
                DeviceAddressDataObject deviceAddressDO = deviceDO.DeviceAddress.FirstOrDefault(x => string.Equals(x.Type, DeviceType.IRCamera.ToString()));
                PTZControlDataObject PTZControlDO = new PTZControlDataObject
                {
                    ChannelID = deviceAddressDO?.ChannelID ?? 0,
                    Command = (PTZCommand)controlButton.Tag,
                    Speed = (int)sldSpeed.Value,
                    Stop = PTZControlAction.Stop //停止
                };
                ApiResult<bool> apiResult = await DeviceManager.Instance.PresetControl(deviceDO, PTZControlDO);
                if (apiResult.Code == ResponseCode.EQUIPMENT_DEVICE_INVALID_PTZ || apiResult.Code == ResponseCode.FIRE_DEVICE_INVALID_PTZ)
                {
                    IRtekMessageBox.ShowDialog(Window.GetWindow(this), I18n.Properties.Resources.InvalidPTZ, I18n.Properties.Resources.Tip, Control.MessageBoxButton.OK);
                }
            }
        }

        private void WheelControl_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            string source = (sender as FrameworkElement).Tag as string;
            PTZCommand command = PTZCommand.TILT_UP;
            if (string.Equals("Up", source, StringComparison.InvariantCultureIgnoreCase))
            {
                command = PTZCommand.TILT_UP;
            }
            else if (string.Equals("Down", source, StringComparison.InvariantCultureIgnoreCase))
            {
                command = PTZCommand.TILT_DOWN;
            }
            else if (string.Equals("Left", source, StringComparison.InvariantCultureIgnoreCase))
            {
                command = PTZCommand.PAN_LEFT;
            }
            else if (string.Equals("Right", source, StringComparison.InvariantCultureIgnoreCase))
            {
                command = PTZCommand.PAN_RIGHT;
            }
            if (ViewModel.SelectedDisplayItem == null)
            {
                return;
            }

            DeviceDO deviceDO = ViewModel.SelectedDisplayItem.DeviceDO;
            DeviceAddressDataObject deviceAddressDO = deviceDO.DeviceAddress.FirstOrDefault(x => string.Equals(x.Type, DeviceType.IRCamera.ToString()));
            PTZControlDataObject PTZControlDO = new PTZControlDataObject
            {
                ChannelID = deviceAddressDO?.ChannelID ?? 0,
                Command = command,
                Speed = (int)sldSpeed.Value,
                Stop = PTZControlAction.Start //开始
            };
            _ = DeviceManager.Instance.PresetControl(deviceDO, PTZControlDO);
        }

        private async void WheelControl_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            string source = (sender as FrameworkElement).Tag as string;
            PTZCommand command = PTZCommand.TILT_UP;
            if (string.Equals("Up", source, StringComparison.InvariantCultureIgnoreCase))
            {
                command = PTZCommand.TILT_UP;
            }
            else if (string.Equals("Down", source, StringComparison.InvariantCultureIgnoreCase))
            {
                command = PTZCommand.TILT_DOWN;
            }
            else if (string.Equals("Left", source, StringComparison.InvariantCultureIgnoreCase))
            {
                command = PTZCommand.PAN_LEFT;
            }
            else if (string.Equals("Right", source, StringComparison.InvariantCultureIgnoreCase))
            {
                command = PTZCommand.PAN_RIGHT;
            }
            if (ViewModel.SelectedDisplayItem == null)
            {
                return;
            }

            DeviceDO deviceDO = ViewModel.SelectedDisplayItem.DeviceDO;

            DeviceAddressDataObject deviceAddressDO = deviceDO.DeviceAddress.FirstOrDefault(x => string.Equals(x.Type, DeviceType.IRCamera.ToString()));
            PTZControlDataObject PTZControlDO = new PTZControlDataObject
            {
                ChannelID = deviceAddressDO?.ChannelID ?? 0,
                Command = command,
                Speed = (int)sldSpeed.Value,
                Stop = PTZControlAction.Stop //停止
            };
            ApiResult<bool> apiResult = await DeviceManager.Instance.PresetControl(deviceDO, PTZControlDO);
            if (apiResult.Code == ResponseCode.EQUIPMENT_DEVICE_INVALID_PTZ || apiResult.Code == ResponseCode.FIRE_DEVICE_INVALID_PTZ)
            {
                IRtekMessageBox.ShowDialog(Window.GetWindow(this), I18n.Properties.Resources.InvalidPTZ, I18n.Properties.Resources.Tip, Control.MessageBoxButton.OK);
            }
        }

        private async void btnDoWiper_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.SelectedDisplayItem == null)
            {
                return;
            }

            DeviceDO deviceDO = ViewModel.SelectedDisplayItem.DeviceDO;
            DeviceAddressDataObject deviceAddressDO = deviceDO.DeviceAddress.FirstOrDefault(x => string.Equals(x.Type, DeviceType.IRCamera.ToString()));
            PTZControlDataObject PTZControlDO = new PTZControlDataObject
            {
                ChannelID = deviceAddressDO?.ChannelID ?? 0,
                Command = PTZCommand.WIPER
            };
            ApiResult<bool> apiResult = await DeviceManager.Instance.PresetControl(deviceDO, PTZControlDO);
            if (apiResult.Code == ResponseCode.EQUIPMENT_DEVICE_INVALID_PTZ || apiResult.Code == ResponseCode.FIRE_DEVICE_INVALID_PTZ)
            {
                IRtekMessageBox.ShowDialog(Window.GetWindow(this), I18n.Properties.Resources.InvalidPTZ, I18n.Properties.Resources.Tip, Control.MessageBoxButton.OK);
            }
        }

        private async void btnDCCameraPresetControl_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (ViewModel.SelectedDisplayItem == null)
            {
                return;
            }

            DeviceDO deviceDO = ViewModel.SelectedDisplayItem.DeviceDO;

            if (sender is Button controlButton)
            {
                DeviceAddressDataObject deviceAddressDO = deviceDO.DeviceAddress.FirstOrDefault(x => string.Equals(x.Type, DeviceType.DCCamera.ToString()));
                PTZControlDataObject PTZControlDO = new PTZControlDataObject
                {
                    ChannelID = deviceAddressDO?.ChannelID ?? 0,
                    Command = (PTZCommand)controlButton.Tag,
                    Speed = (int)sldSpeed.Value,
                    Stop = PTZControlAction.Stop //停止
                };
                ApiResult<bool> apiResult = await DeviceManager.Instance.PresetControl(deviceDO, PTZControlDO);
                if (apiResult.Code == ResponseCode.EQUIPMENT_DEVICE_INVALID_PTZ || apiResult.Code == ResponseCode.FIRE_DEVICE_INVALID_PTZ)
                {
                    IRtekMessageBox.ShowDialog(Window.GetWindow(this), I18n.Properties.Resources.InvalidPTZ, I18n.Properties.Resources.Tip, Control.MessageBoxButton.OK);
                }
            }
        }

        private void btnDCCameraPresetControl_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (ViewModel.SelectedDisplayItem == null)
            {
                return;
            }

            DeviceDO deviceDO = ViewModel.SelectedDisplayItem.DeviceDO;

            if (sender is Button controlButton)
            {
                DeviceAddressDataObject deviceAddressDO = deviceDO.DeviceAddress.FirstOrDefault(x => string.Equals(x.Type, DeviceType.DCCamera.ToString()));
                PTZControlDataObject PTZControlDO = new PTZControlDataObject
                {
                    ChannelID = deviceAddressDO?.ChannelID ?? 0,
                    Command = (PTZCommand)controlButton.Tag,
                    Speed = (int)sldSpeed.Value,
                    Stop = PTZControlAction.Start //开始
                };
                _ = DeviceManager.Instance.PresetControl(deviceDO, PTZControlDO);
            }
        }

        private async void btnGotoPreset_Click(object sender, MouseButtonEventArgs e)
        {
            FrameworkElement element = sender as FrameworkElement;
            if (element?.DataContext is RealTimePresetItemViewModel itemViewModel)
            {
                try
                {
                    ViewModel.SelectedPreset = itemViewModel;
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
                    ApiResult<bool> apiResult = await DeviceManager.Instance.SetPreset(deviceDO, new PTZPresetDataObject
                    {
                        Command = PTZPresetCommand.GOTO,
                        PresetID = itemViewModel.ID,
                        Speed = DEFAULT_SPEED,
                    });
                    if (apiResult.Code == ResponseCode.EQUIPMENT_DEVICE_INVALID_PTZ || apiResult.Code == ResponseCode.FIRE_DEVICE_INVALID_PTZ)
                    {
                        IRtekMessageBox.ShowDialog(Window.GetWindow(this), I18n.Properties.Resources.InvalidPTZ, I18n.Properties.Resources.Tip, Control.MessageBoxButton.OK);
                    }
                }
                finally
                {
                    Mouse.SetCursor(Cursors.Arrow);
                }
            }
        }

        private void LivePreviewPage_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (ViewModel.IsSplitModePopOpen)
            {
                ViewModel.IsSplitModePopOpen = false;
            }
        }

        private void SplitModeListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ListBox listBox && listBox.SelectedItem is Tuple<int, int> selectedItem)
            {
                ViewModel.SelectedSplitMode = selectedItem.Item1;
                ViewModel.SelectedSplitModeVM = _splitVideoControl.ChangeSplitMode(selectedItem.Item1);
            }
        }

        public void OnClosed()
        {
        }

        private void lviSearchResult_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount > 1)
            {
                TreeItem selectedTreeItem = (sender as FrameworkElement).DataContext as TreeItem;
                DependencyObject dependencyObject = VisualTreeHelper.GetChild(VisualTreeHelper.GetChild(tvDevice, 0), 0);
                if (dependencyObject is ScrollViewer scrollViewer)
                {
                    var scrollViewerOffset = scrollViewer.VerticalOffset;
                }
                if (ViewModel.SelectedDisplayItem != null)
                {
                    _splitVideoControl.ChangeSelectedDisplay(selectedTreeItem);
                }
            }
        }

        #endregion Methods

    }
}