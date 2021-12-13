// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Windows;
using System.Globalization;
using System.Windows.Media;
using System.Windows.Threading;

namespace Firefly.Client.Control
{
    public partial class CharacterThumb : System.Windows.Controls.Primitives.Thumb
    {
        #region route

        public static readonly RoutedEvent PositionChangedEvent = EventManager.RegisterRoutedEvent("PositionChanged", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(CharacterThumb));

        public event RoutedEventHandler PositionChanged { add { AddHandler(PositionChangedEvent, value); } remove { RemoveHandler(PositionChangedEvent, value); } }

        #endregion

        #region filed

        private const int REFRESH_TIME_INTERVAL = 1000;
        private const int MARGIN = 1;
        private CharacterViewModel _characterVM;
        private readonly static Pen _thickShapePen = new Pen(Brushes.Black, 3.0F);
        private readonly static Pen _thinShapePen = new Pen(Brushes.White, 1.0F);

        private DispatcherTimer _timer;

        #endregion

        #region constructor

        public CharacterThumb()
        {
            InitializeComponent();
        }

        #endregion

        #region method

        public void SetItem(OSDItem osdItem, Rect layerArea)
        {
            _characterVM = new CharacterViewModel(osdItem, layerArea);
            if (_characterVM != null)
            {
                if (_characterVM.OSDType == OSDType.Time)
                {
                    Dispose();
                    _timer = new DispatcherTimer();
                    _timer.Tick += RefreshTime;
                    _timer.IsEnabled = true;
                    _timer.Interval = new TimeSpan(REFRESH_TIME_INTERVAL);
                }
                else
                {
                    InvalidateVisual();
                }
            }
        }

        public void RefreshTime(object sender, EventArgs e)
        {
            _characterVM.Name = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            InvalidateVisual();
        }

        private FormattedText CreateFormattedText(string text)
        {
            Typeface typeface = new Typeface(new FontFamily("宋体"), FontStyles.Normal, FontWeights.Normal, FontStretch);

            double fontSize = 12.5;

            FormattedText formattedText = new FormattedText(text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, fontSize, Brushes.White);

            return formattedText;
        }

        private void Thumb_DragStarted(object sender, System.Windows.Controls.Primitives.DragStartedEventArgs e)
        {
            CaptureMouse();
        }

        private void Thumb_DragDelta(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e)
        {
            _characterVM.RawX += e.HorizontalChange;
            _characterVM.RawY += e.VerticalChange;
            InvalidateVisual();
        }

        private void Thumb_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            ReleaseMouseCapture();

            RoutedEventArgs args = new RoutedEventArgs(PositionChangedEvent, this);
            RaiseEvent(args);
        }

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);

            if (_characterVM != null && _characterVM.OSDStatus == OSDStatus.Enable)
            {
                FormattedText formattedText = CreateFormattedText(_characterVM.Name);

                if (_characterVM.LayerArea.Width > formattedText.Width && formattedText.Width > 0 && _characterVM.LayerArea.Height > formattedText.Height && formattedText.Height > 0)
                {
                    Geometry geometry = formattedText.BuildGeometry(new Point(0, 0));
                    dc.DrawGeometry(null, _thickShapePen, geometry);
                    dc.DrawGeometry(null, _thinShapePen, geometry);

                    if (_characterVM.Selected)
                    {
                        StreamGeometry streamGeometry = new StreamGeometry();
                        using (StreamGeometryContext streamGeometryContext = streamGeometry.Open())
                        {
                            streamGeometryContext.BeginFigure(new Point(-MARGIN, -MARGIN), false, true);
                            streamGeometryContext.LineTo(new Point(formattedText.Width + MARGIN, -MARGIN), true, true);
                            streamGeometryContext.LineTo(new Point(formattedText.Width + MARGIN, formattedText.Height + MARGIN), true, true);
                            streamGeometryContext.LineTo(new Point(-MARGIN, formattedText.Height + MARGIN), true, true);
                        }
                        dc.DrawGeometry(null, _thickShapePen, streamGeometry);
                        dc.DrawGeometry(null, _thinShapePen, streamGeometry);
                    }

                    _characterVM.SetMovableSize(formattedText.Width, formattedText.Height);
                    Margin = new Thickness(_characterVM.RawX, _characterVM.RawY, 0, 0);
                }
            }
        }

        public void Dispose()
        {
            if (_timer != null)
            {
                _timer.Stop();
                _timer = null;
            }
        }

        public OSDItem OSDItem => _characterVM.ToOSDItem();

        private void Thumb_GotFocus(object sender, RoutedEventArgs e)
        {
            _characterVM.Selected = true;
            InvalidateVisual();
        }

        private void Thumb_LostFocus(object sender, RoutedEventArgs e)
        {
            _characterVM.Selected = false;
            InvalidateVisual();
        }

        #endregion
    }

    public class CharacterViewModel : BaseViewModel
    {
        #region filed

        private int _id;
        private double _rawX;
        private double _rawY;
        private double _minHeight;
        private double _maxHeight;
        private double _minWidth;
        private double _maxWidth;
        private string _name;
        private bool _selected;
        private OSDType _osdType;
        private OSDStatus _osdStatus;
        private Rect _standardArea;
        private Rect _layerArea;

        #endregion

        #region constructor

        public CharacterViewModel(OSDItem osdItem, Rect layerArea)
        {
            _id = osdItem.ID;
            _name = osdItem.Name;
            _standardArea = osdItem.StandardArea;
            _layerArea = layerArea;
            Point point = ThermalCommon.RelToAbsPoint(ThermalCommon.AbsToRelPoint(osdItem.Positon, _standardArea), _layerArea);
            _rawX = point.X;
            _rawY = point.Y;
            _osdType = osdItem.OSDType;
            _osdStatus = osdItem.OSDStatus;
            _selected = false;
        }

        #endregion

        #region peoperty

        internal Rect LayerArea => _layerArea;

        internal double RawX
        {
            get => _rawX;
            set
            {
                if (value > _maxWidth)
                {
                    _rawX = _maxWidth;
                }
                else if (value < _minWidth)
                {
                    _rawX = _minWidth;
                }
                else
                {
                    _rawX = value;
                }
            }
        }

        internal double RawY
        {
            get => _rawY;
            set
            {
                if (value > _maxHeight)
                {
                    _rawY = _maxHeight;
                }
                else if (value < _minHeight)
                {
                    _rawY = _minHeight;
                }
                else
                {
                    _rawY = value;
                }
            }
        }

        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                OnPropertyChanged();
            }
        }

        internal OSDType OSDType => _osdType;

        internal OSDStatus OSDStatus
        {
            get => _osdStatus;
            set
            {
                _osdStatus = value;
                OnPropertyChanged();
            }
        }

        internal bool Selected
        {
            get => _selected;
            set
            {
                _selected = value;
            }
        }

        #endregion

        #region method

        internal void SetMovableSize(double width, double height)
        {
            _minHeight = _layerArea.X;
            _maxHeight = _layerArea.Height - height;
            _minWidth = _layerArea.Y;
            _maxWidth = _layerArea.Width - width;
        }

        public OSDItem ToOSDItem()
        {
            Point positon = ThermalCommon.RelToAbsPoint(ThermalCommon.AbsToRelPoint(new Point(RawX, RawY), _layerArea), _standardArea);
            return new OSDItem(_id, _standardArea, _name, positon, _osdType, _osdStatus);
        }

        #endregion
    }

    public enum OSDType
    {
        ChannelInfo = 0,
        Time = 1,
        PresetTitle = 2,
    }

    public enum OSDStatus
    {
        Disable = 0,
        Enable = 1
    }
}