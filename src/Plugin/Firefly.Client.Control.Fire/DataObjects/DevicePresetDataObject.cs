// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;

using Newtonsoft.Json;

namespace Firefly.Client.Control.Fire
{
    public class DevicePresetDataObject
    {
        public DevicePresetDataObject()
        {
            PresetList = new List<PresetDataObject>();
        }

        [JsonProperty("presetList")]
        public List<PresetDataObject> PresetList { get; set; }
    }

    public class PresetDataObject
    {
        public PresetDataObject()
        {
            ID = 0;
            Name = "";
            Sensitivity = 0;
            ShieldMaskList = new List<ShieldMaskDataObject>();
            MarkerList = new List<MarkerDataObject>();
        }

        [JsonProperty("id")]
        public int ID { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("sensitivity")]
        public int Sensitivity { get; set; }

        [JsonProperty("shieldMaskList")]
        public List<ShieldMaskDataObject> ShieldMaskList { get; set; }

        [JsonProperty("markerList")]
        public List<MarkerDataObject> MarkerList { get; set; }
    }

    public class ShieldMaskDataObject
    {
        public ShieldMaskDataObject()
        {
            Name = "";
            PointList = new List<MarkerPointDataObject>();
        }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("pointList")]
        public List<MarkerPointDataObject> PointList { get; set; }
    }

    public class MarkerPointDataObject
    {
        public MarkerPointDataObject()
        {
            X = 0;
            Y = 0;
        }

        [JsonProperty("x")]
        public int X { get; set; }

        [JsonProperty("y")]
        public int Y { get; set; }
    }

    public class MarkerDataObject
    {
        public MarkerDataObject()
        {
            Name = "";
            MarkerType = 0;
            Emissivity = 0f;
            Distance = 0f;
            ReflTemp = 0f;
            OffsetTemp = 0f;
            AlarmRuleID = 0;
            PointList = new List<MarkerPointDataObject>();
        }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("markerType")]
        public MarkerType MarkerType { get; set; }

        [JsonProperty("emissivity")]
        public float Emissivity { get; set; }

        [JsonProperty("distance")]
        public float Distance { get; set; }

        [JsonProperty("reflTemp")]
        public float ReflTemp { get; set; }

        [JsonProperty("offsetTemp")]
        public float OffsetTemp { get; set; }

        [JsonProperty("alarmRuleID")]
        public int AlarmRuleID { get; set; }

        [JsonProperty("pointList")]
        public List<MarkerPointDataObject> PointList { get; set; }
    }
}
