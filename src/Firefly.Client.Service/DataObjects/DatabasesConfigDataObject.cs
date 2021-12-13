// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System.Text.Json.Serialization;

namespace Firefly.Client.Service
{
    public class DatabasesConfigDataObject
    {
        public DatabasesConfigDataObject()
        {
        }

        [JsonPropertyName("dataRetentionTime")]
        public int DataRetentionTime { get; set; }

        [JsonPropertyName("alarmRetentionTime")]
        public int AlarmRetentionTime { get; set; }

        [JsonPropertyName("logRetentionTime")]
        public int LogRetentionTime { get; set; }
    }
}