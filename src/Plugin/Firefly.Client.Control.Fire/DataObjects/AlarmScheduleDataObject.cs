// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;

using Newtonsoft.Json;

namespace Firefly.Client.Control.Fire
{
    public class AlarmScheduleDataObject
    {
        public AlarmScheduleDataObject()
        {
            Enabled = false;
            AlarmDayList = new List<AlarmDay>();
        }

        [JsonProperty("enabled")]
        public bool Enabled { get; set; }

        [JsonProperty("alarmDayList")]
        public List<AlarmDay> AlarmDayList { get; set; }
    }

    public class AlarmDay
    {
        public AlarmDay()
        {
            DayOfWeek = 0;
            AlarmTimeList = new List<AlarmTime>();
        }

        [JsonProperty("dayOfWeek")]
        public int DayOfWeek { get; set; }

        [JsonProperty("alarmTimeList")]
        public List<AlarmTime> AlarmTimeList { get; set; }
    }

    public class AlarmTime
    {
        public AlarmTime()
        {
            From = 0;
            To = 0;
        }

        [JsonProperty("from")]
        public int From { get; set; }

        [JsonProperty("to")]
        public int To { get; set; }

        [JsonProperty("pauseType")]
        public AlarmType PauseType { get; set; }
    }
}