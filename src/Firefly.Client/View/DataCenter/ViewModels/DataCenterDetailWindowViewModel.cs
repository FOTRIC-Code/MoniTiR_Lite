// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using OxyPlot;

using Firefly.Client.Control;
using Firefly.Client.Service;

namespace Firefly.Client
{
    public class DataCenterDetailWindowViewModel : BaseViewModel
    {
        private PlotModel _canvasModel;

        private bool _isSensorDevice;

        public string FullRegionName { get; set; }

        public PaginationViewModel PaginationViewModel { get; set; }

        public DateTime BeginTime { get; set; } = DateTime.Today;

        public DateTime EndTime { get; set; } = DateTime.Now;
        public ObservableCollection<PlotPointViewModel> PlotPointList { get; set; }
        public List<PlotLegendViewModel> PlotLegendList { get; set; }
        public List<DataViewModel> DataList { get; set; }

        public DataViewModel SelectdData { get; set; }

        public DeviceDataStatisticsViewModel DeviceDataStatisticsViewModel { get; set; }

        public bool IsDeviceRecord => DeviceDataStatisticsViewModel != null;

        public bool HasRecord => DataList?.Any() ?? false;

        public bool IsSensorDevice
        {
            get => _isSensorDevice;
            set
            {
                _isSensorDevice = value;
                OnPropertyChanged();
            }
        }

        public PlotModel CanvasModel
        {
            get => _canvasModel;
            set
            {
                _canvasModel = value;
                OnPropertyChanged(nameof(CanvasModel));
            }
        }

        public DataCenterDetailWindowViewModel()
        {
        }

        private void DataList_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged(nameof(HasRecord));
        }

        public async Task LoadData()
        {
            if (DeviceDataStatisticsViewModel != null)
            {
                await LoadDeviceData();
            }
        }

        private async Task LoadDeviceData()
        {
            if (string.Equals(DeviceDataStatisticsViewModel.DeviceDO.DefaultAgent().Type, ServerType.EnvironmentMonitorAgent.ToString(), StringComparison.InvariantCultureIgnoreCase))
            {
                IsSensorDevice = true;
                DeviceConfigDataObject deviceConfigDO = await DeviceManager.Instance.GetDeviceConfig<DeviceConfigDataObject>(DeviceDataStatisticsViewModel.DeviceDO);

                PaginationData<List<DeviceDataDO>> deviceDataList = await DeviceManager.Instance.GetDeviceData(new DeviceDataQueryParam()
                {
                    StartTime = BeginTime.ToUniversalTime(),
                    EndTime = EndTime.ToUniversalTime(),
                    Page = PaginationViewModel.CurrentPageIndex,
                    PageSize = PaginationViewModel.CurrentPageSize,

                    DeviceID = DeviceDataStatisticsViewModel.DeviceID,
                    ChannelID = DeviceDataStatisticsViewModel.ChannelID,
                    IsContainAlarm = true
                }, DeviceDataStatisticsViewModel.DeviceDO);

                DataList = new List<DataViewModel>();
                PlotLegendList = new List<PlotLegendViewModel>();
                PlotPointList = new ObservableCollection<PlotPointViewModel>();
                PaginationViewModel.TotalCount = deviceDataList.TotalCount;
                if (deviceDataList.TotalCount > 0)
                {
                    foreach (DeviceDataDO deviceData in deviceDataList.Data)
                    {
                        DataViewModel dataViewModel = new DataViewModel(deviceData, true);
                        ChannelOutputModel channelOutputModel = deviceConfigDO?.ChannelConfig?.ChannelList?.FirstOrDefault(it => it.ID == deviceData.ChannelID);
                        if (string.Equals(channelOutputModel.ChannelType, DeviceType.NoiseSensor.ToString(), StringComparison.InvariantCultureIgnoreCase))
                        {
                            NoiseDataViewModel noiseDataVM = new NoiseDataViewModel(deviceData.ID, deviceData.ChannelName, deviceData.CurrentValue, "", deviceData.AlarmLevel);
                            noiseDataVM.HasAlarm = false;
                            dataViewModel.DynamicData = noiseDataVM;

                            // 添加OxyPlot图例
                            string noiseLegendName = $"{deviceData.ChannelName}-{I18n.Properties.Resources.Noise}";
                            TryAddOxyPlotLegend(noiseLegendName, I18n.Properties.Resources.NoiseWithSymbol);

                            AddOxyPlotPoint(noiseLegendName, dataViewModel.HappenTime, deviceData.CurrentValue);
                        }
                        else if (string.Equals(channelOutputModel.ChannelType, DeviceType.EnvTempSensor.ToString(), StringComparison.InvariantCultureIgnoreCase))
                        {
                            ENVDataViewModel envDataVM = new ENVDataViewModel(deviceData.ID, deviceData.ChannelName, deviceData.CurrentValue, null, deviceData.AlarmLevel);
                            envDataVM.HasAlarm = false;
                            dataViewModel.DynamicData = envDataVM;

                            // 添加OxyPlot图例
                            string tempLegendName = $"{deviceData.ChannelName}-{I18n.Properties.Resources.Temperature}";
                            TryAddOxyPlotLegend(tempLegendName, I18n.Properties.Resources.TemperatureWithSymbol);

                            AddOxyPlotPoint(tempLegendName, dataViewModel.HappenTime, deviceData.CurrentValue);
                        }
                        else if (string.Equals(channelOutputModel.ChannelType, DeviceType.EnvHumSensor.ToString(), StringComparison.InvariantCultureIgnoreCase))
                        {
                            ENVDataViewModel envDataVM = new ENVDataViewModel(deviceData.ID, deviceData.ChannelName, null, deviceData.CurrentValue, deviceData.AlarmLevel);
                            envDataVM.HasAlarm = false;
                            dataViewModel.DynamicData = envDataVM;

                            // 添加OxyPlot图例
                            string rhLegendName = $"{deviceData.ChannelName}-{I18n.Properties.Resources.RH}";
                            TryAddOxyPlotLegend(rhLegendName, I18n.Properties.Resources.RHWithSymbol);

                            AddOxyPlotPoint(rhLegendName, dataViewModel.HappenTime, deviceData.CurrentValue);
                        }
                        else if (string.Equals(channelOutputModel.ChannelType, DeviceType.ASensor.ToString(), StringComparison.InvariantCultureIgnoreCase))
                        {
                            VIBRSingleDataViewModel vibrDataVM = new VIBRSingleDataViewModel(deviceData.ID, deviceData.ChannelName, deviceData.CurrentValue, deviceData.AlarmLevel, "");
                            vibrDataVM.AlarmLevelDesc = I18n.Properties.Resources.AccelerationAlarmLevel;
                            vibrDataVM.LegendNameWithSymbol = I18n.Properties.Resources.AccelerationWithSymbol;
                            vibrDataVM.HasAlarm = false;
                            dataViewModel.DynamicData = vibrDataVM;

                            // 添加OxyPlot图例
                            string rhLegendName = $"{deviceData.ChannelName}-{I18n.Properties.Resources.Acceleration}";
                            TryAddOxyPlotLegend(rhLegendName, I18n.Properties.Resources.AccelerationWithSymbol);

                            AddOxyPlotPoint(rhLegendName, dataViewModel.HappenTime, deviceData.CurrentValue);
                        }
                        else if (string.Equals(channelOutputModel.ChannelType, DeviceType.VSensor.ToString(), StringComparison.InvariantCultureIgnoreCase))
                        {
                            VIBRSingleDataViewModel vibrDataVM = new VIBRSingleDataViewModel(deviceData.ID, deviceData.ChannelName, deviceData.CurrentValue, deviceData.AlarmLevel, "");
                            vibrDataVM.AlarmLevelDesc = I18n.Properties.Resources.VelocityAlarmLevel;
                            vibrDataVM.LegendNameWithSymbol = I18n.Properties.Resources.VelocityWithSymbol;
                            vibrDataVM.HasAlarm = false;
                            dataViewModel.DynamicData = vibrDataVM;

                            // 添加OxyPlot图例
                            string rhLegendName = $"{deviceData.ChannelName}-{I18n.Properties.Resources.Velocity}";
                            TryAddOxyPlotLegend(rhLegendName, I18n.Properties.Resources.VelocityWithSymbol);

                            AddOxyPlotPoint(rhLegendName, dataViewModel.HappenTime, deviceData.CurrentValue);
                        }
                        else if (string.Equals(channelOutputModel.ChannelType, DeviceType.SSensor.ToString(), StringComparison.InvariantCultureIgnoreCase))
                        {
                            VIBRSingleDataViewModel vibrDataVM = new VIBRSingleDataViewModel(deviceData.ID, deviceData.ChannelName, deviceData.CurrentValue, deviceData.AlarmLevel, "");
                            vibrDataVM.AlarmLevelDesc = I18n.Properties.Resources.ShiftAlarmLevel;
                            vibrDataVM.LegendNameWithSymbol = I18n.Properties.Resources.ShiftWithSymbol;
                            vibrDataVM.HasAlarm = false;
                            dataViewModel.DynamicData = vibrDataVM;

                            // 添加OxyPlot图例
                            string rhLegendName = $"{deviceData.ChannelName}-{I18n.Properties.Resources.Shift}";
                            TryAddOxyPlotLegend(rhLegendName, I18n.Properties.Resources.ShiftWithSymbol);

                            AddOxyPlotPoint(rhLegendName, dataViewModel.HappenTime, deviceData.CurrentValue);
                        }
                        else if (string.Equals(channelOutputModel.ChannelType, DeviceType.TempSensor.ToString(), StringComparison.InvariantCultureIgnoreCase))
                        {
                            VIBRSingleDataViewModel vibrDataVM = new VIBRSingleDataViewModel(deviceData.ID, deviceData.ChannelName, deviceData.CurrentValue, deviceData.AlarmLevel, "");
                            vibrDataVM.AlarmLevelDesc = I18n.Properties.Resources.TemperatureAlarmLevel;
                            vibrDataVM.LegendNameWithSymbol = I18n.Properties.Resources.TemperatureWithSymbol;
                            vibrDataVM.HasAlarm = false;
                            dataViewModel.DynamicData = vibrDataVM;

                            // 添加OxyPlot图例
                            string rhLegendName = $"{deviceData.ChannelName}-{I18n.Properties.Resources.Temperature}";
                            TryAddOxyPlotLegend(rhLegendName, I18n.Properties.Resources.TemperatureWithSymbol);

                            AddOxyPlotPoint(rhLegendName, dataViewModel.HappenTime, deviceData.CurrentValue);
                        }
                        else if (string.Equals(channelOutputModel.ChannelType, DeviceType.TcSensor.ToString(), StringComparison.InvariantCultureIgnoreCase))
                        {
                            TcDataViewModel tcDataVM = new TcDataViewModel(deviceData.ID, deviceData.ChannelName, deviceData.CurrentValue, deviceData.AlarmLevel, "");
                            tcDataVM.HasAlarm = false;
                            dataViewModel.DynamicData = tcDataVM;

                            // 添加OxyPlot图例
                            string tcLegendName = $"{deviceData.ChannelName}-{I18n.Properties.Resources.Temperature}";
                            TryAddOxyPlotLegend(tcLegendName, I18n.Properties.Resources.TemperatureWithSymbol);

                            AddOxyPlotPoint(tcLegendName, dataViewModel.HappenTime, deviceData.CurrentValue);
                        }

                        DataList.Add(dataViewModel);
                    }
                }
            }
            else
            {
                IsSensorDevice = false;
                PaginationData<List<DeviceDataDO>> deviceDataList = await DeviceManager.Instance.GetDeviceData(new DeviceDataQueryParam()
                {
                    StartTime = BeginTime.ToUniversalTime(),
                    EndTime = EndTime.ToUniversalTime(),
                    Page = PaginationViewModel.CurrentPageIndex,
                    PageSize = PaginationViewModel.CurrentPageSize,

                    DeviceID = DeviceDataStatisticsViewModel.DeviceID,
                    PresetID = DeviceDataStatisticsViewModel.PresetID,
                    IsContainAlarm = true
                }, DeviceDataStatisticsViewModel.DeviceDO);

                DataList = new List<DataViewModel>();
                PlotLegendList = new List<PlotLegendViewModel>();
                PlotPointList = new ObservableCollection<PlotPointViewModel>();
                PaginationViewModel.TotalCount = deviceDataList.TotalCount;
                if (deviceDataList.TotalCount > 0)
                {
                    deviceDataList.Data?.ForEach(it =>
                    {
                        DataViewModel dataViewModel = new DataViewModel(it, false);
                        IRDataViewModel iRDataViewModel = new IRDataViewModel(it.ID, it.MarkerName, new Amur.Common.Temperature(it.MaxTemp, Amur.Common.TemperatureUnit.Kelvin).CurrentValue, new Amur.Common.Temperature(it.MinTemp, Amur.Common.TemperatureUnit.Kelvin).CurrentValue, it.AlarmLevel);
                        iRDataViewModel.AvgTemp = new Amur.Common.Temperature(it.AvgTemp, Amur.Common.TemperatureUnit.Kelvin).CurrentValue;
                        iRDataViewModel.HasAlarm = false;
                        dataViewModel.DynamicData = iRDataViewModel;

                        // 添加OxyPlot图例
                        string maxTempLegendName = $"{it.MarkerName}-{I18n.Properties.Resources.MaxTemperature}";
                        TryAddOxyPlotLegend(maxTempLegendName, I18n.Properties.Resources.TemperatureWithSymbol);
                        string minTempLegendName = $"{it.MarkerName}-{I18n.Properties.Resources.MinTemperature}";
                        TryAddOxyPlotLegend(minTempLegendName, I18n.Properties.Resources.TemperatureWithSymbol);
                        string avgTempLegendName = $"{it.MarkerName}-{I18n.Properties.Resources.AvgTemperature}";
                        TryAddOxyPlotLegend(avgTempLegendName, I18n.Properties.Resources.TemperatureWithSymbol);

                        AddOxyPlotPoint(maxTempLegendName, dataViewModel.HappenTime, iRDataViewModel.MaxTemp);
                        AddOxyPlotPoint(avgTempLegendName, dataViewModel.HappenTime, iRDataViewModel.AvgTemp ?? 0);
                        AddOxyPlotPoint(minTempLegendName, dataViewModel.HappenTime, iRDataViewModel.MinTemp);
                        DataList.Add(dataViewModel);
                    });
                }
            }
            OnPropertyChanged(nameof(DataList));
            OnPropertyChanged(nameof(HasRecord));
        }

        private void TryAddOxyPlotLegend(string name, string title)
        {
            if (PlotLegendList.All(it => !string.Equals(it.Name, name)))
            {
                OxyColor color = ChartHelper.GetColor(PlotLegendList.Count);
                PlotLegendViewModel plotLegend = new PlotLegendViewModel
                {
                    Name = name,
                    Color = color,
                    Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(color.R, color.G, color.B)),
                    AxisTitle = title,
                };
                PlotLegendList.Add(plotLegend);
            }
        }

        private void AddOxyPlotPoint(string name, DateTime dateTime, float value)
        {
            PlotPointList.Add(new PlotPointViewModel
            {
                Name = name,
                DateTime = dateTime,
                Value = value,
            });
        }
    }

    public class MarkerLegendViewModel
    {
        public string Name { get; set; }

        public bool IsShow { get; set; }

        public List<DataPointViewModel> DataList { get; set; }
    }

    public class DataPointViewModel
    {
        public DateTime HappenTime { get; set; }
        public float? Max { get; set; }
        public float? Avg { get; set; }
        public float? Min { get; set; }
        public float? CurrentValue { get; set; }
    }

    public class IRDataViewModel
    {
        public IRDataViewModel(int id, string name, float maxTemp, float minTemp, string alarm)
        {
            ID = id;
            Name = name;
            MaxTemp = maxTemp;
            MinTemp = minTemp;
            Alarm = alarm;
        }

        public int ID { get; set; }
        public string Name { get; set; }
        public bool HasAlarm { get; set; } = true;
        public float MaxTemp { get; set; } // ℃
        public float MinTemp { get; set; } // ℃
        public float? AvgTemp { get; set; }
        public string Alarm { get; set; }
    }

    public class ENVDataViewModel
    {
        public ENVDataViewModel(int id, string name, float? temp, float? rh, string alarm)
        {
            ID = id;
            Name = name;
            HasTemp = false;
            HasRH = false;
            HasAlarm = false;
            if (temp != null)
            {
                HasTemp = true;
                Temp = temp.Value;
            }
            if (rh != null)
            {
                HasRH = true;
                RH = rh.Value;
            }
            Alarm = alarm;
        }

        public int ID { get; set; }
        public string Name { get; set; }
        public bool HasTemp { get; set; }
        public bool HasRH { get; set; }
        public bool HasAlarm { get; set; } = true;
        public float Temp { get; set; } // ℃
        public float RH { get; set; } // %
        public string Alarm { get; set; }
    }

    public class VIBRDataViewModel
    {
        public VIBRDataViewModel(int id, string name)
        {
            ID = id;
            Name = name;
        }

        public int ID { get; set; }
        public string Name { get; set; }
        public float? V { get; set; } // m/s
        public float? A { get; set; } // m/s²
        public float? Temp { get; set; } // ℃
        public string VAlarm { get; set; }
        public string AAlarm { get; set; }
        public string TempAlarm { get; set; }
        public string VDesc { get; set; }
        public string ADesc { get; set; }
        public string TempDesc { get; set; }
    }

    public class VIBRSingleDataViewModel
    {
        public VIBRSingleDataViewModel(int id, string name, float value, string alarm, string desc)
        {
            ID = id;
            Name = name;
            Value = value;
            Alarm = alarm;
            Desc = desc;
        }

        public int ID { get; set; }
        public string Name { get; set; }
        public bool HasAlarm { get; set; } = true;
        public float Value { get; set; }
        public string Alarm { get; set; }
        public string Desc { get; set; }
        public string AlarmLevelDesc { get; set; }
        public string LegendNameWithSymbol { get; set; }
    }

    public class NoiseDataViewModel
    {
        public NoiseDataViewModel(int id, string name, float spl, string desc, string alarm)
        {
            ID = id;
            Name = name;
            Spl = spl;
            Desc = desc;
            Alarm = alarm;
        }

        public int ID { get; set; }
        public string Name { get; set; }
        public bool HasAlarm { get; set; } = true;
        public float Spl { get; set; } // dB
        public string Desc { get; set; }
        public string Alarm { get; set; }
    }

    public class TcDataViewModel
    {
        public TcDataViewModel(int id, string name, float temp, string alarm, string desc)
        {
            ID = id;
            Name = name;
            Temp = temp;
            Desc = desc;
            Alarm = alarm;
        }

        public int ID { get; set; }
        public string Name { get; set; }
        public bool HasAlarm { get; set; } = true;
        public float Temp { get; set; }
        public string Desc { get; set; }
        public string Alarm { get; set; }
    }

    public class FileDataViewModel
    {
        public FileDataViewModel(int id, string name, string url, int channelID, FileType fileType)
        {
            ID = id;
            Name = name;
            Url = url;
            ChannelID = channelID;
            FileType = fileType;
        }

        public int ID { get; set; }
        public string Name { get; set; }
        public string Url { get; set; }

        public int ChannelID { get; set; }
        public FileType FileType { get; set; }
    }

    public class DataViewModel : BaseViewModel
    {
        public int ID { get; set; }

        public DateTime HappenTime { get; set; }

        public string Name { get; set; }

        public object DynamicData { get; set; }

        public List<DataImagePath> ImagePaths { get; set; }

        public string DataTitle { get; set; }

        public bool HasImages => ImagePaths != null && ImagePaths.Count > 0;

        public DataViewModel(DeviceDataDO deviceDataDO, bool useChannelName)
        {
            ID = deviceDataDO.ID;
            HappenTime = deviceDataDO.HappenTime.ToLocalTime();
            if (useChannelName)
            {
                Name = deviceDataDO.ChannelName;
                DataTitle = I18n.Properties.Resources.ChannelName;
            }
            else
            {
                if (string.IsNullOrEmpty(deviceDataDO.PresetName))
                {
                    Name = $"{I18n.Properties.Resources.Preset}{deviceDataDO.PresetID}";
                }
                else
                {
                    Name = deviceDataDO.PresetName;
                }
                DataTitle = I18n.Properties.Resources.PresetName;
            }
            ImagePaths = deviceDataDO?.ImageInfos?.Select(it => new DataImagePath(it)).ToList();
            OnPropertyChanged(nameof(HasImages));
        }

        public List<ImageInfo> ToImageInfos()
        {
            List<ImageInfo> imageInfos = new List<ImageInfo>();

            if (ImagePaths != null)
            {
                foreach (DataImagePath dataImagePath in ImagePaths)
                {
                    imageInfos.Add(new ImageInfo(dataImagePath.ChannelID, dataImagePath.URL, dataImagePath.FileType));
                }
            }
            return imageInfos;
        }
    }

    public class DataImagePath
    {
        #region constructor

        public DataImagePath()
        {
        }

        public DataImagePath(ImageInfo imageInfo)
        {
            ChannelID = imageInfo.ChannelID;
            URL = imageInfo.URL;
            FileType = imageInfo.FileType;
        }

        #endregion constructor

        #region property

        public int ChannelID { get; set; }

        public string URL { get; set; }

        public FileType FileType { get; set; }

        #endregion property

        #region method

        public ImageInfo ToImageInfo()
        {
            return new ImageInfo(ChannelID, URL, FileType);
        }

        #endregion method
    }
}