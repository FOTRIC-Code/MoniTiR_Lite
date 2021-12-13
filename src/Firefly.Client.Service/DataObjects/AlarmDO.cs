// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;

using Newtonsoft.Json;

namespace Firefly.Client.Service
{
    public class AlarmDO
    {
        #region Properties

        [JsonProperty("id")]
        public int ID { get; set; }

        [JsonProperty("deviceID")]
        public string DeviceID { get; set; }

        [JsonProperty("timestamp")]
        public long Timestamp { get; set; }

        [JsonProperty("deviceName")]
        public string DeviceName { get; set; }

        [JsonProperty("alarmType")]
        public string AlarmType { get; set; }

        [JsonProperty("alarmRegion")]
        public string AlarmRegion { get; set; }

        [JsonProperty("alarmLevel")]
        public string AlarmLevel { get; set; }

        [JsonProperty("alarmTime")]
        public DateTime AlarmTime { get; set; }

        [JsonProperty("arrivalTime")]
        public DateTime ArrivalTime { get; set; }

        [JsonProperty("alarmDesc")]
        public string AlarmDescription { get; set; }

        [JsonProperty("imageInfo")]
        public List<AlarmImageInfoDataObject> ImageInfo { get; set; }

        [JsonProperty("isConfirmed")]
        public bool IsConfirmed { get; set; }

        [JsonProperty("confirmedTime")]
        public DateTime? ConfirmedTime { get; set; }

        [JsonProperty("confirmedUser")]
        public string ConfirmedUser { get; set; }

        [JsonProperty("isExported")]
        public bool IsExported { get; set; }

        [JsonProperty("note")]
        public string Note { get; set; }

        [JsonProperty("presetID")]
        public int PresetID { get; set; }

        [JsonProperty("presetName")]
        public string PresetName { get; set; }

        [JsonProperty("channelID")]
        public int ChannelID { get; set; }

        [JsonProperty("channelName")]
        public string ChannelName { get; set; }

        #endregion
    }

    public class AlarmImageInfoDataObject
    {
        [JsonProperty("channelID")]
        public int ChannelID { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }
    }

    public class AlarmContentDataObject
    {
        [JsonProperty("markerName")]
        public string MarkerName { get; set; }

        [JsonProperty("imageUrl")]
        public string ImageUrl { get; set; }

        [JsonProperty("ruleType")]
        public string RuleType { get; set; }

        [JsonProperty("alarmType")]
        public string AlarmType { get; set; }

        [JsonProperty("alarmTemp")]
        public float AlarmTemp { get; set; }

        [JsonProperty("alarmRule")]
        public AlarmRule AlarmRule { get; set; }

        [JsonProperty("preset")]
        public PresetDataObject Preset { get; set; }
    }

    public class AlarmRule
    {
        public AlarmRule()
        {
            ID = 0;
            Name = "";
            RuleType = 0;
            Alert1Temp = 0f;
            Alert1FilteringTime = 0;
            Alert2Temp = 0f;
            Alert2FilteringTime = 0;
            AlarmTemp = 0f;
            AlarmFilteringTime = 0;
            ToleranceTemp = 0f;
            TempSuddenChange = new TempSuddenChange();
        }

        [JsonProperty("id")]
        public int ID { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("ruleType")]
        public int RuleType { get; set; }

        [JsonProperty("alert1Temp")]
        public float Alert1Temp { get; set; }

        [JsonProperty("alert1FilteringTime")]
        public int Alert1FilteringTime { get; set; }

        [JsonProperty("alert2Temp")]
        public float Alert2Temp { get; set; }

        [JsonProperty("alert2FilteringTime")]
        public int Alert2FilteringTime { get; set; }

        [JsonProperty("alarmTemp")]
        public float AlarmTemp { get; set; }

        [JsonProperty("alarmFilteringTime")]
        public int AlarmFilteringTime { get; set; }

        [JsonProperty("toleranceTemp")]
        public float ToleranceTemp { get; set; }

        [JsonProperty("tempSuddenChange")]
        public TempSuddenChange TempSuddenChange { get; set; }
    }

    public class TempSuddenChange
    {
        public TempSuddenChange()
        {
            Enable = false;
            TempDiff = 0f;
            Interval = 0;
            Sensitivity = 0;
            CloseFilteringTime = 0;
        }

        [JsonProperty("enabled")]
        public bool Enable { get; set; }

        [JsonProperty("tempDiff")]
        public float TempDiff { get; set; }

        [JsonProperty("thresholdTemp")]
        public float ThresholdTemp { get; set; }

        [JsonProperty("interval")]
        public int Interval { get; set; }

        [JsonProperty("sensitivity")]
        public int Sensitivity { get; set; }

        [JsonProperty("closeFilteringTime")]
        public int CloseFilteringTime { get; set; }
    }

    public class ImagePathModel
    {
        [JsonProperty("irCamera")]
        public string IRImagePath { get; set; }

        [JsonProperty("dcCamera")]
        public string DCImagePath { get; set; }
    }
}