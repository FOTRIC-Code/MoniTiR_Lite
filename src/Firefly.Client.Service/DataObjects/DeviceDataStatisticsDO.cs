// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;

using Newtonsoft.Json;

namespace Firefly.Client.Service
{
    public class DeviceDataStatisticsDO
    {
        [JsonProperty("deviceID")]
        public string DeviceID { get; set; }

        [JsonProperty("deviceName")]
        public string DeviceName { get; set; }

        [JsonProperty("presetID")]
        public int PresetID { get; set; }

        [JsonProperty("presetName")]
        public string PresetName { get; set; }

        [JsonProperty("channelID")]
        public int ChannelID { get; set; }

        [JsonProperty("channelName")]
        public string ChannelName { get; set; }

        [JsonProperty("statistics")]
        public Statistics Statistics { get; set; }
    }

    public class Statistics
    {
        [JsonProperty("dataStatistics")]
        public int DataStatistics { get; set; }

        [JsonProperty("alarmStatistics")]
        public List<AlarmStatistics> AlarmStatistics { get; set; }

        public Statistics()
        {
            AlarmStatistics = new List<AlarmStatistics>();
        }
    }

    public class AlarmStatistics
    {
        [JsonProperty("alarmLevel")]
        public string AlarmLevel { get; set; }

        [JsonProperty("count")]
        public int Count { get; set; }
    }

    public class DeviceDataStatisticsQueryParam : TimeInfo
    {
        [JsonProperty("deviceIDsStr")]
        public string DeviceIDsStr { get; set; }
    }
}