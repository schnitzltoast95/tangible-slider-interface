using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;

namespace TSI
{
    /// <summary>
    /// Interaktionslogik für ConditionSetupWindow.xaml
    /// </summary>
    public partial class ConditionSetupWindow : Window
    {
        public List<string> Conditions { get; private set; }
        private int conditionCount = 1;

        public ConditionSetupWindow()
        {
            InitializeComponent();
            Conditions = new List<string>();
        }

        public void SetConditions(List<string> existingConditions)
        {
            ConditionPanel.Children.Clear();
            Conditions.Clear();

            Conditions = new List<string>(existingConditions);

            for (int i = 0; i < Conditions.Count; i++)
            {
                AddConditionToPanel(Conditions[i], i + 1);
            }

            conditionCount = Conditions.Count;

            if (Conditions.Count == 0)
            {
                AddConditionToPanel("", 1);
                conditionCount = 1;
            }
        }

        private void AddConditionButton_Click(object sender, RoutedEventArgs e)
        {
            conditionCount++;
            AddConditionToPanel("", conditionCount);
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            Conditions.Clear();

            foreach (UIElement element in ConditionPanel.Children)
            {
                if (element is StackPanel panel)
                {
                    foreach (UIElement child in panel.Children)
                    {
                        if (child is TextBox textBox && !string.IsNullOrWhiteSpace(textBox.Text))
                        {
                            Conditions.Add(textBox.Text.Trim());
                        }
                    }
                }
            }

            DialogResult = true;
        }

        private void AddConditionToPanel(string conditionName, int conditionNumber)
        {
            StackPanel newConditionPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 10, 10)
            };

            TextBlock conditionLabel = new TextBlock
            {
                Text = "Condition " + conditionNumber,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 10, 0)
            };

            TextBox conditionTextBox = new TextBox
            {
                Width = 150,
                Height = 30,
                Name = "ConditionTextBox" + conditionNumber,
                Text = conditionName,
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D0C5B4")),
                Padding = new Thickness(5, 5, 0, 5)
            };
            
            Button deleteButton = new Button
            {
                Content = "🗑",
                Width = 30,
                Height = 30,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#AE4F4F")),
                Foreground = Brushes.White,
                Margin = new Thickness(10, 0, 0, 0)
            };
            
            deleteButton.Tag = newConditionPanel;
            deleteButton.Click += OnDeleteConditionClicked;

            newConditionPanel.Children.Add(conditionLabel);
            newConditionPanel.Children.Add(conditionTextBox);
            newConditionPanel.Children.Add(deleteButton);

            ConditionPanel.Children.Add(newConditionPanel);
        }

        private void OnDeleteConditionClicked(object sender, RoutedEventArgs e)
        {
            if (sender is Button deleteButton)
            {
                var associatedPanel = deleteButton.Tag as StackPanel;
                if (associatedPanel != null && ConditionPanel.Children.Contains(associatedPanel))
                {
                    ConditionPanel.Children.Remove(associatedPanel);
                }
            }
        }

        private void ExportConditionClick(object sender, RoutedEventArgs e)
        {
            Conditions.Clear();
            foreach (UIElement element in ConditionPanel.Children)
            {
                if (element is StackPanel panel)
                {
                    foreach (UIElement child in panel.Children)
                    {
                        if (child is TextBox textBox && !string.IsNullOrWhiteSpace(textBox.Text))
                        {
                            Conditions.Add(textBox.Text.Trim());
                        }
                    }
                }
            }
            
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Title = "Select a File",
                Filter = "Json Files (*.json)|*.txt|All Files (*.*)|*.*",
                InitialDirectory = AppDomain.CurrentDomain.BaseDirectory,
                FileName = "conditions.json"
            };

            if (saveFileDialog.ShowDialog() == true) // ShowDialog() returns a nullable bool
            {
                string filePath = saveFileDialog.FileName;
                try
                {
                    string json = System.Text.Json.JsonSerializer.Serialize(Conditions);
                    File.WriteAllText(filePath, json);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to save conditions: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }

}
