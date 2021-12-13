using Newtonsoft.Json;

namespace Firefly.Client.Service
{
    public class CustomSoftwareDO
    {
        [JsonProperty("iconPath")]
        public string IconPath { get; set; }

        [JsonProperty("i18n")]
        public string I18n { get; set; }

        [JsonProperty("softwareName")]
        public string SoftwareName { get; set; }

        [JsonProperty("fontSize")]
        public int FontSize { get; set; }

        public long UpdateTime { get; set; }
    }
}
