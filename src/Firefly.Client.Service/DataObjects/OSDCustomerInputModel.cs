using System.Collections.Generic;

using Newtonsoft.Json;

namespace Firefly.Client.Service
{
    public class OSDCustomerDO
    {
        [JsonProperty("contentList")]
        public List<ContentOSDParamDO> ContentList { get; set; }
        
        [JsonProperty("markerList")]
        public List<MarkerOSDParamDO> MarkerList { get; set; }

        public OSDCustomerDO()
        {
            ContentList = new List<ContentOSDParamDO>();
            MarkerList = new List<MarkerOSDParamDO>();
        }
    }

    public class ContentOSDParamDO
    {
        [JsonProperty("isShow")]
        public bool IsShow { get; set; }

        [JsonProperty("content")]
        public string Content { get; set; }

        [JsonProperty("status")]
        public int Status { get; set; }

        [JsonProperty("x")]
        public int X { get; set; }

        [JsonProperty("y")]
        public int Y { get; set; }
    }

    public class MarkerOSDParamDO
    {
        [JsonProperty("isShow")]
        public bool IsShow { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("duration")]
        public int Duration { get; set; }

        [JsonProperty("status")]
        public int Status { get; set; }

        [JsonProperty("type")]
        public int Type { get; set; }

        [JsonProperty("points")]
        public List<PointOSDDO> Points { get; set; }
    }

    public class PointOSDDO
    {
        [JsonProperty("rpx")]
        public double Rpx { get; set; }

        [JsonProperty("rpy")]
        public double Rpy { get; set; }
    }
}