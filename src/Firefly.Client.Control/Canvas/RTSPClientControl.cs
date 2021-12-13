// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Collections.Generic;
using System.Windows.Media.Media3D;
using log4net;

namespace Firefly.Client.Control
{
    public enum ShapeCreatorType
    {
        None,
        Spot,
        Rectangle,
        Ellipse,
        Polyline,
        Polygon,
        Mask,
    }

    public class RTSPClientControl : System.Windows.Controls.Control, IRTSPClientCallback, IDisposable
    {
        #region Fields

        private RTSPClient _rtspClient;

        private ImageCanvas _ticImageCanvas;
        private ThermalMarkerCanvas _tmcMarkerCanvas;

        private bool _isInit;
        private bool _isPaused;
        private bool _isRTSPUrlChanged;

        private Rect _clientArea;
        private Rect _thermalArea;

        private ShapeCreatorType _shapeCreatorTypeCache;
        private bool _isShapeCreating;
        private bool _isShapeCreatingCancelled;
        private string _shapeName;
        private List<Point> _shapePoints;

        private bool _isThermalMouseOver;
        private bool _isThermalMouseCaptured;
        private Point _lastMouseOverPoint;
        private Cursor _lastCursor;

        public const int MIN_WIDTH = 4;
        public const int MIN_HEIGHT = 4;

        private const string SPOT_PREFIX = "Sp";
        private const string AREA_PREFIX = "Ar";
        private const string POLYLINE_PREFIX = "Li";
        private const string MASK_PREFIX = "Sh";

        private ILog _logger = LogManager.GetLogger(typeof(RTSPClientControl));
        #endregion Fields

        #region Constructors

        static RTSPClientControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(RTSPClientControl), new FrameworkPropertyMetadata(typeof(RTSPClientControl)));
        }

        public RTSPClientControl()
        {
            _isInit = false;
            _isPaused = true;
            _isRTSPUrlChanged = false;
            IsShapeCreating = false;
            ShapeName = string.Empty;
            ShapePoints = new List<Point>();
            this.SizeChanged += RTSPClientCanvas_SizeChanged;
        }

        #endregion Constructors

        #region Events

        public event RoutedEventHandler MarkerAdded
        {
            add { AddHandler(MarkerAddedEvent, value); }
            remove { RemoveHandler(MarkerAddedEvent, value); }
        }

        public static readonly RoutedEvent MarkerAddedEvent = EventManager.RegisterRoutedEvent("MarkerAdded", RoutingStrategy.Direct, typeof(RoutedEventHandler), typeof(RTSPClientControl));

        public event RoutedEventHandler MarkerChanged
        {
            add { AddHandler(MarkerChangedEvent, value); }
            remove { RemoveHandler(MarkerChangedEvent, value); }
        }

        public static readonly RoutedEvent MarkerChangedEvent = EventManager.RegisterRoutedEvent("MarkerChanged", RoutingStrategy.Direct, typeof(RoutedEventHandler), typeof(RTSPClientControl));

        public event RoutedEventHandler SelectChanged
        {
            add { AddHandler(SelectChangedEvent, value); }
            remove { RemoveHandler(SelectChangedEvent, value); }
        }

        public static readonly RoutedEvent SelectChangedEvent = EventManager.RegisterRoutedEvent("SelectChangedEvent", RoutingStrategy.Direct, typeof(RoutedEventHandler), typeof(RTSPClientControl));

        public event RoutedEventHandler RecordChanged
        {
            add { AddHandler(RecordChangedEvent, value); }
            remove { RemoveHandler(RecordChangedEvent, value); }
        }

        public static readonly RoutedEvent RecordChangedEvent = EventManager.RegisterRoutedEvent("RecordChangedEvent", RoutingStrategy.Direct, typeof(RoutedEventHandler), typeof(RTSPClientControl));

        #endregion Events

        #region Properties

        public string NextSpotName
        {
            get { return string.Format("{0}{1}", SPOT_PREFIX, GetMarkerNextIndex(SPOT_PREFIX)); }
        }

        public string NextAreaName
        {
            get { return string.Format("{0}{1}", AREA_PREFIX, GetMarkerNextIndex(AREA_PREFIX)); }
        }

        public string NextPolylineName
        {
            get { return string.Format("{0}{1}", POLYLINE_PREFIX, GetMarkerNextIndex(POLYLINE_PREFIX)); }
        }

        public string NextShieldMaskName
        {
            get { return string.Format("{0}{1}", MASK_PREFIX, GetMaskNextIndex(MASK_PREFIX)); }
        }

        private Rect ClientArea
        {
            get { return _clientArea; }
            set
            {
                if (_clientArea != value)
                {
                    _clientArea = value;
                }

                if (_ticImageCanvas != null)
                {
                    _ticImageCanvas.ClientArea = _clientArea;
                }

                if (_tmcMarkerCanvas != null)
                {
                    _tmcMarkerCanvas.ClientArea = _clientArea;
                }
            }
        }

        private Rect ThermalArea
        {
            get { return _thermalArea; }
            set
            {
                if (_thermalArea != value)
                {
                    _thermalArea = value;
                }

                if (_tmcMarkerCanvas != null)
                {
                    _tmcMarkerCanvas.ThermalArea = _thermalArea;
                }
            }
        }

        internal bool IsShapeCreating
        {
            get { return _isShapeCreating; }
            set
            {
                if (_isShapeCreating != value)
                {
                    _isShapeCreating = value;
                    InvalidateVisual();
                }
            }
        }

        internal bool IsShapeCreatingCancelled
        {
            get { return _isShapeCreatingCancelled; }
            set { _isShapeCreatingCancelled = value; }
        }

        internal string ShapeName
        {
            get { return _shapeName; }
            set { _shapeName = value; }
        }

        internal List<Point> ShapePoints
        {
            get { return _shapePoints; }
            set
            {
                if (_shapePoints != value)
                {
                    _shapePoints = value;
                    InvalidateVisual();
                }
            }
        }

        internal bool IsThermalMouseOver
        {
            get { return _isThermalMouseOver; }
            set { _isThermalMouseOver = value; }
        }

        internal bool IsThermalMouseCaptured
        {
            get { return _isThermalMouseCaptured; }
            set { _isThermalMouseCaptured = value; }
        }

        internal Point LastMouseOverPoint
        {
            get { return _lastMouseOverPoint; }
            set
            {
                if (_lastMouseOverPoint != value)
                {
                    _lastMouseOverPoint = value;
                    if (_tmcMarkerCanvas != null)
                    {
                        _tmcMarkerCanvas.InvalidateVisual();
                    }
                }
            }
        }

        internal Cursor LastCursor
        {
            get { return _lastCursor; }
            set { _lastCursor = value; }
        }

        public List<DeviceMarker> MarkerList
        {
            get => (List<DeviceMarker>)GetValue(MarkerListProperty);
            set { SetValue(MarkerListProperty, value); }
        }

        public static readonly DependencyProperty MarkerListProperty = DependencyProperty.Register("MarkerList", typeof(List<DeviceMarker>), typeof(RTSPClientControl),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, new PropertyChangedCallback(MarkerListChanged)));

        public List<DeviceMarker> MaskList
        {
            get => (List<DeviceMarker>)GetValue(MaskListProperty);
            set { SetValue(MaskListProperty, value); }
        }

        public static readonly DependencyProperty MaskListProperty = DependencyProperty.Register("MaskList", typeof(List<DeviceMarker>), typeof(RTSPClientControl),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, new PropertyChangedCallback(MaskListChanged)));

        public DeviceMarker SelectedDeviceMarker
        {
            get => (DeviceMarker)GetValue(SelectedDeviceMarkerProperty);
            set { SetValue(SelectedDeviceMarkerProperty, value); }
        }

        public static readonly DependencyProperty SelectedDeviceMarkerProperty = DependencyProperty.Register("DeviceMarker", typeof(DeviceMarker), typeof(RTSPClientControl),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, new PropertyChangedCallback(SelectedDeviceMarkerChanged)));

        public ShapeCreatorType ShapeCreatorType
        {
            get { return _shapeCreatorTypeCache; }
            set { SetValue(ShapeCreatorTypeProperty, value); }
        }

        public static readonly DependencyProperty ShapeCreatorTypeProperty = DependencyProperty.Register("ShapeCreatorType", typeof(ShapeCreatorType), typeof(RTSPClientControl),
            new FrameworkPropertyMetadata(ShapeCreatorType.None, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnShapeCreatorTypeChanged));

        public string RTSPUrl
        {
            get { return GetValue(RTSPUrlProperty)?.ToString() ?? string.Empty; }
            set { SetValue(RTSPUrlProperty, value); }
        }

        public static readonly DependencyProperty RTSPUrlProperty = DependencyProperty.Register("RTSPUrl", typeof(string), typeof(RTSPClientControl),
            new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnRTSPUrlChanged));

        public Size DeviceArea
        {
            get { return (Size)GetValue(DeviceAreaProperty); }
            set { SetValue(DeviceAreaProperty, value); }
        }

        public static readonly DependencyProperty DeviceAreaProperty = DependencyProperty.Register("DeviceArea", typeof(Size), typeof(RTSPClientControl),
            new FrameworkPropertyMetadata(new Size(384, 288), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public bool ConnectStatus
        {
            get { return (bool)GetValue(ConnectStatusProperty); }
            set { SetValue(ConnectStatusProperty, value); }
        }

        public static readonly DependencyProperty ConnectStatusProperty = DependencyProperty.Register(
            nameof(ConnectStatus), typeof(bool), typeof(RTSPClientControl), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public bool RecordStatus
        {
            get { return (bool)GetValue(RecordStatusProperty); }
            set { SetValue(RecordStatusProperty, value); }
        }

        public static readonly DependencyProperty RecordStatusProperty = DependencyProperty.Register(
            nameof(RecordStatus), typeof(bool), typeof(RTSPClientControl), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public bool IsPaused
        {
            get { return (bool)GetValue(IsPausedProperty); }
            set { SetValue(IsPausedProperty, value); }
        }

        public static readonly DependencyProperty IsPausedProperty = DependencyProperty.Register(
            nameof(IsPaused), typeof(bool), typeof(RTSPClientControl), new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnIsPausedChanged));

        #endregion Properties

        #region Methods

        private static void MarkerListChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            RTSPClientControl rtspClientControl = (RTSPClientControl)d;
            List<DeviceMarker> markerList = e.NewValue as List<DeviceMarker>;
            if (rtspClientControl != null && rtspClientControl._rtspClient != null && rtspClientControl.ConnectStatus)
            {
                if (markerList == null)
                {
                    markerList = new List<DeviceMarker>();
                }

                rtspClientControl._tmcMarkerCanvas.InitMarker(markerList);
            }
        }

        private static void MaskListChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            RTSPClientControl rtspClientControl = (RTSPClientControl)d;
            List<DeviceMarker> maskList = e.NewValue as List<DeviceMarker>;
            if (rtspClientControl != null && rtspClientControl._rtspClient != null && rtspClientControl.ConnectStatus)
            {
                rtspClientControl._tmcMarkerCanvas.InitShieldMask(maskList);
            }
        }

        private static void SelectedDeviceMarkerChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            RTSPClientControl rtspClientControl = (RTSPClientControl)d;
            DeviceMarker selectedDeviceMarker = e.NewValue as DeviceMarker;
            if (rtspClientControl._rtspClient != null && rtspClientControl.ConnectStatus)
            {
                rtspClientControl.SelectMarker(rtspClientControl._tmcMarkerCanvas.GetMarkerIndex(selectedDeviceMarker.Name, selectedDeviceMarker.CanvasType), false);
            }
        }

        internal void SelectMarker(int index, bool isOuter = true)
        {
            if (index != -1)
            {
                UnSelectAllMarkers();
                if (!_tmcMarkerCanvas.MarkerList[index].IsSelected)
                {
                    _tmcMarkerCanvas.MarkerList[index].IsSelected = true;
                    _tmcMarkerCanvas.InvalidateVisual();

                    if (isOuter)
                    {
                        DeviceMarker deviceMarker = null;
                        switch (_tmcMarkerCanvas.MarkerList[index].Type)
                        {
                            case ThermalMarkerType.Spot:
                            case ThermalMarkerType.Polyline:
                            case ThermalMarkerType.Polygon:
                                deviceMarker = MarkerList.Where<DeviceMarker>(x => x.Name == _tmcMarkerCanvas.MarkerList[index].Name).FirstOrDefault();
                                break;

                            case ThermalMarkerType.Mask:
                                deviceMarker = MaskList.Where<DeviceMarker>(x => x.Name == _tmcMarkerCanvas.MarkerList[index].Name).FirstOrDefault();
                                break;
                        }
                        OnSelectedMarkerCallback(deviceMarker);
                    }
                }
            }
        }

        internal void UnSelectAllMarkers()
        {
            if (_tmcMarkerCanvas.MarkerList != null)
            {
                for (int i = 0; i < _tmcMarkerCanvas.MarkerList.Count; i++)
                {
                    _tmcMarkerCanvas.MarkerList[i].IsSelected = false;
                }
                _tmcMarkerCanvas.InvalidateVisual();
            }
        }

        internal void Login()
        {
            if (!_isPaused && _isInit)
            {
                if (_isRTSPUrlChanged)
                {
                    if (_rtspClient != null)
                    {
                        Logout();
                    }
                }
                else
                {
                    if (_rtspClient != null)
                    {
                        return;
                    }
                }

                if (!string.IsNullOrEmpty(RTSPUrl))
                {
                    _rtspClient = new RTSPClient();

                    RefreshClientArea(new Size(ActualWidth, ActualHeight));
                    RefreshDeviceMarker();

                    _rtspClient.Register(RTSPUrl, this);
                    _ticImageCanvas.Start();
                }
            }
        }

        internal void Logout()
        {
            try
            {
                Mouse.SetCursor(Cursors.Wait);

                // 仅在控件未播放视频时使用如下判断
                if (_rtspClient != null)
                {
                    _rtspClient.Destroy();
                    _rtspClient = null;
                }

                _ticImageCanvas.Stop();

                if (_tmcMarkerCanvas != null)
                {
                    _tmcMarkerCanvas.InitMarker(new List<DeviceMarker>());
                    _tmcMarkerCanvas.InitShieldMask(new List<DeviceMarker>());
                }
            }
            finally
            {
                Mouse.SetCursor(Cursors.Arrow);
            }
        }

        public void OnConnectChanged(bool isConnected)
        {
            _logger.Info($"Connect Changed {isConnected}");

            this.Dispatcher.BeginInvoke(new Action(() =>
            {
                this.SetCurrentValue(ConnectStatusProperty, isConnected);
            }));
        }

        public void OnRecordStatusChanged(bool record)
        {
            this.Dispatcher.BeginInvoke(new Action(() =>
            {
                this.SetCurrentValue(RecordStatusProperty, record);
            }));
        }

        public void OnRecordFinished(string recordPath)
        {
            this.Dispatcher.BeginInvoke(new Action(() =>
            {
                this.SetCurrentValue(RecordStatusProperty, false);

                if (!string.IsNullOrWhiteSpace(recordPath))
                {
                    RoutedEventArgs routedEventArgs = new RoutedEventArgs(RecordChangedEvent, recordPath);
                    RaiseEvent(routedEventArgs);
                }
            }));
        }

        public void OnFrameChanged(IntPtr data, bool isKeyFrame, int width, int height)
        {
            _ticImageCanvas?.AppendVideoFrame(data, isKeyFrame, width, height);
        }

        private static void OnShapeCreatorTypeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            RTSPClientControl rtspClientControl = (RTSPClientControl)d;

            rtspClientControl.OnShapeCreatorTypeChanged((ShapeCreatorType)e.OldValue, (ShapeCreatorType)e.NewValue);
        }

        private static void OnIsPausedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            RTSPClientControl rtspClientControl = (RTSPClientControl)d;

            if (e.NewValue != e.OldValue)
            {
                rtspClientControl._isPaused = (bool)e.NewValue;
            }

            if (!rtspClientControl._isPaused)
            {
                rtspClientControl.Login();
            }
            else
            {
                rtspClientControl.Logout();
            }
        }

        private static void OnRTSPUrlChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            RTSPClientControl rtspClientControl = (RTSPClientControl)d;

            if (e.NewValue is string rtspURL && !string.IsNullOrEmpty(rtspURL) && e.NewValue != e.OldValue)
            {
                rtspClientControl._isRTSPUrlChanged = true;
            }
            else
            {
                rtspClientControl._isRTSPUrlChanged = false;
            }
        }

        internal void OnShapeCreatorTypeChanged(ShapeCreatorType oldShapeCreatorType, ShapeCreatorType newShapeCreatorType)
        {
            if (IsShapeCreating && !IsShapeCreatingCancelled)
            {
                MarkerType markerType = MarkerType.Spot;
                List<Point> pointList = new List<Point>();
                DeviceMarker deviceMarker = null;
                switch (ShapeCreatorType)
                {
                    case ShapeCreatorType.Spot:
                        markerType = MarkerType.Spot;
                        Point destPoint = ThermalCommon.RelToAbsPoint(ShapePoints[0], ThermalArea);
                        pointList.Add(destPoint);
                        deviceMarker = new DeviceMarker(markerType, CanvasType.Marker, pointList, ShapeName);
                        OnAddMarkerCallback(deviceMarker);
                        break;

                    case ShapeCreatorType.Polyline:
                        if (ShapePoints.Count >= 2)
                        {
                            markerType = MarkerType.Line;
                            Point[] destPoints = ThermalCommon.RelToAbsPoints(ShapePoints.ToArray(), ThermalArea);
                            pointList = destPoints.ToList();
                            deviceMarker = new DeviceMarker(markerType, CanvasType.Marker, pointList, ShapeName);
                            OnAddMarkerCallback(deviceMarker);
                        }
                        break;

                    case ShapeCreatorType.Polygon:
                        if (ShapePoints.Count >= 3)
                        {
                            markerType = MarkerType.Region;
                            Point[] destPoints = ThermalCommon.RelToAbsPoints(ShapePoints.ToArray(), ThermalArea);
                            pointList = destPoints.ToList();
                            deviceMarker = new DeviceMarker(markerType, CanvasType.Marker, pointList, ShapeName);
                            OnAddMarkerCallback(deviceMarker);
                        }
                        break;

                    case ShapeCreatorType.Mask:
                        if (ShapePoints.Count >= 3)
                        {
                            markerType = MarkerType.Region;
                            Point[] destPoints = ThermalCommon.RelToAbsPoints(ShapePoints.ToArray(), ThermalArea);
                            pointList = destPoints.ToList();
                            deviceMarker = new DeviceMarker(markerType, CanvasType.Shield, pointList, ShapeName);
                            OnAddMarkerCallback(deviceMarker);
                        }
                        break;
                }
            }

            _shapeCreatorTypeCache = newShapeCreatorType;

            IsShapeCreating = false;
            ShapeName = string.Empty;
            ShapePoints.Clear();
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _ticImageCanvas = GetTemplateChild("imageCanvas") as ImageCanvas;
            _tmcMarkerCanvas = GetTemplateChild("tmcMarkerCanvas") as ThermalMarkerCanvas;

            _tmcMarkerCanvas.Owner = this;

            ClientArea = new Rect(0.0, 0.0, Width, Height);

            IsThermalMouseOver = false;
            IsThermalMouseCaptured = false;
            LastMouseOverPoint = new Point(0.0, 0.0);
            LastCursor = Cursors.Arrow;
            RefreshArea();
            _isInit = true;
            Login();
        }

        private int GetMarkerNextIndex(string prefix)
        {
            int index;
            SortedDictionary<int, int> idMap = new SortedDictionary<int, int>();
            for (int i = 0; i < MarkerList.Count; i++)
            {
                string name = MarkerList[i].Name;
                if (name.StartsWith(prefix))
                {
                    if (int.TryParse(name.Substring(prefix.Length), out index))
                    {
                        idMap.Add(index, index);
                    }
                }
            }

            index = 1;
            foreach (KeyValuePair<int, int> itr in idMap)
            {
                if (itr.Key != index)
                {
                    break;
                }
                index++;
            }
            return index;
        }

        private int GetMaskNextIndex(string prefix)
        {
            int index;
            SortedDictionary<int, int> idMap = new SortedDictionary<int, int>();
            for (int i = 0; i < MaskList.Count; i++)
            {
                string name = MaskList[i].Name;
                if (name.StartsWith(prefix))
                {
                    if (int.TryParse(name.Substring(prefix.Length), out index))
                    {
                        idMap.Add(index, index);
                    }
                }
            }

            index = 1;
            foreach (KeyValuePair<int, int> itr in idMap)
            {
                if (itr.Key != index)
                {
                    break;
                }
                index++;
            }
            return index;
        }

        private void RTSPClientCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            RefreshArea();
        }

        private void RefreshArea()
        {
            ThermalArea = new Rect(0, 0, DeviceArea.Width, DeviceArea.Height);
            if (_rtspClient != null)
            {
                RefreshClientArea(new Size(ActualWidth, ActualHeight));
                RefreshDeviceMarker();
            }
        }

        private void RefreshDeviceMarker()
        {
            _tmcMarkerCanvas?.InitMarker(MarkerList);
            _tmcMarkerCanvas?.InitShieldMask(MaskList);
        }

        private void RefreshClientArea(Size newSize)
        {
            System.Drawing.Size size = new System.Drawing.Size((int)DeviceArea.Width, (int)DeviceArea.Height);
            if (size.Width <= 0 || size.Height <= 0)
            {
                ClientArea = new Rect(0, 0, newSize.Width, newSize.Height);
                return;
            }

            if (UIConstants.RenderMode == ImageRenderMode.Fit)
            {
                if (size.Width * newSize.Height > size.Height * newSize.Width)
                {
                    double x = 0.0;
                    double width = newSize.Width;
                    double height = width * size.Height / size.Width;
                    double y = (newSize.Height - height) / 2;
                    ClientArea = new Rect(x, y, width, height);
                }
                else
                {
                    double y = 0.0;
                    double height = newSize.Height;
                    double width = height * size.Width / size.Height;
                    double x = (newSize.Width - width) / 2;
                    ClientArea = new Rect(x, y, width, height);
                }
            }
            else if (UIConstants.RenderMode == ImageRenderMode.Fill)
            {
                ClientArea = new Rect(0, 0, newSize.Width, newSize.Height);
            }
        }

        protected override void OnMouseEnter(MouseEventArgs e)
        {
            base.OnMouseEnter(e);

            Point point = e.GetPosition(this);
            if (ClientArea.Contains(point))
            {
                OnThermalMouseEnter();
            }
        }

        internal void OnThermalMouseEnter()
        {
            IsThermalMouseOver = true;

            Cursor = LastCursor;
        }

        protected override void OnMouseLeave(MouseEventArgs e)
        {
            base.OnMouseLeave(e);

            OnThermalMouseLeave();
        }

        internal void OnThermalMouseLeave()
        {
            IsThermalMouseOver = false;

            Cursor = Cursors.Arrow;
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            CaptureMouse();
            base.OnMouseDown(e);

            if (_rtspClient == null || !ConnectStatus)
            {
                return;
            }

            if (IsThermalMouseOver)
            {
                OnThermalMouseDown(e);
            }
        }

        internal void OnThermalMouseDown(MouseButtonEventArgs e)
        {
            Point point = e.GetPosition(this);

            if (e.ChangedButton != MouseButton.Left)
            {
                if (e.ChangedButton == MouseButton.Right)
                {
                    IsShapeCreatingCancelled = true;
                    ShapeCreatorType = ShapeCreatorType.None;
                    IsThermalMouseCaptured = false;

                    int index = _tmcMarkerCanvas.GetMarkerIndexAt(point);
                    if (index != -1)
                    {
                        SelectMarker(index);
                    }

                    if (index == -1)
                    {
                        UnSelectAllMarkers();
                        OnSelectedMarkerCallback(null);
                    }

                    OnMouseMove(e);
                }
                return;
            }

            if (IsThermalMouseCaptured)
            {
                if (IsShapeCreating)
                {
                    if (ClientArea.Contains(point))
                    {
                        switch (ShapeCreatorType)
                        {
                            case ShapeCreatorType.Polyline:
                            case ShapeCreatorType.Polygon:
                            case ShapeCreatorType.Mask:
                                Point lastPoint = ThermalCommon.RelToAbsPoint(ShapePoints[ShapePoints.Count - 1], ClientArea);
                                if (Math.Abs(lastPoint.X - point.X) > MIN_WIDTH ||
                                    Math.Abs(lastPoint.Y - point.Y) > MIN_HEIGHT)
                                {
                                    ShapePoints.Add(ThermalCommon.AbsToRelPoint(point, ClientArea));
                                    InvalidateVisual();
                                }

                                break;
                        }
                    }
                }
            }
            else
            {
                if (!IsShapeCreating)
                {
                    ThermalMarker marker = null;
                    if (_tmcMarkerCanvas.SelectedMarker != null)
                    {
                        int tmpIndex = 0;
                        marker = _tmcMarkerCanvas.SelectedMarker;
                        if (marker.TrySelectPivot(point, out tmpIndex))
                        {
                            switch (marker.Type)
                            {
                                case ThermalMarkerType.Polyline:
                                case ThermalMarkerType.Polygon:
                                case ThermalMarkerType.Mask:
                                    Cursor = IRtekCursors.SpotSizeAll;
                                    LastCursor = Cursor;
                                    marker.BeginMove(point, tmpIndex);
                                    IsThermalMouseCaptured = true;
                                    return;
                            }
                        }
                    }

                    int index = _tmcMarkerCanvas.GetMarkerIndexAt(point);
                    if (index != -1)
                    {
                        Cursor = System.Windows.Input.Cursors.SizeAll;
                        LastCursor = Cursor;
                        SelectMarker(index);
                        marker = _tmcMarkerCanvas.MarkerList[index];
                        marker.BeginMove(point, 0);
                        IsThermalMouseCaptured = true;
                        return;
                    }

                    if (ShapeCreatorType != ShapeCreatorType.None)
                    {
                        switch (ShapeCreatorType)
                        {
                            case ShapeCreatorType.Spot:
                                Cursor = IRtekCursors.Spot;
                                LastCursor = Cursor;
                                ShapeName = NextSpotName;
                                break;

                            case ShapeCreatorType.Polyline:
                                Cursor = IRtekCursors.Polyline;
                                LastCursor = Cursor;
                                ShapeName = NextPolylineName;
                                break;

                            case ShapeCreatorType.Polygon:
                                Cursor = IRtekCursors.Polygon;
                                LastCursor = Cursor;
                                ShapeName = NextAreaName;
                                break;

                            case ShapeCreatorType.Mask:
                                Cursor = IRtekCursors.Polygon;
                                LastCursor = Cursor;
                                ShapeName = NextShieldMaskName;
                                break;
                        }

                        ShapePoints.Add(ThermalCommon.AbsToRelPoint(point, ClientArea));
                        IsShapeCreating = true;
                        IsThermalMouseCaptured = true;
                        InvalidateVisual();
                        return;
                    }
                    else
                    {
                        Cursor = System.Windows.Input.Cursors.Arrow;
                        LastCursor = Cursor;
                    }

                    UnSelectAllMarkers();
                    OnSelectedMarkerCallback(null);
                }
                IsThermalMouseCaptured = true;
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            Point point = e.GetPosition(this);
            if (!IsMouseCaptured)
            {
                if (ClientArea.Contains(point))
                {
                    OnThermalMouseEnter();
                }
                else
                {
                    OnThermalMouseLeave();
                }
            }

            if (_rtspClient == null || !ConnectStatus)
            {
                return;
            }

            if (IsThermalMouseOver)
            {
                OnThermalMouseMove(e);
            }
        }

        internal void OnThermalMouseMove(MouseEventArgs e)
        {
            Point point = e.GetPosition(this);
            LastMouseOverPoint = point;

            if (IsThermalMouseCaptured)
            {
                if (!IsShapeCreating)
                {
                    if (_tmcMarkerCanvas.SelectedMarker != null)
                    {
                        ThermalMarker marker = _tmcMarkerCanvas.SelectedMarker;
                        marker.DoMove(point, ClientArea);
                        _tmcMarkerCanvas.InvalidateVisual();
                    }
                }
            }
            else
            {
                if (_tmcMarkerCanvas.SelectedMarker != null)
                {
                    int index = 0;
                    ThermalMarker marker = _tmcMarkerCanvas.SelectedMarker;
                    if (marker.TrySelectPivot(point, out index))
                    {
                        switch (marker.Type)
                        {
                            case ThermalMarkerType.Polyline:
                            case ThermalMarkerType.Polygon:
                            case ThermalMarkerType.Mask:
                                Cursor = IRtekCursors.SpotSizeAll;
                                LastCursor = Cursor;
                                _tmcMarkerCanvas.InvalidateVisual();
                                return;
                        }
                    }
                }

                if (_tmcMarkerCanvas.GetMarkerIndexAt(point) != -1)
                {
                    Cursor = Cursors.SizeAll;
                    LastCursor = Cursor;
                    _tmcMarkerCanvas.InvalidateVisual();
                    return;
                }

                switch (ShapeCreatorType)
                {
                    case ShapeCreatorType.None:
                    default:
                        Cursor = Cursors.Arrow;
                        LastCursor = Cursor;
                        break;

                    case ShapeCreatorType.Spot:
                        Cursor = IRtekCursors.Spot;
                        LastCursor = Cursor;
                        break;

                    case ShapeCreatorType.Polyline:
                        Cursor = IRtekCursors.Polyline;
                        LastCursor = Cursor;
                        break;

                    case ShapeCreatorType.Polygon:
                        Cursor = IRtekCursors.Polygon;
                        LastCursor = Cursor;
                        break;

                    case ShapeCreatorType.Mask:
                        Cursor = IRtekCursors.Polygon;
                        LastCursor = Cursor;
                        break;
                }
                _tmcMarkerCanvas.InvalidateVisual();
            }
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            try
            {
                base.OnMouseUp(e);

                if (_rtspClient == null || !ConnectStatus)
                {
                    return;
                }

                if (e.ChangedButton != MouseButton.Left)
                {
                    return;
                }

                if (IsThermalMouseOver)
                {
                    OnThermalMouseUp(e);
                }
            }
            finally
            {
                if (!IsThermalMouseCaptured)
                {
                    ReleaseMouseCapture();
                }
            }
        }

        internal void OnThermalMouseUp(MouseButtonEventArgs e)
        {
            if (IsThermalMouseCaptured)
            {
                if (IsShapeCreating)
                {
                    switch (ShapeCreatorType)
                    {
                        case ShapeCreatorType.Spot:
                            IsShapeCreatingCancelled = false;
                            ShapeCreatorType = ShapeCreatorType.None;
                            IsThermalMouseCaptured = false;
                            break;
                    }
                }
                else
                {
                    if (_tmcMarkerCanvas.SelectedMarker != null)
                    {
                        ThermalMarker marker = _tmcMarkerCanvas.SelectedMarker;
                        marker.EndMove();
                        MarkerType markerType = MarkerType.Spot;
                        CanvasType canvasType = CanvasType.Marker;
                        List<Point> pointList = new List<Point>();
                        switch (marker.Type)
                        {
                            case ThermalMarkerType.Spot:
                                markerType = MarkerType.Spot;
                                canvasType = CanvasType.Marker;
                                pointList.Add((marker as ThermalMarkerSpot).Point);
                                break;

                            case ThermalMarkerType.Polyline:
                                markerType = MarkerType.Line;
                                canvasType = CanvasType.Marker;
                                pointList = (marker as ThermalMarkerPolyline).Points.ToList();
                                break;

                            case ThermalMarkerType.Polygon:
                                markerType = MarkerType.Region;
                                canvasType = CanvasType.Marker;
                                pointList = (marker as ThermalMarkerPolygon).Points.ToList();
                                break;

                            case ThermalMarkerType.Mask:
                                markerType = MarkerType.Region;
                                canvasType = CanvasType.Shield;
                                pointList = (marker as ThermalMask).Points.ToList();
                                break;
                        }
                        Point[] destPoints = ThermalCommon.RelToAbsPoints(ThermalCommon.AbsToRelPoints(pointList.ToArray(), ClientArea), ThermalArea);
                        DeviceMarker deviceMarker = new DeviceMarker(markerType, canvasType, destPoints.ToList(), marker.Name);
                        OnMarkerChangedCallback(deviceMarker);
                    }
                    IsThermalMouseCaptured = false;
                }
            }
        }

        protected override void OnMouseDoubleClick(MouseButtonEventArgs e)
        {
            base.OnMouseDoubleClick(e);

            if (_rtspClient == null || !ConnectStatus)
            {
                return;
            }

            if (e.ChangedButton != MouseButton.Left)
            {
                return;
            }

            if (IsThermalMouseOver)
            {
                OnThermalMouseDoubleClick(e);
            }
        }

        internal void OnThermalMouseDoubleClick(MouseButtonEventArgs e)
        {
            if (IsThermalMouseCaptured)
            {
                if (IsShapeCreating)
                {
                    switch (ShapeCreatorType)
                    {
                        case ShapeCreatorType.Polyline:
                        case ShapeCreatorType.Polygon:
                        case ShapeCreatorType.Mask:
                            Point tmpPoint = e.GetPosition(this);
                            Point point = ThermalCommon.GetNearestPointInArea(tmpPoint, ClientArea);
                            Point lastPoint = ThermalCommon.RelToAbsPoint(ShapePoints[ShapePoints.Count - 1], ClientArea);
                            if (Math.Abs(lastPoint.X - point.X) > MIN_WIDTH ||
                                Math.Abs(lastPoint.Y - point.Y) > MIN_HEIGHT)
                            {
                                ShapePoints.Add(ThermalCommon.AbsToRelPoint(point, ClientArea));
                            }
                            IsShapeCreatingCancelled = false;
                            ShapeCreatorType = ShapeCreatorType.None;
                            IsThermalMouseCaptured = false;
                            break;
                    }
                }
            }
        }

        internal void OnAddMarkerCallback(DeviceMarker marker)
        {
            RoutedEventArgs routedEventArgs = new RoutedEventArgs(MarkerAddedEvent, marker);
            RaiseEvent(routedEventArgs);
        }

        internal void OnMarkerChangedCallback(DeviceMarker marker)
        {
            RoutedEventArgs routedEventArgs = new RoutedEventArgs(MarkerChangedEvent, marker);
            RaiseEvent(routedEventArgs);
        }

        internal void OnSelectedMarkerCallback(DeviceMarker marker)
        {
            RoutedEventArgs routedEventArgs = new RoutedEventArgs(SelectChangedEvent, marker);
            RaiseEvent(routedEventArgs);
        }

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);

            try
            {
                Geometry clipGeometry = new RectangleGeometry(ClientArea);
                dc.PushClip(clipGeometry);

                _ticImageCanvas.InvalidateVisual();
            }
            finally
            {
                dc.Pop();
            }
        }

        public void TakeSnapshot(string filePath)
        {
            _ticImageCanvas.SaveBitmap(filePath);
        }

        public void StartRecord(string videoPath)
        {
            _rtspClient.StartRecord(videoPath);
        }

        public void StopRecord()
        {
            _rtspClient.StopRecord();
        }

        public void Dispose()
        {
            Logout();
        }

        #endregion Methods
    }
}