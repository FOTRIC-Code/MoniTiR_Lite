// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Windows;
using System.Windows.Input;
using System.ComponentModel;
using System.Windows.Controls;

using Newtonsoft.Json;

using Firefly.Client.Service;

namespace Firefly.Client.Control.Fire
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private DeviceDO _deviceDO;
        private TabItem selectedTabItem;
        private const int SELECTED_ITEM_NULL_VALUE = -1;
        public MainWindow(DeviceDO deviceDO, Window owner)
        {
            Owner = owner;
            InitializeComponent();

            _deviceDO = deviceDO;

            FireAgentDeviceManager.Instance.SetDeviceDO(deviceDO);
        }

        protected override async void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);

            // Config + device param must be ready before any tab (esp. Preset) is used.
            await FireAgentDeviceManager.Instance.GetDeviceConfig();
            DeviceParamDataObject deviceParamDO = await DeviceManager.Instance.GetDeviceParam(_deviceDO);
            FireAgentDeviceManager.Instance.SetDeviceParamDO(deviceParamDO);
        }

        private void tabItem_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            TabItem tabItem = sender as TabItem;
            if (tabItem == null || tabItem == selectedTabItem)
            {
                return;
            }

            if (IsConfigChanged() && true != IRtekMessageBox.ShowDialog(GetWindow(this), I18n.Properties.Resources.IsLeave, I18n.Properties.Resources.Operation, MessageBoxButton.OKCancel))
            {
                e.Handled = true;
                return;
            }
            tcHeader.SelectedItem = tabItem;
            selectedTabItem = tabItem;
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            if (IsConfigChanged() && true != IRtekMessageBox.ShowDialog(GetWindow(this), I18n.Properties.Resources.IsLeave, I18n.Properties.Resources.Operation, MessageBoxButton.OKCancel))
            {
                e.Cancel = true;
            }
        }

        private void MainWindow_OnClosed(object sender, EventArgs e)
        {
            pcPreset.LogoutDevice();
            crcCruiseRoute.LogoutDevice();
            osdConfig.LogoutDevice();
            FireAgentDeviceManager.Instance.Dispose();
        }

        private async void btnExport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Mouse.SetCursor(Cursors.Wait);

                FireAgentDataObject fireAgentDataObject = await FireAgentDeviceManager.Instance.GetDeviceConfig();

                string fireAgentDataObjectStr = JsonConvert.SerializeObject(fireAgentDataObject ?? new FireAgentDataObject(), Formatting.Indented);

                ExportDialog.ShowDialog(Window.GetWindow(this), fireAgentDataObjectStr);
            }
            finally
            {
                Mouse.SetCursor(Cursors.Arrow);
            }
        }

        private async void btnImport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Mouse.SetCursor(Cursors.Wait);

                if (!FileHelper.ImportDataObjectFromFile(I18n.Properties.Resources.ImportFileFilter, out FireAgentDataObject fireAgentDataObject))
                {
                    return;
                }

                if (fireAgentDataObject == null)
                {
                    IRtekMessageBox.ShowDialog(Window.GetWindow(this), I18n.Properties.Resources.ImportFailed, I18n.Properties.Resources.Import, MessageBoxButton.OK);
                }
                else
                {
                    ApiResult<bool> result = await FireAgentDeviceManager.Instance.UpdateDeviceConfig(fireAgentDataObject);
                    if (result.IsSuccess)
                    {
                        await FireAgentDeviceManager.Instance.GetDeviceConfig();

                        int selectIndex = tcHeader.SelectedIndex;
                        tcHeader.SelectedIndex = SELECTED_ITEM_NULL_VALUE;
                        tcHeader.SelectedIndex = selectIndex;

                        IRtekMessageBox.ShowDialog(Window.GetWindow(this), I18n.Properties.Resources.ImportSuccessfully, I18n.Properties.Resources.Tip, MessageBoxButton.OK);
                    }
                    else
                    {
                        IRtekMessageBox.ShowDialog(Window.GetWindow(this), ErrorCodeTranslator.Translate(result.Code), I18n.Properties.Resources.Tip, MessageBoxButton.OK);
                    }
                }
            }
            catch
            {
                IRtekMessageBox.ShowDialog(Window.GetWindow(this), I18n.Properties.Resources.ImportFailed, I18n.Properties.Resources.Import, MessageBoxButton.OK);
            }
            finally
            {
                Mouse.SetCursor(Cursors.Arrow);
            }
        }

        public void btnCopyTo_Click(object sender, RoutedEventArgs e)
        {
            DeviceOverrideWindow window = new DeviceOverrideWindow();
            window.Owner = Window.GetWindow(this);
            window.ShowDialog();
        }

        public bool IsConfigChanged()
        {
            int index = tcHeader.SelectedIndex;
            bool isChanged = false;
            switch (index)
            {
                case 0: isChanged = gcGeneral.IsConfigChanged(); break;
                case 1: isChanged = etcEventTrigger.IsConfigChanged(); break;
                case 2: isChanged = osdConfig.IsConfigChanged(); break;
                case 3: isChanged = pcPreset.IsConfigChanged(); break;
                case 4: isChanged = crcCruiseRoute.IsConfigChanged(); break;
                case 5: isChanged = tscTaskSchedule.IsConfigChanged(); break;
                case 6: isChanged = ascAlarmSchedule.IsConfigChanged(); break;

                default: break;
            }

            return isChanged;
        }
    }
}