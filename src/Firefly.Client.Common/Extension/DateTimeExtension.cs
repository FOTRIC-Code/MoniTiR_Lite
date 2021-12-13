// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Net;

namespace Firefly.Client.Common
{
    public static class DateTimeExtension
    {
        public static string YearAndMonth(this DateTime date)
        {
            return date.ToString("yyyy-MM");
        }

        public static string ToFireflyFormat(this DateTime date)
        {
            return date.ToString("yyyy-MM-dd HH:mm:ss");
        }

        public static string ToUrlString(this DateTime date)
        {
            return WebUtility.UrlEncode(date.ToString());
        }
    }
}
