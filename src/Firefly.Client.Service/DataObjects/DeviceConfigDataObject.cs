// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;

using Newtonsoft.Json;

namespace Firefly.Client.Service
{
    public class DeviceConfigDataObject
    {
        public DeviceConfigDataObject()
        {
            CruiseRouteConfig = null;
            PresetConfig = null;
        }

        [JsonProperty("cruiseRouteConfig")]
        public CruiseRouteDataObject CruiseRouteConfig { get; set; }

        [JsonProperty("channelConfig")]
        public ChannelConfigOutputModel ChannelConfig { get; set; }

        [JsonProperty("presetConfig")]
        public DevicePresetDataObject PresetConfig { get; set; }

        [JsonProperty("version")]
        public int Version { get; set; }
    }

    public class CruiseRouteDataObject
    {
        public CruiseRouteDataObject()
        {
            CruiseRouteList = new List<CruiseRoute>();
        }

        [JsonProperty("cruiseRouteList")]
        public List<CruiseRoute> CruiseRouteList { get; set; }
    }

    public class CruiseRoute
    {
        public CruiseRoute()
        {
            ID = 0;
            Name = string.Empty;
        }

        [JsonProperty("id")]
        public int ID { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }
    }

    public class DeviceConfDataObject
    {
        [JsonProperty("presetConfig")]
        public DevicePresetDataObject PresetConfig { get; set; }
    }

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
        }

        [JsonProperty("id")]
        public int ID { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("equipmentID")]
        public string EquipmentID { get; set; }

        [JsonProperty("pointID")]
        public string PointID { get; set; }

        [JsonProperty("markerList")]
        public List<MarkerDataObject> MarkerList { get; set; }
    }

    public class ChannelConfigOutputModel
    {
        #region Properties

        [JsonProperty("channelList")]
        public List<ChannelOutputModel> ChannelList { get; set; }

        #endregion Properties
    }

    public class ChannelOutputModel
    {
        #region Properties

        [JsonProperty("id")]
        public int ID { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("alarmRuleID")]
        public int AlarmRuleID { get; set; }

        [JsonProperty("unit")]
        public string Unit { get; set; }

        [JsonProperty("channelType")]
        public string ChannelType { get; set; }

        [JsonProperty("paramID")]
        public string ParamID { get; set; }

        #endregion Properties
    }

    public class MarkerDataObject
    {
        public MarkerDataObject()
        {
            Name = "";
        }

        [JsonProperty("name")]
        public string Name { get; set; }
    }
}