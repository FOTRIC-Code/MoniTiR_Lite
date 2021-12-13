// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System.Windows;
using System.Windows.Media;
using System.Windows.Controls;
using Amur.Controls;
using System.ComponentModel;

namespace Firefly.Client.Control
{
    public class ClosableLabel : Label
    {

        static ClosableLabel()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ClosableLabel), new FrameworkPropertyMetadata(typeof(ClosableLabel)));
        }

        public static readonly RoutedEvent CloseEvent = EventManager.RegisterRoutedEvent("Close", RoutingStrategy.Direct, typeof(RoutedEventHandler), typeof(ClosableLabel));

        public event RoutedEventHandler Close
        {
            add { AddHandler(CloseEvent, value); }
            remove { RemoveHandler(CloseEvent, value); }
        }

        public static readonly DependencyProperty IconProperty = DependencyProperty.Register(nameof(Icon), typeof(IconElement), typeof(ClosableLabel), new PropertyMetadata(null));

        [Bindable(true), Category("Appearance")]
        public IconElement Icon
        {
            get { return (IconElement)GetValue(IconProperty); }
            set { SetValue(IconProperty, value); }
        }

        public static readonly DependencyProperty IsSelectedProperty = DependencyProperty.Register("IsSelected", typeof(bool), typeof(ClosableLabel),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));

        public bool IsSelected
        {
            get { return (bool)GetValue(IsSelectedProperty); }
            set { SetValue(IsSelectedProperty, value); }
        }

        public static readonly DependencyProperty CanCloseProperty = DependencyProperty.Register("CanClose", typeof(bool), typeof(ClosableLabel),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));

        public bool CanClose
        {
            get { return (bool)GetValue(CanCloseProperty); }
            set { SetValue(CanCloseProperty, value); }
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            Amur.Controls.Button btnClose = (Amur.Controls.Button)GetTemplateChild("btnClose");
            btnClose.Click += btnClose_Click;
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            RoutedEventArgs newEvent = new RoutedEventArgs(CloseEvent, this);
            RaiseEvent(newEvent);
        }
    }
}
