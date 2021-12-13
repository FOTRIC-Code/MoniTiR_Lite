// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using Newtonsoft.Json;

using Firefly.Client.Service;

namespace Firefly.Client.Control.Fire
{
    public class PTZPresetDataObject : Service.PTZPresetDataObject
    {
        [JsonProperty("data")]
        public PresetDataObject PresetConfig { get; set; }
    }
}
