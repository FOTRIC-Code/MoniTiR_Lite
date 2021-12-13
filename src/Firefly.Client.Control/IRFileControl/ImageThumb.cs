// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace Firefly.Client.Control
{
    public class ImageThumb : Thumb
    {
        #region field

        private Image PART_Image;
        private ScaleTransform PART_Zoom;
        private TranslateTransform PART_Translate;

        private const int DELTA = 120;
        private const int MAX_ZOOM_MULTIPLIER = 100;
        private const int MIN_ZOOM_MULTIPLIER = 0;
        private const int DEFAULT_ZOOM_MULTIPLIER = 0;
        private const int DEFAULT_X_OFFSET = 0;
        private const int DEFAULT_Y_OFFSET = 0;
        private const float POW = 1.1F;
        private int _zoomMultiple;
        private double _xOffset;
        private double _yOffset;
        private bool _isDragging;

        #endregion

        #region constructor

        static ImageThumb()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ImageThumb), new FrameworkPropertyMetadata(typeof(ImageThumb)));
        }

        #endregion

        #region method

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            PART_Image = GetTemplateChild("PART_Image") as Image;
            PART_Zoom = GetTemplateChild("PART_Zoom") as ScaleTransform;
            PART_Translate = GetTemplateChild("PART_Translate") as TranslateTransform;
            DragDelta += OnDragDelta;
            DragStarted += OnDragStarted;
            DragCompleted += OnDragCompleted;
        }

        public void SetImageSource(ImageSource imageSource)
        {
            if (PART_Image != null)
            {
                PART_Image.Source = imageSource;
            }
        }

        public void Clear()
        {
            PART_Image.ClearValue(Image.SourceProperty);

            PART_Zoom.ClearValue(ScaleTransform.CenterXProperty);
            PART_Zoom.ClearValue(ScaleTransform.CenterYProperty);
            PART_Zoom.ClearValue(ScaleTransform.ScaleXProperty);
            PART_Zoom.ClearValue(ScaleTransform.ScaleYProperty);

            PART_Translate.ClearValue(TranslateTransform.XProperty);
            PART_Translate.ClearValue(TranslateTransform.YProperty);

            _zoomMultiple = DEFAULT_ZOOM_MULTIPLIER;
            _xOffset = DEFAULT_X_OFFSET;
            _yOffset = DEFAULT_Y_OFFSET;
        }

        public Rect GetImageArea()
        {
            return new Rect(0, 0, PART_Image.ActualWidth, PART_Image.ActualHeight);
        }

        public Point GetMousePositionByImage()
        {
            return Mouse.GetPosition(PART_Image);
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            base.OnMouseWheel(e);
            Point point = e.GetPosition(PART_Image);
            if (IsMouseOver)
            {
                int zoomMultiple = e.Delta / DELTA;
                _zoomMultiple += zoomMultiple;

                if (_zoomMultiple > MAX_ZOOM_MULTIPLIER)
                {
                    _zoomMultiple = MAX_ZOOM_MULTIPLIER;
                }
                if (_zoomMultiple < MIN_ZOOM_MULTIPLIER)
                {
                    _zoomMultiple = MIN_ZOOM_MULTIPLIER;
                }
                double zoomRatio = Math.Pow(POW, _zoomMultiple);
                PART_Zoom.CenterX = point.X + _xOffset;
                PART_Zoom.CenterY = point.Y + _yOffset;
                PART_Zoom.ScaleX = zoomRatio;
                PART_Zoom.ScaleY = zoomRatio;
            }
        }

        private void OnDragStarted(object sender, DragStartedEventArgs e)
        {
            if (IsMouseOver)
            {
                CaptureMouse();
                Cursor = Cursors.Hand;
                _isDragging = true;
            }
        }

        private void OnDragCompleted(object sender, DragCompletedEventArgs e)
        {
            if (_isDragging)
            {
                Cursor = Cursors.Arrow;
                _xOffset = PART_Translate.X;
                _yOffset = PART_Translate.Y;
                _isDragging = false;
                ReleaseMouseCapture();
            }
        }

        private void OnDragDelta(object sender, DragDeltaEventArgs e)
        {
            if (_isDragging)
            {
                PART_Translate.X = _xOffset + e.HorizontalChange / PART_Zoom.ScaleX;
                PART_Translate.Y = _yOffset + e.VerticalChange / PART_Zoom.ScaleY;
            }
        }

        #endregion
    }
}
