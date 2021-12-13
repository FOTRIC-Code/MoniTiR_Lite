// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Data.SQLite;
using Newtonsoft.Json;

namespace Firefly.Client.Service.LiteDb
{
    public static class LiteDeviceRepository
    {
        public static (List<DeviceDO> Items, int Total) GetByPagination(PaginationInfo pageInfo)
        {
            LiteDatabase.EnsureInitialized();
            int page = pageInfo?.Page > 0 ? pageInfo.Page : 1;
            int pageSize = pageInfo?.PageSize ?? -1;
            string search = pageInfo?.SearchStr?.Trim();

            using (SQLiteConnection conn = LiteDatabase.OpenConnection())
            {
                string where = string.Empty;
                if (!string.IsNullOrWhiteSpace(search))
                {
                    where = " WHERE (name LIKE @q OR id LIKE @q) ";
                }

                int total;
                using (SQLiteCommand countCmd = conn.CreateCommand())
                {
                    countCmd.CommandText = "SELECT COUNT(1) FROM device" + where + ";";
                    if (!string.IsNullOrWhiteSpace(search))
                    {
                        countCmd.Parameters.AddWithValue("@q", "%" + search + "%");
                    }
                    total = Convert.ToInt32(countCmd.ExecuteScalar());
                }

                List<DeviceDO> list = new List<DeviceDO>();
                using (SQLiteCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT id, name, protocol_type, addr, conf FROM device" + where + " ORDER BY name";
                    if (pageSize > 0)
                    {
                        cmd.CommandText += " LIMIT @limit OFFSET @offset";
                        cmd.Parameters.AddWithValue("@limit", pageSize);
                        cmd.Parameters.AddWithValue("@offset", (page - 1) * pageSize);
                    }
                    if (!string.IsNullOrWhiteSpace(search))
                    {
                        cmd.Parameters.AddWithValue("@q", "%" + search + "%");
                    }

                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(ReadDevice(reader));
                        }
                    }
                }

                AttachLocalAgents(list);
                return (list, total);
            }
        }

        public static DeviceDO Get(string id)
        {
            LiteDatabase.EnsureInitialized();
            using (SQLiteConnection conn = LiteDatabase.OpenConnection())
            using (SQLiteCommand cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT id, name, protocol_type, addr, conf FROM device WHERE id=@id;";
                cmd.Parameters.AddWithValue("@id", id);
                using (SQLiteDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        DeviceDO device = ReadDevice(reader);
                        AttachLocalAgents(new List<DeviceDO> { device });
                        return device;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Lite has a single local Fire/Store pair; all devices are treated as local.
        /// </summary>
        public static List<DeviceDO> GetByAgentId(int agentId)
        {
            var all = GetByPagination(new PaginationInfo { Page = 1, PageSize = -1 });
            return all.Items;
        }

        public static bool Upsert(DeviceDO device)
        {
            LiteDatabase.EnsureInitialized();
            if (string.IsNullOrWhiteSpace(device.ID))
            {
                device.ID = Guid.NewGuid().ToString("N");
            }

            string now = DateTime.UtcNow.ToString("o");
            string addr = JsonConvert.SerializeObject(device.DeviceAddress ?? new List<DeviceAddressDataObject>());

            using (SQLiteConnection conn = LiteDatabase.OpenConnection())
            using (SQLiteCommand cmd = conn.CreateCommand())
            {
                // Shared lite.db with Fire: Client often has no conf. Never wipe Fire's conf with null.
                cmd.CommandText = @"
INSERT INTO device (id, name, protocol_type, addr, conf, insert_time, update_time)
VALUES (@id, @name, @protocolType, @addr, @conf, @now, @now)
ON CONFLICT(id) DO UPDATE SET
  name=excluded.name,
  protocol_type=excluded.protocol_type,
  addr=excluded.addr,
  conf=CASE
    WHEN excluded.conf IS NULL OR excluded.conf = '' THEN device.conf
    ELSE excluded.conf
  END,
  update_time=excluded.update_time;";
                cmd.Parameters.AddWithValue("@id", device.ID);
                cmd.Parameters.AddWithValue("@name", device.Name ?? string.Empty);
                cmd.Parameters.AddWithValue("@protocolType", (object)device.ProtocolType ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@addr", addr);
                cmd.Parameters.AddWithValue("@conf", string.IsNullOrWhiteSpace(device.Conf) ? (object)DBNull.Value : device.Conf);
                cmd.Parameters.AddWithValue("@now", now);
                return cmd.ExecuteNonQuery() > 0;
            }
        }

        public static bool UpdateConf(string id, string conf)
        {
            LiteDatabase.EnsureInitialized();
            using (SQLiteConnection conn = LiteDatabase.OpenConnection())
            using (SQLiteCommand cmd = conn.CreateCommand())
            {
                cmd.CommandText = "UPDATE device SET conf=@conf, update_time=@now WHERE id=@id;";
                cmd.Parameters.AddWithValue("@conf", conf ?? string.Empty);
                cmd.Parameters.AddWithValue("@now", DateTime.UtcNow.ToString("o"));
                cmd.Parameters.AddWithValue("@id", id);
                return cmd.ExecuteNonQuery() > 0;
            }
        }

        public static bool Delete(string id)
        {
            LiteDatabase.EnsureInitialized();
            using (SQLiteConnection conn = LiteDatabase.OpenConnection())
            using (SQLiteCommand cmd = conn.CreateCommand())
            {
                cmd.CommandText = "DELETE FROM device WHERE id=@id;";
                cmd.Parameters.AddWithValue("@id", id);
                return cmd.ExecuteNonQuery() > 0;
            }
        }

        private static DeviceDO ReadDevice(SQLiteDataReader reader)
        {
            string addrJson = reader.IsDBNull(3) ? "[]" : reader.GetString(3);
            return new DeviceDO
            {
                ID = reader.GetString(0),
                Name = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                ProtocolType = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                DeviceAddress = JsonConvert.DeserializeObject<List<DeviceAddressDataObject>>(addrJson) ?? new List<DeviceAddressDataObject>(),
                Conf = reader.IsDBNull(4) ? null : reader.GetString(4),
                AgentAddress = new List<ServerDO>(),
            };
        }

        private static void AttachLocalAgents(List<DeviceDO> devices)
        {
            if (devices == null || devices.Count == 0)
            {
                return;
            }

            List<ServerDO> agents = LiteAgentRepository.GetAll();
            foreach (DeviceDO device in devices)
            {
                device.AgentAddress = agents;
            }
        }
    }
}
