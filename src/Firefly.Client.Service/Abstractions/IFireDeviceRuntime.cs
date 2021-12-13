// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace Firefly.Client.Service.Abstractions
{
    /// <summary>
    /// Fire device runtime operations previously served by HTTP /api/device and /api/service/device*.
    /// </summary>
    public interface IFireDeviceRuntime
    {
        Task SyncDeviceAsync(DeviceDO device, HttpMethod method);

        Task SyncDeviceDeleteAsync(string deviceId);

        Task<T> GetDeviceConfigAsync<T>(DeviceDO device);

        Task<DeviceParamDataObject> GetDeviceParamAsync(DeviceDO device);

        Task<DeviceStatusDataObject> GetDeviceStatusAsync(DeviceDO device);

        Task<List<DeviceStatusDataObject>> GetDeviceStatusListAsync(ServerDO server);

        Task SubscribeAsync(string serverAddr, ServerType serverType, string deviceId, string consumerId);

        Task UnsubscribeAsync(string serverAddr, ServerType serverType, string deviceId, string consumerId);

        Task<DeviceStreamDO> GetStreamAsync(DeviceDO device, StreamType streamType);

        /// <summary>
        /// Merges the non-null sections of <paramref name="partialConfig"/> into the stored device config.
        /// </summary>
        Task<bool> UpdateDeviceConfigAsync(DeviceDO device, object partialConfig);

        Task<bool> OverrideDeviceConfigAsync(DeviceDO device, DeviceOverrideDataObject request);

        Task<bool> SetPresetAsync(DeviceDO device, PTZPresetDataObject preset);

        Task<bool> PtzControlAsync(DeviceDO device, PTZControlDataObject control);

        Task<bool> SetCruiseAsync(DeviceDO device, PTZCruiseDataObject cruise);

        Task<bool> RestoreCruiseAsync(DeviceDO device);

        Task<bool> SetAlarmOutAsync(DeviceDO device, List<AlarmOutDataObject> alarmOuts);

        Task<bool> SetCustomerOsdAsync(DeviceDO device, int channelId, OSDCustomerDO osd);

        Task<ImageInfo> CaptureImageAsync(DeviceDO device, int channel);

        Task StartAsync();

        Task StopAsync();

        bool IsReady { get; }
    }
}
