// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;

using Newtonsoft.Json;

namespace Firefly.Client.Service
{
    public class PlaybackStreamDO
    {
        public PlaybackStreamDO()
        {
            PlaybackStreamItems = new List<PlaybackStreamItemDO>();
        }

        [JsonProperty("id")]
        public string ID { get; set; }

        [JsonProperty("playbackStreamItems")]
        public List<PlaybackStreamItemDO> PlaybackStreamItems { get; set; }
    }

    public class PlaybackStreamItemDO
    {
        [JsonProperty("channelID")]
        public int ChannelID { get; set; }

        [JsonProperty("rtspUrl")]
        public string RtspUrl { get; set; }
    }
}