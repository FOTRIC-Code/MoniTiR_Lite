// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Data.SQLite;
using System.IO;

namespace Firefly.Client.Service.LiteDb
{
    /// <summary>
    /// SQLite access for Store tables on shared lite.db ({exe directory}\lite.db).
    /// Path must match <see cref="LiteDbPaths"/>.
    /// </summary>
    internal static class StoreSqlite
    {
        public const string DatabaseFileName = "lite.db";

        public static string DatabasePath
        {
            get
            {
                string root = AppDomain.CurrentDomain.BaseDirectory;
                Directory.CreateDirectory(root);
                return Path.Combine(root, DatabaseFileName);
            }
        }

        public static string ConnectionString =>
            new SQLiteConnectionStringBuilder
            {
                DataSource = DatabasePath,
                Version = 3,
                JournalMode = SQLiteJournalModeEnum.Wal,
                SyncMode = SynchronizationModes.Normal,
                BusyTimeout = 5000,
                ForeignKeys = true,
            }.ToString();

        public static void EnsureNvrSchema()
        {
            using (SQLiteConnection conn = OpenConnection())
            using (SQLiteCommand cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
PRAGMA journal_mode=WAL;
PRAGMA foreign_keys=ON;
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
        }

        public static SQLiteConnection OpenConnection()
        {
            SQLiteConnection conn = new SQLiteConnection(ConnectionString);
            conn.Open();
            return conn;
        }
    }
}
