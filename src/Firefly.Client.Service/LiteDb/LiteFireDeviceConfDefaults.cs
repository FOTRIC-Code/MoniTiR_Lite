// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Firefly.Client.Service.LiteDb
{
    /// <summary>
    /// Default Fire device <c>conf</c> written on create (Lite in-proc).
    /// Shape matches FireAgentDataObject / engine FireMonitorDeviceConfigBO camelCase JSON.
    /// </summary>
    public static class LiteFireDeviceConfDefaults
    {
        public static string CreateDefaultJson()
        {
            JObject conf = new JObject
            {
                ["version"] = 0,
                ["generalConfig"] = new JObject
                {
                    ["sampleInterval"] = 5.0,
                    ["dataRecordEnabled"] = false,
                    ["dataRecordInterval"] = 5,
                    ["imageRecordEnabled"] = false,
                    ["imageRecordInterval"] = 5,
                    ["fixedMonitoring"] = true,
                    // Matches GeneralConfigDataObject JsonProperty (includes two U+200B after "Resolution").
                    ["enableSuperResolution\u200b\u200b"] = false
                },
                ["eventTrigger"] = new JObject
                {
                    ["alert1Out"] = new JArray(),
                    ["alert2Out"] = new JArray(),
                    ["alarmOut"] = new JArray()
                },
                ["osdConfigEnabled"] = false,
                ["channelConfig"] = new JObject
                {
                    ["channelList"] = new JArray
                    {
                        new JObject
                        {
                            ["id"] = 1,
                            ["status"] = 0,
                            ["name"] = "Channel0",
                            ["position"] = new JObject { ["x"] = 0, ["y"] = 0 },
                            ["time"] = new JObject
                            {
                                ["status"] = 0,
                                ["position"] = new JObject { ["x"] = 0, ["y"] = 0 }
                            }
                        }
                    }
                },
                ["ptzOSDConfig"] = new JObject
                {
                    ["status"] = 0,
                    ["position"] = new JObject { ["x"] = 0, ["y"] = 0 }
                },
                ["markerOSDConfig"] = new JObject
                {
                    ["status"] = 0,
                    ["showMax"] = false,
                    ["showMin"] = false,
                    ["showAvg"] = false
                },
                ["presetConfig"] = new JObject
                {
                    ["presetList"] = new JArray()
                },
                ["alarmRuleConfig"] = new JObject
                {
                    ["alarmRuleList"] = new JArray()
                },
                ["cruiseRouteConfig"] = new JObject
                {
                    ["cruiseRouteList"] = new JArray()
                },
                ["taskSchedule"] = new JObject
                {
                    ["enabled"] = false,
                    ["restoretime"] = 5,
                    ["taskDayList"] = CreateWeekDays("taskTimeList")
                },
                ["alarmSchedule"] = new JObject
                {
                    ["enabled"] = false,
                    ["alarmDayList"] = CreateWeekDays("alarmTimeList")
                }
            };

            return conf.ToString(Formatting.None);
        }

        private static JArray CreateWeekDays(string timeListProperty)
        {
            JArray days = new JArray();
            foreach (DayOfWeek day in Enum.GetValues(typeof(DayOfWeek)))
            {
                days.Add(new JObject
                {
                    ["dayOfWeek"] = (int)day,
                    [timeListProperty] = new JArray()
                });
            }

            return days;
        }
    }
}
