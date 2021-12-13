// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;

using OWB;
using OWB.DCCamera;
using OWB.IRCamera;
using OWB.DataFrame;
using Firefly.Painter;
using Firefly.LogFactory;
using Firefly.Stream.Core;
using Microsoft.Extensions.Logging;

namespace Firefly.Device.FireMonitor
{
    internal class FireMonitorDeviceEngine : DeviceEngine
    {
        //IR Channel
        private OWBIRCameraAdapter _irDeviceAdapter;

        internal readonly int ExpectedImageWidth = 640;
        internal readonly int ExpectedImageHeight = 480;

        private readonly StreamFilter _irStreamPusherPri;
        private readonly StreamFilter _irStreamPusherSub;
        private DevicePainter _devicePainter;
        internal OSDInfoBO OSDInfoBO;

        //DC Channel
        private OWBDCCameraAdapter _dcDeviceAdapter;

        private readonly StreamFilter _dcStreamPusherPri;
        private readonly StreamFilter _dcStreamPusherSub;

        private Task _refreshOSDTask;
        private bool _refreshOSDTaskCanceled;
        private readonly object _deviceOsdSync = new object();

        private ILogger<FireMonitorDeviceEngine> _logger = DeviceLogFactory.GetLogger<FireMonitorDeviceEngine>();

        internal FireMonitorDeviceConfigBO DeviceConfigBO;

        internal bool CurrentPresetEnabled => CurrentPresetID != Constants.GOING_PRESET;
        internal int CurrentPresetID;
        internal int CurrentCruiseID;
        internal string CurrentCruiseName;
        internal FireMonitorAlarmLevel CurrentAlarmLevel; //缺少none状态，只能用于巡航恢复
        internal DateTime SampleTime;
        internal DateTime ArrivalTime;

        internal CruiseAction CurrentCruiseAction => (TimerTask as FireMonitorTimerTask).CurrentCruiseAction;
        
        internal int IRExpectedImageWidth { get; private set; }
        internal int IRExpectedImageHeight { get; private set; }

        internal FireMonitorDeviceEngine()
        {
            _irStreamPusherPri = new StreamFilter();
            _irStreamPusherSub = new StreamFilter();
            _dcStreamPusherPri = new StreamFilter();
            _dcStreamPusherSub = new StreamFilter();

            CurrentPresetID = Constants.GOING_PRESET;
            OSDInfoBO = new OSDInfoBO();
        }

        #region public methods

        public override bool Init(DeviceBO deviceBO, string deviceConfigStr)
        {
            bool initSuccess = true;

            try
            {

                if (DeviceBO == null && DeviceConfigStr == null)
                {
                    _deviceBO = deviceBO;
                    _deviceConfigStr = deviceConfigStr;

                    DeviceConfigBO = JsonSerializer.Deserialize<FireMonitorDeviceConfigBO>(deviceConfigStr, Constants.DEFAULT_SERIALIZER_OPTIONS);

                    SampleTask = new FireMonitorSampleTask(this);
                    AlarmTask = new FireMonitorAlarmTask(this);
                    TimerTask = new FireMonitorTimerTask(this);

                    Start();

                    MediaKitRtspServer.Register(DeviceStreamStartEvent, DeviceStreamStopEvent);
                }
                else
                {
                    // stop tasks before change device config: always keep device config in task is latest
                    if (JsonSerializer.Serialize(DeviceBO, Constants.DEFAULT_SERIALIZER_OPTIONS) != JsonSerializer.Serialize(deviceBO, Constants.DEFAULT_SERIALIZER_OPTIONS))
                    {
                        Stop();

                        _deviceBO = deviceBO;
                        _deviceConfigStr = deviceConfigStr;
                        DeviceConfigBO = JsonSerializer.Deserialize<FireMonitorDeviceConfigBO>(deviceConfigStr, Constants.DEFAULT_SERIALIZER_OPTIONS);

                        Start();
                    }
                    else if (DeviceConfigStr != deviceConfigStr)
                    {
                        StopTask();
                        StopOSDTask();
                        _deviceConfigStr = deviceConfigStr;
                        bool oldOSDConfig = DeviceConfigBO.OSDConfigEnabled;
                        DeviceConfigBO = JsonSerializer.Deserialize<FireMonitorDeviceConfigBO>(deviceConfigStr, Constants.DEFAULT_SERIALIZER_OPTIONS);
                        StartTask();
                        if (DeviceConfigBO.OSDConfigEnabled != oldOSDConfig)
                        {
                            StopPullStream();
                            StartPullStream();
                        }
                        StartOSDTask();
                    }
                }
                FireMonitorGeneralConfigBO generalConfigBO = DeviceConfigBO?.GeneralConfigBO;
                if (generalConfigBO != null)
                {
                    if (generalConfigBO.FixedMonitoring)
                    {
                        Task.Factory.StartNew(() =>
                        {
                            PresetParamBO param = new PresetParamBO
                            {
                                Command = PresetCommand.GOTO,
                                PresetID = 1,
                                Speed = 5,
                                AutoFocus = true
                            };
                            _ = PTZPreset(param);
                        });
                    }

                    if (generalConfigBO.EnableSuperResolution)
                    {
                        IRExpectedImageWidth = ExpectedImageWidth * 2;
                        IRExpectedImageHeight = ExpectedImageHeight * 2;
                    }
                    else
                    {
                        IRExpectedImageWidth = ExpectedImageWidth;
                        IRExpectedImageHeight = ExpectedImageHeight;
                    }
                }
            }
            catch (Exception ex)
            {
                initSuccess = false;
                _logger.LogError(ex.ToString());
            }

            return initSuccess;
        }

        public override void Destory()
        {
            Stop();
        }

        public override async Task<DeviceParamBO> GetParam()
        {
            var param = await _irDeviceAdapter.GetParam();
            DeviceParamBO deviceParamBO = new DeviceParamBO
            {
                //使用IRDevice参数
                HardwareAbilityBO = new HardwareAbilityBO
                {
                    AlarmInPortNum = param.HardwareAbility.AlarmInPortNum,
                    AlarmOutPortNum = param.HardwareAbility.AlarmOutPortNum,
                },
                SoftwareAbilityBO = new SoftwareAbilityBO
                {
                    PTZSupport = param.SoftwareAbility.PTZSupport,
                    MaxSpotNum = param.SoftwareAbility.MaxSpotNum,
                    MaxLineNum = param.SoftwareAbility.MaxLineNum,
                    MaxRegionNum = param.SoftwareAbility.MaxRegionNum,
                    SensitivitySupport = param.SoftwareAbility.SensitivitySupport,
                    ShieldMaskSupport = param.SoftwareAbility.ShieldMaskSupport,
                },
                DeviceInfoBOList = new List<DeviceInfoBO>()
            };

            foreach (DeviceAddressBO deviceAddressModel in DeviceBO.DeviceAddressList)
            {
                DeviceInfoBO deviceModel = new DeviceInfoBO();
                if (string.Equals(deviceAddressModel.Type, Constants.IRCamera, StringComparison.InvariantCultureIgnoreCase))
                {
                    _irDeviceAdapter.GetModel(out OWB.IRCamera.DeviceModel irCameraModel);
                    deviceModel.ChannelID = 0;
                    deviceModel.Model = irCameraModel.Model;
                    deviceModel.SeriesNo = irCameraModel.SeriesNO;
                    deviceModel.FirmwareVersion = irCameraModel.FirmwareVersion;
                    deviceModel.Width = irCameraModel.Width;
                    deviceModel.Height = irCameraModel.Height;
                    deviceModel.StreamWidth = irCameraModel.StreamWidth;
                    deviceModel.StreamHeight = irCameraModel.StreamHeight;
                }
                else if (string.Equals(deviceAddressModel.Type, Constants.DCCamera, StringComparison.InvariantCultureIgnoreCase))
                {
                    _dcDeviceAdapter.GetModel(out OWB.DCCamera.DeviceModel dcCameraModel);
                    deviceModel.ChannelID = 1;
                    deviceModel.Model = dcCameraModel.Model;
                    deviceModel.SeriesNo = dcCameraModel.SeriesNO;
                    deviceModel.FirmwareVersion = dcCameraModel.FirmwareVersion;
                    deviceModel.Width = dcCameraModel.Width;
                    deviceModel.Height = dcCameraModel.Height;
                }

                deviceParamBO.DeviceInfoBOList.Add(deviceModel);
            }

            return deviceParamBO;
        }

        public override async Task<DeviceStatusBO> GetStatus()
        {
            DeviceStatusBO deviceStatusBO = new DeviceStatusBO
            {
                ID = DeviceBO.ID,
                RunningStatus = RunningStatus.Online
            };

            switch (CurrentCruiseAction)
            {
                case CruiseAction.None:
                    deviceStatusBO.CruiseStatus = CruiseStatus.None;
                    break;

                case CruiseAction.AutoCruiseRun:
                    deviceStatusBO.CruiseStatus = CruiseStatus.Auto;
                    break;

                case CruiseAction.ManualCruiseRun:
                    deviceStatusBO.CruiseStatus = CruiseStatus.Manual;
                    break;

                case CruiseAction.InstantCruiseRun:
                    deviceStatusBO.CruiseStatus = CruiseStatus.Instant;
                    break;
            }

            //报警状态使用热像设备的报警状态
            var alarmInStatus = await _irDeviceAdapter.GetStatus(OWB.IRCamera.StatusType.AlarmInStatus);
            var alarmOutStatus = await _irDeviceAdapter.GetStatus(OWB.IRCamera.StatusType.AlarmOutStatus);
            deviceStatusBO.AlarmInStatus = alarmInStatus.AlarmInStatus;
            deviceStatusBO.AlarmOutStatus = alarmOutStatus.AlarmOutStatus;
            deviceStatusBO.AlarmStatus = (int)((FireMonitorAlarmTask)AlarmTask).GetCurrentMaxAlarmLevel();
            foreach (DeviceAddressBO deviceAddressModel in DeviceBO.DeviceAddressList)
            {
                RunningStatus runningStatus = RunningStatus.Online;
                if (string.Equals(deviceAddressModel.Type, Constants.IRCamera, StringComparison.InvariantCultureIgnoreCase))
                {
                    var status = await _irDeviceAdapter.GetStatus(OWB.IRCamera.StatusType.ConnectStatus);
                    runningStatus = status.ConnectStatus ? RunningStatus.Online : RunningStatus.Offline;
                }
                else if (string.Equals(deviceAddressModel.Type, Constants.DCCamera, StringComparison.InvariantCultureIgnoreCase))
                {
                    DCDeviceStatus status = await _dcDeviceAdapter.GetStatus(OWB.DCCamera.StatusType.ConnectStatus);
                    runningStatus = status.ConnectStatus ? RunningStatus.Online : RunningStatus.Offline;
                }

                string statusDesc = runningStatus == RunningStatus.Online ? string.Format(I18n.Properties.Resources.ChannelOnline, deviceAddressModel.ChannelID + 1) : string.Format(I18n.Properties.Resources.ChannelOffline, deviceAddressModel.ChannelID + 1);
                deviceStatusBO.RunningStatus = (deviceStatusBO.RunningStatus == RunningStatus.Online && runningStatus == RunningStatus.Online) ? RunningStatus.Online : RunningStatus.Offline;
                deviceStatusBO.StatusDesc += statusDesc;
            }

            deviceStatusBO.StatusDesc = deviceStatusBO.StatusDesc.Remove(deviceStatusBO.StatusDesc.Length - 1);
            return deviceStatusBO;
        }

        public override async Task<bool> PTZPreset(PresetParamBO presetParamBO)
        {
            bool setPresetResult = false;
            bool leaveGoingPreset = presetParamBO.Command == PresetCommand.GOTO || presetParamBO.Command == PresetCommand.SET;

            PauseCruiseTask(PauseType.InclusivePause);

            if (leaveGoingPreset)
            {
                // tell alarm task need reset predict model
                CurrentPresetID = Constants.GOING_PRESET;
            }

            try
            {
                PTZPresetType ptzPresetType = PTZPresetType.GOTO;
                PresetParam presetParam = PresetParam.Default;
                switch (presetParamBO.Command)
                {
                    case PresetCommand.GOTO:
                        ptzPresetType = PTZPresetType.GOTO;

                        FireMonitorPresetBO presetModel = DeviceConfigBO?.PresetConfigBO?.PresetList?.Find(a => a.ID == presetParamBO.PresetID);

                        if (presetModel != null)
                        {
                            presetParam = new PresetParam(presetParamBO.PresetID, presetModel.Name, presetModel.Sensitivity, GetDeviceShieldMask(presetModel.ShieldMaskList), presetModel.GetDeviceMarker());
                        }
                        break;

                    case PresetCommand.SET:
                        ptzPresetType = PTZPresetType.SET;

                        List<OWBMarker> markerList = null;
                        if (presetParamBO.PresetBO?.MarkerList != null && presetParamBO.PresetBO?.MarkerList.Count > 0)
                        {
                            markerList = presetParamBO.PresetBO.GetDeviceMarker();
                        }

                        List<OWBShieldMask> shieldMaskList = null;
                        if (presetParamBO.PresetBO?.ShieldMaskList != null && presetParamBO.PresetBO?.ShieldMaskList.Count > 0)
                        {
                            shieldMaskList = GetDeviceShieldMask(presetParamBO.PresetBO.ShieldMaskList);
                        }

                        int sensitivity = presetParamBO.PresetBO == null ? 1 : presetParamBO.PresetBO.Sensitivity;

                        presetParam = new PresetParam(presetParamBO.PresetID, presetParamBO.PresetBO?.Name, sensitivity, shieldMaskList, markerList);
                        break;

                    case PresetCommand.CLE:
                        ptzPresetType = PTZPresetType.CLE;
                        break;
                }

                if (presetParam != null && _irDeviceAdapter != null)
                {
                    MarkerOSDConfigBO markerOSDConfigBO = DeviceConfigBO.MarkerOSDConfig;
                    if (Device.Constants.OSDConfigMode == OSDConfigMode.ToDevice)
                    {
                        // ToDevice: always push measurement markers to the camera body.
                        // OSDConfigEnabled only controls character overlay (title/time/channel), not marker write.
                        bool writeMarkersToDevice = true;
                        setPresetResult = await _irDeviceAdapter.PTZPreset(ptzPresetType, presetParamBO.PresetID, presetParamBO.Speed, presetParamBO.AutoFocus, presetParam, writeMarkersToDevice, markerOSDConfigBO?.Status == OSDStatus.Enable, markerOSDConfigBO?.ShowMax ?? false, markerOSDConfigBO?.ShowAvg ?? false, markerOSDConfigBO?.ShowMin ?? false);
                        FireMonitorPresetBO presetForTitle = null;
                        if (presetParamBO.Command == PresetCommand.SET && presetParamBO.PresetBO != null)
                        {
                            presetForTitle = new FireMonitorPresetBO { ID = presetParamBO.PresetID, Name = presetParamBO.PresetBO.Name };
                        }
                        else
                        {
                            presetForTitle = DeviceConfigBO?.PresetConfigBO?.PresetList?.Find(a => a.ID == presetParamBO.PresetID);
                        }
                        OSDInfoBO.RefreshPresetName(BuildPresetInfo(presetForTitle, CurrentCruiseAction));
                        UpdateDeviceOSD();
                    }
                    else
                    {
                        setPresetResult = await _irDeviceAdapter.PTZPreset(ptzPresetType, presetParamBO.PresetID, presetParamBO.Speed, presetParamBO.AutoFocus, presetParam, false, markerOSDConfigBO?.Status == OSDStatus.Enable, markerOSDConfigBO?.ShowMax ?? false, markerOSDConfigBO?.ShowAvg ?? false, markerOSDConfigBO?.ShowMin ?? false);
                        RefreshOSDInfo(presetParamBO.PresetID);
                        await _irDeviceAdapter.SetOSDInfo(false, false, "", 6, 6, false, 6, 44);
                    }
                }
                else if (_irDeviceAdapter == null)
                {
                    _logger.LogWarning($"PTZPreset skipped, IR adapter not ready,{DeviceBO}, PresetID:{presetParamBO.PresetID}");
                }

                if (leaveGoingPreset)
                {
                    await Task.Delay(Constants.WAIT_TIMEOUT_GOING_PRESET);

                    if (presetParamBO.Command == PresetCommand.GOTO)
                    {
                        PauseCruiseTask(PauseType.None);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"PTZPreset failed,{DeviceBO}, PresetID:{presetParamBO.PresetID},{ex}");
            }
            finally
            {
                // Always leave GOING_PRESET so SampleTask can record (FixedMonitoring / cruise).
                if (leaveGoingPreset)
                {
                    CurrentPresetID = presetParamBO.PresetID;
                    ArrivalTime = DateTime.UtcNow;
                }
            }

            return setPresetResult;
        }

        public override async Task<bool> PTZControl(PTZParamBO ptzParamBO)
        {
            PTZControlType ptzControlType = PTZControlType.PTZ_STOP;

            switch (ptzParamBO.Action)
            {
                case PTZControlAction.Start:
                    PauseCruiseTask(PauseType.ExclusivePause);
                    break;

                case PTZControlAction.Stop:
                    PauseCruiseTask(PauseType.InclusivePause);
                    break;
            }

            switch (ptzParamBO.Command)
            {
                case PTZCommand.FOCUS_NEAR:
                    ptzControlType = ptzParamBO.Action == PTZControlAction.Start ? PTZControlType.PTZ_FOCUSDEC : PTZControlType.PTZ_FOCUSDECSTOP;
                    break;

                case PTZCommand.FOCUS_FAR:
                    ptzControlType = ptzParamBO.Action == PTZControlAction.Start ? PTZControlType.PTZ_FOCUSADD : PTZControlType.PTZ_FOCUSADDSTOP;
                    break;

                case PTZCommand.FOCUS_AUTO:
                    ptzControlType = PTZControlType.PTZ_AUTOFOCUS;
                    break;

                case PTZCommand.ZOOM_IN:
                    ptzControlType = ptzParamBO.Action == PTZControlAction.Start ? PTZControlType.PTZ_ZOOMDEC : PTZControlType.PTZ_ZOOMDECSTOP;
                    break;

                case PTZCommand.ZOOM_OUT:
                    ptzControlType = ptzParamBO.Action == PTZControlAction.Start ? PTZControlType.PTZ_ZOOMADD : PTZControlType.PTZ_ZOOMADDSTOP;
                    break;

                case PTZCommand.IRIS_OPEN:
                    ptzControlType = ptzParamBO.Action == PTZControlAction.Start ? PTZControlType.PTZ_IRISADD : PTZControlType.PTZ_IRISADDSTOP;
                    break;

                case PTZCommand.IRIS_CLOSE:
                    ptzControlType = ptzParamBO.Action == PTZControlAction.Start ? PTZControlType.PTZ_IRISDEC : PTZControlType.PTZ_IRISDECSTOP;
                    break;

                case PTZCommand.TILT_UP:
                    ptzControlType = ptzParamBO.Action == PTZControlAction.Start ? PTZControlType.PTZ_UP : PTZControlType.PTZ_UPSTOP;
                    break;

                case PTZCommand.TILT_DOWN:
                    ptzControlType = ptzParamBO.Action == PTZControlAction.Start ? PTZControlType.PTZ_DOWN : PTZControlType.PTZ_DOWNSTOP;
                    break;

                case PTZCommand.PAN_LEFT:
                    ptzControlType = ptzParamBO.Action == PTZControlAction.Start ? PTZControlType.PTZ_LEFT : PTZControlType.PTZ_LEFTSTOP;
                    break;

                case PTZCommand.PAN_RIGHT:
                    ptzControlType = ptzParamBO.Action == PTZControlAction.Start ? PTZControlType.PTZ_RIGHT : PTZControlType.PTZ_RIGHTSTOP;
                    break;

                case PTZCommand.UP_LEFT:
                    ptzControlType = ptzParamBO.Action == PTZControlAction.Start ? PTZControlType.PTZ_LEFTUP : PTZControlType.PTZ_LEFTUPSTOP;
                    break;

                case PTZCommand.UP_RIGHT:
                    ptzControlType = ptzParamBO.Action == PTZControlAction.Start ? PTZControlType.PTZ_RIGHTUP : PTZControlType.PTZ_RIGHTUPSTOP;
                    break;

                case PTZCommand.DOWN_LEFT:
                    ptzControlType = ptzParamBO.Action == PTZControlAction.Start ? PTZControlType.PTZ_LEFTDOWN : PTZControlType.PTZ_LEFTDOWNSTOP;
                    break;

                case PTZCommand.DOWN_RIGHT:
                    ptzControlType = ptzParamBO.Action == PTZControlAction.Start ? PTZControlType.PTZ_RIGHTDOWN : PTZControlType.PTZ_RIGHTDOWNSTOP;
                    break;

                case PTZCommand.WIPER:
                    ptzControlType = PTZControlType.PTZ_WIPER;
                    break;
            }

            foreach (DeviceAddressBO deviceAddressModel in DeviceBO.DeviceAddressList)
            {
                if (ptzParamBO.ChannelID == deviceAddressModel.ChannelID)
                {
                    if (string.Equals(deviceAddressModel.Type, ChannelType.IRCamera.ToString(), StringComparison.InvariantCultureIgnoreCase))
                    {
                        return await _irDeviceAdapter.PTZControlAll(ptzControlType, ptzParamBO.Speed);
                    }
                    else if (string.Equals(deviceAddressModel.Type, ChannelType.DCCamera.ToString(), StringComparison.InvariantCultureIgnoreCase))
                    {
                        if (_dcDeviceAdapter != null)
                        {
                            return await _dcDeviceAdapter.PTZControlAll(ptzControlType, ptzParamBO.Speed);
                        }
                    }
                }
            }

            return false;
        }

        public override void PTZCruise(CruiseParamBO cruiseParamBO)
        {
            TimerTask.Manual(cruiseParamBO.CruiseID, cruiseParamBO.Command);
        }

        protected override void PauseCruiseTask(PauseType pauseType)
        {
            TimerTask.Pause(pauseType);
        }

        public override void RestoreCruiseTask()
        {
            TimerTask.Restore();
        }

        public override async Task<bool> SetAlarmOut(List<AlarmOutBO> alarmOutBOs)
        {
            bool result = true;
            foreach (AlarmOutBO alarmOutBO in alarmOutBOs)
            {
                if (!await _irDeviceAdapter.SetStatus(alarmOutBO.ChannelID, alarmOutBO.IsOn))
                {
                    result = false;
                };
            }
            return result;
        }

        public override void SetName(string name)
        {
            _deviceBO.SetName(name);
        }

        public override async Task<bool> SetOSDCustomer(int channel, CustomerOSDBO customer)
        {
            DeviceAddressBO channelEntity = _deviceBO.DeviceAddressList.FirstOrDefault(x => x.ChannelID == channel);
            if (channelEntity == null)
            {
                return false;
            }

            Enum.TryParse(channelEntity.Type, out ChannelType type);
            switch (type)
            {
                case ChannelType.IRCamera:
                    for (int i = 0; i < customer.MarkerList.Count; i++)
                    {
                        var markerBO = OSDInfoBO.CustomerOSD.MarkerList.Where(it => string.Equals(customer.MarkerList[i].Name, it.Name)).FirstOrDefault();
                        if(markerBO!=null)
                        {
                            OSDInfoBO.CustomerOSD.MarkerList[OSDInfoBO.CustomerOSD.MarkerList.IndexOf(markerBO)] = customer.MarkerList[i];
                        }
                        else
                        {
                            OSDInfoBO.CustomerOSD.MarkerList.Add(customer.MarkerList[i]);
                        }
                    }
                    OSDInfoBO.CustomerOSD.ContentList = customer.ContentList;
                    return true;

                case ChannelType.DCCamera:
                    return await _dcDeviceAdapter.SetCustomerOSD(new CustomerOSDParam()
                    {
                        ContentOSDParams = customer.ContentList.Select(x => new ContentOSDParam()
                        {
                            Content = x.Content,
                            IsShow = x.IsShow,
                            Status = (int)x.Status,
                            X = x.X,
                            Y = x.Y
                        }).ToArray()
                    });

                default:
                    return false;
            }
        }

        public override DeviceStreamBO GetStream(StreamType streamType)
        {
            DeviceStreamBO deviceStreamBO = new DeviceStreamBO(DeviceBO.ID);

            foreach (DeviceAddressBO deviceAddressBO in _deviceBO.DeviceAddressList)
            {
                string irRTSPUrl;
                string irWebRTCUrl;
                switch (streamType)
                {
                    case StreamType.MainStream:
                        irRTSPUrl = string.Format(MediaKitRtspServer.RTSPServerConfig.RTSPServerOutputURL, DeviceBO.ID, deviceAddressBO.ChannelID, MediaKitRtspServer.RTSPServerConfig.PriStr);
                        irWebRTCUrl = string.Format(MediaKitRtspServer.RTSPServerConfig.WebRTCServerOutputURL, DeviceBO.ID, deviceAddressBO.ChannelID, MediaKitRtspServer.RTSPServerConfig.PriStr);
                        break;

                    case StreamType.SubStream:
                        irRTSPUrl = string.Format(MediaKitRtspServer.RTSPServerConfig.RTSPServerOutputURL, DeviceBO.ID, deviceAddressBO.ChannelID, MediaKitRtspServer.RTSPServerConfig.SubStr);
                        irWebRTCUrl = string.Format(MediaKitRtspServer.RTSPServerConfig.WebRTCServerOutputURL, DeviceBO.ID, deviceAddressBO.ChannelID, MediaKitRtspServer.RTSPServerConfig.SubStr);
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(nameof(streamType), streamType, null);
                }
                deviceStreamBO.StreamItems.Add(new DeviceStreamItemBO(deviceAddressBO.ChannelID, irRTSPUrl, irWebRTCUrl));
            }

            return deviceStreamBO;
        }

        public override bool SetInstantTask(TaskBO taskBO)
        {
            return false;
        }

        public override List<TaskBO> GetInstantTaskList()
        {
            return new List<TaskBO>();
        }

        public override ImageInfoBO CaptureImage(int channel)
        {
            ChannelType channelType = GetChannelType(channel);
            FrameBO frameBO = GetFrame().Result;
            SaveFileBO saveFileBO = null;
            string imagePath = string.Empty;
            DateTime dateTime = DateTime.UtcNow;
            FileType fileType = FileType.None;
            switch (channelType)
            {
                case ChannelType.IRCamera:
                    saveFileBO = new SaveFileBO(SaveFileType.Snapshot, _deviceBO.ID, _deviceBO.Name, CurrentPresetID, channel, channelType, dateTime.ToLocalTime(), frameBO.IRFrame, null, IRExpectedImageWidth, IRExpectedImageHeight, ((FireMonitorSampleTask)SampleTask).SampleDataBO.Markers);
                    fileType = frameBO.IRFrame.FileType;
                    break;
                case ChannelType.DCCamera:
                    saveFileBO = new SaveFileBO(SaveFileType.Snapshot, _deviceBO.ID, _deviceBO.Name, CurrentPresetID, channel, channelType, dateTime.ToLocalTime(), null, frameBO.DCFrame, ExpectedImageWidth, ExpectedImageHeight, null);
                    fileType = frameBO.DCFrame.FileType;
                    break;
            }
            if (saveFileBO != null)
            {
                imagePath = Device.DeviceConfigBO.SaveFile(saveFileBO);
            }
            return new ImageInfoBO(channel, channelType, fileType, imagePath, Device.DeviceConfigBO.SnapshotRootPath);
        }

        public override float GetSpotTemperature(int x, int y) => 0;

        public override List<string> GetPaletteList() => null;

        public override string GetPalette() => null;

        public override bool SetPalette(string paletteName) => false;

        public override List<TemperatureRangeBO> GetTemperatureRangeList() => null;

        public override int GetTemperatureRangeID() => 0;

        public override bool SetTemperatureRange(int setID) => false;

        public override Task<bool> ResetAlarmStatus()
        {
            return ((FireMonitorAlarmTask)AlarmTask).ResetAlarmStatus();
        }

        internal async Task<FrameBO> GetFrame()
        {
            OWBIRFrame irFrame = null;
            OWBDCFrame dcFrame = null;
            bool isSuccess = false;

            if (_irDeviceAdapter?.IsConnected == false)
            {
                return new FrameBO(irFrame, dcFrame, false);
            }

            if (_irDeviceAdapter != null)
            {
                irFrame = await _irDeviceAdapter.GetFrame();
                isSuccess = irFrame != null;
            }

            if (_dcDeviceAdapter != null)
            {
                dcFrame = await _dcDeviceAdapter.GetFrame();
                isSuccess = dcFrame != null && isSuccess;
            }

            if (!isSuccess)
            {
                _logger.LogWarning($"GetFrame:Device: {_deviceBO}, IRFrame null: {irFrame == null}, DCFrame null: {dcFrame == null}, success:{isSuccess}");
            }

            return new FrameBO(irFrame, dcFrame, isSuccess);
        }

        internal SampleDataBO GetSampleDataBO()
        {
            return (SampleTask as FireMonitorSampleTask).SampleDataBO;
        }

        internal void InternalPauseCruiseTask(PauseType pauseType)
        {
            PauseCruiseTask(pauseType);
        }

        internal List<FireAlarmMarkerBO> CheckAlarm(List<FireMonitorMarkerBO> markerBOs, List<OWBMarker> markers)
        {
            return (AlarmTask as FireMonitorAlarmTask).CheckAlarm(markerBOs, markers);
        }

        internal void ChangeAlarmDataByScript(int presetID, string presetName, OWBIRFrame irFrame, OWBDCFrame dcFrame, ref List<FireAlarmMarkerBO> alarmMarkers)
        {
            (AlarmTask as FireMonitorAlarmTask).ChangeAlarmDataByScript(presetID, presetName, irFrame, dcFrame, ref alarmMarkers);
        }

        #endregion public methods

        #region private methods

        private void Start()
        {
            // Must finish Login before SampleTask / FixedMonitoring GOTO; otherwise PTZPreset
            // races with a null adapter, CurrentPresetID stays GOING_PRESET, and recording never runs.
            try
            {
                Login().ConfigureAwait(false).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Start Login failed,{DeviceBO},{ex}");
            }

            // Holly Login awaits LoginCamera; keep a short wait as safety for status races.
            try
            {
                DateTime waitUntil = DateTime.UtcNow.AddSeconds(20);
                while (_irDeviceAdapter != null && !_irDeviceAdapter.IsConnected && DateTime.UtcNow < waitUntil)
                {
                    System.Threading.Thread.Sleep(500);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Start wait-connected failed,{DeviceBO},{ex}");
            }


            StartTask();
            StartPullStream();
            StartOSDTask();
        }

        private void Stop()
        {
            StopTask();
            StopOSDTask();
            StopPullStream();
            Logout();
        }

        private async Task<bool> Login()
        {
            bool result = false;
            DevicePainterFactory.Instance.TryGetOrAdd(_deviceBO.ID, out DevicePainter devicePainter);
            if (devicePainter != null)
            {
                _devicePainter = devicePainter;
            }

            if (DeviceBO.DeviceAddressList != null)
            {
                bool loginResult = true;
                foreach (DeviceAddressBO deviceAddressBO in DeviceBO.DeviceAddressList)
                {
                    bool tmpLoginResult = false;

                    if (string.Equals(deviceAddressBO.Type, ChannelType.IRCamera.ToString(), StringComparison.InvariantCultureIgnoreCase))
                    {
                        if (_irDeviceAdapter == null)
                        {
                            _irDeviceAdapter = new OWBIRCameraAdapter(DeviceBO.ID);
                        }
                        else
                        {
                            _irDeviceAdapter.Logout();
                        }

                        Enum.TryParse(deviceAddressBO.ProtocolType, out IRCameraType deviceProtocol);
                        tmpLoginResult = await _irDeviceAdapter.Login(deviceProtocol, deviceAddressBO.IP, deviceAddressBO.Port, deviceAddressBO.Username, deviceAddressBO.Password);
                        _irDeviceAdapter.RegisterStatusHandler(OnDeviceStatusChanged);
                    }
                    else if (string.Equals(deviceAddressBO.Type, ChannelType.DCCamera.ToString(), StringComparison.InvariantCultureIgnoreCase))
                    {
                        if (_dcDeviceAdapter == null)
                        {
                            _dcDeviceAdapter = new OWBDCCameraAdapter(DeviceBO.ID);
                            _dcDeviceAdapter.SetChannel(int.Parse(deviceAddressBO.ParamID));
                        }
                        else
                        {
                            _dcDeviceAdapter.Logout();
                        }

                        Enum.TryParse(deviceAddressBO.ProtocolType, out DCCameraType deviceProtocol);
                        tmpLoginResult = await _dcDeviceAdapter.Login(deviceProtocol, deviceAddressBO.IP, deviceAddressBO.Port, deviceAddressBO.Username, deviceAddressBO.Password);
                    }

                    loginResult &= tmpLoginResult;

                    _logger.LogInformation($"Login,{DeviceBO}, Login:{loginResult}, Device IP: {deviceAddressBO.IP}, Device Port: {deviceAddressBO.Port}");
                }
                result = loginResult;
            }

            return result;
        }

        private void Logout()
        {
            _devicePainter.Dispose();
            _irDeviceAdapter.UnRegisterStatusHandler(OnDeviceStatusChanged);
            _irDeviceAdapter?.Logout();
            _dcDeviceAdapter?.Logout();
            _logger.LogInformation($"Logout,{DeviceBO}");
        }

        private void StartPullStream()
        {
            _irStreamPusherPri.Init(DeviceConfigBO.OSDConfigEnabled && Constants.OSDConfigMode == OSDConfigMode.ToServer);
            _irStreamPusherSub.Init(DeviceConfigBO.OSDConfigEnabled && Constants.OSDConfigMode == OSDConfigMode.ToServer);
            _dcStreamPusherPri.Init(false);
            _dcStreamPusherSub.Init(false);
        }

        private void StopPullStream()
        {
            _irStreamPusherPri.Dispose();
            _irStreamPusherSub.Dispose();
            _dcStreamPusherPri.Dispose();
            _dcStreamPusherSub.Dispose();
        }

        private void StartTask()
        {
            TimerTask.Start();
            SampleTask.Start();
            AlarmTask.Start();
        }

        private void StopTask()
        {
            TimerTask.Stop();
            SampleTask.Stop();
            AlarmTask.Stop();
        }

        private void StartOSDTask()
        {
            _refreshOSDTaskCanceled = false;
            _refreshOSDTask = Task.Factory.StartNew(DoRefreshOSDLayer);

            OSDInfoBO.RefreshConfig(DeviceConfigBO.OSDConfigEnabled, DeviceConfigBO.ChannelConfigBO, DeviceConfigBO.PTZOSDConfig, DeviceConfigBO.MarkerOSDConfig);
            if (Constants.OSDConfigMode == OSDConfigMode.ToDevice)
            {
                UpdateDeviceOSD();
            }
        }

        private void UpdateDeviceOSD()
        {
            if (_irDeviceAdapter == null || DeviceConfigBO == null)
            {
                return;
            }

            bool enablePresetTitle = DeviceConfigBO.PTZOSDConfig?.Status == OSDStatus.Enable;
            PointBO presetTitlePosition = DeviceConfigBO.PTZOSDConfig?.Position ?? new PointBO();

            ChannelBO channelBO = DeviceConfigBO.ChannelConfigBO?.ChannelItems?.FirstOrDefault();
            TimeBO timeBO = channelBO?.TimeBO ?? new TimeBO { Status = OSDStatus.Disable, Position = new PointBO() };
            bool enableChannelName = channelBO?.Status == OSDStatus.Enable;
            PointBO channelPosition = channelBO?.Position ?? new PointBO();
            string presetTitle = ResolvePresetOsdTitle();
            bool osdEnabled = DeviceConfigBO.OSDConfigEnabled;
            int titleX = presetTitlePosition.X;
            int titleY = presetTitlePosition.Y;
            bool enableTime = timeBO.Status == OSDStatus.Enable;
            int timeX = timeBO.Position?.X ?? 0;
            int timeY = timeBO.Position?.Y ?? 0;
            string channelName = channelBO?.Name;
            int channelX = channelPosition.X;
            int channelY = channelPosition.Y;

            // Serialize device-side OSD writes; concurrent StartOSDTask + Online push previously raced native net_set_channel.
            lock (_deviceOsdSync)
            {
                try
                {
                    bool ok = _irDeviceAdapter.SetOSDInfo(
                        osdEnabled,
                        enablePresetTitle, presetTitle, titleX, titleY,
                        enableTime, timeX, timeY,
                        enableChannelName, channelName, channelX, channelY)
                        .ConfigureAwait(false).GetAwaiter().GetResult();
                    if (!ok)
                    {
                        _logger.LogWarning($"UpdateDeviceOSD partial/failed,{DeviceBO}, enabled={osdEnabled}, title={enablePresetTitle}, time={enableTime}, name={enableChannelName}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"UpdateDeviceOSD failed,{DeviceBO},{ex}");
                }
            }
        }

        private string ResolvePresetOsdTitle()
        {
            if (CurrentPresetEnabled && DeviceConfigBO?.PresetConfigBO?.PresetList != null)
            {
                FireMonitorPresetBO presetBO = DeviceConfigBO.PresetConfigBO.PresetList.Find(a => a.ID == CurrentPresetID);
                string fromConfig = BuildPresetInfo(presetBO, CurrentCruiseAction);
                if (!string.IsNullOrEmpty(fromConfig))
                {
                    return fromConfig;
                }
            }

            return OSDInfoBO.PresetName ?? string.Empty;
        }

        private void StopOSDTask()
        {
            _refreshOSDTaskCanceled = true;
            _refreshOSDTask?.Wait();
        }

        /// <summary>
        /// 1: get layer param
        /// 2: generate layer by Device.Painter
        /// 3: apply layer to rtsp pusher
        /// </summary>
        private async void DoRefreshOSDLayer()
        {
            while (!_refreshOSDTaskCanceled)
            {
                try
                {
                    if (OSDInfoBO.Enabled && _irDeviceAdapter.Width != 0 && _irDeviceAdapter.Height != 0 && _irDeviceAdapter.StreamWidth != 0 && _irDeviceAdapter.StreamHeight != 0)
                    {
                        ChannelBO channelBO = OSDInfoBO.GetFirstChannel();
                        PTZOSDConfigBO ptzOSDConfig = OSDInfoBO.GetPTZConfig();
                        MarkerOSDConfigBO markerOSDConfig = OSDInfoBO.GetMarkerConfig();
                        AlarmOSDConfigBO alarmOSDConfig = OSDInfoBO.GetAlarmOSDConfig();

                        if (Device.Constants.OSDConfigMode == OSDConfigMode.ToDevice)
                        {
                            List<OWBMarker> customMarkers = new List<OWBMarker>();
                            List<MarkerOSDParamBO> removeMarkerBOs = new List<MarkerOSDParamBO>();
                            foreach (MarkerOSDParamBO customerMarker in OSDInfoBO.CustomerOSD.MarkerList)
                            {
                                customerMarker.Duration--;
                                OWBMarker marker;
                                switch (customerMarker.Type)
                                {
                                    case MarkerType.Spot:
                                        marker = new OWBSpotMarker(customerMarker.Name, new Point((int)(customerMarker.Points[0].Rpx * _irDeviceAdapter.Width), (int)(customerMarker.Points[0].Rpy * _irDeviceAdapter.Height)), false, 1, OWBMath.KelvinCelsiurTwenty, 1, 0);
                                        break;

                                    case MarkerType.Line:
                                        marker = new OWBPolylineMarker(customerMarker.Name, customerMarker.Points.Select(x => new Point((int)(x.Rpx * _irDeviceAdapter.Width - 1), (int)(x.Rpy * _irDeviceAdapter.Height - 1))).ToArray(), false, 1, OWBMath.KelvinCelsiurTwenty, 1, 0);
                                        break;

                                    case MarkerType.Region:
                                        marker = new OWBPolygonMarker(customerMarker.Name, customerMarker.Points.Select(x => new Point((int)(x.Rpx * _irDeviceAdapter.Width - 1), (int)(x.Rpy * _irDeviceAdapter.Height - 1))).ToArray(), false, 1, OWBMath.KelvinCelsiurTwenty, 1, 0);
                                        break;

                                    default:
                                        throw new ArgumentOutOfRangeException();
                                }

                                if (customerMarker.Duration <= 0)
                                {
                                    if (await _irDeviceAdapter.DeleteMarker(marker))
                                    {
                                        removeMarkerBOs.Add(customerMarker);
                                    }
                                }
                                else
                                {
                                    customMarkers.Add(marker);
                                }
                            }

                            foreach (MarkerOSDParamBO markerBO in removeMarkerBOs)
                            {
                                OSDInfoBO.CustomerOSD.MarkerList.Remove(markerBO);
                            }

                            await _irDeviceAdapter.SetMarkers(customMarkers, OSDInfoBO.Enabled, markerOSDConfig.Status == OSDStatus.Enable, markerOSDConfig.ShowMax, markerOSDConfig.ShowAvg, markerOSDConfig.ShowMin);
                        }
                        else
                        {
                            List<OWBMarker> allMarkers = new List<OWBMarker>();
                            if (OSDInfoBO.Markers?.Any() == true)
                            {
                                allMarkers.AddRange(OSDInfoBO.Markers);
                            }

                            foreach (MarkerOSDParamBO customerMarker in OSDInfoBO.CustomerOSD.MarkerList)
                            {
                                if (customerMarker.Duration <= 0)
                                {
                                    continue;
                                }
                                customerMarker.Duration--;
                                OWBMarker marker;
                                switch (customerMarker.Type)
                                {
                                    case MarkerType.Spot:
                                        marker = new OWBSpotMarker(customerMarker.Name, new Point((int)(customerMarker.Points[0].Rpx * _irDeviceAdapter.Width), (int)(customerMarker.Points[0].Rpy * _irDeviceAdapter.Height)));
                                        break;

                                    case MarkerType.Line:
                                        marker = new OWBPolylineMarker(customerMarker.Name, customerMarker.Points.Select(x => new Point((int)(x.Rpx * _irDeviceAdapter.Width - 1), (int)(x.Rpy * _irDeviceAdapter.Height - 1))).ToArray());
                                        break;

                                    case MarkerType.Region:
                                        marker = new OWBPolygonMarker(customerMarker.Name, customerMarker.Points.Select(x => new Point((int)(x.Rpx * _irDeviceAdapter.Width - 1), (int)(x.Rpy * _irDeviceAdapter.Height - 1))).ToArray());
                                        break;

                                    default:
                                        throw new ArgumentOutOfRangeException();
                                }

                                if (OSDInfoBO.IRFrame.GetTemperature(marker, out OWBMarkerTemperaturePoint tempPoint))
                                {
                                    marker.Value = tempPoint;
                                    allMarkers.Add(marker);
                                }
                            }

                            byte[] layer = null;

                            if (_irStreamPusherPri.IsRun)
                            {
                                DevicePainterBO devicePainterBO =
                                   new DevicePainterBO
                                   (
                                       _irDeviceAdapter.Width, _irDeviceAdapter.Height, _irDeviceAdapter.StreamWidth, _irDeviceAdapter.StreamHeight,
                                       channelBO.Status, channelBO.Name, channelBO.Position, channelBO.TimeBO.Status, channelBO.TimeBO.Position,
                                       ptzOSDConfig.Status, OSDInfoBO.PresetName, ptzOSDConfig.Position,
                                       markerOSDConfig.Status, markerOSDConfig.ShowMax, markerOSDConfig.ShowMin, markerOSDConfig.ShowAvg,
                                       allMarkers,
                                       alarmOSDConfig.AlarmContent, alarmOSDConfig.Status,
                                       OSDInfoBO.CustomerOSD
                                   );

                                _devicePainter.RefreshParam(devicePainterBO);
                                layer = _devicePainter.GetLayer();

                                _irStreamPusherPri.SetOSD(layer);
                            }

                            if (_irStreamPusherSub.IsRun)
                            {
                                // regenerate sub layer:
                                // 1: pri layer resolution is not equal with sub layer
                                // 2: pri layer is not created
                                if (_irDeviceAdapter.StreamWidth != _irDeviceAdapter.SubStreamWidth || _irDeviceAdapter.StreamHeight != _irDeviceAdapter.SubStreamHeight
                                    || layer == null)
                                {
                                    DevicePainterBO devicePainterBO =
                                       new DevicePainterBO
                                       (
                                           _irDeviceAdapter.Width, _irDeviceAdapter.Height, _irDeviceAdapter.SubStreamWidth, _irDeviceAdapter.SubStreamHeight,
                                           channelBO.Status, channelBO.Name, channelBO.Position, channelBO.TimeBO.Status, channelBO.TimeBO.Position,
                                           ptzOSDConfig.Status, OSDInfoBO.PresetName, ptzOSDConfig.Position,
                                           markerOSDConfig.Status, markerOSDConfig.ShowMax, markerOSDConfig.ShowMin, markerOSDConfig.ShowAvg,
                                           OSDInfoBO.Markers,
                                           alarmOSDConfig.AlarmContent, alarmOSDConfig.Status,
                                           OSDInfoBO.CustomerOSD
                                       );

                                    _devicePainter.RefreshParam(devicePainterBO);
                                    layer = _devicePainter.GetLayer();
                                }

                                _irStreamPusherSub.SetOSD(layer);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError(e.ToString());
                }
                finally
                {
                    await Task.Delay(Constants.WAIT_TIMEOUT_DEFAULT);
                }
            }
        }

        private List<OWBShieldMask> GetDeviceShieldMask(List<ShieldMaskBO> shieldMarkerModelList)
        {
            List<OWBShieldMask> shieldMaskList = new List<OWBShieldMask>();
            foreach (ShieldMaskBO shieldMarkerModel in shieldMarkerModelList)
            {
                List<Point> pointList = new List<Point>();
                shieldMarkerModel.PointList.ForEach(a => pointList.Add(new Point(a.X, a.Y)));
                shieldMaskList.Add(new OWBShieldMask(shieldMarkerModel.Name, pointList.ToArray()));
            }
            return shieldMaskList;
        }

        private List<OWBShieldMask> GetDeviceShieldMask(List<FireMonitorShieldMaskBO> shieldMarkerModelList)
        {
            List<OWBShieldMask> shieldMaskList = new List<OWBShieldMask>();
            foreach (FireMonitorShieldMaskBO shieldMarkerModel in shieldMarkerModelList)
            {
                List<Point> pointList = new List<Point>();
                shieldMarkerModel.PointList.ForEach(a => pointList.Add(new Point(a.X, a.Y)));
                shieldMaskList.Add(new OWBShieldMask(shieldMarkerModel.Name, pointList.ToArray()));
            }
            return shieldMaskList;
        }

        private ChannelType GetChannelType(int channelID)
        {
            DeviceAddressBO deviceAddressBO = _deviceBO.DeviceAddressList.FirstOrDefault(x => x.ChannelID == channelID);
            ChannelType channelType = ChannelType.IRCamera;
            Enum.TryParse(deviceAddressBO.Type, out channelType);
            return channelType;
        }

        private StreamType GetStreamType(string streamTypeStr)
        {
            return streamTypeStr == MediaKitRtspServer.RTSPServerConfig.PriStr ? StreamType.MainStream : StreamType.SubStream;
        }

        private async Task<StreamFilterContext> GetFilterContext(int channelID, string streamTypeStr, ChannelType channelType, StreamType streamType)
        {
            DeviceAddressBO deviceAddressBO = _deviceBO.DeviceAddressList.FirstOrDefault(x => x.ChannelID == channelID);
            if (deviceAddressBO == null)
            {
                return null;
            }

            StreamFilterContext context = new StreamFilterContext
            {
                OutUrl = string.Format(MediaKitRtspServer.RTSPServerConfig.RTSPServerInputURL, _deviceBO.ID, deviceAddressBO.ChannelID, streamTypeStr),
                OutStream = string.Format(MediaKitRtspServer.RTSPServerConfig.RTSPServerInputStream, _deviceBO.ID, deviceAddressBO.ChannelID, streamTypeStr),
                StreamContentType = MediaKitRtspServer.RTSPServerConfig.StreamContentType,
                UserName = deviceAddressBO.Username,
                Password = deviceAddressBO.Password
            };

            switch (channelType)
            {
                case ChannelType.IRCamera:
                    context.EnableFilter = OSDInfoBO.Enabled;
                    //todo await
                    context.InUrl = _irDeviceAdapter.GetStream(streamType).Result;

                    if (streamTypeStr == MediaKitRtspServer.RTSPServerConfig.PriStr)
                    {
                        context.StreamHeight = _irDeviceAdapter.StreamHeight;
                        context.StreamWidth = _irDeviceAdapter.StreamWidth;
                        context.ImageHeight = _irDeviceAdapter.StreamHeight;
                        context.ImageWidth = _irDeviceAdapter.StreamWidth;
                    }
                    else
                    {
                        context.StreamHeight = _irDeviceAdapter.SubStreamHeight;
                        context.StreamWidth = _irDeviceAdapter.SubStreamWidth;
                        context.ImageHeight = _irDeviceAdapter.SubStreamHeight;
                        context.ImageWidth = _irDeviceAdapter.SubStreamWidth;
                    }
                    break;

                case ChannelType.DCCamera:
                    context.EnableFilter = false;
                    context.InUrl = await _dcDeviceAdapter.GetStream(streamType, int.Parse(deviceAddressBO.ParamID));
                    context.StreamHeight = -1;
                    context.StreamWidth = -1;
                    context.ImageHeight = -1;
                    context.ImageWidth = -1;
                    break;
            }

            return context;
        }

        private async void DeviceStreamStartEvent(object sender, DeviceStreamArgs args)
        {
            if (args.DeviceID != _deviceBO.ID)
            {
                return;
            }
            ChannelType channelType = GetChannelType(args.ChannelID);
            StreamType streamType = GetStreamType(args.StreamType);
            StreamFilterContext context = await GetFilterContext(args.ChannelID, args.StreamType, channelType, streamType);

            OnNetEvent?.Invoke(this, new DeviceNetArgs()
            {
                DeviceID = _deviceBO.ID,
                EventType = NetEventType.StreamOpen,
                Data = JsonSerializer.Serialize(args)
            });
            _logger.LogInformation($"DeviceStreamStartEvent: Device: {_deviceBO}, StreamOpen");

            switch (streamType)
            {
                case StreamType.MainStream:
                    switch (channelType)
                    {
                        case ChannelType.IRCamera:
                            _irStreamPusherPri.StartFilter(context);
                            break;

                        case ChannelType.DCCamera:
                            _dcStreamPusherPri.StartFilter(context);
                            break;

                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                    break;

                case StreamType.SubStream:
                    switch (channelType)
                    {
                        case ChannelType.IRCamera:
                            _irStreamPusherSub.StartFilter(context);
                            break;

                        case ChannelType.DCCamera:
                            _dcStreamPusherSub.StartFilter(context);
                            break;

                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void DeviceStreamStopEvent(object sender, DeviceStreamArgs args)
        {
            if (args.DeviceID != _deviceBO.ID)
            {
                return;
            }

            OnNetEvent?.Invoke(this, new DeviceNetArgs()
            {
                DeviceID = _deviceBO.ID,
                EventType = NetEventType.StreamClose,
                Data = JsonSerializer.Serialize(args)
            });
            _logger.LogInformation($"DeviceStreamStopEvent: Device: {_deviceBO}, StreamClose");

            DeviceAddressBO deviceAddressBO = _deviceBO.DeviceAddressList.FirstOrDefault(x => x.ChannelID == args.ChannelID);
            if (deviceAddressBO == null)
            {
                return;
            }
            Enum.TryParse(deviceAddressBO.Type, out ChannelType channelType);
            StreamType streamType = args.StreamType == MediaKitRtspServer.RTSPServerConfig.PriStr ? StreamType.MainStream : StreamType.SubStream;
            switch (streamType)
            {
                case StreamType.MainStream:
                    switch (channelType)
                    {
                        case ChannelType.IRCamera:
                            _irStreamPusherPri.StopFilter();
                            break;

                        case ChannelType.DCCamera:
                            _dcStreamPusherPri.StopFilter();
                            break;

                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                    break;

                case StreamType.SubStream:
                    switch (channelType)
                    {
                        case ChannelType.IRCamera:
                            _irStreamPusherSub.StopFilter();
                            break;

                        case ChannelType.DCCamera:
                            _dcStreamPusherSub.StopFilter();
                            break;

                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void OnDeviceStatusChanged(object sender, DeviceStatusArgs args)
        {
            if (args.Type == DeviceStatusChangeType.Online && args.IsConnected)
            {
                PushDeviceSideConfigAfterOnline();
            }

            if (OnMonitorEvent == null)
            {
                return;
            }

            var moldMonitorStatusChangedBO = new FireMonitorStatusChangedBO
            {
                DeviceID = DeviceBO.ID,
                AlarmInStatus = args.AlarmInStatus,
                IsConnected = args.IsConnected,
                DeviceInfoBOList = new List<DeviceInfoBO>()
            };

            foreach (DeviceAddressBO deviceAddressModel in DeviceBO.DeviceAddressList)
            {
                DeviceInfoBO deviceModel = new DeviceInfoBO();
                if (string.Equals(deviceAddressModel.Type, Constants.IRCamera, StringComparison.InvariantCultureIgnoreCase))
                {
                    _irDeviceAdapter.GetModel(out OWB.IRCamera.DeviceModel irCameraModel);
                    deviceModel.ChannelID = 0;
                    deviceModel.Model = irCameraModel.Model;
                    deviceModel.SeriesNo = irCameraModel.SeriesNO;
                    deviceModel.FirmwareVersion = irCameraModel.FirmwareVersion;
                    deviceModel.Width = irCameraModel.Width;
                    deviceModel.Height = irCameraModel.Height;
                    deviceModel.StreamWidth = irCameraModel.StreamWidth;
                    deviceModel.StreamHeight = irCameraModel.StreamHeight;
                }
                else if (string.Equals(deviceAddressModel.Type, Constants.DCCamera, StringComparison.InvariantCultureIgnoreCase))
                {
                    _dcDeviceAdapter.GetModel(out OWB.DCCamera.DeviceModel dcCameraModel);
                    deviceModel.ChannelID = 1;
                    deviceModel.Model = dcCameraModel.Model;
                    deviceModel.SeriesNo = dcCameraModel.SeriesNO;
                    deviceModel.FirmwareVersion = dcCameraModel.FirmwareVersion;
                    deviceModel.Width = dcCameraModel.Width;
                    deviceModel.Height = dcCameraModel.Height;
                }

                moldMonitorStatusChangedBO.DeviceInfoBOList.Add(deviceModel);
            }

            switch (args.Type)
            {
                case DeviceStatusChangeType.Online:
                    OnMonitorEvent(this, new DeviceMonitorArgs(DeviceBO.ID, DeviceBO.Name, DeviceEventType.OnlineStatusChanged, moldMonitorStatusChangedBO, null));
                    break;
                case DeviceStatusChangeType.AlarmIn:
                    OnMonitorEvent(this, new DeviceMonitorArgs(DeviceBO.ID, DeviceBO.Name, DeviceEventType.AlarmInput, moldMonitorStatusChangedBO, null));
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// After IR reconnects, re-push OSD text and current preset markers to the camera body (ToDevice).
        /// </summary>
        private void PushDeviceSideConfigAfterOnline()
        {
            if (Device.Constants.OSDConfigMode != OSDConfigMode.ToDevice || DeviceConfigBO == null)
            {
                return;
            }

            UpdateDeviceOSD();

            int presetId = CurrentPresetID;
            if (presetId == Constants.GOING_PRESET || presetId <= 0)
            {
                FireMonitorPresetBO firstPreset = DeviceConfigBO.PresetConfigBO?.PresetList?.FirstOrDefault();
                if (firstPreset == null)
                {
                    return;
                }
                presetId = firstPreset.ID;
            }

            Task.Factory.StartNew(async () =>
            {
                try
                {
                    PresetParamBO param = new PresetParamBO
                    {
                        Command = PresetCommand.GOTO,
                        PresetID = presetId,
                        Speed = 5,
                        AutoFocus = false
                    };
                    bool ok = await PTZPreset(param);
                    if (!ok)
                    {
                        _logger.LogWarning($"PushDeviceSideConfigAfterOnline failed, device={DeviceBO?.ID}, presetID={presetId}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.ToString());
                }
            });
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

        internal void RefreshOSDInfo(int presetID)
        {
            SampleDataBO sampleDataBO = GetSampleDataBO();
            if (sampleDataBO != null)
            {
                FireMonitorPresetBO presetBO = DeviceConfigBO.PresetConfigBO.PresetList.Find(a => a.ID == presetID);

                List<OWBMarker> markers = new List<OWBMarker>();
                if (presetBO != null)
                {
                    List<OWBMarker> rawMarkers = presetBO.GetDeviceMarker();
                    foreach (OWBMarker rawMarker in rawMarkers)
                    {
                        if (sampleDataBO.IRFrame.GetTemperature(rawMarker, out OWBMarkerTemperaturePoint tempPoint))
                        {
                            rawMarker.Value = tempPoint;
                            markers.Add(rawMarker);
                        }
                    }
                }
                string presetInfo = BuildPresetInfo(presetBO, CurrentCruiseAction);

                OSDInfoBO.RefreshFrame(sampleDataBO.IRFrame, sampleDataBO.DCFrame, markers, presetInfo);
            }
        }

        #endregion private methods
    }
}