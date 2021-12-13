// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;

using Newtonsoft.Json;

namespace Firefly.Client.Control.Fire
{
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
            Name = "";
            CruisePointList = new List<CruisePoint>();
        }

        [JsonProperty("id")]
        public int ID { get; set; }
        
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("cruisePointList")]
        public List<CruisePoint> CruisePointList { get; set; }
    }

    public class CruisePoint
    {
        public CruisePoint()
        {
            PresetID = 0;
            Speed = 0;
            WaitTime = 0;
            AutoFocus = false;
        }

        [JsonProperty("presetID")]
        public int PresetID { get; set; }

        [JsonProperty("speed")]
        public int Speed { get; set; }

        [JsonProperty("waitTime")]
        public int WaitTime { get; set; }

        [JsonProperty("autoFocus")]
        public bool AutoFocus { get; set; }
    }
}
