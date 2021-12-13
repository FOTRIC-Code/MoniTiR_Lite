// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Data.SQLite;

using Newtonsoft.Json;

namespace Firefly.Client.Service.LiteDb
{
    /// <summary>
    /// Raw SQLite access for nvr / nvr_channel on shared lite.db (in-proc Store).
    /// </summary>
    public static class LiteNvrRepository
    {
        public sealed class NvrRow
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string ProtocolType { get; set; }
            public string AddrJson { get; set; }
            public DateTime InsertTime { get; set; }
            public DateTime UpdateTime { get; set; }
        }

        public sealed class NvrChannelRow
        {
            public int Id { get; set; }
            public int? NvrId { get; set; }
            public string DeviceId { get; set; }
            public int ChannelId { get; set; }
            public string ChannelInfoJson { get; set; }
            public DateTime InsertTime { get; set; }
            public DateTime UpdateTime { get; set; }
            public NvrRow Nvr { get; set; }
        }

        public static bool ExistsByName(string name)
        {
            StoreSqlite.EnsureNvrSchema();
            using (SQLiteConnection conn = StoreSqlite.OpenConnection())
            using (SQLiteCommand cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT 1 FROM nvr WHERE name=@name LIMIT 1;";
                cmd.Parameters.AddWithValue("@name", name ?? string.Empty);
                return cmd.ExecuteScalar() != null;
            }
        }

        public static List<NvrRow> GetAllNvrs()
        {
            StoreSqlite.EnsureNvrSchema();
            var list = new List<NvrRow>();
            using (SQLiteConnection conn = StoreSqlite.OpenConnection())
            using (SQLiteCommand cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT id, name, protocol_type, addr, insert_time, update_time FROM nvr ORDER BY id;";
                using (SQLiteDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(ReadNvr(reader));
                    }
                }
            }
            return list;
        }

        public static NvrRow GetNvr(int id)
        {
            StoreSqlite.EnsureNvrSchema();
            using (SQLiteConnection conn = StoreSqlite.OpenConnection())
            using (SQLiteCommand cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT id, name, protocol_type, addr, insert_time, update_time FROM nvr WHERE id=@id;";
                cmd.Parameters.AddWithValue("@id", id);
                using (SQLiteDataReader reader = cmd.ExecuteReader())
                {
                    return reader.Read() ? ReadNvr(reader) : null;
                }
            }
        }

        public static int AddNvr(NVRDataObject nvr)
        {
            StoreSqlite.EnsureNvrSchema();
            string now = DateTime.UtcNow.ToString("o");
            string addr = JsonConvert.SerializeObject(nvr.Address ?? new NVRAddressDataObject());
            using (SQLiteConnection conn = StoreSqlite.OpenConnection())
            using (SQLiteCommand cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
INSERT INTO nvr (name, protocol_type, addr, insert_time, update_time)
VALUES (@name, @protocolType, @addr, @now, @now);
SELECT last_insert_rowid();";
                cmd.Parameters.AddWithValue("@name", nvr.Name ?? string.Empty);
                cmd.Parameters.AddWithValue("@protocolType", nvr.ProtocolType ?? string.Empty);
                cmd.Parameters.AddWithValue("@addr", addr);
                cmd.Parameters.AddWithValue("@now", now);
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        public static bool UpdateNvr(NVRDataObject nvr)
        {
            StoreSqlite.EnsureNvrSchema();
            string now = DateTime.UtcNow.ToString("o");
            string addr = JsonConvert.SerializeObject(nvr.Address ?? new NVRAddressDataObject());
            using (SQLiteConnection conn = StoreSqlite.OpenConnection())
            using (SQLiteCommand cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
UPDATE nvr SET name=@name, protocol_type=@protocolType, addr=@addr, update_time=@now
WHERE id=@id;";
                cmd.Parameters.AddWithValue("@id", nvr.ID);
                cmd.Parameters.AddWithValue("@name", nvr.Name ?? string.Empty);
                cmd.Parameters.AddWithValue("@protocolType", nvr.ProtocolType ?? string.Empty);
                cmd.Parameters.AddWithValue("@addr", addr);
                cmd.Parameters.AddWithValue("@now", now);
                return cmd.ExecuteNonQuery() > 0;
            }
        }

        public static bool DeleteNvr(int id)
        {
            StoreSqlite.EnsureNvrSchema();
            using (SQLiteConnection conn = StoreSqlite.OpenConnection())
            using (SQLiteTransaction tx = conn.BeginTransaction())
            {
                using (SQLiteCommand delCh = conn.CreateCommand())
                {
                    delCh.Transaction = tx;
                    delCh.CommandText = "DELETE FROM nvr_channel WHERE nvr_id=@id;";
                    delCh.Parameters.AddWithValue("@id", id);
                    delCh.ExecuteNonQuery();
                }

                int rows;
                using (SQLiteCommand del = conn.CreateCommand())
                {
                    del.Transaction = tx;
                    del.CommandText = "DELETE FROM nvr WHERE id=@id;";
                    del.Parameters.AddWithValue("@id", id);
                    rows = del.ExecuteNonQuery();
                }

                tx.Commit();
                return rows > 0;
            }
        }

        public static List<NvrChannelRow> GetAllChannels()
        {
            return QueryChannels("SELECT c.id, c.nvr_id, c.device_id, c.channel_id, c.channel_info, c.insert_time, c.update_time, n.name, n.protocol_type, n.addr, n.insert_time AS n_insert, n.update_time AS n_update FROM nvr_channel c LEFT JOIN nvr n ON n.id=c.nvr_id ORDER BY c.id;", null);
        }

        public static List<NvrChannelRow> GetChannelsByDeviceId(string deviceId)
        {
            return QueryChannels(@"
SELECT c.id, c.nvr_id, c.device_id, c.channel_id, c.channel_info, c.insert_time, c.update_time,
       n.name, n.protocol_type, n.addr, n.insert_time AS n_insert, n.update_time AS n_update
FROM nvr_channel c LEFT JOIN nvr n ON n.id=c.nvr_id
WHERE c.device_id=@deviceId ORDER BY c.id;", cmd => cmd.Parameters.AddWithValue("@deviceId", deviceId ?? string.Empty));
        }

        public static List<NvrChannelRow> GetChannelsByNvrId(int nvrId)
        {
            return QueryChannels(@"
SELECT c.id, c.nvr_id, c.device_id, c.channel_id, c.channel_info, c.insert_time, c.update_time,
       n.name, n.protocol_type, n.addr, n.insert_time AS n_insert, n.update_time AS n_update
FROM nvr_channel c LEFT JOIN nvr n ON n.id=c.nvr_id
WHERE c.nvr_id=@nvrId ORDER BY c.id;", cmd => cmd.Parameters.AddWithValue("@nvrId", nvrId));
        }

        public static NvrChannelRow GetChannel(int id)
        {
            var list = QueryChannels(@"
SELECT c.id, c.nvr_id, c.device_id, c.channel_id, c.channel_info, c.insert_time, c.update_time,
       n.name, n.protocol_type, n.addr, n.insert_time AS n_insert, n.update_time AS n_update
FROM nvr_channel c LEFT JOIN nvr n ON n.id=c.nvr_id
WHERE c.id=@id;", cmd => cmd.Parameters.AddWithValue("@id", id));
            return list.Count > 0 ? list[0] : null;
        }

        public static bool CreateChannel(NVRChannelDataObject channel)
        {
            StoreSqlite.EnsureNvrSchema();
            string now = DateTime.UtcNow.ToString("o");
            string info = channel.ChannelInfo == null ? null : JsonConvert.SerializeObject(channel.ChannelInfo);
            using (SQLiteConnection conn = StoreSqlite.OpenConnection())
            using (SQLiteCommand cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
INSERT INTO nvr_channel (nvr_id, channel_id, channel_info, device_id, insert_time, update_time)
VALUES (@nvrId, @channelId, @channelInfo, @deviceId, @now, @now);";
                cmd.Parameters.AddWithValue("@nvrId", channel.NVRID);
                cmd.Parameters.AddWithValue("@channelId", channel.ChannelID);
                cmd.Parameters.AddWithValue("@channelInfo", (object)info ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@deviceId", (object)channel.DeviceID ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@now", now);
                return cmd.ExecuteNonQuery() > 0;
            }
        }

        public static bool CreateChannels(IEnumerable<NVRChannelDataObject> channels)
        {
            StoreSqlite.EnsureNvrSchema();
            string now = DateTime.UtcNow.ToString("o");
            using (SQLiteConnection conn = StoreSqlite.OpenConnection())
            using (SQLiteTransaction tx = conn.BeginTransaction())
            {
                foreach (NVRChannelDataObject channel in channels)
                {
                    using (SQLiteCommand cmd = conn.CreateCommand())
                    {
                        cmd.Transaction = tx;
                        string info = channel.ChannelInfo == null ? null : JsonConvert.SerializeObject(channel.ChannelInfo);
                        cmd.CommandText = @"
INSERT INTO nvr_channel (nvr_id, channel_id, channel_info, device_id, insert_time, update_time)
VALUES (@nvrId, @channelId, @channelInfo, @deviceId, @now, @now);";
                        cmd.Parameters.AddWithValue("@nvrId", channel.NVRID);
                        cmd.Parameters.AddWithValue("@channelId", channel.ChannelID);
                        cmd.Parameters.AddWithValue("@channelInfo", (object)info ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@deviceId", (object)channel.DeviceID ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@now", now);
                        cmd.ExecuteNonQuery();
                    }
                }

                tx.Commit();
                return true;
            }
        }

        public static bool UpdateChannel(NVRChannelDataObject channel)
        {
            StoreSqlite.EnsureNvrSchema();
            string now = DateTime.UtcNow.ToString("o");
            string info = channel.ChannelInfo == null ? null : JsonConvert.SerializeObject(channel.ChannelInfo);
            using (SQLiteConnection conn = StoreSqlite.OpenConnection())
            using (SQLiteCommand cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
UPDATE nvr_channel SET nvr_id=@nvrId, channel_id=@channelId, channel_info=@channelInfo, device_id=@deviceId, update_time=@now
WHERE id=@id;";
                cmd.Parameters.AddWithValue("@id", channel.ID);
                cmd.Parameters.AddWithValue("@nvrId", channel.NVRID);
                cmd.Parameters.AddWithValue("@channelId", channel.ChannelID);
                cmd.Parameters.AddWithValue("@channelInfo", (object)info ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@deviceId", (object)channel.DeviceID ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@now", now);
                return cmd.ExecuteNonQuery() > 0;
            }
        }

        public static bool UpdateChannels(IEnumerable<NVRChannelDataObject> channels)
        {
            StoreSqlite.EnsureNvrSchema();
            string now = DateTime.UtcNow.ToString("o");
            using (SQLiteConnection conn = StoreSqlite.OpenConnection())
            using (SQLiteTransaction tx = conn.BeginTransaction())
            {
                foreach (NVRChannelDataObject channel in channels)
                {
                    using (SQLiteCommand cmd = conn.CreateCommand())
                    {
                        cmd.Transaction = tx;
                        string info = channel.ChannelInfo == null ? null : JsonConvert.SerializeObject(channel.ChannelInfo);
                        cmd.CommandText = @"
UPDATE nvr_channel SET nvr_id=@nvrId, channel_id=@channelId, channel_info=@channelInfo, device_id=@deviceId, update_time=@now
WHERE id=@id;";
                        cmd.Parameters.AddWithValue("@id", channel.ID);
                        cmd.Parameters.AddWithValue("@nvrId", channel.NVRID);
                        cmd.Parameters.AddWithValue("@channelId", channel.ChannelID);
                        cmd.Parameters.AddWithValue("@channelInfo", (object)info ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@deviceId", (object)channel.DeviceID ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@now", now);
                        cmd.ExecuteNonQuery();
                    }
                }

                tx.Commit();
                return true;
            }
        }

        public static bool DeleteChannel(int id)
        {
            StoreSqlite.EnsureNvrSchema();
            using (SQLiteConnection conn = StoreSqlite.OpenConnection())
            using (SQLiteCommand cmd = conn.CreateCommand())
            {
                cmd.CommandText = "DELETE FROM nvr_channel WHERE id=@id;";
                cmd.Parameters.AddWithValue("@id", id);
                return cmd.ExecuteNonQuery() > 0;
            }
        }

        private static List<NvrChannelRow> QueryChannels(string sql, Action<SQLiteCommand> bind)
        {
            StoreSqlite.EnsureNvrSchema();
            var list = new List<NvrChannelRow>();
            using (SQLiteConnection conn = StoreSqlite.OpenConnection())
            using (SQLiteCommand cmd = conn.CreateCommand())
            {
                cmd.CommandText = sql;
                bind?.Invoke(cmd);
                using (SQLiteDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(ReadChannel(reader));
                    }
                }
            }
            return list;
        }

        private static NvrRow ReadNvr(SQLiteDataReader reader)
        {
            return new NvrRow
            {
                Id = reader.GetInt32(0),
                Name = reader.IsDBNull(1) ? null : reader.GetString(1),
                ProtocolType = reader.IsDBNull(2) ? null : reader.GetString(2),
                AddrJson = reader.IsDBNull(3) ? null : reader.GetString(3),
                InsertTime = ParseTime(reader.IsDBNull(4) ? null : reader.GetString(4)),
                UpdateTime = ParseTime(reader.IsDBNull(5) ? null : reader.GetString(5)),
            };
        }

        private static NvrChannelRow ReadChannel(SQLiteDataReader reader)
        {
            var row = new NvrChannelRow
            {
                Id = reader.GetInt32(0),
                NvrId = reader.IsDBNull(1) ? (int?)null : reader.GetInt32(1),
                DeviceId = reader.IsDBNull(2) ? null : reader.GetString(2),
                ChannelId = reader.IsDBNull(3) ? 0 : reader.GetInt32(3),
                ChannelInfoJson = reader.IsDBNull(4) ? null : reader.GetString(4),
                InsertTime = ParseTime(reader.IsDBNull(5) ? null : reader.GetString(5)),
                UpdateTime = ParseTime(reader.IsDBNull(6) ? null : reader.GetString(6)),
            };

            if (!reader.IsDBNull(1) && reader.FieldCount > 7 && !reader.IsDBNull(7))
            {
                row.Nvr = new NvrRow
                {
                    Id = reader.GetInt32(1),
                    Name = reader.IsDBNull(7) ? null : reader.GetString(7),
                    ProtocolType = reader.IsDBNull(8) ? null : reader.GetString(8),
                    AddrJson = reader.IsDBNull(9) ? null : reader.GetString(9),
                    InsertTime = ParseTime(reader.IsDBNull(10) ? null : reader.GetString(10)),
                    UpdateTime = ParseTime(reader.IsDBNull(11) ? null : reader.GetString(11)),
                };
            }

            return row;
        }

        private static DateTime ParseTime(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return DateTime.UtcNow;
            }

            return DateTime.TryParse(value, null, System.Globalization.DateTimeStyles.RoundtripKind, out DateTime dt)
                ? dt
                : DateTime.UtcNow;
        }
    }
}
