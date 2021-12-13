// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Collections.Generic;

using Firefly.Client.Common;
using Firefly.Client.Service;
using Firefly.Client.Control;

namespace Firefly.Client
{
    /// <summary>
    /// Interaction logic for DataCenterPage.xaml
    /// </summary>
    public partial class DataCenterPage : Page, IPage
    {
        private readonly DataCenterPageViewModel _viewModel;

        public DataCenterPage()
        {
            InitializeComponent();
            _viewModel = new DataCenterPageViewModel();
            DataContext = _viewModel;
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                this.IsEnabled = false;
                Mouse.SetCursor(Cursors.Wait);
                if (!_viewModel.HasItem)
                {
                    LoadSelection();
                }
            }
            finally
            {
                this.IsEnabled = true;
                Mouse.SetCursor(Cursors.Arrow);
            }
        }

        private async void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.SelectionTree == null)
            {
                return;
            }
            IRtekWaitWindow waitWindow = new IRtekWaitWindow(Window.GetWindow(this), IRtekWaitWindowType.Animation);
            try
            {
                Mouse.SetCursor(Cursors.Wait);
                waitWindow.Show();
                //获取用户所选择的所有监控点，根据监控点所属的服务分类汇总后查询
                List<DeviceDO> selectedDevices = _viewModel.SelectionTree.GetSelectedItems<DeviceDO>();
                Dictionary<string, List<DeviceDO>> serverMap = new Dictionary<string, List<DeviceDO>>();
                foreach (DeviceDO deviceDO in selectedDevices)
                {
                    string serverAddress = deviceDO.DefaultAgent().GetServerAddress();
                    if (!serverMap.ContainsKey(serverAddress))
                    {
                        serverMap.Add(serverAddress, new List<DeviceDO>());
                    }
                    serverMap[serverAddress].Add(deviceDO);
                }

                List<DeviceDataStatisticsDO> deviceDataStatistics = new List<DeviceDataStatisticsDO>();
                List<Task<List<DeviceDataStatisticsDO>>> taskList = new List<Task<List<DeviceDataStatisticsDO>>>();
                foreach (KeyValuePair<string, List<DeviceDO>> keyValuePair in serverMap)
                {
                    Task<List<DeviceDataStatisticsDO>> task = await Task.Factory.StartNew(async () =>
                    {
                        return await DeviceManager.Instance.GetDeviceDataStatistics(new DeviceDataStatisticsQueryParam()
                        {
                            DeviceIDsStr = keyValuePair.Value.Select(x => x.ID).Aggregate("", (cur, x) => cur + x + ",").TrimEnd(','),
                            BeginTime = _viewModel.BeginTime.ToUniversalTime(),
                            EndTime = _viewModel.EndTime.ToUniversalTime()
                        }, keyValuePair.Key);
                    });
                    taskList.Add(task);
                }
                Task.WaitAll(taskList.ToArray(), Constants.TIMEOUT_REQUEST_DEFAULT);
                foreach (Task<List<DeviceDataStatisticsDO>> task in taskList)
                {
                    if (task.IsCompleted)
                    {
                        deviceDataStatistics.AddRange(task.Result);
                    }
                }
                _viewModel.LoadDeviceStatistics(deviceDataStatistics);
            }
            finally
            {
                await Task.Delay(200);
                waitWindow.Close();
                Mouse.SetCursor(Cursors.Arrow);
            }
        }

        private async void rbQueryDevice_OnChecked(object sender, RoutedEventArgs e)
        {
            if (_viewModel == null)
            {
                return;
            }
            LoadSelection();
        }

        private bool LoadSelection()
        {
            //从中心服务拉取所有的监控点，构造树形结构
            List<DeviceDO> deviceDOList = DeviceManager.Instance.GetCachedDeviceList();
            _viewModel.LoadDevice(deviceDOList);

            return true;
        }

        private void btnShowChart_Click(object sender, RoutedEventArgs e)
        {
            DataCenterDetailWindow window = new DataCenterDetailWindow(_viewModel.SelectedDeviceDataRecord, _viewModel.BeginTime, _viewModel.EndTime);
            window.Owner = Window.GetWindow(this);
            window.ShowDialog();
        }

        private void btnExport_Click(object sender, RoutedEventArgs e)
        {
            bool? result = CSVWriter.OuputData(_viewModel.ConvertAllToCSV());
            if (result == null)
            {
                return;
            }
            IRtekMessageBox.ShowDialog(Window.GetWindow(this), result == true ? I18n.Properties.Resources.ExportSuccess : I18n.Properties.Resources.ExportFailed, I18n.Properties.Resources.Tip);
        }

        private async void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                this.IsEnabled = false;
                Mouse.SetCursor(Cursors.Wait);
                LoadSelection();
            }
            finally
            {
                this.IsEnabled = true;
                Mouse.SetCursor(Cursors.Arrow);
            }
        }

        public void OnClosed()
        {
        }
    }
}
