using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;

namespace Firefly.SDK.File
{
    public enum FileType
    {
        Other = 0,
        ThermalPicture = 1,
        ThermalVideo = 2,
        VisualPicture = 3,
        GridThermalPicture = 4,
    }

    public enum AudioType
    {
        Wav = 1,
        MP3 = 2
    }

    public enum MarkerType
    {
        Spot = 1,
        Rectangle = 3,
        Ellipse = 4,
        Polygon = 5,
        Polyline = 6,
    }

    public enum ValueType
    {
        Max = 1,
        Min = 2,
        Avg = 3,
    }

    public enum RotateType
    {
        Rotate90 = 1,
        Rotate180 = 2,
        Rotate270 = 3,
    }

    public enum MirrorType
    {
        MirrorHorizontal = 1,
        MirrorVertical = 2,
    }

    public enum GainMode
    {
        Fixed = 1,
        Auto = 2,
        Touch = 3,
    }

    public enum AlarmType
    {
        Normal = 0,
        Below = 1,
        Above = 2,
        Between = 3,
        MagicThermal = 4,
        Outside = 5,
        Humidity = 6,
        Insulation = 7,
    }

    public enum ViewType
    {
        Thermal = 1,
        PicInPic = 2,
        Blend = 3,
        Fusion = 4,
    }

    public enum ColorDistributionType
    {
        HistEqual = 1,
        TempLinear = 2,
        ADLinear = 3,
    }

    public struct CameraInfo
    {
        public readonly static CameraInfo DEFAULT = new CameraInfo(string.Empty, string.Empty, string.Empty, string.Empty, null);

        public CameraInfo(string model, string serialNo, string brand, string company, Bitmap logo)
        {
            Model = model;
            SerialNo = serialNo;
            Brand = brand;
            Company = company;
            Logo = logo;
        }

        public string Model { get; set; }
        public string SerialNo { get; set; }
        public string Brand { get; set; }
        public string Company { get; set; }
        public Bitmap Logo { get; set; }
    }

    public struct LensInfo
    {
        public readonly static LensInfo DEFAULT = new LensInfo(string.Empty, 0.0F, 0.0F);

        public LensInfo(string name, float focalLength, float hFov)
        {
            Name = name;
            FocalLength = focalLength;
            HFov = hFov;
        }

        public string Name { get; set; }
        public float FocalLength { get; set; }
        public float HFov { get; set; }
    }

    public struct TempRange
    {
        public readonly static TempRange DEFAULT = new TempRange(0.0F, 0.0F);

        public TempRange(float max, float min)
        {
            Max = max;
            Min = min;
        }

        public float Max { get; set; }
        public float Min { get; set; }
    }

    public struct GeoLocation
    {
        public readonly static GeoLocation DEFAULT = new GeoLocation(false, 0.0F, 0.0F, 0.0F, false, 0);

        public GeoLocation(bool isGPSValid, float longitude, float latitude, float altitude, bool isCompassValid, short direction)
        {
            IsGPSValid = isGPSValid;
            Longitude = longitude;
            Latitude = latitude;
            Altitude = altitude;
            IsCompassValid = isCompassValid;
            Direction = direction;
        }

        public bool IsGPSValid { get; set; }
        public double Longitude { get; set; }
        public double Latitude { get; set; }
        public double Altitude { get; set; }
        public bool IsCompassValid { get; set; }
        public short Direction { get; set; }
    }

    public struct ThermalParams
    {
        public readonly static ThermalParams DEFAULT = new ThermalParams(293.15F, 293.15F, 1.0F, 0.5F, 1.0F, 293.15F, 1.0F);

        public ThermalParams(float reflTemp, float atmTemp, float distance, float relHumidity, float emissivity, float extOptTemp, float extOptTrans)
        {
            ReflTemp = reflTemp;
            AtmTemp = atmTemp;
            Distance = distance;
            RelHumidity = relHumidity;
            Emissivity = emissivity;
            ExtOptTemp = extOptTemp;
            ExtOptTrans = extOptTrans;
        }

        public float ReflTemp { get; set; }
        public float AtmTemp { get; set; }
        public float Distance { get; set; }
        public float RelHumidity { get; set; }
        public float Emissivity { get; set; }
        public float ExtOptTemp { get; set; }
        public float ExtOptTrans { get; set; }
    }

    public class TimerFileInfo
    {
        public TimerFileInfo(string guid, int no)
        {
            GUID = guid;
            No = no;
        }

        public string GUID { get; private set; }
        public int No { get; private set; }
    }

    public struct LocalThermalParams
    {
        public bool LocalParams { get; set; }
        public float Emissivity { get; set; }
        public float Distance { get; set; }
        public float ReflTemp { get; set; }
    }

    public struct AudioAnnotation
    {
        public AudioAnnotation(bool isValid, AudioType type, byte[] data)
        {
            IsValid = isValid;
            Type = type;
            Data = data;
        }

        public bool IsValid { get; private set; }
        public AudioType Type { get; private set; }
        public byte[] Data { get; private set; }
    }

    public struct DeltaParams
    {
        public DeltaParams(string srcName, ValueType srcValueType, float srcTemp, string destName, ValueType destValueType, float destTemp)
        {
            SrcName = srcName;
            SrcValueType = srcValueType;
            SrcTemp = srcTemp;
            DestName = destName;
            DestValueType = destValueType;
            DestTemp = destTemp;
        }

        public string SrcName { get; set; }
        public ValueType SrcValueType { get; set; }
        public float SrcTemp { get; set; }
        public string DestName { get; set; }
        public ValueType DestValueType { get; set; }
        public float DestTemp { get; set; }
    }

    public struct Reference
    {
        public readonly static Reference DEFAULT = new Reference(false, string.Empty, ValueType.Max, 293.15F);

        public Reference(bool enabled, string name, ValueType valueType, float temp)
        {
            Enabled = enabled;
            Name = name;
            ValueType = valueType;
            Temp = temp;
        }

        public bool Enabled { get; set; }
        public string Name { get; set; }
        public ValueType ValueType { get; set; }
        public float Temp { get; set; }
    }

    public struct Gain
    {
        public readonly static Gain DEFAULT = new Gain(GainMode.Auto, 293.15F, 273.15F, false, 2.0F);

        public Gain(GainMode mode, float max, float min, bool fixedSpan, float span)
        {
            Mode = mode;
            Max = max;
            Min = min;
            FixedSpan = fixedSpan;
            Span = span;
        }

        public GainMode Mode { get; set; }
        public float Max { get; set; }
        public float Min { get; set; }
        public bool FixedSpan { get; set; }
        public float Span { get; set; }
    }

    public struct Alarm
    {
        public readonly static Alarm DEFAULT = new Alarm(AlarmType.Normal, false, Color.Black, 313.15F, 293.15F, 1.0F, 293.15F, 283.15F, 0.8F);

        public Alarm(AlarmType type, bool custom, Color color, float max, float min, float rhLimit, float indoorTemp, float outdoorTemp, float heatIndex)
        {
            Type = type;
            Custom = custom;
            Color = color;
            Max = max;
            Min = min;
            RHLimit = rhLimit;
            IndoorTemp = indoorTemp;
            OutdoorTemp = outdoorTemp;
            HeatIndex = heatIndex;
        }

        public AlarmType Type { get; set; }
        public bool Custom { get; set; }
        public Color Color { get; set; }
        public float Max { get; set; }
        public float Min { get; set; }
        public float RHLimit { get; set; }
        public float IndoorTemp { get; set; }
        public float OutdoorTemp { get; set; }
        public float HeatIndex { get; set; }
    }

    public struct Palette
    {
        public readonly static Palette DEFAULT = new Palette(0x01, null, false);

        public Palette(byte refNo, uint[] colors, bool inverted)
        {
            RefNo = refNo;
            Colors = colors;
            Inverted = inverted;
        }

        public byte RefNo { get; set; }
        public uint[] Colors { get; set; }
        public bool Inverted { get; set; }
    }

    public struct PaletteV2
    {
        public readonly static PaletteV2 DEFAULT = new PaletteV2(0x01, null, null, false);

        public PaletteV2(byte refNo, uint[] srcColors, uint[] destColors, bool inverted)
        {
            RefNo = refNo;
            SrcColors = srcColors;
            DestColors = destColors;
            Inverted = inverted;
        }

        public byte RefNo { get; set; }
        public uint[] SrcColors { get; set; }
        public uint[] DestColors { get; set; }
        public bool Inverted { get; set; }
    }

    public struct PresetPalette
    {
        public byte RefNo { get; set; }
        public string Name { get; set; }
    }

    public struct LUT
    {
        public readonly static LUT DEFAULT = new LUT(0, new ushort[] { 0, 65535 }, new float[] { 253.15F, 423.15F });

        public LUT(byte radMethod, ushort[] adArray, float[] tempArray)
        {
            RadMethod = radMethod;
            ADArray = adArray;
            TempArray = tempArray;
        }

        public byte RadMethod { get; set; }
        public ushort[] ADArray { get; set; }
        public float[] TempArray { get; set; }
    }

    public struct ViewInfo
    {
        public readonly static ViewInfo DEFAULT = new ViewInfo(ViewType.Thermal, 313.15F, 293.15F, 0, new RectangleF(0.25F, 0.25F, 0.5F, 0.5F));

        public ViewInfo(ViewType type, float max, float min, byte alpha, RectangleF area)
        {
            Type = type;
            Max = max;
            Min = min;
            Alpha = alpha;
            Area = area;
        }

        public ViewType Type { get; set; }
        public float Max { get; set; }
        public float Min { get; set; }
        public byte Alpha { get; set; }
        public RectangleF Area { get; set; }
    }

    public struct MappingInfo
    {
        public readonly static MappingInfo DEFAULT = new MappingInfo(new Size(800, 600), new RectangleF(0.25F, 0.25F, 0.5F, 0.5F));

        public MappingInfo(Size visualSize, RectangleF mappingArea)
        {
            VisualSize = visualSize;
            MappingArea = mappingArea;
        }

        public Size VisualSize { get; set; }
        public RectangleF MappingArea { get; set; }
    }

    public struct ZoomInfo
    {
        public readonly static ZoomInfo DEFAULT = new ZoomInfo(false, new PointF(0.0F, 0.0F), 1.0F);

        public ZoomInfo(bool enabled, PointF location, float ratio)
        {
            Enabled = enabled;
            Location = location;
            Ratio = ratio;
        }

        public bool Enabled { get; set; }
        public PointF Location { get; set; }
        public float Ratio { get; set; }
    }

    public struct Frame
    {
        public readonly static Frame DEFAULT = new Frame(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc), new ushort[0]);

        public Frame(DateTime timeStamp, ushort[] data)
        {
            TimeStamp = timeStamp;
            Data = data;
        }

        public DateTime TimeStamp { get; set; }
        public ushort[] Data { get; set; }
    }

    public abstract class ImageBase
    {
        protected const byte TRUE = 0x01;
        protected const byte FALSE = 0x00;

        public ImageBase()
        {
            HImage = IntPtr.Zero;
            IsModified = false;
        }

        public static string Version
        {
            get
            {
                uint version = 0;
                IRtekFileSDK.ir_file_buffer strVersion = new IRtekFileSDK.ir_file_buffer();
                int result = IRtekFileSDK.ir_file_get_sdk_version(ref version, ref strVersion);
                if (result == IRtekFileSDK.IR_FILE_EC_OK)
                {
                    try
                    {
                        strVersion.buffer = Marshal.AllocHGlobal((int)strVersion.length);
                        result = IRtekFileSDK.ir_file_get_sdk_version(ref version, ref strVersion);
                        if (result == IRtekFileSDK.IR_FILE_EC_OK)
                        {
                            byte[] buffer = new byte[strVersion.length];
                            Marshal.Copy(strVersion.buffer, buffer, 0, (int)strVersion.length);
                            return StringConvertHelper.BytesToString(buffer, Encoding.UTF8);
                        }
                        else
                        {
                            throw new Exception(string.Format("invoke ir_file_get_sdk_version error code {0}.", result));
                        }
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(strVersion.buffer);
                    }
                }
                else
                {
                    throw new Exception(string.Format("invoke ir_file_get_sdk_version error code {0}.", result));
                }
            }
        }

        protected IntPtr HImage { get; set; }

        public bool IsModified { get; protected set; }

        public bool IsValid
        {
            get { return HImage != IntPtr.Zero; }
        }

        public string FileName
        {
            get
            {
                if (HImage == IntPtr.Zero)
                {
                    return string.Empty;
                }

                IRtekFileSDK.ir_file_buffer irBuffer = new IRtekFileSDK.ir_file_buffer();
                int result = IRtekFileSDK.ir_file_get_file_name(HImage, ref irBuffer);
                if (result == IRtekFileSDK.IR_FILE_EC_OK)
                {
                    try
                    {
                        irBuffer.buffer = Marshal.AllocHGlobal((int)irBuffer.length);
                        result = IRtekFileSDK.ir_file_get_file_name(HImage, ref irBuffer);
                        if (result == IRtekFileSDK.IR_FILE_EC_OK)
                        {
                            byte[] buffer = new byte[irBuffer.length];
                            Marshal.Copy(irBuffer.buffer, buffer, 0, (int)irBuffer.length);
                            return StringConvertHelper.BytesToString(buffer, Encoding.Default);
                        }
                        else
                        {
                            throw new Exception(string.Format("invoke ir_file_get_file_name error code {0}.", result));
                        }
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(irBuffer.buffer);
                    }
                }
                else
                {
                    throw new Exception(string.Format("invoke ir_file_get_file_name error code {0}.", result));
                }
            }
        }

        public FileType FileType
        {
            get
            {
                if (HImage == IntPtr.Zero)
                {
                    return FileType.Other;
                }

                int irFileType = 0;
                int result = IRtekFileSDK.ir_file_get_file_type(HImage, ref irFileType);
                if (result == IRtekFileSDK.IR_FILE_EC_OK)
                {
                    switch (irFileType)
                    {
                        case IRtekFileSDK.IR_FILE_THERMAL_PICTURE:
                            return FileType.ThermalPicture;
                        case IRtekFileSDK.IR_FILE_THERMAL_VIDEO:
                            return FileType.ThermalVideo;
                        case IRtekFileSDK.IR_FILE_VISUAL_PICTURE:
                            return FileType.VisualPicture;
                        case IRtekFileSDK.IR_FILE_GRID_THERMAL_PICTURE:
                            return FileType.GridThermalPicture;
                        default:
                            return FileType.Other;
                    }
                }
                else
                {
                    throw new Exception(string.Format("invoke ir_file_get_file_type error code {0}.", result));
                }
            }
        }

        public ushort FileVersion
        {
            get
            {
                if (HImage == IntPtr.Zero)
                {
                    return 0;
                }

                ushort irFileVersion = 0;
                int result = IRtekFileSDK.ir_file_get_file_version(HImage, ref irFileVersion);
                if (result == IRtekFileSDK.IR_FILE_EC_OK)
                {
                    return irFileVersion;
                }
                else
                {
                    throw new Exception(string.Format("invoke ir_file_get_file_version error code {0}.", result));
                }
            }
        }

        public static FileType PeekFileType(string fileName)
        {
            int irFileType = 0;
            int result = IRtekFileSDK.ir_file_peek_file_type(fileName, ref irFileType);
            if (result == IRtekFileSDK.IR_FILE_EC_OK)
            {
                switch (irFileType)
                {
                    case IRtekFileSDK.IR_FILE_THERMAL_PICTURE:
                        return FileType.ThermalPicture;
                    case IRtekFileSDK.IR_FILE_THERMAL_VIDEO:
                        return FileType.ThermalVideo;
                    case IRtekFileSDK.IR_FILE_VISUAL_PICTURE:
                        return FileType.VisualPicture;
                    case IRtekFileSDK.IR_FILE_GRID_THERMAL_PICTURE:
                        return FileType.GridThermalPicture;
                    default:
                        return FileType.Other;
                }
            }
            else
            {
                return FileType.Other;
            }
        }
    }
}
