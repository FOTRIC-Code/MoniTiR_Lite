// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using Newtonsoft.Json;

namespace Firefly.Client.Service
{
    public class PTZCruiseDataObject
    {
        [JsonProperty("command")]
        public PTZCruiseCommand Command { get; set; }   //巡航指令 "RUN"：开始 "STOP"：停止

        [JsonProperty("cruiseID")]
        public int CruiseID { get; set; }
    }
}