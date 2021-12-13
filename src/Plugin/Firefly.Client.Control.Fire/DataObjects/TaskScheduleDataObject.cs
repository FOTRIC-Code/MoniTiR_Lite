// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;

using Newtonsoft.Json;

namespace Firefly.Client.Control.Fire
{
    public class TaskScheduleDataObject
    {
        public TaskScheduleDataObject()
        {
            Enabled = false;
            TaskDayList = new List<TaskDay>();
            RestoreTime = 5;
        }

        [JsonProperty("enabled")]
        public bool Enabled { get; set; }

        [JsonProperty("taskDayList")]
        public List<TaskDay> TaskDayList { get; set; }

        [JsonProperty("restoretime")]
        public int RestoreTime { get; set; }
    }

    public class TaskDay
    {
        public TaskDay()
        {
            DayOfWeek = 0;
            TaskTimeList = new List<TaskTime>();
        }

        [JsonProperty("dayOfWeek")]
        public int DayOfWeek { get; set; }

        [JsonProperty("taskTimeList")]
        public List<TaskTime> TaskTimeList { get; set; }
    }

    public class TaskTime
    {
        public TaskTime()
        {
            From = 0;
            To = 0;
            CruiseRouteID = 0;
        }

        [JsonProperty("from")]
        public int From { get; set; }

        [JsonProperty("to")]
        public int To { get; set; }

        [JsonProperty("cruiseRouteID")]
        public int CruiseRouteID { get; set; }
    }
}