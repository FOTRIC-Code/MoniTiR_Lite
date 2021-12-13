// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using Firefly.Client.Common;
using Firefly.Client.Control;
using Firefly.Client.Service;
using log4net;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Firefly.Client
{
    /// <summary>
    /// DataCenterDetailWindow.xaml 的交互逻辑
    /// </summary>
    public partial class DataCenterDetailWindow : Window
    {
        private DataCenterDetailWindowViewModel _viewModel;

        private ILog _logger = LogManager.GetLogger(typeof(DataCenterDetailWindow));

        public DataCenterDetailWindow(DeviceDataStatisticsViewModel deviceDataStatisticsVM, DateTime beginTime, DateTime endTime)
        {
            InitializeComponent();
            _viewModel = new DataCenterDetailWindowViewModel()
            {
                FullRegionName = string.Join(Constants.NAVIGATION_SEPARATOR.ToString(), new[] { deviceDataStatisticsVM.FullRegionName, deviceDataStatisticsVM.PresetName }.Where(x => !string.IsNullOrWhiteSpace(x))),
                DeviceDataStatisticsViewModel = deviceDataStatisticsVM,
                BeginTime = beginTime,
                EndTime = endTime,
            };
            _viewModel.PaginationViewModel = pPagination.PaginationViewModel;
            _viewModel.PaginationViewModel.CurrentPageIndex = Constants.PAGE_NO_DEFAULT;
            DataContext = _viewModel;
            var descriptor = DependencyPropertyDescriptor.FromProperty(DynamicDataGrid.ItemsSourceProperty, typeof(DynamicDataGrid));
            descriptor.AddValueChanged(dgData, dgData_ItemsSourceChanged);
        }

        private void dgData_ItemsSourceChanged(object sender, EventArgs e)
        {
        }

        private async void Pagination_PageChanged(object sender, PageChangedEventArgs args)
        {
            IRtekWaitWindow waitWindow = new IRtekWaitWindow(this, IRtekWaitWindowType.Animation);
            try
            {
                waitWindow.Show();
                this.IsEnabled = false;
                Mouse.SetCursor(Cursors.Wait);
                await _viewModel.LoadData();
                amurPlotView.RefreshChart(_viewModel.PlotLegendList, _viewModel.PlotPointList);
            }
            finally
            {
                await Task.Delay(200);
                waitWindow.Close();
                this.IsEnabled = true;
                Mouse.SetCursor(Cursors.Arrow);
            }
        }

        private void btnShowImage_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.SelectdData == null)
            {
                return;
            }

            List<List<Service.ImageInfo>> imagesGroups = new List<List<Service.ImageInfo>>();
            int currentIndex = 0;
            foreach (DataViewModel dataVM in _viewModel.DataList)
            {
                List<ImageInfo> imageInfos = dataVM.ToImageInfos();
                if (imageInfos != null && imageInfos.Count > 0)
                {
                    imagesGroups.Add(imageInfos);
                    if (dataVM == _viewModel.SelectdData)
                    {
                        currentIndex = imagesGroups.Count - 1;
                    }
                }
            }
            if (imagesGroups.Count > 0)
            {
                new IRFileViewerWindow(imagesGroups, currentIndex) { Owner = this }.ShowDialog();
            }
        }

        private async void btnExport_Click(object sender, RoutedEventArgs e)
        {
            bool? exportImages = IRtekMessageBox.ShowDialog(Window.GetWindow(this), I18n.Properties.Resources.ExportImagesAsWell, I18n.Properties.Resources.Tip, Control.MessageBoxButton.YesNo);
            Microsoft.Win32.SaveFileDialog fileDialog = new Microsoft.Win32.SaveFileDialog();
            fileDialog.Filter = "Excel(*.csv)|*.csv";
            fileDialog.FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"{DateTime.Now:yyyyMMddHHmmssfff}.csv");
            fileDialog.InitialDirectory = AppDomain.CurrentDomain.BaseDirectory;
            if (fileDialog.ShowDialog() == true)
            {
                bool? result = await ExportData(fileDialog.FileName, _viewModel.DataList, exportImages ?? false);
                if (result == null)
                {
                    return;
                }
                IRtekMessageBox.ShowDialog(Window.GetWindow(this), result == true ? I18n.Properties.Resources.ExportSuccess : I18n.Properties.Resources.ExportFailed, I18n.Properties.Resources.Tip);
            }
        }

        private async void btnExportAll_Click(object sender, RoutedEventArgs e)
        {
            bool? exportImages = IRtekMessageBox.ShowDialog(Window.GetWindow(this), I18n.Properties.Resources.ExportImagesAsWell, I18n.Properties.Resources.Tip, Control.MessageBoxButton.YesNo);
            Microsoft.Win32.SaveFileDialog fileDialog = new Microsoft.Win32.SaveFileDialog();
            fileDialog.Filter = "Excel(*.csv)|*.csv";
            fileDialog.FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"{DateTime.Now:yyyyMMddHHmmssfff}.csv");
            fileDialog.InitialDirectory = AppDomain.CurrentDomain.BaseDirectory;
            if (fileDialog.ShowDialog() == true)
            {
                List<DataViewModel> dataVMList = new List<DataViewModel>();

                IRtekWaitWindow irtekWaitWindow = new IRtekWaitWindow(GetWindow(this), IRtekWaitWindowType.Animation);
                irtekWaitWindow.Show();
                if (_viewModel.DeviceDataStatisticsViewModel != null)
                {
                    if (string.Equals(_viewModel.DeviceDataStatisticsViewModel.DeviceDO.DefaultAgent().Type, ServerType.EnvironmentMonitorAgent.ToString(), StringComparison.InvariantCultureIgnoreCase))
                    {
                        DeviceConfigDataObject deviceConfigDO = await DeviceManager.Instance.GetDeviceConfig<DeviceConfigDataObject>(_viewModel.DeviceDataStatisticsViewModel.DeviceDO);

                        PaginationData<List<DeviceDataDO>> deviceDataList = await DeviceManager.Instance.GetDeviceData(new DeviceDataQueryParam()
                        {
                            StartTime = _viewModel.BeginTime.ToUniversalTime(),
                            EndTime = _viewModel.EndTime.ToUniversalTime(),
                            Page = 1,
                            PageSize = -1,

                            DeviceID = _viewModel.DeviceDataStatisticsViewModel.DeviceID,
                            ChannelID = _viewModel.DeviceDataStatisticsViewModel.ChannelID,
                            IsContainAlarm = true
                        }, _viewModel.DeviceDataStatisticsViewModel.DeviceDO);

                        if (deviceDataList.TotalCount > 0)
                        {
                            foreach (DeviceDataDO deviceData in deviceDataList.Data)
                            {
                                DataViewModel dataViewModel = new DataViewModel(deviceData, true);
                                ChannelOutputModel channelOutputModel = deviceConfigDO?.ChannelConfig?.ChannelList?.FirstOrDefault(it => it.ID == deviceData.ChannelID);
                                if (string.Equals(channelOutputModel.ChannelType, DeviceType.NoiseSensor.ToString(), StringComparison.InvariantCultureIgnoreCase))
                                {
                                    dataViewModel.DynamicData = new NoiseDataViewModel(deviceData.ID, deviceData.ChannelName, deviceData.CurrentValue, "", deviceData.AlarmLevel);
                                }
                                else if (string.Equals(channelOutputModel.ChannelType, DeviceType.EnvTempSensor.ToString(), StringComparison.InvariantCultureIgnoreCase))
                                {
                                    dataViewModel.DynamicData = new ENVDataViewModel(deviceData.ID, deviceData.ChannelName, deviceData.CurrentValue, 0.0f, deviceData.AlarmLevel);
                                }
                                else if (string.Equals(channelOutputModel.ChannelType, DeviceType.EnvHumSensor.ToString(), StringComparison.InvariantCultureIgnoreCase))
                                {
                                    dataViewModel.DynamicData = new ENVDataViewModel(deviceData.ID, deviceData.ChannelName, 0.0f, deviceData.CurrentValue, deviceData.AlarmLevel);
                                }

                                dataVMList.Add(dataViewModel);
                            }
                        }
                    }
                    else
                    {
                        PaginationData<List<DeviceDataDO>> deviceDataList = await DeviceManager.Instance.GetDeviceData(new DeviceDataQueryParam()
                        {
                            StartTime = _viewModel.BeginTime.ToUniversalTime(),
                            EndTime = _viewModel.EndTime.ToUniversalTime(),
                            Page = 1,
                            PageSize = -1,

                            DeviceID = _viewModel.DeviceDataStatisticsViewModel.DeviceID,
                            PresetID = _viewModel.DeviceDataStatisticsViewModel.PresetID,
                            IsContainAlarm = true
                        }, _viewModel.DeviceDataStatisticsViewModel.DeviceDO);

                        if (deviceDataList.TotalCount > 0)
                        {
                            deviceDataList.Data?.ForEach(it =>
                            {
                                DataViewModel dataViewModel = new DataViewModel(it, false);
                                IRDataViewModel iRDataViewModel = new IRDataViewModel(it.ID, it.MarkerName, new Amur.Common.Temperature(it.MaxTemp, Amur.Common.TemperatureUnit.Kelvin).CurrentValue, new Amur.Common.Temperature(it.MinTemp, Amur.Common.TemperatureUnit.Kelvin).CurrentValue, it.AlarmLevel);
                                iRDataViewModel.AvgTemp = new Amur.Common.Temperature(it.AvgTemp, Amur.Common.TemperatureUnit.Kelvin).CurrentValue;
                                dataViewModel.DynamicData = iRDataViewModel;
                                dataVMList.Add(dataViewModel);
                            });
                        }
                    }
                }
                irtekWaitWindow.Close();

                bool? result = await ExportData(fileDialog.FileName, dataVMList, exportImages ?? false);
                if (result == null)
                {
                    return;
                }
                IRtekMessageBox.ShowDialog(Window.GetWindow(this), result == true ? I18n.Properties.Resources.ExportSuccess : I18n.Properties.Resources.ExportFailed, I18n.Properties.Resources.Tip);
            }
        }

        private async Task<bool?> ExportData(string fileName, List<DataViewModel> datas, bool isExportImages)
        {
            bool? result = null;

            double maxValue = datas.Count;
            IRtekWaitWindow irtekWaitWindow = new IRtekWaitWindow(GetWindow(this), maxValue, IRtekWaitWindowType.AnimationCancelProgress);
            irtekWaitWindow.Show();
            double currentItemIndex = 0;
            result = true;
            List<string> csvList = new List<string>();
            List<string> firstRowList = new List<string>();
            Dictionary<string, int> dynamicTypeWithIndexList = new Dictionary<string, int>();
            firstRowList.Add(I18n.Properties.Resources.HappenTime);
            firstRowList.Add(datas[0].DataTitle);
            if (datas.Count > 0)
            {
                foreach (DataViewModel dataViewModel in datas)
                {
                    if (dataViewModel.DynamicData is IRDataViewModel)
                    {
                        IRDataViewModel dynamicData = dataViewModel.DynamicData as IRDataViewModel;
                        if (dynamicTypeWithIndexList.ContainsKey($"{nameof(IRDataViewModel)}_{dynamicData.Name}"))
                        {
                            continue;
                        }
                        dynamicTypeWithIndexList.Add($"{nameof(IRDataViewModel)}_{dynamicData.Name}", firstRowList.Count);
                        firstRowList.Add($"{dynamicData.Name}:{I18n.Properties.Resources.MaxTemperatureWithSymbol}");
                        firstRowList.Add($"{dynamicData.Name}:{I18n.Properties.Resources.MinTemperatureWithSymbol}");
                        if (dynamicData.AvgTemp != null)
                        {
                            firstRowList.Add($"{dynamicData.Name}:{I18n.Properties.Resources.AvgTemperatureWithSymbol}");
                            firstRowList.Add($"{dynamicData.Name}:{I18n.Properties.Resources.AlarmLevel}");
                        }
                        else
                        {
                            firstRowList.Add($"{dynamicData.Name}:{I18n.Properties.Resources.AlarmLevel}");
                        }
                        firstRowList.Add($"{dynamicData.Name}:{I18n.Properties.Resources.AlarmLevel}");
                    }
                    else if (dataViewModel.DynamicData is ENVDataViewModel)
                    {
                        ENVDataViewModel dynamicData = dataViewModel.DynamicData as ENVDataViewModel;
                        if (dynamicTypeWithIndexList.ContainsKey($"{nameof(ENVDataViewModel)}_{dynamicData.Name}"))
                        {
                            continue;
                        }
                        dynamicTypeWithIndexList.Add($"{nameof(ENVDataViewModel)}_{dynamicData.Name}", firstRowList.Count);
                        firstRowList.Add($"{dynamicData.Name}:{I18n.Properties.Resources.TemperatureWithSymbol}");
                        firstRowList.Add($"{dynamicData.Name}:{I18n.Properties.Resources.RHWithSymbol}");
                        firstRowList.Add($"{dynamicData.Name}:{I18n.Properties.Resources.AlarmLevel}");
                    }
                    else if (dataViewModel.DynamicData is VIBRDataViewModel)
                    {
                        VIBRDataViewModel dynamicData = dataViewModel.DynamicData as VIBRDataViewModel;
                        if (dynamicTypeWithIndexList.ContainsKey($"{nameof(VIBRDataViewModel)}_{dynamicData.Name}"))
                        {
                            continue;
                        }
                        dynamicTypeWithIndexList.Add($"{nameof(VIBRDataViewModel)}_{dynamicData.Name}", firstRowList.Count);
                        firstRowList.Add($"{dynamicData.Name}:{I18n.Properties.Resources.VelocityWithSymbol}");
                        firstRowList.Add($"{dynamicData.Name}:{I18n.Properties.Resources.AccelerationWithSymbol}");
                        firstRowList.Add($"{dynamicData.Name}:{I18n.Properties.Resources.TemperatureWithSymbol}");
                        firstRowList.Add($"{dynamicData.Name}:{I18n.Properties.Resources.VelocityAlarmLevel}");
                        firstRowList.Add($"{dynamicData.Name}:{I18n.Properties.Resources.AccelerationAlarmLevel}");
                        firstRowList.Add($"{dynamicData.Name}:{I18n.Properties.Resources.TemperatureAlarmLevel}");
                    }
                    else if (dataViewModel.DynamicData is VIBRSingleDataViewModel)
                    {
                        VIBRSingleDataViewModel dynamicData = dataViewModel.DynamicData as VIBRSingleDataViewModel;
                        if (dynamicTypeWithIndexList.ContainsKey($"{nameof(VIBRSingleDataViewModel)}_{dynamicData.Name}"))
                        {
                            continue;
                        }
                        dynamicTypeWithIndexList.Add($"{nameof(VIBRSingleDataViewModel)}_{dynamicData.Name}", firstRowList.Count);
                        firstRowList.Add($"{dynamicData.Name}:{dynamicData.LegendNameWithSymbol}");
                        firstRowList.Add($"{dynamicData.Name}:{dynamicData.AlarmLevelDesc}");
                    }
                    else if (dataViewModel.DynamicData is NoiseDataViewModel)
                    {
                        NoiseDataViewModel dynamicData = dataViewModel.DynamicData as NoiseDataViewModel;
                        if (dynamicTypeWithIndexList.ContainsKey($"{nameof(NoiseDataViewModel)}_{dynamicData.Name}"))
                        {
                            continue;
                        }
                        dynamicTypeWithIndexList.Add($"{nameof(NoiseDataViewModel)}_{dynamicData.Name}", firstRowList.Count);
                        firstRowList.Add($"{dynamicData.Name}:{I18n.Properties.Resources.NoiseWithSymbol}");
                        firstRowList.Add($"{dynamicData.Name}:{I18n.Properties.Resources.AlarmLevel}");
                    }
                    else if (dataViewModel.DynamicData is FileDataViewModel)
                    {
                        FileDataViewModel dynamicData = dataViewModel.DynamicData as FileDataViewModel;
                        if (dynamicTypeWithIndexList.ContainsKey($"{nameof(FileDataViewModel)}_{dynamicData.Name}"))
                        {
                            continue;
                        }
                        dynamicTypeWithIndexList.Add($"{nameof(FileDataViewModel)}_{dynamicData.Name}", firstRowList.Count);
                        firstRowList.Add($"{dynamicData.Name}:{I18n.Properties.Resources.FileName}");
                    }
                }
            }

            firstRowList.Add(I18n.Properties.Resources.FileName);

            int columnCount = firstRowList.Count;
            string titleStr = "";
            foreach (string title in firstRowList)
            {
                titleStr += $"{title},";
            }
            csvList.Add(titleStr.TrimEnd(','));

            foreach (DataViewModel dataVM in datas)
            {
                if (irtekWaitWindow.IsClosed)
                {
                    result = false;
                    break;
                }

                List<string> fileNames = new List<string>();
                if (dataVM.ImagePaths != null && isExportImages)
                {
                    foreach (DataImagePath imageInfo in dataVM.ImagePaths)
                    {
                        FileDataObject fileObject = await IRTekHttpClient.DownlodFileAsync(imageInfo.URL);
                        if (fileObject.HasFile)
                        {
                            string filePath = Path.Combine(Directory.GetParent(fileName).FullName, fileObject.FileName);
                            fileNames.Add(fileObject.FileName);
                            fileObject.Save(filePath);
                        }
                    }
                }
                string[] rowStrs = new string[columnCount];
                rowStrs[0] = dataVM.HappenTime.ToString("yyyy-MM-dd HH:mm:ss");
                rowStrs[1] = dataVM.Name;
                int startIndex = 0;
                if (dataVM.DynamicData is IRDataViewModel)
                {
                    IRDataViewModel dynamicData = dataVM.DynamicData as IRDataViewModel;
                    if (dynamicTypeWithIndexList.TryGetValue($"{nameof(IRDataViewModel)}_{dynamicData.Name}", out startIndex))
                    {
                        rowStrs[startIndex] = dynamicData.MaxTemp.ToString("F1");
                        rowStrs[startIndex + 1] = dynamicData.MinTemp.ToString("F1");

                        if (dynamicData.AvgTemp != null)
                        {
                            rowStrs[startIndex + 2] = dynamicData.AvgTemp?.ToString("F1");
                            rowStrs[startIndex + 3] = dynamicData.Alarm;
                        }
                        else
                        {
                            rowStrs[startIndex + 2] = dynamicData.Alarm;
                        }
                    }
                }
                else if (dataVM.DynamicData is ENVDataViewModel)
                {
                    ENVDataViewModel dynamicData = dataVM.DynamicData as ENVDataViewModel;
                    if (dynamicTypeWithIndexList.TryGetValue($"{nameof(ENVDataViewModel)}_{dynamicData.Name}", out startIndex))
                    {
                        rowStrs[startIndex] = dynamicData.Temp.ToString("F1");
                        rowStrs[startIndex + 1] = dynamicData.RH.ToString("F1");
                        rowStrs[startIndex + 2] = dynamicData.Alarm;
                    }
                }
                else if (dataVM.DynamicData is VIBRDataViewModel)
                {
                    VIBRDataViewModel dynamicData = dataVM.DynamicData as VIBRDataViewModel;
                    if (dynamicTypeWithIndexList.TryGetValue($"{nameof(VIBRDataViewModel)}_{dynamicData.Name}", out startIndex))
                    {
                        rowStrs[startIndex] = dynamicData.V?.ToString("F1");
                        rowStrs[startIndex + 1] = dynamicData.A?.ToString("F1");
                        rowStrs[startIndex + 2] = dynamicData.Temp?.ToString("F1");
                        rowStrs[startIndex + 3] = dynamicData.VAlarm;
                        rowStrs[startIndex + 4] = dynamicData.AAlarm;
                        rowStrs[startIndex + 5] = dynamicData.TempAlarm;
                    }
                }
                else if (dataVM.DynamicData is VIBRSingleDataViewModel)
                {
                    VIBRSingleDataViewModel dynamicData = dataVM.DynamicData as VIBRSingleDataViewModel;
                    if (dynamicTypeWithIndexList.TryGetValue($"{nameof(VIBRSingleDataViewModel)}_{dynamicData.Name}", out startIndex))
                    {
                        rowStrs[startIndex] = dynamicData.Value.ToString("F1");
                        rowStrs[startIndex + 1] = dynamicData.Alarm;
                    }
                }
                else if (dataVM.DynamicData is NoiseDataViewModel)
                {
                    NoiseDataViewModel dynamicData = dataVM.DynamicData as NoiseDataViewModel;
                    if (dynamicTypeWithIndexList.TryGetValue($"{nameof(NoiseDataViewModel)}_{dynamicData.Name}", out startIndex))
                    {
                        rowStrs[startIndex] = dynamicData.Spl.ToString("F1");
                        rowStrs[startIndex + 1] = dynamicData.Alarm;
                    }
                }
                else if (dataVM.DynamicData is FileDataViewModel)
                {
                    FileDataViewModel dynamicData = dataVM.DynamicData as FileDataViewModel;
                    if (dynamicTypeWithIndexList.TryGetValue($"{nameof(FileDataViewModel)}_{dynamicData.Name}", out startIndex))
                    {
                        rowStrs[startIndex] = dynamicData.Url;
                    }
                }

                foreach (string file in fileNames)
                {
                    rowStrs[rowStrs.Length - 1] += $"{file}";
                }
                string rowStr = "";
                foreach (string row in rowStrs)
                {
                    rowStr += $"{row},";
                }
                csvList.Add(rowStr.TrimEnd(','));
                irtekWaitWindow.SetValue(currentItemIndex);
                currentItemIndex++;
            }
            irtekWaitWindow.Close();
            CSVWriter.OuputData(fileName, csvList);

            return result;
        }
    }
}