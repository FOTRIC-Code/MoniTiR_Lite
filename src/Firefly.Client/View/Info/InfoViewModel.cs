using Firefly.Client.Control;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Firefly.Client.View.Info
{
    public class InfoViewModel : BaseViewModel
    {
        public InfoViewModel()
        {
            Infos = new ObservableCollection<InfoItemViewModel>();
        }

        public ObservableCollection<InfoItemViewModel> Infos { get; set; }
    }

    public class InfoItemViewModel : BaseViewModel
    {
        private string _title;
        private string _description;

        public InfoItemViewModel()
        {
        }

        public string Title
        {
            get => _title;
            set
            {
                _title = value;
                OnPropertyChanged();
            }
        }

        public string Description
        {
            get => _description;
            set
            {
                _description = value;
                OnPropertyChanged();
            }
        }
    }
}