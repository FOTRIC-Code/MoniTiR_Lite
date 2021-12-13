using Firefly.Client.View.Info;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Firefly.Client
{
    /// <summary>
    /// Interaction logic for InfoWindow.xaml
    /// </summary>
    public partial class InfoWindow : Window
    {
        public InfoWindow()
        {
            ViewModel = new InfoViewModel();
            DataContext = this;
            InitializeComponent();
        }

        public InfoViewModel ViewModel { get; }

        public void AddItem(string title, string description)
        {
            ViewModel.Infos.Add(new InfoItemViewModel
            {
                Title = title,
                Description = description
            });
        }

        private void btnCopyInfo_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is InfoItemViewModel viewModel)
            {
                Clipboard.SetText(viewModel.Description);
            }
        }
    }
}