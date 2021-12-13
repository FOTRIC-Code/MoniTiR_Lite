// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.IO;
using System.Text;
using System.Configuration;
using System.Security.Cryptography;

using Firefly.Client.Common;
using Firefly.Client.Service;
using Firefly.Client.Service.LiteDb;

namespace Firefly.Client
{
    public class AppConfig
    {
        public static AppConfig Instance = null;

        public string SalesTitle { get; set; }

        public string SalesMD5 { get; set; }

        public bool IsSales { get; set; }

        public bool EnableHardwareAcc { get; set; }

        public bool SupportLivePreviewGroup { get; set; }

        public bool ShowSplash { get; set; }

        public string LiteServerRoot { get; set; }

        public AppConfig()
        { }

        public void Load()
        {
            LiteDatabase.EnsureInitialized();

            // Fixed local agents — no Admin gateway.
            string fireUrl = "http://127.0.0.1:9080";
            ServerManager.Instance.ServerManagerURL = fireUrl;
            DeviceManager.Instance.ServerURL = fireUrl;
            AlarmManager.Instance.ServerURL = fireUrl;
            SystemConfigManager.Instance.ServerURL = fireUrl;
            SupportLivePreviewGroup = Convert.ToBoolean(ConfigurationManager.AppSettings["SUPPORT_LIVE_GROUP"]);
            ShowSplash = Convert.ToBoolean(ConfigurationManager.AppSettings["SHOW_SPLASH"]);

            LiteServerRoot = ConfigurationManager.AppSettings["LITE.SERVER_ROOT"] ?? string.Empty;

            SalesTitle = ConfigurationManager.AppSettings["SALESTITLE"];
            SalesMD5 = ConfigurationManager.AppSettings["SALESMD5"];
            EnableHardwareAcc = Convert.ToBoolean(ConfigurationManager.AppSettings["HWACC"]);

            if (!string.IsNullOrWhiteSpace(SalesTitle))
            {
                string tempSalesTitle = SalesTitle;
                string tempSalesMD5 = SalesMD5;

                byte[] byKey = ASCIIEncoding.ASCII.GetBytes(Constants.KEY_64);
                byte[] byIV = ASCIIEncoding.ASCII.GetBytes(Constants.IV_64);

                using (DESCryptoServiceProvider des = new DESCryptoServiceProvider())
                {
                    using (MemoryStream ms = new MemoryStream())
                    {
                        CryptoStream cs = new CryptoStream(ms, des.CreateEncryptor(byKey, byIV), CryptoStreamMode.Write);
                        StreamWriter sw = new StreamWriter(cs);
                        sw.Write(tempSalesTitle);
                        sw.Flush();
                        cs.FlushFinalBlock();
                        sw.Flush();
                        byte[] buffer = ms.GetBuffer();
                        using (MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider())
                        {
                            buffer = md5.ComputeHash(buffer);
                            //tempSalesMD5 = Convert.ToBase64String(buffer, 0, (int)buffer.Length);
                            if (tempSalesMD5 == Convert.ToBase64String(buffer, 0, (int)buffer.Length))
                            {
                                SalesTitle = tempSalesTitle;
                                IsSales = true;
                            }
                        }
                    }
                }
            }
        }
    }
}