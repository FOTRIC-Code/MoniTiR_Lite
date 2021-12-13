// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0
using System;
using System.ComponentModel;

using Newtonsoft.Json;

namespace Firefly.Client.Service
{
    public class EventMessageBO
    {
        [JsonProperty("deviceID")]
        public string DeviceID { get; set; }

        [JsonProperty("eventType")]
        public EventType EventType { get; set; }

        [JsonProperty("event")]
        public string Event { get; set; }

        public int ServerID { get; set; }
    }

    [Flags]
    public enum EventType
    {
        Fire_DataEvent = 1100,
        Fire_AlarmEvent = 1110,
        Fire_CruiseStatusEvent = 1111,
        Fire_CruisePositionEvent = 1112,
        Fire_AlarmInputEvent = 1113,
        Fire_DeviceAlarmIOEvent = 1114,
        Fire_PreviewException = 1120,
        Fire_PreviewReconnected = 1121,


        Equipment_DataEvent = 1200,
        Equipment_AlarmEvent = 1210,
        Equipment_CruiseStatusEvent = 1211,
        Equipment_CruisePositionEvent = 1212,
        Equipment_DefectEvent = 1213,
        Equipment_AlarmInputEvent = 1214,
        Equipment_DeviceAlarmIOEvent = 1215,

        PreviewException = 1220,
        PreviewReconnected = 1221,

        Environment_DataEvent = 1300,
        Environment_AlarmEvent = 1310
    }
}