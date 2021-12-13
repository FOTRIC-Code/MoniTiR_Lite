// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using log4net;

using Firefly.Client.Service.LiteDb;

namespace Firefly.Client.Service.Abstractions
{
    /// <summary>
    /// Owns in-process Fire device engines. Loads devices from LiteDeviceRepository.
    /// Soft watchdog re-ensures engines; native AV still kills the process (known plan cost).
    /// </summary>
    public sealed class InProcFireEngineHost
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(InProcFireEngineHost));
        public static InProcFireEngineHost Instance { get; } = new InProcFireEngineHost();

        private readonly ConcurrentDictionary<string, DeviceRuntimeState> _devices =
            new ConcurrentDictionary<string, DeviceRuntimeState>(StringComparer.OrdinalIgnoreCase);

        private IFireCallbackDispatcher _dispatcher;
        private bool _started;
        private Timer _watchdog;

        private InProcFireEngineHost()
        {
        }

        public async Task StartAsync(IFireCallbackDispatcher dispatcher)
        {
            _dispatcher = dispatcher;
            if (_started)
            {
                return;
            }

            try
            {
                LiteDatabase.EnsureInitialized();
                LiteFireDataRepository.EnsureTables();
                List<DeviceDO> devices = LiteDeviceRepository.GetByPagination(new PaginationInfo { Page = 1, PageSize = -1 }).Items
                    ?? new List<DeviceDO>();
                foreach (DeviceDO d in devices)
                {
                    if (d == null || string.IsNullOrEmpty(d.ID))
                    {
                        continue;
                    }

                    _devices[d.ID] = new DeviceRuntimeState
                    {
                        DeviceId = d.ID,
                        Name = d.Name,
                        Online = false,
                    };
                }

                InProcFireEngineBridge.TryInitialize(_dispatcher);

                foreach (string id in _devices.Keys.ToList())
                {
                    await ReloadDeviceAsync(id).ConfigureAwait(false);
                }

                _watchdog = new Timer(WatchdogTick, null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
                _started = true;
                Logger.Info($"InProcFireEngineHost started with {_devices.Count} device(s).");
            }
            catch (Exception ex)
            {
                Logger.Error("InProcFireEngineHost start failed.", ex);
                throw;
            }
        }

        public Task StopAsync()
        {
            try
            {
                _watchdog?.Dispose();
                _watchdog = null;
                InProcFireEngineBridge.TryShutdown();
            }
            catch (Exception ex)
            {
                Logger.Warn("Engine bridge shutdown: " + ex.Message);
            }

            _devices.Clear();
            _started = false;
            return Task.CompletedTask;
        }

        public async Task ReloadDeviceAsync(string deviceId)
        {
            if (string.IsNullOrEmpty(deviceId))
            {
                return;
            }

            DeviceDO device = LiteDeviceRepository.Get(deviceId);
            if (device == null)
            {
                await RemoveDeviceAsync(deviceId).ConfigureAwait(false);
                return;
            }

            bool exists = _devices.ContainsKey(deviceId);
            bool online = await Task.Run(() =>
                exists
                    ? InProcFireEngineBridge.TryUpdateDevice(deviceId)
                    : InProcFireEngineBridge.TryEnsureDevice(deviceId)).ConfigureAwait(false);

            _devices[deviceId] = new DeviceRuntimeState
            {
                DeviceId = deviceId,
                Name = device.Name,
                Online = online,
            };
        }

        public Task RemoveDeviceAsync(string deviceId)
        {
            if (string.IsNullOrEmpty(deviceId))
            {
                return Task.CompletedTask;
            }

            _devices.TryRemove(deviceId, out _);
            InProcFireEngineBridge.TryRemoveDevice(deviceId);
            return Task.CompletedTask;
        }

        public Task<DeviceParamDataObject> GetDeviceParamAsync(DeviceDO device)
        {
            string id = device?.ID;
            return Task.Run(() => InProcFireEngineBridge.TryGetParam(id) ?? new DeviceParamDataObject());
        }

        public Task<DeviceStatusDataObject> GetDeviceStatusAsync(DeviceDO device)
        {
            if (device == null)
            {
                return Task.FromResult(new DeviceStatusDataObject { Status = DeviceStatus.Offline });
            }

            string id = device.ID;
            return Task.Run(() =>
            {
                DeviceStatusDataObject fromEngine = InProcFireEngineBridge.TryGetStatus(id);
                if (fromEngine != null)
                {
                    return fromEngine;
                }

                DeviceRuntimeState state;
                bool online = _devices.TryGetValue(id, out state) && state.Online;
                return new DeviceStatusDataObject
                {
                    ID = id,
                    Status = online ? DeviceStatus.Online : DeviceStatus.Offline,
                };
            });
        }

        public Task<List<DeviceStatusDataObject>> GetDeviceStatusListAsync()
        {
            List<string> ids = _devices.Keys.ToList();
            return Task.Run(() =>
            {
                return ids.Select(id =>
                {
                    DeviceStatusDataObject s = InProcFireEngineBridge.TryGetStatus(id);
                    if (s != null)
                    {
                        return s;
                    }

                    DeviceRuntimeState state;
                    bool online = _devices.TryGetValue(id, out state) && state.Online;
                    return new DeviceStatusDataObject
                    {
                        ID = id,
                        Status = online ? DeviceStatus.Online : DeviceStatus.Offline,
                    };
                }).ToList();
            });
        }

        public Task<DeviceStreamDO> GetStreamAsync(DeviceDO device, StreamType streamType)
        {
            string id = device?.ID;
            return Task.Run(() => InProcFireEngineBridge.TryGetStream(id, streamType));
        }

        public void OnAlarmFromEngine(AlarmDO alarm)
        {
            _dispatcher?.PublishAlarm(alarm);
        }

        public void OnEventFromEngine(int serverId, string payloadJson)
        {
            _dispatcher?.PublishEvent(serverId, payloadJson);
        }

        private void WatchdogTick(object state)
        {
            try
            {
                InProcFireEngineBridge.WatchdogTick(_devices.Keys.ToList());
            }
            catch (Exception ex)
            {
                Logger.Warn("Watchdog tick failed: " + ex.Message);
            }
        }

        private sealed class DeviceRuntimeState
        {
            public string DeviceId { get; set; }

            public string Name { get; set; }

            public bool Online { get; set; }
        }
    }
}
