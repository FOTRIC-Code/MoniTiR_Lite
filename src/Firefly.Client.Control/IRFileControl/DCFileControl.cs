// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Windows;
using System.Windows.Media.Imaging;

namespace Firefly.Client.Control
{
    public class DCFileControl : System.Windows.Controls.Control
    {
        #region field

        private ImageThumb PART_ThumbImage;

        #endregion

        #region constructor

        static DCFileControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(DCFileControl), new FrameworkPropertyMetadata(typeof(DCFileControl)));
        }

        #endregion

        #region method

        public bool OpenFile(string fileName)
        {
            try
            {
                Uri uri = new Uri(fileName);
                BitmapImage bitmapImage = new BitmapImage(uri);
                PART_ThumbImage.SetImageSource(bitmapImage);
                return true;
            }
            catch 
            {
                return false;
            }
        }

        public void CloseFile()
        {
            PART_ThumbImage.Clear();
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            PART_ThumbImage = GetTemplateChild("PART_ThumbImage") as ImageThumb;
        }

        #endregion
    }
}
