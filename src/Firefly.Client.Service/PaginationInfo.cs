// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System;

using Newtonsoft.Json;

namespace Firefly.Client.Service
{
    public class PaginationInfo
    {
        [JsonProperty("searchStr")]
        public string SearchStr { get; set; }

        [JsonProperty("page")]
        public int Page { get; set; }

        [JsonProperty("pagesz")]
        public int PageSize { get; set; }
    }

    public class TimeInfo
    {
        [JsonProperty("startTime")]
        public DateTime BeginTime { get; set; }

        [JsonProperty("endTime")]
        public DateTime EndTime { get; set; }
    }

    public class TimePageInfo : PaginationInfo
    {
        [JsonProperty("startTime")]
        public DateTime StartTime { get; set; }

        [JsonProperty("endTime")]
        public DateTime EndTime { get; set; }

        [JsonProperty("alarmConfirmationStatus")]
        public AlarmConfirmationStatus AlarmConfirmationStatus { get; set; }

        [JsonProperty("alarmTypes")]
        public string AlarmTypes { get; set; }
    }

    public class PaginationData<T>
    {
        public int TotalCount { get; set; }

        public T Data { get; set; }
    }
}