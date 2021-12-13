// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using Newtonsoft.Json;
using System.Collections.Generic;

namespace Firefly.Client.Service
{
    public class LivePreviewCruisePlanConfigDO
    {
        [JsonProperty("livePreviewPollingGroupInfos")]
        public List<LivePreviewCruisePlan> LivePreviewPollingGroupInfos { get; set; }
    }

    public class LivePreviewCruisePlan
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("second")]
        public int Second { get; set; }

        [JsonProperty("splitMode")]
        public int SplitMode { get; set; }

        [JsonProperty("groupItems")]
        public List<LivePreviewCruiseItem> GroupItems { get; set; }
    }

    public class LivePreviewCruiseItem
    {
        [JsonProperty("deviceName")]
        public string DeviceName { get; set; }

        [JsonProperty("deviceID")]
        public string DeviceID { get; set; }

        [JsonProperty("channelID")]
        public string ChannelID { get; set; }

        [JsonProperty("order")]
        public int Order { get; set; }
    }
}