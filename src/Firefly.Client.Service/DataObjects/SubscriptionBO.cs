// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0
using System.Collections.Generic;

using Newtonsoft.Json;

namespace Firefly.Client.Service
{
    public class SubscriptionBO
    {
        [JsonProperty("eventConsumer")]
        public string EventConsumer { get; set; }

        [JsonProperty("eventTypes")]
        public List<EventType> EventTypes { get; set; }
    }
}