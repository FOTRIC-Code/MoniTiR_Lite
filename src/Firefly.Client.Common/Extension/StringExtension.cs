// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Firefly.Client.Common
{
    public static class StringExtension
    {
        /// <summary>
        /// Check current file name whether is tracked by system.
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static bool EndsWith(this string s, List<string> value)
        {
            return value.Any(item => s.EndsWith(item, System.StringComparison.OrdinalIgnoreCase));
        }

        private static int CountByte(this string s)
        {
            if (s == null)
            {
                return 0;
            }
            int valueLength = 0;
            string chinese = @"[\u4E00-\u9FFF]+";
            /* 获取字段值的长度，如果含中文字符，则每个中文字符长度为2，否则为1 */
            for (int i = 0; i < s.Length; i++)
            {
                /* 获取一个字符 */
                string temp = s.Substring(i, 1);
                /* 判断是否为中文字符 */
                if (Regex.IsMatch(temp, chinese))
                {
                    /* 中文字符长度为2 */
                    valueLength += 2;
                }
                else
                {
                    /* 其他字符长度为1 */
                    valueLength += 1;
                }
            }
            return valueLength;
        }
    }
}
