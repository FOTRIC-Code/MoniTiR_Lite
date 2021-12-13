// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Collections.Generic;
using System.Threading.Tasks;

using Newtonsoft.Json;

using Firefly.Client.Common;
using Firefly.Client.Service;
using Firefly.Client.Control;

namespace Firefly.Client
{
    /// <summary>
    /// Interaction logic for DeviceDetailWindow.xaml
    /// </summary>
    public partial class DeviceDetailWindow : Window
    {
        #region Fields

        private DeviceDetailViewModel _deviceDetailVM;
        private DeviceDO _deviceDO;

        #endregion Fields

        #region Constructors

        public DeviceDetailWindow(DeviceDO deviceDO, Window owner)
        {
            Owner = owner;
            InitializeComponent();

            _deviceDO = deviceDO;
            irwhTittle.Title = deviceDO == null ? I18n.Properties.Resources.Add : I18n.Properties.Resources.Edit;
        }

        #endregion Constructors

        #region Methods

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                Cursor = Cursors.Wait;

                await LiteAgentBootstrap.EnsureLocalAgentsAsync();
                List<ServerDO> agentDOs = await ServerManager.Instance.GetServerDOsAsync()
                    ?? new List<ServerDO>();

                _deviceDetailVM = new DeviceDetailViewModel(_deviceDO, agentDOs);
                // Ensure channels are built before first paint so the grid is not empty for seconds.
                await _deviceDetailVM.UpdateAgentConfigAsync();
                DataContext = _deviceDetailVM;
            }
            finally
            {
                Cursor = Cursors.Arrow;
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            OnvifManager.Instance.CameraDiscovered -= Instance_CameraDiscovered;
        }

        private bool CheckData()
        {
            if (!NameHelper.IsNameCompliance(_deviceDetailVM.Name))
            {
                IRtekMessageBox.ShowDialog(this, I18n.Properties.Resources.InvalidName, I18n.Properties.Resources.Tip);
                return false;
            }

            if (_deviceDetailVM.SelectedStore == null || _deviceDetailVM.SelectedAgent == null)
            {
                IRtekMessageBox.ShowDialog(this, I18n.Properties.Resources.ServerTypeError, I18n.Properties.Resources.Tip);
                return false;
            }

            if (string.IsNullOrWhiteSpace(_deviceDetailVM.SelectedProtocolType))
            {
                IRtekMessageBox.ShowDialog(this, I18n.Properties.Resources.ServerTypeError, I18n.Properties.Resources.Tip);
                return false;
            }

            // FireMonitor: empty visible-light IP means single-channel — skip protocol check on that row.
            bool fireLiteChannelRule = _deviceDetailVM.SelectedAgent != null
                && _deviceDetailVM.SelectedAgent.Type == Service.ServerType.FireMonitorAgent;

            IEnumerable<SubDeviceViewModel> channelsToValidate = fireLiteChannelRule
                ? _deviceDetailVM.SubDeviceList.Where(it =>
                    it.CurrentSubDeviceTypeItem?.DeviceType != Service.DeviceType.DCCamera
                    || !string.IsNullOrWhiteSpace(it.IP))
                : _deviceDetailVM.SubDeviceList;

            if (channelsToValidate.Any(it => string.IsNullOrWhiteSpace(it.Protocol)))
            {
                IRtekMessageBox.ShowDialog(Window.GetWindow(this), I18n.Properties.Resources.InvalidProtocolType, I18n.Properties.Resources.Tip);
                return false;
            }

            if (fireLiteChannelRule)
            {
                SubDeviceViewModel ir = _deviceDetailVM.SubDeviceList.FirstOrDefault(it =>
                    it.CurrentSubDeviceTypeItem?.DeviceType == Service.DeviceType.IRCamera);
                if (ir == null || string.IsNullOrWhiteSpace(ir.IP))
                {
                    IRtekMessageBox.ShowDialog(this, I18n.Properties.Resources.InvalidIP, I18n.Properties.Resources.Tip);
                    return false;
                }
            }

            return true;
        }

        private void cbtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (_deviceDetailVM.SelectedSubDevice == null)
            {
                return;
            }

            SubDeviceDetailViewModel subDeviceDetailVM = new SubDeviceDetailViewModel(_deviceDetailVM.SelectedSubDevice);
            SubDeviceDetailWindow window = new SubDeviceDetailWindow(subDeviceDetailVM) { Owner = this };
            window.DataContext = subDeviceDetailVM;
            subDeviceDetailVM.AgentVM = _deviceDetailVM.SelectedAgent;
            subDeviceDetailVM.Title = I18n.Properties.Resources.Edit;
            if (window.ShowDialog() != true)
            {
                return;
            }

            _deviceDetailVM.SelectedSubDevice?.CopyFrom(subDeviceDetailVM.SubDeviceVM);
            if (_deviceDetailVM.IsLiteFireAdd)
            {
                _deviceDetailVM.SyncLiteFireProtocolFromChannels();
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private async void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (!CheckData())
            {
                return;
            }

            try
            {
                Cursor = Cursors.Wait;
                dgSubDeviceList.SelectedItem = null;
                this.IsEnabled = false;
                DeviceDO newValue = _deviceDetailVM.ConvertToDataObject();
                bool result = false;
                if (_deviceDetailVM.IsAddOperation)
                {
                    result = await DeviceManager.Instance.AddDevice(newValue);
                }
                else
                {
                    DeviceDO serverValue = await DeviceManager.Instance.GetDevice(newValue.ID);
                    if (JsonConvert.SerializeObject(newValue) != JsonConvert.SerializeObject(serverValue))
                    {

                        result = await DeviceManager.Instance.UpdateDevice(newValue);
                    }
                    else
                    {
                        result = true;
                    }
                }

                if (!result)
                {
                    IRtekMessageBox.ShowDialog(this, _deviceDetailVM.IsAddOperation ? I18n.Properties.Resources.AddFail : I18n.Properties.Resources.UpdateFail);
                    return;
                }

                if (IsVisible)
                {
                    DialogResult = true;
                }
            }
            finally
            {
                this.IsEnabled = true;
                Cursor = Cursors.Arrow;
            }
        }

        #endregion Methods

        private void cbtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (_deviceDetailVM.SelectedSubDevice == null)
            {
                return;
            }

            if (_deviceDetailVM.IsLiteFireAdd && !_deviceDetailVM.CanDeleteLiteFireChannel(_deviceDetailVM.SelectedSubDevice))
            {
                return;
            }

            if (IRtekMessageBox.ShowDialog(GetWindow(this), I18n.Properties.Resources.IsDelete, I18n.Properties.Resources.Tip, Control.MessageBoxButton.OKCancel) == true)
            {
                _deviceDetailVM.SubDeviceList.Remove(_deviceDetailVM.SelectedSubDevice);
                if (_deviceDetailVM.IsLiteFireAdd)
                {
                    _deviceDetailVM.SyncLiteFireProtocolFromChannels();
                }
                else
                {
                    _deviceDetailVM.IsSaveEnabled = _deviceDetailVM.SubDeviceList?.Count > 0;
                }
            }
        }

        private void btnShowDiscovery_Click(object sender, RoutedEventArgs e)
        {
            _deviceDetailVM.DiscoveryCameraList.Clear();
            _deviceDetailVM.IsShowDiscovery = !_deviceDetailVM.IsShowDiscovery;

            if (_deviceDetailVM.IsShowDiscovery)
            {
                OnvifManager.Instance.CameraDiscovered -= Instance_CameraDiscovered;
                OnvifManager.Instance.CameraDiscovered += Instance_CameraDiscovered;
                OnvifManager.Instance.StartDiscover();
            }
        }

        private void btnStartDiscovery_Click(object sender, RoutedEventArgs e)
        {
            _deviceDetailVM.DiscoveryCameraList.Clear();
            OnvifManager.Instance.CameraDiscovered -= Instance_CameraDiscovered;
            OnvifManager.Instance.CameraDiscovered += Instance_CameraDiscovered;
            OnvifManager.Instance.StartDiscover();
        }

        private void btnExport2Channel_Click(object sender, RoutedEventArgs e)
        {
            if (_deviceDetailVM.SelectedDiscoveryCamera == null)
            {
                return;
            }

            _deviceDetailVM.Try2UpdateSubDevice();
        }

        private void Instance_CameraDiscovered(object sender, CameraDiscoveryEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                _deviceDetailVM.DiscoverNewCamera(e.DiscoveryItems);
            }));
        }
    }
}