// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Linq;
using System.Windows.Data;
using System.Windows.Media;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Media.Animation;

using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using Newtonsoft.Json;

using Firefly.Client.Service;
using Temperature = Amur.Common.Temperature;
using TemperatureUnit = Amur.Common.TemperatureUnit;
using BaseViewModel = Firefly.Client.Control.BaseViewModel;

namespace Firefly.Client
{
    public class StatusMonitorViewModel : BaseViewModel
    {
        private string _oldSelectedDevice;

        private string _firstVideoRTSPUrl;
        private string _lastVideoRTSPUrl;

        private object _itemsLockObj = new object();

        private PlotModel _plotDataModel;
        private PlotController _mouseHoverController;

        public bool HasPointPlot => _plotDataModel != null;

        public PlotModel PlotDataModel
        {
            get => _plotDataModel;
            set
            {
                _plotDataModel = value;
                OnPropertyChanged(nameof(HasPointPlot));
                OnPropertyChanged(nameof(PlotDataModel));
            }
        }

        public PlotController MouseHoverController
        {
            get => _mouseHoverController;
            set
            {
                _mouseHoverController = value;
                OnPropertyChanged(nameof(MouseHoverController));
            }
        }

        public ObservableCollection<DeviceItemViewModel> DeviceItems { get; private set; }

        public DeviceItemViewModel SelectedDevice { get; set; }

        public string FirstVideoRTSPUrl
        {
            get => _firstVideoRTSPUrl;
            set
            {
                _firstVideoRTSPUrl = value;
                OnPropertyChanged(nameof(FirstVideoRTSPUrl));
            }
        }

        public string LastVideoRTSPUrl
        {
            get => _lastVideoRTSPUrl;
            set
            {
                _lastVideoRTSPUrl = value;
                OnPropertyChanged(nameof(LastVideoRTSPUrl));
            }
        }

        public int TotalDeviceCount => DeviceItems.Count;

        public int TotalOnlineDevices => DeviceItems.Count(it => it.IsOnline);

        public int TotalAlarmDeviceCount => DeviceItems.Count(item => item.NeedAlarm);

        public int TotalUnconfirmedAlarmCount { get; private set; }

        public StatusMonitorViewModel()
        {
            DeviceItems = new ObservableCollection<DeviceItemViewModel>();

            BindingOperations.EnableCollectionSynchronization(DeviceItems, _itemsLockObj);
            ICollectionView collectionView = CollectionViewSource.GetDefaultView(DeviceItems);
            collectionView.GroupDescriptions.Add(new PropertyGroupDescription("HeaderTitle"));

            MouseHoverController = new PlotController();
            MouseHoverController.BindMouseEnter(PlotCommands.HoverPointsOnlyTrack);
        }

        public async Task Load(List<DeviceDO> deviceDOs)
        {
            DeviceItems.Clear();

            await Task.Delay(20);

            deviceDOs?.ForEach(it =>
            {
                DeviceItemViewModel deviceItemViewModel = new DeviceItemViewModel
                {
                    DeviceDO = it,
                };
                it.DeviceAddress.ForEach(channel =>
                {
                    string type;
                    if (channel.Type == DeviceType.IRCamera.ToString())
                    {
                        type = I18n.Properties.Resources.IRCamera;
                    }
                    else if (channel.Type == DeviceType.DCCamera.ToString())
                    {
                        type = I18n.Properties.Resources.DCCamera;
                    }
                    else
                    {
                        type = channel.Type;
                    }

                    ChannelInfoViewModel channelViewModel = new ChannelInfoViewModel()
                    {
                        ID = channel.ChannelID,
                        Type = type,
                        ProtocolType = channel.ProtocolType
                    };
                    deviceItemViewModel.ChannelInfoList.Add(channelViewModel);
                });

                DeviceItems.Add(deviceItemViewModel);
            });

            if (_oldSelectedDevice == null)
            {
                FirstVideoRTSPUrl = string.Empty;
                LastVideoRTSPUrl = string.Empty;
                SelectedDevice = null;
            }
        }

        public void UpdateDeviceStatus(List<DeviceStatusDataObject> allDeviceStatusList)
        {
            lock (_itemsLockObj)
            {
                foreach (DeviceItemViewModel deviceItem in DeviceItems)
                {
                    deviceItem.UpdateStates(allDeviceStatusList.FirstOrDefault(it => string.Equals(it.ID, deviceItem.DeviceID)));
                }
            }

            FlushUI();
        }

        public void UpdateDeviceAlarm(int unconfirmedAlarm)
        {
            lock (_itemsLockObj)
            {
                TotalUnconfirmedAlarmCount = unconfirmedAlarm;

                foreach (DeviceItemViewModel deviceItemVM in DeviceItems)
                {
                    deviceItemVM.UpdateUIStatus();
                }
            }

            FlushUI();
        }

        public void UpdateDeviceFromSampleData(List<EventMessageBO> dataEventMessageList)
        {
            if (DeviceItems == null)
            {
                return;
            }
            lock (_itemsLockObj)
            {
                foreach (DeviceItemViewModel deviceItemVM in DeviceItems)
                {
                    List<EventMessageBO> deviceEventMessageBOList = dataEventMessageList?.Where(it => string.Equals(it.DeviceID, deviceItemVM.DeviceID)).ToList();
                    if (deviceEventMessageBOList == null || deviceEventMessageBOList.Count <= 0)
                    {
                        continue;
                    }

                    foreach (EventMessageBO eventMessageBO in deviceEventMessageBOList)
                    {
                        switch (eventMessageBO.EventType)
                        {
                            case EventType.Fire_AlarmEvent:
                            case EventType.Fire_CruiseStatusEvent:
                            case EventType.Fire_CruisePositionEvent:
                            case EventType.Fire_AlarmInputEvent:
                            case EventType.Fire_DeviceAlarmIOEvent:
                            case EventType.Equipment_AlarmEvent:
                            case EventType.Equipment_CruiseStatusEvent:
                            case EventType.Equipment_CruisePositionEvent:
                            case EventType.Equipment_DefectEvent:
                            case EventType.Equipment_AlarmInputEvent:
                            case EventType.Equipment_DeviceAlarmIOEvent:
                            case EventType.PreviewException:
                            case EventType.PreviewReconnected:
                            case EventType.Environment_AlarmEvent:
                                break;

                            case EventType.Fire_DataEvent:
                            case EventType.Equipment_DataEvent:
                                SampleOutputModel sampleOutputModel = JsonConvert.DeserializeObject<SampleOutputModel>(eventMessageBO.Event);
                                if (sampleOutputModel?.MarkerList != null && sampleOutputModel.MarkerList.Count > 0)
                                {
                                    deviceItemVM.UpdateSampleMessage(sampleOutputModel).UpdateSampleData(sampleOutputModel).UpdateChart();
                                    deviceItemVM.IsOnline = true;
                                }

                                break;

                            case EventType.Environment_DataEvent:
                                List<EnvironmentDataOutputModel> environmentDataList = JsonConvert.DeserializeObject<List<EnvironmentDataOutputModel>>(eventMessageBO.Event);
                                if (environmentDataList != null && environmentDataList.Count > 0)
                                {
                                    deviceItemVM.UpdateSampleMessage(environmentDataList).UpdateSampleData(environmentDataList).UpdateChart();
                                    deviceItemVM.IsOnline = true;
                                }

                                break;

                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }
                }
            }

            if (SelectedDevice != null)
            {
                DeviceItemViewModel temp = new DeviceItemViewModel
                {
                    SampleDataList = SelectedDevice.SampleDataList
                }.UpdateChart();
                PlotDataModel = temp?.PlotDataModel;
            }
        }

        public void PauseLoadPlotData()
        {
            _oldSelectedDevice = SelectedDevice?.DeviceDO.ID;
            SelectedDevice = null;
        }

        public void ResumeLoadPlotData()
        {
            SelectedDevice = DeviceItems.FirstOrDefault(x => x.DeviceDO.ID == _oldSelectedDevice);
            if (SelectedDevice != null)
            {
                DeviceItemViewModel temp = new DeviceItemViewModel
                {
                    SampleDataList = SelectedDevice.SampleDataList
                }.UpdateChart();
                PlotDataModel = temp?.PlotDataModel;
            }
            _oldSelectedDevice = null;
        }

        public void NotifyAlarm(AlarmDO alarmDO)
        {
            if (alarmDO != null && !alarmDO.IsConfirmed)
            {
                DeviceItemViewModel deviceItemViewModel = DeviceItems?.FirstOrDefault(it => string.Equals(it.DeviceID, alarmDO.DeviceID));
                if (deviceItemViewModel != null)
                {
                    lock (_itemsLockObj)
                    {
                        deviceItemViewModel.NeedAlarm = true;
                    }
                }
            }
        }

        public void ChangedDevice(DeviceStreamDO streamDO)
        {
            if (streamDO != null)
            {
                FirstVideoRTSPUrl = streamDO.StreamItems.FirstOrDefault(x => x.ChannelID == 0)?.RTSPUrl;
                LastVideoRTSPUrl = streamDO.StreamItems.FirstOrDefault(x => x.ChannelID == 1)?.RTSPUrl;
            }
            else
            {
                FirstVideoRTSPUrl = string.Empty;
                LastVideoRTSPUrl = string.Empty;
            }

            DeviceItemViewModel temp = new DeviceItemViewModel
            {
                SampleDataList = SelectedDevice.SampleDataList
            }.UpdateChart();
            PlotDataModel = temp?.PlotDataModel;
        }

        private void FlushUI()
        {
            OnPropertyChanged(nameof(TotalDeviceCount));
            OnPropertyChanged(nameof(TotalOnlineDevices));
            OnPropertyChanged(nameof(TotalAlarmDeviceCount));
            OnPropertyChanged(nameof(TotalUnconfirmedAlarmCount));
        }
    }

    public static class DeviceItemViewModelExpress
    {
        public static DeviceItemViewModel UpdateSampleMessage(this DeviceItemViewModel deviceItemVM, List<EnvironmentDataOutputModel> environmentDataList)
        {
            if (deviceItemVM == null)
            {
                return deviceItemVM;
            }
            string message = string.Empty;
            foreach (EnvironmentDataOutputModel environmentDataOM in environmentDataList)
            {
                message += $"{environmentDataOM.ChannelName}:{environmentDataOM.CurrentValue}{environmentDataOM.Unit}\n";
            }
            message = message.TrimEnd('\n');

            deviceItemVM.Message = message;
            deviceItemVM.MeasurementTime = environmentDataList.FirstOrDefault()?.HappenTime.ToLocalTime() ?? DateTime.Now;

            deviceItemVM.ChannelInfoList.ForEach(x =>
            {
                EnvironmentDataOutputModel channelData = environmentDataList.FirstOrDefault(data => data.ChannelID == x.ID);
                x.Name = channelData.ChannelName;
                x.Detail = $"{channelData.CurrentValue}{channelData.Unit}";
            });

            return deviceItemVM;
        }

        public static DeviceItemViewModel UpdateSampleData(this DeviceItemViewModel deviceItemVM, List<EnvironmentDataOutputModel> environmentDataList)
        {
            if (deviceItemVM == null)
            {
                return deviceItemVM;
            }
            foreach (EnvironmentDataOutputModel environmentDataOM in environmentDataList)
            {
                List<PlotDataViewModel> sampleDataList = deviceItemVM.SampleDataList.FirstOrDefault(x => x.Any(sub => sub.Title == environmentDataOM.ChannelName));
                if (sampleDataList == null)
                {
                    sampleDataList = new List<PlotDataViewModel>();
                    deviceItemVM.SampleDataList.Add(sampleDataList);
                }
                if (sampleDataList.Count >= DeviceItemViewModel.DATA_COUNT)
                {
                    sampleDataList.RemoveAt(0);
                }
                sampleDataList.Add(new PlotDataViewModel
                {
                    Title = environmentDataOM.ChannelName,
                    GroupName = string.Empty,
                    HappenTime = environmentDataOM.HappenTime,
                    Value = environmentDataOM.CurrentValue,
                    Unit = environmentDataOM.Unit
                });
            }

            return deviceItemVM;
        }

        public static DeviceItemViewModel UpdateSampleMessage(this DeviceItemViewModel deviceItemVM, SampleOutputModel sampleOutputModel)
        {
            if (deviceItemVM == null)
            {
                return deviceItemVM;
            }
            string presetName = $"{(string.IsNullOrWhiteSpace(sampleOutputModel.PresetName) ? I18n.Properties.Resources.Preset + sampleOutputModel.PresetID : sampleOutputModel.PresetName)}\n";
            string markerData = string.Empty;
            foreach (CommonMarkerOutputModel markerOutputModel in sampleOutputModel.MarkerList)
            {
                Temperature temperature = new Temperature(markerOutputModel.MaxTemp, TemperatureUnit.Kelvin);
                markerData += $"{markerOutputModel.MarkerName}:{temperature.ToString()}\n";
            }
            markerData = markerData.TrimEnd('\n');

            deviceItemVM.Message = markerData;
            deviceItemVM.MeasurementTime = sampleOutputModel.HappenTime.ToLocalTime();
            deviceItemVM.ChannelInfoList.ForEach(x =>
            {
                x.ImageUrl = sampleOutputModel.ImageInfo.FirstOrDefault(image => image.ChannelID == x.ID)?.URL;
                if (x.ID == 0)
                {
                    //采样数据未按通道分层，当前数据只有热像，只取通道0
                    x.Name = sampleOutputModel.DeviceName + presetName;
                    x.Detail = markerData;
                }
                else
                {
                    x.Name = sampleOutputModel.DeviceName + presetName;
                    x.Detail = I18n.Properties.Resources.NoData;
                }
            });

            return deviceItemVM;
        }

        public static DeviceItemViewModel UpdateSampleData(this DeviceItemViewModel deviceItemVM, SampleOutputModel sampleOutputModel)
        {
            if (deviceItemVM == null)
            {
                return deviceItemVM;
            }

            deviceItemVM.BuildSampleData(sampleOutputModel);

            return deviceItemVM;
        }

        public static DeviceItemViewModel UpdateStates(this DeviceItemViewModel deviceItemVM, DeviceStatusDataObject deviceStatusDO)
        {
            if (deviceItemVM == null || deviceStatusDO == null)
            {
                return deviceItemVM;
            }
            deviceItemVM.IsOnline = deviceStatusDO.Status == DeviceStatus.Online;

            if (deviceItemVM.AlarmOutStatus != null)
            {
                deviceItemVM.AlarmOutStatus.Clear();
                deviceStatusDO.AlarmOut?.ForEach(x =>
                {
                    deviceItemVM.AlarmOutStatus.Add(x);
                });
                if (!deviceItemVM.AlarmOutStatus.Any())
                {
                    deviceItemVM.AlarmOutStatus.Add(AlarmStatus.Normal);
                }
            }

            return deviceItemVM;
        }

        public static DeviceItemViewModel UpdateUIStatus(this DeviceItemViewModel deviceItemVM)
        {
            if (deviceItemVM == null)
            {
                return deviceItemVM;
            }

            if (deviceItemVM.CurrentAlarmTick > 6)
            {
                deviceItemVM.NeedAlarm = false;
                deviceItemVM.CurrentAlarmTick = 0;
            }
            if (deviceItemVM.NeedAlarm)
            {
                deviceItemVM.CurrentAlarmTick++;
            }

            return deviceItemVM;
        }

        public static DeviceItemViewModel UpdateChart(this DeviceItemViewModel deviceItemVM)
        {
            if (deviceItemVM == null || deviceItemVM.SampleDataList.Count <= 0)
            {
                return null;
            }

            PlotModel plotDataModel = ChartHelper.InitChartModel();
            plotDataModel.Series.Clear();
            int index = 0;
            foreach (List<PlotDataViewModel> dataPair in deviceItemVM.SampleDataList)
            {
                PlotDataViewModel dataItem = dataPair.FirstOrDefault();
                if (dataItem == null)
                {
                    continue;
                }
                LineSeries lineSeries = new LineSeries().SetSeriesStyle($"{dataItem.Title}({dataItem.Unit})", dataItem.GroupName, ChartHelper.GetColor(index++));
                foreach (PlotDataViewModel plotDataVM in dataPair)
                {
                    lineSeries.Points.Add(DateTimeAxis.CreateDataPoint(plotDataVM.HappenTime.ToLocalTime(), Math.Round(plotDataVM.Value, 1)));
                }
                plotDataModel.Series.Add(lineSeries);
            }

            deviceItemVM.PlotDataModel = plotDataModel;
            if (deviceItemVM.PlotDataModel?.PlotView != null)
            {
                //DeviceItemViewModel still used with last window.
                deviceItemVM.PlotDataModel = null;
                return null;
            }
            return deviceItemVM;
        }
    }

    public class DeviceItemViewModel : BaseViewModel
    {
        private bool _isOnline;
        private bool _isAlarmed;
        private int _currentAlarmTick;
        private string _message;
        private DateTime? _measurementTime;
        private PlotModel _plotDataModel;
        private PlotController _mouseHoverController;
        private List<List<PlotDataViewModel>> _cachedSampleDataList;
        public const int DATA_COUNT = 10;

        public bool HasPointPlot => _plotDataModel != null;

        public PlotModel PlotDataModel
        {
            get => _plotDataModel;
            set
            {
                _plotDataModel = value;
                OnPropertyChanged(nameof(HasPointPlot));
                OnPropertyChanged(nameof(PlotDataModel));
            }
        }

        public PlotController MouseHoverController
        {
            get => _mouseHoverController;
            set
            {
                _mouseHoverController = value;
                OnPropertyChanged(nameof(MouseHoverController));
            }
        }

        public DeviceItemViewModel()
        {
            SampleDataList = new List<List<PlotDataViewModel>>();
            AlarmOutStatus = new List<AlarmStatus>();
            ChannelInfoList = new List<ChannelInfoViewModel>();

            MouseHoverController = new PlotController();
            MouseHoverController.BindMouseEnter(PlotCommands.HoverPointsOnlyTrack);

            _cachedSampleDataList = new List<List<PlotDataViewModel>>();
        }

        public DeviceDO DeviceDO { get; set; }

        public DateTime? MeasurementTime
        {
            get => _measurementTime;
            set
            {
                _measurementTime = value;
                OnPropertyChanged(nameof(MeasurementTime));
            }
        }

        public bool NeedAlarm { get; set; }

        public int CurrentAlarmTick
        {
            get => _currentAlarmTick;
            set
            {
                _currentAlarmTick = value;
                IsAlarmed = _currentAlarmTick % 2 == 1;
            }
        }

        public string HeaderTitle => DeviceDO.Name;

        public string DeviceID => DeviceDO.ID;

        public string DeviceName => DeviceDO.Name;

        public string ServerName => DeviceDO?.DefaultAgent()?.Name ?? string.Empty;

        public bool IsOnline
        {
            get => _isOnline;
            set
            {
                _isOnline = value;
                OnPropertyChanged();
            }
        }

        public bool IsAlarmed
        {
            get => _isAlarmed;
            set
            {
                _isAlarmed = value;
                OnPropertyChanged();
            }
        }

        public string Message
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_message))
                {
                    return I18n.Properties.Resources.NoData;
                }
                return _message;
            }
            set
            {
                _message = value;
                OnPropertyChanged();
            }
        }

        public List<List<PlotDataViewModel>> SampleDataList { get; set; }

        public List<AlarmStatus> AlarmOutStatus { get; set; }

        public List<ChannelInfoViewModel> ChannelInfoList { get; set; }

        public void BuildSampleData(SampleOutputModel sampleOutputModel)
        {
            SampleDataList.Clear();
            foreach (CommonMarkerOutputModel markerOutputModel in sampleOutputModel.MarkerList)
            {
                string presetName = $"{(string.IsNullOrWhiteSpace(sampleOutputModel.PresetName) ? I18n.Properties.Resources.Preset + sampleOutputModel.PresetID : sampleOutputModel.PresetName)}";
                List<PlotDataViewModel> sampleDataList = _cachedSampleDataList.FirstOrDefault(x => x.Any(sub => sub.Title == markerOutputModel.MarkerName && sub.GroupName == presetName));
                if (sampleDataList == null)
                {
                    sampleDataList = new List<PlotDataViewModel>();
                    _cachedSampleDataList.Add(sampleDataList);
                }
                Temperature temperature = new Temperature(markerOutputModel.MaxTemp, TemperatureUnit.Kelvin);
                if (sampleDataList.Count >= DATA_COUNT)
                {
                    sampleDataList.RemoveAt(0);
                }
                sampleDataList.Add(new PlotDataViewModel
                {
                    Title = markerOutputModel.MarkerName,
                    GroupName = presetName,
                    HappenTime = sampleOutputModel.HappenTime,
                    Value = temperature.CurrentValue,
                    Unit = I18n.Properties.Resources.TemperatureWithSymbol
                });

                SampleDataList.Add(sampleDataList);
            }
        }
    }

    public class ChannelInfoViewModel : BaseViewModel
    {
        private string _detail;
        private string _imageURL;

        public int ID { get; set; }

        public string Name { get; set; }

        public string ProtocolType { get; set; }

        public string Type { get; set; }

        public string Detail
        {
            get => _detail;
            set
            {
                _detail = value;
                OnPropertyChanged(nameof(Detail));
            }
        }

        public string ImageUrl
        {
            get => _imageURL;
            set
            {
                _imageURL = value;
                OnPropertyChanged(nameof(ImageUrl));
                OnPropertyChanged(nameof(IsEnable));
            }
        }

        public bool IsEnable => !string.IsNullOrWhiteSpace(ImageUrl);
    }

    public class PlotDataViewModel
    {
        public string Title { get; set; }
        public string GroupName { get; set; }
        public DateTime HappenTime { get; set; }
        public string Unit { get; set; }
        public float Value { get; set; }
    }
}