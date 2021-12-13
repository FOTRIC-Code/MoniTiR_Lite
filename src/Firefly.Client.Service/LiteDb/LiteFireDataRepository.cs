// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Globalization;

using Newtonsoft.Json;

namespace Firefly.Client.Service.LiteDb
{
    /// <summary>
    /// System.Data.SQLite access for Fire private tables fire_data / file (replaces Fire EF Core DO).
    /// </summary>
    public static class LiteFireDataRepository
    {
        public static void EnsureTables()
        {
            LiteDatabase.EnsureInitialized();
        }

        public static void InsertSample(
            string deviceId,
            string deviceName,
            int presetId,
            string presetName,
            string markerName,
            float? maxTemp,
            float? minTemp,
            float? avgTemp,
            float? ruleTemp,
            long timestamp,
            DateTime happenTime,
            string rootPath,
            string imagePath,
            string filesJson)
        {
            LiteDatabase.EnsureInitialized();
            string now = DateTime.UtcNow.ToString("o");
            using (SQLiteConnection conn = LiteDatabase.OpenConnection())
            using (SQLiteCommand cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
INSERT INTO fire_data (
  avg_temp, device_id, device_name, files, happen_time, image_path, insert_time,
  marker_name, max_temp, min_temp, preset_id, preset_name, root_path, rule_temp, timestamp, update_time)
VALUES (
  @avg, @deviceId, @deviceName, @files, @happen, @image, @now,
  @marker, @max, @min, @presetId, @presetName, @root, @rule, @ts, @now);";
                cmd.Parameters.AddWithValue("@avg", (object)avgTemp ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@deviceId", deviceId ?? string.Empty);
                cmd.Parameters.AddWithValue("@deviceName", (object)deviceName ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@files", (object)filesJson ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@happen", happenTime.ToString("o"));
                cmd.Parameters.AddWithValue("@image", (object)imagePath ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@now", now);
                cmd.Parameters.AddWithValue("@marker", (object)markerName ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@max", (object)maxTemp ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@min", (object)minTemp ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@presetId", presetId);
                cmd.Parameters.AddWithValue("@presetName", (object)presetName ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@root", (object)rootPath ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@rule", (object)ruleTemp ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ts", timestamp);
                cmd.ExecuteNonQuery();
            }
        }

        public static int InsertFile(string path, int type, string rootPath)
        {
            LiteDatabase.EnsureInitialized();
            string now = DateTime.UtcNow.ToString("o");
            using (SQLiteConnection conn = LiteDatabase.OpenConnection())
            using (SQLiteCommand cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
INSERT INTO file (path, type, insert_time, root_path, update_time)
VALUES (@path, @type, @now, @root, @now);
SELECT last_insert_rowid();";
                cmd.Parameters.AddWithValue("@path", path ?? string.Empty);
                cmd.Parameters.AddWithValue("@type", type);
                cmd.Parameters.AddWithValue("@now", now);
                cmd.Parameters.AddWithValue("@root", rootPath ?? string.Empty);
                object id = cmd.ExecuteScalar();
                return Convert.ToInt32(id);
            }
        }

        public static (List<DeviceDataDO> Items, int Total) GetByPagination(DeviceDataQueryParam param)
        {
            LiteDatabase.EnsureInitialized();
            int page = param?.Page > 0 ? param.Page : 1;
            int pageSize = param?.PageSize ?? 20;
            using (SQLiteConnection conn = LiteDatabase.OpenConnection())
            {
                var where = new List<string>();
                if (!string.IsNullOrWhiteSpace(param?.DeviceID))
                {
                    where.Add("device_id=@deviceId");
                }

                if (param != null && param.PresetID > 0)
                {
                    where.Add("preset_id=@presetId");
                }

                if (param != null && param.StartTime > DateTime.MinValue)
                {
                    where.Add("happen_time>=@start");
                }

                if (param != null && param.EndTime > DateTime.MinValue)
                {
                    where.Add("happen_time<=@end");
                }

                string whereSql = where.Count > 0 ? " WHERE " + string.Join(" AND ", where) : string.Empty;

                int total;
                using (SQLiteCommand countCmd = conn.CreateCommand())
                {
                    countCmd.CommandText = "SELECT COUNT(1) FROM fire_data" + whereSql + ";";
                    BindQueryParams(countCmd, param);
                    total = Convert.ToInt32(countCmd.ExecuteScalar());
                }

                var list = new List<DeviceDataDO>();
                using (SQLiteCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
SELECT id, happen_time, device_id, device_name, preset_id, preset_name, marker_name,
       max_temp, min_temp, avg_temp, rule_temp, files, update_time, insert_time, root_path
FROM fire_data" + whereSql + " ORDER BY happen_time DESC LIMIT @limit OFFSET @offset;";
                    BindQueryParams(cmd, param);
                    cmd.Parameters.AddWithValue("@limit", pageSize > 0 ? pageSize : 20);
                    cmd.Parameters.AddWithValue("@offset", (page - 1) * (pageSize > 0 ? pageSize : 20));
                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(ReadDeviceData(reader));
                        }
                    }
                }

                return (list, total);
            }
        }

        public static List<DeviceDataStatisticsDO> GetStatistics(DeviceDataStatisticsQueryParam param)
        {
            LiteDatabase.EnsureInitialized();
            var list = new List<DeviceDataStatisticsDO>();
            using (SQLiteConnection conn = LiteDatabase.OpenConnection())
            using (SQLiteCommand cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
SELECT device_id, device_name, preset_id, preset_name, COUNT(1) AS cnt
FROM fire_data
WHERE (@hasStart=0 OR happen_time>=@start)
  AND (@hasEnd=0 OR happen_time<=@end)
GROUP BY device_id, device_name, preset_id, preset_name;";
                bool hasStart = param != null && param.BeginTime > DateTime.MinValue;
                bool hasEnd = param != null && param.EndTime > DateTime.MinValue;
                cmd.Parameters.AddWithValue("@hasStart", hasStart ? 1 : 0);
                cmd.Parameters.AddWithValue("@hasEnd", hasEnd ? 1 : 0);
                cmd.Parameters.AddWithValue("@start", hasStart ? (object)param.BeginTime.ToString("o") : DBNull.Value);
                cmd.Parameters.AddWithValue("@end", hasEnd ? (object)param.EndTime.ToString("o") : DBNull.Value);
                using (SQLiteDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int count = reader.IsDBNull(4) ? 0 : Convert.ToInt32(reader.GetValue(4));
                        list.Add(new DeviceDataStatisticsDO
                        {
                            DeviceID = reader.IsDBNull(0) ? null : reader.GetString(0),
                            DeviceName = reader.IsDBNull(1) ? null : reader.GetString(1),
                            PresetID = reader.IsDBNull(2) ? 0 : reader.GetInt32(2),
                            PresetName = reader.IsDBNull(3) ? null : reader.GetString(3),
                            Statistics = new Statistics { DataStatistics = count },
                        });
                    }
                }
            }

            return list;
        }

        private static void BindQueryParams(SQLiteCommand cmd, DeviceDataQueryParam param)
        {
            if (!string.IsNullOrWhiteSpace(param?.DeviceID))
            {
                cmd.Parameters.AddWithValue("@deviceId", param.DeviceID);
            }

            if (param != null && param.PresetID > 0)
            {
                cmd.Parameters.AddWithValue("@presetId", param.PresetID);
            }

            if (param != null && param.StartTime > DateTime.MinValue)
            {
                cmd.Parameters.AddWithValue("@start", param.StartTime.ToString("o"));
            }

            if (param != null && param.EndTime > DateTime.MinValue)
            {
                cmd.Parameters.AddWithValue("@end", param.EndTime.ToString("o"));
            }
        }

        private static DeviceDataDO ReadDeviceData(SQLiteDataReader reader)
        {
            string files = reader.IsDBNull(11) ? null : reader.GetString(11);
            string rootPath = reader.FieldCount > 14 && !reader.IsDBNull(14) ? reader.GetString(14) : null;
            List<ImageInfo> images = null;
            if (!string.IsNullOrWhiteSpace(files))
            {
                try
                {
                    images = JsonConvert.DeserializeObject<List<ImageInfo>>(files);
                    ResolveImageUrls(images, rootPath);
                }
                catch
                {
                    images = null;
                }
            }

            return new DeviceDataDO
            {
                ID = reader.GetInt32(0),
                HappenTime = ParseDate(reader.IsDBNull(1) ? null : reader.GetString(1)),
                DeviceID = reader.IsDBNull(2) ? null : reader.GetString(2),
                DeviceName = reader.IsDBNull(3) ? null : reader.GetString(3),
                PresetID = reader.IsDBNull(4) ? 0 : reader.GetInt32(4),
                PresetName = reader.IsDBNull(5) ? null : reader.GetString(5),
                MarkerName = reader.IsDBNull(6) ? null : reader.GetString(6),
                MaxTemp = reader.IsDBNull(7) ? 0 : Convert.ToSingle(reader.GetValue(7)),
                MinTemp = reader.IsDBNull(8) ? 0 : Convert.ToSingle(reader.GetValue(8)),
                AvgTemp = reader.IsDBNull(9) ? 0 : Convert.ToSingle(reader.GetValue(9)),
                CurrentValue = reader.IsDBNull(10) ? 0 : Convert.ToSingle(reader.GetValue(10)),
                ImageInfos = images,
                UpdateTime = ParseDate(reader.IsDBNull(12) ? null : reader.GetString(12)),
                InsertTime = ParseDate(reader.IsDBNull(13) ? null : reader.GetString(13)),
            };
        }

        /// <summary>
        /// In-proc sample SaveFile stores relative paths under root_path (…/sample).
        /// UI / IRFileViewer historically expected HTTP URLs — resolve to absolute local paths.
        /// </summary>
        private static void ResolveImageUrls(List<ImageInfo> images, string rootPath)
        {
            if (images == null || images.Count == 0)
            {
                return;
            }

            foreach (ImageInfo image in images)
            {
                if (image == null || string.IsNullOrWhiteSpace(image.URL))
                {
                    continue;
                }

                string resolved = LocalImagePath.ResolveAbsolute(rootPath, image.URL);
                if (!string.IsNullOrWhiteSpace(resolved))
                {
                    image.URL = resolved;
                }
            }
        }

        private static DateTime ParseDate(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return DateTime.MinValue;
            }

            DateTime dt;
            return DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out dt)
                ? dt
                : DateTime.MinValue;
        }
    }
}
