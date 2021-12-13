using Newtonsoft.Json;

using Firefly.Client.Control;

namespace Firefly.Client
{
    public class AppPreference : IPreference
    {
        private static AppPreference instance = new AppPreference();
        private static AppBO _appBO;

        private AppPreference()
        {
            FileName = "~App";
        }

        public static AppPreference Instance => instance;

        public string FileName { get; }

        public bool IsAlarmOutEnable
        {
            get => _appBO.IsAlarmOutEnable;
            set
            {
                _appBO.IsAlarmOutEnable = value;
            }
        }

        public string SystemLanguage
        {
            get => _appBO?.SystemLanguage;
            set
            {
                _appBO.SystemLanguage = value;
            }
        }

        public void LoadJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                _appBO = new AppBO();
                _appBO.IsAlarmOutEnable = true;
                _appBO.SystemLanguage = "en-US";
            }
            else
            {
                _appBO = JsonConvert.DeserializeObject<AppBO>(json);
            }
        }

        public string GetJson()
        {
            return JsonConvert.SerializeObject(_appBO);
        }
    }
}