// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Windows;
using System.Windows.Media.Imaging;

using Amur.Controls;
using Firefly.Client.Common;

namespace Firefly.Client
{
    /// <summary>
    /// Interaction logic for SplashScreenWindow.xaml
    /// </summary>
    public partial class SplashWindow : Window, ISplashScreen
    {
        public SplashWindow()
        {
            InitializeComponent();
        }

        public void AddMessage(string message)
        {
            Dispatcher.Invoke((Action)delegate()
            {
                this.UpdateMessageTextBox.Text = message;
            });
        }

        public void LoadComplete()
        {
            Dispatcher.InvokeShutdown();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (string.Equals(AppPreference.Instance.SystemLanguage, Constants.ZH_CN_LANGUAGE))
            {
                ImageBehavior.SetAnimatedSource(imgSplash, new BitmapImage(new Uri(@"pack://application:,,,/MoniTiRLite;component/Resources/splash.gif")));
            }
            else
            {
                ImageBehavior.SetAnimatedSource(imgSplash, new BitmapImage(new Uri(@"pack://application:,,,/MoniTiRLite;component/Resources/splash_en.gif")));
            }
        }
    }

    public interface ISplashScreen
    {
        void AddMessage(string message);
        void LoadComplete();
    }
}
