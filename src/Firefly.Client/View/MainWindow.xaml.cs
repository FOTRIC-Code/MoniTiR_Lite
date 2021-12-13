// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using Amur.Common.Extension;
using Firefly.Client.Common;
using Firefly.Client.Control;
using Firefly.Client.Service;
using log4net;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;

namespace Firefly.Client
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Fields

        [DllImport("user32.dll")]
        private static extern int ShowWindow(IntPtr hwnd, int nCmdShow);

        private const int MAX_TITLE_LENGTH = 16;
        private MainViewModel _viewModel;
        private NotifyIcon _notifyIcon;
        private bool _visibleFlag = true;

        private DispatcherTimer _dispatcherTimer;

        private string _title;

        private ILog _logger = LogManager.GetLogger(typeof(MainWindow));

        #endregion Fields

        public bool IsDoClose { get; set; }

        #region Constructors

        public MainWindow()
        {
            Thread.CurrentThread.CurrentUICulture = new CultureInfo(AppPreference.Instance.SystemLanguage);

            InitializeComponent();

            if (!string.IsNullOrWhiteSpace(MainWindowPreference.Instance.Title))
            {
                _title = MainWindowPreference.Instance.Title;
            }
            else
            {
                _title = I18n.Properties.Resources.Title;
            }

            _viewModel = new MainViewModel();

            DataContext = _viewModel;
            InitLog();
            SwitchToPage(_viewModel.Main);
            AddHandler(MainPage.NavigatingEvent, new RoutedEventHandler(OnNavigating));
            AddHandler(MainPage.NotifyBalloonTipEvent, new RoutedEventHandler(OnNotifyBalloonTip));

            _notifyIcon = new NotifyIcon();

            _notifyIcon.BalloonTipText = _title;
            _notifyIcon.ShowBalloonTip(2000);
            _notifyIcon.Text = _title;
            _notifyIcon.Icon = System.Drawing.Icon.ExtractAssociatedIcon(System.Windows.Forms.Application.ExecutablePath);
            _notifyIcon.Visible = true;

            _notifyIcon.ContextMenuStrip = new ContextMenuStrip();
            _notifyIcon.ContextMenuStrip.Items.Add(I18n.Properties.Resources.ShowMainWindow, null, cmsShowMainWindow_Click);
            _notifyIcon.ContextMenuStrip.Items.Add(I18n.Properties.Resources.ExitExe, null, cmsExitExe_Click);
            _notifyIcon.MouseDoubleClick += notifyIcon_MouseDoubleClick;
            _notifyIcon.BalloonTipClicked += notifyIcon_BalloonTipClicked;

            lvPages.AddHandler(MouseWheelEvent, new MouseWheelEventHandler(lvPages_MouseWheel), true);

            if (_dispatcherTimer == null)
            {
                _dispatcherTimer = new DispatcherTimer();
                _dispatcherTimer.Interval = TimeSpan.FromMilliseconds(1000);
                _dispatcherTimer.Tick += Dispatcher_Tick;
                _dispatcherTimer.Start();
            }
        }

        #endregion Constructors

        #region Methods

        private void InitLog()
        {
            if (MainWindowPreference.Instance.LastStartTime == 0)
            {
                MainWindowPreference.Instance.LastStartTime = DateTime.Now.ToBinary();
            }
            string dateTime = DateTime.FromBinary(MainWindowPreference.Instance.LastStartTime).ToString("yyyy-MM-dd");
            if (string.Equals(DateTime.Today.ToString("yyyy-MM-dd"), dateTime))
            {
                MainWindowPreference.Instance.StartCount++;
            }
            else
            {
                MainWindowPreference.Instance.LastStartTime = DateTime.Now.ToBinary();
                MainWindowPreference.Instance.StartCount = 1;
            }
            AssemblyName assemblyName = Assembly.GetExecutingAssembly().GetName();
            _logger.Info($"\n************************* {dateTime} *************************" +
                $"\n\tv{assemblyName.Version}" +
                $"\n\t{WindowsIdentity.GetCurrent().Name} The software's {MainWindowPreference.Instance.StartCount} launch." +
                $"\n**************************************************************");
            _logger.Error($"\n************************* {dateTime} *************************" +
                $"\n\tv{assemblyName.Version}" +
                $"\n\t{WindowsIdentity.GetCurrent().Name} The software's {MainWindowPreference.Instance.StartCount} launch." +
                $"\n**************************************************************");
            _logger.EventTracking($"\n************************* {dateTime} *************************" +
                $"\n\tv{assemblyName.Version}" +
                $"\n\t{WindowsIdentity.GetCurrent().Name} The software's {MainWindowPreference.Instance.StartCount} launch." +
                $"\n**************************************************************");
            MainWindowPreference.Instance.Save();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);

            TrackingService.Instance.Close(typeof(MainWindow));
            if (!IsDoClose)
            {
                _visibleFlag = false;
                if (_dispatcherTimer.IsEnabled)
                {
                    _dispatcherTimer.Stop();
                }
                ShowWindow(new WindowInteropHelper(this).Handle, App.SW_HIDE);
                _notifyIcon.BalloonTipIcon = ToolTipIcon.Info;
                _notifyIcon.Tag = null;
                _notifyIcon.ShowBalloonTip(20, _title, I18n.Properties.Resources.MinimizeToTray, ToolTipIcon.Info);
                e.Cancel = true;
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            fmPages.NavigationService.RemoveBackEntry();
            _notifyIcon.Visible = false;
            _notifyIcon.MouseDoubleClick -= notifyIcon_MouseDoubleClick;
            _notifyIcon.BalloonTipClicked -= notifyIcon_BalloonTipClicked;
            _notifyIcon.Dispose();
        }

        private void notifyIcon_BalloonTipClicked(object sender, EventArgs e)
        {
            if (sender is NotifyIcon notifyIcon)
            {
                if (notifyIcon.BalloonTipIcon == ToolTipIcon.Warning)
                {
                    _visibleFlag = true;
                    ShowWindow();
                }
                if (notifyIcon.Tag is BalloonFileTipArgs tipArgs)
                {
                    if (tipArgs.Type == BalloonTipType.FileSave)
                    {
                        List<List<IRFileViewerFileInfo>> imagesGroups = new List<List<IRFileViewerFileInfo>>();
                        int currentIndex = 0;
                        imagesGroups.Add(new List<IRFileViewerFileInfo>()
                        {
                            new IRFileViewerFileInfo(tipArgs.ChannelID,tipArgs.FilePath,tipArgs.FileType)
                        });
                        new IRFileViewerWindow(imagesGroups, currentIndex) { Owner = GetWindow(this) }.ShowDialog();
                    }
                }
            }
        }

        private void clPage_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            PageViewModel pageVM = (sender as ClosableLabel).Tag as PageViewModel;
            SwitchToPage(pageVM);
        }

        private void clPage_Close(object sender, RoutedEventArgs e)
        {
            PageViewModel pageVM = (sender as ClosableLabel).Tag as PageViewModel;
            if (_viewModel.SelectedItem == pageVM)
            {
                TrackingService.Instance.Close(pageVM.Page.GetType());
                SwitchToPage(_viewModel.Main);
            }

            _viewModel.TryRemoveFromDisplayPages(pageVM);
        }

        internal void OnNavigating(object sender, RoutedEventArgs args)
        {
            PageViewModel pageView = args.OriginalSource as PageViewModel;

            if (pageView != null)
            {
                SwitchToPage(pageView);
            }
        }

        private void OnNotifyBalloonTip(object sender, RoutedEventArgs e)
        {
            _notifyIcon.Tag = null;
            if (e.OriginalSource is BalloonTipArgs balloonTip)
            {
                if (balloonTip.Type == BalloonTipType.Alarm && !_visibleFlag)
                {
                    _notifyIcon.BalloonTipIcon = ToolTipIcon.Warning;
                    _notifyIcon.ShowBalloonTip(20, _title, balloonTip.Message, ToolTipIcon.Warning);
                }
                else if (balloonTip.Type == BalloonTipType.FileSave)
                {
                    _notifyIcon.BalloonTipIcon = ToolTipIcon.Info;
                    string recordMessage = $"{I18n.Properties.Resources.FileSaved}:{balloonTip.Message}";
                    _notifyIcon.ShowBalloonTip(20, _title, recordMessage, ToolTipIcon.Info);
                    _notifyIcon.Tag = balloonTip;
                }
                else if (balloonTip.Type == BalloonTipType.FullPage)
                {
                    if (irHeader.Visibility == Visibility.Visible)
                    {
                        irHeader.Visibility = Visibility.Collapsed;
                        fmPages.Margin = new Thickness(0, 0, 0, 0);
                    }
                    else
                    {
                        irHeader.Visibility = Visibility.Visible;
                        fmPages.Margin = new Thickness(0, 0, 0, _viewModel.NoContainAlarmPage ? 45 : 0);
                    }
                    if (alarmBar.Visibility == Visibility.Visible)
                    {
                        alarmBar.Visibility = Visibility.Collapsed;
                    }
                    else
                    {
                        alarmBar.Visibility = Visibility.Visible;
                    }
                }
            }
        }

        private void cmsShowMainWindow_Click(object sender, EventArgs e)
        {
            _visibleFlag = true;

            if (!_dispatcherTimer.IsEnabled)
            {
                _dispatcherTimer.Start();
            }
            ShowWindow(new WindowInteropHelper(this).Handle, App.SW_SHOW);
        }

        private void cmsExitExe_Click(object sender, EventArgs e)
        {
            IsDoClose = true;
            Close();
        }

        private void notifyIcon_MouseDoubleClick(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            _visibleFlag = true;
            if (!_dispatcherTimer.IsEnabled)
            {
                _dispatcherTimer.Start();
            }
            ShowWindow(new WindowInteropHelper(this).Handle, App.SW_SHOW);
        }

        private void SwitchToPage(PageViewModel pageView)
        {
            try
            {
                TrackingService.Instance.In(pageView.Page.GetType());
                Cursor = System.Windows.Input.Cursors.Wait;

                _viewModel.TryAddToDisplayPages(pageView);
                _viewModel.SelectedItem = pageView;
                lvPages.SelectedItem = pageView;
                fmPages.Navigate(pageView.Page);
            }
            finally
            {
                Cursor = System.Windows.Input.Cursors.Arrow;
            }
        }

        #endregion Methods

        private void Dispatcher_Tick(object sender, EventArgs e)
        {
            _viewModel.Time = DateTime.Now;
        }

        private async void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            this.Topmost = true;
            this.Topmost = false;
            IRtekWaitWindow waitWindow = new IRtekWaitWindow(Window.GetWindow(this), IRtekWaitWindowType.Animation);

            try
            {
                Mouse.SetCursor(System.Windows.Input.Cursors.Wait);
                waitWindow.Show();
                _notifyIcon.BalloonTipText = _title;
                _notifyIcon.Text = _title;
                irHeader.Title = _title;
                Title = _title;

                await LiteAgentBootstrap.EnsureLocalAgentsAsync();

                CustomSoftwareDO customSoftwareDO = await SystemConfigManager.Instance.GetCustomSoftware();
                string fileName = string.Empty;
                if (!string.IsNullOrWhiteSpace(customSoftwareDO?.IconPath) && File.Exists(customSoftwareDO.IconPath))
                {
                    fileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Path.GetFileName(customSoftwareDO.IconPath));
                    File.Copy(customSoftwareDO.IconPath, fileName, true);
                    SystemConfigPreference.Intance.SoftwareIconPath = fileName;
                    SystemConfigPreference.Intance.SoftwareDownloadIconPath = customSoftwareDO.IconPath;
                }

                SystemConfigPreference.Intance.SoftwareTitleFontSize = customSoftwareDO.FontSize <= 0 ? 16 : customSoftwareDO.FontSize;
                SystemConfigPreference.Intance.Save();

                _viewModel.SoftwareTitleFontSize = SystemConfigPreference.Intance.SoftwareTitleFontSize;

                if (!string.IsNullOrWhiteSpace(SystemConfigPreference.Intance.SoftwareIconPath))
                {
                    _viewModel.IconPath = SystemConfigPreference.Intance.SoftwareIconPath;
                }
            }
            finally
            {
                await Task.Delay(1500);
                waitWindow.Close();
                Mouse.SetCursor(System.Windows.Input.Cursors.Arrow);
            }
        }

        private void FormatTitle()
        {
            if (_title.Length > MAX_TITLE_LENGTH)
            {
                _title = $"{_title.Substring(0, MAX_TITLE_LENGTH)}...";
            }
        }

        private void lvPages_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            ItemsControl items = (ItemsControl)sender;

            Border border = VisualTreeHelper.GetChild(items, 0) as Border;
            ScrollViewer scroll = VisualTreeHelper.GetChild(border, 0) as ScrollViewer;
            if (scroll != null)
            {
                int d = e.Delta;
                if (d > 0)
                {
                    scroll.LineLeft();
                }
                if (d < 0)
                {
                    scroll.LineRight();
                }
                scroll.ScrollToTop();
            }
        }

        public new void ShowDialog()
        {
            AlarmManager.Instance.Start();
            ServerManager.Instance.Start();
            DeviceManager.Instance.Start();

            if (IsLoaded)
            {
                ShowWindow();
            }
            else
            {
                base.ShowDialog();
            }
        }

        private async Task ShowWindow()
        {
            this.Topmost = true;
            this.Topmost = false;
            IRtekWaitWindow waitWindow = new IRtekWaitWindow(Window.GetWindow(this), IRtekWaitWindowType.Animation);

            try
            {
                Mouse.SetCursor(System.Windows.Input.Cursors.Wait);
                waitWindow.Show();
                if (!_dispatcherTimer.IsEnabled)
                {
                    _dispatcherTimer.Start();
                }
                _notifyIcon.Visible = true;
                ShowWindow(new WindowInteropHelper(this).Handle, App.SW_SHOW);
            }
            finally
            {
                await Task.Delay(1500);
                waitWindow.Close();
                Mouse.SetCursor(System.Windows.Input.Cursors.Arrow);
            }
        }

        public void HideWindow()
        {
            if (_dispatcherTimer.IsEnabled)
            {
                _dispatcherTimer.Stop();
            }
            _notifyIcon.Visible = false;
            ShowWindow(new WindowInteropHelper(this).Handle, App.SW_HIDE);
        }
    }
}