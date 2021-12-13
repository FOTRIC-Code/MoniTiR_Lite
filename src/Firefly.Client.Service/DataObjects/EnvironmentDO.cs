// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using System.Windows.Documents;

namespace Firefly.Client.Service.DataObjects
{
    public class EnvironmentDO
    {
        public string IP { get; set; }
        public List<EnvironmentChannelDO> ChannelList { get; set; }
    }

    public class EnvironmentChannelDO
    {
        public int ChannelID { get; set; }
        public string ChannelName { get; set; }
    }
}
