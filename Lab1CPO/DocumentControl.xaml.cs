using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Media.Imaging;
using System.IO;
using Path = System.IO.Path;

namespace Lab1CPO
{
    public partial class DocumentControl : UserControl
    {
        private Point startPoint;
        private Shape currentShape;
        private bool isDrawing;

        public Tools CurrentTool { get; set; } = Tools.Pen;
        
        private Brush color = Brushes.Black;
        public Brush Color 
        { 
            get => color; 
            set 
            { 
                color = value; 
                if (currentShape != null) 
                {
                    currentShape.Stroke = color; 
                }
            } 
        }

        public double LineWidth { get; set; } = 1;

        public DocumentControl()
        {
            InitializeComponent();
        }

        public void SaveToFile(string fileName)
        {
            var bounds = VisualTreeHelper.GetDescendantBounds(DrawingCanvas);
            var renderTarget = new RenderTargetBitmap(
                (int)bounds.Width,
                (int)bounds.Height,
                96, 96, PixelFormats.Pbgra32);

            var drawingVisual = new DrawingVisual();
            using (var context = drawingVisual.RenderOpen())
            {
                var visualBrush = new VisualBrush(DrawingCanvas);
                context.DrawRectangle(visualBrush, null, bounds);
            }

            renderTarget.Render(drawingVisual);

            BitmapEncoder encoder = Path.GetExtension(fileName).ToLower() switch
            {
                ".jpg" => new JpegBitmapEncoder(),
                ".bmp" => new BmpBitmapEncoder(),
                _ => new PngBitmapEncoder()
            };

            encoder.Frames.Add(BitmapFrame.Create(renderTarget));

            using (var stream = File.Create(fileName))
            {
                encoder.Save(stream);
            }
        }

        private void DrawingCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            isDrawing = true;
            startPoint = e.GetPosition(DrawingCanvas);

            switch (CurrentTool)
            {
                case Tools.Pen:
                    currentShape = new Polyline
                    {
                        Stroke = Color,
                        StrokeThickness = LineWidth,
                        StrokeLineJoin = PenLineJoin.Round,
                        StrokeStartLineCap = PenLineCap.Round,
                        StrokeEndLineCap = PenLineCap.Round
                    };
                    ((Polyline)currentShape).Points.Add(startPoint);
                    break;

                case Tools.Circle:
                    currentShape = new Ellipse
                    {
                        Stroke = Color,
                        StrokeThickness = LineWidth,
                        Fill = Brushes.Transparent
                    };
                    Canvas.SetLeft(currentShape, startPoint.X);
                    Canvas.SetTop(currentShape, startPoint.Y);
                    break;
            }

            if (currentShape != null)
            {
                DrawingCanvas.Children.Add(currentShape);
            }
        }

        private void DrawingCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (!isDrawing || currentShape == null) return;

            var currentPoint = e.GetPosition(DrawingCanvas);

            switch (CurrentTool)
            {
                case Tools.Pen:
                    ((Polyline)currentShape).Points.Add(currentPoint);
                    break;

                case Tools.Circle:
                    var deltaX = currentPoint.X - startPoint.X;
                    var deltaY = currentPoint.Y - startPoint.Y;

                    Canvas.SetLeft(currentShape, Math.Min(startPoint.X, currentPoint.X));
                    Canvas.SetTop(currentShape, Math.Min(startPoint.Y, currentPoint.Y));

                    ((Ellipse)currentShape).Width = Math.Abs(deltaX);
                    ((Ellipse)currentShape).Height = Math.Abs(deltaY);
                    break;
            }
        }

        private void DrawingCanvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            isDrawing = false;
            currentShape = null;
        }

        private void DrawingCanvas_MouseLeave(object sender, MouseEventArgs e)
        {
            isDrawing = false;
            currentShape = null;
        }
    }
}