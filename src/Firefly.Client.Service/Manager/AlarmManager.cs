// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using log4net;
using Microsoft.AspNetCore.SignalR.Client;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Firefly.Client.Service.LiteDb;

namespace Firefly.Client.Service
{
    public class AlarmManager
    {
        private const int CHECK_CONNECTION_PERIOD = 10 * 1000;
        public const int MAX_REALTIME_ALARM_COUNT = 100;
        private HubConnection _hub;
        private Timer _checkHubConnectionTimer;
        private static AlarmManager alarmManager = new AlarmManager();
        public static AlarmManager Instance => alarmManager;

        private ConcurrentDictionary<int, HubConnection> _regionHubConnectionDict;

        /// <summary>
        /// Fire Monitor Agent base URL for realtime Hub only.
        /// History / confirm use shared SQLite.
        /// </summary>
        public string ServerURL { get; set; }

        public delegate void OnAlarmEventHandle(AlarmDO message);

        private List<AlarmDO> _alarmDOs;

        public event OnAlarmEventHandle AddOrUpdateAlarmEvent;

        private ILog _logger = LogManager.GetLogger(typeof(AlarmManager));

        private IDisposable _inProcAlarmSub;

        private AlarmManager()
        {
            _alarmDOs = new List<AlarmDO>();
            _regionHubConnectionDict = new ConcurrentDictionary<int, HubConnection>();
            LiteDatabase.EnsureInitialized();
        }

        private bool UseInProcCallbacks =>
            Firefly.Client.Service.Abstractions.FireRuntimeServices.Mode
            == Firefly.Client.Service.Abstractions.FireHostMode.InProcess;

        ~AlarmManager()
        {
            _checkHubConnectionTimer?.Dispose();
            _inProcAlarmSub?.Dispose();
        }

        private void HandleAlarmPayload(AlarmDO alarmDO)
        {
            try
            {
                if (alarmDO != null)
                {
                    LiteAlarmRepository.Upsert(alarmDO);
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Persist alarm to SQLite failed.", ex);
            }

            if (alarmDO == null)
            {
                return;
            }

            lock (_alarmDOs)
            {
                AlarmDO sameAlarmDO = _alarmDOs.FirstOrDefault(it => it.ID == alarmDO.ID);
                if (sameAlarmDO != null)
                {
                    _alarmDOs[_alarmDOs.IndexOf(sameAlarmDO)] = alarmDO;
                }
                else
                {
                    while (_alarmDOs.Count >= MAX_REALTIME_ALARM_COUNT)
                    {
                        _alarmDOs.RemoveAt(0);
                    }

                    _alarmDOs.Add(alarmDO);
                }
            }

            AddOrUpdateAlarmEvent?.Invoke(alarmDO);
        }

        private void Init()
        {
            if (UseInProcCallbacks)
            {
                return;
            }

            _hub = new HubConnectionBuilder().WithUrl(ServerURL + "/alarm").WithAutomaticReconnect().Build();
            _hub.On<string>("alarmNotify", (data) =>
            {
                AlarmDO alarmDO = JsonConvert.DeserializeObject<AlarmDO>(data);
                HandleAlarmPayload(alarmDO);
            });
        }

        public void Start()
        {
            try
            {
                if (UseInProcCallbacks)
                {
                    _inProcAlarmSub?.Dispose();
                    _inProcAlarmSub = Firefly.Client.Service.Abstractions.FireRuntimeServices.CallbackDispatcher
                        .SubscribeAlarms(HandleAlarmPayload);
                    return;
                }

                if (_hub == null)
                {
                    Init();
                }
                _hub.StartAsync();
                _checkHubConnectionTimer = new Timer(CheckHubConnectionTask, null, Timeout.Infinite, Timeout.Infinite);
                _checkHubConnectionTimer.Change(0, CHECK_CONNECTION_PERIOD);
            }
            catch (Exception e)
            {
                _logger.Error(e);
            }
        }

        public void Stop()
        {
            _inProcAlarmSub?.Dispose();
            _inProcAlarmSub = null;

            if (_hub != null)
            {
                _hub.StopAsync();
            }
            _alarmDOs.Clear();
            _checkHubConnectionTimer?.Dispose();
        }

        private void CheckHubConnectionTask(object obj)
        {
            if (_hub.State == HubConnectionState.Disconnected)
            {
                _hub.StartAsync();
            }
        }

        public List<AlarmDO> GetAlarm()
        {
            lock (_alarmDOs)
            {
                return _alarmDOs.ToList();
            }
        }

        public int GetUnconfirmedCount()
        {
            lock (_alarmDOs)
            {
                return _alarmDOs.Count(it => !it.IsConfirmed);
            }
        }

        public int GetAlarmDeviceCount()
        {
            lock (_alarmDOs)
            {
                return _alarmDOs.Where(it => !it.IsConfirmed).GroupBy(it => it.DeviceID).Count();
            }
        }

        public void ClearAll()
        {
            lock (_alarmDOs)
            {
                _alarmDOs.Clear();
            }
        }

        public Task<PaginationData<List<AlarmDO>>> GetAlarmList(TimePageInfo param)
        {
            var result = LiteAlarmRepository.GetByPagination(param);
            return Task.FromResult(new PaginationData<List<AlarmDO>>()
            {
                Data = result.Items,
                TotalCount = result.Total
            });
        }

        public Task<bool> ConfirmAlarm(AlarmConfirmerDataObject alarmConfirmerDO)
        {
            bool ok = LiteAlarmRepository.Confirm(alarmConfirmerDO);
            if (ok)
            {
                lock (_alarmDOs)
                {
                    AlarmDO local = _alarmDOs.FirstOrDefault(it => it.ID == alarmConfirmerDO.ID);
                    if (local != null)
                    {
                        local.IsConfirmed = alarmConfirmerDO.IsConfirmed;
                        local.ConfirmedUser = alarmConfirmerDO.ConfirmedUser;
                        local.Note = alarmConfirmerDO.Note;
                        local.ConfirmedTime = DateTime.UtcNow;
                        AddOrUpdateAlarmEvent?.Invoke(local);
                    }
                }
            }
            return Task.FromResult(ok);
        }
    }
}
