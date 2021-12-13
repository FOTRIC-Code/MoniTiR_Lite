// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Data.SQLite;
using System.IO;
using log4net;

namespace Firefly.Client.Service.LiteDb
{
    /// <summary>
    /// Opens shared lite.db with WAL. Client hosts in-proc Fire + Store.
    /// </summary>
    public sealed class LiteDatabase
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(LiteDatabase));
        private static readonly object InitLock = new object();
        private static bool _initialized;

        public static string ConnectionString =>
            new SQLiteConnectionStringBuilder
            {
                DataSource = LiteDbPaths.DatabasePath,
                Version = 3,
                JournalMode = SQLiteJournalModeEnum.Wal,
                SyncMode = SynchronizationModes.Normal,
                BusyTimeout = 5000,
                ForeignKeys = true,
            }.ToString();

        public static void EnsureInitialized()
        {
            if (_initialized)
            {
                return;
            }

            lock (InitLock)
            {
                if (_initialized)
                {
                    return;
                }

                Directory.CreateDirectory(LiteDbPaths.RootDirectory);
                using (SQLiteConnection conn = OpenConnection())
                {
                    using (SQLiteCommand cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = @"
PRAGMA journal_mode=WAL;
PRAGMA synchronous=NORMAL;
PRAGMA busy_timeout=5000;
PRAGMA foreign_keys=ON;

CREATE TABLE IF NOT EXISTS device (
    id TEXT PRIMARY KEY NOT NULL,
    name TEXT NOT NULL,
    protocol_type TEXT NULL,
    addr TEXT NULL,
    conf TEXT NULL,
    insert_time TEXT NOT NULL,
    update_time TEXT NOT NULL
);

CREATE INDEX IF NOT EXISTS idx_device_name ON device(name);

CREATE TABLE IF NOT EXISTS alarm (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    timestamp INTEGER NOT NULL DEFAULT 0,
    device_id TEXT NULL,
    device_name TEXT NOT NULL DEFAULT '',
    preset_id INTEGER NOT NULL DEFAULT 0,
    preset_name TEXT NOT NULL DEFAULT '',
    channel_id INTEGER NOT NULL DEFAULT 0,
    channel_name TEXT NOT NULL DEFAULT '',
    content TEXT NULL,
    alarm_type TEXT NULL,
    alarm_region TEXT NULL,
    alarm_level TEXT NULL,
    alarm_time TEXT NOT NULL,
    arrival_time TEXT NOT NULL,
    alarm_desc TEXT NULL,
    alarm_source TEXT NULL,
    alarm_value TEXT NULL,
    image_info TEXT NULL,
    is_confirmed INTEGER NOT NULL DEFAULT 0,
    confirmed_time TEXT NULL,
    confirmed_user TEXT NULL,
    is_exported INTEGER NOT NULL DEFAULT 0,
    note TEXT NULL,
    insert_time TEXT NOT NULL,
    update_time TEXT NOT NULL
);

CREATE INDEX IF NOT EXISTS idx_alarm_time ON alarm(alarm_time);
CREATE INDEX IF NOT EXISTS idx_alarm_device_time ON alarm(device_id, alarm_time);

CREATE TABLE IF NOT EXISTS system_config (
    key TEXT PRIMARY KEY NOT NULL,
    value TEXT NOT NULL,
    update_time TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS fire_data (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    avg_temp REAL NULL,
    device_id TEXT NOT NULL,
    device_name TEXT NULL,
    files TEXT NULL,
    happen_time TEXT NULL,
    image_path TEXT NULL,
    insert_time TEXT NULL,
    marker_name TEXT NULL,
    max_temp REAL NULL,
    min_temp REAL NULL,
    preset_id INTEGER NULL,
    preset_name TEXT NULL,
    root_path TEXT NULL,
    rule_temp REAL NULL,
    timestamp INTEGER NULL,
    update_time TEXT NULL
);
CREATE INDEX IF NOT EXISTS idx_fire_data_device_id ON fire_data(device_id);
CREATE INDEX IF NOT EXISTS idx_fire_data_happen_time ON fire_data(happen_time);

CREATE TABLE IF NOT EXISTS file (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    path TEXT NOT NULL,
    type INTEGER NOT NULL,
    insert_time TEXT NULL,
    root_path TEXT NOT NULL,
    update_time TEXT NULL
);

CREATE TABLE IF NOT EXISTS nvr (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    name TEXT NOT NULL,
    protocol_type TEXT NOT NULL,
    addr TEXT NULL,
    insert_time TEXT NULL,
    update_time TEXT NULL
);
CREATE UNIQUE INDEX IF NOT EXISTS idx_nvr_name ON nvr(name);

CREATE TABLE IF NOT EXISTS nvr_channel (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    nvr_id INTEGER NOT NULL,
    channel_id INTEGER NULL,
    channel_info TEXT NULL,
    device_id TEXT NULL,
    insert_time TEXT NULL,
    update_time TEXT NULL,
    FOREIGN KEY(nvr_id) REFERENCES nvr(id)
);
CREATE INDEX IF NOT EXISTS idx_nvr_channel_nvr_id ON nvr_channel(nvr_id);
";
                        cmd.ExecuteNonQuery();
                    }

                    MigrateLegacySchema(conn);
                }

                _initialized = true;
                Logger.Info($"Lite SQLite ready at {LiteDbPaths.DatabasePath}");
            }
        }

        public static SQLiteConnection OpenConnection()
        {
            SQLiteConnection conn = new SQLiteConnection(ConnectionString);
            conn.Open();
            return conn;
        }

        private static void MigrateLegacySchema(SQLiteConnection conn)
        {
            // Drop obsolete agent table if present.
            using (SQLiteCommand dropAgent = conn.CreateCommand())
            {
                dropAgent.CommandText = "DROP TABLE IF EXISTS agent;";
                dropAgent.ExecuteNonQuery();
            }

            bool hasAgentIds = ColumnExists(conn, "device", "agent_ids");
            bool hasConf = ColumnExists(conn, "device", "conf");
            if (!hasAgentIds && hasConf)
            {
                return;
            }

            // Rebuild device when migrating off agent_ids or adding conf on old layouts.
            using (SQLiteTransaction tx = conn.BeginTransaction())
            {
                using (SQLiteCommand cmd = conn.CreateCommand())
                {
                    cmd.Transaction = tx;
                    cmd.CommandText = @"
DROP TABLE IF EXISTS device_new;
CREATE TABLE device_new (
    id TEXT PRIMARY KEY NOT NULL,
    name TEXT NOT NULL,
    protocol_type TEXT NULL,
    addr TEXT NULL,
    conf TEXT NULL,
    insert_time TEXT NOT NULL,
    update_time TEXT NOT NULL
);

INSERT INTO device_new (id, name, protocol_type, addr, conf, insert_time, update_time)
SELECT
    id,
    name,
    protocol_type,
    addr,
    " + (hasConf ? "conf" : "NULL") + @",
    insert_time,
    update_time
FROM device;

DROP TABLE device;
ALTER TABLE device_new RENAME TO device;
CREATE INDEX IF NOT EXISTS idx_device_name ON device(name);
";
                    try
                    {
                        cmd.ExecuteNonQuery();
                        tx.Commit();
                    }
                    catch (Exception ex)
                    {
                        tx.Rollback();
                        Logger.Warn("device schema migrate skipped/failed: " + ex.Message);
                    }
                }
            }
        }

        private static bool ColumnExists(SQLiteConnection conn, string table, string column)
        {
            using (SQLiteCommand cmd = conn.CreateCommand())
            {
                cmd.CommandText = $"PRAGMA table_info({table});";
                using (SQLiteDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string name = reader.GetString(1);
                        if (string.Equals(name, column, StringComparison.OrdinalIgnoreCase))
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }
    }
}
