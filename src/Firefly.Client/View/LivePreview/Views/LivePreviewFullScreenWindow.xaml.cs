// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System.Windows;
using System.Windows.Input;
using System.ComponentModel;

namespace Firefly.Client
{
    public partial class LivePreviewFullScreenWindow : Window
    {
        public SplitVideoControl VideoControl { get; set; }

        public LivePreviewFullScreenWindow()
        {
            InitializeComponent();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.Key == Key.Escape)
            {
                this.Close();
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            gridParent.Children.Remove(VideoControl);
            this.VideoControl = null;
        }

        private void LivePreviewFullScreenWindow_Loaded(object sender, RoutedEventArgs e)
        {
            gridParent.Children.Add(VideoControl);
        }
    }
}
