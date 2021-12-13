// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.IO;
using System.Drawing;

namespace Firefly.SDK.File
{
    public class UNITheramlImage
    {
        #region filed

        private const int FILE_END_TYPE_LENGTH = 16;
        private const int OFFSET_LENGTH = 4;
        private const int SHOOTING_TIME_LENGTH = 14;
        private const int PRODUCTOR_LENGTH = 32;
        private const int TYPE_LENGTH = 32;
        private const int SERIAL_NO_LENGTH = 32;

        #endregion

        #region property

        private byte[] _imageBytes;
        private byte[] _fileEndType;
        private float[,] _tempData;

        public ushort Version { get; private set; }
        public ushort Width { get; private set; }
        public ushort Height { get; private set; }
        public string ShootingTime { get; private set; }
        public float Emissivity { get; private set; }
        public float AmbientTemperature { get; private set; }
        public char Lens { get; private set; }
        public uint Distance { get; private set; }
        public char RelativeHumidity { get; private set; }
        public float ReflectiveTemp { get; private set; }
        public string Company { get; private set; }
        public string ProductModel { get; private set; }
        public string ProductSerNO { get; private set; }
        public double Longitude { get; private set; }
        public double Latitude { get; private set; }
        public int Elevation { get; private set; }
        public string Description { get; private set; }

        #endregion

        #region method

        public bool Open(string fileName)
        {
            try
            {
                using (FileStream fs = new FileStream(fileName, FileMode.Open))
                {
                    using (BinaryReader br = new BinaryReader(fs))
                    {
                        long fileEndTypeIndex = fs.Length - FILE_END_TYPE_LENGTH;
                        br.BaseStream.Seek(fileEndTypeIndex, SeekOrigin.Begin);
                        _fileEndType = br.ReadBytes(FILE_END_TYPE_LENGTH);
                        br.BaseStream.Seek(fs.Length - FILE_END_TYPE_LENGTH - OFFSET_LENGTH, SeekOrigin.Begin);
                        uint irDataOffset = br.ReadUInt32();
                        br.BaseStream.Seek(0, SeekOrigin.Begin);
                        _imageBytes = br.ReadBytes((int)irDataOffset);
                        Version = br.ReadUInt16();
                        Width = br.ReadUInt16();
                        Height = br.ReadUInt16();
                        ShootingTime = System.Text.Encoding.Default.GetString(br.ReadBytes(SHOOTING_TIME_LENGTH));
                        _tempData = new float[Width, Height];
                        for (int h = 0; h < Height; h++)
                        {
                            for (int w = 0; w < Width; w++)
                            {
                                _tempData[w, h] = br.ReadSingle();
                            }
                        }
                        Emissivity = br.ReadSingle();
                        AmbientTemperature = br.ReadSingle();
                        Lens = br.ReadChar();
                        Distance = br.ReadUInt32();
                        RelativeHumidity = br.ReadChar();
                        ReflectiveTemp = br.ReadSingle();
                        Company = new string(br.ReadChars(PRODUCTOR_LENGTH));
                        ProductModel = new string(br.ReadChars(TYPE_LENGTH));
                        ProductSerNO = new string(br.ReadChars(SERIAL_NO_LENGTH));
                        Longitude = br.ReadDouble();
                        Latitude = br.ReadDouble();
                        Elevation = br.ReadInt32();
                        uint descriptionLength = br.ReadUInt32();
                        Description = new string(br.ReadChars((int)descriptionLength));
                    }
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        public Image GetImage()
        {
            using (MemoryStream ms = new MemoryStream(_imageBytes))
            {
                return Image.FromStream(ms);
            }
        }

        public float GetTemp(Point point)
        {
            float temp = 0.0f;
            
            try
            {
                temp= _tempData[point.X, point.Y];
            }
            catch
            {
            }
            return temp;
        }

        #endregion
    }
}