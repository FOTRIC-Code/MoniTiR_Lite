// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System.Linq;
using System.Collections.Generic;

using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace Firefly.Client.Service
{
    public class DeviceParamDataObject
    {
        #region Properties

        public DeviceParamDataObject()
        {
            DeviceModel = new List<DeviceModelDataObject>();
        }

        [JsonProperty("softwareAbility")]
        public SoftwareAbilityDataObject SoftwareAbility { get; set; }

        [JsonProperty("hardwareAbility")]
        public HardwareAbilityDataObject HardwareAbility { get; set; }

        [JsonProperty("deviceModel")]
        public List<DeviceModelDataObject> DeviceModel { get; set; }

        #endregion

        public DeviceModelDataObject GetIRDeviceModelDO()
        {
            return DeviceModel?.FirstOrDefault();
        }
    }

    public class SoftwareAbilityDataObject
    {
        [JsonProperty("ptzSupport")]
        public bool PtzSupport { get; set; }
    }

    public class HardwareAbilityDataObject
    {
        [JsonProperty("alarmInPortNum")]
        public int AlarmInPortNum { get; set; }

        [JsonProperty("alarmOutPortNum")]
        public int AlarmOutPortNum { get; set; }
    }

    public class ParamInfoModel
    {
        [JsonProperty("paramID")]
        public int ParamID { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }
    }

    public class DeviceModelDataObject
    {
        [JsonProperty("channelID")]
        public int ChannelID { get; set; }

        [JsonProperty("model")]
        public string Model { get; set; }

        [JsonProperty("seriesNo")]
        public string SeriesNO { get; set; }

        [JsonProperty("firmwareVersion")]
        public string FirmwareVersion { get; set; }

        [JsonProperty("width")]
        public int Width { get; set; }

        [JsonProperty("height")]
        public int Height { get; set; }

        [JsonPropertyName("paramList")]
        public List<ParamInfoModel> ParamList { get; set; }
    }
}