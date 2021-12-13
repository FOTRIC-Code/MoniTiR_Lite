// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Collections.Generic;
using System.Windows.Documents;
using Firefly.Client.Service;

namespace Firefly.Client.Control.Fire
{
    /// <summary>
    /// Interaction logic for CopyWindow.xaml
    /// </summary>
    public partial class DeviceOverrideWindow : Window
    {
        private DeviceOverrideViewModel _deviceOverrideVM;

        public DeviceOverrideWindow()
        {
            InitializeComponent();

            _deviceOverrideVM = new DeviceOverrideViewModel();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        private async void btnOK_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Mouse.SetCursor(Cursors.Wait);

                List<DeviceItemViewModel> deviceItemVMs = _deviceOverrideVM.DeviceItems.Where(item => item.IsDeviceChecked).ToList();

                if (deviceItemVMs.Count == 0)
                {
                    IRtekMessageBox.ShowDialog(Window.GetWindow(this), I18n.Properties.Resources.NotSelectDeviceFailed, I18n.Properties.Resources.Tip, MessageBoxButton.OK);
                }
                else
                {
                    DeviceDO deviceDO = FireAgentDeviceManager.Instance.GetDeviceDO();
                    var configItems = new List<string>();
                    _deviceOverrideVM.ConfigItems.ForEach(x =>
                    {
                        if (x.IsChecked)
                        {
                            configItems.AddRange(x.PropertiesName);
                        }
                    });

                    DeviceOverrideDataObject deviceOverrideDataObject = new DeviceOverrideDataObject(deviceDO.ID,
                        deviceItemVMs.Select(item => item.DeviceID).ToList(), configItems);

                    ApiResult<bool> result = await FireAgentDeviceManager.Instance.OverriedDeviceConfig(deviceDO, deviceOverrideDataObject);

                    IRtekMessageBox.ShowDialog(Window.GetWindow(this), GetSaveInfo(result.Code), I18n.Properties.Resources.Tip, MessageBoxButton.OK);
                }
            }
            finally
            {
                Mouse.SetCursor(Cursors.Arrow);
                this.DialogResult = false;
            }
        }

        private string GetSaveInfo(int code)
        {
            string info = I18n.Properties.Resources.CopyFailed;

            switch (code)
            {
                case ResponseCode.SUCCESS:
                    info = I18n.Properties.Resources.CopySucceeded;
                    break;
                case ResponseCode.FAILED:
                    info = I18n.Properties.Resources.CopyFailed;
                    break;
                default:
                    info = ErrorCodeTranslator.Translate(code);
                    break;
            }
            return info;
        }

        private void cb_OnClick(object send, RoutedEventArgs e)
        {
            _deviceOverrideVM.RefreshSelectedProperty();
        }

        private void DeviceOverrideWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            _deviceOverrideVM.LoadData();
            DataContext = _deviceOverrideVM;
        }
    }
}
