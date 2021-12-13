using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;

namespace Firefly.Client.Control
{
    public partial class WheelControl : UserControl
    {
        public WheelControl()
        {
            InitializeComponent();
        }

        private void btnIRCameraPresetControl_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.Tag = (sender as FrameworkElement).Tag;
            this.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, 1, MouseButton.Left)
            {
                RoutedEvent = MouseLeftButtonDownEvent,
            });
        }

        private void btnIRCameraPresetControl_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            this.Tag = (sender as FrameworkElement).Tag;
            this.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, 1, MouseButton.Left)
            {
                RoutedEvent = MouseLeftButtonUpEvent,
                Source = sender
            });
        }
    }
}
