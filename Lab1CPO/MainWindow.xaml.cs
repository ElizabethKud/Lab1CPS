using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;

namespace Lab1CPO
{
    public partial class MainWindow : Window
    {
        public static readonly RoutedCommand New = new RoutedCommand();
        public static readonly RoutedCommand Save = new RoutedCommand();

        private Color currentColor = Colors.Black;
        private double currentLineWidth = 1;

        public MainWindow()
        {
            InitializeComponent();
    
            CommandBindings.Add(new CommandBinding(New, New_Executed));
            CommandBindings.Add(new CommandBinding(Save, Save_Executed));
    
            New.InputGestures.Add(new KeyGesture(Key.N, ModifierKeys.Control));
            Save.InputGestures.Add(new KeyGesture(Key.S, ModifierKeys.Control));
    
            // Создаем первый документ
            NewDocument();
    
            // Временно отключаем обработчик для инициализации ComboBox
            lineWidthCombo.SelectionChanged -= LineWidthCombo_SelectionChanged;
            lineWidthCombo.SelectedIndex = 0;
            lineWidthCombo.SelectionChanged += LineWidthCombo_SelectionChanged;
    
            UpdateToolButtons();
        }

        private void NewDocument()
        {
            var doc = new DocumentControl
            {
                Color = new SolidColorBrush(currentColor),
                LineWidth = currentLineWidth
            };

            var tabItem = new TabItem
            {
                Header = $"Документ {DocumentsTabControl.Items.Count + 1}",
                Content = doc,
                Tag = doc
            };

            DocumentsTabControl.Items.Add(tabItem);
            DocumentsTabControl.SelectedItem = tabItem;
        }

        private DocumentControl CurrentDocument => 
            (DocumentsTabControl.SelectedItem as TabItem)?.Content as DocumentControl;

        private void New_Executed(object sender, ExecutedRoutedEventArgs e) => NewDocument();

        private void Save_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var saveDialog = new SaveFileDialog
            {
                Filter = "PNG Image (*.png)|*.png|JPEG Image (*.jpg)|*.jpg|BMP Image (*.bmp)|*.bmp"
            };

            if (saveDialog.ShowDialog() == true && CurrentDocument != null)
            {
                CurrentDocument.SaveToFile(saveDialog.FileName);
            }
        }

        private void Exit_Click(object sender, RoutedEventArgs e) => Close();

        private void About_Click(object sender, RoutedEventArgs e)
        {
            var about = new AboutWindow { Owner = this };
            about.ShowDialog();
        }

        private void ColorPicker_Click(object sender, RoutedEventArgs e)
        {
            var colorDialog = new System.Windows.Forms.ColorDialog();
            if (colorDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                currentColor = Color.FromArgb(
                    colorDialog.Color.A,
                    colorDialog.Color.R,
                    colorDialog.Color.G,
                    colorDialog.Color.B);

                if (CurrentDocument != null)
                {
                    CurrentDocument.Color = new SolidColorBrush(currentColor);
                }
            }
        }

        private void Tool_Pen_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentDocument != null)
            {
                CurrentDocument.CurrentTool = Tools.Pen;
                UpdateToolButtons();
            }
        }

        private void Tool_Circle_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentDocument != null)
            {
                CurrentDocument.CurrentTool = Tools.Circle;
                UpdateToolButtons();
            }
        }

        private void UpdateToolButtons()
        {
            if (CurrentDocument == null) return;
            
            penButton.IsChecked = CurrentDocument.CurrentTool == Tools.Pen;
            circleButton.IsChecked = CurrentDocument.CurrentTool == Tools.Circle;
        }

        private void DocumentsTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CurrentDocument == null) return;

            statusLabelSize.Text = $"{CurrentDocument.ActualWidth:F0}x{CurrentDocument.ActualHeight:F0}";
            CurrentDocument.MouseMove += Document_MouseMove;
            UpdateToolButtons();
        }

        private void Document_MouseMove(object sender, MouseEventArgs e)
        {
            var pos = e.GetPosition(CurrentDocument);
            statusLabelPosition.Text = $"X: {(int)pos.X} Y: {(int)pos.Y}";
        }

        private void CloseTab_Click(object sender, RoutedEventArgs e)
        {
            if (((Button)sender).Tag is TabItem tabItem && DocumentsTabControl.Items.Contains(tabItem))
            {
                DocumentsTabControl.Items.Remove(tabItem);
                if (DocumentsTabControl.Items.Count > 0)
                {
                    DocumentsTabControl.SelectedIndex = 0;
                    statusLabelSize.Text = $"{CurrentDocument.ActualWidth:F0}x{CurrentDocument.ActualHeight:F0}";
                }
                else
                {
                    statusLabelSize.Text = "---";
                    statusLabelPosition.Text = "X: -- Y:--";
                }
                UpdateToolButtons();
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (DocumentsTabControl.Items.Count > 0)
            {
                var result = MessageBox.Show(
                    "Сохранить изменения перед выходом?",
                    "Выход",
                    MessageBoxButton.YesNoCancel);

                if (result == MessageBoxResult.Cancel)
                {
                    e.Cancel = true;
                }
                else if (result == MessageBoxResult.Yes)
                {
                    Save_Executed(this, null);
                }
            }
        }

        private void LineWidthCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CurrentDocument == null || lineWidthCombo.SelectedItem == null) return;

            if (lineWidthCombo.SelectedItem is ComboBoxItem item &&
                double.TryParse(item.Content.ToString().Replace("px", ""), out double width))
            {
                currentLineWidth = width;
                CurrentDocument.LineWidth = currentLineWidth;
            }
        }

        private void Cascade_Click(object sender, RoutedEventArgs e) { /* Реализация MDI */ }
        private void TileHorizontal_Click(object sender, RoutedEventArgs e) { /* Реализация MDI */ }
        private void ArrangeIcons_Click(object sender, RoutedEventArgs e) { /* Реализация MDI */ }
    }
}