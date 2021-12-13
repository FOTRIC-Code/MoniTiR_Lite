// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;

using Newtonsoft.Json;

namespace Firefly.Client.Service
{
    public class DeviceOverrideDataObject
    {
        public DeviceOverrideDataObject(string sourceID, List<string> destinationIDs, List<string> configItemList = null)
        {
            SourceID = sourceID;
            DestinationIDs = destinationIDs;
            ConfigItemList = configItemList;
        }

        [JsonProperty("sourceID")]
        public string SourceID { get; set; }

        [JsonProperty("destinationIDs")]
        public List<string> DestinationIDs { get; set; } = new List<string>();

        [JsonProperty("configItemList")]
        public List<string> ConfigItemList { get; set; } = new List<string>();
    }
}
