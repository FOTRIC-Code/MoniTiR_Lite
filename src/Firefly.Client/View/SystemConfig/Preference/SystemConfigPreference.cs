// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using Firefly.Client.Control;

using Newtonsoft.Json;

namespace Firefly.Client
{
    public class SystemConfigPreference : IPreference
    {
        #region Fields

        private static SystemConfigPreference preference = new SystemConfigPreference();
        private SystemConfigBO _systemConfigBO;

        #endregion Fields

        #region Properties

        public ImageRenderMode RenderMode
        {
            get
            {
                if (string.Equals(_systemConfigBO.RenderMode, ImageRenderMode.Fit.ToString()))
                {
                    return ImageRenderMode.Fit;
                }
                else if (string.Equals(_systemConfigBO.RenderMode, ImageRenderMode.Fill.ToString()))
                {
                    return ImageRenderMode.Fill;
                }
                return ImageRenderMode.Fit;
            }
            set
            {
                _systemConfigBO.RenderMode = value.ToString();
                UIConstants.RenderMode = value;
            }
        }

        public string SoftwareDownloadIconPath
        {
            get => _systemConfigBO.SoftwareDownloadIconPath;
            set
            {
                _systemConfigBO.SoftwareDownloadIconPath = value;
            }
        }

        public string SoftwareIconPath
        {
            get => _systemConfigBO.SoftwareIconPath;
            set
            {
                _systemConfigBO.SoftwareIconPath = value;
            }
        }

        public string SoftwareIconETag
        {
            get => _systemConfigBO.SoftwareIconETag;
            set
            {
                _systemConfigBO.SoftwareIconETag = value;
            }
        }

        public int SoftwareTitleFontSize
        {
            get => _systemConfigBO.SoftwareTitleFontSize;
            set
            {
                _systemConfigBO.SoftwareTitleFontSize = value;
            }
        }

        #endregion Properties

        #region Constructors

        private SystemConfigPreference()
        {
            FileName = "~SystemConfig";
        }

        #endregion Constructors

        #region Properties

        public static SystemConfigPreference Intance => preference;

        public string FileName { get; }

        #endregion Properties

        #region Methods

        public void LoadJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                _systemConfigBO = new SystemConfigBO();
                _systemConfigBO.RenderMode = ImageRenderMode.Fit.ToString();
                _systemConfigBO.SoftwareDownloadIconPath = "";
                _systemConfigBO.SoftwareIconPath = "";
                _systemConfigBO.SoftwareTitleFontSize = 16;
                _systemConfigBO.SoftwareIconETag = "";
            }
            else
            {
                _systemConfigBO = JsonConvert.DeserializeObject<SystemConfigBO>(json);
            }
            UIConstants.RenderMode = RenderMode;
        }

        public string GetJson()
        {
            return JsonConvert.SerializeObject(_systemConfigBO);
        }

        #endregion Methods
    }
}