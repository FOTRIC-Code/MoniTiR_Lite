// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Windows;
using System.ComponentModel;

using Amur.Controls;

namespace Firefly.Client.Control
{
    public class SymbolIcon : FontIcon
    {
        public static readonly DependencyProperty SymbolProperty = DependencyProperty.Register(nameof(Symbol), typeof(SymbolRegular), typeof(SymbolIcon), new PropertyMetadata(SymbolRegular.Empty, OnGlyphChanged));

        [Bindable(true), Category("Appearance")]
        public SymbolRegular Symbol
        {
            get => (SymbolRegular)GetValue(SymbolProperty);
            set => SetValue(SymbolProperty, value);
        }

        public SymbolIcon()
        {
            
        }

        public SymbolIcon(SymbolRegular symbol, double fontSize = 14)
        {
            Symbol = symbol;
            FontSize = fontSize;
        }

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);

            SetResourceReference(FontFamilyProperty, "SymbolRegular");
        }

        private static void OnGlyphChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            SymbolIcon icon = (SymbolIcon)d;
            if(icon != null)
            {
                icon.Glyph = icon.Symbol.GetString();
            }
        }
    }
}
