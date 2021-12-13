// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.IO;

using Newtonsoft.Json;

using Firefly.Client.Common;

namespace Firefly.Client.Control
{
    public static class PreferenceHelper
    {
        public static void Load(this IPreference preference)
        {
            string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Constants.PREFERENCE_FOLDER + preference.FileName);
            if (File.Exists(filePath))
            {
                try
                {
                    preference.LoadJson(File.ReadAllText(filePath));
                }
                catch
                {
                }
            }
            else
            {
                preference.LoadJson(string.Empty);
            }
        }

        public static void Save(this IPreference preference)
        {
            string folderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Constants.PREFERENCE_FOLDER);
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
                DirectoryInfo info = new DirectoryInfo(folderPath);
                info.Attributes = FileAttributes.Hidden;
            }

            string filePath = Path.Combine(folderPath, preference.FileName);
            try
            {
                File.WriteAllText(filePath, preference.GetJson());
            }
            catch
            {
            }
        }
    }
}