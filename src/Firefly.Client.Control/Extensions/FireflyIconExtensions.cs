// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System.Text;
using System;

namespace Firefly.Client.Control
{
    public static class FireflyIconExtensions
    {
        public static string GetString(this SymbolRegular icon)
        {
            return Encoding.Unicode.GetString(BitConverter.GetBytes((int)icon)).TrimEnd('\0');
        }
    }
}
