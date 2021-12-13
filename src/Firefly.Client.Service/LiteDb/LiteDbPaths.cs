// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.IO;

namespace Firefly.Client.Service.LiteDb
{
    /// <summary>
    /// Shared Lite SQLite database used by Client now; Fire/Store will attach to the same file later.
    /// Default path: {exe directory}\lite.db
    /// </summary>
    public static class LiteDbPaths
    {
        public const string DatabaseFileName = "lite.db";
        public const string UploadsFolderName = "Uploads";

        public static string RootDirectory
        {
            get
            {
                string root = AppDomain.CurrentDomain.BaseDirectory;
                Directory.CreateDirectory(root);
                return root;
            }
        }

        public static string DatabasePath => Path.Combine(RootDirectory, DatabaseFileName);

        public static string UploadsDirectory
        {
            get
            {
                string dir = Path.Combine(RootDirectory, UploadsFolderName);
                Directory.CreateDirectory(dir);
                return dir;
            }
        }
    }

    /// <summary>
    /// Fixed logical node ids for in-memory local Fire/Store (no agent table).
    /// </summary>
    public static class LiteLocalAgentIds
    {
        public const int Fire = 1;
        public const int Store = 2;
    }
}
