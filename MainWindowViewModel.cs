using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WpfTileMap
{
    internal class MainWindowViewModel : INotifyPropertyChanged
    {
        private string _lonLatText = "";
        public string LonLatText
        {
            get
            {
                return _lonLatText;
            }
            set
            {
                _lonLatText = value;
                this.OnPropertyChanged();
            }
        }

        private string _levelText = "Test";
        public string LevelText
        {
            get
            {
                return _levelText;
            }
            set
            {
                _levelText = value;
                this.OnPropertyChanged();
            }
        }


        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }
    }
}
