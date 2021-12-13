// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Data.SQLite;
using System.IO;
using Newtonsoft.Json;

namespace Firefly.Client.Service.LiteDb
{
    public static class LiteSystemConfigRepository
    {
        public const string KeyI18n = "i18n";
        public const string KeyCommon = "common";
        public const string KeyLivePreview = "livePreviewPollingGroup";
        public const string KeyCustomSoftware = "customSoftware";
        public const string KeyAdminConfig = "adminConfig";

        public static T Get<T>(string key, T defaultValue = default(T))
        {
            LiteDatabase.EnsureInitialized();
            string json = GetRaw(key);
            if (string.IsNullOrWhiteSpace(json))
            {
                return defaultValue;
            }
            return JsonConvert.DeserializeObject<T>(json);
        }

        public static string GetRaw(string key)
        {
            LiteDatabase.EnsureInitialized();
            using (SQLiteConnection conn = LiteDatabase.OpenConnection())
            using (SQLiteCommand cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT value FROM system_config WHERE key=@key;";
                cmd.Parameters.AddWithValue("@key", key);
                object value = cmd.ExecuteScalar();
                return value == null || value == DBNull.Value ? null : Convert.ToString(value);
            }
        }

        public static void SetObject(string key, object value)
        {
            SetRaw(key, JsonConvert.SerializeObject(value));
        }

        public static void SetRaw(string key, string value)
        {
            LiteDatabase.EnsureInitialized();
            string now = DateTime.UtcNow.ToString("o");
            using (SQLiteConnection conn = LiteDatabase.OpenConnection())
            using (SQLiteCommand cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
INSERT INTO system_config (key, value, update_time) VALUES (@key, @value, @now)
ON CONFLICT(key) DO UPDATE SET value=excluded.value, update_time=excluded.update_time;";
                cmd.Parameters.AddWithValue("@key", key);
                cmd.Parameters.AddWithValue("@value", value ?? string.Empty);
                cmd.Parameters.AddWithValue("@now", now);
                cmd.ExecuteNonQuery();
            }
        }

        public static string SaveIconFile(string sourceFilePath)
        {
            string ext = Path.GetExtension(sourceFilePath);
            if (string.IsNullOrWhiteSpace(ext))
            {
                ext = ".png";
            }
            string fileName = Guid.NewGuid().ToString("N") + ext;
            string dest = Path.Combine(LiteDbPaths.UploadsDirectory, fileName);
            File.Copy(sourceFilePath, dest, true);
            return dest;
        }
    }
}
