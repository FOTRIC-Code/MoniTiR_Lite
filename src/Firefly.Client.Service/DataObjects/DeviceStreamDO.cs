// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;

using Newtonsoft.Json;

namespace Firefly.Client.Service
{
    public class DeviceStreamDO
    {
        #region Constructors

        public DeviceStreamDO()
        {
            StreamItems = new List<DeviceStreamItemDO>();
        }

        #endregion

        #region Properties

        [JsonProperty("id")]
        public string ID { get; set; }

        [JsonProperty("streamItems")]
        public List<DeviceStreamItemDO> StreamItems { get; set; }

        public DeviceStreamItemDO IRDeviceSteamItem
        {
            get
            {
                DeviceStreamItemDO deviceStreamItemDO = null;

                if (StreamItems.Count > 0)
                {
                    deviceStreamItemDO = StreamItems[0];
                }

                return deviceStreamItemDO;
            }
        }

        public DeviceStreamItemDO DCDeviceSteamItem
        {
            get
            {
                DeviceStreamItemDO deviceStreamItemDO = null;

                if (StreamItems.Count > 1)
                {
                    deviceStreamItemDO = StreamItems[1];
                }

                return deviceStreamItemDO;
            }
        }

        #endregion
    }

    public class DeviceStreamItemDO
    {
        [JsonProperty("channelID")]
        public int ChannelID { get; set; }

        [JsonProperty("rtspUrl")]
        public string RTSPUrl { get; set; }

        [JsonProperty("webRTCUrl")]
        public string WebRTCUrl { get; set; }
    }

    public enum StreamType
    {
        MainStream,
        SubStream,
        Unknown
    }
}
