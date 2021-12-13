// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System.Windows;
using System.Windows.Controls;

using Firefly.Client.Control;

namespace Firefly.Client
{
    /// <summary>
    /// Interaction logic for ImageCanvasConfigControl.xaml
    /// </summary>
    public partial class ImageCanvasConfigControl : TabItem
    {
        public ImageCanvasConfigControl()
        {
            InitializeComponent();
        }

        private void ImageCanvasConfigControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            SystemConfigPreference.Intance.Load();
            if (SystemConfigPreference.Intance.RenderMode == ImageRenderMode.Fit)
            {
                cbRenderMode.SelectedIndex = 0;
            }
            else if (SystemConfigPreference.Intance.RenderMode == ImageRenderMode.Fill)
            {
                cbRenderMode.SelectedIndex = 1;
            }
        }

        private void btnSave_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (cbRenderMode.SelectedIndex == 0)
            {
                SystemConfigPreference.Intance.RenderMode = ImageRenderMode.Fit;
            }
            else
            {
                SystemConfigPreference.Intance.RenderMode = ImageRenderMode.Fill;
            }
            SystemConfigPreference.Intance.Save(); 
            IRtekMessageBox.ShowDialog(Window.GetWindow(this), I18n.Properties.Resources.SavedSuccessfully, I18n.Properties.Resources.Tip, Control.MessageBoxButton.OK);
        }
    }
}