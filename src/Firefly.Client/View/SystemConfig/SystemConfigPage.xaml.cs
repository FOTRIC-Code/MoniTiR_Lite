// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System.Windows.Controls;

using Firefly.Client.Control;

namespace Firefly.Client
{
    /// <summary>
    /// Interaction logic for SystemConfigPage.xaml
    /// </summary>
    public partial class SystemConfigPage : Page, IPage
    {
        public SystemConfigPage()
        {
            InitializeComponent();
        }

        public void OnClosed()
        {
        }

        private void SystemConfigPage_OnLoaded(object sender, System.Windows.RoutedEventArgs e)
        {
        }
    }
}
