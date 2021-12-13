// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0
using System.Collections.Generic;

namespace Firefly.Client
{
    public class LivePreviewBO
    {
        public int? SelectedMode { get; set; }
        public int? SelectedPollingTime { get; set; }
        public List<LivePreviewDisplayItem> DisplayItems { get; set; }
        public List<LivePreviewDeviceItem> DeviceItems { get; set; }
        public bool IsPtzExpanded { get; set; }
    }

    public class LivePreviewDeviceItem
    {
        public string ID { get; set; }
        public string ParentID { get; set; }
        public bool IsExpanded { get; set; }
        public ItemType ItemType { get; set; }
    }

    public class LivePreviewDisplayItem
    {
        public int Index { get; set; }
        public string DeviceID { get; set; }
        public int ChannelID { get; set; }
        public string DeviceType { get; set; }
    }
}