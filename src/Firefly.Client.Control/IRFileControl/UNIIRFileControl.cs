// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Drawing.Imaging;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Controls.Primitives;

using Amur.Common;
using Firefly.SDK.File;

namespace Firefly.Client.Control
{
    public partial class UNIIRFileControl : System.Windows.Controls.Control
    {
        #region field

        private ImageThumb PART_ThumbImage;
        private ThermalHint PART_Hint;
        private Grid PART_DisplayArea;
        private UNITheramlImage _uniTheramlImage;

        private bool _isOpenFile;
        private Rect _theramlArea;

        #endregion

        #region constructor

        static UNIIRFileControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(UNIIRFileControl), new FrameworkPropertyMetadata(typeof(UNIIRFileControl)));
        }

        #endregion

        #region method

        public bool OpenFile(string fileName)
        {
            bool isOpenFile = false;
            try
            {
                _uniTheramlImage = new UNITheramlImage();
                if (_uniTheramlImage.Open(fileName))
                {
                    isOpenFile = true;

                    System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(_uniTheramlImage.GetImage());

                    PART_ThumbImage.SetImageSource(bitmap.ToBitmapImage());

                    _theramlArea = new Rect(0, 0, _uniTheramlImage.Width, _uniTheramlImage.Height);
                    PART_Hint.ThermalArea = new Rect(0, 0, PART_DisplayArea.ActualWidth, PART_DisplayArea.ActualHeight);
                }
                _isOpenFile = isOpenFile;
                return isOpenFile;
            }
            catch
            {
                return isOpenFile;
            }
        }

        public void CloseFile()
        {
            _uniTheramlImage = null;
            _isOpenFile = false;
            _theramlArea = new Rect();
            PART_ThumbImage.Clear();
            PART_Hint.ClearThermalArea();
            PART_Hint.ClearTemperatureHint();
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            PART_ThumbImage = GetTemplateChild("PART_ThumbImage") as ImageThumb;
            PART_Hint = GetTemplateChild("PART_Hint") as ThermalHint;
            PART_DisplayArea = GetTemplateChild("PART_DisplayArea") as Grid;

            PART_DisplayArea.MouseLeave += OnMouseLeave;
            PART_ThumbImage.DragStarted += OnDragStarted;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            Point hintPoint = e.GetPosition(this);
            TryShowTempHint(hintPoint);
        }

        private void OnMouseLeave(object sender, MouseEventArgs e)
        {
            PART_Hint.ClearTemperatureHint();
        }

        private void OnDragStarted(object sender, DragStartedEventArgs e)
        {
             PART_Hint.ClearTemperatureHint();
        }

        private void TryShowTempHint(Point hintPoint)
        {
            bool needShowTempHint = PART_Hint.ThermalArea.Contains(hintPoint) && PART_ThumbImage.IsMouseOver && _isOpenFile;
            if (needShowTempHint)
            {
                Point point = PART_ThumbImage.GetMousePositionByImage();
                Rect imageArea = PART_ThumbImage.GetImageArea();
                Point positon = ThermalCommon.RelToAbsPoint(ThermalCommon.AbsToRelPoint(point, imageArea), _theramlArea);
                float temp = _uniTheramlImage.GetTemp(new System.Drawing.Point((int)positon.X, (int)positon.Y));
                PART_Hint.LastMouseOverPoint = hintPoint;
                PART_Hint.TemperatureHint = new Temperature(temp, TemperatureUnit.Celsiur).ToString();
            }
            else
            {
                PART_Hint.ClearTemperatureHint();
            }
        }

        #endregion
    }
}