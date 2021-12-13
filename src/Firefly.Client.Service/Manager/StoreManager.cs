// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Firefly.Client.Service.Abstractions;

namespace Firefly.Client.Service
{
    public class StoreManager
    {
        #region Fields

        private static StoreManager storeManager = new StoreManager();

        #endregion Fields

        #region Properties

        public static StoreManager Instance => storeManager;

        #endregion Properties

        #region Constructors

        private StoreManager()
        {
        }

        #endregion Constructors

        #region Methods

        // serverAddr kept for call-site compatibility; Lite Store is always in-process.

        public async Task<List<NVRDataObject>> GetNVRList(string serverAddr)
        {
            return await StoreRuntimeServices.Runtime.GetNVRListAsync().ConfigureAwait(false);
        }

        public async Task<bool> AddNVR(string serverAddr, NVRDataObject dataObject)
        {
            return await StoreRuntimeServices.Runtime.AddNVRAsync(dataObject).ConfigureAwait(false);
        }

        public async Task<bool> UpdateNVR(string serverAddr, NVRDataObject dataObject)
        {
            return await StoreRuntimeServices.Runtime.UpdateNVRAsync(dataObject).ConfigureAwait(false);
        }

        public async Task<bool> DeleteNVR(string serverAddr, int id)
        {
            return await StoreRuntimeServices.Runtime.DeleteNVRAsync(id).ConfigureAwait(false);
        }

        public async Task<List<NVRChannelDataObject>> GetChannelList(string serverAddr)
        {
            return await StoreRuntimeServices.Runtime.GetChannelListAsync().ConfigureAwait(false);
        }

        public async Task<List<NVRChannelDataObject>> GetChannelsByDeviceID(string serverAddr, string deviceID)
        {
            return await StoreRuntimeServices.Runtime.GetChannelsByDeviceIDAsync(deviceID).ConfigureAwait(false);
        }

        public async Task<bool> AddNVRChannel(string serverAddr, NVRChannelDataObject nvrChannelDO)
        {
            return await StoreRuntimeServices.Runtime.AddNVRChannelAsync(nvrChannelDO).ConfigureAwait(false);
        }

        public async Task<bool> UpdateNVRChannel(string serverAddr, NVRChannelDataObject nvrChannelDO)
        {
            return await StoreRuntimeServices.Runtime.UpdateNVRChannelAsync(nvrChannelDO).ConfigureAwait(false);
        }

        public async Task<bool> DeleteNVRChannel(string serverAddr, int id)
        {
            return await StoreRuntimeServices.Runtime.DeleteNVRChannelAsync(id).ConfigureAwait(false);
        }

        public async Task<PlaybackStreamDO> GetPlaybackStream(string serverAddr, string id, DateTime startTime, DateTime endTime)
        {
            return await StoreRuntimeServices.Runtime.GetPlaybackStreamAsync(id, startTime, endTime).ConfigureAwait(false);
        }

        public async Task<List<NVRChannelInfoDataObject>> GetChannelsByNVRID(string serverAddr, int nvrID)
        {
            return await StoreRuntimeServices.Runtime.GetChannelsByNVRIDAsync(nvrID).ConfigureAwait(false);
        }

        #endregion Methods
    }
}
