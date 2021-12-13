using Amur.Common;

namespace Firefly.Client
{
    public class CustomSoftwareViewModel : BaseViewModel
    {
        private string _softwareIconName;
        private string _softwareIcon;
        private string _softwareName;
        private int _fontSize = 16;

        public CustomSoftwareViewModel()
        {
        }

        public string SoftwareIconName
        {
            get => _softwareIconName;
            set
            {
                _softwareIconName = value;
                OnPropertyChanged();
            }
        }

        public string SoftwareIcon
        {
            get => _softwareIcon;
            set
            {
                _softwareIcon = value;
                OnPropertyChanged();
            }
        }

        public string SoftwareName
        {
            get => _softwareName;
            set
            {
                _softwareName = value;
                OnPropertyChanged();
            }
        }

        public int FontSize
        {
            get => _fontSize;
            set
            {
                if (value <= 0)
                {
                    _fontSize = 16;
                }
                else
                {
                    _fontSize = value;
                }
                OnPropertyChanged();
            }
        }
    }
}