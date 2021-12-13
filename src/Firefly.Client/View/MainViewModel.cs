// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Linq;
using System.Windows.Data;
using System.ComponentModel;
using System.Windows.Media.Imaging;
using System.Collections.ObjectModel;

using Amur.Controls;
using Firefly.Client.Control;
using Firefly.Client.Service;

namespace Firefly.Client
{
    public class MainViewModel : BaseViewModel
    {
        #region Fields

        private ObservableCollection<PageViewModel> allPages;
        private ObservableCollection<PageViewModel> displayPages;
        private PageViewModel selectedPage;
        private PageViewModel alarmCenterPage;
        private DateTime _time;

        private string _iconPath;
        private int _softwareTitleFontSize = 16;

        #endregion Fields

        #region Constructors

        public MainViewModel()
        {
            _iconPath = "/MoniTiRLite;component/Resources/logo.ico";
            allPages = new ObservableCollection<PageViewModel>();
            ICollectionView collectionView = CollectionViewSource.GetDefaultView(allPages);
            collectionView.GroupDescriptions.Add(new PropertyGroupDescription("GroupName"));

            PageViewModel resourceConfigPage = new PageViewModel
            {
                Page = new ResourcePage(),
                CanClose = true,
                TitleIcon = new Control.SymbolIcon(Control.SymbolRegular.ResourceConfig, 18),
                Icon = new ImageIcon
                {
                    Source = new BitmapImage(new Uri(@"pack://application:,,,/MoniTiRLite;component/Resources/resource_config_main_icon.png"))
                },
                Name = I18n.Properties.Resources.ResourceConfig,
                Description = I18n.Properties.Resources.ResourceConfigDescription,
                GroupName = I18n.Properties.Resources.Management
            };
            allPages.Add(resourceConfigPage);

            PageViewModel systemConfigPage = new PageViewModel
            {
                Page = new SystemConfigPage(),
                CanClose = true,
                TitleIcon = new Control.SymbolIcon(Control.SymbolRegular.SystemConfig, 18),
                Icon = new ImageIcon
                {
                    Source = new BitmapImage(new Uri(@"pack://application:,,,/MoniTiRLite;component/Resources/system_config_main_icon.png"))
                },
                Name = I18n.Properties.Resources.SystemConfig,
                Description = I18n.Properties.Resources.SystemConfigDescription,
                GroupName = I18n.Properties.Resources.Management
            };
            allPages.Add(systemConfigPage);

            alarmCenterPage = new PageViewModel
            {
                Page = new AlarmPage(),
                CanClose = true,
                TitleIcon = new Control.SymbolIcon(Control.SymbolRegular.AlarmCenter, 18),
                Icon = new ImageIcon
                {
                    Source = new BitmapImage(new Uri(@"pack://application:,,,/MoniTiRLite;component/Resources/alarm_center_main_icon.png"))
                },
                Name = I18n.Properties.Resources.AlarmCenter,
                Description = I18n.Properties.Resources.AlarmCenterDescription,
                GroupName = I18n.Properties.Resources.FunctinalCenter
            };
            allPages.Add(alarmCenterPage);

            PageViewModel dataCenterPage = new PageViewModel
            {
                Page = new DataCenterPage(),
                CanClose = true,
                TitleIcon = new Control.SymbolIcon(Control.SymbolRegular.DataCenter, 18),
                Icon = new ImageIcon
                {
                    Source = new BitmapImage(new Uri(@"pack://application:,,,/MoniTiRLite;component/Resources/data_center_main_icon.png"))
                },
                Name = I18n.Properties.Resources.DataCenter,
                Description = I18n.Properties.Resources.DataCenterDescription,
                GroupName = I18n.Properties.Resources.FunctinalCenter
            };
            allPages.Add(dataCenterPage);

            PageViewModel livePreviewPage = new PageViewModel
            {
                Page = new LivePreviewPage(),
                CanClose = true,
                TitleIcon = new Control.SymbolIcon(Control.SymbolRegular.LivePreview, 18),
                Icon = new ImageIcon
                {
                    Source = new BitmapImage(new Uri(@"pack://application:,,,/MoniTiRLite;component/Resources/live_preview_main_icon.png"))
                },
                Name = I18n.Properties.Resources.LivePreview,
                Description = I18n.Properties.Resources.LivePreviewDescription,
                GroupName = I18n.Properties.Resources.FunctinalCenter
            };
            allPages.Add(livePreviewPage);

            PageViewModel statusMonitorPage = new PageViewModel
            {
                Page = new StatusMonitorPage(),
                CanClose = true,
                TitleIcon = new Control.SymbolIcon(Control.SymbolRegular.StatusMonitor, 18),
                Icon = new ImageIcon
                {
                    Source = new BitmapImage(new Uri(@"pack://application:,,,/MoniTiRLite;component/Resources/status_monitor_main_icon.png"))
                },
                Name = I18n.Properties.Resources.StatusMonitor,
                Description = I18n.Properties.Resources.StatusMonitorDescription,
                GroupName = I18n.Properties.Resources.IntelligentApplications
            };
            allPages.Add(statusMonitorPage);

            displayPages = new ObservableCollection<PageViewModel>();

            PageViewModel mainPage = new PageViewModel
            {
                Page = new MainPage(this),
                CanClose = false,
                TitleIcon = new Control.SymbolIcon(Control.SymbolRegular.Home, 18),
                Name = ""
            };
            displayPages.Add(mainPage);

            selectedPage = displayPages[0];

            _time = DateTime.Now;
        }

        internal void TryAddToDisplayPages(PageViewModel pageView)
        {
            bool canAdd = true;

            for (int i = 0; i < DisplayPages.Count; i++)
            {
                if (DisplayPages[i] == pageView)
                {
                    canAdd = false;
                    break;
                }
            }

            if (canAdd)
            {
                DisplayPages.Add(pageView);
                OnPropertyChanged(nameof(NoContainAlarmPage));
            }
        }

        internal void TryRemoveFromDisplayPages(PageViewModel pageView)
        {
            if (DisplayPages.Contains(pageView))
            {
                DisplayPages.Remove(pageView);
                pageView.Page?.OnClosed();
                OnPropertyChanged(nameof(NoContainAlarmPage));
            }
        }

        internal void CleanAllPage()
        {
            while (DisplayPages.Count > 1)
            {
                var page = DisplayPages[1];
                DisplayPages.Remove(page);
                page.Page?.OnClosed();
                OnPropertyChanged(nameof(NoContainAlarmPage));
            }
        }

        #endregion Constructors

        #region Properties

        public DateTime Time
        {
            get => _time;
            set
            {
                _time = value;
                OnPropertyChanged();
            }
        }

        public PageViewModel Main { get => DisplayPages[0]; }

        public ObservableCollection<PageViewModel> AllPages
        { get => allPages; set { allPages = value; } }

        public ObservableCollection<PageViewModel> DisplayPages
        { get => displayPages; set { displayPages = value; } }

        public PageViewModel SelectedItem
        { get => selectedPage; internal set { selectedPage = value; OnPropertyChanged(nameof(SelectedItem)); } }

        public PageViewModel AlarmCenterPage
        { get => alarmCenterPage; internal set { alarmCenterPage = value; OnPropertyChanged(nameof(AlarmCenterPage)); } }

        public bool NoContainAlarmPage => !displayPages?.Any(it => it.Page is AlarmPage) ?? true;

        public PageViewModel AlarmPage => allPages?.FirstOrDefault(it => it.Page is AlarmPage);

        public string IconPath 
        {
            get => _iconPath;
            set
            {
                _iconPath = value;
                OnPropertyChanged();
            }
        }

        public int SoftwareTitleFontSize
        {
            get => _softwareTitleFontSize;
            set
            {
                _softwareTitleFontSize = value;
                OnPropertyChanged();
            }
        }
        #endregion Properties
    }

    public class PageViewModel : BaseViewModel
    {
        #region Fields

        private IPage _page;
        private bool _canClose;
        private IconElement _titleIcon;
        private IconElement _icon;
        private string _name;
        private string _description;
        private string _groupName;

        #endregion Fields

        #region Constructors

        public PageViewModel()
        {
            _page = null;
            _canClose = false;
            _titleIcon = null;
            _icon = null;
            _name = string.Empty;
            _groupName = string.Empty;
        }

        #endregion Constructors

        #region Properties

        public IPage Page
        { get => _page; set { _page = value; } }

        public bool CanClose
        { get => _canClose; set { _canClose = value; OnPropertyChanged(nameof(CanClose)); } }

        public IconElement TitleIcon
        { get => _titleIcon; set { _titleIcon = value; OnPropertyChanged(nameof(TitleIcon)); } }

        public IconElement Icon
        { get => _icon; set { _icon = value; OnPropertyChanged(nameof(Icon)); } }

        public string Name
        { get => _name; set { _name = value; OnPropertyChanged(nameof(Name)); } }

        public string Description
        { get => _description; set { _description = value; OnPropertyChanged(nameof(Description)); } }

        public string GroupName
        { get => _groupName; set { _groupName = value; OnPropertyChanged(); } }

        #endregion Properties
    }

    public enum BalloonTipType
    {
        Alarm,
        FileSave,
        FullPage,
    }

    public class BalloonTipArgs
    {
        public BalloonTipType Type { get; set; }
        public string Message { get; set; }
    }

    public class BalloonFileTipArgs : BalloonTipArgs
    {
        public int ChannelID { get; set; }

        public FileType FileType { get; set; }

        public string FilePath { get; set; }
    }
}