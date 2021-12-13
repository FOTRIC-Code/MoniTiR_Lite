// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using Newtonsoft.Json;

namespace Firefly.Client.Service
{
    public class PTZControlDataObject
    {
        [JsonProperty("channelID")]
        public int ChannelID { get; set; }

        [JsonProperty("command")]
        public PTZCommand Command { get; set; }

        [JsonProperty("speed")]
        public int Speed { get; set; }  // 速度取值范围[1-10]

        [JsonProperty("stop")]
        public PTZControlAction Stop { get; set; }   //云台动作停止标志位 0：开始 1：停止
    }
}
