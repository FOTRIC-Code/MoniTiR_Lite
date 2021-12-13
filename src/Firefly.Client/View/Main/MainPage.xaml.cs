// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System.Windows;
using System.Windows.Controls;

using Firefly.Client.Control;

namespace Firefly.Client
{
    /// <summary>
    /// Interaction logic for MainControl.xaml
    /// </summary>
    public partial class MainPage : Page, IPage
    {
        public MainViewModel _viewModel;
        public static readonly RoutedEvent NavigatingEvent = EventManager.RegisterRoutedEvent("NavigateToNewPage", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(MainPage));
        public static readonly RoutedEvent NotifyBalloonTipEvent = EventManager.RegisterRoutedEvent("NotifyBallTip", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(MainPage));

        public event RoutedEventHandler Navigating
        {
            add { AddHandler(NavigatingEvent, value); }
            remove { RemoveHandler(NavigatingEvent, value); }
        }

        public event RoutedEventHandler BalloonTip
        {
            add { AddHandler(NavigatingEvent, value); }
            remove { RemoveHandler(NavigatingEvent, value); }
        }

        public MainPage(MainViewModel mainViewModel)
        {
            InitializeComponent();
            _viewModel = mainViewModel;
            DataContext = _viewModel;
        }

        private void StackPanel_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            PageViewModel selectedPage = (sender as FrameworkElement).Tag as PageViewModel;

            if (selectedPage.Page == null)
            {
                IRtekMessageBox.ShowDialog(Window.GetWindow(this), I18n.Properties.Resources.NoPageTip, I18n.Properties.Resources.Tip);
            }
            else
            {
                RoutedEventArgs args = new RoutedEventArgs
                {
                    RoutedEvent = NavigatingEvent,
                    Source = selectedPage
                };

                RaiseEvent(args);
            }
        }

        public void OnClosed()
        {
        }
    }
}
