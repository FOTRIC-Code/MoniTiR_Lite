// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;

using Newtonsoft.Json;

using Firefly.Client.Service;

namespace Firefly.Client.Control.Fire
{
    public partial class OSDConfigControl : TabItem
    {
        #region filed

        private OSDConfigViewModel _osdConfigVM;

        #endregion

        #region constructor

        public OSDConfigControl()
        {
            InitializeComponent();
            _osdConfigVM = new OSDConfigViewModel();
            DataContext = _osdConfigVM;
        }

        #endregion

        #region method

        protected async override void OnSelected(RoutedEventArgs e)
        {
            base.OnSelected(e);

            try
            {
                Cursor = Cursors.Wait;
                DeviceDO deviceDO = FireAgentDeviceManager.Instance.GetDeviceDO();
                DeviceStreamDO streamDO = await DeviceManager.Instance.GetStream(deviceDO, StreamType.MainStream);
                _osdConfigVM.SetRTSPUrl(streamDO);

                bool osdConfigDO = FireAgentDeviceManager.Instance.LoadOSDConfigDataObject();
                ChannelConfigDataObject channelConfigDO = FireAgentDeviceManager.Instance.LoadChannelConfigDataObject();
                PTZOSDConfigDataObject ptzOSDConfigDO = FireAgentDeviceManager.Instance.LoadPTZOSDConfigDataObject();
                MarkerOSDConfigDataObject markerOSDConfigDO = FireAgentDeviceManager.Instance.LoadMarkerOSDConfigDataObject();
                DeviceModelDataObject deviceModelDO = FireAgentDeviceManager.Instance.GetDeviceParamDO().GetIRDeviceModelDO();

                _osdConfigVM.Load(deviceDO.ProtocolType, osdConfigDO, deviceModelDO, channelConfigDO, ptzOSDConfigDO, markerOSDConfigDO);
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
                _osdConfigVM.CleanRTSPUrl();
                LogoutDevice();
                Cursor = Cursors.Wait;
            }
            finally
            {
                Cursor = Cursors.Arrow;
            }
        }

        internal void LogoutDevice()
        {
            PART_osd.Dispose();
        }

        public bool IsConfigChanged()
        {
            ChannelConfigDataObject channelConfigDO = FireAgentDeviceManager.Instance.LoadChannelConfigDataObject();
            if (channelConfigDO.ChannelDataObjectList == null)
            {
                channelConfigDO = new ChannelConfigDataObject(new System.Collections.Generic.List<ChannelDataObject> { new ChannelDataObject() });
            }
            PTZOSDConfigDataObject ptzOSDConfigDO = FireAgentDeviceManager.Instance.LoadPTZOSDConfigDataObject();
            if (ptzOSDConfigDO.Positon == null)
            {
                ptzOSDConfigDO.Positon = new OSDPoint();
            }

            bool oldOSDConfig = FireAgentDeviceManager.Instance.LoadOSDConfigDataObject();
            ChannelConfigDataObject oldChannelConfig = channelConfigDO;
            PTZOSDConfigDataObject oldPTZOSDConfig = ptzOSDConfigDO;
            MarkerOSDConfigDataObject oldMarkerOSDConfig = FireAgentDeviceManager.Instance.LoadMarkerOSDConfigDataObject();

            bool newOSDConfig = _osdConfigVM.EnableOSD;
            ChannelConfigDataObject newChannelConfig = _osdConfigVM.ToChannelConfigDataObject(PART_osd.ChannelOSDItem, PART_osd.TimeOSDItem);
            PTZOSDConfigDataObject newPTZOSDConfig = _osdConfigVM.ToPTZOSDConfigDataObject(PART_osd.PTZOSDItem);
            MarkerOSDConfigDataObject newMarkerOSDConfig = _osdConfigVM.ToMarkerOSDConfigDataObject();

            return oldOSDConfig != newOSDConfig ||
                JsonConvert.SerializeObject(oldChannelConfig) != JsonConvert.SerializeObject(newChannelConfig) ||
                JsonConvert.SerializeObject(oldPTZOSDConfig) != JsonConvert.SerializeObject(newPTZOSDConfig) ||
                JsonConvert.SerializeObject(oldMarkerOSDConfig) != JsonConvert.SerializeObject(newMarkerOSDConfig);
        }

        private async void btnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Cursor = Cursors.Wait;
                bool osdConfigDO = _osdConfigVM.EnableOSD;
                ChannelConfigDataObject channelConfigDO = _osdConfigVM.ToChannelConfigDataObject(PART_osd.ChannelOSDItem, PART_osd.TimeOSDItem);
                PTZOSDConfigDataObject ptzOSDConfigDO = _osdConfigVM.ToPTZOSDConfigDataObject(PART_osd.PTZOSDItem);
                MarkerOSDConfigDataObject markerOSDConfigDO = _osdConfigVM.ToMarkerOSDConfigDataObject();

                ApiResult<bool> result = await FireAgentDeviceManager.Instance.UpdateDeviceConfig(new FireAgentDataObject
                {
                    OSDConfigEnabled = osdConfigDO,
                    ChannelConfig = channelConfigDO,
                    PTZOSDConfig = ptzOSDConfigDO,
                    MarkerOSDConfig = markerOSDConfigDO
                });
                if (result.IsSuccess)
                {
                    FireAgentDeviceManager.Instance.UpdateOSDConfig(osdConfigDO);
                    FireAgentDeviceManager.Instance.UpdateChannelConfig(channelConfigDO);
                    FireAgentDeviceManager.Instance.UpdatePTZOSDConfig(ptzOSDConfigDO);
                    FireAgentDeviceManager.Instance.UpdateMarkerOSDConfig(markerOSDConfigDO);
                    IRtekMessageBox.ShowDialog(Window.GetWindow(this), I18n.Properties.Resources.SavedSuccessfully, I18n.Properties.Resources.Tip, MessageBoxButton.OK);
                }
                else if (result.IsForbidden)
                {
                    IRtekMessageBox.ShowDialog(Window.GetWindow(this), ErrorCodeTranslator.Translate(result.Code), I18n.Properties.Resources.Tip, MessageBoxButton.OK);
                }
                else
                {
                    IRtekMessageBox.ShowDialog(Window.GetWindow(this), I18n.Properties.Resources.SaveFailed, I18n.Properties.Resources.Tip, MessageBoxButton.OK);
                }
            }
            finally
            {
                Cursor = Cursors.Arrow;
            }
        }

        private void osd_PositionChanged(object sender, RoutedEventArgs e)
        {
            _osdConfigVM.RefreshOSD(PART_osd.ChannelOSDItem, PART_osd.TimeOSDItem, PART_osd.PTZOSDItem);
        }

        private void tbChannelName_KeyDown(object sender, KeyEventArgs e)
        {
            if (sender is FrameworkElement textBox && e.Key == Key.Enter)
            {
                TraversalRequest traversalRequest = new TraversalRequest(FocusNavigationDirection.Next);
                textBox.MoveFocus(traversalRequest);
            }
        }

        #endregion
    }
}