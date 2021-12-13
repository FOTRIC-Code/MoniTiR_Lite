// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;

using Newtonsoft.Json;

namespace Firefly.Client.Service
{
    public class DeviceDataDO
    {
        [JsonProperty("id")]
        public int ID { get; set; }

        [JsonProperty("happenTime")]
        public DateTime HappenTime { get; set; }

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

        [JsonProperty("markerName")]
        public string MarkerName { get; set; }

        [JsonProperty("maxTemp")]
        public float MaxTemp { get; set; }

        [JsonProperty("minTemp")]
        public float MinTemp { get; set; }

        [JsonProperty("avgTemp")]
        public float AvgTemp { get; set; }

        [JsonProperty("currentValue")]
        public float CurrentValue { get; set; }

        [JsonProperty("imageInfo")]
        public List<ImageInfo> ImageInfos { get; set; }

        [JsonProperty("updateTime")]
        public DateTime UpdateTime { get; set; }

        [JsonProperty("insertTime")]
        public DateTime InsertTime { get; set; }

        [JsonProperty("alarmLevel")]
        public string AlarmLevel { get; set; }
    }

    public class DeviceDataQueryParam : TimePageInfo
    {
        [JsonProperty("deviceID")] 
        public string DeviceID { get; set; }

        [JsonProperty("presetID")]
        public int PresetID { get; set; }

        [JsonProperty("channelID")]
        public int ChannelID { get; set; }

        [JsonProperty("isContainAlarm")]
        public bool IsContainAlarm { get; set; }
    }

    public class ImageInfo
    {
        public ImageInfo()
        {
        }

        public ImageInfo(int channelID, string url, FileType fileType)
        {
            ChannelID = channelID;
            URL = url;
            FileType = fileType;
        }

        [JsonProperty("channelID")]
        public int ChannelID { get; set; }

        [JsonProperty("url")]
        public string URL { get; set; }

        [JsonProperty("fileType")]
        public FileType FileType { get; set; }
    }
}