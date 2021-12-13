// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using Newtonsoft.Json;

namespace Firefly.Client.Service
{
    public class PTZPresetDataObject
    {
        [JsonProperty("command")]
        public PTZPresetCommand Command { get; set; }

        [JsonProperty("presetID")]
        public int PresetID { get; set; }

        [JsonProperty("speed")]
        public int Speed { get; set; }
    }
}