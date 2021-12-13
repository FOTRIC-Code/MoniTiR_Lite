// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Collections.Generic;

using Newtonsoft.Json;

using Firefly.Client.Service;
using System;

namespace Firefly.Client.Control.Fire
{
    /// <summary>
    /// Interaction logic for PresetControl.xaml
    /// </summary>
    public partial class PresetControl : TabItem
    {
        private readonly PresetViewModel _presetVM;
        private const int DEFAULT_SPEED = 1;
        private const int DEFAULT_SENSITIVITY = 1;
        private DeviceDO _deviceDO;

        private bool _fromCanvas;

        public PresetControl()
        {
            InitializeComponent();
            _presetVM = new PresetViewModel();
            DataContext = _presetVM;
            _fromCanvas = false;
        }

        private void PresetControl_Unloaded(object sender, RoutedEventArgs e)
        {
            try
            {
                Cursor = Cursors.Wait;
                _presetVM?.CleanRTSPUrl();
                LogoutDevice();
            }
            finally
            {
                Cursor = Cursors.Arrow;
            }
        }

        protected async override void OnSelected(RoutedEventArgs e)
        {
            base.OnSelected(e);

            try
            {
                Cursor = Cursors.Wait;
                _deviceDO = FireAgentDeviceManager.Instance.GetDeviceDO();

                // Local SQLite read — keeps Preset usable even if General tab was never opened.
                await FireAgentDeviceManager.Instance.GetDeviceConfig();

                DeviceParamDataObject deviceParamDO = FireAgentDeviceManager.Instance.GetDeviceParamDO();
                if (deviceParamDO?.GetIRDeviceModelDO() == null || deviceParamDO.GetIRDeviceModelDO().Width <= 0)
                {
                    deviceParamDO = await DeviceManager.Instance.GetDeviceParam(_deviceDO);
                    FireAgentDeviceManager.Instance.SetDeviceParamDO(deviceParamDO);
                }

                DevicePresetDataObject presetListDO = FireAgentDeviceManager.Instance.LoadPresetListDataObject();

                _presetVM.UpdateDevice(_deviceDO);
                _presetVM.Load(presetListDO, FireAgentDeviceManager.Instance.GetDeviceParamDO());

                DeviceStreamDO streamDO = await DeviceManager.Instance.GetStream(_deviceDO, StreamType.MainStream);
                _presetVM.SetRTSPUrl(streamDO);
                LoginDevice();
            }
            finally
            {
                Cursor = Cursors.Arrow;
            }
        }

        protected override void OnUnselected(RoutedEventArgs e)
        {
            base.OnUnselected(e);

            try
            {
                Cursor = Cursors.Wait;
                _presetVM.CleanRTSPUrl();
                LogoutDevice();
            }
            finally
            {
                Cursor = Cursors.Arrow;
            }
        }

        internal bool IsConfigChanged()
        {
            if (_presetVM?.SelectedItem != null)
            {
                DevicePresetDataObject devicePresetDO = FireAgentDeviceManager.Instance.LoadPresetListDataObject();
                PresetDataObject oldPresetDO = devicePresetDO.PresetList.FirstOrDefault(item => item.ID == _presetVM.SelectedItem.ID) ?? new PresetDataObject() { ID = _presetVM.SelectedItem.ID, Sensitivity = DEFAULT_SENSITIVITY };
                PresetDataObject newPresetDO = _presetVM.SelectedItem.ToPresetItemDO();
                return JsonConvert.SerializeObject(oldPresetDO) != JsonConvert.SerializeObject(newPresetDO);
            }
            return false;
        }

        internal void LoginDevice()
        {
            irClient.IsPaused = false;
            dcClient.IsPaused = false;
        }

        internal void LogoutDevice()
        {
            irClient.IsPaused = true;
            dcClient.IsPaused = true;
        }

        private void dgPreset_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (_presetVM?.SelectedItem != null)
            {
                if (IsConfigChanged())
                {
                    if (true == IRtekMessageBox.ShowDialog(Window.GetWindow(this), I18n.Properties.Resources.IsLeave, I18n.Properties.Resources.Operation, MessageBoxButton.OKCancel))
                    {
                        if (e.OriginalSource is FrameworkElement element && element.DataContext is PresetItemViewModel presetItemVM)
                        {
                            _presetVM.SelectedItem = presetItemVM;
                        }
                    }
                }
            }
        }

        private async void dgPreset_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_presetVM.SelectedItem == null)
            {
                return;
            }

            ApiResult<bool> apiResult;

            try
            {
                Cursor = Cursors.Wait;

                DevicePresetDataObject devicePresetDO = FireAgentDeviceManager.Instance.LoadPresetListDataObject();
                PresetDataObject presetDO = devicePresetDO.PresetList.FirstOrDefault(item => item.ID == _presetVM.SelectedItem.ID);
                _presetVM.LoadPresetItem(_presetVM.SelectedItem.ID, presetDO);

                apiResult = await DeviceManager.Instance.SetPreset(_deviceDO, new PTZPresetDataObject
                {
                    Command = PTZPresetCommand.GOTO,
                    PresetID = _presetVM.SelectedItem.ID,
                    Speed = DEFAULT_SPEED,
                });
            }
            finally
            {
                Cursor = Cursors.Arrow;
            }

            if (!apiResult.IsSuccess)
            {
                IRtekMessageBox.ShowDialog(Window.GetWindow(this), ErrorCodeTranslator.Translate(apiResult.Code), I18n.Properties.Resources.Tip, MessageBoxButton.OK);
                dgPreset.SelectedItem = null;
            }
        }

        private async void cbtnPresetClear_Click(object sender, RoutedEventArgs e)
        {
            bool? res = IRtekMessageBox.ShowDialog(Window.GetWindow(this), I18n.Properties.Resources.IsClear, I18n.Properties.Resources.Operation, MessageBoxButton.OKCancel);
            if (res != true)
            {
                return;
            }

            CruiseRouteDataObject cruiseRouteDO = FireAgentDeviceManager.Instance.LoadCruiseRouteDataObject();
            if (cruiseRouteDO.CruiseRouteList.Any(x => x.CruisePointList.Any(preset => preset.PresetID == _presetVM.SelectedItem.ID)))
            {
                IRtekMessageBox.ShowDialog(Window.GetWindow(this), I18n.Properties.Resources.DeletePresetNotify, I18n.Properties.Resources.Tip, MessageBoxButton.OK);
                return;
            }

            try
            {
                Cursor = Cursors.Wait;

                PresetItemViewModel presetItemVM = (sender as Button).Tag as PresetItemViewModel;

                ApiResult<bool> apiResult = await DeviceManager.Instance.SetPreset(_deviceDO, new PTZPresetDataObject
                {
                    Command = PTZPresetCommand.CLE,
                    PresetID = presetItemVM.ID,
                    Speed = DEFAULT_SPEED,
                });
                presetItemVM.Clear();
                if (apiResult.Code == ResponseCode.FIRE_DEVICE_INVALID_PTZ)
                {
                    IRtekMessageBox.ShowDialog(Window.GetWindow(this), I18n.Properties.Resources.InvalidPTZ, I18n.Properties.Resources.Tip, MessageBoxButton.OK);
                }
            }
            finally
            {
                Cursor = Cursors.Arrow;
            }
        }

        #region Marker DataGrid

        private void tbFireAlarm_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (dgMarker.SelectedItem is Marker marker)
            {
                AlarmRuleWindow alarmRuleControl = new AlarmRuleWindow
                {
                    Owner = Window.GetWindow(this),
                };
                alarmRuleControl.SelectedRuleID = marker.AlarmRuleID;
                alarmRuleControl.AlarmRuleSelectedCallback = AlarmRuleControl_Selected;
                alarmRuleControl.AlarmRuleDeletedCallback = AlarmRuleControl_Deleted;

                bool result = alarmRuleControl.ShowDialog() ?? false;
                // mabay already change alarm rule name, so need update datagrid data
                if (result == false)
                {
                    marker.RefreshAlarmRule();
                }
            }
        }

        private void AlarmRuleControl_Selected(AlarmRuleItemViewModel selectedItem)
        {
            if (dgMarker.SelectedItem is Marker marker)
            {
                marker.AlarmRule = selectedItem;
                marker.AlarmRuleID = selectedItem?.ID ?? 0;
            }
        }

        private void AlarmRuleControl_Deleted(List<int> deletedIDList)
        {
            if (deletedIDList != null && deletedIDList.Count > 0)
            {
                List<Marker> markerList = _presetVM.SelectedItem.Markers.Where(it => deletedIDList.Contains(it.AlarmRuleID)).ToList();
                foreach (Marker marker in markerList)
                {
                    marker.AlarmRule = null;
                    marker.AlarmRuleID = 0;
                }
            }
        }

        private void cbMarkersDelete_Click(object sender, RoutedEventArgs e)
        {
            bool? result = IRtekMessageBox.ShowDialog(Window.GetWindow(this), I18n.Properties.Resources.IsDelete, I18n.Properties.Resources.Delete, MessageBoxButton.OKCancel);
            if (result == true)
            {
                if (dgMarker.SelectedItem is Marker marker)
                {
                    _presetVM.SelectedItem.RemoveMarker(marker);
                    RefreshMarker();
                }
            }
        }

        private void cbtMarkerEdit_Click(object sender, RoutedEventArgs e)
        {
            if (dgMarker.SelectedItem is Marker marker)
            {
                PresetMarkerEditWindow window = new PresetMarkerEditWindow(marker, _presetVM.SelectedItem?.Markers);
                if (window.ShowDialog() == true)
                {
                    RefreshMarker();
                }
            }
        }

        #endregion

        #region ShieldMasks DataGrid

        private void cbShieldMasksDelete_Click(object sender, RoutedEventArgs e)
        {
            bool? result = IRtekMessageBox.ShowDialog(Window.GetWindow(this), I18n.Properties.Resources.IsDelete, I18n.Properties.Resources.Delete, MessageBoxButton.OKCancel);
            if (result == true)
            {
                if (dgShieldMasks.SelectedItem is ShieldMask mask)
                {
                    _presetVM.SelectedItem.RemoveMask(mask);
                    RefreshMarker();
                }
            }
        }

        private void cbtShieldMaskEdit_Click(object sender, RoutedEventArgs e)
        {
            if (dgShieldMasks.SelectedItem is ShieldMask mask)
            {
                PresetShieldMaskEditWindow windiow = new PresetShieldMaskEditWindow(mask, _presetVM.SelectedItem?.ShieldMasks);
                if (windiow.ShowDialog() == true)
                {
                    RefreshMarker();
                }
            }
        }

        #endregion

        private async void btSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Cursor = Cursors.Wait;
                ApiResult<bool> apiResult = new ApiResult<bool>(true);

                if (_presetVM.SelectedItem != null)
                {
                    apiResult = await DeviceManager.Instance.SetPreset(_deviceDO, new PTZPresetDataObject
                    {
                        Command = PTZPresetCommand.SET,
                        PresetID = _presetVM.SelectedItem.ID,
                        Speed = DEFAULT_SPEED,
                        PresetConfig = _presetVM.SelectedItem.ToPresetItemDO()
                    });
                }

                if (!apiResult.Data)
                {
                    IRtekMessageBox.ShowDialog(Window.GetWindow(this), GetSetInfo(apiResult.Code), I18n.Properties.Resources.Tip, MessageBoxButton.OK);
                }
                else
                {
                    DevicePresetDataObject presetListDataObject = _presetVM.ConvertToDataObject();
                    ApiResult<bool> result = await FireAgentDeviceManager.Instance.UpdateDeviceConfig(new FireAgentDataObject { PresetConfig = presetListDataObject });
                    if (result.IsSuccess)
                    {
                        FireAgentDeviceManager.Instance.UpdatePresetListDataObject(presetListDataObject);
                        foreach (PresetItemViewModel item in _presetVM.Items)
                        {
                            item.UpdatePresetUsed();
                        }
                    }

                    IRtekMessageBox.ShowDialog(Window.GetWindow(this), GetSaveInfo(result.Code), I18n.Properties.Resources.Tip, MessageBoxButton.OK);
                }
            }
            finally
            {
                Cursor = Cursors.Arrow;
            }
        }

        #region Video
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

            DeviceAddressDataObject deviceAddressDO = _deviceDO.DeviceAddress.FirstOrDefault(x => string.Equals(x.Type, DeviceType.IRCamera.ToString()));
            PTZControlDataObject PTZControlDO = new PTZControlDataObject
            {
                ChannelID = deviceAddressDO?.ChannelID ?? 0,
                Command = command,
                Speed = (int)sldSpeed.Value,
                Stop = PTZControlAction.Start //开始
            };
            _ = DeviceManager.Instance.PresetControl(_deviceDO, PTZControlDO);
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

            DeviceAddressDataObject deviceAddressDO = _deviceDO.DeviceAddress.FirstOrDefault(x => string.Equals(x.Type, DeviceType.IRCamera.ToString()));
            PTZControlDataObject PTZControlDO = new PTZControlDataObject
            {
                ChannelID = deviceAddressDO?.ChannelID ?? 0,
                Command = command,
                Speed = (int)sldSpeed.Value,
                Stop = PTZControlAction.Stop //停止
            };
            ApiResult<bool> apiResult = await DeviceManager.Instance.PresetControl(_deviceDO, PTZControlDO);
            if (apiResult.Code == ResponseCode.FIRE_DEVICE_INVALID_PTZ)
            {
                IRtekMessageBox.ShowDialog(Window.GetWindow(this), I18n.Properties.Resources.InvalidPTZ, I18n.Properties.Resources.Tip, MessageBoxButton.OK);
            }
        }

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
                if (apiResult.Code == ResponseCode.FIRE_DEVICE_INVALID_PTZ)
                {
                    IRtekMessageBox.ShowDialog(Window.GetWindow(this), I18n.Properties.Resources.InvalidPTZ, I18n.Properties.Resources.Tip, MessageBoxButton.OK);
                }
            }
        }

        private async void btnIRCameraPresetControl_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
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
                await DeviceManager.Instance.PresetControl(_deviceDO, PTZControlDO);
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
                if (apiResult.Code == ResponseCode.FIRE_DEVICE_INVALID_PTZ)
                {
                    IRtekMessageBox.ShowDialog(Window.GetWindow(this), I18n.Properties.Resources.InvalidPTZ, I18n.Properties.Resources.Tip, MessageBoxButton.OK);
                }
            }
        }

        private async void btnDCCameraPresetControl_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
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
                await DeviceManager.Instance.PresetControl(_deviceDO, PTZControlDO);
            }
        }

        #endregion

        #region Methods

        private void tabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.Source is TabControl)
            {
                dgMarker.SelectedItem = null;
                dgShieldMasks.SelectedItem = null;
                RefreshMarker();
            }
        }

        private void cbMarkersAdd_Click(object sender, RoutedEventArgs e)
        {
            popMarkerType.IsOpen = true;
        }

        private void cbShieldsAdd_Click(object sender, RoutedEventArgs e)
        {
            if (_presetVM.SelectedItem.ShieldMasks.Count >= Marker.MAX_SHIELD_COUNT)
            {
                IRtekMessageBox.ShowDialog(Window.GetWindow(this), I18n.Properties.Resources.PresetFullShieldMarkerMessage, I18n.Properties.Resources.Tip);
                return;
            }

            dgShieldMasks.SelectedItem = null;
            irClient.ShapeCreatorType = ShapeCreatorType.Mask;
        }

        private void ltbMarkerType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                popMarkerType.IsOpen = false;
                if (ltbMarkerType.SelectedItem is ListBoxItem item)
                {
                    if (item.Tag is MarkerType type)
                    {
                        if (type == MarkerType.Spot)
                        {
                            irClient.ShapeCreatorType = ShapeCreatorType.Spot;
                        }
                        else if (type == MarkerType.Line)
                        {
                            irClient.ShapeCreatorType = ShapeCreatorType.Polyline;
                        }
                        else if (type == MarkerType.Region)
                        {
                            irClient.ShapeCreatorType = ShapeCreatorType.Polygon;
                        }
                    }
                }
            }
            finally
            {
                ltbMarkerType.SelectedItem = null;
                dgMarker.SelectedItem = null;
            }
        }

        private void dpcCanvas_MarkerAdded(object sender, RoutedEventArgs e)
        {
            DeviceMarker deviceMarker = e.OriginalSource as DeviceMarker;
            if (deviceMarker.CanvasType == CanvasType.Marker)
            {
                PresetMarkerEditWindow window = new PresetMarkerEditWindow(deviceMarker, _presetVM.SelectedItem?.Markers)
                {
                    Owner = Window.GetWindow(this)
                };
                if (window.ShowDialog() == true)
                {
                    _presetVM.SelectedItem.Markers.Add(window.RawMarker);

                    dgMarker.SelectedItem = window.RawMarker;
                    dgMarker.ScrollIntoView(window.RawMarker);
                }
            }
            else if (deviceMarker.CanvasType == CanvasType.Shield)
            {
                PresetShieldMaskEditWindow window = new PresetShieldMaskEditWindow(deviceMarker, _presetVM.SelectedItem.ShieldMasks)
                {
                    Owner = Window.GetWindow(this)
                };
                if (window.ShowDialog() == true)
                {
                    _presetVM.SelectedItem.ShieldMasks.Add(window.RawShieldMask);

                    dgShieldMasks.SelectedItem = window.RawShieldMask;
                    dgShieldMasks.ScrollIntoView(window.RawShieldMask);
                }
            }

            RefreshMarker();
        }

        private void dpcCanvas_MarkerChanged(object sender, RoutedEventArgs e)
        {
            DeviceMarker deviceMarker = e.OriginalSource as DeviceMarker;
            _presetVM.SelectedItem.UpdateDeviceMarker(deviceMarker);
        }

        private void dpcCanvas_SelectChanged(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is DeviceMarker deviceMarker)
            {
                switch (deviceMarker.CanvasType)
                {
                    case CanvasType.Marker:
                        Marker marker = _presetVM.SelectedItem.Markers.FirstOrDefault(x => x.Name == deviceMarker.Name);
                        if (marker != null)
                        {
                            _fromCanvas = true;
                            dgMarker.SelectedItem = marker;
                            _fromCanvas = false;
                        }
                        break;
                    case CanvasType.Shield:
                        ShieldMask mask = _presetVM.SelectedItem.ShieldMasks.FirstOrDefault(x => x.Name == deviceMarker.Name);
                        if (mask != null)
                        {
                            _fromCanvas = true;
                            dgShieldMasks.SelectedItem = mask;
                            _fromCanvas = false;
                        }
                        break;
                }
            }
            else
            {
                _fromCanvas = true;
                dgMarker.SelectedItem = null;
                dgShieldMasks.SelectedItem = null;
                _fromCanvas = false;
            }
        }

        private void dgShieldMasks_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_fromCanvas)
            {
                return;
            }

            if (_presetVM.SelectedItem == null)
            {
                return;
            }

            if (dgShieldMasks.SelectedValue is ShieldMask mask)
            {
                irClient.SelectedDeviceMarker = mask.DeviceMarker;
            }

            RefreshVideoCanvas();
        }

        private void dgMarker_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_fromCanvas)
            {
                return;
            }

            if (_presetVM.SelectedItem == null)
            {
                return;
            }

            if (dgMarker.SelectedValue is Marker marker)
            {
                irClient.SelectedDeviceMarker = marker.DeviceMarker;
            }

            RefreshVideoCanvas();
        }

        private void RefreshMarker()
        {
            _presetVM.SelectedItem?.OnMarkerChanged();

            irClient.InvalidateVisual();
        }

        private void RefreshVideoCanvas()
        {
            _presetVM.SelectedItem?.RefreshCanvasMarker();

            irClient.InvalidateVisual();
        }

        #endregion

        #region private methods

        private string GetSaveInfo(int code)
        {
            string info = I18n.Properties.Resources.SavedSuccessfully;

            switch (code)
            {
                case ResponseCode.SUCCESS:
                    info = I18n.Properties.Resources.SavedSuccessfully;
                    break;
                case ResponseCode.FAILED:
                    info = I18n.Properties.Resources.SaveFailed;
                    break;
                default:
                    info = ErrorCodeTranslator.Translate(code);
                    break;
            }

            return info;
        }

        private string GetSetInfo(int code)
        {
            string info = I18n.Properties.Resources.SetSuccess;

            switch (code)
            {
                case ResponseCode.SUCCESS:
                    info = I18n.Properties.Resources.SetSuccess;
                    break;
                case ResponseCode.FAILED:
                    info = I18n.Properties.Resources.SetFail;
                    break;
                default:
                    info = ErrorCodeTranslator.Translate(code);
                    break;
            }

            return info;
        }

        #endregion
    }
}