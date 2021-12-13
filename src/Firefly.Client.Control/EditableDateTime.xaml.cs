// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Text.RegularExpressions;

namespace Firefly.Client.Control
{
    public enum DateType
    {
        Year,
        Month,
        Day,
        Hour,
        Minute,
        Second
    }

    public partial class EditableDateTime : UserControl
    {
        #region Fields

        private const int MAX_YEAR = 2099;
        private const int MIN_YEAR = 2000;
        private const int MAX_MONTH = 12;
        private const int MIN_MONTH = 1;
        private const int MIN_DAY = 1;
        private const int MAX_HOUR = 23;
        private const int MIN_HOUR = 0;
        private const int MAX_MINUTE = 59;
        private const int MIN_MINUTE = 0;
        private const int MAX_SECOND = 59;
        private const int MIN_SECOND = 0;

        private TextBox lastestFocusTextBox;

        #endregion

        #region Constructors

        public EditableDateTime()
        {
            InitializeComponent();
        }

        #endregion

        #region Properties

        public static readonly DependencyProperty TimeProperty = DependencyProperty.Register(
            "Time", typeof(DateTime), typeof(EditableDateTime), new FrameworkPropertyMetadata(default(DateTime), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault | FrameworkPropertyMetadataOptions.Journal, TimeChangedCallback));

        public DateTime Time
        {
            get { return (DateTime)GetValue(TimeProperty); }
            set { SetValue(TimeProperty, value); }
        }

        #endregion


        #region Methods

        private static void TimeChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is EditableDateTime depend && e.NewValue is DateTime newValue)
            {
                depend.SetTextBoxValue(DateType.Year, newValue.Year);
                depend.SetTextBoxValue(DateType.Month, newValue.Month);
                depend.SetTextBoxValue(DateType.Day, newValue.Day);
                depend.SetTextBoxValue(DateType.Hour, newValue.Hour);
                depend.SetTextBoxValue(DateType.Minute, newValue.Minute);
                depend.SetTextBoxValue(DateType.Second, newValue.Second);
            }
        }

        private void textBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (Regex.IsMatch(e.Text, "[^0-9]+"))
            {
                e.Handled = true;
            }

            if (sender is TextBox textBox)
            {
                if (textBox.SelectionLength == textBox.Text.Length)
                {
                    return;
                }

                if (textBox.Tag is DateType type && ((type == DateType.Year && textBox.Text.Length >= 4)
                                                     || (type != DateType.Year && textBox.Text.Length >= 2)))
                {
                    e.Handled = true;
                }
            }
        }

        private void textBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                SetTextBoxValue((DateType)textBox.Tag, string.IsNullOrWhiteSpace(textBox.Text) ? 0 : int.Parse(textBox.Text));
                SetTimeValue();
            }
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            base.OnPreviewKeyDown(e);

            if (e.Key == Key.Delete || e.Key == Key.Space)
            {
                e.Handled = true;
            }

            if (e.Key == Key.Right)
            {
                IInputElement element = Keyboard.FocusedElement;
                if (element is TextBox textBox && textBox.CaretIndex == textBox.Text.Length)
                {
                    TraversalRequest traversalRequest = new TraversalRequest(FocusNavigationDirection.Next);
                    textBox.MoveFocus(traversalRequest);
                }
            }

            if (e.Key == Key.Left)
            {
                IInputElement element = Keyboard.FocusedElement;
                if (element is TextBox textBox && textBox.CaretIndex == 0)
                {
                    TraversalRequest traversalRequest = new TraversalRequest(FocusNavigationDirection.Previous);
                    textBox.MoveFocus(traversalRequest);
                }
            }
        }


        private void btnIncrease_Click(object sender, RoutedEventArgs e)
        {
            if (lastestFocusTextBox != null)
            {
                if (int.TryParse(lastestFocusTextBox.Text, out int result))
                {
                    if (lastestFocusTextBox.Tag is DateType type)
                    {
                        result++;
                        SetTextBoxValue(type, result);
                    }

                    SetTimeValue();
                }

                lastestFocusTextBox.SelectAll();
                lastestFocusTextBox.Focus();
            }
        }

        private void btnDecrease_Click(object sender, RoutedEventArgs e)
        {
            if (lastestFocusTextBox != null)
            {
                if (int.TryParse(lastestFocusTextBox.Text, out int result))
                {
                    if (lastestFocusTextBox.Tag is DateType type)
                    {
                        result--;
                        SetTextBoxValue(type, result);
                    }

                    SetTimeValue();
                }

                lastestFocusTextBox.SelectAll();
                lastestFocusTextBox.Focus();
            }
        }

        private void cb_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (this.IsKeyboardFocusWithin)
            {
                IInputElement element = Keyboard.FocusedElement;
                if (element is TextBox textBox)
                {
                    lastestFocusTextBox = textBox;
                }
            }
        }

        private void SetTimeValue()
        {
            SetTextBoxValue(DateType.Year,int.Parse(tbYear.Text));
            SetTextBoxValue(DateType.Month,int.Parse(tbMonth.Text));
            SetTextBoxValue(DateType.Day,int.Parse(tbDay.Text));
            SetCurrentValue(TimeProperty, new DateTime(int.Parse(tbYear.Text), int.Parse(tbMonth.Text), int.Parse(tbDay.Text), int.Parse(tbHour.Text), int.Parse(tbMinute.Text), int.Parse(tbSecond.Text)));
        }

        private void SetTextBoxValue(DateType type, int value)
        {
            switch (type)
            {
                case DateType.Year:
                    value = value < MIN_YEAR ? MIN_YEAR : value;
                    value = value > MAX_YEAR ? MAX_YEAR : value;
                    tbYear.Text = value.ToString("D4");
                    break;
                case DateType.Month:
                    value = value < MIN_MONTH ? MIN_MONTH : value;
                    value = value > MAX_MONTH ? MAX_MONTH : value;
                    tbMonth.Text = value.ToString("D2");
                    break;
                case DateType.Day:
                    int year = int.Parse(tbYear.Text);
                    int month = int.Parse(tbMonth.Text);
                    int daysInMonth = DateTime.DaysInMonth(year, month);
                    value = value > daysInMonth ? daysInMonth : value;
                    value = value < MIN_DAY ? MIN_DAY : value;
                    tbDay.Text = value.ToString("D2");
                    break;
                case DateType.Hour:
                    value = value < MIN_HOUR ? MIN_HOUR : value;
                    value = value > MAX_HOUR ? MAX_HOUR : value;
                    tbHour.Text = value.ToString("D2");
                    break;
                case DateType.Minute:
                    value = value < MIN_MINUTE ? MIN_MINUTE : value;
                    value = value > MAX_MINUTE ? MAX_MINUTE : value;
                    tbMinute.Text = value.ToString("D2");
                    break;
                case DateType.Second:
                    value = value < MIN_SECOND ? MIN_SECOND : value;
                    value = value > MAX_SECOND ? MAX_SECOND : value;
                    tbSecond.Text = value.ToString("D2");
                    break;
            }
        }

        #endregion

    }
}
