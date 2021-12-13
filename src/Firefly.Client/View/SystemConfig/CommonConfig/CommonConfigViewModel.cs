// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using Firefly.Client.Control;
using Firefly.Client.Service;

namespace Firefly.Client
{
    public class CommonConfigViewModel : BaseViewModel
    {
        private string _dataRetentionTime = "0";
        private string _alarmRetentionTime = "0";
        private string _logRetentionTime = "0";

        private string _speechCount;
        private string _soundCount;

        public CommonConfigViewModel()
        {
        }

        public string SpeechCount
        {
            get => _speechCount;
            set
            {
                _speechCount = value;
                OnPropertyChanged();
            }
        }

        public string SoundCount
        {
            get => _soundCount;
            set
            {
                _soundCount = value;
                OnPropertyChanged();
            }
        }

        public string DataRetentionTime
        {
            get => _dataRetentionTime;
            set
            {
                _dataRetentionTime = value;
                OnPropertyChanged();
            }
        }

        public string AlarmRetentionTime
        {
            get => _alarmRetentionTime;
            set
            {
                _alarmRetentionTime = value;
                OnPropertyChanged();
            }
        }

        public string LogRetentionTime
        {
            get => _logRetentionTime;
            set
            {
                _logRetentionTime = value;
                OnPropertyChanged();
            }
        }

        public async void LoadData()
        {
            CommonConfigDO commonConfigDO = await SystemConfigManager.Instance.GetCommonConfig();
            DataRetentionTime = $"{commonConfigDO?.DataRetentionTime ?? 0}";
            AlarmRetentionTime = $"{commonConfigDO?.AlarmRetentionTime ?? 0}";
            LogRetentionTime = $"{commonConfigDO?.LogRetentionTime ?? 30}";

            SpeechCount = commonConfigDO.SpeechCount.ToString();
            SoundCount = commonConfigDO.SoundCount.ToString();

            AlarmCenterPreference.Instance.SpeechCount = commonConfigDO.SpeechCount;
            AlarmCenterPreference.Instance.SoundCount = commonConfigDO.SoundCount;
        }
    }
}