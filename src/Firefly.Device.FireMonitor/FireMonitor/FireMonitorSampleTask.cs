// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Linq;
using System.Threading;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;

using Microsoft.Extensions.Logging;

using OWB.DataFrame;
using Firefly.LogFactory;

namespace Firefly.Device.FireMonitor
{
    public class FireMonitorSampleTask : SampleTask
    {
        private FireMonitorDeviceEngine _deviceEngine;
        private ILogger<FireMonitorSampleTask> _logger = DeviceLogFactory.GetLogger<FireMonitorSampleTask>();

        internal SampleDataBO SampleDataBO { get; private set; }

        internal FireMonitorSampleTask(DeviceEngine deviceEngine)
        {
            Owner = deviceEngine;
            _deviceEngine = Owner as FireMonitorDeviceEngine;
        }

        public override void Start()
        {
            _logger.LogInformation($"StartSampleTask,{_deviceEngine.DeviceBO},Start SampleTask");
            //配置时间跨度大于采样时间，进入线程时采样一次
            _deviceEngine.SampleTime = DateTime.MinValue;
            InternalCancellationTokenSource = new System.Threading.CancellationTokenSource();
            Task.Factory.StartNew(() => DoTask(InternalCancellationTokenSource.Token));
        }

        public override void Stop()
        {
            InternalCancellationTokenSource?.Cancel();
            InternalCancellationTokenSource = null;
        }

        private async Task DoTask(CancellationToken cancellationToken)
        {
            float sampleInterval = _deviceEngine.DeviceConfigBO?.GeneralConfigBO?.SampleInterval ?? 0;
            if (sampleInterval <= 0)
            {
                sampleInterval = 60;
            }

            int latestPresetID = Constants.GOING_PRESET;
            int dataSampleTimes = 0;
            int lastDataSampleTimes = 0;

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    // 触发监控点采集数据：
                    // 1: 进入一个新的预置位并且当前预置位有效
                    // 2: 等待时间大于采集时间
                    int currentPresetID = _deviceEngine.CurrentPresetID;
                    bool currentPresetEnabled = _deviceEngine.CurrentPresetEnabled;
                    bool isNewPreset = latestPresetID != currentPresetID && currentPresetEnabled;
                    bool overTime = (DateTime.UtcNow - _deviceEngine.SampleTime).TotalMilliseconds >= sampleInterval * 1000;
                    bool needSample = isNewPreset || overTime;

                    FireMonitorPresetBO presetBO = _deviceEngine.DeviceConfigBO?.PresetConfigBO?.PresetList
                        ?.Find(a => a.ID == currentPresetID);

                    if (needSample)
                    {
                        long timestamp = DateTime.UtcNow.Ticks;

                        FrameBO frameBO = await _deviceEngine.GetFrame();

                        if (frameBO.IsSuccess)
                        {
                            List<OWBMarker> markers = new List<OWBMarker>();

                            if (presetBO != null)
                            {
                                List<OWBMarker> rawMarkers = presetBO.GetDeviceMarker();
                                foreach (OWBMarker rawMarker in rawMarkers)
                                {
                                    if (frameBO.IRFrame.GetTemperature(rawMarker, out OWBMarkerTemperaturePoint tempPoint))
                                    {
                                        rawMarker.Value = tempPoint;
                                        markers.Add(rawMarker);
                                    }
                                    else
                                    {
                                        _logger.LogError($"DoSampleTask,{_deviceEngine.DeviceBO}, Get Market Temp Fail, PresetID:{presetBO.ID}, Marker:{JsonSerializer.Serialize(rawMarker)}");
                                    }
                                }
                            }
                            else
                            {
                                _logger.LogError($"DoSampleTask,{_deviceEngine.DeviceBO}, PresetBO is null,");
                            }
                            string presetInfo = BuildPresetInfo(presetBO, _deviceEngine.CurrentCruiseAction);
                            DoRefreshOSDInfo(frameBO, markers, presetInfo);

                            SampleDataBO = new SampleDataBO(frameBO.IRFrame, frameBO.DCFrame, markers, timestamp);

                            _deviceEngine.SampleTime = DateTime.UtcNow;
                        }
                        else
                        {
                            SampleDataBO = null;
                        }

                        dataSampleTimes++;
                    }

                    // 获取到新的一帧数据需要判断是否进行数据采集和照片保存
                    // 当前预置位有效继续进行照片和数据保存的判断
                    if (lastDataSampleTimes != dataSampleTimes && currentPresetEnabled)
                    {
                        bool canRecordData = CanRecordData(dataSampleTimes);
                        bool canRecordImage = CanRecordImage(dataSampleTimes);

                        if ((canRecordData || canRecordImage || needSample) && SampleDataBO != null)
                        {
                            DoDataRecord(SampleDataBO.Timestamp, SampleDataBO.IRFrame, SampleDataBO.DCFrame, SampleDataBO.Markers, presetBO, canRecordData, canRecordImage, needSample);
                        }

                        lastDataSampleTimes = dataSampleTimes;
                    }

                    // InstantTask removed — no per-preset InstantCruise recording.

                    latestPresetID = currentPresetID;
                }
                catch (Exception e)
                {
                    _logger.LogError(e.ToString());
                }
                finally
                {
                    await Task.Delay(Constants.WAIT_TIMEOUT_500);
                }
            }
        }

        private void DoRefreshOSDInfo(FrameBO frameBO, List<OWBMarker> markers, string presetName)
        {
            _deviceEngine.OSDInfoBO.RefreshFrame(frameBO.IRFrame, frameBO.DCFrame, markers, presetName);
        }

        private void DoDataRecord(long timestamp, OWBIRFrame irFrame, OWBDCFrame dcFrame, List<OWBMarker> markers, FireMonitorPresetBO presetBO, bool dataRecordEnabled, bool imageRecordEnabled, bool needSample)
        {
            DateTime happenTime = DateTime.UtcNow;
            int presetID = Constants.GOING_PRESET;
            string presetName = string.Empty;

            List<FireMarkerBO> fireMarkerBOs = new List<FireMarkerBO>();
            List<OWBMarker> frameMarkers = new List<OWBMarker>();

            if (presetBO != null)
            {
                presetID = presetBO.ID;
                presetName = presetBO.Name;
                fireMarkerBOs = GetMarkerData(presetBO.MarkerList, markers);
                frameMarkers = markers;
            }

            string irSampleFileName = string.Empty;
            string dcSampleFileName = string.Empty;
            List<ImageInfoBO> imageInfoBOList = new List<ImageInfoBO>();

            if (imageRecordEnabled)
            {
                if (irFrame != null)
                {
                    SaveFileBO irSaveFileBO = new SaveFileBO(SaveFileType.Sample, _deviceEngine.DeviceBO.ID, _deviceEngine.DeviceBO.Name, presetID, 0, ChannelType.IRCamera, happenTime.ToLocalTime(), irFrame, null, _deviceEngine.IRExpectedImageWidth, _deviceEngine.IRExpectedImageHeight, frameMarkers);

                    irSampleFileName = DeviceConfigBO.SaveFile(irSaveFileBO);
                    imageInfoBOList.Add(new ImageInfoBO(0, ChannelType.IRCamera, irFrame.FileType, irSampleFileName, DeviceConfigBO.SampleRootPath));
                }

                if (dcFrame != null)
                {
                    SaveFileBO dcSaveFileBO = new SaveFileBO(SaveFileType.Sample, _deviceEngine.DeviceBO.ID, _deviceEngine.DeviceBO.Name, presetID, 1, ChannelType.DCCamera, happenTime.ToLocalTime(), null, dcFrame, _deviceEngine.ExpectedImageWidth, _deviceEngine.ExpectedImageHeight, frameMarkers);

                    dcSampleFileName = DeviceConfigBO.SaveFile(dcSaveFileBO);
                    imageInfoBOList.Add(new ImageInfoBO(1, ChannelType.DCCamera, dcFrame.FileType, dcSampleFileName, DeviceConfigBO.SampleRootPath));
                }
            }

            FireDataBO fireDataBO = new FireDataBO(
                 happenTime,
                 timestamp,
                 _deviceEngine.DeviceBO.ID,
                 _deviceEngine.DeviceBO.Name,
                 _deviceEngine.DeviceBO.ProtocolType,
                 presetID,
                 presetName,
                 fireMarkerBOs,
                 DeviceConfigBO.SampleRootPath,
                 imageInfoBOList,
                 (_deviceEngine.DeviceConfigBO?.GeneralConfigBO?.SampleInterval ?? 60) * 1000,
                 dataRecordEnabled,
                 imageRecordEnabled);

            if (needSample)
            {
                _deviceEngine.OnDataEvent?.Invoke(this, new DeviceDataArgs(_deviceEngine.DeviceBO.ID, DataEventType.Sample, fireDataBO));
            }
        }

        private List<FireMarkerBO> GetMarkerData(List<FireMonitorMarkerBO> markerBOs, List<OWBMarker> markers)
        {
            List<FireMarkerBO> fireMarkerBOs = new List<FireMarkerBO>();
            foreach (FireMonitorMarkerBO markerBO in markerBOs)
            {
                FireMarkerBO fireMarker = new FireMarkerBO();

                foreach (OWBMarker marker in markers)
                {
                    if (marker.Name.Equals(markerBO.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        fireMarker.MarkerName = markerBO.Name;
                        fireMarker.MarkerType = (MarkerType)marker.MarkerType;
                        fireMarker.PointList = markerBO.PointList;
                        fireMarker.MaxTemp = marker.Value.Max.Value;
                        fireMarker.MinTemp = marker.Value.Min.Value;
                        fireMarker.AvgTemp = marker.Value.Avg;

                        fireMarkerBOs.Add(fireMarker);
                        break;
                    }
                }
            }

            return fireMarkerBOs;
        }

        private bool CanRecordData(int dataSampleTimes)
        {
            FireMonitorGeneralConfigBO general = _deviceEngine.DeviceConfigBO?.GeneralConfigBO;
            if (general == null || !general.DataRecordEnabled || dataSampleTimes == 0 || general.DataRecordValue == 0)
            {
                return false;
            }

            return dataSampleTimes % general.DataRecordValue == 0;
        }

        private bool CanRecordImage(int dataSampleTimes)
        {
            FireMonitorGeneralConfigBO general = _deviceEngine.DeviceConfigBO?.GeneralConfigBO;
            if (general == null || !general.ImageRecordEnabled || dataSampleTimes == 0 || general.ImageRecordValue == 0)
            {
                return false;
            }

            return dataSampleTimes % general.ImageRecordValue == 0;
        }

        private string BuildPresetInfo(FireMonitorPresetBO presetBO, CruiseAction currentCruiseAction)
        {
            string presetName = string.Empty;
            if (presetBO != null)
            {
                presetName = presetBO.Name;
            }

            string taskName = string.Empty;

            string presetInfo = string.Empty;
            if (!string.IsNullOrWhiteSpace(taskName) && !string.IsNullOrWhiteSpace(presetName))
            {
                presetInfo = string.Format(I18n.Properties.Resources.PresetInfo, $"{taskName}-{presetName}");
            }
            else if (!string.IsNullOrWhiteSpace(taskName) && string.IsNullOrWhiteSpace(presetName))
            {
                presetInfo = string.Format(I18n.Properties.Resources.PresetInfo, taskName);
            }
            else if (string.IsNullOrWhiteSpace(taskName) && !string.IsNullOrWhiteSpace(presetName))
            {
                presetInfo = presetName;
            }

            return presetInfo;
        }
    }
}