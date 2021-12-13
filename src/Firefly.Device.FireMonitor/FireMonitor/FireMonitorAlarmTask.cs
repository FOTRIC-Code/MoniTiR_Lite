// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.Json;

using Microsoft.Extensions.Logging;

using OWB;
using Amur.Common;
using OWB.DataFrame;
using Firefly.LogFactory;
using System.Threading;

namespace Firefly.Device.FireMonitor
{
    public class FireMonitorAlarmTask : AlarmTask
    {
        private FireMonitorDeviceEngine _deviceEngine;
        private ILogger<FireMonitorAlarmTask> _logger = DeviceLogFactory.GetLogger<FireMonitorAlarmTask>();

        private DateTime _preSampleTime;
        private Dictionary<string, FireMonitorPredictAlarmBO> _predictAlarmModels;
        private List<FireMonitorAlarmLevel> _lastAlarmLevels;

        internal FireMonitorAlarmTask(DeviceEngine deviceEngine)
        {
            Owner = deviceEngine;
            _deviceEngine = Owner as FireMonitorDeviceEngine;
            _predictAlarmModels = new Dictionary<string, FireMonitorPredictAlarmBO>();
            _lastAlarmLevels = new List<FireMonitorAlarmLevel>();
        }

        public override void Start()
        {
            if (!(_deviceEngine.DeviceConfigBO?.AlarmScheduleBO?.Enabled ?? false))
            {
                _logger.LogWarning($"StartAlarmTask,{_deviceEngine.DeviceBO},Start alarm task failed: alarm schedule is disabled. this.HasCode{this.GetHashCode()}");
                return;
            }

            _logger.LogInformation($"StartAlarmTask,{_deviceEngine.DeviceBO},Start AlarmTask this.HasCode{this.GetHashCode()}");
            _predictAlarmModels.Clear();
            _lastAlarmLevels.Clear();
            InternalCancelTokenSource = new System.Threading.CancellationTokenSource();
            Task.Factory.StartNew(() => DoTask(InternalCancelTokenSource.Token));
        }

        public override void Stop()
        {
            InternalCancelTokenSource?.Cancel();
            InternalCancelTokenSource = null;
        }

        #region internal methods

        internal List<FireAlarmMarkerBO> CheckAlarm(List<FireMonitorMarkerBO> markerBOs, List<OWBMarker> markers)
        {
            List<FireAlarmMarkerBO> alarmMarkers = new List<FireAlarmMarkerBO>();
            FireMonitorAlarmLevel markerAlarmLevel = FireMonitorAlarmLevel.None;
            float alarmTemp = OWBMath.KelvinKelvinZero;
            foreach (FireMonitorMarkerBO markerBO in markerBOs)
            {
                FireAlarmMarkerBO alarmMarker = new FireAlarmMarkerBO();

                foreach (OWBMarker marker in markers)
                {
                    if (marker.Name.Equals(markerBO.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        alarmMarker.MarkerName = markerBO.Name;
                        alarmMarker.MarkerType = (MarkerType)marker.MarkerType;
                        alarmMarker.PointList = markerBO.PointList;
                        alarmMarker.TempPoint = marker.Value;

                        FireMonitorAlarmRuleBO alarmRuleBO = _deviceEngine.DeviceConfigBO.AlarmRuleConfigBO.AlarmRuleList.Find(a => a.ID == markerBO.AlarmRuleID);
                        if (alarmRuleBO != null)
                        {
                            // TODO: need to implement temp sudden
                            //if (alarmRuleBO.TempSuddenChange != null && alarmRuleBO.TempSuddenChange.Enabled)
                            //{
                            //    TempSuddenChangeTrigger();
                            //}

                            switch (alarmRuleBO.RuleType)
                            {
                                case RuleType.HighestGreater:
                                    markerAlarmLevel = GetAlarmLevel(RuleCompareType.Greater, marker.Value.Max.Value, alarmRuleBO);
                                    alarmTemp = marker.Value.Max.Value;
                                    break;

                                case RuleType.HighestLess:
                                    markerAlarmLevel = GetAlarmLevel(RuleCompareType.Less, marker.Value.Max.Value, alarmRuleBO);
                                    alarmTemp = marker.Value.Max.Value;
                                    break;

                                case RuleType.AverageGreater:
                                    markerAlarmLevel = GetAlarmLevel(RuleCompareType.Greater, marker.Value.Avg, alarmRuleBO);
                                    alarmTemp = marker.Value.Avg;
                                    break;

                                case RuleType.AverageLess:
                                    markerAlarmLevel = GetAlarmLevel(RuleCompareType.Less, marker.Value.Avg, alarmRuleBO);
                                    alarmTemp = marker.Value.Avg;
                                    break;

                                case RuleType.LowestGreater:
                                    markerAlarmLevel = GetAlarmLevel(RuleCompareType.Greater, marker.Value.Min.Value, alarmRuleBO);
                                    alarmTemp = marker.Value.Min.Value;
                                    break;

                                case RuleType.LowestLess:
                                    markerAlarmLevel = GetAlarmLevel(RuleCompareType.Less, marker.Value.Min.Value, alarmRuleBO);
                                    alarmTemp = marker.Value.Min.Value;
                                    break;

                                case RuleType.DiffTempGreater:
                                    markerAlarmLevel = GetAlarmLevel(RuleCompareType.Greater, marker.Value.Max.Value - marker.Value.Min.Value, alarmRuleBO);
                                    alarmTemp = marker.Value.Max.Value - marker.Value.Min.Value;
                                    break;

                                case RuleType.DiffTempLess:
                                    markerAlarmLevel = GetAlarmLevel(RuleCompareType.Less, marker.Value.Max.Value - marker.Value.Min.Value, alarmRuleBO);
                                    alarmTemp = marker.Value.Max.Value - marker.Value.Min.Value;
                                    break;
                            }

                            alarmMarker.AlarmTemp = alarmTemp;
                            alarmMarker.SetAlarmRule(alarmRuleBO);
                            alarmMarker.SetAlarmLevel(markerAlarmLevel);
                            if (alarmMarker.RuleType == RuleType.DiffTempGreater || alarmMarker.RuleType == RuleType.DiffTempLess)
                            {
                                alarmMarker.MarkerDesc = new TemperatureDifference(alarmMarker.AlarmTemp, TemperatureUnit.Kelvin).ToString();
                            }
                            else
                            {
                                alarmMarker.MarkerDesc = new Temperature(alarmMarker.AlarmTemp, TemperatureUnit.Kelvin).ToString();
                            }
                        }

                        alarmMarkers.Add(alarmMarker);
                        break;
                    }
                }
            }

            return alarmMarkers;
        }

        internal void ChangeAlarmDataByScript(int presetID, string presetName, OWBIRFrame irFrame, OWBDCFrame dcFrame, ref List<FireAlarmMarkerBO> alarmMarkers)
        {
            try
            {
                if (FireflyScriptSingleton.Instance.FireflyScript != null)
                {
                    List<FireAlarmMarkerBO> copyAlarmMarkers = alarmMarkers.Select(it => it.Clone()).ToList();
                    ScriptAlarmDataBO alarmDataBO = new ScriptAlarmDataBO(_deviceEngine.DeviceBO.ID,
                        _deviceEngine.DeviceBO.Name,
                        presetID,
                        presetName,
                        irFrame,
                        dcFrame,
                        copyAlarmMarkers);
                    bool hasChanged = false;
                    Task task = Task.Run(() =>
                    {
                        FireflyScriptSingleton.Instance.FireflyScript.CheckAlarm(alarmDataBO);
                        hasChanged = true;
                    });
                    task.Wait(new TimeSpan(0, 0, 0, 0, (int)_deviceEngine.DeviceConfigBO.GeneralConfigBO.SampleInterval * 1000));
                    if (hasChanged)
                    {
                        foreach (FireAlarmMarkerBO alarmMarker in copyAlarmMarkers)
                        {
                            if (alarmMarker.AlarmLevel != FireMonitorAlarmLevel.None
                                && alarmMarker.AlarmLevel != FireMonitorAlarmLevel.Alert1
                                && alarmMarker.AlarmLevel != FireMonitorAlarmLevel.Alert2
                                && alarmMarker.AlarmLevel != FireMonitorAlarmLevel.Alarm)
                            {
                                _logger.LogWarning($"AlarmCustomScript,{_deviceEngine.DeviceBO},AlarmLevel-{alarmMarker.AlarmLevel} | 不是红外监控服务中约定的报警等级列表（None=0,Alert1=1,Alert2=2,Alarm=3)");
                                return;
                            }

                            FireAlarmMarkerBO originalAlarmMarker = alarmMarkers.FirstOrDefault(it => string.Equals(it.MarkerName, alarmMarker.MarkerName));
                            if (originalAlarmMarker != null)
                            {
                                originalAlarmMarker.AlarmTemp = alarmMarker.AlarmTemp;
                                originalAlarmMarker.SetAlarmLevel(alarmMarker.AlarmLevel);
                            }
                        }
                    }
                    else
                    {
                        _logger.LogWarning($"AlarmCustomScript,{_deviceEngine.DeviceBO},CheckAlarm方法处理超时，允许的超时时间是{(int)_deviceEngine.DeviceConfigBO.GeneralConfigBO.SampleInterval * 1000}毫秒");
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogError($"AlarmCustomScript,{_deviceEngine.DeviceBO},{e.ToString()}");
            }
        }

        internal FireMonitorAlarmLevel GetCurrentMaxAlarmLevel()
        {
            if (!_lastAlarmLevels.Any())
            {
                return FireMonitorAlarmLevel.None;
            }
            return _lastAlarmLevels.Max(x => x);
        }

        internal Task<bool> ResetAlarmStatus()
        {
            _predictAlarmModels.Clear();
            _lastAlarmLevels.Clear();
            return Task.FromResult(true);
        }

        #endregion internal methods

        #region private methods

        private async Task DoTask(CancellationToken cancellationToken)
        {
            int latestPresetID = Constants.GOING_PRESET;

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        DateTime happenTime = DateTime.UtcNow;
                        bool isArmingTime = CheckAlarmSchedule(out FireMonitorAlarmLevel alarmLevel);
                        if (_deviceEngine.GetSampleDataBO() != null && _deviceEngine.CurrentPresetEnabled)
                        {
                            SampleDataBO sampleDataBO = _deviceEngine.GetSampleDataBO();

                            FireMonitorPresetBO presetBO = _deviceEngine.DeviceConfigBO.PresetConfigBO.PresetList.Find(a => a.ID == _deviceEngine.CurrentPresetID);

                            if (_preSampleTime != _deviceEngine.SampleTime)
                            {
                                _logger.LogInformation($"DoAlarmTask,{_deviceEngine.DeviceBO}, Frame null:{sampleDataBO.IRFrame == null}, SampleTime:{_preSampleTime:HH:MM:ss.fff} -> {_deviceEngine.SampleTime:HH:MM:ss.fff}");
                                foreach (var marker in sampleDataBO.Markers)
                                {
                                    _logger.LogInformation($"DoAlarmTask,{_deviceEngine.DeviceBO}, Marker:{marker.Name}, Value:{(marker.Value.Max.Value - 273.15):F2} / {(marker.Value.Min.Value - 273.15):F2} / {(marker.Value.Avg - 273.15):F2}");
                                }
                            }

                            if (_deviceEngine.DeviceConfigBO?.TaskScheduleBO?.Enabled ?? false)
                            {
                                _logger.LogInformation($"DoAlarmTask,{_deviceEngine.DeviceBO}, PresetID:{presetBO?.ID}, ArrivalTime:{_deviceEngine.ArrivalTime}");
                            }

                            if (sampleDataBO.IRFrame != null && presetBO != null && _preSampleTime != _deviceEngine.SampleTime && _deviceEngine.SampleTime > _deviceEngine.ArrivalTime)
                            {
                                // if device go to another preset, need to clear predict alarm model and use new marker to init.
                                if (_predictAlarmModels.Count == 0 || latestPresetID != presetBO.ID)
                                {
                                    InitPredictAlarmModels(presetBO.MarkerList);
                                    latestPresetID = presetBO.ID;
                                }

                                bool isInstantCruiseAction = _deviceEngine.CurrentCruiseAction == CruiseAction.InstantCruiseRun;
                                if ((isArmingTime && _deviceEngine.DeviceConfigBO.AlarmScheduleBO.Enabled) || isInstantCruiseAction)
                                {
                                    DoAlarmData(happenTime, sampleDataBO, presetBO, alarmLevel);
                                }

                                _preSampleTime = _deviceEngine.SampleTime;
                            }
                        }
                        else
                        {
                            _predictAlarmModels.Clear();
                            DoRefreshOSDInfo(null);
                            if (!isArmingTime && _lastAlarmLevels.Any(x => x != FireMonitorAlarmLevel.None))
                            {
                                _lastAlarmLevels.Clear();
                                await SetEventTrigger(_lastAlarmLevels);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex.ToString());
                    }
                    finally
                    {
                        await Task.Delay(Constants.WAIT_TIMEOUT_DEFAULT);
                    }
                }
            }
            finally
            {
                _logger.LogInformation($"DoAlarmTask,{_deviceEngine.DeviceBO}, Stopped");
            }
        }

        private void DoAlarmData(DateTime happenTime, SampleDataBO sampleDataBO, FireMonitorPresetBO presetBO, FireMonitorAlarmLevel pauseAlarmLevel)
        {
            List<FireAlarmMarkerBO> alarmMarkers = CheckAlarm(presetBO.MarkerList, sampleDataBO.Markers);

            ChangeAlarmDataByScript(presetBO.ID, presetBO.Name, sampleDataBO.IRFrame, sampleDataBO.DCFrame, ref alarmMarkers);

            #region alarm data

            FireMonitorAlarmBO fireMonitorAlarmBO = new FireMonitorAlarmBO(sampleDataBO.Timestamp,
                _deviceEngine.DeviceBO.ID,
                _deviceEngine.DeviceBO.Name,
                happenTime,
                DateTime.UtcNow,
                presetBO,
                _deviceEngine.DeviceBO.ProtocolType,
                DeviceConfigBO.AlarmRootPath);

            fireMonitorAlarmBO.MarkerList = new List<FireAlarmMarkerBO>();

            foreach (FireAlarmMarkerBO alarmMarker in alarmMarkers)
            {
                if (alarmMarker.AlarmRule == null)
                {
                    continue;
                }

                #region alarm

                FireMonitorPredictAlarmBO predictAlarmModel = _predictAlarmModels[alarmMarker.MarkerName];

                // never change alarm level if alarm temperature is arround with tolerance.
                if (Math.Abs(alarmMarker.AlarmTemp - predictAlarmModel.OutputAlarmTemp) <= alarmMarker.AlarmRule.ToleranceTemp && alarmMarker.AlarmLevel < predictAlarmModel.LastAlarmLevel)
                {
                    alarmMarker.SetAlarmLevel(predictAlarmModel.LastAlarmLevel);
                }

                // pause cruise according to alarm level, and never alarm if alarm level is none
                if (alarmMarker.AlarmLevel >= pauseAlarmLevel && pauseAlarmLevel != FireMonitorAlarmLevel.None)
                {
                    _deviceEngine.CurrentAlarmLevel = alarmMarker.AlarmLevel;
                    _deviceEngine.InternalPauseCruiseTask(PauseType.AlarmPause);
                }

                int intervalSeconds = (int)(_deviceEngine.SampleTime - _preSampleTime).TotalSeconds;

                alarmMarker.DeviceEventType = HasReachedAlarmLevel(predictAlarmModel, alarmMarker, intervalSeconds);

                #endregion alarm

                #region post alarm

                if (alarmMarker.DeviceEventType != DeviceEventType.None && _deviceEngine.OnMonitorEvent != null)
                {
                    alarmMarker.SetAlarmLevel(predictAlarmModel.CurrentAlarmLevel);
                    fireMonitorAlarmBO.MarkerList.Add(alarmMarker);
                }

                #endregion post alarm
            }

            if (fireMonitorAlarmBO.MarkerList.Count > 0)
            {
                string irAlarmFileName = string.Empty;
                string dcAlarmFileName = string.Empty;
                List<ImageInfoBO> imageInfoBOList = new List<ImageInfoBO>();

                #region save file

                if (sampleDataBO.IRFrame != null)
                {
                    // apply rule temp to marker
                    foreach (FireAlarmMarkerBO alarmMarker in alarmMarkers)
                    {
                        foreach (OWBMarker marker in sampleDataBO.Markers)
                        {
                            if (alarmMarker.MarkerName.Equals(marker.Name, StringComparison.OrdinalIgnoreCase))
                            {
                                marker.MarkerDesc = alarmMarker.MarkerDesc;
                                break;
                            }
                        }
                    }
                    SaveFileBO irSaveFileBO = new SaveFileBO(SaveFileType.Alarm, _deviceEngine.DeviceBO.ID, _deviceEngine.DeviceBO.Name, presetBO.ID, 0, ChannelType.IRCamera, happenTime.ToLocalTime(), sampleDataBO.IRFrame, null, _deviceEngine.IRExpectedImageWidth, _deviceEngine.IRExpectedImageHeight, sampleDataBO.Markers);

                    irAlarmFileName = DeviceConfigBO.SaveFile(irSaveFileBO);
                    imageInfoBOList.Add(new ImageInfoBO(0, ChannelType.IRCamera, sampleDataBO.IRFrame.FileType, irAlarmFileName, DeviceConfigBO.AlarmRootPath));
                }

                if (sampleDataBO.DCFrame != null)
                {
                    SaveFileBO dcSaveFileBO = new SaveFileBO(SaveFileType.Alarm, _deviceEngine.DeviceBO.ID, _deviceEngine.DeviceBO.Name, presetBO.ID, 1, ChannelType.DCCamera, happenTime.ToLocalTime(), null, sampleDataBO.DCFrame, _deviceEngine.ExpectedImageWidth, _deviceEngine.ExpectedImageHeight, new List<OWBMarker>());

                    dcAlarmFileName = DeviceConfigBO.SaveFile(dcSaveFileBO);
                    imageInfoBOList.Add(new ImageInfoBO(1, ChannelType.DCCamera, sampleDataBO.DCFrame.FileType, dcAlarmFileName, DeviceConfigBO.AlarmRootPath));
                }

                #endregion save file

                _deviceEngine.OnMonitorEvent?.Invoke(this, new DeviceMonitorArgs(_deviceEngine.DeviceBO.ID, _deviceEngine.DeviceBO.Name, DeviceEventType.Alarm, fireMonitorAlarmBO, imageInfoBOList));
            }

            DoRefreshOSDInfo(fireMonitorAlarmBO);

            #endregion alarm data

            #region change channel relay_out status

            List<FireMonitorAlarmLevel> curAlarmLevel = _predictAlarmModels.Select(x => x.Value.CurrentAlarmLevel).Distinct().ToList();
            if (curAlarmLevel.Except(_lastAlarmLevels).Any() || _lastAlarmLevels.Except(curAlarmLevel).Any())
            {
                _lastAlarmLevels = curAlarmLevel;
                _ = SetEventTrigger(curAlarmLevel);
            }

            #endregion change channel relay_out status
        }

        private void DoRefreshOSDInfo(FireMonitorAlarmBO fireMonitorAlarmBO)
        {
            string alarmContent = string.Empty;
            if (fireMonitorAlarmBO != null && fireMonitorAlarmBO.MarkerList.Count > 0)
            {
                foreach (FireAlarmMarkerBO alarmMarker in fireMonitorAlarmBO.MarkerList)
                {
                    float threadholdTempValue = alarmMarker.AlarmRule?.GetThreadholdTemp(alarmMarker.AlarmLevel) ?? 0.0f;
                    string threadholdTemp = new Temperature(threadholdTempValue, TemperatureUnit.Kelvin).ToString();
                    string alarmTemp = new Temperature(alarmMarker.AlarmTemp, TemperatureUnit.Kelvin).ToString();

                    string alarmLevelDesc = string.Empty;
                    switch (alarmMarker.AlarmLevel)
                    {
                        case FireMonitorAlarmLevel.Alert1:
                            alarmLevelDesc = I18n.Properties.Resources.Alert1;
                            break;

                        case FireMonitorAlarmLevel.Alert2:
                            alarmLevelDesc = I18n.Properties.Resources.Alert2;
                            break;

                        case FireMonitorAlarmLevel.Alarm:
                            alarmLevelDesc = I18n.Properties.Resources.Alarm;
                            break;
                    }

                    string alarmRuleDesc = string.Empty;
                    switch (alarmMarker.RuleType)
                    {
                        case RuleType.HighestGreater:
                            alarmRuleDesc = I18n.Properties.Resources.HighestGreater;
                            break;

                        case RuleType.HighestLess:
                            alarmRuleDesc = I18n.Properties.Resources.HighestLess;
                            break;

                        case RuleType.LowestGreater:
                            alarmRuleDesc = I18n.Properties.Resources.LowestGreater;
                            break;

                        case RuleType.LowestLess:
                            alarmRuleDesc = I18n.Properties.Resources.LowestLess;
                            break;

                        case RuleType.AverageGreater:
                            alarmRuleDesc = I18n.Properties.Resources.AverageGreater;
                            break;

                        case RuleType.AverageLess:
                            alarmRuleDesc = I18n.Properties.Resources.AverageLess;
                            break;

                        case RuleType.DiffTempGreater:
                            alarmRuleDesc = I18n.Properties.Resources.DiffTempGreater;
                            threadholdTemp = new TemperatureDifference(threadholdTempValue, TemperatureUnit.Kelvin).ToString();
                            alarmTemp = new TemperatureDifference(alarmMarker.AlarmTemp, TemperatureUnit.Kelvin).ToString();
                            break;

                        case RuleType.DiffTempLess:
                            alarmRuleDesc = I18n.Properties.Resources.DiffTempLess;
                            threadholdTemp = new TemperatureDifference(threadholdTempValue, TemperatureUnit.Kelvin).ToString();
                            alarmTemp = new TemperatureDifference(alarmMarker.AlarmTemp, TemperatureUnit.Kelvin).ToString();
                            break;
                    }

                    alarmContent += string.Format(I18n.Properties.Resources.AlarmContentDesc, alarmLevelDesc, fireMonitorAlarmBO.DeviceName, alarmMarker.MarkerName, fireMonitorAlarmBO.Preset.ID, alarmTemp, alarmRuleDesc, threadholdTemp, Constants.WRAP_LINE);
                }

                alarmContent.TrimEnd(Constants.WRAP_LINE.ToCharArray());
            }

            _deviceEngine.OSDInfoBO.RefreshAlarm(alarmContent);
        }

        private async Task SetEventTrigger(List<FireMonitorAlarmLevel> allMarkerAlarmLevel)
        {
            DeviceParamBO deviceParamBO = await _deviceEngine.GetParam();
            List<AlarmOutBO> alarmOutBOs = new List<AlarmOutBO>();
            for (int i = 1; i <= deviceParamBO.HardwareAbilityBO.AlarmOutPortNum; i++)
            {
                //能触发当前通道的所有报警等级
                List<FireMonitorAlarmLevel> alarmLevelList = new List<FireMonitorAlarmLevel>();
                if (_deviceEngine.DeviceConfigBO.EventTriggerBO.Alert1OutList.Any(x => x == i))
                {
                    alarmLevelList.Add(FireMonitorAlarmLevel.Alert1);
                }
                if (_deviceEngine.DeviceConfigBO.EventTriggerBO.Alert2OutList.Any(x => x == i))
                {
                    alarmLevelList.Add(FireMonitorAlarmLevel.Alert2);
                }
                if (_deviceEngine.DeviceConfigBO.EventTriggerBO.AlarmOutList.Any(x => x == i))
                {
                    alarmLevelList.Add(FireMonitorAlarmLevel.Alarm);
                }

                //任何Marker的报警等级能触发当前通道，则当前通道输出开启。
                if (alarmLevelList.Intersect(allMarkerAlarmLevel).Any())
                {
                    alarmOutBOs.Add(new AlarmOutBO(i, ChannelCommand.High));
                }
            }
            await _deviceEngine.SetAlarmOut(alarmOutBOs);
        }

        private bool CheckAlarmSchedule(out FireMonitorAlarmLevel alarmLevel)
        {
            bool flag = false;
            alarmLevel = FireMonitorAlarmLevel.None;

            TimeSpan timeSpan = new TimeSpan(DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second);

            FireMonitorAlarmDayBO alarmDayBO = _deviceEngine.DeviceConfigBO.AlarmScheduleBO.AlarmDayList.Find(a => a.DayOfWeek.IsToday());
            if (alarmDayBO != null)
            {
                List<FireMonitorAlarmTimeBO> alarmTimeBOList = alarmDayBO.AlarmTimeList;
                if (alarmTimeBOList != null)
                {
                    int index = -1;
                    for (int i = 0; i < alarmTimeBOList.Count; i++)
                    {
                        if (timeSpan <= alarmTimeBOList[i].To.ConvertToTimeSpan())
                        {
                            index = i;
                            break;
                        }
                    }
                    if (index != -1)
                    {
                        if (timeSpan >= alarmTimeBOList[index].From.ConvertToTimeSpan())
                        {
                            flag = true;
                            alarmLevel = alarmTimeBOList[index].PauseType;
                        }
                        else
                        {
                            flag = false;
                        }
                    }
                }
            }
            return flag;
        }

        /// <summary>
        /// Can post alarm: current alarm level is greater than last alarm level
        /// </summary>
        /// <param name="alarmMarker"></param>
        /// <param name="intervalSeconds"></param>
        /// <returns></returns>
        private DeviceEventType HasReachedAlarmLevel(FireMonitorPredictAlarmBO predictAlarmModel, FireAlarmMarkerBO alarmMarker, int intervalSeconds)
        {
            string predictAlarmDesc = string.Empty;
            predictAlarmModel.CurrentAlarmLevel = FireMonitorAlarmLevel.None;

            foreach (FireMonitorAlarmLevel rawLevel in Enum.GetValues(typeof(FireMonitorAlarmLevel)))
            {
                if (predictAlarmModel.Model.ContainsKey(rawLevel))
                {
                    if (alarmMarker.AlarmLevel >= rawLevel)
                    {
                        predictAlarmModel.Model[rawLevel] = (predictAlarmModel.Model[rawLevel] == -1) ? 0 : predictAlarmModel.Model[rawLevel] + intervalSeconds;
                    }
                    else
                    {
                        predictAlarmModel.Model[rawLevel] = -1;
                    }

                    switch (rawLevel)
                    {
                        case FireMonitorAlarmLevel.Alert1:
                            if (predictAlarmModel.Model[rawLevel] >= alarmMarker.AlarmRule.Alert1FilteringTime)
                            {
                                if (FireMonitorAlarmLevel.Alert1 > predictAlarmModel.CurrentAlarmLevel)
                                {
                                    predictAlarmModel.CurrentAlarmLevel = FireMonitorAlarmLevel.Alert1;
                                }
                            }
                            break;

                        case FireMonitorAlarmLevel.Alert2:
                            if (predictAlarmModel.Model[rawLevel] >= alarmMarker.AlarmRule.Alert2FilteringTime)
                            {
                                if (FireMonitorAlarmLevel.Alert2 > predictAlarmModel.CurrentAlarmLevel)
                                {
                                    predictAlarmModel.CurrentAlarmLevel = FireMonitorAlarmLevel.Alert2;
                                }
                            }
                            break;

                        case FireMonitorAlarmLevel.Alarm:
                            if (predictAlarmModel.Model[rawLevel] >= alarmMarker.AlarmRule.AlarmFilteringTime)
                            {
                                if (FireMonitorAlarmLevel.Alarm > predictAlarmModel.CurrentAlarmLevel)
                                {
                                    predictAlarmModel.CurrentAlarmLevel = FireMonitorAlarmLevel.Alarm;
                                }
                            }
                            break;
                    }
                }
            }

            DeviceEventType eventType = DeviceEventType.None;
            if (predictAlarmModel.CurrentAlarmLevel != predictAlarmModel.LastAlarmLevel)
            {
                //报警等级变化=》AlarmStatusChanged
                eventType = DeviceEventType.AlarmStatusChanged;
            }
            if (predictAlarmModel.CurrentAlarmLevel > predictAlarmModel.LastAlarmLevel)
            {
                //报警等级升级=》Alarm
                eventType = DeviceEventType.Alarm;
            }

            predictAlarmModel.OutputAlarmTemp = predictAlarmModel.CurrentAlarmLevel != FireMonitorAlarmLevel.None ? alarmMarker.AlarmTemp : OWBMath.KelvinKelvinZero;

            foreach (FireMonitorAlarmLevel level in Enum.GetValues(typeof(FireMonitorAlarmLevel)))
            {
                if (predictAlarmModel.Model.ContainsKey(level))
                {
                    switch (level)
                    {
                        case FireMonitorAlarmLevel.Alert1:
                            predictAlarmDesc += $"{level}Time: {predictAlarmModel.Model[level]}s / {alarmMarker.AlarmRule.Alert1FilteringTime}s, ";
                            break;

                        case FireMonitorAlarmLevel.Alert2:
                            predictAlarmDesc += $"{level}Time: {predictAlarmModel.Model[level]}s / {alarmMarker.AlarmRule.Alert2FilteringTime}s, ";
                            break;

                        case FireMonitorAlarmLevel.Alarm:
                            predictAlarmDesc += $"{level}Time: {predictAlarmModel.Model[level]}s / {alarmMarker.AlarmRule.AlarmFilteringTime}s, ";
                            break;
                    }
                }
            }

            _logger.LogInformation($"HasReachedAlarmLevel,{_deviceEngine.DeviceBO}, {alarmMarker.MarkerName}：{predictAlarmModel.LastAlarmLevel} => {predictAlarmModel.CurrentAlarmLevel}, EventType: {eventType}, Value: {alarmMarker.AlarmTemp:F1}, {predictAlarmDesc}");

            predictAlarmModel.LastAlarmLevel = predictAlarmModel.CurrentAlarmLevel;

            return eventType;
        }

        private FireMonitorAlarmLevel GetAlarmLevel(RuleCompareType compareType, float value, FireMonitorAlarmRuleBO alarmRuleModel)
        {
            FireMonitorAlarmLevel level = FireMonitorAlarmLevel.None;
            switch (compareType)
            {
                case RuleCompareType.Greater:
                    if (value > alarmRuleModel.Alert1Temp)
                    {
                        level = FireMonitorAlarmLevel.Alert1;
                    }
                    if (value > alarmRuleModel.Alert2Temp)
                    {
                        level = FireMonitorAlarmLevel.Alert2;
                    }
                    if (value > alarmRuleModel.AlarmTemp)
                    {
                        level = FireMonitorAlarmLevel.Alarm;
                    }
                    break;

                case RuleCompareType.Less:
                    if (value < alarmRuleModel.Alert1Temp)
                    {
                        level = FireMonitorAlarmLevel.Alert1;
                    }
                    if (value < alarmRuleModel.Alert2Temp)
                    {
                        level = FireMonitorAlarmLevel.Alert2;
                    }
                    if (value < alarmRuleModel.AlarmTemp)
                    {
                        level = FireMonitorAlarmLevel.Alarm;
                    }
                    break;
            }
            return level;
        }

        private void InitPredictAlarmModels(List<FireMonitorMarkerBO> markerList)
        {
            _predictAlarmModels.Clear();

            foreach (FireMonitorMarkerBO marker in markerList)
            {
                _predictAlarmModels[marker.Name] = new FireMonitorPredictAlarmBO();
            }
        }

        #endregion private methods
    }
}