// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;

using Newtonsoft.Json;

namespace Firefly.Client.Control.Fire
{
    public class EventTriggerDataObject
    {
        public EventTriggerDataObject()
        {
            Alert1Out = new List<int>();
            Alert2Out = new List<int>();
            AlarmOut = new List<int>();
        }

        [JsonProperty("alert1Out")]
        public List<int> Alert1Out { get; set; }

        [JsonProperty("alert2Out")]
        public List<int> Alert2Out { get; set; }

        [JsonProperty("alarmOut")]
        public List<int> AlarmOut { get; set; }
    }
}