// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System.IO;

namespace Firefly.Client.Service
{
    public class FileDataObject
    {
        private string _fileName;
        private byte[] _fileByte;

        public FileDataObject(string fileName, byte[] fileByte)
        {
            _fileName = fileName;
            _fileByte = fileByte;
        }

        public string FileName => _fileName;

        public bool HasFile => _fileByte?.Length > 0;

        public bool Save(string fileName)
        {
            try
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    ms.Write(_fileByte, 0, _fileByte.Length);
                    FileStream fs = new FileStream(fileName, FileMode.Create);
                    BinaryWriter bw = new BinaryWriter(fs);
                    bw.Write(ms.ToArray());
                    bw.Close();
                    ms.Close();
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        public string BuildFilterStr()
        {
            try
            {
                string extension = Path.GetExtension(_fileName);
                return $"({extension.TrimStart('.')}*{extension})|*{extension}";
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}