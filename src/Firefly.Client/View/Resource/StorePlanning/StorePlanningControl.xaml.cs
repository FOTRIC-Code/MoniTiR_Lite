// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Collections.Generic;

using Firefly.Client.Common;
using Firefly.Client.Control;
using Firefly.Client.Service;
using System.Threading.Tasks;

namespace Firefly.Client
{
    public partial class StorePlanningControl : TabItem
    {
        internal const int INVALID_NVR_ID = 0;
        internal const int INVALID_NVR_CHANNEL_ID = -1;

        public StorePlanningControl()
        {
            ViewModel = new StorePlanningViewModel();
            this.DataContext = this;
            InitializeComponent();
        }

        public StorePlanningViewModel ViewModel { get; }

        #region Methods

        private void btnNvrManage_Click(object sender, RoutedEventArgs e)
        {
            ServerDO store = LiteAgentBootstrap.GetLocalStoreAgent();
            string ip = store?.IP ?? LiteAgentBootstrap.LocalIp;
            int port = store != null ? store.Port : LiteAgentBootstrap.StorePort;
            StoreServerConfigWindow window = new StoreServerConfigWindow(ip, port)
            {
                Owner = Window.GetWindow(this)
            };
            window.ShowDialog();
            _ = Load();
        }

        protected override void OnSelected(RoutedEventArgs e)
        {
            base.OnSelected(e);
            TrackingService.Instance.In(typeof(StorePlanningControl));
            _ = Load();
        }

        private async Task Load()
        {
            try
            {
                Cursor = Cursors.Wait;
                List<DeviceDO> deviceList = DeviceManager.Instance.GetCachedDeviceList();

                await ViewModel.LoadDeviceDOs(deviceList);
            }
            catch
            {
                ViewModel.Clear();
                throw;
            }
            finally
            {
                Cursor = Cursors.Arrow;
            }
        }

        private void tvDevice_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is StorePlanningNodeViewModel itemViewModel)
            {
                ViewModel.SelectedDevice = itemViewModel;
                LoadSelectedDevice();
            }
        }

        private void LoadSelectedDevice()
        {
            if (ViewModel.SelectedDevice != null)
            {
                bool hasServerAddress = !string.IsNullOrWhiteSpace(ViewModel.SelectedDevice.StoreServerAddress);
                List<NVRDataObject> nvrDataObjects = new List<NVRDataObject>();
                if (!hasServerAddress)
                {
                    return;
                }

                if (ViewModel.SelectedDevice.IsDeviceChannel)
                {
                    ViewModel.LoadNVRs();
                }
            }
        }

        private async void btnChannelSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Cursor = Cursors.Wait;

                NVRChannelDataObject nvrChannelDO = new NVRChannelDataObject();
                bool result = false;
                nvrChannelDO.DeviceID = ViewModel.SelectedDevice.DeviceID;
                nvrChannelDO.ChannelID = ViewModel.SelectedDevice.DeviceChannelID;

                // 新增
                if (ViewModel.SelectedDevice.NVRChannelDO == null
                    && ViewModel.SelectedNVR.NVRID != INVALID_NVR_ID)
                {
                    nvrChannelDO.NVRID = ViewModel.SelectedNVR.NVRID;

                    if (ViewModel.SelectedNVRChannel.ChannelInfo != null)
                    {
                        nvrChannelDO.ChannelInfo = new NVRChannelInfoDataObject();
                        nvrChannelDO.ChannelInfo.NVRChannelID = ViewModel.SelectedNVRChannel.ChannelInfo.NVRChannelID;

                        DeviceStreamDO mainStreamDO = await DeviceManager.Instance.GetStream(ViewModel.SelectedDevice.DeviceDO, StreamType.MainStream);
                        if (mainStreamDO != null && mainStreamDO.StreamItems.Count > ViewModel.SelectedDevice.DeviceChannelID)
                        {
                            nvrChannelDO.ChannelInfo.StreamUrl = mainStreamDO?.StreamItems[ViewModel.SelectedDevice.DeviceChannelID].RTSPUrl;
                        }
                        if (string.IsNullOrWhiteSpace(nvrChannelDO.ChannelInfo.StreamUrl))
                        {
                            IRtekMessageBox.ShowDialog(Window.GetWindow(this), I18n.Properties.Resources.SaveFailed, I18n.Properties.Resources.Tip);
                            return;
                        }
                    }

                    result = await StoreManager.Instance.AddNVRChannel(ViewModel.SelectedDevice.StoreServerAddress, nvrChannelDO);
                    if (result)
                    {
                        IRtekMessageBox.ShowDialog(Window.GetWindow(this), I18n.Properties.Resources.SavedSuccessfully, I18n.Properties.Resources.Tip, Control.MessageBoxButton.OK);
                    }
                    else
                    {
                        IRtekMessageBox.ShowDialog(Window.GetWindow(this), I18n.Properties.Resources.SaveFailed, I18n.Properties.Resources.Tip);
                    }

                    return;
                }

                // 删除
                if (ViewModel.SelectedDevice.NVRChannelDO != null
                    && ViewModel.SelectedNVR.NVRID == INVALID_NVR_ID)
                {
                    result = await StoreManager.Instance.DeleteNVRChannel(ViewModel.SelectedDevice.StoreServerAddress, ViewModel.SelectedDevice.NVRChannelDO.ID);
                    if (result)
                    {
                        IRtekMessageBox.ShowDialog(Window.GetWindow(this), I18n.Properties.Resources.SavedSuccessfully, I18n.Properties.Resources.Tip, Control.MessageBoxButton.OK);
                    }
                    else
                    {
                        IRtekMessageBox.ShowDialog(Window.GetWindow(this), I18n.Properties.Resources.SaveFailed, I18n.Properties.Resources.Tip);
                    }
                    return;
                }

                // 更新
                if (ViewModel.SelectedDevice.NVRChannelDO != null)
                {
                    if (ViewModel.SelectedDevice.NVRChannelDO.NVRID == ViewModel.SelectedNVR.NVRID
                        && Equals(ViewModel.SelectedDevice.NVRChannelDO.ChannelInfo, ViewModel.SelectedNVRChannel.ChannelInfo))
                    {
                        return;
                    }

                    nvrChannelDO.ID = ViewModel.SelectedDevice.NVRChannelDO.ID;
                    nvrChannelDO.NVRID = ViewModel.SelectedNVR.NVRID;

                    if (ViewModel.SelectedNVRChannel.ChannelInfo != null)
                    {
                        nvrChannelDO.ChannelInfo = new NVRChannelInfoDataObject();
                        nvrChannelDO.ChannelInfo.NVRChannelID = ViewModel.SelectedNVRChannel.ChannelInfo.NVRChannelID;

                        DeviceStreamDO mainStreamDO = await DeviceManager.Instance.GetStream(ViewModel.SelectedDevice.DeviceDO, StreamType.MainStream);
                        if (mainStreamDO != null && mainStreamDO.StreamItems.Count > ViewModel.SelectedDevice.DeviceChannelID)
                        {
                            nvrChannelDO.ChannelInfo.StreamUrl = mainStreamDO?.StreamItems[ViewModel.SelectedDevice.DeviceChannelID].RTSPUrl;
                        }
                        if (string.IsNullOrWhiteSpace(nvrChannelDO.ChannelInfo.StreamUrl))
                        {
                            IRtekMessageBox.ShowDialog(Window.GetWindow(this), I18n.Properties.Resources.SaveFailed, I18n.Properties.Resources.Tip);
                            return;
                        }
                    }
                    else if (ViewModel.SelectedDevice.NVRChannelDO.ChannelInfo != null)
                    {
                        nvrChannelDO.ChannelInfo = new NVRChannelInfoDataObject();
                        nvrChannelDO.ChannelInfo.NVRChannelID = ViewModel.SelectedDevice.NVRChannelDO.ChannelInfo.NVRChannelID;
                    }

                    result = await StoreManager.Instance.UpdateNVRChannel(ViewModel.SelectedDevice.StoreServerAddress, nvrChannelDO);
                    if (result)
                    {
                        IRtekMessageBox.ShowDialog(Window.GetWindow(this), I18n.Properties.Resources.SavedSuccessfully, I18n.Properties.Resources.Tip, Control.MessageBoxButton.OK);
                    }
                    else
                    {
                        IRtekMessageBox.ShowDialog(Window.GetWindow(this), I18n.Properties.Resources.SaveFailed, I18n.Properties.Resources.Tip);
                    }
                }
            }
            finally
            {
                LoadSelectedDevice();
                Cursor = Cursors.Arrow;
            }
        }

        #endregion Methods
    }
}