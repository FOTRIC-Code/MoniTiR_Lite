// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using Newtonsoft.Json;

namespace Firefly.Client.Control.Fire
{
    public class GeneralConfigDataObject
    {
        public GeneralConfigDataObject()
        {
            SampleInterval = 5.0f;
            DataRecordEnabled = false;
            DataRecordInterval = 5;
            ImageRecordEnabled = false;
            ImageRecordInterval = 5;
            FixedMonitoring = true;
            EnableSuperResolution​​ = false;
        }

        [JsonProperty("sampleInterval")]
        public float SampleInterval { get; set; }

        [JsonProperty("dataRecordEnabled")]
        public bool DataRecordEnabled { get; set; }
        
        [JsonProperty("dataRecordInterval")]
        public int DataRecordInterval { get; set; }
        
        [JsonProperty("imageRecordEnabled")]
        public bool ImageRecordEnabled { get; set; }
        
        [JsonProperty("imageRecordInterval")]
        public int ImageRecordInterval { get; set; }

        [JsonProperty("fixedMonitoring")]
        public bool FixedMonitoring { get; set; }

        [JsonProperty("enableSuperResolution​​")]
        public bool EnableSuperResolution​​ { get; set; }
    }
}