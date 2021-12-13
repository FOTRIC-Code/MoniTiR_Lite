// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System;

using Newtonsoft.Json;

namespace Firefly.Client.Service
{
    public class AreaDO
    {
        [JsonProperty("id")]
        public string ID { get; set; }

        [JsonProperty("parentID")]
        public string ParentID { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("serialNO")]
        public string SerialNO { get; set; }

        [JsonProperty("info")]
        public string Info { get; set; }

        [JsonProperty("order")]
        public int Order { get; set; }

        [JsonProperty("updatetime")]
        public DateTime UpdateTime { get; set; }

        [JsonProperty("inserttime")]
        public DateTime InsertTime { get; set; }
    }
}
