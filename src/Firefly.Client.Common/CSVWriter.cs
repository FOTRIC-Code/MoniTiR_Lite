// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System.IO;
using System.Text;
using Microsoft.Win32;
using System.Collections.Generic;
using System;

namespace Firefly.Client.Common
{
    public static class CSVWriter
    {
        private static object obj = new object();

        /// <summary>
        /// Writes http request data to csv file, to check request whether healthy
        /// </summary>
        /// <param name="model"></param>
        public static bool? OuputData(List<string> model)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "(CSV*.csv)|*.csv"
            };
            saveFileDialog.InitialDirectory = AppDomain.CurrentDomain.BaseDirectory;
            try
            {
                if (saveFileDialog.ShowDialog() == true)
                {
                    OutputDataToCsv(saveFileDialog.FileName, model);
                    return true;
                }
                return null;
            }
            catch
            {
                return false;
            }
        }

        public static bool? OuputData(string filePath, List<string> model)
        {
            try
            {
                OutputDataToCsv(filePath, model);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static string FormatString(string str)
        {
            return $"\"{str.Replace("\"", "\"\"")}\"";
        }

        private static void OutputDataToCsv(string filePath, List<string> model)
        {
            lock (obj)
            {
                StringBuilder stringBuilder = new StringBuilder();

                model?.ForEach(it => { stringBuilder.AppendLine(it.ToString()); });
                File.WriteAllText(filePath, stringBuilder.ToString());
            }
        }
    }
}