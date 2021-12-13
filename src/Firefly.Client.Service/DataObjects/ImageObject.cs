// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using Newtonsoft.Json;

namespace Firefly.Client.Service
{
    public class ImageObject
    {
        #region property

        [JsonProperty("channelID")]
        public int ChannelID { get; set; }

        [JsonProperty("url")]
        public string URL { get; set; }

        [JsonProperty("fileType")]
        public FileType FileType { get; set; }

        #endregion
    }
}