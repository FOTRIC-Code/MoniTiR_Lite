// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using Newtonsoft.Json;

namespace Firefly.Client.Service
{
    public class NVRChannelDataObject
    {
        public NVRChannelDataObject()
        { }

        public NVRChannelDataObject(int id, int nvrID, string deviceID, int channelID, NVRChannelInfoDataObject channelInfo)
        {
            ID = id;
            NVRID = nvrID;
            DeviceID = deviceID;
            ChannelID = channelID;
            ChannelInfo = channelInfo;
        }

        [JsonProperty("id")]
        public int ID { get; set; }

        [JsonProperty("nvrID")]
        public int NVRID { get; set; }

        [JsonProperty("deviceID")]
        public string DeviceID { get; set; }

        [JsonProperty("channelID")]
        public int ChannelID { get; set; }

        [JsonProperty("channelInfo")]
        public NVRChannelInfoDataObject ChannelInfo { get; set; }

        [JsonProperty("NVR")]
        public NVRDataObject NVRDO { get; set; }
    }

    public class NVRChannelInfoDataObject
    {
        public NVRChannelInfoDataObject()
        { }

        [JsonProperty("nvrChannelID")]
        public int NVRChannelID { get; set; }

        [JsonProperty("streamUrl")]
        public string StreamUrl { get; set; }

        [JsonProperty("usedBy")]
        public string UsedBy { get; set; }

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }
            if(!(obj is NVRChannelInfoDataObject))
            {
                return false;
            }
            if (NVRChannelID != (obj as NVRChannelInfoDataObject).NVRChannelID)
            {
                return false;
            }
            if(!string.Equals(StreamUrl, (obj as NVRChannelInfoDataObject).StreamUrl))
            {
                return false;
            }
            return true;
        }
    }
}