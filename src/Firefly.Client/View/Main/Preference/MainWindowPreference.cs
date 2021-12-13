using Firefly.Client.Control;
using Newtonsoft.Json;
using System;

namespace Firefly.Client
{
    public class MainWindowPreference : IPreference
    {
        private static MainWindowPreference preference = new MainWindowPreference();
        private MainWindowBO _mainWindowBO;

        private MainWindowPreference()
        {
            FileName = "~MainWindow";
        }

        public int StartCount
        {
            get => _mainWindowBO.StartCount ?? 0;
            set
            {
                _mainWindowBO.StartCount = value;
            }
        }

        public long LastStartTime
        {
            get => _mainWindowBO.LastStartTime ?? 0;
            set
            {
                _mainWindowBO.LastStartTime = value;
            }
        }

        public string Title
        {
            get => _mainWindowBO.Title ?? string.Empty;
            set
            {
                _mainWindowBO.Title = value;
            }
        }

        public static MainWindowPreference Instance => preference;

        public string FileName { get; }

        public string GetJson()
        {
            return JsonConvert.SerializeObject(_mainWindowBO);
        }

        public void LoadJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                _mainWindowBO = new MainWindowBO();
                _mainWindowBO.StartCount = 0;
                _mainWindowBO.LastStartTime = DateTime.Now.ToBinary();
                _mainWindowBO.Title = Title;
            }
            else
            {
                _mainWindowBO = JsonConvert.DeserializeObject<MainWindowBO>(json);
            }
        }
    }
}