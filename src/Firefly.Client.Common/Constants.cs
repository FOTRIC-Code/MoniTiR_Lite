// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;

namespace Firefly.Client.Common
{
    public static class Constants
    {
        public const int PAGE_NO_DEFAULT = 1;
        public const int PAGE_SIZE_INFINITAS = -1;
        public const int PAGE_SIZE_10 = 10;
        public const int PAGE_SIZE_20 = 20;
        public const int PAGE_SIZE_50 = 50;
        public const int PAGE_SIZE_100 = 100;
        public const int PAGE_SIZE_1000 = 1000;

        public static readonly List<int> PAGE_SIZE_LIST = new List<int>
        {
           PAGE_SIZE_10, PAGE_SIZE_20, PAGE_SIZE_50, PAGE_SIZE_100, PAGE_SIZE_1000
        };

        public const int MAX_NAME_LENGTH = 64;

        public const int MAX_PASSWORD_LENGTH = 24;

        public const int MIN_PASSWORD_LENGTH = 6;

        public const string ALLOWED_USERNAME_CHARACTERS = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";

        public const string NAME_REG = @"^[\u4e00-\u9fa5\dA-Za-z-_#]{0,64}$";

        public const string SOFTWARE_NAME_REG = @"^[\u4e00-\u9fa5\dA-Za-z_#\r\n]{0,64}$";

        public const string NOTE_REG = @"^.{0,255}$";

        public const string IP_REG = @"^((25[0-5]|2[0-4]\d|((1\d{2})|([1-9]?\d)))\.){3}(25[0-5]|2[0-4]\d|((1\d{2})|([1-9]?\d)))$";

        public const string NUMBER_REG = @"^[1-9]\d*$";

        public const string FLOAT_REG = @"^((0{1}\.\d{1,2})|([1-9]\d*\.{1}\d{1,2})|([1-9]+\d*))$";

        public const string PHONE_REG = @"^[1][3,4,5,7,8][0-9]{9}$";

        public const string EMAIL_REG = @"^[a-zA-Z0-9_\-\.]+@[a-zA-Z0-9_\-\.]+(.[a-zA-Z0-9_-]+)+$";

        public const string JSONNAME_REG = @"^[\u4e00-\u9fa5\dA-Za-z-/*#%()]+$";

        public const string JSON_REG = @"^[\u4e00-\u9fa5\dA-Za-z-\\/*#%()" + "\":,{}]+$";

        public const string IPS_REG = @"\b(?:\d{1,3}\.){3}\d{1,3}\b";

        public const int INTERVAL_REFRESH_DEVICE_STATUS = 60 * 1000;

        public const int TIMEOUT_REQUEST_DEFAULT = 30 * 1000;

        public const int TIMEOUT_REQUEST_REFRESH_DEVICE_STATUS = 5 * 1000;

        public const int TIMEOUT_REQUEST_TEST = 200;

        public const int DOWNLOAD_IMAGE_RETRY_COUNT = 5;

        public const char NAVIGATION_SEPARATOR = '>';

        public const int DECIMAL_COUNT = 1;

        public const string SIGNALR_ROUTER = "/event";
        public const string SIGNALR_METHOD_NAME = "eventNotify";

        public const int SIGNALR_CONNECTED_RETRY_COUNT = 3;
        public const int SIGNALR_CONNECTED_RETRY_DELAY = 300;
        public const int SIGNALR_SERVER_CHECK_DELAY = 60;
        public const int DEVICE_STATUS_CHECK_DELAY = 20;

        public const string PREFERENCE_FOLDER = @"~Preference\";

        public const int INVALID_ID = 0;
        public const string DEFAULT_ID = "0";
        public const string DEFAULT_PARENT_ID = "-1";

        public const string DEFAULT_IP = "192.168.1.1";
        public const int DEFAULT_PORT = 80;

        public const int LANTERN_DEFAULT_PORT = 10080;
        public const int IRTEKSDK2_DEFAULT_PORT = 80;
        public const int SV_DEFAULT_PORT = 8000;
        public const int CZF_ONVIF_IR_DEFAULT_PORT = 2000;

        public const int ONVIF_DC_DEFAULT_PORT = 80;
        public const int EDGE_BOX_DEFAULT_PORT = 80;

        public const int HIK_NVR_DEFAULT_PORT = 8000;
        public const int DH_NVR_DEFAULT_PORT = 37777;
        public const int XM_NVR_DEFAULT_PORT = 34567;
        public const int TW_NVR_DEFAULT_PORT = 6036;

        public const string ONVIF_MODEL_IR_LANTERN = "";
        public const string ONVIF_NAME_IR_LANTERN = "Fotric";

        // 舜程
        public const string ONVIF_MODEL_IR_SV = "ATiSCL113";
        public const string ONVIF_NAME_IR_SV = "ATiSCL113";
        // 创智飞
        public const string ONVIF_MODEL_IR_CZF = "P256";
        public const string ONVIF_NAME_IR_CZF = "skyworth";
        // 雄迈
        public const string ONVIF_MODEL_DC_XM = "IPC-model";
        public const string ONVIF_NAME_DC_XM = "NVT";
        // 创维
        public const string ONVIF_MODEL_DC_CW = "CSD03";
        public const string ONVIF_NAME_DC_CW = "skyworth";

        public const int STORE_SERVER_ADDRESS_DEFAULT_PORT = 9070;
        public const int FIRE_SERVER_ADDRESS_DEFAULT_PORT = 9080;

        public const string KEY_64 = ")(*&^%$#";
        public const string IV_64 = ")(*&^%$#";

        public const char SPLIT_CHAR = ',';

        public const int ONE_SECOND = 1000;
        public const int ONE_MINUTE = 60 * 1000;
        public const int NO_DETECTED_POINT_DATA_ID = -1;

        public const string ZH_CN_LANGUAGE = "zh-CN";

        public const int TRACKING_TIME = 10 * 1000;

        public static string DOWNLOAD_PATH = "download";
    }
}