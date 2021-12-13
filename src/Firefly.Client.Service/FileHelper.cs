// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System.IO;
using System.Text;
using Microsoft.Win32;

using Newtonsoft.Json;

namespace Firefly.Client.Service
{
    public class FileHelper
    {
        public static string Read(string fileName)
        {
            string result = string.Empty;
            if (!File.Exists(fileName))
            {
                return result;
            }

            using (StreamReader streamReader = new StreamReader(fileName, Encoding.UTF8))
            {
                result = streamReader.ReadToEnd();
            }

            return result;
        }

        public static string Read(string fileName, Encoding encode)
        {
            string result = string.Empty;
            if (!File.Exists(fileName))
            {
                return result;
            }
            using (StreamReader streamReader = new StreamReader(fileName, encode))
            {
                result = streamReader.ReadToEnd();
            }

            return result;
        }

        public static void WriteFile(string fileName, string Strings)
        {
            using (FileStream fileStream = new FileStream(fileName, FileMode.OpenOrCreate))
            {
                using (StreamWriter streamWriter = new StreamWriter(fileName, false, Encoding.UTF8))
                {
                    streamWriter.Write(Strings);
                }
            }
        }

        public static void WriteFile(string fileName, string Strings, Encoding encode)
        {
            using (FileStream fileStream = new FileStream(fileName, FileMode.OpenOrCreate))
            {
                using (StreamWriter streamWriter = new StreamWriter(fileName, false, encode))
                {
                    streamWriter.Write(Strings);
                }
            }
        }

        public static bool ImportDataObjectFromFile<T>(string filter, out T dataObject)
        {
            dataObject = default(T);
            OpenFileDialog openFileDialog = new OpenFileDialog()
            {
                CheckPathExists = true,
                Filter = filter
            };
            if (openFileDialog.ShowDialog() != true)
            {
                return false;
            }   
            string fileName = openFileDialog.FileName;
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return false;
            }
            dataObject = JsonConvert.DeserializeObject<T>(Read(fileName));
            return true;
        }
    }
}
