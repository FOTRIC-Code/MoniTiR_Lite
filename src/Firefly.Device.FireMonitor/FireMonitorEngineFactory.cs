// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.IO;
using System.Text.Json;
using System.Collections.Concurrent;
using System.Threading.Tasks;

using OWB.IRCamera;
using OWB.DCCamera;
using Firefly.Painter;
using Firefly.Stream.Core;

namespace Firefly.Device.FireMonitor
{
    public class FireMonitorEngineFactory : IDisposable
    {
        private ConcurrentDictionary<string, DeviceEngine> _deviceDict;

        public DeviceLoadHandler DeviceLoading;

        /// <summary>
        /// Raised after the engine instance is created and before <see cref="DeviceEngine.Init"/>.
        /// Hosts (Lite in-proc) attach OnDataEvent here so the first samples are not dropped.
        /// </summary>
        public Action<string, DeviceEngine> EngineCreated;

        private static FireMonitorEngineFactory _instance = new FireMonitorEngineFactory();

        private const int DEFAULT_PORT = 29080;
        private const int DEFAULT_RTP_PORT = 10000;
        private const int DEFAULT_RTC_PORT = 8000;
        private const int DEFAULT_HTTP_PORT = 19080;
        private const string DEFAULT_STREAM_CONTENT_TYPE = "video";
        private const string CONFIG_FILE_PATH = "FireMonitorRTSPServerConfig.json";
        
        protected FireMonitorEngineFactory()
        {
            _deviceDict = new ConcurrentDictionary<string, DeviceEngine>();

            RTSPServerConfig rtspServerConfig = new RTSPServerConfig();
            string configFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, CONFIG_FILE_PATH);
            if (!File.Exists(configFilePath))
            {
                rtspServerConfig.RTSPServerPort = DEFAULT_PORT;
                rtspServerConfig.RTPServerPort = DEFAULT_RTP_PORT;
                rtspServerConfig.RTCServerPort = DEFAULT_RTC_PORT;
                rtspServerConfig.HTTPServerPort = DEFAULT_HTTP_PORT;
                rtspServerConfig.StreamContentType = DEFAULT_STREAM_CONTENT_TYPE;
            }
            else
            {
                rtspServerConfig = JsonSerializer.Deserialize<RTSPServerConfig>(File.ReadAllText(configFilePath), Constants.DEFAULT_SERIALIZER_OPTIONS) ?? new RTSPServerConfig()
                {
                    RTSPServerPort = DEFAULT_PORT,
                    RTPServerPort = DEFAULT_RTP_PORT,
                    RTCServerPort = DEFAULT_RTC_PORT,
                    HTTPServerPort = DEFAULT_HTTP_PORT,
                    StreamContentType = DEFAULT_STREAM_CONTENT_TYPE,
                };
            }
            MediaKitRtspServer.Init(rtspServerConfig);
        }

        public static FireMonitorEngineFactory Instance => _instance;

        public virtual bool TryGet(string deviceID, out DeviceEngine deviceEngine)
        {
            bool isSuccessful = false;
            deviceEngine = null;

            if (_deviceDict.TryGetValue(deviceID, out DeviceEngine oldDeviceEngine))
            {
                deviceEngine = oldDeviceEngine;
                isSuccessful = true;
            }

            return isSuccessful;
        }

        public virtual async Task<DeviceEngine> TryGetOrAdd(string deviceID)
        {
            DeviceEngine deviceEngine = null;

            if (_deviceDict.TryGetValue(deviceID, out DeviceEngine oldDeviceEngine))
            {
                return oldDeviceEngine;
            }

            if (DeviceLoading == null)
            {
                return null;
            }

            DeviceLoadBO deviceLoadBO = await DeviceLoading(this, new DeviceLoadArgs { DeviceID = deviceID });
            if (deviceLoadBO == null)
            {
                return null;
            }

            DeviceEngine newDeviceEngine;
            Enum.TryParse(deviceLoadBO.DeviceBO.ProtocolType, out ProtocolType protocolType);
            switch (protocolType)
            {
                case ProtocolType.FireMonitor_S:
                case ProtocolType.FireMonitor_D:
                    newDeviceEngine = new FireMonitorDeviceEngine();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            try
            {
                EngineCreated?.Invoke(deviceLoadBO.DeviceBO?.ID, newDeviceEngine);
            }
            catch
            {
            }

            bool initSuccess = newDeviceEngine.Init(deviceLoadBO.DeviceBO, deviceLoadBO.DeviceConfigStr);
            if (!initSuccess)
            {
                return null;
            }

            deviceEngine = newDeviceEngine;
            _deviceDict.TryAdd(deviceID, newDeviceEngine);

            DevicePainterFactory.Instance.TryGetOrAdd(deviceID, out _);

            return deviceEngine;
        }

        public bool TryRemove(string deviceID)
        {
            bool removeSuccess = false;

            if (_deviceDict.TryRemove(deviceID, out DeviceEngine oldDeviceEngine))
            {
                oldDeviceEngine.Destory();

                removeSuccess = true;

                DevicePainterFactory.Instance.TryRemove(deviceID);
            }

            return removeSuccess;
        }

        public async Task<bool> TryUpdate(string deviceID)
        {
            bool successUpdate = false;

            if (_deviceDict.TryGetValue(deviceID, out DeviceEngine oldDeviceEngine))
            {
                if (DeviceLoading != null)
                {
                    DeviceLoadBO deviceLoadBO = await DeviceLoading(this, new DeviceLoadArgs { DeviceID = deviceID });
                    if (deviceLoadBO != null)
                    {
                        // this lock can avoid init device many times in multiple threads.
                        // if above case is executed will result in: sample/alarm/cruise task will start multiple loop thread: execute multiple start and only one stop is executed.
                        lock (oldDeviceEngine)
                        {
                            successUpdate = oldDeviceEngine.Init(deviceLoadBO.DeviceBO, deviceLoadBO.DeviceConfigStr);
                        }
                    }
                }
            }

            return successUpdate;
        }

        public void Dispose()
        {
            MediaKitRtspServer.Dispose();
            OWBIRCameraAdapter.UnInit();
            OWBDCCameraAdapter.UnInit();
        }
    }
}
