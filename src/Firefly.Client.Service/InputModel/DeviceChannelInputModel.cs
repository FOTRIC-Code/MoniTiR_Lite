// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;

using Newtonsoft.Json;

namespace Firefly.Client.Service
{
    public class DeviceChannelInputModel
    {
        [JsonProperty("serialNo")]
        public string SerialNo { get; set; }

        [JsonProperty("channelList")]
        public List<ChannelInfoModel> ChannelList { get; set; }
    }

    public class ChannelInfoModel
    {
        [JsonProperty("channelID")]
        public int ChannelID { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }
    }
}
