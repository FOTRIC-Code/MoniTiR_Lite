// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using Firefly.Client.Control;
using Firefly.Client.Service;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Path = System.IO.Path;

namespace Firefly.Client
{
    public partial class SplitVideoControl : UserControl
    {
        private Point _orginalPoint;
        private bool _enterDraw;
        private SplitVideoViewModel _splitVideoViewModel;

#pragma warning disable 0649

        [ImportMany(typeof(IDeviceConfigurer))]
        private IEnumerable<IDeviceConfigurer> _deviceConfigurers;

#pragma warning restore 0649

        public SplitVideoControl()
        {
            InitializeComponent();

            _splitVideoViewModel = new SplitVideoViewModel();
            DataContext = _splitVideoViewModel;

            using (AggregateCatalog catalog = new AggregateCatalog())
            {
                catalog.Catalogs.Add(new DirectoryCatalog(Environment.CurrentDirectory, "Firefly.Client.Control.*.dll"));
                using (CompositionContainer container = new CompositionContainer(catalog))
                {
                    container.ComposeParts(this);
                }
            }
        }

        #region Properties

        public SplitVideoItemSelectionChanged SelectedSelectionChanged { get; set; }

        #endregion Properties

        #region Public Methods

        public void OpenAddPollingMode()
        {
            StopPlayer();
            _splitVideoViewModel.IsAddPollingMode = true;
            foreach (var item in _splitVideoViewModel.DisplayItems)
            {
                item.IsAddPollingMode = true;
            }
        }

        public void CloseAddPollingMode()
        {
            StopPlayer();
            _splitVideoViewModel.IsAddPollingMode = false;
            foreach (var item in _splitVideoViewModel.DisplayItems)
            {
                item.IsAddPollingMode = false;
            }
        }

        public SplitModeViewModel ChangeSplitMode(int mode)
        {
            _splitVideoViewModel.SelectedMode = _splitVideoViewModel.SplitModeViewModelList.FirstOrDefault(it => it.Mode == mode);
            _splitVideoViewModel.UpdateView();
            LivePreviewPreference.Intance.SelectedMode = mode;
            return _splitVideoViewModel.SelectedMode;
        }

        public List<Tuple<int, int>> GetModeList()
        {
            return _splitVideoViewModel.SplitModeViewModelList.Select(it => new Tuple<int, int>(it.Mode, it.ItemCount)).ToList();
        }

        public void SplitVideoControl_Loaded()
        {
            _splitVideoViewModel.LoadPreference();
        }

        public void SplitVideoControl_Unloaded()
        {
            SavePreference();
        }

        public void UpdateDevice(List<DeviceDO> deviceDOList)
        {
            _splitVideoViewModel.UpdateDevice(deviceDOList);
        }

        public void ChangeDisplayDataByPolling(List<TreeItem> itemList)
        {
            _splitVideoViewModel.ReplaceItems(itemList);
        }

        public void ChangeSelectedDisplay(TreeItem item)
        {
            int index = _splitVideoViewModel.DisplayItems.IndexOf(_splitVideoViewModel.SelectedDisplayItem);

            _splitVideoViewModel.ChangeDisplayDataByDrop(index, new List<TreeItem> { item });
        }

        public void StopPlayer()
        {
            _splitVideoViewModel.StopPlay();
            _splitVideoViewModel.Release();
        }

        public override string ToString()
        {
            string str = "";
            if (_splitVideoViewModel?.DisplayItems == null)
            {
                return str;
            }

            if (_splitVideoViewModel.DisplayItems.Count == 0)
            {
                return str;
            }

            str += $"Total {_splitVideoViewModel.DisplayItems.Count()}, ";
            List<DisplayItemViewModel> offlineItemList = _splitVideoViewModel.DisplayItems.Where(it => !it.ConnectStatus).ToList();
            if (offlineItemList.Count > 0)
            {
                str += $"Offline {offlineItemList.Count} ";
                foreach (DisplayItemViewModel item in offlineItemList)
                {
                    str += $"[index (row:{item.Row}, column:{item.Column}, rowSpan:{item.RowSpan}, columnSpan:{item.ColumnSpan}) ";
                    if (!item.HasDevice)
                    {
                        str += "NoDevice] ";
                    }
                    else
                    {
                        str += $"{item.Description}] ";
                    }
                }
            }

            return str;
        }

        public List<LivePreviewCruiseItem> GetDisplayCruiseItems()
        {
            return _splitVideoViewModel.DisplayItems.Where(it => it.HasDevice).Select(it => new LivePreviewCruiseItem
            {
                DeviceName = it.DeviceDO.Name,
                DeviceID = it.DeviceDO.ID,
                ChannelID = it.ChannelID.ToString(),
                Order = _splitVideoViewModel.DisplayItems.IndexOf(it),
            }).ToList();
        }

        public void SetDisplayCruiseItems(List<LivePreviewCruiseItem> items)
        {
            _splitVideoViewModel.AddPollingDisplayData(items);
        }

        #endregion Public Methods

        #region Private Methods

        private void btnConfigDevice_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Cursor = Cursors.Wait;
                if (sender is FrameworkElement element && element.DataContext is DisplayItemViewModel itemViewModel)
                {
                    DeviceDO deviceDO = itemViewModel.DeviceDO;
                    if (deviceDO == null)
                    {
                        IRtekMessageBox.ShowDialog(Window.GetWindow(this), I18n.Properties.Resources.DeviceNotExist);
                    }
                    else
                    {
                        if (_deviceConfigurers != null)
                        {
                            foreach (IDeviceConfigurer deviceConfigurer in _deviceConfigurers)
                            {
                                if (deviceConfigurer.GetServerType().ToString() == deviceDO.DefaultAgent().Type)
                                {
                                    deviceConfigurer.Open(deviceDO, Window.GetWindow(this));
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            finally
            {
                Cursor = Cursors.Arrow;
            }
        }

        private void SavePreference()
        {
            if (LivePreviewPreference.Intance.DisplayItems == null)
            {
                LivePreviewPreference.Intance.DisplayItems = new List<LivePreviewDisplayItem>();
            }

            LivePreviewPreference.Intance.DisplayItems.Clear();
            for (int i = 0; i < _splitVideoViewModel.DisplayItems.Count; i++)
            {
                if (_splitVideoViewModel.DisplayItems[i].HasDevice)
                {
                    LivePreviewPreference.Intance.DisplayItems.Add(new LivePreviewDisplayItem
                    {
                        ChannelID = _splitVideoViewModel.DisplayItems[i].ChannelID,
                        DeviceID = _splitVideoViewModel.DisplayItems[i].DeviceDO?.ID,
                        Index = i,
                        DeviceType = _splitVideoViewModel.DisplayItems[i].DeviceType,
                    });
                }
            }
        }

        private void ListBoxItem_Drop(object sender, DragEventArgs e)
        {
            TreeItem dataContext = e.Data.GetData("DataContext") as TreeItem;
            List<TreeItem> deviceItemList = new List<TreeItem>();
            Queue<TreeItem> queue = new Queue<TreeItem>();
            queue.Enqueue(dataContext);
            while (queue.Count > 0)
            {
                TreeItem treeItem = queue.Dequeue();
                if (treeItem.ItemType == ItemType.Channel)
                {
                    deviceItemList.Add(treeItem);
                    continue;
                }

                if (treeItem.Children != null)
                {
                    foreach (TreeItem child in treeItem.Children)
                    {
                        queue.Enqueue(child);
                    }
                }
            }

            FrameworkElement element = sender as FrameworkElement;
            DisplayItemViewModel curDisplayItemVM = element.DataContext as DisplayItemViewModel;

            if (_splitVideoViewModel.IsAddPollingMode)
            {
                int index = _splitVideoViewModel.DisplayItems.IndexOf(curDisplayItemVM);

                _splitVideoViewModel.AddPollingDisplayDataByDrop(index, deviceItemList);
            }
            else if (_splitVideoViewModel != null)
            {
                int index = _splitVideoViewModel.DisplayItems.IndexOf(curDisplayItemVM);

                _splitVideoViewModel.ChangeDisplayDataByDrop(index, deviceItemList);
                SavePreference();
            }
        }

        private void btnCameraState_Click(object sender, RoutedEventArgs e)
        {
            if (_splitVideoViewModel != null)
            {
                if (((FrameworkElement)sender).DataContext is DisplayItemViewModel itemViewModel)
                {
                    if (_splitVideoViewModel.SelectedDisplayItem == itemViewModel)
                    {
                        _splitVideoViewModel.SelectedDisplayItem = null;
                    }
                    _splitVideoViewModel.StopPlay(_splitVideoViewModel.DisplayItems.IndexOf(itemViewModel));
                }
            }
        }

        private void btnPollingItemState_Click(object sender, RoutedEventArgs e)
        {
            if (((FrameworkElement)sender).DataContext is DisplayItemViewModel itemViewModel)
            {
                itemViewModel.DeviceDO = null;
                itemViewModel.ChannelID = 0;
                itemViewModel.Description = string.Empty;
            }
        }

        private async void btnSnapshot_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.Tag is RTSPClientControl)
            {
                DisplayItemViewModel itemViewModel = element.DataContext as DisplayItemViewModel;
                if (itemViewModel?.DeviceDO != null)
                {
                    string path = Path.Combine(Environment.CurrentDirectory, "Snapshot", itemViewModel.DeviceDO.Name);
                    if (!Directory.Exists(path))
                    {
                        Directory.CreateDirectory(path);
                    }
                    ImageInfo imageInfo = await DeviceManager.Instance.CaptureImage(itemViewModel.DeviceDO, itemViewModel.ChannelID);

                    // In-proc the engine writes the snapshot locally; only a remote agent serves it over HTTP.
                    bool captured = File.Exists(imageInfo.URL);
                    FileDataObject fileObject = captured ? null : await IRTekHttpClient.DownlodFileAsync(imageInfo.URL);
                    if (captured || fileObject.HasFile)
                    {
                        path = Path.Combine(path, $"{DateTime.Now:yyyyMMddHHmmss}.jpg");
                        if (captured)
                        {
                            File.Copy(imageInfo.URL, path, true);
                        }
                        else
                        {
                            fileObject.Save(path);
                        }
                        if (Window.GetWindow(this) is MainWindow window)
                        {
                            RoutedEventArgs routedEventArgs = new RoutedEventArgs
                            {
                                RoutedEvent = MainPage.NotifyBalloonTipEvent,
                                Source = new BalloonFileTipArgs
                                {
                                    Type = BalloonTipType.FileSave,
                                    Message = path,
                                    FilePath = path,
                                    FileType = imageInfo.FileType,
                                    ChannelID = imageInfo.ChannelID
                                }
                            };
                            window.RaiseEvent(routedEventArgs);
                        }
                    }
                }
            }
        }

        private void btnRecord_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.Tag is RTSPClientControl rtspClientControl)
            {
                DisplayItemViewModel itemViewModel = element.DataContext as DisplayItemViewModel;
                if (itemViewModel?.DeviceDO != null)
                {
                    if (itemViewModel.RecordStatus)
                    {
                        rtspClientControl.StopRecord();
                    }
                    else
                    {
                        string path = Path.Combine(Environment.CurrentDirectory, "Record", itemViewModel.DeviceDO.Name);
                        string fileName = Path.Combine(path, $"video_{DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss")}.mp4");
                        if (!Directory.Exists(path))
                        {
                            Directory.CreateDirectory(path);
                        }

                        rtspClientControl.StartRecord(fileName);
                        rtspClientControl.RecordChanged += RtspClientControl_RecordChanged;
                    }
                }
            }
        }

        private void btnHiddenBar_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.Tag is RTSPClientControl rtspClientControl)
            {
                DisplayItemViewModel itemViewModel = element.DataContext as DisplayItemViewModel;
                if (itemViewModel?.DeviceDO != null)
                {
                    itemViewModel.IsHiddenBar = !itemViewModel.IsHiddenBar;
                }
            }
        }

        private void RtspClientControl_RecordChanged(object sender, RoutedEventArgs e)
        {
            this.Dispatcher.BeginInvoke(new Action(() =>
            {
                if (Window.GetWindow(this) is MainWindow window)
                {
                    RoutedEventArgs routedEventArgs = new RoutedEventArgs
                    {
                        RoutedEvent = MainPage.NotifyBalloonTipEvent,
                        Source = new BalloonTipArgs
                        {
                            Type = BalloonTipType.FileSave,
                            Message = e.OriginalSource as string
                        }
                    };
                    window.RaiseEvent(routedEventArgs);
                }

                if (sender is RTSPClientControl rtspClientControl)
                {
                    rtspClientControl.RecordChanged -= RtspClientControl_RecordChanged;
                }
            }));
        }

        private void Grid_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2 && e.ChangedButton == System.Windows.Input.MouseButton.Left)
            {
                DisplayItemViewModel displayItemVM = (sender as Grid).DataContext as DisplayItemViewModel;

                // current view model is 1, never change it
                if (_splitVideoViewModel.IsSingleView == false && _splitVideoViewModel.SelectedMode.Mode == 1)
                {
                    // nothing to do
                }
                else if ((displayItemVM != null && displayItemVM.ConnectStatus) || _splitVideoViewModel.IsSingleView)
                {
                    if (_splitVideoViewModel.IsSingleView)
                    {
                        _splitVideoViewModel.SwitchToNormalView();
                    }
                    else
                    {
                        _splitVideoViewModel.SwitchToFullView();
                    }
                }
            }
        }

        private void RTSPClientControl_MouseDown(object sender, MouseButtonEventArgs e)
        {
            DisplayItemViewModel displayItemVM = (sender as Grid).DataContext as DisplayItemViewModel;
            if (displayItemVM == null)
            {
                return;
            }
            if (!displayItemVM.EnableDrawMask)
            {
                return;
            }
            ImageCanvas imageCanvas = GetChildObject<ImageCanvas>(sender as Grid, "imageCanvas");
            if (imageCanvas == null)
            {
                return;
            }
            _orginalPoint = e.GetPosition(imageCanvas);

            imageCanvas.CaptureMouse();
            _enterDraw = true;
            e.Handled = true;
        }

        private void RtspControl_OnMouseMove(object sender, MouseEventArgs e)
        {
            DisplayItemViewModel displayItemVM = (sender as Grid).DataContext as DisplayItemViewModel;

            if (displayItemVM == null)
            {
                return;
            }

            if (!displayItemVM.EnableDrawMask)
            {
                return;
            }
            if (!_enterDraw)
            {
                return;
            }
            Canvas maskCanvas = GetChildObject<Canvas>(sender as Grid, "cMask");
            if (maskCanvas == null)
            {
                return;
            }
            ImageCanvas imageCanvas = GetChildObject<ImageCanvas>(sender as Grid, "imageCanvas");
            if (imageCanvas == null)
            {
                return;
            }
            if (e.LeftButton != MouseButtonState.Pressed)
            {
                _enterDraw = false;
                imageCanvas.ReleaseMouseCapture();
                maskCanvas.Children.Clear();
                Point original = ThermalCommon.AbsToRelPoint(_orginalPoint, imageCanvas.ClientArea);
                Point point = ThermalCommon.AbsToRelPoint(e.GetPosition(imageCanvas), imageCanvas.ClientArea);
                EndDrawMarker(displayItemVM, point, original);
                return;
            }

            Point curPoint = e.GetPosition(maskCanvas);

            Rectangle tipRect = new Rectangle()
            {
                Fill = Brushes.White,
                Opacity = 0.3,
                Width = Math.Abs(_orginalPoint.X - curPoint.X),
                Height = Math.Abs(_orginalPoint.Y - curPoint.Y)
            };

            if (curPoint.X > _orginalPoint.X)
            {
                if (curPoint.Y > _orginalPoint.Y)
                {
                    Canvas.SetLeft(tipRect, _orginalPoint.X);
                    Canvas.SetTop(tipRect, _orginalPoint.Y);
                }
                else
                {
                    Canvas.SetLeft(tipRect, _orginalPoint.X);
                    Canvas.SetTop(tipRect, curPoint.Y);
                }
            }
            else
            {
                if (curPoint.Y > _orginalPoint.Y)
                {
                    Canvas.SetLeft(tipRect, curPoint.X);
                    Canvas.SetTop(tipRect, _orginalPoint.Y);
                }
                else
                {
                    Canvas.SetLeft(tipRect, curPoint.X);
                    Canvas.SetTop(tipRect, curPoint.Y);
                }
            }

            maskCanvas.Children.Clear();
            maskCanvas.Children.Add(tipRect);
        }

        private void Grid_MouseEnter(object sender, MouseEventArgs e)
        {
            DisplayItemViewModel displayItemVM = (sender as Grid).DataContext as DisplayItemViewModel;

            if (displayItemVM == null)
            {
                return;
            }

            displayItemVM.IsHiddenBar = false;
        }

        private void Grid_MouseLeave(object sender, MouseEventArgs e)
        {
            DisplayItemViewModel displayItemVM = (sender as Grid).DataContext as DisplayItemViewModel;

            if (displayItemVM == null)
            {
                return;
            }

            if (!displayItemVM.LockingBar)
            {
                displayItemVM.IsHiddenBar = true;
            }
        }

        private void RtspControl_OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            DisplayItemViewModel displayItemVM = (sender as Grid).DataContext as DisplayItemViewModel;
            if (displayItemVM == null)
            {
                return;
            }
            if (!displayItemVM.EnableDrawMask)
            {
                return;
            }

            Canvas maskCanvas = GetChildObject<Canvas>(sender as Grid, "cMask");
            if (maskCanvas != null)
            {
                maskCanvas.Children.Clear();
            }
            ImageCanvas imageCanvas = GetChildObject<ImageCanvas>(sender as Grid, "imageCanvas");
            if (imageCanvas == null)
            {
                return;
            }
            _enterDraw = false;
            imageCanvas.ReleaseMouseCapture();

            Point original = ThermalCommon.AbsToRelPoint(_orginalPoint, imageCanvas.ClientArea);
            Point point = ThermalCommon.AbsToRelPoint(e.GetPosition(imageCanvas), imageCanvas.ClientArea);
            EndDrawMarker(displayItemVM, point, original);
        }

        private async void EndDrawMarker(DisplayItemViewModel displayItemVM, Point point, Point original)
        {
            point = FilterPoint(point);
            original = FilterPoint(original);
            if (displayItemVM != null)
            {
                List<MarkerOSDParamDO> pointList = new List<MarkerOSDParamDO>();
                if (Math.Abs(point.X - original.X) > 0.0000001 || Math.Abs(point.Y - original.Y) > 0.0000001)
                {
                    pointList.Add(new MarkerOSDParamDO()
                    {
                        Type = (int)MarkerType.Region,
                        Duration = 5,
                        IsShow = true,
                        Name = "Ar",
                        Points = new List<PointOSDDO>()
                        {
                            new PointOSDDO()
                            {
                                Rpx = point.X,
                                Rpy = point.Y,
                            },
                            new PointOSDDO()
                            {
                                Rpx = point.X,
                                Rpy = original.Y,
                            },
                            new PointOSDDO()
                            {
                                Rpx = original.X,
                                Rpy = original.Y,
                            },
                            new PointOSDDO()
                            {
                                Rpx = original.X,
                                Rpy = point.Y,
                            },
                        }
                    });
                }
                else
                {
                    pointList.Add(new MarkerOSDParamDO()
                    {
                        Type = (int)MarkerType.Spot,
                        Duration = 5,
                        IsShow = true,
                        Name = "Sp",
                        Points = new List<PointOSDDO>()
                        {
                            new PointOSDDO()
                            {
                                Rpx = point.X,
                                Rpy = point.Y,
                            }
                        }
                    });
                }

                bool result = await DeviceManager.Instance.SetCustomerOSD(displayItemVM.DeviceDO, new OSDCustomerDO()
                {
                    MarkerList = pointList
                });
            }
        }

        private T GetChildObject<T>(DependencyObject obj, string name) where T : FrameworkElement
        {
            DependencyObject child = null;
            T grandChild = null;

            for (int i = 0; i <= VisualTreeHelper.GetChildrenCount(obj) - 1; i++)
            {
                child = VisualTreeHelper.GetChild(obj, i);

                if (child is T && (((T)child).Name == name | string.IsNullOrEmpty(name)))
                {
                    return (T)child;
                }
                else
                {
                    grandChild = GetChildObject<T>(child, name);
                    if (grandChild != null)
                        return grandChild;
                }
            }
            return null;
        }

        private Point FilterPoint(Point point)
        {
            if (point.X <= 0)
            {
                point.X = 0;
            }

            if (point.Y <= 0)
            {
                point.Y = 0;
            }

            if (point.X >= 1)
            {
                point.X = 1;
            }

            if (point.Y >= 1)
            {
                point.Y = 1;
            }

            return point;
        }

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                _splitVideoViewModel.SelectedDisplayItem = e.AddedItems[0] as DisplayItemViewModel;
                SelectedSelectionChanged?.Invoke(_splitVideoViewModel.SelectedDisplayItem);
            }
            else if (e.RemovedItems.Count > 0)
            {
                SelectedSelectionChanged?.Invoke(null);
            }
        }

        private void btnPin_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is DisplayItemViewModel itemViewModel)
            {
                itemViewModel.LockingBar = !itemViewModel.LockingBar;
            }
        }

        private void btnMark_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is DisplayItemViewModel itemViewModel)
            {
                itemViewModel.EnableDrawMask = !itemViewModel.EnableDrawMask;
            }
        }

        #endregion Private Methods
    }
}