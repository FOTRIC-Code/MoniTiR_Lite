// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using Newtonsoft.Json;

namespace Firefly.Client.Service
{
    public class NVRDataObject
    {
        #region Properties

        [JsonProperty("id")]
        public int ID { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("protocolType")]
        public string ProtocolType { get; set; }

        [JsonProperty("addr")]
        public NVRAddressDataObject Address { get; set; }

        #endregion
    }

    public class NVRAddressDataObject
    {
        #region Properties

        [JsonProperty("ip")]
        public string IP { get; set; }

        [JsonProperty("port")]
        public int Port { get; set; }

        [JsonProperty("username")]
        public string UserName { get; set; }

        [JsonProperty("password")]
        public string Password { get; set; }

        #endregion
    }
}