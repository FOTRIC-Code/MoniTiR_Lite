// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using Firefly.Client.Common;
using Firefly.Client.Control;
using Firefly.Client.Service;

namespace Firefly.Client
{
    public partial class AlarmTipWindow : Window
    {
        #region field

        private const int DEFAULT_SPEED = 1;
        private AlarmDO _alarmDO;
        private DeviceDO _deviceDO;

        #endregion field

        #region constructor

        public AlarmTipWindow(AlarmDO alarmDO, DeviceDO deviceDO)
        {
            ViewModel = new AlarmTipViewModel();
            DataContext = this;
            InitializeComponent();
            _alarmDO = alarmDO;
            _deviceDO = deviceDO;
        }

        #endregion constructor

        public AlarmTipViewModel ViewModel { get; set; }

        #region method

        private async void root_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                Cursor = Cursors.Wait;

                DateTime startTime = _alarmDO.AlarmTime.ToLocalTime().AddSeconds(-30);
                DateTime endTime = _alarmDO.AlarmTime.ToLocalTime().AddSeconds(30);
                ServerDO storeAgentDO = _deviceDO.GetAgent(ServerType.StoreAgent);
                PlaybackStreamDO playbackStreamDO = new PlaybackStreamDO();
                string storeAddress = storeAgentDO?.GetServerAddress();
                if (!string.IsNullOrWhiteSpace(storeAddress))
                {
                    playbackStreamDO = await StoreManager.Instance.GetPlaybackStream(storeAddress, _alarmDO.DeviceID, startTime, endTime);
                }
                DeviceStreamDO mainStreamDO = await DeviceManager.Instance.GetStream(_deviceDO, StreamType.MainStream);
                DeviceStreamDO subStreamDO = await DeviceManager.Instance.GetStream(_deviceDO, StreamType.SubStream);
                ViewModel.Load(_alarmDO, playbackStreamDO, mainStreamDO, subStreamDO);
            }
            finally
            {
                Cursor = Cursors.Arrow;
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            ViewModel.LiveVideoInfoVMs?.ForEach(it => it.IsPaused = true);
            ViewModel.PlaybackVideoInfoVMs?.ForEach(it => it.IsPaused = true);
        }

        private void btnPaly_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is VideoInfoViewModel videoInfoVM)
            {
                videoInfoVM.IsPaused = false;
            }
        }

        private async void btnTriggerAlarm_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Cursor = Cursors.Wait;
                DeviceDO deviceDO = DeviceManager.Instance.GetCached(_deviceDO.ID);
                DeviceParamDataObject deviceParamDO = await DeviceManager.Instance.GetDeviceParam(deviceDO);
                List<AlarmOutDataObject> alarmOutDOs = new List<AlarmOutDataObject>();
                for (int i = 1; i <= deviceParamDO.HardwareAbility.AlarmOutPortNum; i++)
                {
                    alarmOutDOs.Add(new AlarmOutDataObject(i, ChannelCommand.High));
                }
                await DeviceManager.Instance.SetAlarmOut(deviceDO, alarmOutDOs);
            }
            finally
            {
                Cursor = Cursors.Arrow;
            }
        }

        private async void btnClearAlarm_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Cursor = Cursors.Wait;
                DeviceDO deviceDO = await DeviceManager.Instance.GetDevice(_deviceDO.ID);
                DeviceParamDataObject deviceParamDO = await DeviceManager.Instance.GetDeviceParam(deviceDO);
                List<AlarmOutDataObject> alarmOutDOs = new List<AlarmOutDataObject>();
                for (int i = 1; i <= deviceParamDO.HardwareAbility.AlarmOutPortNum; i++)
                {
                    alarmOutDOs.Add(new AlarmOutDataObject(i, ChannelCommand.Low));
                }
                await DeviceManager.Instance.SetAlarmOut(deviceDO, alarmOutDOs);
            }
            finally
            {
                Cursor = Cursors.Arrow;
            }
        }

        private void rtspClientControl_MouserDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is VideoInfoViewModel videoInfoVM)
            {
                ViewModel.IsVideoFullScreen = true;
                ViewModel.FullScreenURL = videoInfoVM.MainStreamURL;
                PART_videoFullScreen.IsPaused = false;
            }
        }
        private void ccRestore_MouserDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ViewModel.IsVideoFullScreen)
            {
                PART_videoFullScreen.IsPaused = true;

            }
            ViewModel.RestoreScreen();
        }

        private void btnAnalyzIR_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is PlaceHolderViewModel itemVM)
            {
                List<List<Service.ImageInfo>> imagesGroups = new List<List<Service.ImageInfo>>();
                int currentIndex = 0;
                imagesGroups.Add(new List<Service.ImageInfo>()
                {
                    new ImageInfo
                    {
                        ChannelID = 0,
                        FileType = FileType.IRTekFile_IR,
                        URL = itemVM.Url
                    }
                });
                new IRFileViewerWindow(imagesGroups, currentIndex) { Owner = Window.GetWindow(this) }.ShowDialog();
            }
        }

        #endregion method

        private async void btnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!Regex.IsMatch(ViewModel.Note.TrimEnd(), Constants.NOTE_REG))
                {
                    IRtekMessageBox.ShowDialog(GetWindow(this), I18n.Properties.Resources.InvalidNote, I18n.Properties.Resources.Tip, Control.MessageBoxButton.OKCancel);
                    return;
                }
                AlarmConfirmerDataObject alarmConfirmerDO = new AlarmConfirmerDataObject(_alarmDO.ID, true, ViewModel.Note);
                bool result = await AlarmManager.Instance.ConfirmAlarm(alarmConfirmerDO);
                if (!result)
                {
                    IRtekMessageBox.ShowDialog(GetWindow(this), I18n.Properties.Resources.SaveFailed, I18n.Properties.Resources.Tip, Control.MessageBoxButton.OKCancel);
                }
                else
                {
                    Close();
                }
            }
            finally
            {
                Cursor = Cursors.Arrow;
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void tcAlarmTip_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(sender is TabControl tabControl)
            {
                if(tabControl.SelectedIndex == 2)
                {
                    ViewModel.LiveVideoInfoVMs?.ForEach(it => it.IsPaused = false);
                    ViewModel.PlaybackVideoInfoVMs?.ForEach(it => it.IsPaused = true);
                }
                else if(tabControl.SelectedIndex == 1)
                {
                    ViewModel.LiveVideoInfoVMs?.ForEach(it => it.IsPaused = true);
                }
                else
                {
                    ViewModel.LiveVideoInfoVMs?.ForEach(it => it.IsPaused = true);
                    ViewModel.PlaybackVideoInfoVMs?.ForEach(it => it.IsPaused = true);
                }
            }
        }

        private async void cbPreset_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_deviceDO == null)
            {
                return;
            }
            if (sender is ComboBox comboBox && comboBox.SelectedValue is Tuple<int, string> selectedValue)
            {
                try
                {
                    Cursor = Cursors.Wait;
                    ApiResult<bool> apiResult = await DeviceManager.Instance.SetPreset(_deviceDO, new PTZPresetDataObject
                    {
                        Command = PTZPresetCommand.GOTO,
                        PresetID = _alarmDO.PresetID,
                        Speed = DEFAULT_SPEED,
                    });
                    if (!apiResult.IsSuccess && (apiResult.Code == ResponseCode.EQUIPMENT_DEVICE_INVALID_PTZ || apiResult.Code == ResponseCode.FIRE_DEVICE_INVALID_PTZ))
                    {
                        IRtekMessageBox.ShowDialog(Window.GetWindow(this), I18n.Properties.Resources.InvalidPTZ, I18n.Properties.Resources.Tip, Control.MessageBoxButton.OK);
                    }
                }
                finally
                {
                    Cursor = Cursors.Arrow;
                }
            }
        }
    }
}