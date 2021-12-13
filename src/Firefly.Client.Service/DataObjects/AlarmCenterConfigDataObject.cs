// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using Newtonsoft.Json;

namespace Firefly.Client.Service
{
    public class AlarmCenterConfigDataObject
    {
        [JsonProperty("alarmCenterConfig")]
        public AlarmCenterConfig AlarmCenterConfig { get; set; }
    }

    public class AlarmCenterConfig
    {
        [JsonProperty("speechCount")]
        public int SpeechCount { get; set; }

        [JsonProperty("soundCount")]
        public int SoundCount { get; set; }
    }
}