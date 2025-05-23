using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace WpfTileMap
{
    /// <summary>
    /// TileNode坐標系參照自BingMapTileSystem
    /// 使用Level23的長寬作為坐標系
    /// https://learn.microsoft.com/en-us/bingmaps/articles/bing-maps-tile-system
    /// </summary>
    internal class MapDrawingCanvas : FrameworkElement
    {
        readonly List<TileNode> Root = [];
        readonly double MapSize = 2147483648;
        readonly double MapSizeLevel = 23;
        /// <summary>
        /// 可視範圍
        /// </summary>
        readonly Boundary View = new();
        /// <summary>
        /// 螢幕中心地圖Pixel座標
        /// </summary>
        Point Center = new();
        /// <summary>
        /// 當前Level
        /// </summary>
        int Level = 1;
        /// <summary>
        /// 最小Level限制
        /// </summary>
        readonly int MinLevel = 1;
        /// <summary>
        /// 最大Level限制
        /// </summary>
        readonly int MaxLevel = 20;
        /// <summary>
        /// 當前需繪製的Node
        /// </summary>
        readonly List<TileNode> DrawNodes = [];
        /// <summary>
        /// 當前視窗大小
        /// </summary>
        Size WindowSize = new();

        public MapDrawingCanvas()
        {
            this.Loaded += (s, e) =>
            {
                this.WindowSize.Width = this.ActualWidth;
                this.WindowSize.Height = this.ActualHeight;
                this.UpdateView();

                double halfMapSize = this.MapSize / 2;
                this.Root.Add(new TileNode(this, 1, "0", new Boundary(0, 0, -halfMapSize, halfMapSize)));
                this.Root.Add(new TileNode(this, 1, "1", new Boundary(halfMapSize, 0, 0, halfMapSize)));
                this.Root.Add(new TileNode(this, 1, "2", new Boundary(0, -halfMapSize, -halfMapSize, 0)));
                this.Root.Add(new TileNode(this, 1, "3", new Boundary(halfMapSize, -halfMapSize, 0, 0)));
                foreach (var node in this.Root)
                {
                    _ = node.LoadImage();
                }
            };
        }

        public void UpdateDrawNodes()
        {
            this.DrawNodes.Clear();
            List<TileNode> list = [];
            foreach (var node in this.Root)
            {
                node.CollectLeaf(ref list);
            }

            foreach (var node in list)
            {
                if (node.IsVisible(this.View))
                {
                    this.DrawNodes.Add(node);
                }
            }
            this.InvalidateVisual();
        }

        /// <summary>
        /// 從螢幕中心放大
        /// </summary>
        public void ZoomIn()
        {
            this.Level = Math.Min(this.Level + 1, this.MaxLevel);
            this.UpdateView();
        }

        /// <summary>
        /// 從螢幕中心縮小
        /// </summary>
        public void ZoomOut()
        {
            this.Level = Math.Max(this.Level - 1, this.MinLevel);
            this.UpdateView();
        }

        /// <summary>
        /// 向指定座標放大
        /// </summary>
        /// <param name="pixelX"></param>
        /// <param name="pixelY"></param>
        public void ZoomIn(double pixelX, double pixelY)
        {
            this.Level = Math.Min(this.Level + 1, this.MaxLevel);
            double scale = this.GetOffsetScale();
            double offsetX = (pixelX - (this.WindowSize.Width / 2)) * scale;
            double offsetY = ((this.WindowSize.Height / 2) - pixelY) * scale;
            this.Center.Offset(offsetX, offsetY);
            this.UpdateView();
        }

        /// <summary>
        /// 向指定座標縮小
        /// </summary>
        /// <param name="pixelX"></param>
        /// <param name="pixelY"></param>
        public void ZoomOut(double pixelX, double pixelY)
        {
            double scale = this.GetOffsetScale();
            this.Level = Math.Max(this.Level - 1, this.MinLevel);
            double offsetX = (pixelX - (this.WindowSize.Width / 2)) * scale;
            double offsetY = ((this.WindowSize.Height / 2) - pixelY) * scale;
            this.Center.Offset(-offsetX, -offsetY);
            this.UpdateView();
        }

        /// <summary>
        /// 取得當前Level
        /// </summary>
        /// <returns></returns>
        public int GetLevel()
        {
            return this.Level;
        }

        /// <summary>
        /// 偏移
        /// </summary>
        /// <param name="offsetX"></param>
        /// <param name="offsetY"></param>
        public void Offset(double offsetX, double offsetY)
        {
            double scale = this.GetOffsetScale();
            this.Center.Offset(offsetX * scale, offsetY * scale);
            this.UpdateView();
        }

        /// <summary>
        /// 取得經緯度
        /// </summary>
        /// <param name="pixelX">x pixel座標</param>
        /// <param name="pixelY">y pixel座標</param>
        /// <returns></returns>
        public Point GetLonLat(double pixelX, double pixelY)
        {
            //邊界東、南、西、北
            //TileNode:   1073741824, -1073741824, -1073741824, 1073741824
            //TileSystem: 2147483648, 2147483648, 0, 0
            //需作正規化
            double halfMapSize = this.MapSize / 2;
            double x = this.View.West + (pixelX / this.WindowSize.Width) * this.View.Width + halfMapSize;
            double y = -(this.View.North - (pixelY / this.WindowSize.Height) * this.View.Height) + halfMapSize;
            TileSystem.PixelXYToLatLong((int)x, (int)y, 23, out double lat, out double lon);
            return new Point(lon, lat);
        }

        /// <summary>
        /// 更新樹狀結構
        /// </summary>
        public void AdjustTree()
        {
            foreach (var node in this.Root)
            {
                node.Collapse(this.Level, this.View);
            }
            this.UpdateDrawNodes();

            foreach (var node in this.Root)
            {
                node.Expand(this.Level, this.View);
            }
        }

        /// <summary>
        /// 更新可見範圍
        /// </summary>
        void UpdateView()
        {
            double halfWidth = this.WindowSize.Width / 2;
            double halfHeight = this.WindowSize.Height / 2;
            double scale = this.GetOffsetScale();
            this.View.Set(
                this.Center.X + (halfWidth * scale),
                this.Center.Y - (halfHeight * scale),
                this.Center.X - (halfWidth * scale),
                this.Center.Y + (halfHeight * scale)
            );
            this.AdjustTree();
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);

            this.WindowSize.Width = sizeInfo.NewSize.Width;
            this.WindowSize.Height = sizeInfo.NewSize.Height;
            this.UpdateView();
        }

        protected override void OnRender(DrawingContext dc)
        {
            foreach (var node in this.DrawNodes)
            {
                var img = node.GetImage();
                if (img != null)
                {
                    Rect rect = node.GetTileRect(this.View, this.WindowSize);
                    dc.DrawImage(img, rect);
                }
            }
        }

        double GetOffsetScale()
        {
            return Math.Pow(2, this.MapSizeLevel - this.Level);
        }
    }

    /// <summary>
    /// Tile節點
    /// </summary>
    class TileNode
    {
        /// <summary>
        /// 節點Level
        /// </summary>
        private int Level;
        /// <summary>
        /// BingMap QuadKey
        /// </summary>
        private string QuadKey;
        /// <summary>
        /// 節點邊界，Pixel座標(lv23)
        /// </summary>
        private readonly Boundary Boundary;
        /// <summary>
        /// 子節點
        /// </summary>
        private readonly List<TileNode> Children = [];
        /// <summary>
        /// 父節點
        /// </summary>
        private readonly TileNode? Parent = null;
        /// <summary>
        /// 圖片
        /// </summary>
        private BitmapImage? Image = null;
        /// <summary>
        /// 用於取消請求
        /// </summary>
        private readonly CancellationTokenSource CancellationToken = new CancellationTokenSource();
        /// <summary>
        /// 當前狀態
        /// </summary>
        private NODE_STATUS Status = NODE_STATUS.NONE;
        private readonly MapDrawingCanvas Canvas;

        public TileNode(MapDrawingCanvas canvas, int level, string quadKey, Boundary boundary)
        {
            this.Canvas = canvas;
            this.Level = level;
            this.QuadKey = quadKey;
            this.Boundary = boundary;
        }

        public TileNode(MapDrawingCanvas canvas, TileNode parent, int level, string quadKey, Boundary boundary)
        {
            this.Canvas = canvas;
            this.Level = level;
            this.QuadKey = quadKey;
            this.Boundary = boundary;
            this.Parent = parent;
        }

        /// <summary>
        /// 此節點在view範圍內是否可見
        /// </summary>
        /// <param name="view"></param>
        /// <returns></returns>
        public bool IsVisible(Boundary view)
        {
            return this.Boundary.IsIntersect(view);
        }

        /// <summary>
        /// 分裂節點
        /// </summary>
        /// <param name="level"></param>
        /// <param name="view"></param>
        public void Expand(int level, Boundary view)
        {
            if (this.Status < NODE_STATUS.IMAGE_LOADED || this.Level >= level || !this.Boundary.IsIntersect(view))
            {
                return;
            }

            if (this.Status == NODE_STATUS.IMAGE_LOADED)
            {
                this.Status = NODE_STATUS.LOADING_CHILDREN;
                double halfWidth = this.Boundary.Width / 2;
                double halfHeight = this.Boundary.Height / 2;

                //QuadKey參照BingMapTileSystem
                this.Children.Add(
                    new TileNode(this.Canvas, this, this.Level + 1, $"{this.QuadKey}0",
                    new Boundary(this.Boundary.West + halfWidth, this.Boundary.North - halfHeight, this.Boundary.West, this.Boundary.North))
                );
                this.Children.Add(
                    new TileNode(this.Canvas, this, this.Level + 1, $"{this.QuadKey}1",
                    new Boundary(this.Boundary.East, this.Boundary.North - halfHeight, this.Boundary.East - halfWidth, this.Boundary.North))
                );
                this.Children.Add(
                    new TileNode(this.Canvas, this, this.Level + 1, $"{this.QuadKey}2",
                    new Boundary(this.Boundary.West + halfWidth, this.Boundary.South, this.Boundary.West, this.Boundary.South + halfHeight))
                );
                this.Children.Add(
                    new TileNode(this.Canvas, this, this.Level + 1, $"{this.QuadKey}3",
                    new Boundary(this.Boundary.East, this.Boundary.South, this.Boundary.East - halfWidth, this.Boundary.South + halfHeight))
                );

                foreach (var node in this.Children)
                {
                    _ = node.LoadImage();
                }
            }
            else if (this.Status == NODE_STATUS.CHILDREN_LOADED)
            {
                foreach (var node in this.Children)
                {
                    node.Expand(level, view);
                }
            }
        }

        /// <summary>
        /// 收斂節點
        /// </summary>
        /// <param name="level"></param>
        /// <param name="view"></param>
        public void Collapse(int level, Boundary view)
        {
            if (!this.Boundary.IsIntersect(view) || this.Level == level)
            {
                foreach (var node in this.Children)
                {
                    node.Abort();
                }
                this.Children.Clear();
                this.Status = NODE_STATUS.IMAGE_LOADED;
            }
            else
            {
                foreach (var node in this.Children)
                {
                    node.Collapse(level, view);
                }
            }
        }

        public BitmapImage? GetImage()
        {
            return this.Image;
        }

        /// <summary>
        /// 收集所有葉節點
        /// </summary>
        /// <param name="list"></param>
        public void CollectLeaf(ref List<TileNode> list)
        {
            if (this.Status == NODE_STATUS.IMAGE_LOADED || this.Status == NODE_STATUS.LOADING_CHILDREN)
            {
                list.Add(this);
            }
            else
            {
                foreach (var node in this.Children)
                {
                    node.CollectLeaf(ref list);
                }
            }
        }

        /// <summary>
        /// 讀取圖片
        /// </summary>
        /// <returns></returns>
        public async Task LoadImage()
        {
            if (this.Status == NODE_STATUS.LOADING_IMAGE)
            {
                Debug.WriteLine("asdasdasdasd");
            }

            try
            {
                this.Status = NODE_STATUS.LOADING_IMAGE;
                using HttpClient client = new HttpClient();
                byte[] data = await client.GetByteArrayAsync($"https://ecn.t1.tiles.virtualearth.net/tiles/r{this.QuadKey}?g=3649", CancellationToken.Token);

                using MemoryStream ms = new MemoryStream(data);
                BitmapImage bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.StreamSource = ms;
                bmp.EndInit();
                bmp.Freeze(); // 可跨執行緒使用

                this.Image = bmp;
                this.Status = NODE_STATUS.IMAGE_LOADED;

                if (this.Parent != null)
                {
                    this.Parent.ChildLoaded();
                }
                else
                {
                    this.Canvas.UpdateDrawNodes();
                }
            }
            catch (TaskCanceledException)
            {
                this.Status = NODE_STATUS.NONE;
                Console.WriteLine($"請求已被取消");
            }
            catch (Exception ex)
            {
                this.Status = NODE_STATUS.NONE;
                Console.WriteLine($"圖片載入失敗: {ex.Message}");
            }
        }

        /// <summary>
        /// 取得此節點在左上點相對於視窗原點的Pixel Rext
        /// </summary>
        /// <param name="view"></param>
        /// <param name="window"></param>
        /// <returns></returns>
        public Rect GetTileRect(Boundary view, Size window)
        {
            double topLeftX = ((this.Boundary.West - view.West) / view.Width) * window.Width;
            double topLeftY = ((view.North - this.Boundary.North) / view.Height) * window.Height;

            double bottomRightX = ((this.Boundary.East - view.West) / view.Width) * window.Width;
            double bottomRightY = ((view.North - this.Boundary.South) / view.Height) * window.Height;
            return new Rect(new Point(topLeftX, topLeftY), new Point(bottomRightX, bottomRightY));
        }

        public int GetLevel()
        {
            return this.Level;
        }

        /// <summary>
        /// 子節點讀取完成後呼叫
        /// </summary>
        void ChildLoaded()
        {
            if (this.Children.All(node => node.GetImage() != null))
            {
                this.Status = NODE_STATUS.CHILDREN_LOADED;
                this.Canvas.AdjustTree();
            }
        }

        /// <summary>
        /// 終止此節點下的所有請求
        /// </summary>
        void Abort()
        {
            if (this.Status == NODE_STATUS.LOADING_IMAGE)
            {
                this.CancellationToken.Cancel();
            }

            foreach (var node in this.Children)
            {
                node.Abort();
            }
        }
    }

    enum NODE_STATUS
    {
        NONE = 0,
        LOADING_IMAGE = 1,
        IMAGE_LOADED = 2,
        LOADING_CHILDREN = 3,
        CHILDREN_LOADED = 4
    }
}
