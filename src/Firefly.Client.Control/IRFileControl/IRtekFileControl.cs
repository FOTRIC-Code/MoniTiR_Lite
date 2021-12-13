// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

using Amur.Common;
using Firefly.SDK.File;

namespace Firefly.Client.Control
{
    public class IRtekFileControl : System.Windows.Controls.Control
    {
        #region filed

        private ThermalHint PART_Hint;
        private ImageThumb PART_ThumbImage;
        private ListBox PART_ImageList;
        private Grid PART_DisplayArea;

        private IRtekThermalImage _irtekThermalImage;
        private bool _isOpenFile;
        private System.Drawing.Size _theramlArea;
        private IRtekFileViewModel _irtekFileVM;

        #endregion

        #region property

        static IRtekFileControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(IRtekFileControl), new FrameworkPropertyMetadata(typeof(IRtekFileControl)));
        }

        #endregion

        #region method

        public bool OpenFile(string fileName)
        {
            bool isOpenFile = false;
            try
            {
                _irtekFileVM.Clear();
                _irtekThermalImage = new IRtekThermalImage();
                if (_irtekThermalImage.Open(fileName))
                {
                    isOpenFile = true;
                    System.Drawing.Bitmap coverBitmap = _irtekThermalImage.GetCoverImage();
                    PART_ThumbImage.SetImageSource(new System.Drawing.Bitmap(coverBitmap).ToBitmapImage());

                    if (coverBitmap != null)
                    {
                        _irtekFileVM.AddImageItem(new ImageItemViewModel(I18n.Properties.Resources.CoverImage, new System.Drawing.Bitmap(coverBitmap).ToBitmapImage()));
                    }

                    System.Drawing.Bitmap visualBitmap = _irtekThermalImage.GetVisualImage();
                    if (visualBitmap != null)
                    {
                        _irtekFileVM.AddImageItem(new ImageItemViewModel(I18n.Properties.Resources.VisualImage, new System.Drawing.Bitmap(visualBitmap).ToBitmapImage()));
                    }

                    System.Drawing.Bitmap thermalBitmap = _irtekThermalImage.GetThermalImage();
                    if (coverBitmap != null)
                    {
                        _irtekFileVM.AddImageItem(new ImageItemViewModel(I18n.Properties.Resources.ThermalImage, new System.Drawing.Bitmap(thermalBitmap).ToBitmapImage()));
                    }

                    System.Drawing.Bitmap defectBitmap = _irtekThermalImage.GetFireflyDefectImage();
                    if (defectBitmap != null)
                    {
                        _irtekFileVM.AddImageItem(new ImageItemViewModel(I18n.Properties.Resources.DefectImage, new System.Drawing.Bitmap(defectBitmap).ToBitmapImage()));
                    }

                    _theramlArea = _irtekThermalImage.GetThermalSize();

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
            _irtekThermalImage?.Close();
            _irtekFileVM?.Clear();
            _isOpenFile = false;
            _theramlArea = new System.Drawing.Size();
            PART_ThumbImage.Clear();
            PART_Hint.ClearThermalArea();
            PART_Hint.ClearTemperatureHint();
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _irtekFileVM = new IRtekFileViewModel();
            DataContext = _irtekFileVM;

            PART_ThumbImage = GetTemplateChild("PART_ThumbImage") as ImageThumb;
            PART_Hint = GetTemplateChild("PART_Hint") as ThermalHint;
            PART_ImageList = GetTemplateChild("PART_ImageList") as ListBox;
            PART_DisplayArea = GetTemplateChild("PART_DisplayArea") as Grid;

            PART_ImageList.SelectionChanged += ImageSelectionChanged;
            PART_ThumbImage.DragStarted += OnDragStarted;
        }

        private void ImageSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ListBox listBox && listBox.SelectedItem is ImageItemViewModel imageItemVM)
            {
                PART_ThumbImage.SetImageSource(imageItemVM.Source);
            }
        }

        private void OnDragStarted(object sender, DragStartedEventArgs e)
        {
            PART_Hint.ClearTemperatureHint();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            Point hintPoint = e.GetPosition(this);
            TryShowTempHint(hintPoint);
        }

        private void TryShowTempHint(Point hintPoint)
        {
            bool needShowTempHint = PART_Hint.ThermalArea.Contains(hintPoint) && PART_ThumbImage.IsMouseOver && _isOpenFile;
            if (needShowTempHint)
            {
                Point point = PART_ThumbImage.GetMousePositionByImage();
                Rect imageArea = PART_ThumbImage.GetImageArea();
                System.Drawing.Point positon = ThermalCommon.RelToAbsPoint(ThermalCommon.AbsToRelPoint(point, imageArea), _theramlArea);
                float temp = _irtekThermalImage.GetSpotValue(positon);
                PART_Hint.LastMouseOverPoint = hintPoint;
                PART_Hint.TemperatureHint = new Temperature(temp, TemperatureUnit.Kelvin).ToString();
            }
            else
            {
                PART_Hint.ClearTemperatureHint();
            }
        }

        #endregion
    }
}