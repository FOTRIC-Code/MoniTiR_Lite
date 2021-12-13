// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;

namespace Firefly.Client.Control
{
    public partial class OSDConfigGrid : Grid, IRTSPClientCallback
    {
        #region route

        public static readonly RoutedEvent PositionChangedEvent = EventManager.RegisterRoutedEvent("PositionChanged", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(OSDConfigGrid));

        public event RoutedEventHandler PositionChanged { add { AddHandler(PositionChangedEvent, value); } remove { RemoveHandler(PositionChangedEvent, value); } }

        #endregion

        #region

        private const int LAYER_HEIGHT = 480;
        private const int LAYER_WIDTH = 640;
        private Rect _layerArea;
        private RTSPClient _rtspClient;

        #endregion

        #region constructor

        public OSDConfigGrid()
        {
            InitializeComponent();
            _layerArea = new Rect(new Size(LAYER_WIDTH, LAYER_HEIGHT));
            PART_imageCanvas.ClientArea = _layerArea;
            _rtspClient = new RTSPClient();
        }

        #endregion

        #region Property

        public OSDItem ChannelOSD
        {
            get { return (OSDItem)GetValue(ChannelOSDProperty); }
            set { SetValue(ChannelOSDProperty, value); }
        }

        public static readonly DependencyProperty ChannelOSDProperty =
            DependencyProperty.Register("ChannelOSD", typeof(OSDItem), typeof(OSDConfigGrid), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, new PropertyChangedCallback(ChannelOSDPropertyChanged)));

        public OSDItem TimeOSD
        {
            get { return (OSDItem)GetValue(TimeOSDProperty); }
            set { SetValue(TimeOSDProperty, value); }
        }

        public static readonly DependencyProperty TimeOSDProperty =
            DependencyProperty.Register("TimeOSD", typeof(OSDItem), typeof(OSDConfigGrid), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, new PropertyChangedCallback(TimeOSDPropertyChanged)));

        public OSDItem PTZOSD
        {
            get { return (OSDItem)GetValue(PTZOSDProperty); }
            set { SetValue(PTZOSDProperty, value); }
        }

        public static readonly DependencyProperty PTZOSDProperty =
            DependencyProperty.Register("PTZOSD", typeof(OSDItem), typeof(OSDConfigGrid), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, new PropertyChangedCallback(PTZOSDPropertyChanged)));

        public string RTSPUrl
        {
            get { return (string)GetValue(RTSPUrlProperty); }
            set { SetValue(RTSPUrlProperty, value); }
        }

        public static readonly DependencyProperty RTSPUrlProperty =
            DependencyProperty.Register("RTSPUrl", typeof(string), typeof(OSDConfigGrid), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, OnRTSPUrlChanged));

        public bool EnableOSD
        {
            get { return (bool)GetValue(EnableOSDProperty); }
            set { SetValue(EnableOSDProperty, value); }
        }

        public static readonly DependencyProperty EnableOSDProperty =
            DependencyProperty.Register("EnableOSD", typeof(bool), typeof(OSDConfigGrid), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));

        public OSDItem ChannelOSDItem => PART_channel.OSDItem;

        public OSDItem TimeOSDItem => PART_time.OSDItem;

        public OSDItem PTZOSDItem => PART_ptz.OSDItem;

        #endregion

        #region method

        public static void ChannelOSDPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            OSDConfigGrid osdConfigGrid = (OSDConfigGrid)d;
            OSDItem osdItem = (OSDItem)e.NewValue;
            if (osdConfigGrid != null)
            {
                osdConfigGrid.PART_channel.Dispose();
                if (osdItem != null)
                {
                    osdConfigGrid.PART_channel.SetItem(osdItem, osdConfigGrid._layerArea);
                }
            }
        }

        public static void TimeOSDPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            OSDConfigGrid osdConfigGrid = (OSDConfigGrid)d;
            OSDItem osdItem = (OSDItem)e.NewValue;
            if (osdConfigGrid != null)
            {
                osdConfigGrid.PART_time.Dispose();
                if (osdItem != null)
                {
                    osdConfigGrid.PART_time.SetItem(osdItem, osdConfigGrid._layerArea);
                }
            }
        }

        public static void PTZOSDPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            OSDConfigGrid osdConfigGrid = (OSDConfigGrid)d;
            OSDItem osdItem = (OSDItem)e.NewValue;
            if (osdConfigGrid != null)
            {
                osdConfigGrid.PART_ptz.Dispose();
                if (osdItem != null)
                {
                    osdConfigGrid.PART_ptz.SetItem(osdItem, osdConfigGrid._layerArea);
                }
            }
        }

        private void CharacterThumb_PositionChanged(object sender, RoutedEventArgs e)
        {
            RoutedEventArgs args = new RoutedEventArgs(PositionChangedEvent, this);
            RaiseEvent(args);
        }

        private static void OnRTSPUrlChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            OSDConfigGrid osdConfigGrid = (OSDConfigGrid)d;

            string rtspUrl = e.NewValue as string;
            if (!string.IsNullOrWhiteSpace(rtspUrl))
            {
                osdConfigGrid.Logout();
                osdConfigGrid.Login();
            }
            else
            {
                osdConfigGrid.Dispose();
            }
        }
        
        internal void Login()
        {
            _rtspClient.Register(RTSPUrl, this);
            PART_imageCanvas.Start();
        }

        internal void Logout()
        {
            try
            {
                Mouse.SetCursor(Cursors.Wait);
                
                _rtspClient.Destroy();
                PART_imageCanvas.Stop();
            }
            finally
            {
                Mouse.SetCursor(Cursors.Arrow);
            }
        }

        public void Dispose()
        {
            _rtspClient.Destroy();
            PART_channel.Dispose();
            PART_ptz.Dispose();
            PART_time.Dispose();
            PART_imageCanvas.Stop();
        }

        public void OnConnectChanged(bool isConnected)
        {
        }

        public void OnFrameChanged(IntPtr data, bool isKeyFrame, int width, int height)
        {
            PART_imageCanvas?.AppendVideoFrame(data, isKeyFrame, width, height);
        }

        public void OnRecordStatusChanged(bool record)
        {
            
        }

        public void OnRecordFinished(string recordPath)
        {

        }

        #endregion
    }

    public class OSDItem
    {
        #region constructor

        public OSDItem(int id, Rect standardArea, string name, Point positon, OSDType osdType, OSDStatus osdStatus)
        {
            ID = id;
            StandardArea = standardArea;
            Name = name;
            Positon = positon;
            OSDType = osdType;
            OSDStatus = osdStatus;
        }

        #endregion

        #region property

        public int ID { get; set; }

        public Rect StandardArea { get; set; }

        public string Name { get; set; }

        public Point Positon { get; set; }

        public OSDType OSDType { get; set; }

        public OSDStatus OSDStatus { get; set; }

        #endregion
    }
}