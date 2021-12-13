// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System.Windows;
using System.Collections.Generic;

using Newtonsoft.Json;

namespace Firefly.Client.Control.Fire
{
    public class ChannelConfigDataObject
    {
        public ChannelConfigDataObject()
        {
        }

        public ChannelConfigDataObject(List<ChannelDataObject> channelDataObjectList)
        {
            ChannelDataObjectList = channelDataObjectList;
        }

        [JsonProperty("channelList")]
        public List<ChannelDataObject> ChannelDataObjectList { get; set; }
    }

    public class ChannelDataObject
    {
        private const int DEFAULT_CHANNEL_ID = 1;

        public ChannelDataObject()
        {
            ID = DEFAULT_CHANNEL_ID;
            Name = I18n.Properties.Resources.ChannelZero;
            Status = (int)OSDStatus.Disable;
            Positon = new OSDPoint();
            TimeDataObject = new TimeDataObject();
        }

        public ChannelDataObject(int id, OSDStatus osdStatus, string name, Point positon, TimeDataObject timeDataObject)
        {
            ID = id;
            Status = (int)osdStatus;
            Name = name;
            Positon = new OSDPoint((int)positon.X, (int)positon.Y);
            TimeDataObject = timeDataObject;
        }

        [JsonProperty("id")]
        public int ID { get; set; }

        [JsonProperty("status")]
        public int Status { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("position")]
        public OSDPoint Positon { get; set; }

        [JsonProperty("time")]
        public TimeDataObject TimeDataObject { get; set; }
    }

    public class TimeDataObject
    {
        public TimeDataObject()
        {
            Status = (int)OSDStatus.Disable;
            Positon = new OSDPoint();
        }

        public TimeDataObject(OSDStatus osdStatus, Point positon)
        {
            Status = (int)osdStatus;
            Positon = new OSDPoint((int)positon.X, (int)positon.Y);
        }

        [JsonProperty("status")]
        public int Status { get; set; }

        [JsonProperty("position")]
        public OSDPoint Positon { get; set; }
    }

    public class PTZOSDConfigDataObject
    {
        public PTZOSDConfigDataObject()
        {
            Status = (int)OSDStatus.Disable;
            Positon = new OSDPoint();
        }

        public PTZOSDConfigDataObject(OSDStatus osdStatus, Point positon)
        {
            Status = (int)osdStatus;
            Positon = new OSDPoint((int)positon.X, (int)positon.Y);
        }

        [JsonProperty("status")]
        public int Status { get; set; }

        [JsonProperty("position")]
        public OSDPoint Positon { get; set; }
    }

    public class MarkerOSDConfigDataObject
    {
        public MarkerOSDConfigDataObject()
        {
            Status = (int)OSDStatus.Disable;
            ShowMax = false;
            ShowMin = false;
            ShowAvg = false;
        }

        public MarkerOSDConfigDataObject(OSDStatus osdStatus, bool showMax, bool showMin, bool showAvg)
        {
            Status = (int)osdStatus;
            ShowMax = showMax;
            ShowMin = showMin;
            ShowAvg = showAvg;
        }

        [JsonProperty("status")]
        public int Status { get; set; }

        [JsonProperty("showMax")]
        public bool ShowMax { get; set; }

        [JsonProperty("showMin")]
        public bool ShowMin { get; set; }

        [JsonProperty("showAvg")]
        public bool ShowAvg { get; set; }
    }

    public class OSDPoint
    {
        public OSDPoint()
        {
        }

        public OSDPoint(int x, int y)
        {
            X = x;
            Y = y;
        }

        [JsonProperty("x")]
        public int X { get; set; }

        [JsonProperty("y")]
        public int Y { get; set; }
    }
}