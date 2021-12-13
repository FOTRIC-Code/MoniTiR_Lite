// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Windows;
using System.Windows.Threading;

namespace Firefly.Client.Control
{
    public partial class IRtekWaitWindow : Window
    {
        #region filed

        private bool _isClosed = true;
        private double _max = double.NaN;
        private double _min = double.NaN;
        private double _value = double.NaN;

        #endregion

        #region property

        public bool IsClosed => _isClosed;

        #endregion

        #region constructor

        public IRtekWaitWindow(Window owner, IRtekWaitWindowType rtekWaitWindowType) : this(owner, 0, rtekWaitWindowType)
        {
        }

        public IRtekWaitWindow(Window owner, double max, IRtekWaitWindowType rtekWaitWindowType) : this(owner, 0, max, 0, rtekWaitWindowType)
        {
        }

        public IRtekWaitWindow(Window owner, double min, double max, double value, IRtekWaitWindowType rtekWaitWindowType)
        {
            if (owner != null)
            {
                Owner = owner;
            }
            InitializeComponent();
            _max = max;
            _min = min;
            _value = value;
            Init(rtekWaitWindowType);
            SetValue(_value);
        }

        #endregion

        #region method

        public void SetValue(double value)
        {
            if (IsValidValue(value))
            {
                _value = value;
                Dispatcher.Invoke(() =>
                {
                    PART_ProgressText.Text = $"{Math.Floor(_value / _max * 100)} %";
                }, DispatcherPriority.Background);
            }
        }

        #endregion

        #region private method

        private void Init(IRtekWaitWindowType rtekWaitWindowType)
        {
            switch (rtekWaitWindowType)
            {
                case IRtekWaitWindowType.Animation:
                    PART_ProgressText.Visibility = Visibility.Collapsed;
                    PART_Cancel.Visibility = Visibility.Collapsed;
                    break;
                case IRtekWaitWindowType.AnimationCancel:
                    PART_ProgressText.Visibility = Visibility.Collapsed;
                    PART_Cancel.Visibility = Visibility.Visible;
                    break;
                case IRtekWaitWindowType.AnimationProgress:
                    PART_ProgressText.Visibility = Visibility.Visible;
                    PART_Cancel.Visibility = Visibility.Collapsed;
                    break;
                case IRtekWaitWindowType.AnimationCancelProgress:
                    PART_ProgressText.Visibility = Visibility.Visible;
                    PART_Cancel.Visibility = Visibility.Visible;
                    break;
            }
        }

        private void root_Loaded(object sender, RoutedEventArgs e)
        {
            _isClosed = false;
        }

        private void root_Closed(object sender, EventArgs e)
        {
            _isClosed = true;
        }

        private void hCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private bool IsValidValue(double value)
        {
            return value >= _min && value <= _max;
        }

        #endregion
    }

    public enum IRtekWaitWindowType
    {
        Animation = 1,
        AnimationCancel = 2,
        AnimationProgress = 3,
        AnimationCancelProgress = 4
    }
}