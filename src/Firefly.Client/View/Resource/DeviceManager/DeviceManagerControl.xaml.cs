// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using Firefly.Client.Common;
using Firefly.Client.Control;
using Firefly.Client.Service;

using log4net;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace Firefly.Client
{
    /// <summary>
    /// Interaction logic for DeviceManagerControl.xaml
    /// </summary>
    public partial class DeviceManagerControl : TabItem
    {
        private readonly DeviceManagerViewModel _deviceManagerVM;
        private PaginationViewModel _paginationViewModel;
        private CancellationTokenSource _cancellationTokenSource;

        private ILog _logger = LogManager.GetLogger(typeof(DeviceManagerControl));

#pragma warning disable 0649

        [ImportMany(typeof(IDeviceConfigurer))]
        private IEnumerable<IDeviceConfigurer> _deviceConfigurers;

#pragma warning restore 0649

        private long _threadFlag = 0;
        private Thread _thread;

        public DeviceManagerControl()
        {
            InitializeComponent();
            _deviceManagerVM = new DeviceManagerViewModel();
            DataContext = _deviceManagerVM;

            _paginationViewModel = pPagination.PaginationViewModel;

            using (AggregateCatalog catalog = new AggregateCatalog())
            {
                catalog.Catalogs.Add(new DirectoryCatalog(Environment.CurrentDirectory, "Firefly.Client.Control.*.dll"));
                using (CompositionContainer container = new CompositionContainer(catalog))
                {
                    container.ComposeParts(this);
                }
            }
        }

        protected override void OnSelected(RoutedEventArgs e)
        {
            base.OnSelected(e);

            TrackingService.Instance.In(typeof(DeviceManagerControl));

            _cancellationTokenSource = new CancellationTokenSource();
            CancellationToken token = _cancellationTokenSource.Token;
            _thread = new Thread(() => UpdateDeviceRun(token));
            _thread.IsBackground = true;
            _thread.Start();
        }

        private void UpdateDeviceRun(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                this.Dispatcher.Invoke(() => { _ = Load(); });
                Thread.Sleep(10 * 1000);
            }
        }

        protected override void OnUnselected(RoutedEventArgs e)
        {
            base.OnUnselected(e);
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource = null;
            _deviceManagerVM.Clear();
            _paginationViewModel.TotalCount = 0;
        }

        private void Pagination_PageChanged(object sender, PageChangedEventArgs e)
        {
            _paginationViewModel = e.PaginationViewModel;
            if (this.IsLoaded)
            {
                _ = Load();
            }
        }

        private async Task Load()
        {
            try
            {
                PaginationData<List<DeviceDO>> deviceDOs = DeviceManager.Instance.GetDeviceListWithSort(new PaginationInfo
                {
                    SearchStr = _deviceManagerVM.SearchText,
                    Page = _paginationViewModel.CurrentPageIndex,
                    PageSize = _paginationViewModel.CurrentPageSize
                }, true);

                _paginationViewModel.TotalCount = deviceDOs.TotalCount;
                await _deviceManagerVM.Load(deviceDOs.Data);
            }
            catch
            {
                _deviceManagerVM.Clear();
                throw;
            }
            finally
            {
            }
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            DeviceManager.Instance.InvalidDeviceState();
            _ = Load();
        }

        private async void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Cursor = Cursors.Wait;

                DeviceDetailWindow window = new DeviceDetailWindow(null, Window.GetWindow(this));
                if (window.ShowDialog() != true)
                {
                    return;
                }
            }
            finally
            {
                Cursor = Cursors.Arrow;
            }

            await Load();

            DeviceManager.Instance.InvalidDeviceState();
        }

        private async void cbtnEdit_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Cursor = Cursors.Wait;
                root.CaptureMouse();

                DeviceViewModel deviceVM = (sender as Button).Tag as DeviceViewModel;

                DeviceDO deviceDO = await DeviceManager.Instance.GetDevice(deviceVM.ID);
                if (deviceDO == null)
                {
                    IRtekMessageBox.ShowDialog(Window.GetWindow(this), I18n.Properties.Resources.DeviceNotExist);
                    return;
                }

                DeviceDetailWindow window = new DeviceDetailWindow(deviceDO, Window.GetWindow(this));
                if (window.ShowDialog() != true)
                {
                    return;
                }
            }
            finally
            {
                root.ReleaseMouseCapture();
                Cursor = Cursors.Arrow;
            }

            _ = Load();

            DeviceManager.Instance.InvalidDeviceState();
        }

        private async void cbtnDelete_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Cursor = Cursors.Wait;

                DeviceViewModel deviceVM = (sender as Button).Tag as DeviceViewModel;

                DeviceDO deviceDO = await DeviceManager.Instance.GetDevice(deviceVM.ID);
                if (deviceDO == null)
                {
                    IRtekMessageBox.ShowDialog(Window.GetWindow(this), I18n.Properties.Resources.DeviceNotExist, I18n.Properties.Resources.Delete);
                    return;
                }

                if (IRtekMessageBox.ShowDialog(Window.GetWindow(this), I18n.Properties.Resources.IsDelete, I18n.Properties.Resources.Delete, Control.MessageBoxButton.OKCancel) != true)
                {
                    return;
                }

                if (!await DeviceManager.Instance.DeleteDevice(deviceVM.ID))
                {
                    IRtekMessageBox.ShowDialog(Window.GetWindow(this), I18n.Properties.Resources.DeleteFail, I18n.Properties.Resources.Delete);
                    return;
                }
            }
            finally
            {
                Cursor = Cursors.Arrow;
            }

            _ = Load();

            DeviceManager.Instance.InvalidDeviceState();
        }

        private async void cbtnDeviceConfig_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Cursor = Cursors.Wait;
                root.CaptureMouse();
                DeviceViewModel deviceVM = (sender as Button).Tag as DeviceViewModel;
                DeviceDO deviceDO = await DeviceManager.Instance.GetDevice(deviceVM.ID);

                if (deviceDO == null)
                {
                    IRtekMessageBox.ShowDialog(Window.GetWindow(this), I18n.Properties.Resources.DeviceNotExist);
                }
                else
                {
                    if (_deviceConfigurers != null)
                    {
                        foreach (IDeviceConfigurer deviceConfigurer in _deviceConfigurers)
                        {
                            if (deviceConfigurer.GetServerType().ToString() == deviceDO.DefaultAgent().Type)
                            {
                                deviceConfigurer.Open(deviceDO, Window.GetWindow(this));
                                break;
                            }
                        }
                    }
                }
            }
            finally
            {
                root.ReleaseMouseCapture();
                Cursor = Cursors.Arrow;
            }
            _ = Load();
        }

        private async void btnInfo_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Cursor = Cursors.Wait;
                root.CaptureMouse();
                DeviceViewModel deviceVM = (sender as Button).DataContext as DeviceViewModel;
                if (deviceVM == null)
                {
                    return;
                }
                DeviceStreamDO streamMainDO = await DeviceManager.Instance.GetStream(deviceVM.DeviceDO, StreamType.MainStream);
                DeviceStreamDO streamSubDO = await DeviceManager.Instance.GetStream(deviceVM.DeviceDO, StreamType.SubStream);
                string advertiseIp = DeviceManager.GetLocalIPv4();
                InfoWindow infoWindow = new InfoWindow();
                infoWindow.AddItem(I18n.Properties.Resources.DeviceID, deviceVM.DeviceDO.ID);
                streamMainDO?.StreamItems?.ForEach(it =>
                {
                    string url = Regex.Replace(it.RTSPUrl ?? string.Empty, Constants.IPS_REG, advertiseIp);
                    infoWindow.AddItem(I18n.Properties.Resources.MainStream + it.ChannelID, url);
                });
                streamSubDO?.StreamItems?.ForEach(it =>
                {
                    string url = Regex.Replace(it.RTSPUrl ?? string.Empty, Constants.IPS_REG, advertiseIp);
                    infoWindow.AddItem(I18n.Properties.Resources.SubStream + it.ChannelID, url);
                });
                infoWindow.Owner = Window.GetWindow(this);
                infoWindow.ShowDialog();
            }
            finally
            {
                root.ReleaseMouseCapture();
                Cursor = Cursors.Arrow;
            }
        }

        private void searchBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                _ = Load();
            }
        }

        private void dgCruiseRoute_Sorting(object sender, DataGridSortingEventArgs e)
        {
            e.Handled = true;
            if (CollectionViewSource.GetDefaultView(dgCruiseRoute.ItemsSource) is ListCollectionView collectionView)
            {
                ListSortDirection direction = e.Column.SortDirection != ListSortDirection.Ascending
    ? ListSortDirection.Ascending
    : ListSortDirection.Descending;
                e.Column.SortDirection = direction;

                IComparer comparer = new NaturalComparer(e.Column.SortMemberPath, direction);
                collectionView.SortDescriptions.Clear();
                collectionView.CustomSort = comparer;
            }
        }

        private class NaturalComparer : IComparer
        {
            private readonly string _sortColumn;
            private readonly ListSortDirection _direction;

            public NaturalComparer(string sortColumn, ListSortDirection direction)
            {
                _sortColumn = sortColumn;
                _direction = direction;
            }

            public int Compare(object x, object y)
            {
                int comparisonResult = 0;
                if (x is DeviceViewModel itemx && y is DeviceViewModel itemy)
                {
                    switch (_sortColumn)
                    {
                        case "Name":
                            comparisonResult = CompareStrings(itemx.Name, itemy.Name);
                            break;
                        case "DeviceType":
                            comparisonResult = CompareStrings(itemx.DeviceType, itemy.DeviceType);
                            break;
                        case "ServerName":
                            comparisonResult = CompareStrings(itemx.ServerName, itemy.ServerName);
                            break;
                        case "IP":
                            comparisonResult = CompareStrings(itemx.IP, itemy.IP);
                            break;
                        case "IsOnline":
                            comparisonResult = CompareStrings(itemx.StatusDesc, itemy.StatusDesc);
                            break;
                        default:
                            comparisonResult = 0;
                            break;
                    }
                }
                return _direction == ListSortDirection.Ascending ? comparisonResult : -comparisonResult;
            }

            private int CompareStrings(string strX, string strY)
            {
                int i = 0;
                int j = 0;

                while (i < strX.Length && j < strY.Length)
                {
                    char c1 = strX[i];
                    char c2 = strY[j];

                    // 如果字符是数字，提取数字并比较
                    if (Char.IsDigit(c1) && Char.IsDigit(c2))
                    {
                        string number1 = ExtractNumber(strX, ref i);
                        string number2 = ExtractNumber(strY, ref j);

                        int num1 = int.Parse(number1);
                        int num2 = int.Parse(number2);

                        if (num1 != num2)
                            return num1.CompareTo(num2);
                    }
                    else
                    {
                        if (c1 != c2)
                            return c1.CompareTo(c2);
                        i++;
                        j++;
                    }
                }

                return strX.Length.CompareTo(strY.Length);
            }

            private string ExtractNumber(string str, ref int index)
            {
                StringBuilder sb = new StringBuilder();
                while (index < str.Length && Char.IsDigit(str[index]))
                {
                    sb.Append(str[index]);
                    index++;
                }
                return sb.ToString();
            }
        }
    }
}