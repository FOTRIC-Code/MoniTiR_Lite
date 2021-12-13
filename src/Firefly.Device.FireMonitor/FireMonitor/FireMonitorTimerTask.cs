// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

using Firefly.LogFactory;
using System.Threading;

namespace Firefly.Device.FireMonitor
{
    public class FireMonitorTimerTask : TimerTask
    {
        private FireMonitorDeviceEngine _deviceEngine;
        private ILogger<FireMonitorTimerTask> _logger = DeviceLogFactory.GetLogger<FireMonitorTimerTask>();

        private int _cruiseID;
        private PauseType _pauseType;
        private int _cruiseResumInAccTime;
        private DateTime _cruiseResumInTime;
        private CruiseAction _currentCruiseAction;
        private CruiseAction _comingCruiseAction;

        internal CruiseAction CurrentCruiseAction => _currentCruiseAction;

        internal FireMonitorTimerTask(DeviceEngine deviceEngine)
        {
            Owner = deviceEngine;
            _deviceEngine = Owner as FireMonitorDeviceEngine;
        }

        public override void Start()
        {
            _pauseType = PauseType.None;
            _comingCruiseAction = CruiseAction.AutoCruiseRun;

            _logger.LogInformation($"StartTimerTask,{_deviceEngine.DeviceBO}");
            InternalCancellationTokenSource = new System.Threading.CancellationTokenSource();
            Task.Factory.StartNew(() => DoTask(InternalCancellationTokenSource.Token));
        }

        public override void Stop()
        {
            InternalCancellationTokenSource?.Cancel();
            InternalCancellationTokenSource = null;
        }

        public override void Pause(PauseType pauseType)
        {
            List<PauseType> noneCanGoTo = new List<PauseType> { PauseType.ExclusivePause, PauseType.InclusivePause, PauseType.AlarmPause };
            List<PauseType> exclusiveCanGoTo = new List<PauseType> { PauseType.None, PauseType.InclusivePause };
            List<PauseType> inclusiveCanGoTo = new List<PauseType> { PauseType.None, PauseType.ExclusivePause };
            List<PauseType> alarmCanGoTo = new List<PauseType> { PauseType.None, PauseType.ExclusivePause, PauseType.InclusivePause };

            switch (_pauseType)
            {
                case PauseType.None:
                    if (noneCanGoTo.Contains(pauseType))
                    {
                        _pauseType = pauseType;
                    }
                    break;

                case PauseType.ExclusivePause:
                    if (exclusiveCanGoTo.Contains(pauseType))
                    {
                        _pauseType = pauseType;
                    }
                    break;

                case PauseType.InclusivePause:
                    if (inclusiveCanGoTo.Contains(pauseType))
                    {
                        _pauseType = pauseType;
                    }
                    break;

                case PauseType.AlarmPause:
                    if (alarmCanGoTo.Contains(pauseType) && _currentCruiseAction == CruiseAction.AutoCruiseRun)
                    {
                        _pauseType = pauseType;
                        if (_deviceEngine.OnMonitorEvent != null)
                        {
                            _deviceEngine.OnMonitorEvent(this, new DeviceMonitorArgs(_deviceEngine.DeviceBO.ID, _deviceEngine.DeviceBO.Name, DeviceEventType.CruiseStatus, new FireMonitorCruiseBO(_deviceEngine.CurrentCruiseID, PauseType.AlarmPause, _deviceEngine.CurrentCruiseName, false), null));
                        }
                    }
                    break;
            }

            _logger.LogInformation($"PauseTimerTask,{_deviceEngine.DeviceBO}, PauseType: {_pauseType}");
        }

        public override void Restore()
        {
            _pauseType = PauseType.None;
            if (_deviceEngine.OnMonitorEvent != null && _currentCruiseAction == CruiseAction.AutoCruiseRun)
            {
                _deviceEngine.OnMonitorEvent(this, new DeviceMonitorArgs(_deviceEngine.DeviceBO.ID, _deviceEngine.DeviceBO.Name, DeviceEventType.CruiseStatus, new FireMonitorCruiseBO(_deviceEngine.CurrentCruiseID, PauseType.None, _deviceEngine.CurrentCruiseName, true), null));
            }
            _logger.LogInformation($"PestoreTimerTask,{_deviceEngine.DeviceBO}");
        }

        public override void Manual(int cruiseID, CruiseCommand command)
        {
            switch (command)
            {
                case CruiseCommand.Start:
                    _comingCruiseAction = CruiseAction.ManualCruiseRun;
                    break;

                case CruiseCommand.Stop:
                    _comingCruiseAction = CruiseAction.ManualCruiseStop;
                    break;
            }
            _cruiseID = cruiseID;
        }

        public override bool AddInstantTask(TaskBO taskBO)
        {
            return false;
        }

        public override bool DeleteInstantTask(TaskBO taskBO)
        {
            return false;
        }

        #region private methods

        private async Task DoTask(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    FireMonitorCruiseRouteBO cruiseRouteModel = null;

                    switch (_comingCruiseAction)
                    {
                        case CruiseAction.InstantCruiseStop:
                        case CruiseAction.InstantCruiseRun:
                        case CruiseAction.InstantCruiseWaiting:
                            _currentCruiseAction = CruiseAction.None;
                            _comingCruiseAction = CruiseAction.AutoCruiseRun;
                            break;

                        case CruiseAction.ManualCruiseStop:
                            _currentCruiseAction = CruiseAction.ManualCruiseStop;
                            _comingCruiseAction = CruiseAction.AutoCruiseRun;
                            break;

                        case CruiseAction.ManualCruiseRun:
                            cruiseRouteModel = FindCruiseByID(_cruiseID);
                            if (cruiseRouteModel != null)
                            {
                                _deviceEngine.CurrentCruiseID = cruiseRouteModel.ID;
                                _deviceEngine.CurrentCruiseName = cruiseRouteModel.Name;
                            }
                            _currentCruiseAction = CruiseAction.ManualCruiseRun;
                            _comingCruiseAction = CruiseAction.AutoCruiseRun;
                            break;

                        case CruiseAction.AutoCruiseStop:
                        case CruiseAction.AutoCruiseRun:
                            cruiseRouteModel = FindCurrentAutoCruise();
                            if (cruiseRouteModel == null
                                || _deviceEngine.DeviceConfigBO?.TaskScheduleBO == null
                                || !_deviceEngine.DeviceConfigBO.TaskScheduleBO.Enabled)
                            {
                                _currentCruiseAction = CruiseAction.None;
                                await Task.Delay(Constants.WAIT_TIMEOUT_DEFAULT);
                                continue;
                            }

                            _deviceEngine.CurrentCruiseID = cruiseRouteModel.ID;
                            _deviceEngine.CurrentCruiseName = cruiseRouteModel.Name;

                            _currentCruiseAction = CruiseAction.AutoCruiseRun;
                            _comingCruiseAction = CruiseAction.AutoCruiseRun;
                            break;

                        case CruiseAction.None:
                            _currentCruiseAction = CruiseAction.None;
                            _comingCruiseAction = CruiseAction.None;
                            break;
                    }

                    if (cruiseRouteModel != null)
                    {
                        await DoCruise(cancellationToken, cruiseRouteModel);
                    }

                    if (_deviceEngine.DeviceConfigBO?.GeneralConfigBO?.FixedMonitoring != true)
                    {
                        _deviceEngine.CurrentPresetID = Constants.GOING_PRESET;
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

        private FireMonitorCruiseRouteBO FindCurrentAutoCruise()
        {
            try
            {
                FireMonitorCruiseRouteBO cruiseRouteModel = null;
                FireMonitorDeviceConfigBO config = _deviceEngine?.DeviceConfigBO;
                if (config?.TaskScheduleBO?.TaskDayList == null)
                {
                    return null;
                }

                TimeSpan timeSpan = new TimeSpan(DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second);
                FireMonitorTaskDayBO taskDayOutputModel = config.TaskScheduleBO.TaskDayList.Find(a => a != null && a.DayOfWeek.IsToday());
                if (taskDayOutputModel?.TaskTimeList == null)
                {
                    return null;
                }

                List<FireMonitorTaskTimeBO> taskTimeList = taskDayOutputModel.TaskTimeList;
                for (int i = 0; i < taskTimeList.Count; i++)
                {
                    FireMonitorTaskTimeBO taskTime = taskTimeList[i];
                    if (taskTime == null)
                    {
                        continue;
                    }

                    if (timeSpan >= taskTime.From.ConvertToTimeSpan() && timeSpan <= taskTime.To.ConvertToTimeSpan())
                    {
                        cruiseRouteModel = config.CruiseRouteConfigBO?.CruiseRouteList?.Find(a => a != null && a.ID == taskTime.CruiseRouteID);
                        break;
                    }
                }

                return cruiseRouteModel;
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"FindCurrentAutoCruise failed: {ex.Message}");
                return null;
            }
        }

        private FireMonitorCruiseRouteBO FindCruiseByID(int cruiseID)
        {
            return _deviceEngine.DeviceConfigBO?.CruiseRouteConfigBO?.CruiseRouteList?.Find(a => a.ID == cruiseID);
        }

        private async Task DoCruise(CancellationToken cancellationToken, FireMonitorCruiseRouteBO cruiseRouteModel)
        {
            try
            {
                foreach (FireMonitorCruisePointBO cruisePointModel in cruiseRouteModel.CruisePointList)
                {
                    // go to next preset
                    PresetParamBO param = new PresetParamBO
                    {
                        Command = PresetCommand.GOTO,
                        PresetID = cruisePointModel.PresetID,
                        Speed = cruisePointModel.Speed,
                        AutoFocus = cruisePointModel.AutoFocus
                    };

                    await Owner.PTZPreset(param);

                    // Pop up an event to notify app current preset is changed.
                    if (_deviceEngine.OnMonitorEvent != null)
                    {
                        FireMonitorPresetBO presetBO = _deviceEngine.DeviceConfigBO.PresetConfigBO.Get(cruisePointModel.PresetID);
                        if (presetBO != null)
                        {
                            _deviceEngine.OnMonitorEvent(this, new DeviceMonitorArgs(_deviceEngine.DeviceBO.ID, _deviceEngine.DeviceBO.Name, DeviceEventType.CruisePosition, new FireCruisePositionBO(presetBO.ID, presetBO.Name), null));
                        }
                    }

                    // Must be go out:
                    // 1: task is canceled.
                    // 2: user set new cruise action is great than current executing cruise action
                    bool isExit = false;
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        if (_currentCruiseAction < _comingCruiseAction)
                        {
                            isExit = true;
                            break;
                        }

                        if (_currentCruiseAction == CruiseAction.ManualCruiseRun)
                        {
                            if (_pauseType == PauseType.None || _pauseType == PauseType.AlarmPause)
                            {
                                if (CheckPresetWaitTimeout(cruisePointModel))
                                {
                                    break;
                                }
                            }
                            else
                            {
                                isExit = true;
                                break;
                            }
                        }
                        if (_currentCruiseAction == CruiseAction.AutoCruiseRun)
                        {
                            if (_pauseType == PauseType.None)
                            {
                                // go to next preset if stay current preset time is timeout
                                if (CheckPresetWaitTimeout(cruisePointModel))
                                {
                                    break;
                                }
                            }
                            else if (_pauseType == PauseType.ExclusivePause)
                            {
                                // nothing to do
                            }
                            else if (_pauseType == PauseType.InclusivePause)
                            {
                                if (_cruiseResumInTime == DateTime.MinValue)
                                {
                                    // nothing to do
                                }
                                else
                                {
                                    _cruiseResumInAccTime += (int)(DateTime.UtcNow - _cruiseResumInTime).TotalSeconds;
                                }

                                _cruiseResumInTime = DateTime.UtcNow;

                                if (_cruiseResumInAccTime >= _deviceEngine.DeviceConfigBO.TaskScheduleBO.RestoreTime)
                                {
                                    _cruiseResumInTime = DateTime.MinValue;
                                    _pauseType = PauseType.None;
                                    _cruiseResumInAccTime = 0;
                                }
                            }
                            else if (_pauseType == PauseType.AlarmPause)
                            {
                                // input alarm signal is on: clear alarm pause
                                // input alarm signal is off: keep waiting
                                DeviceStatusBO deviceStatusBO = await _deviceEngine.GetStatus();
                                switch (_deviceEngine.CurrentAlarmLevel)
                                {
                                    case FireMonitorAlarmLevel.Alarm:
                                        foreach (int channelIndex in _deviceEngine.DeviceConfigBO.EventTriggerBO.AlarmOutList)
                                        {
                                            if (deviceStatusBO.AlarmInStatus[channelIndex])
                                            {
                                                _pauseType = PauseType.None;
                                                break;
                                            }
                                        }
                                        break;

                                    case FireMonitorAlarmLevel.Alert1:
                                        foreach (int channelIndex in _deviceEngine.DeviceConfigBO.EventTriggerBO.Alert1OutList)
                                        {
                                            if (deviceStatusBO.AlarmInStatus[channelIndex])
                                            {
                                                _pauseType = PauseType.None;
                                                break;
                                            }
                                        }
                                        break;

                                    case FireMonitorAlarmLevel.Alert2:
                                        foreach (int channelIndex in _deviceEngine.DeviceConfigBO.EventTriggerBO.Alert2OutList)
                                        {
                                            if (deviceStatusBO.AlarmInStatus[channelIndex])
                                            {
                                                _pauseType = PauseType.None;
                                                break;
                                            }
                                        }
                                        break;
                                }
                            }
                        }

                        await Task.Delay(Constants.WAIT_TIMEOUT_DEFAULT);
                    }

                    if (isExit || cancellationToken.IsCancellationRequested)
                    {
                        _pauseType = PauseType.None;
                        break;
                    }
                }
            }
            finally
            {
                _logger.LogInformation($"DoCruise,{_deviceEngine.DeviceBO},Stopped");
            }
        }

        private bool CheckPresetWaitTimeout(FireMonitorCruisePointBO cruisePointModel)
        {
            if ((DateTime.UtcNow - _deviceEngine.ArrivalTime).TotalSeconds >= cruisePointModel.WaitTime)
            {
                return true;
            }
            return false;
        }

        #endregion private methods
    }
}