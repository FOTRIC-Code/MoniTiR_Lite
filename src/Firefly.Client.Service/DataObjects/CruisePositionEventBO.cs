// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0
using Newtonsoft.Json;

namespace Firefly.Client.Service
{
    public class CruisePositionEventBO
    {
        [JsonProperty("deviceID")]
        public string DeviceID { get; set; }

        [JsonProperty("deviceName")]
        public string DeviceName { get; set; }

        [JsonProperty("equipmentID")]
        public string EquipmentID { get; set; }

        [JsonProperty("equipmentName")]
        public string EquipmentName { get; set; }

        [JsonProperty("presetID")]
        public int? PresetID { get; set; }

        [JsonProperty("presetName")]
        public string PresetName { get; set; }

        [JsonProperty("pointID")]
        public string PointID { get; set; }

        [JsonProperty("pointName")]
        public string PointName { get; set; }
    }
}