// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using System.Threading.Tasks;

using Firefly.Client.Common;

namespace Firefly.Client.Service.Abstractions
{
    /// <summary>
    /// In-process agent health — no HTTP; ready when Fire runtime started.
    /// </summary>
    public sealed class InProcAgentHealth : IAgentHealth
    {
        public Task<AgentConfig> GetConfigAsync(string serverAddr)
        {
            // Mirror ServerConfigOutputModel.CreateLiteDefault so DeviceDetail can
            // enable channel edit (CanEditChannel) and load SN / firmware.
            return Task.FromResult(CreateLiteConfig());
        }

        public Task<bool> PingAsync(string serverAddr)
        {
            return Task.FromResult(FireRuntimeServices.DeviceRuntime != null && FireRuntimeServices.DeviceRuntime.IsReady);
        }

        private static AgentConfig CreateLiteConfig()
        {
            string[] irProtocols =
            {
                IRCameraType.IRTEKSDK_NET.ToString(),
                IRCameraType.IRTEKSDK2_NET.ToString(),
                IRCameraType.RDSDK_NET.ToString(),
                IRCameraType.SVSDK_NET.ToString(),
                IRCameraType.CZF_ONVIF_IR_NET.ToString(),
            };

            return new AgentConfig
            {
                Name = "Local Infrared",
                MetaData = new AgentMetaData
                {
                    ProtocolList = new List<AgentProtocol>
                    {
                        new AgentProtocol
                        {
                            ProtocolType = "FireMonitor_S",
                            ChannelList = new List<Channel>
                            {
                                new Channel
                                {
                                    ID = 0,
                                    Type = "IRCamera",
                                    SupportedProtocolTypeList = irProtocols,
                                },
                            },
                        },
                        new AgentProtocol
                        {
                            ProtocolType = "FireMonitor_D",
                            ChannelList = new List<Channel>
                            {
                                new Channel
                                {
                                    ID = 0,
                                    Type = "IRCamera",
                                    SupportedProtocolTypeList = irProtocols,
                                },
                                new Channel
                                {
                                    ID = 1,
                                    Type = "DCCamera",
                                    SupportedProtocolTypeList = new[]
                                    {
                                        DCCameraType.ONVIF_DC_NET.ToString(),
                                    },
                                },
                            },
                        },
                    },
                },
            };
        }
    }
}
