// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System.Windows.Controls;
using Firefly.Client.Control;

namespace Firefly.Client
{
    /// <summary>
    /// Interaction logic for ResourceControl.xaml
    /// </summary>
    public partial class ResourcePage : Page, IPage
    {
        public ResourcePage()
        {
            InitializeComponent();
        }

        public void OnClosed()
        {
            tcResource.SelectedItem = null;
        }

        private void ResourcePage_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            tcResource.SelectedIndex = 0;
        }
    }
}
