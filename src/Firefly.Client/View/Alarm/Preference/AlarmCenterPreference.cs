// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using Firefly.Client.Control;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Firefly.Client
{
    public class AlarmCenterPreference : IPreference
    {
        private static AlarmCenterPreference preference = new AlarmCenterPreference();
        private AlarmCenterBO _alarmCenterBO;

        private AlarmCenterPreference()
        {
            FileName = "~AlarmCenter";
        }

        #region Properties

        public static AlarmCenterPreference Instance => preference;

        public string FileName { get; }

        public bool NeedPop
        {
            get => _alarmCenterBO.NeedPop;
            set
            {
                _alarmCenterBO.NeedPop = value;
            }
        }

        public bool EnableSound
        {
            get => _alarmCenterBO.EnableSound;
            set
            {
                _alarmCenterBO.EnableSound = value;
            }
        }

        public bool EnableSpeech
        {
            get => _alarmCenterBO.EnableSpeech;
            set
            {
                _alarmCenterBO.EnableSpeech = value;
            }
        }

        public bool AutoPlayLiveVideo
        {
            get => _alarmCenterBO.AutoPlayLiveVideo;
            set
            {
                _alarmCenterBO.AutoPlayLiveVideo = value;
            }
        }

        public bool DetailViewVisibility
        {
            get => _alarmCenterBO.DetailViewVisibility;
            set
            {
                _alarmCenterBO.DetailViewVisibility = value;
            }
        }

        public int SpeechCount
        {
            get => _alarmCenterBO.SpeechCount;
            set
            {
                _alarmCenterBO.SpeechCount = value;
            }
        }

        public int SoundCount
        {
            get => _alarmCenterBO.SoundCount;
            set
            {
                _alarmCenterBO.SoundCount = value;
            }
        }

        #endregion Properties

        public void LoadJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                _alarmCenterBO = new AlarmCenterBO();
                _alarmCenterBO.NeedPop = true;
                _alarmCenterBO.EnableSound = true;
                _alarmCenterBO.EnableSpeech = true;
                _alarmCenterBO.AutoPlayLiveVideo = true;
                _alarmCenterBO.DetailViewVisibility = true;
                _alarmCenterBO.SoundCount = 3;
                _alarmCenterBO.SpeechCount = 3;
            }
            else
            {
                _alarmCenterBO = JsonConvert.DeserializeObject<AlarmCenterBO>(json);
            }
        }

        public string GetJson()
        {
            return JsonConvert.SerializeObject(_alarmCenterBO);
        }
    }
}