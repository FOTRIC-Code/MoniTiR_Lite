// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Windows.Markup;

namespace Firefly.Client.Control
{
    public class ValidationRang
    {
        public Type TargetType { get; set; }

        public int MaxDecimalLength { get; set; }

        public object DefaultValue { get; set; }

        public object Minimum { get; set; }

        public object Maximum { get; set; }

        public object ConvertBackValue { get; set; }

        // /// <summary>
        // /// 是否可正数
        // /// </summary>
        // public bool IsPositiveNumber { get; set; } = true;
        //
        // /// <summary>
        // /// 是否可负数
        // /// </summary>
        // public bool IsNegativeNumber { get; set; } = false;
    }
}