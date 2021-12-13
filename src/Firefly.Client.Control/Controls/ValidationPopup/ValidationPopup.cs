// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System.Windows;
using System.Windows.Input;
using System.Windows.Controls.Primitives;

namespace Firefly.Client.Control
{
    public class ValidationPopup : Popup
    {
        #region Properties

        public static readonly DependencyProperty CloseOnMouseLeftButtonDownProperty =
            DependencyProperty.Register("CloseOnMouseLeftButtonDown", typeof(bool), typeof(ValidationPopup), new PropertyMetadata(true));

        public bool CloseOnMouseLeftButtonDown
        {
            get { return (bool)GetValue(CloseOnMouseLeftButtonDownProperty); }
            set { SetValue(CloseOnMouseLeftButtonDownProperty, value); }
        }

        #endregion

        #region Constructors

        public ValidationPopup()
        {
        }

        #endregion

        #region Methods

        protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            if (CloseOnMouseLeftButtonDown)
            {
                // this.SetCurrentValue(IsOpenProperty, false);
            }
        }

        #endregion
    }
}
