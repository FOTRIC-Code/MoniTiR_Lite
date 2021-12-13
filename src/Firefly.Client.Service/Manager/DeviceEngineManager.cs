// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System.Net.Http;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace Firefly.Client.Service
{
    public class DeviceEngineManager
    {
        #region Fields

        private static DeviceEngineManager instance = new DeviceEngineManager();

        #endregion

        #region Properties

        public static DeviceEngineManager Instance => instance;

        #endregion

        #region Constructors

        private DeviceEngineManager() { }

        #endregion

        #region Methods

        public async Task<DeviceChannelInputModel> GetChannelList(string serverAddress, DeviceAddressDataObject deviceAddressDO)
        {
            ApiResult<DeviceChannelInputModel> result = await IRTekHttpClient.SendAsync<DeviceChannelInputModel>(HttpMethod.Get, $"http://{serverAddress}/api/device/channelList?channelInfo=" + JsonConvert.SerializeObject(deviceAddressDO));
            return result.Data ?? new DeviceChannelInputModel();
        }

        public async Task<DeviceParamDataObject> GetParam(string serverAddress, string deviceID)
        {
            ApiResult<DeviceParamDataObject> result = await IRTekHttpClient.SendAsync<DeviceParamDataObject>(HttpMethod.Get, $"{serverAddress}/api/device/{deviceID}/deviceParam");
            return result.Data ?? new DeviceParamDataObject();
        }

        #endregion
    }
}
