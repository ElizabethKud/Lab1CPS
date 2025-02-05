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
using Xceed.Wpf.AvalonDock; 
using Xceed.Wpf.AvalonDock.Layout;
using Fluent;
using Button = System.Windows.Controls.Button;

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
            CommandBindings.Add(new CommandBinding(ApplicationCommands.Save, SaveFile_Click, SaveFile_CanExecute));
            CommandBindings.Add(new CommandBinding(ApplicationCommands.SaveAs, SaveFileAs_Click, SaveFile_CanExecute));
            CommandBindings.Add(new CommandBinding(ApplicationCommands.Close, CloseTab_Click, CloseTab_CanExecute));
        }
        
        private void SaveFile_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = DocumentPane != null && DocumentPane.Children?.OfType<LayoutDocument>().Any() == true;
        }

        private void CloseTab_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = DocumentPane != null && DocumentPane.Children.OfType<LayoutDocument>().Any();
        }


        private void NewCanvas_Click(object sender, RoutedEventArgs e) => OpenNewCanvas();

        private void OpenNewCanvas()
        {
            LayoutDocument doc = new LayoutDocument { Title = "Новый холст" };
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

            doc.Content = grid;
            DocumentPane.Children.Add(doc);
        }

        private void OpenFile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OpenFileDialog openFileDialog = new OpenFileDialog { Filter = "Изображения|*.bmp;*.jpg;*.png" };
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
            LayoutDocument doc = new LayoutDocument { Title = Path.GetFileName(filePath) };
            Image image = new Image { Source = new BitmapImage(new Uri(filePath)), Stretch = Stretch.None };
            Canvas drawingCanvas = new Canvas { Background = Brushes.Transparent, Width = image.Source.Width, Height = image.Source.Height };

            drawingCanvas.MouseDown += Canvas_MouseDown;
            drawingCanvas.MouseMove += Canvas_MouseMove;
            drawingCanvas.MouseUp += Canvas_MouseUp;

            Grid grid = new Grid();
            grid.Children.Add(image);
            grid.Children.Add(drawingCanvas);

            doc.Content = new ScrollViewer { Content = grid };
            DocumentPane.Children.Add(doc);
            doc.IsSelected = true;
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
            if (DocumentPane.SelectedContent is LayoutDocument doc && doc.Content is ScrollViewer viewer && viewer.Content is Canvas canvas)
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog { Filter = "BMP Files|*.bmp|JPEG Files|*.jpg" };
                if (saveFileDialog.ShowDialog() == true)
                {
                    SaveImage(canvas, saveFileDialog.FileName);
                }
            }
        }

        private void SaveFileAs_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (DocumentPane.SelectedContent is LayoutDocument selectedDoc &&
                    selectedDoc.Content is ScrollViewer viewer &&
                    viewer.Content is Canvas canvas)
                {
                    SaveFileDialog saveFileDialog = new SaveFileDialog { Filter = "BMP Files|*.bmp|JPEG Files|*.jpg" };
                    if (saveFileDialog.ShowDialog() == true)
                    {
                        SaveImage(canvas, saveFileDialog.FileName);
                    }
                }
                else
                {
                    MessageBox.Show("Нет активного документа для сохранения.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении файла: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void SaveImage(Canvas canvas, string filePath)
        {
            RenderTargetBitmap renderBitmap = new RenderTargetBitmap((int)canvas.ActualWidth, (int)canvas.ActualHeight, 96d, 96d, PixelFormats.Pbgra32);
            canvas.Measure(new Size(canvas.ActualWidth, canvas.ActualHeight));
            canvas.Arrange(new Rect(new Size(canvas.ActualWidth, canvas.ActualHeight)));
            renderBitmap.Render(canvas);

            BitmapEncoder encoder = Path.GetExtension(filePath).ToLower() == ".bmp" ? new BmpBitmapEncoder() : new JpegBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(renderBitmap));

            using (var stream = File.Create(filePath))
            {
                encoder.Save(stream);
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
                if (DocumentPane.SelectedContent is LayoutDocument selectedDoc)
                {
                    DocumentPane.Children.Remove(selectedDoc);
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
            double offsetX = 30, offsetY = 30;
            int index = 0;

            // Создание списка из элементов, которые нужно обработать
            var documents = DocumentPane.Children.OfType<LayoutDocument>().ToList();

            foreach (var doc in documents)
            {
                doc.Float();
                doc.FloatingLeft = offsetX * index;
                doc.FloatingTop = offsetY * index;
                index++;
            }
        }
        
        private void TileWindows_Click(object sender, RoutedEventArgs e)
        {
            var documents = DocumentPane.Children.OfType<LayoutDocument>().ToList();
            int count = documents.Count;

            if (count == 0)
                return;

            // Получаем DockingManager
            var dockingManager = FindVisualParent<DockingManager>(DocumentPane);
            if (dockingManager == null)
                return;

            // Получаем размеры рабочей области DockingManager
            double paneWidth = dockingManager.ActualWidth;
            double paneHeight = dockingManager.ActualHeight;

            // Определяем количество строк и столбцов для сетки
            int rows = (int)Math.Ceiling(Math.Sqrt(count));
            int cols = (int)Math.Ceiling((double)count / rows);

            // Размеры каждой ячейки сетки
            double cellWidth = paneWidth / cols;
            double cellHeight = paneHeight / rows;

            for (int i = 0; i < count; i++)
            {
                var doc = documents[i];

                // Вычисляем позицию и размер для каждого окна
                int row = i / cols;
                int col = i % cols;

                // Делаем окно плавающим
                doc.Float();

                // Устанавливаем положение и размер плавающего окна
                doc.FloatingLeft = col * cellWidth;
                doc.FloatingTop = row * cellHeight;
                doc.FloatingWidth = Math.Max(cellWidth, 100); // Минимальная ширина 100
                doc.FloatingHeight = Math.Max(cellHeight, 100); // Минимальная высота 100
            }
        }

        // Вспомогательный метод для поиска DockingManager
        private T FindVisualParent<T>(DependencyObject child) where T : DependencyObject
        {
            var parentObject = VisualTreeHelper.GetParent(child);
            if (parentObject == null)
                return null;

            if (parentObject is T parent)
                return parent;

            return FindVisualParent<T>(parentObject);
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
            foreach (var doc in DocumentPane.Children.OfType<LayoutDocument>())
            {
                if (doc.Content is ScrollViewer scrollViewer && scrollViewer.Content is Canvas canvas)
                {
                    canvas.Width = scrollViewer.ActualWidth;
                    canvas.Height = scrollViewer.ActualHeight;
                }
            }
        }

        private void UpdateCommandsState()
        {
            bool hasDocuments = DocumentPane.Children.OfType<LayoutDocument>().Any();
            SaveMenuItem.IsEnabled = hasDocuments;
            SaveAsMenuItem.IsEnabled = hasDocuments;
            SaveButton.IsEnabled = hasDocuments;
            SaveAsButton.IsEnabled = hasDocuments;
            CloseMenuItem.IsEnabled = hasDocuments;
            CloseButton.IsEnabled = hasDocuments;
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