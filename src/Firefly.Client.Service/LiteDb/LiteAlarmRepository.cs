// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Globalization;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace Firefly.Client.Service.LiteDb
{
    public static class LiteAlarmRepository
    {
        public static (List<AlarmDO> Items, int Total) GetByPagination(TimePageInfo pageInfo)
        {
            LiteDatabase.EnsureInitialized();
            int page = pageInfo?.Page > 0 ? pageInfo.Page : 1;
            int pageSize = pageInfo?.PageSize ?? -1;
            DateTime start = pageInfo?.StartTime.ToUniversalTime() ?? DateTime.MinValue.ToUniversalTime();
            DateTime end = pageInfo?.EndTime.ToUniversalTime() ?? DateTime.MaxValue.ToUniversalTime();

            using (SQLiteConnection conn = LiteDatabase.OpenConnection())
            {
                List<string> filters = new List<string>
                {
                    "alarm_time >= @start",
                    "alarm_time <= @end"
                };
                if (pageInfo != null && pageInfo.AlarmConfirmationStatus == AlarmConfirmationStatus.Confirmed)
                {
                    filters.Add("is_confirmed = 1");
                }
                else if (pageInfo != null && pageInfo.AlarmConfirmationStatus == AlarmConfirmationStatus.Unconfirmed)
                {
                    filters.Add("is_confirmed = 0");
                }

                List<string> types = null;
                if (!string.IsNullOrWhiteSpace(pageInfo?.AlarmTypes))
                {
                    types = pageInfo.AlarmTypes.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).ToList();
                    if (types.Count > 0)
                    {
                        filters.Add($"alarm_type IN ({string.Join(",", types.Select((_, i) => "@t" + i))})");
                    }
                }

                string where = " WHERE " + string.Join(" AND ", filters);
                int total;
                using (SQLiteCommand countCmd = conn.CreateCommand())
                {
                    countCmd.CommandText = "SELECT COUNT(1) FROM alarm" + where + ";";
                    BindCommon(countCmd, start, end, types);
                    total = Convert.ToInt32(countCmd.ExecuteScalar());
                }

                List<AlarmDO> list = new List<AlarmDO>();
                using (SQLiteCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT id, timestamp, device_id, device_name, preset_id, preset_name, channel_id, channel_name, content, alarm_type, alarm_region, alarm_level, alarm_time, arrival_time, alarm_desc, alarm_source, alarm_value, image_info, is_confirmed, confirmed_time, confirmed_user, is_exported, note FROM alarm"
                        + where + " ORDER BY alarm_time DESC";
                    if (pageSize > 0)
                    {
                        cmd.CommandText += " LIMIT @limit OFFSET @offset";
                        cmd.Parameters.AddWithValue("@limit", pageSize);
                        cmd.Parameters.AddWithValue("@offset", (page - 1) * pageSize);
                    }
                    BindCommon(cmd, start, end, types);
                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(ReadAlarm(reader));
                        }
                    }
                }

                return (list, total);
            }
        }

        public static int Upsert(AlarmDO alarm)
        {
            LiteDatabase.EnsureInitialized();
            string now = DateTime.UtcNow.ToString("o");
            using (SQLiteConnection conn = LiteDatabase.OpenConnection())
            {
                if (alarm.ID > 0 && Exists(conn, alarm.ID))
                {
                    using (SQLiteCommand cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = @"
UPDATE alarm SET
  timestamp=@timestamp, device_id=@deviceId, device_name=@deviceName, preset_id=@presetId, preset_name=@presetName,
  channel_id=@channelId, channel_name=@channelName, content=@content, alarm_type=@alarmType, alarm_region=@alarmRegion,
  alarm_level=@alarmLevel, alarm_time=@alarmTime, arrival_time=@arrivalTime, alarm_desc=@alarmDesc,
  alarm_source=@alarmSource, alarm_value=@alarmValue, image_info=@imageInfo, is_confirmed=@isConfirmed,
  confirmed_time=@confirmedTime, confirmed_user=@confirmedUser, is_exported=@isExported, note=@note, update_time=@now
WHERE id=@id;";
                        BindAlarm(cmd, alarm, now);
                        cmd.Parameters.AddWithValue("@id", alarm.ID);
                        cmd.ExecuteNonQuery();
                        return alarm.ID;
                    }
                }

                using (SQLiteCommand insert = conn.CreateCommand())
                {
                    insert.CommandText = @"
INSERT INTO alarm (timestamp, device_id, device_name, preset_id, preset_name, channel_id, channel_name, content,
  alarm_type, alarm_region, alarm_level, alarm_time, arrival_time, alarm_desc, alarm_source, alarm_value, image_info,
  is_confirmed, confirmed_time, confirmed_user, is_exported, note, insert_time, update_time)
VALUES (@timestamp, @deviceId, @deviceName, @presetId, @presetName, @channelId, @channelName, @content,
  @alarmType, @alarmRegion, @alarmLevel, @alarmTime, @arrivalTime, @alarmDesc, @alarmSource, @alarmValue, @imageInfo,
  @isConfirmed, @confirmedTime, @confirmedUser, @isExported, @note, @now, @now);
SELECT last_insert_rowid();";
                    BindAlarm(insert, alarm, now);
                    object idObj = insert.ExecuteScalar();
                    alarm.ID = Convert.ToInt32(idObj);
                    return alarm.ID;
                }
            }
        }

        public static bool Confirm(AlarmConfirmerDataObject confirmer)
        {
            LiteDatabase.EnsureInitialized();
            string now = DateTime.UtcNow.ToString("o");
            using (SQLiteConnection conn = LiteDatabase.OpenConnection())
            using (SQLiteCommand cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
UPDATE alarm SET is_confirmed=@isConfirmed, confirmed_user=@user, confirmed_time=@now, note=@note, update_time=@now
WHERE id=@id;";
                cmd.Parameters.AddWithValue("@isConfirmed", confirmer.IsConfirmed ? 1 : 0);
                cmd.Parameters.AddWithValue("@user", (object)confirmer.ConfirmedUser ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@note", (object)confirmer.Note ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@now", now);
                cmd.Parameters.AddWithValue("@id", confirmer.ID);
                return cmd.ExecuteNonQuery() > 0;
            }
        }

        private static bool Exists(SQLiteConnection conn, int id)
        {
            using (SQLiteCommand cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT 1 FROM alarm WHERE id=@id LIMIT 1;";
                cmd.Parameters.AddWithValue("@id", id);
                return cmd.ExecuteScalar() != null;
            }
        }

        private static void BindCommon(SQLiteCommand cmd, DateTime start, DateTime end, List<string> types)
        {
            cmd.Parameters.AddWithValue("@start", start.ToString("o"));
            cmd.Parameters.AddWithValue("@end", end.ToString("o"));
            if (types != null)
            {
                for (int i = 0; i < types.Count; i++)
                {
                    cmd.Parameters.AddWithValue("@t" + i, types[i]);
                }
            }
        }

        private static void BindAlarm(SQLiteCommand cmd, AlarmDO alarm, string now)
        {
            cmd.Parameters.AddWithValue("@timestamp", alarm.Timestamp);
            cmd.Parameters.AddWithValue("@deviceId", (object)alarm.DeviceID ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@deviceName", alarm.DeviceName ?? string.Empty);
            cmd.Parameters.AddWithValue("@presetId", alarm.PresetID);
            cmd.Parameters.AddWithValue("@presetName", alarm.PresetName ?? string.Empty);
            cmd.Parameters.AddWithValue("@channelId", alarm.ChannelID);
            cmd.Parameters.AddWithValue("@channelName", alarm.ChannelName ?? string.Empty);
            cmd.Parameters.AddWithValue("@content", DBNull.Value);
            cmd.Parameters.AddWithValue("@alarmType", (object)alarm.AlarmType ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@alarmRegion", (object)alarm.AlarmRegion ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@alarmLevel", (object)alarm.AlarmLevel ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@alarmTime", alarm.AlarmTime.ToUniversalTime().ToString("o"));
            cmd.Parameters.AddWithValue("@arrivalTime", alarm.ArrivalTime.ToUniversalTime().ToString("o"));
            cmd.Parameters.AddWithValue("@alarmDesc", (object)alarm.AlarmDescription ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@alarmSource", DBNull.Value);
            cmd.Parameters.AddWithValue("@alarmValue", DBNull.Value);
            cmd.Parameters.AddWithValue("@imageInfo", alarm.ImageInfo == null ? (object)DBNull.Value : JsonConvert.SerializeObject(alarm.ImageInfo));
            cmd.Parameters.AddWithValue("@isConfirmed", alarm.IsConfirmed ? 1 : 0);
            cmd.Parameters.AddWithValue("@confirmedTime", alarm.ConfirmedTime.HasValue ? (object)alarm.ConfirmedTime.Value.ToUniversalTime().ToString("o") : DBNull.Value);
            cmd.Parameters.AddWithValue("@confirmedUser", (object)alarm.ConfirmedUser ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@isExported", alarm.IsExported ? 1 : 0);
            cmd.Parameters.AddWithValue("@note", (object)alarm.Note ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@now", now);
        }

        private static AlarmDO ReadAlarm(SQLiteDataReader reader)
        {
            AlarmDO alarm = new AlarmDO
            {
                ID = reader.GetInt32(0),
                Timestamp = reader.GetInt64(1),
                DeviceID = reader.IsDBNull(2) ? null : reader.GetString(2),
                DeviceName = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                PresetID = reader.GetInt32(4),
                PresetName = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
                ChannelID = reader.GetInt32(6),
                ChannelName = reader.IsDBNull(7) ? string.Empty : reader.GetString(7),
                AlarmType = reader.IsDBNull(9) ? null : reader.GetString(9),
                AlarmRegion = reader.IsDBNull(10) ? null : reader.GetString(10),
                AlarmLevel = reader.IsDBNull(11) ? null : reader.GetString(11),
                AlarmDescription = reader.IsDBNull(14) ? null : reader.GetString(14),
                IsConfirmed = reader.GetInt32(18) == 1,
                ConfirmedUser = reader.IsDBNull(20) ? null : reader.GetString(20),
                IsExported = reader.GetInt32(21) == 1,
                Note = reader.IsDBNull(22) ? null : reader.GetString(22),
            };
            if (!reader.IsDBNull(12) && DateTime.TryParse(reader.GetString(12), null, DateTimeStyles.RoundtripKind, out DateTime alarmTime))
            {
                alarm.AlarmTime = DateTime.SpecifyKind(alarmTime, DateTimeKind.Utc);
            }
            if (!reader.IsDBNull(13) && DateTime.TryParse(reader.GetString(13), null, DateTimeStyles.RoundtripKind, out DateTime arrival))
            {
                alarm.ArrivalTime = DateTime.SpecifyKind(arrival, DateTimeKind.Utc);
            }
            if (!reader.IsDBNull(19) && DateTime.TryParse(reader.GetString(19), null, DateTimeStyles.RoundtripKind, out DateTime confirmed))
            {
                alarm.ConfirmedTime = DateTime.SpecifyKind(confirmed, DateTimeKind.Utc);
            }
            if (!reader.IsDBNull(17))
            {
                alarm.ImageInfo = JsonConvert.DeserializeObject<List<AlarmImageInfoDataObject>>(reader.GetString(17));
                ResolveAlarmImageUrls(alarm.ImageInfo);
            }
            return alarm;
        }

        /// <summary>
        /// Older in-proc alarms stored relative paths under alarm\; UI expects file:/// or absolute.
        /// </summary>
        private static void ResolveAlarmImageUrls(List<AlarmImageInfoDataObject> images)
        {
            if (images == null || images.Count == 0)
            {
                return;
            }

            string defaultRoot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "alarm");
            foreach (AlarmImageInfoDataObject image in images)
            {
                if (image == null || string.IsNullOrWhiteSpace(image.Url))
                {
                    continue;
                }

                string url = image.Url.Trim();
                if (url.StartsWith("file:", StringComparison.OrdinalIgnoreCase)
                    || url.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                    || url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string absolute = LocalImagePath.ResolveAbsolute(Path.IsPathRooted(url) ? null : defaultRoot, url);
                if (string.IsNullOrWhiteSpace(absolute))
                {
                    continue;
                }

                image.Url = LocalImagePath.ToFileUri(absolute) ?? absolute;
            }
        }
    }
}
