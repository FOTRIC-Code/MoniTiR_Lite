// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;

using Firefly.Client.Service.LiteDb;

namespace Firefly.Client.Service
{
    public class SystemConfigManager
    {
        #region Fields

        private static readonly SystemConfigManager _systemConfigManager = new SystemConfigManager();

        public static SystemConfigManager Instance => _systemConfigManager;

        #endregion Fields

        /// <summary>Deprecated: no longer points to Admin.</summary>
        public string ServerURL { get; set; }

        #region Constructors

        private SystemConfigManager()
        {
            LiteDatabase.EnsureInitialized();
        }

        #endregion Constructors

        #region Methods

        public Task<string> GetI18n()
        {
            string i18n = LiteSystemConfigRepository.GetRaw(LiteSystemConfigRepository.KeyI18n);
            if (string.IsNullOrWhiteSpace(i18n))
            {
                i18n = CultureInfo.CurrentUICulture?.Name ?? "zh-CN";
            }
            return Task.FromResult(i18n);
        }

        public Task<LivePreviewCruisePlanConfigDO> GetLivePreviewPollingGroup()
        {
            LivePreviewCruisePlanConfigDO config = LiteSystemConfigRepository.Get<LivePreviewCruisePlanConfigDO>(LiteSystemConfigRepository.KeyLivePreview)
                ?? new LivePreviewCruisePlanConfigDO();
            return Task.FromResult(config);
        }

        public Task<bool> UpdateLivePreviewPollingGroup(LivePreviewCruisePlanConfigDO livePreviewPollingGroupConfigDO)
        {
            LiteSystemConfigRepository.SetObject(LiteSystemConfigRepository.KeyLivePreview, livePreviewPollingGroupConfigDO);
            return Task.FromResult(true);
        }

        public Task<CommonConfigDO> GetCommonConfig()
        {
            CommonConfigDO config = LiteSystemConfigRepository.Get<CommonConfigDO>(LiteSystemConfigRepository.KeyCommon)
                ?? new CommonConfigDO();
            return Task.FromResult(config);
        }

        public Task<bool> UpdateCommonConfig(CommonConfigDO commonConfigDO)
        {
            LiteSystemConfigRepository.SetObject(LiteSystemConfigRepository.KeyCommon, commonConfigDO);
            return Task.FromResult(true);
        }

        public Task<CustomSoftwareDO> GetCustomSoftware()
        {
            CustomSoftwareDO config = LiteSystemConfigRepository.Get<CustomSoftwareDO>(LiteSystemConfigRepository.KeyCustomSoftware)
                ?? new CustomSoftwareDO();
            return Task.FromResult(config);
        }

        public Task<bool> UpdateCustomSoftware(CustomSoftwareDO customSoftwareDO)
        {
            LiteSystemConfigRepository.SetObject(LiteSystemConfigRepository.KeyCustomSoftware, customSoftwareDO);
            return Task.FromResult(true);
        }

        public Task<ApiResult<string>> UploadSoftwareIcon(string filePath)
        {
            ApiResult<string> apiResult = new ApiResult<string>();
            try
            {
                string saved = LiteSystemConfigRepository.SaveIconFile(filePath);
                apiResult.Data = saved;
                apiResult.Code = ResponseCode.SUCCESS;
            }
            catch (Exception ex)
            {
                apiResult.Code = ResponseCode.FAILED;
                apiResult.Msg = ex.Message;
            }
            return Task.FromResult(apiResult);
        }

        #endregion Methods
    }
}
