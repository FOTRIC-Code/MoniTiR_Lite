 // Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Windows.Media.Imaging;
using System.Runtime.InteropServices;
using Pen = System.Windows.Media.Pen;
using Brushes = System.Windows.Media.Brushes;
using System.Threading;

namespace Firefly.Client.Control
{
    public class FrameBufferInfo
    {
        public byte[] Data { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public bool NewFrame { get; set; }

        public FrameBufferInfo()
        {
            Data = null;
            Width = 0;
            Height = 0;
        }
    }

    public class ImageCanvas : Canvas
    {
        #region Fields

        private bool _pause = false;
        private Rect _clientArea;
        private volatile FrameBufferInfo _argbFrame = new FrameBufferInfo();

        private WriteableBitmap _writeableBitmap;
        private Int32Rect _int32Rect;
        private Pen _pen;
        private AutoResetEvent _frameEvent;

        private DispatcherTimer _timer;

        #endregion Fields

        #region Constructors

        public ImageCanvas()
        {
            _pen = new Pen { Brush = Brushes.DarkGray, Thickness = 0 };
            _frameEvent = new AutoResetEvent(true);
        }

        #endregion Constructors

        #region Properties

        public Rect ClientArea
        {
            get { return _clientArea; }
            set
            {
                lock (this)
                {
                    if (_clientArea != value)
                    {
                        _clientArea = value;
                    }
                }
            }
        }

        #endregion Properties

        #region Methods

        public void Start()
        {
            Pause(false);
        }

        public void Stop()
        {
            Pause(true);
            _writeableBitmap = null;
        }

        private void Pause(bool pause)
        {
            if (pause)
            {
                StopTimer();
            }
            else
            {
                StartTimer();
            }
            if (_pause != pause)
            {
                _pause = pause;
                if (_pause)
                {
                    this.Background = new SolidColorBrush(Color.FromArgb(0xFF, 0x20, 0x20, 0x20));
                }
                else
                {
                    this.Background = new SolidColorBrush(Color.FromArgb(0xFF, 0x00, 0x00, 0x00));
                }

                //lock (_argbFrame)
                {
                    ReleaseBuffer(_argbFrame);
                }
                this.InvalidateVisual();
            }
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (_argbFrame.Data != null && _argbFrame.NewFrame)
            {
                _argbFrame.NewFrame = false;
                InvalidateVisual();
            }
        }

        public bool AppendVideoFrame(IntPtr data, bool isKeyFrame, int width, int height)
        {
            if (data == IntPtr.Zero)
            {
                return false;
            }


            if (_argbFrame.Data == null || _argbFrame.Width != width || _argbFrame.Height != height)
            {
                ReleaseBuffer(_argbFrame);
                _argbFrame.Width = width;
                _argbFrame.Height = height;
                _argbFrame.Data = new byte[width * height * 3];
            }

            _argbFrame.NewFrame = true;
            Marshal.Copy(data, _argbFrame.Data, 0, _argbFrame.Data.Length);


            if (isKeyFrame)
            {
                _argbFrame.NewFrame = false;
            }
            return true;
        }

        protected override void OnRender(DrawingContext dc)
        {
            bool needDrawFrame = _argbFrame.Data != null;

            if (!needDrawFrame)
            {
                return;
            }


            if (_argbFrame.Data == null)
            {
                return;
            }
            if (UIConstants.RenderMode == ImageRenderMode.Fit)
            {
                RenderFitMode(dc, _argbFrame.Data, _argbFrame.Width, _argbFrame.Height);
            }
            else if (UIConstants.RenderMode == ImageRenderMode.Fill)
            {
                RenderFillMode(dc, _argbFrame.Data, _argbFrame.Width, _argbFrame.Height);
            }
        }

        private void RenderFillMode(DrawingContext dc, byte[] data, int width, int height)
        {
            if (_writeableBitmap == null || _writeableBitmap.PixelWidth != width || _writeableBitmap.PixelHeight != height)
            {
                _writeableBitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgr24, null);
                _int32Rect = new Int32Rect(0, 0, width, height);
            }

            _writeableBitmap.Lock();
            Marshal.Copy(data, 0, _writeableBitmap.BackBuffer, data.Length);
            _writeableBitmap.AddDirtyRect(_int32Rect);
            _writeableBitmap.Unlock();

            ImageBrush brush = new ImageBrush(_writeableBitmap);
            brush.Stretch = Stretch.Fill;
            dc.DrawRectangle(brush, _pen, _clientArea);
        }

        private void RenderFitMode(DrawingContext dc, byte[] data, int width, int height)
        {
            if (_writeableBitmap == null || _writeableBitmap.PixelWidth != width || _writeableBitmap.PixelHeight != height)
            {
                _writeableBitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgr24, null);
                _int32Rect = new Int32Rect(0, 0, width, height);
            }

            _writeableBitmap.Lock();
            Marshal.Copy(data, 0, _writeableBitmap.BackBuffer, data.Length);
            _writeableBitmap.AddDirtyRect(_int32Rect);
            _writeableBitmap.Unlock();

            ImageBrush brush = new ImageBrush(_writeableBitmap);
            brush.Stretch = Stretch.Fill;
            dc.DrawRectangle(brush, _pen, _clientArea);
        }

        private void ReleaseBuffer(FrameBufferInfo info)
        {
            if (info.Data != null)
                info.Data = null;
            info.Width = 0;
            info.Height = 0;
        }

        public void SaveBitmap(string filePath)
        {
            lock (_argbFrame)
            {
                if (_writeableBitmap != null)
                {
                    BitmapEncoder encoder = new JpegBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(_writeableBitmap));

                    if (Directory.Exists(Path.GetFullPath(filePath)))
                    {
                        Directory.CreateDirectory(Path.GetFullPath(filePath));
                    }
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                    }
                    using (FileStream stream = new FileStream(filePath, FileMode.Create))
                    {
                        encoder.Save(stream);
                    }
                }
            }
        }

        private void StartTimer()
        {
            StopTimer();
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(30);
            _timer.Tick += Timer_Tick;
            _timer.Start();
        }

        private void StopTimer()
        {
            _timer?.Stop();
            _timer = null;
        }

        #endregion Methods
    }
}