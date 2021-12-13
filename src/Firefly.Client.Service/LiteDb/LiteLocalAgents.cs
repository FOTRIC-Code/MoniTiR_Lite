// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;

namespace Firefly.Client.Service.LiteDb
{
    /// <summary>
    /// In-memory fixed local Fire/Store endpoints. Replaces agent table.
    /// </summary>
    public static class LiteLocalAgents
    {
        public const string LocalIp = "127.0.0.1";
        public const int FirePort = 9080;
        public const int StorePort = 9070;

        public static ServerDO CreateFire(int status = 0)
        {
            return new ServerDO
            {
                ID = LiteLocalAgentIds.Fire,
                Name = "Local Infrared",
                Type = ServerType.FireMonitorAgent.ToString(),
                IP = LocalIp,
                Port = FirePort,
                Status = status,
            };
        }

        public static ServerDO CreateStore(int status = 0)
        {
            return new ServerDO
            {
                ID = LiteLocalAgentIds.Store,
                Name = "Local Storage",
                Type = ServerType.StoreAgent.ToString(),
                IP = LocalIp,
                Port = StorePort,
                Status = status,
            };
        }

        public static List<ServerDO> GetAll(int fireStatus = 0, int storeStatus = 0)
        {
            return new List<ServerDO>
            {
                CreateFire(fireStatus),
                CreateStore(storeStatus),
            };
        }

        public static ServerDO Get(int id)
        {
            if (id == LiteLocalAgentIds.Fire)
            {
                return CreateFire();
            }
            if (id == LiteLocalAgentIds.Store)
            {
                return CreateStore();
            }
            return null;
        }
    }
}
