// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using Firefly.Client.Common;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;

namespace Firefly.Client.Service
{
    public class DeviceDO
    {
        public static List<ServerType> BaseServers = new List<ServerType> { ServerType.StoreAgent };
        private long _deviceStatus;
        private string _deviceStatusDesc;

        #region Constructors

        public DeviceDO()
        {
            ID = string.Empty;
            Name = string.Empty;
            DeviceAddress = new List<DeviceAddressDataObject>();
            AgentAddress = new List<ServerDO>();
        }

        #endregion Constructors

        #region Properties

        [JsonProperty("id")]
        public string ID { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("protocolType")]
        public string ProtocolType { get; set; }

        [JsonProperty("deviceAddr")]
        public List<DeviceAddressDataObject> DeviceAddress { get; set; }

        [JsonProperty("agentAddr")]
        public List<ServerDO> AgentAddress { get; set; }

        /// <summary>
        /// Device engine config JSON (shared SQLite conf column). Not persisted via agent_ids.
        /// </summary>
        [JsonProperty("conf")]
        public string Conf { get; set; }

        public ServerDO DefaultAgent()
        {
            return AgentAddress.FirstOrDefault(item =>
            {
                Enum.TryParse(item.Type, out ServerType serverType);
                return !BaseServers.Contains(serverType);
            });
        }

        public ServerDO GetAgent(ServerType serverType)
        {
            return AgentAddress?.FirstOrDefault(it => string.Equals(it.Type, serverType.ToString()));
        }

        public void SetDeviceStatus(DeviceStatusDataObject deviceStatusDO)
        {
            _deviceStatusDesc = deviceStatusDO.StatusDescription;
            if (deviceStatusDO?.Status == DeviceStatus.Online)
            {
                Interlocked.Exchange(ref _deviceStatus, 1);
            }
            else
            {
                Interlocked.Exchange(ref _deviceStatus, 0);
            }
        }

        public string GetDeviceStatusDesc() => _deviceStatusDesc;

        public bool IsOnline() => Interlocked.Read(ref _deviceStatus) == 1;

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj is DeviceDO deviceDO)
            {
                bool isChannelEqual = false;
                if (DeviceAddress?.Count == deviceDO.DeviceAddress?.Count)
                {
                    isChannelEqual = true;
                    if (DeviceAddress?.Count > 0)
                    {
                        for (int i = 0; i < DeviceAddress.Count; i++)
                        {
                            isChannelEqual = DeviceAddress[i].Equals(deviceDO.DeviceAddress[i]);
                            if (!isChannelEqual)
                            {
                                break;
                            }
                        }
                    }
                }

                return ID == deviceDO.ID
                       && string.Equals(Name, deviceDO.Name)
                       && isChannelEqual;
            }
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        #endregion Properties
    }

    public class DeviceAddressDataObject
    {
        public DeviceAddressDataObject()
        { }

        [JsonProperty("channelID")]
        public int ChannelID { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("protocolType")]
        public string ProtocolType { get; set; }

        [JsonProperty("ip")]
        public string IP { get; set; }

        [JsonProperty("port")]
        public int Port { get; set; }

        [JsonProperty("username")]
        public string Username { get; set; }

        [JsonProperty("password")]
        public string Password { get; set; }

        [JsonProperty("model")]
        public string Model { get; set; }

        [JsonProperty("sn")]
        public string SN { get; set; }

        [JsonProperty("firewareVersion")]
        public string FirewareVersion { get; set; }

        [JsonProperty("paramID")]
        public string ParamID { get; set; }

        public static bool IsIRProtocolType(string protocolType)
        {
            return (protocolType.Equals(IRCameraType.IRTEKSDK_NET.ToString(), StringComparison.OrdinalIgnoreCase) ||
                    protocolType.Equals(IRCameraType.IRTEKSDK2_NET.ToString(), StringComparison.OrdinalIgnoreCase) ||
                    protocolType.Equals(IRCameraType.FASDK_NET.ToString(), StringComparison.OrdinalIgnoreCase) ||
                    protocolType.Equals(IRCameraType.SVSDK_NET.ToString(), StringComparison.OrdinalIgnoreCase) ||
                    protocolType.Equals(IRCameraType.RDSDK_NET.ToString(), StringComparison.OrdinalIgnoreCase) ||
                    protocolType.Equals(IRCameraType.CZF_ONVIF_IR_NET.ToString(), StringComparison.OrdinalIgnoreCase));
        }

        public static bool IsDCProtocolType(string protocolType)
        {
            return (protocolType.Equals(DCCameraType.HIKSDK_NET.ToString(), StringComparison.OrdinalIgnoreCase) ||
                    protocolType.Equals(DCCameraType.DHSDK_NET.ToString(), StringComparison.OrdinalIgnoreCase) ||
                    protocolType.Equals(DCCameraType.ONVIF_DC_NET.ToString(), StringComparison.OrdinalIgnoreCase));
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj is DeviceAddressDataObject deviceAddressDO)
            {
                return ChannelID == deviceAddressDO.ChannelID
                    && string.Equals(IP, deviceAddressDO.IP)
                    && string.Equals(Port, deviceAddressDO.Port);
            }
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}