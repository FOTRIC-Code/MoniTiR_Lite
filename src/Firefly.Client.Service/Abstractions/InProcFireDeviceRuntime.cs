// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

using log4net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Firefly.Client.Service.LiteDb;

namespace Firefly.Client.Service.Abstractions
{
    /// <summary>
    /// In-process Fire device runtime. Device registry is LiteDeviceRepository;
    /// engine lifecycle is owned by <see cref="InProcFireEngineHost"/>.
    /// HTTP sync to 9080 is skipped.
    /// </summary>
    public sealed class InProcFireDeviceRuntime : IFireDeviceRuntime
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(InProcFireDeviceRuntime));
        private readonly IFireCallbackDispatcher _dispatcher;

        public InProcFireDeviceRuntime(IFireCallbackDispatcher dispatcher)
        {
            _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        }

        public bool IsReady { get; private set; }

        public async Task StartAsync()
        {
            await InProcFireEngineHost.Instance.StartAsync(_dispatcher).ConfigureAwait(false);
            IsReady = true;
            Logger.Info("InProc Fire device runtime started.");
        }

        public async Task StopAsync()
        {
            await InProcFireEngineHost.Instance.StopAsync().ConfigureAwait(false);
            IsReady = false;
            Logger.Info("InProc Fire device runtime stopped.");
        }

        public Task SyncDeviceAsync(DeviceDO device, HttpMethod method)
        {
            // Local SQLite is source of truth; notify engine host to reload device.
            if (device != null && !string.IsNullOrEmpty(device.ID))
            {
                return InProcFireEngineHost.Instance.ReloadDeviceAsync(device.ID);
            }

            return Task.CompletedTask;
        }

        public Task SyncDeviceDeleteAsync(string deviceId)
        {
            return InProcFireEngineHost.Instance.RemoveDeviceAsync(deviceId);
        }

        public Task<T> GetDeviceConfigAsync<T>(DeviceDO device)
        {
            string conf = LiteDeviceRepository.Get(device?.ID)?.Conf;
            if (string.IsNullOrWhiteSpace(conf))
            {
                return Task.FromResult(default(T));
            }

            try
            {
                return Task.FromResult(JsonConvert.DeserializeObject<T>(conf));
            }
            catch (Exception ex)
            {
                Logger.Error($"Parse device conf failed. id={device?.ID}", ex);
                return Task.FromResult(default(T));
            }
        }

        public async Task<bool> UpdateDeviceConfigAsync(DeviceDO device, object partialConfig)
        {
            if (device == null || string.IsNullOrEmpty(device.ID) || partialConfig == null)
            {
                return false;
            }

            string existing = LiteDeviceRepository.Get(device.ID)?.Conf;
            JObject merged = MergeConf(existing, JObject.FromObject(partialConfig));
            if (!LiteDeviceRepository.UpdateConf(device.ID, merged.ToString(Formatting.None)))
            {
                return false;
            }

            await InProcFireEngineHost.Instance.ReloadDeviceAsync(device.ID).ConfigureAwait(false);
            return true;
        }

        public async Task<bool> OverrideDeviceConfigAsync(DeviceDO device, DeviceOverrideDataObject request)
        {
            if (request == null || string.IsNullOrEmpty(request.SourceID) || request.DestinationIDs == null)
            {
                return false;
            }

            string sourceConf = LiteDeviceRepository.Get(request.SourceID)?.Conf;
            if (string.IsNullOrWhiteSpace(sourceConf))
            {
                return false;
            }

            JObject source = JObject.Parse(sourceConf);
            if (request.ConfigItemList != null && request.ConfigItemList.Count > 0)
            {
                foreach (JProperty prop in source.Properties().ToList())
                {
                    bool selected = request.ConfigItemList.Any(
                        item => string.Equals(item, prop.Name, StringComparison.OrdinalIgnoreCase));
                    if (!selected)
                    {
                        prop.Remove();
                    }
                }
            }

            bool all = true;
            foreach (string destinationId in request.DestinationIDs)
            {
                string existing = LiteDeviceRepository.Get(destinationId)?.Conf;
                JObject merged = MergeConf(existing, (JObject)source.DeepClone());
                if (LiteDeviceRepository.UpdateConf(destinationId, merged.ToString(Formatting.None)))
                {
                    await InProcFireEngineHost.Instance.ReloadDeviceAsync(destinationId).ConfigureAwait(false);
                }
                else
                {
                    all = false;
                }
            }

            return all;
        }

        // Engine calls block on native camera I/O and may Delay(5s) for GOTO.
        // Must not run on the WPF UI thread (Task.FromResult would freeze the window).
        public Task<bool> SetPresetAsync(DeviceDO device, PTZPresetDataObject preset)
        {
            string id = device?.ID;
            return Task.Run(() => InProcFireEngineBridge.TryPtzPreset(id, preset));
        }

        public Task<bool> PtzControlAsync(DeviceDO device, PTZControlDataObject control)
        {
            string id = device?.ID;
            return Task.Run(() => InProcFireEngineBridge.TryPtzControl(id, control));
        }

        public Task<bool> SetCruiseAsync(DeviceDO device, PTZCruiseDataObject cruise)
        {
            string id = device?.ID;
            return Task.Run(() => InProcFireEngineBridge.TryPtzCruise(id, cruise));
        }

        public Task<bool> RestoreCruiseAsync(DeviceDO device)
        {
            string id = device?.ID;
            return Task.Run(() => InProcFireEngineBridge.TryRestoreCruise(id));
        }

        public Task<bool> SetAlarmOutAsync(DeviceDO device, List<AlarmOutDataObject> alarmOuts)
        {
            string id = device?.ID;
            return Task.Run(() => InProcFireEngineBridge.TrySetAlarmOut(id, alarmOuts));
        }

        public Task<bool> SetCustomerOsdAsync(DeviceDO device, int channelId, OSDCustomerDO osd)
        {
            string id = device?.ID;
            return Task.Run(() => InProcFireEngineBridge.TrySetOsdCustomer(id, channelId, osd));
        }

        public Task<ImageInfo> CaptureImageAsync(DeviceDO device, int channel)
        {
            string id = device?.ID;
            return Task.Run(() => InProcFireEngineBridge.TryCaptureImage(id, channel));
        }

        /// <summary>
        /// Mirrors the agent's section-wise merge: only sections present in the update replace
        /// stored ones, and osdConfigEnabled only applies alongside one of the OSD sections.
        /// </summary>
        private static JObject MergeConf(string existingConf, JObject update)
        {
            foreach (JProperty prop in update.Properties().ToList())
            {
                if (prop.Value == null || prop.Value.Type == JTokenType.Null)
                {
                    prop.Remove();
                }
            }

            bool hasOsdSection = update["channelConfig"] != null
                || update["ptzOSDConfig"] != null
                || update["markerOSDConfig"] != null;
            if (!hasOsdSection)
            {
                update.Remove("osdConfigEnabled");
            }

            if (string.IsNullOrWhiteSpace(existingConf))
            {
                return update;
            }

            JObject merged = JObject.Parse(existingConf);
            merged.Merge(update, new JsonMergeSettings
            {
                MergeArrayHandling = MergeArrayHandling.Replace,
                MergeNullValueHandling = MergeNullValueHandling.Ignore,
            });
            return merged;
        }

        public Task<DeviceParamDataObject> GetDeviceParamAsync(DeviceDO device)
        {
            return InProcFireEngineHost.Instance.GetDeviceParamAsync(device);
        }

        public Task<DeviceStatusDataObject> GetDeviceStatusAsync(DeviceDO device)
        {
            return InProcFireEngineHost.Instance.GetDeviceStatusAsync(device);
        }

        public Task<List<DeviceStatusDataObject>> GetDeviceStatusListAsync(ServerDO server)
        {
            return InProcFireEngineHost.Instance.GetDeviceStatusListAsync();
        }

        public Task SubscribeAsync(string serverAddr, ServerType serverType, string deviceId, string consumerId)
        {
            // In-proc: subscription is implicit via CallbackDispatcher; keep API no-op success.
            return Task.CompletedTask;
        }

        public Task UnsubscribeAsync(string serverAddr, ServerType serverType, string deviceId, string consumerId)
        {
            return Task.CompletedTask;
        }

        public Task<DeviceStreamDO> GetStreamAsync(DeviceDO device, StreamType streamType)
        {
            return InProcFireEngineHost.Instance.GetStreamAsync(device, streamType);
        }
    }
}
