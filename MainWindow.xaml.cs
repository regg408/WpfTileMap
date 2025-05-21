using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WpfTileMap
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        bool IsPress = false;
        Point LastMousePos = new();

        public MainWindow()
        {
            InitializeComponent();
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
            if (e.Delta > 0)
            {
                this.TileMapCanvas.ZoomIn();
            }
            else
            {
                this.TileMapCanvas.ZoomOut();
            }
        }

        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            Point currentPos = e.GetPosition(this.TileMapCanvas);
            if (this.IsPress)
            {
                this.TileMapCanvas.Offset(this.LastMousePos.X - currentPos.X, currentPos.Y - this.LastMousePos.Y);
            }
            this.LastMousePos = currentPos;
        }
    }
}