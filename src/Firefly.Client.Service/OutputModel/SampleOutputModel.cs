// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;

using Newtonsoft.Json;

namespace Firefly.Client.Service
{
    public class SampleOutputModel
    {
        [JsonProperty("happenTime")]
        public DateTime HappenTime { get; set; }

        [JsonProperty("timestamp")]
        public long Timestamp { get; set; }

        [JsonProperty("deviceID")]
        public string DeviceID { get; set; }

        [JsonProperty("deviceName")]
        public string DeviceName { get; set; }

        [JsonProperty("protocolType")]
        public string ProtocolType { get; set; }

        [JsonProperty("presetID")]
        public int PresetID { get; set; }

        [JsonProperty("presetName")]
        public string PresetName { get; set; }

        [JsonProperty("imageInfo")]
        public List<ImageOutputModel> ImageInfo { get; set; }

        [JsonProperty("markerList")]
        public List<CommonMarkerOutputModel> MarkerList { get; set; }
    }

    public class ImageOutputModel
    {
        [JsonProperty("channelID")]
        public int ChannelID { get; set; }

        [JsonProperty("url")]
        public string URL { get; set; }
    }

    public class CommonMarkerOutputModel
    {
        [JsonProperty("markerName")]
        public string MarkerName { get; set; }

        [JsonProperty("maxTemp")]
        public float MaxTemp { get; set; }

        [JsonProperty("minTemp")]
        public float MinTemp { get; set; }

        [JsonProperty("avgTemp")]
        public float AvgTemp { get; set; }
    }

    public class EnvironmentDataOutputModel
    {
        [JsonProperty("happenTime")]
        public DateTime HappenTime { get; set; }

        [JsonProperty("deviceID")]
        public string DeviceID { get; set; }

        [JsonProperty("deviceName")]
        public string DeviceName { get; set; }

        [JsonProperty("channelID")]
        public int ChannelID { get; set; }

        [JsonProperty("channelName")]
        public string ChannelName { get; set; }

        [JsonProperty("currentValue")]
        public float CurrentValue { get; set; }

        [JsonProperty("channelType")]
        public DeviceType ChannelType { get; set; }

        [JsonProperty("unit")]
        public string Unit { get; set; }
    }
}