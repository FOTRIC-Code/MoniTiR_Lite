// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Linq;
using System.Windows;
using System.Threading;
using System.Windows.Input;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Collections.Generic;

using Firefly.Client.Common;
using Firefly.Client.Control;
using Firefly.Client.Service;

namespace Firefly.Client
{
    /// <summary>
    /// Interaction logic for StatusMonitorPage.xaml
    /// </summary>
    public partial class StatusMonitorPage : Page, IPage
    {
        private readonly StatusMonitorViewModel _statusMonitorVM;

        private long _lastestUpdateDeviceStatus = 0;

        private EventMessageBO _lastDataEventMsgBO;
        private CancellationTokenSource _cancellationTokenSource;

        public StatusMonitorPage()
        {
            InitializeComponent();
            _statusMonitorVM = new StatusMonitorViewModel();
            DataContext = _statusMonitorVM;

            AlarmManager.Instance.AddOrUpdateAlarmEvent += Page_AddOrUpdateDeviceAlarm;
        }

        private void Page_AddOrUpdateDeviceAlarm(AlarmDO alarmDO)
        {
            _statusMonitorVM.NotifyAlarm(alarmDO);
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            DeviceManager.Instance.InvalidDeviceState();
            List<DeviceDO> paginationDeviceData = DeviceManager.Instance.GetCachedDeviceList();
            _statusMonitorVM.Load(paginationDeviceData);

            _statusMonitorVM?.ResumeLoadPlotData();
            LoginDevice();

            _cancellationTokenSource = new CancellationTokenSource();
            CancellationToken token = _cancellationTokenSource.Token;
            Thread thread = new Thread(() => RefreshData(token));
            thread.IsBackground = true;
            thread.Start();
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            if (_statusMonitorVM != null)
            {
                LogoutDevice();
                _statusMonitorVM.PauseLoadPlotData();
                _lastDataEventMsgBO = null;
            }
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;

            _lastestUpdateDeviceStatus = 0;
            LogoutDevice();
        }

        private void RefreshData(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                RefreshDeviceStatus();
                RefreshDeviceAlarm();
                RefreshSampleData();
                Thread.Sleep(1000);
            }
        }

        private void RefreshSampleData()
        {
            List<EventMessageBO> dataEventMessageList = DeviceManager.Instance.GetDataEventMsgRange(_lastDataEventMsgBO, 500);

            if (dataEventMessageList?.Count > 0)
            {
                _lastDataEventMsgBO = dataEventMessageList.Last();
            }
            _statusMonitorVM.UpdateDeviceFromSampleData(dataEventMessageList);
        }

        private void RefreshDeviceAlarm()
        {
            _statusMonitorVM?.UpdateDeviceAlarm(AlarmManager.Instance.GetUnconfirmedCount());
        }

        private async void RefreshDeviceStatus()
        {
            if (_lastestUpdateDeviceStatus == 0)
            {
                _lastestUpdateDeviceStatus = DateTime.Now.ToBinary();
            }
            else if (DateTime.Now.Subtract(DateTime.FromBinary(_lastestUpdateDeviceStatus)).TotalMilliseconds > Constants.INTERVAL_REFRESH_DEVICE_STATUS)
            {
                _lastestUpdateDeviceStatus = DateTime.Now.ToBinary();
            }
            else
            {
                return;
            }
            List<ServerDO> serverDOs = await ServerManager.Instance.GetServerDOsAsync();
            List<DeviceStatusDataObject> allDeviceStatusList = new List<DeviceStatusDataObject>();
            if (serverDOs != null)
            {
                foreach (ServerDO server in serverDOs)
                {
                    if (server.Status == (int)DeviceStatus.Offline)
                    {
                        continue;
                    }
                    List<DeviceStatusDataObject> deviceStatusDOList = await DeviceManager.Instance.GetDeviceStatusList(server);
                    if (deviceStatusDOList != null && deviceStatusDOList?.Count > 0)
                    {
                        allDeviceStatusList.AddRange(deviceStatusDOList);
                    }
                }
            }
            _statusMonitorVM.UpdateDeviceStatus(allDeviceStatusList);
        }

        private async void RealTimeMonitor_OnClick(object sender, RoutedEventArgs e)
        {
            if (!(sender is Button deviceButton))
            {
                return;
            }

            if (!(deviceButton.DataContext is DeviceItemViewModel deviceItemVM))
            {
                return;
            }

            LogoutDevice();
            Cursor = Cursors.Wait;
            _statusMonitorVM.SelectedDevice = deviceItemVM;
            DeviceStreamDO streamDO = await DeviceManager.Instance.GetStream(deviceItemVM.DeviceDO, StreamType.MainStream);
            _statusMonitorVM.ChangedDevice(streamDO);
            LoginDevice();

            Cursor = Cursors.Arrow;
        }

        private async void ResumeCruise_OnClick(object sender, RoutedEventArgs e)
        {
            if (!(sender is Button deviceButton))
            {
                return;
            }

            if (!(deviceButton.DataContext is DeviceItemViewModel deviceItemViewModel))
            {
                return;
            }
            Cursor = Cursors.Wait;
            if (!await DeviceManager.Instance.RestoreCruise(deviceItemViewModel.DeviceDO))
            {
                IRtekMessageBox.ShowDialog(Window.GetWindow(this), I18n.Properties.Resources.ResumeCruiseFail, I18n.Properties.Resources.Tip);
            }
            Cursor = Cursors.Arrow;
        }

        private async void ToggleAlarmOutput_OnClick(object sender, RoutedEventArgs e)
        {
            if (!(sender is Button deviceButton))
            {
                return;
            }

            if (!(deviceButton.DataContext is DeviceItemViewModel deviceItemViewModel))
            {
                return;
            }
            Cursor = Cursors.Wait;
            List<AlarmOutDataObject> cmd = new List<AlarmOutDataObject>();
            int chl = 1;
            foreach (AlarmStatus alarmStatus in deviceItemViewModel.AlarmOutStatus)
            {
                cmd.Add(new AlarmOutDataObject(chl++, alarmStatus == AlarmStatus.Alarm ? ChannelCommand.Low : ChannelCommand.High));
            }

            if (!await DeviceManager.Instance.SetAlarmOut(deviceItemViewModel.DeviceDO, cmd))
            {
                IRtekMessageBox.ShowDialog(Window.GetWindow(this), I18n.Properties.Resources.SetAlarmOutFail, I18n.Properties.Resources.Tip);
            }
            else
            {
                for (int i = 0; i < deviceItemViewModel.AlarmOutStatus.Count; i++)
                {
                    deviceItemViewModel.AlarmOutStatus[i] = deviceItemViewModel.AlarmOutStatus[i] == AlarmStatus.Normal ? AlarmStatus.Alarm : AlarmStatus.Normal;
                }
            }
            Cursor = Cursors.Arrow;
        }

        private void lv_MainContainer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (!e.Handled)
            {
                FrameworkElement element = sender as FrameworkElement;
                e.Handled = true;
                var eventArg = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta);
                eventArg.RoutedEvent = MouseWheelEvent;
                eventArg.Source = sender;
                element.RaiseEvent(eventArg);
            }
        }

        private void Timeline_OnCompleted(object sender, EventArgs e)
        {
            if (_statusMonitorVM?.DeviceItems != null)
            {
                foreach (DeviceItemViewModel item in _statusMonitorVM.DeviceItems)
                {
                    item.IsAlarmed = false;
                }
            }
        }

        private void DeviceList_ItemMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is DeviceItemViewModel itemViewModel)
            {
                if (!itemViewModel.IsOnline)
                {
                    return;
                }

                if (!itemViewModel.SampleDataList.Any())
                {
                    return;
                }
                DeviceDetailMonitorWindow window = new DeviceDetailMonitorWindow(itemViewModel)
                {
                    Owner = Window.GetWindow(this)
                };
                window.ShowDialog();
            }
        }

        internal void LogoutDevice()
        {
            rtspVideo1.IsPaused = true;
            rtspVideo2.IsPaused = true;
        }

        internal void LoginDevice()
        {
            if (!string.IsNullOrEmpty(_statusMonitorVM.FirstVideoRTSPUrl))
            {
                rtspVideo1.IsPaused = false;
            }
            if (!string.IsNullOrEmpty(_statusMonitorVM.LastVideoRTSPUrl))
            {
                rtspVideo2.IsPaused = false;
            }

        }

        public void OnClosed()
        {
        }
    }
}
