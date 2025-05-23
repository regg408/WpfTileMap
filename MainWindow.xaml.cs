using System.Windows;
using System.Windows.Input;

namespace WpfTileMap
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        bool IsPress = false;
        Point LastMousePos = new();
        MainWindowViewModel ViewModel = new();

        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = this.ViewModel;
            this.ViewModel.LevelText = $"Level: {this.TileMapCanvas.GetLevel()}";
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            this.IsPress = true;
        }

        private void Window_MouseUp(object sender, MouseButtonEventArgs e)
        {
            this.IsPress = false;
        }

        private void Window_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            Point currentPos = e.GetPosition(this.TileMapCanvas);
            if (e.Delta > 0)
            {
                this.TileMapCanvas.ZoomIn(currentPos.X, currentPos.Y);
            }
            else
            {
                this.TileMapCanvas.ZoomOut(currentPos.X, currentPos.Y);
            }
            this.ViewModel.LevelText = $"Level: {this.TileMapCanvas.GetLevel()}";
        }

        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            Point currentPos = e.GetPosition(this.TileMapCanvas);
            if (this.IsPress)
            {
                this.TileMapCanvas.Offset(this.LastMousePos.X - currentPos.X, currentPos.Y - this.LastMousePos.Y);
            }
            this.LastMousePos = currentPos;
            Point lonLat = this.TileMapCanvas.GetLonLat(currentPos.X, currentPos.Y);
            this.ViewModel.LonLatText = $"{lonLat.X}, {lonLat.Y}";
        }
    }
}