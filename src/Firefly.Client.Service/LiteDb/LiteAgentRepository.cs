// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Firefly.Client.Service.LiteDb
{
    /// <summary>
    /// Compatibility facade: fixed local agents in memory (no SQLite agent table).
    /// Upsert only refreshes runtime Status / Version held in process.
    /// </summary>
    public static class LiteAgentRepository
    {
        private static readonly ConcurrentDictionary<int, ServerDO> Runtime = new ConcurrentDictionary<int, ServerDO>();

        static LiteAgentRepository()
        {
            foreach (ServerDO s in LiteLocalAgents.GetAll())
            {
                Runtime[s.ID] = s;
            }
        }

        public static List<ServerDO> GetAll()
        {
            EnsureSeed();
            return new List<ServerDO>
            {
                Clone(Runtime[LiteLocalAgentIds.Fire]),
                Clone(Runtime[LiteLocalAgentIds.Store]),
            };
        }

        public static ServerDO Get(int id)
        {
            EnsureSeed();
            return Runtime.TryGetValue(id, out ServerDO s) ? Clone(s) : null;
        }

        public static void Upsert(ServerDO server)
        {
            if (server == null || (server.ID != LiteLocalAgentIds.Fire && server.ID != LiteLocalAgentIds.Store))
            {
                return;
            }

            EnsureSeed();
            ServerDO baseNode = server.ID == LiteLocalAgentIds.Fire
                ? LiteLocalAgents.CreateFire(server.Status)
                : LiteLocalAgents.CreateStore(server.Status);
            baseNode.Name = string.IsNullOrWhiteSpace(server.Name) ? baseNode.Name : server.Name;
            baseNode.Version = server.Version;
            baseNode.DeviceCount = server.DeviceCount;
            baseNode.StartTime = server.StartTime;
            // Keep fixed IP/port/type for Lite.
            Runtime[server.ID] = baseNode;
        }

        public static bool Delete(int id)
        {
            // Fixed local nodes cannot be deleted.
            return false;
        }

        private static void EnsureSeed()
        {
            Runtime.TryAdd(LiteLocalAgentIds.Fire, LiteLocalAgents.CreateFire());
            Runtime.TryAdd(LiteLocalAgentIds.Store, LiteLocalAgents.CreateStore());
        }

        private static ServerDO Clone(ServerDO s)
        {
            return new ServerDO
            {
                ID = s.ID,
                Name = s.Name,
                Type = s.Type,
                IP = s.IP,
                Port = s.Port,
                Status = s.Status,
                DeviceCount = s.DeviceCount,
                StartTime = s.StartTime,
                Version = s.Version,
            };
        }
    }
}
