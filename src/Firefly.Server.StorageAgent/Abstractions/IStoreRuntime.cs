// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Firefly.Client.Service.Abstractions
{
    /// <summary>
    /// In-process Store (NVR) runtime — replaces MoniTiR.StorageAgent HTTP (9070).
    /// </summary>
    public interface IStoreRuntime
    {
        bool IsReady { get; }

        Task StartAsync();

        Task StopAsync();

        Task<List<NVRDataObject>> GetNVRListAsync();

        Task<bool> AddNVRAsync(NVRDataObject dataObject);

        Task<bool> UpdateNVRAsync(NVRDataObject dataObject);

        Task<bool> DeleteNVRAsync(int id);

        Task<List<NVRChannelDataObject>> GetChannelListAsync();

        Task<List<NVRChannelDataObject>> GetChannelsByDeviceIDAsync(string deviceID);

        Task<bool> AddNVRChannelAsync(NVRChannelDataObject nvrChannelDO);

        Task<bool> UpdateNVRChannelAsync(NVRChannelDataObject nvrChannelDO);

        Task<bool> DeleteNVRChannelAsync(int id);

        Task<PlaybackStreamDO> GetPlaybackStreamAsync(string deviceId, DateTime startTime, DateTime endTime);

        Task<List<NVRChannelInfoDataObject>> GetChannelsByNVRIDAsync(int nvrID);
    }
}
