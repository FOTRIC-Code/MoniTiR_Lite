// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;

using Newtonsoft.Json;

namespace Firefly.Client.Service
{
    public class DeviceStatusDataObject 
    {
        [JsonProperty("id")]
        public string ID { get; set; }

        [JsonProperty("runningStatus")]
        public DeviceStatus Status { get; set; }

        [JsonProperty("alarmOutStatus")]
        public List<AlarmStatus> AlarmOut { get; set; }

        [JsonProperty("alarmInStatus")]
        public List<AlarmStatus> AlarmIn { get; set; }

        [JsonProperty("alarmStatus")]
        public int AlarmStatus { get; set; }

        [JsonProperty("statusDesc")]
        public string StatusDescription { get; set; }
    }

    public class DeviceAlarmStatusDataObject
    {
        [JsonProperty("alarmOutStatus")]
        public List<AlarmStatus> AlarmOut { get; set; }
    }
}
