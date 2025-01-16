using Microsoft.Win32;
using System.Data;
using System.IO;
using System.IO.Ports;
using System.Text;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace TSI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private List<string> conditions = new List<string>();
        private List<QuestionnaireItem> importedItems = null;
        private Color error = Color.FromArgb(186, 26, 26, 1);
        private Color tertiary = Color.FromArgb(175, 207, 171, 1);
        private DataTable sliderDataTable;
        private SerialPort _arduinoPort;
        private double currentItemCount = -1.00;
        private double currentThreshold = 0.00;
        private ParticipantView _participantView;

        public class DataRow
        {
            public string Questionnaire { get; set; }
            public string ParticipantID { get; set; }
            public string Condition { get; set; }
            public DateTime TimeStamp { get; set; }
            public int Raw { get; set; }
            public double Data { get; set; }
            public int Items { get; set; }
            public double Threshold { get; set; }
            public bool ShouldVibrate { get; set; }
        }


        public MainWindow()
        {
            InitializeComponent();
            InitializeDataTable();
            LoadAvailableComPorts();
        }

       private void MainWindow_Closed(object sender, EventArgs e) 
        {
            SaveConditions();

            if (_arduinoPort != null && _arduinoPort.IsOpen)
            {
                _arduinoPort.Close();
            }
        }


        private void ComPortComboBox_DropDownOpened(object sender, EventArgs e)
        {
            string[] ports = SerialPort.GetPortNames();
                        
            ComPortComboBox.ItemsSource = ports;

            if (ports.Length > 0)
            {
                if (!ports.Contains(ComPortComboBox.SelectedItem?.ToString()))
                {
                    ComPortComboBox.SelectedIndex = 0;
                }
            }
            else
            {
                MessageBox.Show("Kein COM-Port verfügbar. Bitte ein Gerät anschließen.", "Warnung", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }


        private void LoadAvailableComPorts()
        {
            string[] ports = SerialPort.GetPortNames();
            ComPortComboBox.ItemsSource = ports;
            if (ports.Length > 0)
            {
                ComPortComboBox.SelectedIndex = 0;
            }
            else
            {
                MessageBox.Show("No COM ports available. Please connect a device.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void ComPortComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ComPortComboBox.SelectedItem != null)
            {
                string selectedPort = ComPortComboBox.SelectedItem.ToString();

                if (_arduinoPort != null && _arduinoPort.IsOpen)
                {
                    _arduinoPort.Close();
                    _arduinoPort.Dispose();
                }

                _arduinoPort = new SerialPort(selectedPort, 9600);
                _arduinoPort.DataReceived += ArduinoPort_DataReceived;

                try
                {
                    _arduinoPort.Open();
                    MessageBox.Show($"Connected to {selectedPort}", "Connection", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Could not connect to {selectedPort}. Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        private void ArduinoPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                while (_arduinoPort != null && _arduinoPort.IsOpen && _arduinoPort.BytesToRead > 0)
                {
                    string data = _arduinoPort.ReadLine();
                    Console.WriteLine(data);
                    Dispatcher.Invoke(() => DisplaySerialData(data));
                }
            }
            catch (IOException ex)
            {
                Console.WriteLine($"I/O Exception: {ex.Message}");
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine($"Invalid Operation: {ex.Message}");
            }
        }


        private void DisplaySerialData(string valueString)
        {
            bool pressed = false;

            if (valueString.StartsWith("Pressed:"))
            {
                valueString = valueString.Replace("Pressed:", "").Trim();
                pressed = true;             
            }

            if (double.TryParse(valueString, out double sliderValue))
            {
                double scaledValue = sliderValue * currentItemCount / 1023.0;

                IndicateVibration(scaledValue, currentThreshold);
                if (pressed)
                {
                    string questionnaire = Questionnaire_Name.Text;
                    string participantID = ParticipantID.Text;
                    string condition = ConditionPanel.Children.OfType<RadioButton>().FirstOrDefault(rb => rb.IsChecked == true)?.Content?.ToString() ?? "no con";
                    int items = (int)currentItemCount;
                    double threshold = currentThreshold;
                    bool shouldVibrate = ShouldVibrate(sliderValue, threshold);

                    sliderDataTable.Rows.Add(questionnaire, participantID, condition, DateTime.Now, sliderValue,
                                     sliderValue * items / 1023.0, items, threshold, shouldVibrate);
                    _participantView?.QuestionnaireForward();
                }
                Raw_Value.Text = valueString;
                Slider_Visualizer.Value = sliderValue;
                Data_Value.Text = (sliderValue * currentItemCount / 1023.0).ToString("F2");
                
                UpdateZonesOnThresholdChange();
            }
        }

        private void InitializeDataTable()
        {
            sliderDataTable = new DataTable();

            sliderDataTable.Columns.Add("Questionnaire", typeof(string));
            sliderDataTable.Columns.Add("ParticipantID", typeof(string));
            sliderDataTable.Columns.Add("Condition", typeof(string));
            sliderDataTable.Columns.Add("TimeStamp", typeof(DateTime));
            sliderDataTable.Columns.Add("Raw", typeof(double));
            sliderDataTable.Columns.Add("Data", typeof(double));
            sliderDataTable.Columns.Add("Items", typeof(int));
            sliderDataTable.Columns.Add("Threshold", typeof(double));
            sliderDataTable.Columns.Add("ShouldVibrate", typeof(bool));

            DataGridSliderValues.ItemsSource = sliderDataTable.DefaultView;
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            if (importedItems == null || importedItems.Count == 0)
            {
                MessageBox.Show("Please import a questionnaire CSV file before starting.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            _participantView = new ParticipantView(importedItems, _arduinoPort);
            _participantView.OnItemSentToArduino += UpdateSliderValues;
            _participantView.Show();
        }

        private void ImportQuestionnaireButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                string filePath = openFileDialog.FileName;
                importedItems = CsvParser.ParseCsv(filePath);

                if (importedItems == null || importedItems.Count == 0)
                {
                    MessageBox.Show("Die CSV-Datei konnte nicht geladen werden oder enthält keine gültigen Daten.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else
                {
                    MessageBox.Show("Die CSV-Datei wurde erfolgreich importiert.", "Erfolg", MessageBoxButton.OK, MessageBoxImage.Information);
                    Questionnaire_Name.Text = System.IO.Path.GetFileName(filePath);
                    Questionnaire_Name_Pannel.Visibility = Visibility.Visible;
                    currentItemCount = !string.IsNullOrWhiteSpace(importedItems[0].ItemCount.ToString()) ? importedItems[0].ItemCount : -1;
                    Items_Value.Text = currentItemCount.ToString();
                    currentThreshold = !string.IsNullOrWhiteSpace(importedItems[0].Threshold.ToString()) ? importedItems[0].Threshold : -1;
                    Threshold_Value.Text = currentThreshold.ToString();
                    UpdateZonesOnThresholdChange();
                }
            }
        }

        private void SetupConditionsButton_Click(object sender, RoutedEventArgs e)
        {
            ConditionSetupWindow setupWindow = new ConditionSetupWindow();
            setupWindow.SetConditions(new List<string>(conditions));
            Overlay.Visibility = Visibility.Visible;

            setupWindow.Owner = this;

            if (setupWindow.ShowDialog() == true)
            {
                conditions = new List<string>(setupWindow.Conditions);
                conditions = conditions.Where(c => !string.IsNullOrWhiteSpace(c)).Distinct().ToList();

                UpdateConditionDisplay();
            }

            Overlay.Visibility = Visibility.Collapsed;
        }

        private void UpdateConditionDisplay()
        {
            ConditionPanel.Children.Clear();
            ConditionPanel.Visibility = conditions.Any() ? Visibility.Visible : Visibility.Collapsed;

            foreach (var condition in conditions)
            {
                if (!string.IsNullOrWhiteSpace(condition))
                {
                    RadioButton rb = new RadioButton
                    {
                        Content = condition,
                        GroupName = "Conditions",
                        Margin = new Thickness(5)
                    };
                    ConditionPanel.Children.Add(rb);
                }
            }
        }

        private void SaveConditions()
        {
            string filePath = "conditions.json";
            try
            {
                string json = System.Text.Json.JsonSerializer.Serialize(conditions);
                File.WriteAllText(filePath, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save conditions: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadConditions()
        {
            string filePath = "conditions.json";
            if (File.Exists(filePath))
            {
                try
                {
                    string json = File.ReadAllText(filePath);
                    conditions = System.Text.Json.JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
                    UpdateConditionDisplay();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to load conditions: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }



        private void UpdateSliderValues(double items, double threshold)
        {
            Items_Value.Text = items.ToString();
            currentItemCount = items;
            Threshold_Value.Text = threshold.ToString("F2");
            currentThreshold = threshold;
        }

        private void IndicateVibration(double rawValue, double threshold)
        {
            bool shouldVibrate = ShouldVibrate(rawValue, threshold);
            Vibe_Label.Text = shouldVibrate ? "True" : "False";

            Vibe_Label.Foreground = shouldVibrate
                ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#AFCFAB"))
                : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#BA1A1A"));
        }



        private bool ShouldVibrate(double rawValue, double threshold)
        {
            double intPart = Math.Floor(rawValue);
            double fractPart = rawValue - intPart;
            return (fractPart < threshold) || (fractPart > 1 - threshold);
        }

        private void UpdateZonesOnThresholdChange()
        {
            if (Slider_Visualizer.Template != null)
            {
                int items = (int)currentItemCount;
                double threshold = currentThreshold;

                Canvas zonesCanvas = (Canvas)Slider_Visualizer.Template.FindName("ZonesCanvas", Slider_Visualizer);
                if (zonesCanvas != null)
                {
                    double sliderWidth = Slider_Visualizer.ActualWidth;
                    UpdateVibrationZones(zonesCanvas, items, threshold, Slider_Visualizer);
                }
            }
        }

        private void UpdateVibrationZones(Canvas zonesCanvas, int items, double threshold, Slider slider)
        {
            zonesCanvas.Children.Clear();

            if (items <= 0 || threshold <= 0)
                return;

            Track track = (Track)slider.Template.FindName("PART_Track", slider);
            if (track == null) return;

            double trackWidth = track.ActualWidth;
            GeneralTransform transform = track.TransformToVisual(slider);
            Point trackPosition = transform.Transform(new Point(0, 0));
            double trackOffset = trackPosition.X;

            double stepSize = trackWidth / (items - 1);
            double zoneWidth = stepSize * threshold;

            for (int i = 0; i < items; i++)
            {
                double zoneCenter = i * stepSize;
                double zoneStart = zoneCenter - (zoneWidth / 2);
                double zoneEnd = zoneCenter + (zoneWidth / 2);

                if (zoneStart < 0) zoneStart = 0;
                if (zoneEnd > trackWidth) zoneEnd = trackWidth;

                Rectangle zone = new Rectangle
                {
                    Width = zoneEnd - zoneStart,
                    Height = 6,
                    Fill = new SolidColorBrush(Color.FromRgb(96, 72, 50)),
                    HorizontalAlignment = HorizontalAlignment.Left
                };

                Canvas.SetLeft(zone, zoneStart + trackOffset);
                Canvas.SetTop(zone, 0);
                zonesCanvas.Children.Add(zone);
            }
        }

    }
}