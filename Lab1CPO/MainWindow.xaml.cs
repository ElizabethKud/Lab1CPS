using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.Win32;
using Path = System.IO.Path;

namespace Lab1CPO
{
    public partial class MainWindow : Window
    {
        private bool isDrawing = false;
        private Point lastPoint;
        private SolidColorBrush penBrush = Brushes.Black;
        private double penThickness = 2;
        private Border selectedColorBorder = null;
        private Canvas activeCanvas = null;
        private string currentFilePath = null;

        public MainWindow()
        {
            InitializeComponent();
            InitializeShortcuts();
            InitializeDefaultSelection();
            UpdateCommandsState();
            SizeChanged += MainWindow_SizeChanged;
        }

        private void InitializeShortcuts()
        {
            CommandBindings.Add(new CommandBinding(ApplicationCommands.New, NewCanvas_Click));
            CommandBindings.Add(new CommandBinding(ApplicationCommands.Open, OpenFile_Click));
            CommandBindings.Add(new CommandBinding(ApplicationCommands.Save, SaveFile_Click));
            CommandBindings.Add(new CommandBinding(ApplicationCommands.SaveAs, SaveFileAs_Click));
            CommandBindings.Add(new CommandBinding(ApplicationCommands.Close, CloseTab_Click));
        }

        private void NewCanvas_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OpenNewCanvas();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при создании нового холста: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OpenNewCanvas()
        {
            TabItem tabItem = new TabItem { Header = "Новый холст" };
            Grid grid = new Grid();

            Canvas drawingCanvas = new Canvas
            {
                Background = Brushes.White,
                Width = 800,
                Height = 600
            };

            grid.Children.Add(drawingCanvas);

            drawingCanvas.MouseDown += Canvas_MouseDown;
            drawingCanvas.MouseMove += Canvas_MouseMove;
            drawingCanvas.MouseUp += Canvas_MouseUp;

            ScrollViewer scrollViewer = new ScrollViewer { Content = grid };
            tabItem.Content = scrollViewer;
            tabItem.Tag = drawingCanvas;

            ImageTabs.Items.Add(tabItem);
            ImageTabs.SelectedItem = tabItem;
            UpdateCommandsState();
        }

        private void OpenFile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OpenFileDialog openFileDialog = new OpenFileDialog { Filter = "Image Files|*.bmp;*.jpg;*.png" };
                if (openFileDialog.ShowDialog() == true)
                {
                    OpenImageTab(openFileDialog.FileName);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при открытии файла: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OpenImageTab(string filePath)
        {
            TabItem tabItem = new TabItem { Header = Path.GetFileName(filePath) };
            Grid grid = new Grid();

            Image image = new Image
            {
                Source = new BitmapImage(new Uri(filePath)),
                Stretch = Stretch.None
            };

            Canvas drawingCanvas = new Canvas
            {
                Background = Brushes.Transparent,
                Width = image.Source.Width,
                Height = image.Source.Height
            };

            grid.Children.Add(image);
            grid.Children.Add(drawingCanvas);

            drawingCanvas.MouseDown += Canvas_MouseDown;
            drawingCanvas.MouseMove += Canvas_MouseMove;
            drawingCanvas.MouseUp += Canvas_MouseUp;

            ScrollViewer scrollViewer = new ScrollViewer { Content = grid };
            tabItem.Content = scrollViewer;
            tabItem.Tag = drawingCanvas;

            ImageTabs.Items.Add(tabItem);
            ImageTabs.SelectedItem = tabItem;
            currentFilePath = filePath;
            UpdateCommandsState();
        }

        private void Canvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Canvas canvas)
            {
                isDrawing = true;
                lastPoint = e.GetPosition(canvas);
                activeCanvas = canvas;
            }
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDrawing && activeCanvas != null)
            {
                Point currentPoint = e.GetPosition(activeCanvas);
                Line line = new Line
                {
                    Stroke = penBrush,
                    StrokeThickness = penThickness,
                    X1 = lastPoint.X,
                    Y1 = lastPoint.Y,
                    X2 = currentPoint.X,
                    Y2 = currentPoint.Y
                };
                activeCanvas.Children.Add(line);
                lastPoint = currentPoint;
            }
        }

        private void Canvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            isDrawing = false;
            activeCanvas = null;
        }

        private void SaveFile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (currentFilePath == null)
                {
                    SaveFileAs_Click(sender, e);
                }
                else
                {
                    SaveImage(currentFilePath);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении файла: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveFileAs_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog { Filter = "BMP Files|*.bmp|JPEG Files|*.jpg" };
                if (saveFileDialog.ShowDialog() == true)
                {
                    currentFilePath = saveFileDialog.FileName;
                    SaveImage(currentFilePath);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении файла: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveImage(string filePath)
        {
            if (ImageTabs.SelectedItem is TabItem selectedTab && selectedTab.Content is ScrollViewer scrollViewer && scrollViewer.Content is Grid grid)
            {
                RenderTargetBitmap renderBitmap = new RenderTargetBitmap((int)grid.ActualWidth, (int)grid.ActualHeight, 96d, 96d, PixelFormats.Pbgra32);
                grid.Measure(new Size(grid.ActualWidth, grid.ActualHeight));
                grid.Arrange(new Rect(new Size(grid.ActualWidth, grid.ActualHeight)));
                renderBitmap.Render(grid);
                BitmapEncoder encoder = Path.GetExtension(filePath).ToLower() == ".bmp" ? new BmpBitmapEncoder() : new JpegBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(renderBitmap));
                using (var stream = File.Create(filePath))
                {
                    encoder.Save(stream);
                }
            }
        }

        private void ChangePenColor_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string colorName)
            {
                penBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorName));

                if (selectedColorBorder != null)
                {
                    selectedColorBorder.BorderThickness = new Thickness(0);
                }

                if (button.Parent is Border border)
                {
                    border.BorderThickness = new Thickness(3);
                    border.BorderBrush = Brushes.Black;
                    selectedColorBorder = border;
                }
            }
        }

        private void ChangePenThickness_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (double.TryParse(PenThicknessInput.Text, out double thickness))
            {
                penThickness = Math.Clamp(thickness, 1, 20);
            }
        }

        private void CloseTab_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (ImageTabs.SelectedItem is TabItem selectedTab)
                {
                    ImageTabs.Items.Remove(selectedTab);
                    UpdateCommandsState();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при закрытии вкладки: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CascadeWindows_Click(object sender, RoutedEventArgs e)
        {
            double offsetX = 30;
            double offsetY = 30;
            int index = 0;
            foreach (TabItem tab in ImageTabs.Items)
            {
                if (tab.Content is ScrollViewer scrollViewer)
                {
                    scrollViewer.Margin = new Thickness(offsetX * index, offsetY * index, 0, 0);
                    index++;
                }
            }
        }

        private void TileWindows_Click(object sender, RoutedEventArgs e)
        {
            int count = ImageTabs.Items.Count;
            if (count == 0) return;
            int rows = (int)Math.Ceiling(Math.Sqrt(count));
            int cols = (int)Math.Ceiling((double)count / rows);
            double tileWidth = this.ActualWidth / cols;
            double tileHeight = this.ActualHeight / rows;
            int index = 0;
            foreach (TabItem tab in ImageTabs.Items)
            {
                if (tab.Content is ScrollViewer scrollViewer)
                {
                    int row = index / cols;
                    int col = index % cols;
                    scrollViewer.Width = tileWidth;
                    scrollViewer.Height = tileHeight;
                    scrollViewer.Margin = new Thickness(tileWidth * col, tileHeight * row, 0, 0);
                    index++;
                }
            }
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                "Программа: MDI Редактор Изображений\n" +
                "Версия: 1.0\n" +
                "Описание: Позволяет создавать, загружать, редактировать изображения. Поддерживается рисование пером, выбор цвета, толщина линии.\n" +
                "Форматы: BMP, JPG\n" +
                "Горячие клавиши:\n" +
                "   Ctrl+N - Новый холст\n" +
                "   Ctrl+O - Открыть изображение\n" +
                "   Ctrl+S - Сохранить\n" +
                "   Ctrl+Shift+S - Сохранить как\n" +
                "   Ctrl+W - Закрыть вкладку\n" +
                "   Alt+F4 - Выход\n" +
                "Использование:\n" +
                "   1. Выберите 'Новый холст' или 'Открыть' для загрузки изображения.\n" +
                "   2. Используйте инструменты рисования: изменяйте цвет и толщину пера.\n" +
                "   3. Сохраняйте работу в BMP или JPG.\n" +
                "   4. Используйте опции 'Каскад' и 'Рядом' для удобного расположения окон.",
                "О программе", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ImageTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateCommandsState();
        }
        
        private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            foreach (TabItem tab in ImageTabs.Items)
            {
                if (tab.Content is ScrollViewer scrollViewer && scrollViewer.Content is Grid grid)
                {
                    foreach (UIElement child in grid.Children)
                    {
                        if (child is Canvas canvas)
                        {
                            canvas.Width = scrollViewer.ActualWidth;
                            canvas.Height = scrollViewer.ActualHeight;
                        }
                    }
                }
            }
        }

        private void UpdateCommandsState()
        {
            bool hasTabs = ImageTabs.Items.Count > 0;
            SaveMenuItem.IsEnabled = hasTabs;
            SaveAsMenuItem.IsEnabled = hasTabs;
            SaveButton.IsEnabled = hasTabs;
            SaveAsButton.IsEnabled = hasTabs;
            CloseMenuItem.IsEnabled = hasTabs;
            CloseButton.IsEnabled = hasTabs;
        }

        private void InitializeDefaultSelection()
        {
            if (PenColorPanel == null) return;
            foreach (UIElement element in PenColorPanel.Children)
            {
                if (element is Border border && border.Child is Button button && button.Tag as string == "Black")
                {
                    border.BorderThickness = new Thickness(3);
                    border.BorderBrush = Brushes.Black;
                    selectedColorBorder = border;
                    break;
                }
            }
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}