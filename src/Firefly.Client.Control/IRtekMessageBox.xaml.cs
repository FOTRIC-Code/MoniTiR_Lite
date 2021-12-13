// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Windows;
using System.Windows.Media;

namespace Firefly.Client.Control
{
    /// <summary>
    /// Interaction logic for MessageBox.xaml
    /// </summary>
    public partial class IRtekMessageBox : Window
    {
        private IRtekMessageBox(Window owner, string messageBoxText, string caption, MessageBoxButton button, MessageBoxImage icon)
        {
            InitializeComponent();

            if (owner != null)
            {
                Owner = owner;
            }
            MessageBoxText = messageBoxText;
            Caption = caption;
            Button = button;

            switch (icon)
            {
                case MessageBoxImage.None:
                    imgIcon.Visibility = Visibility.Collapsed;
                    break;

                default:
                    throw new InvalidProgramException();
            }

            switch (Button)
            {
                case MessageBoxButton.None:
                    btnYes.Visibility = Visibility.Collapsed;
                    btnNo.Visibility = Visibility.Collapsed;
                    btnOK.Visibility = Visibility.Collapsed;
                    btnCancel.Visibility = Visibility.Collapsed;
                    break;

                case MessageBoxButton.OK:
                    btnYes.Visibility = Visibility.Collapsed;
                    btnNo.Visibility = Visibility.Collapsed;
                    btnOK.Visibility = Visibility.Visible;
                    btnCancel.Visibility = Visibility.Collapsed;
                    break;

                case MessageBoxButton.OKCancel:
                    btnYes.Visibility = Visibility.Collapsed;
                    btnNo.Visibility = Visibility.Collapsed;
                    btnOK.Visibility = Visibility.Visible;
                    btnCancel.Visibility = Visibility.Visible;
                    break;

                case MessageBoxButton.YesNoCancel:
                    btnYes.Visibility = Visibility.Visible;
                    btnNo.Visibility = Visibility.Visible;
                    btnOK.Visibility = Visibility.Collapsed;
                    btnCancel.Visibility = Visibility.Visible;
                    break;

                case MessageBoxButton.YesNo:
                    btnYes.Visibility = Visibility.Visible;
                    btnNo.Visibility = Visibility.Visible;
                    btnOK.Visibility = Visibility.Collapsed;
                    btnCancel.Visibility = Visibility.Collapsed;
                    break;
            }
        }

        public static bool? ShowDialog(Window owner, string messageBoxText)
        {
            return ShowDialog(owner, messageBoxText, string.Empty);
        }

        public static bool? ShowDialog(Window owner, string messageBoxText, string caption)
        {
            return ShowDialog(owner, messageBoxText, caption, MessageBoxButton.None);
        }

        public static bool? ShowDialog(Window owner, string messageBoxText, string caption, MessageBoxButton button)
        {
            return ShowDialog(owner, messageBoxText, caption, button, MessageBoxImage.None);
        }

        public static bool? ShowDialog(Window owner, string messageBoxText, string caption, MessageBoxButton button, MessageBoxImage icon)
        {
            bool? result = false;
            try
            {
                IRtekMessageBox messageBox = new IRtekMessageBox(owner, messageBoxText, caption, button, icon);
                result = messageBox.ShowDialog();
            }
            catch
            {
                result = false;
            }

            return result;
        }

        public static readonly DependencyProperty MessageBoxTextProperty = DependencyProperty.Register("MessageBoxText", typeof(string), typeof(IRtekMessageBox),
            new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault | FrameworkPropertyMetadataOptions.AffectsRender));

        public string MessageBoxText
        {
            get { return (string)GetValue(MessageBoxTextProperty); }
            set { SetValue(MessageBoxTextProperty, value); }
        }

        public static readonly DependencyProperty CaptionProperty = DependencyProperty.Register("Caption", typeof(string), typeof(IRtekMessageBox),
            new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault | FrameworkPropertyMetadataOptions.AffectsRender));

        public string Caption
        {
            get { return (string)GetValue(CaptionProperty); }
            set { SetValue(CaptionProperty, value); }
        }

        public static readonly DependencyProperty ButtonProperty = DependencyProperty.Register("Button", typeof(MessageBoxButton), typeof(IRtekMessageBox),
            new FrameworkPropertyMetadata(MessageBoxButton.None, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault | FrameworkPropertyMetadataOptions.AffectsRender));

        public MessageBoxButton Button
        {
            get { return (MessageBoxButton)GetValue(ButtonProperty); }
            set { SetValue(ButtonProperty, value); }
        }

        public static readonly DependencyProperty ImageIconProperty = DependencyProperty.Register("ImageIcon", typeof(ImageSource), typeof(IRtekMessageBox),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault | FrameworkPropertyMetadataOptions.AffectsRender));

        public ImageSource ImageIcon
        {
            get { return (ImageSource)GetValue(ImageIconProperty); }
            set { SetValue(ImageIconProperty, value); }
        }

        private void Close(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Close();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void btnNo_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void btnYes_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }

    public enum MessageBoxButton
    {
        None = 0,

        //
        // Summary:
        //     The message box displays an OK button.
        OK = 1,

        //
        // Summary:
        //     The message box displays OK and Cancel buttons.
        OKCancel = 2,

        //
        // Summary:
        //     The message box displays Yes, No, and Cancel buttons.
        YesNoCancel = 3,

        //
        // Summary:
        //     The message box displays Yes and No buttons.
        YesNo = 4
    }
}