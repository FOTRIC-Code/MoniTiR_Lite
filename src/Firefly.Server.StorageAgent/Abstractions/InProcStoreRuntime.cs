// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using log4net;
using Newtonsoft.Json;

using OWB.NVR;

using Firefly.Client.Service.LiteDb;

namespace Firefly.Client.Service.Abstractions
{
    /// <summary>
    /// In-process Store: LiteNvrRepository + OWB.NVR (no HTTP / no StorageAgent process).
    /// </summary>
    public sealed class InProcStoreRuntime : IStoreRuntime
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(InProcStoreRuntime));

        private const string HikTimeFormat = "yyyyMMddTHHmmssfff";
        private const string DhTimeFormat = "yyyy_MM_dd_HH_mm_ss";
        private const string XmTimeFormat = "yyyyMMddTHHmmssfff";
        private const string TwDataFormat = "yyyy-MM-dd";
        private const string TwTimeFormat = "HH-mm-ss";

        public bool IsReady { get; private set; }

        public Task StartAsync()
        {
            StoreSqlite.EnsureNvrSchema();
            IsReady = true;
            Logger.Info("InProc Store runtime started.");
            return Task.CompletedTask;
        }

        public Task StopAsync()
        {
            IsReady = false;
            Logger.Info("InProc Store runtime stopped.");
            return Task.CompletedTask;
        }

        public Task<List<NVRDataObject>> GetNVRListAsync()
        {
            List<NVRDataObject> list = LiteNvrRepository.GetAllNvrs().Select(ToNvrDo).ToList();
            return Task.FromResult(list);
        }

        public Task<bool> AddNVRAsync(NVRDataObject dataObject)
        {
            if (dataObject == null || string.IsNullOrWhiteSpace(dataObject.Name))
            {
                return Task.FromResult(false);
            }

            if (LiteNvrRepository.ExistsByName(dataObject.Name))
            {
                return Task.FromResult(false);
            }

            int id = LiteNvrRepository.AddNvr(dataObject);
            dataObject.ID = id;
            return Task.FromResult(id > 0);
        }

        public Task<bool> UpdateNVRAsync(NVRDataObject dataObject)
        {
            if (dataObject == null || LiteNvrRepository.GetNvr(dataObject.ID) == null)
            {
                return Task.FromResult(false);
            }

            return Task.FromResult(LiteNvrRepository.UpdateNvr(dataObject));
        }

        public Task<bool> DeleteNVRAsync(int id)
        {
            if (LiteNvrRepository.GetNvr(id) == null)
            {
                return Task.FromResult(false);
            }

            return Task.FromResult(LiteNvrRepository.DeleteNvr(id));
        }

        public Task<List<NVRChannelDataObject>> GetChannelListAsync()
        {
            return Task.FromResult(LiteNvrRepository.GetAllChannels().Select(ToChannelDo).ToList());
        }

        public Task<List<NVRChannelDataObject>> GetChannelsByDeviceIDAsync(string deviceID)
        {
            return Task.FromResult(LiteNvrRepository.GetChannelsByDeviceId(deviceID).Select(ToChannelDo).ToList());
        }

        public Task<bool> AddNVRChannelAsync(NVRChannelDataObject nvrChannelDO)
        {
            if (nvrChannelDO == null)
            {
                return Task.FromResult(false);
            }

            LiteNvrRepository.NvrRow nvr = LiteNvrRepository.GetNvr(nvrChannelDO.NVRID);
            if (nvr == null)
            {
                return Task.FromResult(false);
            }

            bool ok;
            if (nvrChannelDO.ChannelInfo != null && !string.IsNullOrWhiteSpace(nvrChannelDO.ChannelInfo.StreamUrl))
            {
                ok = ApplyOwbChannelChange(nvr, nvrChannelDO, deleting: false)
                    && LiteNvrRepository.CreateChannel(nvrChannelDO);
            }
            else
            {
                ok = LiteNvrRepository.CreateChannel(nvrChannelDO);
            }

            return Task.FromResult(ok);
        }

        public Task<bool> UpdateNVRChannelAsync(NVRChannelDataObject nvrChannelDO)
        {
            if (nvrChannelDO == null)
            {
                return Task.FromResult(false);
            }

            LiteNvrRepository.NvrRow nvr = LiteNvrRepository.GetNvr(nvrChannelDO.NVRID);
            if (nvr == null)
            {
                return Task.FromResult(false);
            }

            bool owbOk = ApplyOwbChannelChange(nvr, nvrChannelDO, deleting: false);
            if (!owbOk)
            {
                return Task.FromResult(false);
            }

            return Task.FromResult(LiteNvrRepository.UpdateChannel(nvrChannelDO));
        }

        public Task<bool> DeleteNVRChannelAsync(int id)
        {
            LiteNvrRepository.NvrChannelRow existing = LiteNvrRepository.GetChannel(id);
            if (existing == null)
            {
                return Task.FromResult(false);
            }

            NVRChannelDataObject channelDo = ToChannelDo(existing);
            if (channelDo.ChannelInfo != null
                && !string.IsNullOrWhiteSpace(channelDo.ChannelInfo.StreamUrl)
                && existing.Nvr != null)
            {
                if (!ApplyOwbChannelChange(existing.Nvr, channelDo, deleting: true))
                {
                    return Task.FromResult(false);
                }
            }

            return Task.FromResult(LiteNvrRepository.DeleteChannel(id));
        }

        public Task<PlaybackStreamDO> GetPlaybackStreamAsync(string deviceId, DateTime startTime, DateTime endTime)
        {
            var result = new PlaybackStreamDO { ID = deviceId };
            foreach (LiteNvrRepository.NvrChannelRow channel in LiteNvrRepository.GetChannelsByDeviceId(deviceId))
            {
                NVRChannelDataObject channelDo = ToChannelDo(channel);
                if (channelDo.ChannelInfo == null || channel.Nvr == null)
                {
                    continue;
                }

                NVRDataObject nvrDo = ToNvrDo(channel.Nvr);
                string rtsp = BuildPlaybackRtsp(channelDo, nvrDo, startTime, endTime);
                if (!string.IsNullOrEmpty(rtsp))
                {
                    result.PlaybackStreamItems.Add(new PlaybackStreamItemDO
                    {
                        ChannelID = channelDo.ChannelID,
                        RtspUrl = rtsp,
                    });
                }
            }

            return Task.FromResult(result);
        }

        public Task<List<NVRChannelInfoDataObject>> GetChannelsByNVRIDAsync(int nvrID)
        {
            var list = new List<NVRChannelInfoDataObject>();
            LiteNvrRepository.NvrRow nvr = LiteNvrRepository.GetNvr(nvrID);
            if (nvr == null)
            {
                return Task.FromResult(list);
            }

            NVRDataObject nvrDo = ToNvrDo(nvr);
            if (nvrDo.Address == null)
            {
                return Task.FromResult(list);
            }

            OWBNVR instance = OWBNVR.GetInstance(nvrDo.ProtocolType, nvrID);
            if (instance == null)
            {
                return Task.FromResult(list);
            }

            try
            {
                instance.LoginNVR(nvrDo.Address.IP, nvrDo.Address.Port, nvrDo.Address.UserName, nvrDo.Address.Password);
                if (instance.IsValidLoginID())
                {
                    list = instance.GetChannel()
                        .Select(x => new NVRChannelInfoDataObject
                        {
                            NVRChannelID = x.NVRChannelID,
                            StreamUrl = x.StreamUrl,
                            UsedBy = x.UsedBy,
                        })
                        .ToList();
                    instance.LogoutNVR();
                    instance.CleanupNVR();
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"GetChannelsByNVRID failed. nvrId={nvrID}", ex);
            }

            return Task.FromResult(list);
        }

        private static bool ApplyOwbChannelChange(LiteNvrRepository.NvrRow nvr, NVRChannelDataObject channelDo, bool deleting)
        {
            NVRDataObject nvrDo = ToNvrDo(nvr);
            if (nvrDo.Address == null)
            {
                return false;
            }

            OWBNVR owbnvr = OWBNVR.GetInstance(nvr.ProtocolType, nvr.Id);
            if (owbnvr == null)
            {
                return false;
            }

            try
            {
                owbnvr.LoginNVR(nvrDo.Address.IP, nvrDo.Address.Port, nvrDo.Address.UserName, nvrDo.Address.Password);
                if (!owbnvr.IsValidLoginID())
                {
                    return false;
                }

                List<LiteNvrRepository.NvrChannelRow> existing = LiteNvrRepository.GetChannelsByNvrId(nvr.Id);
                var channelInfos = new NVRChannelInfo[owbnvr.ChannelIDArray.Length];
                for (int i = 0; i < channelInfos.Length; i++)
                {
                    channelInfos[i] = new NVRChannelInfo();
                }

                foreach (LiteNvrRepository.NvrChannelRow it in existing)
                {
                    if (string.IsNullOrWhiteSpace(it.ChannelInfoJson) || string.Equals(it.ChannelInfoJson, "null", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    NVRChannelInfoDataObject info = JsonConvert.DeserializeObject<NVRChannelInfoDataObject>(it.ChannelInfoJson);
                    if (info == null || string.IsNullOrWhiteSpace(info.StreamUrl))
                    {
                        continue;
                    }

                    if (info.NVRChannelID < 0 || info.NVRChannelID >= channelInfos.Length)
                    {
                        continue;
                    }

                    string[] parts = SplitStreamUrl(info.StreamUrl);
                    channelInfos[info.NVRChannelID].Enabled = true;
                    channelInfos[info.NVRChannelID].IpAddr = parts[0];
                    channelInfos[info.NVRChannelID].Port = Convert.ToInt32(parts[1]);
                    channelInfos[info.NVRChannelID].Url = parts[2];
                }

                if (channelDo.ChannelInfo != null && channelDo.ChannelInfo.NVRChannelID >= 0
                    && channelDo.ChannelInfo.NVRChannelID < channelInfos.Length)
                {
                    if (deleting || string.IsNullOrWhiteSpace(channelDo.ChannelInfo.StreamUrl))
                    {
                        channelInfos[channelDo.ChannelInfo.NVRChannelID].Enabled = false;
                        channelInfos[channelDo.ChannelInfo.NVRChannelID].IpAddr = string.Empty;
                        channelInfos[channelDo.ChannelInfo.NVRChannelID].Port = 0;
                        channelInfos[channelDo.ChannelInfo.NVRChannelID].Url = string.Empty;
                        if (!deleting)
                        {
                            channelDo.ChannelInfo = null;
                        }
                    }
                    else
                    {
                        string[] parts = SplitStreamUrl(channelDo.ChannelInfo.StreamUrl);
                        channelInfos[channelDo.ChannelInfo.NVRChannelID] = new NVRChannelInfo
                        {
                            Enabled = true,
                            IpAddr = parts[0],
                            Port = Convert.ToInt32(parts[1]),
                            Url = parts[2],
                        };
                    }
                }

                bool updateResult = owbnvr.AddChannel(channelInfos);
                owbnvr.LogoutNVR();
                return updateResult;
            }
            catch (Exception ex)
            {
                Logger.Error($"OWB channel change failed. nvrId={nvr.Id}", ex);
                return false;
            }
        }

        private static string BuildPlaybackRtsp(NVRChannelDataObject channelDo, NVRDataObject nvrDo, DateTime startTime, DateTime endTime)
        {
            if (nvrDo?.Address == null || channelDo?.ChannelInfo == null)
            {
                return null;
            }

            int nvrChannelID = channelDo.ChannelInfo.NVRChannelID + 1;
            string ip = nvrDo.Address.IP;
            string user = nvrDo.Address.UserName;
            string pass = nvrDo.Address.Password;

            if (string.Equals(nvrDo.ProtocolType, NVRType.HIKSDK_NET.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                nvrChannelID = nvrChannelID * 100 + 1;
                return $"rtsp://{user}:{pass}@{ip}/streaming/tracks/{nvrChannelID}?starttime={startTime.ToString(HikTimeFormat)}&endtime={endTime.ToString(HikTimeFormat)}";
            }

            if (string.Equals(nvrDo.ProtocolType, NVRType.DHSDK_NET.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                return $"rtsp://{user}:{pass}@{ip}/cam/playback?channel={nvrChannelID}&starttime={startTime.ToString(DhTimeFormat)}&endtime={endTime.ToString(DhTimeFormat)}";
            }

            if (string.Equals(nvrDo.ProtocolType, NVRType.XMSDK_NET.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                return $"rtsp://{user}:{pass}@{ip}:554/playback?starttime={startTime.ToString(XmTimeFormat)}&endtime={endTime.ToString(XmTimeFormat)}&channel={nvrChannelID}&stream=100.sdp?";
            }

            if (string.Equals(nvrDo.ProtocolType, NVRType.TWSDK_NET.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                int timeLen = (int)endTime.Subtract(startTime).TotalMilliseconds;
                return $"rtsp://{ip}:554/chID={nvrChannelID}&data={startTime.ToString(TwDataFormat)}&time={startTime.ToString(TwTimeFormat)}&timelen={timeLen}&StreamType=main";
            }

            return null;
        }

        private static string[] SplitStreamUrl(string streamUrl)
        {
            string[] channelInfos = new string[3];
            string[] splitStrings = streamUrl.Split('/');
            string[] ipAddr = splitStrings[2].Split(':');
            string path = string.Empty;
            for (int i = 3; i < splitStrings.Length; i++)
            {
                path += $"/{splitStrings[i]}";
            }

            channelInfos[0] = ipAddr[0];
            channelInfos[1] = ipAddr[1];
            channelInfos[2] = path;
            return channelInfos;
        }

        private static NVRDataObject ToNvrDo(LiteNvrRepository.NvrRow row)
        {
            NVRAddressDataObject addr = null;
            if (!string.IsNullOrWhiteSpace(row.AddrJson))
            {
                try
                {
                    addr = JsonConvert.DeserializeObject<NVRAddressDataObject>(row.AddrJson);
                }
                catch
                {
                    addr = null;
                }
            }

            return new NVRDataObject
            {
                ID = row.Id,
                Name = row.Name,
                ProtocolType = row.ProtocolType,
                Address = addr,
            };
        }

        private static NVRChannelDataObject ToChannelDo(LiteNvrRepository.NvrChannelRow row)
        {
            NVRChannelInfoDataObject info = null;
            if (!string.IsNullOrWhiteSpace(row.ChannelInfoJson) && !string.Equals(row.ChannelInfoJson, "null", StringComparison.Ordinal))
            {
                try
                {
                    info = JsonConvert.DeserializeObject<NVRChannelInfoDataObject>(row.ChannelInfoJson);
                }
                catch
                {
                    info = null;
                }
            }

            return new NVRChannelDataObject
            {
                ID = row.Id,
                NVRID = row.NvrId ?? 0,
                DeviceID = row.DeviceId,
                ChannelID = row.ChannelId,
                ChannelInfo = info,
                NVRDO = row.Nvr != null ? ToNvrDo(row.Nvr) : null,
            };
        }
    }
}
