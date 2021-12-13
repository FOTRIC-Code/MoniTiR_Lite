// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using Firefly.Client.Control;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Firefly.Client
{
    public class LivePreviewPreference : IPreference
    {
        #region Fields

        private static LivePreviewPreference preference = new LivePreviewPreference();
        private LivePreviewBO _livePreviewBO;

        #endregion Fields

        #region Properties

        public bool IsPtzVisibe
        {
            get => _livePreviewBO.IsPtzExpanded;
            set
            {
                _livePreviewBO.IsPtzExpanded = value;
            }
        }

        public int? SelectedMode
        {
            get => _livePreviewBO.SelectedMode;
            set
            {
                _livePreviewBO.SelectedMode = value;
            }
        }

        public List<LivePreviewDisplayItem> DisplayItems
        {
            get => _livePreviewBO.DisplayItems;
            set
            {
                _livePreviewBO.DisplayItems = value;
            }
        }

        public List<LivePreviewDeviceItem> DeviceItems
        {
            get => _livePreviewBO.DeviceItems;
            set
            {
                _livePreviewBO.DeviceItems = value;
            }
        }

        #endregion Properties

        #region Constructors

        private LivePreviewPreference()
        {
            FileName = "~LivePreview";
        }

        #endregion Constructors

        #region Properties

        public static LivePreviewPreference Intance => preference;

        public string FileName { get; }

        #endregion Properties

        #region Methods

        public void LoadJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                _livePreviewBO = new LivePreviewBO();
                _livePreviewBO.SelectedMode = 2;
                _livePreviewBO.DisplayItems = new List<LivePreviewDisplayItem>();
                _livePreviewBO.DeviceItems = new List<LivePreviewDeviceItem>();
            }
            else
            {
                _livePreviewBO = JsonConvert.DeserializeObject<LivePreviewBO>(json);
            }
        }

        public string GetJson()
        {
            return JsonConvert.SerializeObject(_livePreviewBO);
        }

        #endregion Methods
    }
}