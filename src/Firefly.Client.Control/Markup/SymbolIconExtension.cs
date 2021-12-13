// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Windows.Markup;

namespace Firefly.Client.Control
{
    [ContentProperty(nameof(Symbol))]
    [MarkupExtensionReturnType(typeof(SymbolIcon))]
    public class SymbolIconExtension : MarkupExtension
    {
        public SymbolIconExtension(SymbolRegular symbol)
        {
            Symbol = symbol;
        }

        [ConstructorArgument("symbol")]
        public SymbolRegular Symbol { get; set; }

        public double FontSize { get; set; }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            var symbolIcon = new SymbolIcon { Symbol = Symbol };

            if (FontSize > 0)
            {
                symbolIcon.FontSize = FontSize;
            }

            return symbolIcon;
        }
    }
}
