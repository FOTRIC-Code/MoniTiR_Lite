// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using Newtonsoft.Json;

namespace Firefly.Client.Service
{
    public class DeviceStoreDataObject : DeviceDO
    {
        [JsonProperty("nvrid")]
        public int NVRID { get; set; }
    }
}