// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Collections.Generic;

using Firefly.Client.Control;
using Firefly.Client.Service;

namespace Firefly.Client
{
    /// <summary>
    /// Interaction logic for RealTimeMonitorWindow.xaml
    /// </summary>
    public partial class RealTimeMonitorWindow : Window
    {
        #region Fields

        private DeviceDO _deviceDO;
        private RealTimeMonitorViewModel _viewModel;

        #endregion

        #region Constructors

        public RealTimeMonitorWindow(DeviceDO deviceDO)
        {
            InitializeComponent();
            _viewModel = new RealTimeMonitorViewModel(deviceDO);
            DataContext = _viewModel;

            _deviceDO = deviceDO;
        }

        private async void root_Loaded(object sender, RoutedEventArgs e)
        {
            _viewModel.Load(await DeviceManager.Instance.GetStream(_deviceDO, StreamType.MainStream));
        }

        #endregion

        #region Methods

        private async void btnIRCameraPresetControl_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is Button controlButton)
            {
                DeviceAddressDataObject deviceDO = _deviceDO.DeviceAddress.FirstOrDefault(x => string.Equals(x.Type, DeviceType.IRCamera.ToString()));
                PTZControlDataObject PTZControlDO = new PTZControlDataObject
                {
                    ChannelID = deviceDO?.ChannelID ?? 0,
                    Command = (PTZCommand)controlButton.Tag,
                    Speed = (int)sldSpeed.Value,
                    Stop = PTZControlAction.Stop //停止
                };
                ApiResult<bool> apiResult = await DeviceManager.Instance.PresetControl(_deviceDO, PTZControlDO);
                if (apiResult.Code == ResponseCode.EQUIPMENT_DEVICE_INVALID_PTZ || apiResult.Code == ResponseCode.FIRE_DEVICE_INVALID_PTZ)
                {
                    IRtekMessageBox.ShowDialog(Window.GetWindow(this), I18n.Properties.Resources.InvalidPTZ, I18n.Properties.Resources.Tip, Control.MessageBoxButton.OK);
                }
            }
        }

        private void btnIRCameraPresetControl_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Button controlButton)
            {
                DeviceAddressDataObject deviceDO = _deviceDO.DeviceAddress.FirstOrDefault(x => string.Equals(x.Type, DeviceType.IRCamera.ToString()));
                PTZControlDataObject PTZControlDO = new PTZControlDataObject
                {
                    ChannelID = deviceDO?.ChannelID ?? 0,
                    Command = (PTZCommand)controlButton.Tag,
                    Speed = (int)sldSpeed.Value,
                    Stop = PTZControlAction.Start //开始
                };
                _ = DeviceManager.Instance.PresetControl(_deviceDO, PTZControlDO);
            }
        }

        private async void btnDCCameraPresetControl_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is Button controlButton)
            {
                DeviceAddressDataObject deviceDO = _deviceDO.DeviceAddress.FirstOrDefault(x => string.Equals(x.Type, DeviceType.DCCamera.ToString()));
                PTZControlDataObject PTZControlDO = new PTZControlDataObject
                {
                    ChannelID = deviceDO?.ChannelID ?? 0,
                    Command = (PTZCommand)controlButton.Tag,
                    Speed = (int)sldSpeed.Value,
                    Stop = PTZControlAction.Stop //停止
                };
                ApiResult<bool> apiResult = await DeviceManager.Instance.PresetControl(_deviceDO, PTZControlDO);
                if (apiResult.Code == ResponseCode.EQUIPMENT_DEVICE_INVALID_PTZ || apiResult.Code == ResponseCode.FIRE_DEVICE_INVALID_PTZ)
                {
                    IRtekMessageBox.ShowDialog(Window.GetWindow(this), I18n.Properties.Resources.InvalidPTZ, I18n.Properties.Resources.Tip, Control.MessageBoxButton.OK);
                }
            }
        }

        private void btnDCCameraPresetControl_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Button controlButton)
            {
                DeviceAddressDataObject deviceDO = _deviceDO.DeviceAddress.FirstOrDefault(x => string.Equals(x.Type, DeviceType.DCCamera.ToString()));
                PTZControlDataObject PTZControlDO = new PTZControlDataObject
                {
                    ChannelID = deviceDO?.ChannelID ?? 0,
                    Command = (PTZCommand)controlButton.Tag,
                    Speed = (int)sldSpeed.Value,
                    Stop = PTZControlAction.Start //开始
                };
                _ = DeviceManager.Instance.PresetControl(_deviceDO, PTZControlDO);
            }
        }

        private async void cbtnCruisePlay_Click(object sender, RoutedEventArgs e)
        {
            if (((FrameworkElement)sender).DataContext is RealTimeCruiseItemViewModel currentVM)
            {
                ApiResult<bool> apiResult = await DeviceManager.Instance.SetPTZCruise(_deviceDO,
                    new PTZCruiseDataObject()
                    {
                        Command = PTZCruiseCommand.RUN,
                        CruiseID = currentVM.ID
                    });
                if (apiResult.Code == ResponseCode.EQUIPMENT_DEVICE_INVALID_PTZ || apiResult.Code == ResponseCode.FIRE_DEVICE_INVALID_PTZ)
                {
                    IRtekMessageBox.ShowDialog(Window.GetWindow(this), I18n.Properties.Resources.InvalidPTZ, I18n.Properties.Resources.Tip, Control.MessageBoxButton.OK);
                }
            }
        }

        private async void cbtnCruisePause_Click(object sender, RoutedEventArgs e)
        {
            if (((FrameworkElement)sender).DataContext is RealTimeCruiseItemViewModel currentVM)
            {
                ApiResult<bool> apiResult = await DeviceManager.Instance.SetPTZCruise(_deviceDO,
                    new PTZCruiseDataObject()
                    {
                        Command = PTZCruiseCommand.STOP,
                        CruiseID = currentVM.ID
                    });
                if (apiResult.Code == ResponseCode.EQUIPMENT_DEVICE_INVALID_PTZ || apiResult.Code == ResponseCode.FIRE_DEVICE_INVALID_PTZ)
                {
                    IRtekMessageBox.ShowDialog(Window.GetWindow(this), I18n.Properties.Resources.InvalidPTZ, I18n.Properties.Resources.Tip, Control.MessageBoxButton.OK);
                }
            }
        }

        private async void btnDoWiper_Click(object sender, RoutedEventArgs e)
        {
            DeviceAddressDataObject deviceDO = _deviceDO.DeviceAddress.FirstOrDefault(x => string.Equals(x.Type, DeviceType.IRCamera.ToString()));
            PTZControlDataObject PTZControlDO = new PTZControlDataObject
            {
                ChannelID = deviceDO?.ChannelID ?? 0,
                Command = PTZCommand.WIPER
            };
            ApiResult<bool> apiResult = await DeviceManager.Instance.PresetControl(_deviceDO, PTZControlDO);
            if (apiResult.Code == ResponseCode.EQUIPMENT_DEVICE_INVALID_PTZ || apiResult.Code == ResponseCode.FIRE_DEVICE_INVALID_PTZ)
            {
                IRtekMessageBox.ShowDialog(Window.GetWindow(this), I18n.Properties.Resources.InvalidPTZ, I18n.Properties.Resources.Tip, Control.MessageBoxButton.OK);
            }
        }

        private void RealTimeMonitorWindow_OnClosed(object sender, EventArgs e)
        {
            irClient.Dispose();
            dcClient.Dispose();
        }

        #endregion
    }
}