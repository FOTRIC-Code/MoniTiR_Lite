// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System;

using Newtonsoft.Json;

namespace Firefly.Client.Service
{
    public class ServerDO
    {
        #region Constructors
        public ServerDO()
        {

        }
        #endregion

        #region Properties
        [JsonProperty("id")]
        public int ID { get; set; }
        
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("ip")]
        public string IP { get; set; }

        [JsonProperty("port")]
        public int Port { get; set; }

        [JsonProperty("status")]
        public int Status { get; set; }

        [JsonProperty("deviceCount")]
        public int DeviceCount { get; set; }

        [JsonProperty("startTime")]
        public DateTime? StartTime { get; set; }

        [JsonProperty("version")]
        public string Version { get; set; }

        #endregion

        public string GetServerAddress()
        {
            return $"http://{IP}:{Port}";
        }

        public override string ToString()
        {
            return $"{ID},{Name},{Type},{IP},{Port},{Status},{DeviceCount}";
        }
    }
}
