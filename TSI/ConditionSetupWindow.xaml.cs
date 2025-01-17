using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace TSI
{
    /// <summary>
    /// Interaktionslogik für ConditionSetupWindow.xaml
    /// </summary>
    public partial class ConditionSetupWindow : Window
    {
        public List<string> Conditions { get; private set; } // Liste der Bedingungen
        private int conditionCount = 1;

        public ConditionSetupWindow()
        {
            InitializeComponent();
            Conditions = new List<string>();
        }

        // Methode zum Setzen der Bedingungen aus dem MainWindow
        public void SetConditions(List<string> existingConditions)
        {
            // Lösche bestehende Items im Panel (falls vorhanden)
            ConditionPanel.Children.Clear();
            Conditions.Clear(); // Leere die interne Liste der Bedingungen, um doppelte Einträge zu verhindern

            // Übernehme die Bedingungen aus dem MainWindow
            Conditions = new List<string>(existingConditions);

            // Füge die bestehenden Bedingungen hinzu
            for (int i = 0; i < Conditions.Count; i++)
            {
                AddConditionToPanel(Conditions[i], i + 1);
            }

            // Setze den Zähler auf die Anzahl der bestehenden Bedingungen
            conditionCount = Conditions.Count;

            // Sonderfall: Wenn keine Bedingungen existieren, füge eine leere Bedingung hinzu
            if (Conditions.Count == 0)
            {
                AddConditionToPanel("", 1); // Füge das erste leere Textfeld hinzu
                conditionCount = 1;
            }
        }

        // Methode zum Hinzufügen einer neuen Condition
        private void AddConditionButton_Click(object sender, RoutedEventArgs e)
        {
            conditionCount++;
            AddConditionToPanel("", conditionCount); // Neue leere Bedingung hinzufügen
        }

        // Methode zum Speichern der Bedingungen und Schließen des Popups
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Liste der Bedingungen leeren, um doppelte Einträge zu vermeiden
            Conditions.Clear();

            // Speichere alle TextBox-Inhalte, aber nur nicht-leere Einträge hinzufügen
            foreach (UIElement element in ConditionPanel.Children)
            {
                if (element is StackPanel panel)
                {
                    foreach (UIElement child in panel.Children)
                    {
                        if (child is TextBox textBox && !string.IsNullOrWhiteSpace(textBox.Text))
                        {
                            Conditions.Add(textBox.Text.Trim()); // Füge nur nicht-leere Bedingungen hinzu
                        }
                    }
                }
            }

            DialogResult = true; // Schließt das Fenster und gibt die Bedingungen zurück
        }

        // Methode zum dynamischen Hinzufügen einer Condition ins Panel
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
                Content = "🗑", // Icon-Inhalt
                Width = 30,
                Height = 30,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#AE4F4F")),
                Foreground = Brushes.White,
                Margin = new Thickness(10, 0, 0, 0) // Abstand nach links
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
    }

}
