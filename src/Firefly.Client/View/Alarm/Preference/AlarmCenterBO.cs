// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

namespace Firefly.Client
{
    public class AlarmCenterBO
    {
        public bool NeedPop { get; set; }

        public bool EnableSound { get; set; }

        public bool EnableSpeech { get; set; }

        public bool AutoPlayLiveVideo { get; set; }

        public bool DetailViewVisibility { get; set; }

        public int SpeechCount { get; set; }

        public int SoundCount { get; set; }
    }
}