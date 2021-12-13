// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using Amur.Common;

namespace Firefly.Client.Control
{
    public class NotEqualityToBoolConverter : EqualityConverter<bool>
    {
        public NotEqualityToBoolConverter()
            : base(false, true)
        {

        }
    }
}
