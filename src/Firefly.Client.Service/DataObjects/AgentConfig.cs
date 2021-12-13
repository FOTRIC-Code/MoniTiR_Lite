// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;

using Newtonsoft.Json;

namespace Firefly.Client.Service
{
    public class AgentConfig
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("metaData")]
        public AgentMetaData MetaData { get; set; }

        [JsonProperty("alarmDescription")]
        public string AlarmDescription { get; set; }
    }

    public class AgentMetaData
    {
        [JsonProperty("protocolList")]
        public List<AgentProtocol> ProtocolList { get; set; }
    }

    public class AgentProtocol
    {
        [JsonProperty("protocolType")]
        public string ProtocolType { get; set; }

        [JsonProperty("channelList")]
        public List<Channel> ChannelList { get; set; }
        [JsonProperty("alarmLevelInfoList")]
        public List<AlarmLevelInfo> AlarmLevelInfoList { get; set; }
    }

    public class Channel
    {
        [JsonProperty("id")]
        public int ID { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("supportedProtocolTypeList")]
        public string[] SupportedProtocolTypeList { get; set; }
    }

    public class AlarmLevelInfo
    {
        [JsonProperty("alarmCategory")]
        public string AlarmCategory { get; set; }
        [JsonProperty("alarmLevel")]
        public string AlarmLevel { get; set; }
        [JsonProperty("alarmLevelDescription")]
        public string AlarmLevelDescription { get; set; }
    }
}