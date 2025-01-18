using System.Data;
using System.Globalization;
using System.IO;
using System.IO.Ports;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Shapes;
using Microsoft.Win32;
using MsgBoxEx;

namespace TSI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private List<string> _conditions = new List<string>();
        private List<QuestionnaireItem> _importedItems;
        private Color _error = Color.FromArgb(186, 26, 26, 1);
        private Color _tertiary = Color.FromArgb(175, 207, 171, 1);
        private DataTable _sliderDataTable;
        private SerialPort? _arduinoPort;
        private double _currentItemCount = -1.00;
        private double _currentThreshold = 0.00;
        private ParticipantView? _participantView;

        public MainWindow()
        {
            InitializeComponent();
            InitializeDataTable();
            LoadAvailableComPorts();
        }

       private void MainWindow_Closed(object sender, EventArgs e) 
        {
            var check = MessageBoxEx.Show(
                $"Do you want to export the collected data before closing the application?", 
                "EXPORT DATA?", 
                MessageBoxButton.YesNo, MessageBoxImage.Error
            );
            
            if (check == MessageBoxResult.Yes)
                SaveDataTable();
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
                MessageBoxEx.Show("No device available. Please connect a device.", "SLIDER NOT FOUND", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }


        private void LoadAvailableComPorts()
        {
            string[] ports = SerialPort.GetPortNames();
            ComPortComboBox.ItemsSource = ports;
            if (ports.Length > 0)
                ComPortComboBox.SelectedIndex = 0;
            else
                MessageBoxEx.Show("No device available. Please connect a device.", "SLIDER NOT FOUND", MessageBoxButton.OK, MessageBoxImage.Warning);
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
                    if (_participantView != null) 
                        _participantView.ArduinoPort = _arduinoPort;
                }
                catch (Exception ex)
                {
                    MessageBoxEx.Show($"Could not connect to {selectedPort}. Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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

            if (int.TryParse(valueString, out int sliderValue))
            {
                double scaledValue = sliderValue * (_currentItemCount - 1) / 1024.0;

                IndicateVibration(scaledValue, _currentThreshold);
                if (pressed)
                {
                    string questionnaire = Questionnaire_Name.Text;
                    string participantID = ParticipantId.Text;
                    string condition = ConditionPanel.Children.OfType<RadioButton>().FirstOrDefault(rb => rb.IsChecked == true)?.Content?.ToString() ?? "no con";
                    string? item = _participantView == null ? "" : _participantView.CurrentQuestionnaireItem.Title;
                    int items = (int)_currentItemCount;
                    double threshold = _currentThreshold;
                    bool shouldVibrate = ShouldVibrate(scaledValue, threshold);

                    _sliderDataTable.Rows.Add(questionnaire, participantID, condition, item, DateTime.Now, sliderValue,
                        scaledValue, items, threshold, shouldVibrate);
                    _participantView?.QuestionnaireForward();
                }
                Raw_Value.Text = valueString;
                Slider_Visualizer.Value = sliderValue;
                Data_Value.Text = scaledValue.ToString("F2");
                
                UpdateZonesOnThresholdChange();
            }
        }

        private void InitializeDataTable()
        {
            _sliderDataTable = new DataTable();

            _sliderDataTable.Columns.Add("Questionnaire", typeof(string));
            _sliderDataTable.Columns.Add("ParticipantID", typeof(string));
            _sliderDataTable.Columns.Add("Condition", typeof(string));
            _sliderDataTable.Columns.Add("Item", typeof(string));
            _sliderDataTable.Columns.Add("TimeStamp", typeof(DateTime));
            _sliderDataTable.Columns.Add("Raw", typeof(double));
            _sliderDataTable.Columns.Add("Data", typeof(double));
            _sliderDataTable.Columns.Add("Items", typeof(int));
            _sliderDataTable.Columns.Add("Threshold", typeof(double));
            _sliderDataTable.Columns.Add("ShouldVibrate", typeof(bool));

            DataGridSliderValues.ItemsSource = _sliderDataTable.DefaultView;
        }

        private void InitParticipantView()
        {
            if (_importedItems == null || _importedItems.Count == 0)
            {
                MessageBoxEx.Show("Please import a questionnaire CSV file before starting.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            
            if (_participantView != null)
            {
                if (_participantView.IsVisible)
                {
                    _participantView.Close();
                }
                _participantView = null;
            }

            if (_arduinoPort == null) 
                MessageBoxEx.Show("Please connect a device.", "NO DEVICE", MessageBoxButton.OK, MessageBoxImage.Error);

            else
            {
                _participantView = new ParticipantView(_importedItems, _arduinoPort);
                _participantView.OnItemSentToArduino += UpdateSliderValues;
                _participantView.Show();
            }
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            InitParticipantView();
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
                _importedItems = CsvParser.ParseCsv(filePath);

                if (_importedItems == null || _importedItems.Count == 0)
                {
                    MessageBoxEx.Show("Could not load the CSV file or it does not contain valid data.", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else
                {
                    Questionnaire_Name.Text = System.IO.Path.GetFileName(filePath);
                    Questionnaire_Name_Pannel.Visibility = Visibility.Visible;
                    _currentItemCount = !string.IsNullOrWhiteSpace(_importedItems[0].ItemCount.ToString()) ? _importedItems[0].ItemCount : -1;
                    Items_Value.Text = _currentItemCount.ToString();
                    _currentThreshold = !string.IsNullOrWhiteSpace(_importedItems[0].Threshold.ToString()) ? _importedItems[0].Threshold : -1;
                    Threshold_Value.Text = _currentThreshold.ToString();
                    UpdateZonesOnThresholdChange();
                    InitParticipantView();
                }
            }
        }

        private void SetupConditionsButton_Click(object sender, RoutedEventArgs e)
        {
            ConditionSetupWindow setupWindow = new ConditionSetupWindow();
            setupWindow.SetConditions(new List<string>(_conditions));
            Overlay.Visibility = Visibility.Visible;

            setupWindow.Owner = this;

            if (setupWindow.ShowDialog() == true)
            {
                _conditions = new List<string>(setupWindow.Conditions);
                _conditions = _conditions.Where(c => !string.IsNullOrWhiteSpace(c)).Distinct().ToList();

                UpdateConditionDisplay();
            }

            Overlay.Visibility = Visibility.Collapsed;
        }

        private void UpdateConditionDisplay()
        {
            ConditionPanel.Children.Clear();
            ConditionPanel.Visibility = _conditions.Any() ? Visibility.Visible : Visibility.Collapsed;

            foreach (var condition in _conditions)
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

        private void LoadConditions(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*",
                InitialDirectory = AppDomain.CurrentDomain.BaseDirectory,
                Title = "Select a Conditions File"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                string filePath = openFileDialog.FileName;

                if (File.Exists(filePath))
                {
                    try
                    {
                        string json = File.ReadAllText(filePath);
                        _conditions = System.Text.Json.JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();

                        UpdateConditionDisplay();
                    }
                    catch (Exception ex)
                    {
                        MessageBoxEx.Show($"Failed to load conditions: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }
        
        private void SaveDataTable()
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Title = "Save CSV File",
                Filter = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*",
                InitialDirectory = AppDomain.CurrentDomain.BaseDirectory,
                FileName = string.IsNullOrWhiteSpace(ParticipantId.Text) ? "sliderData.csv" : $"{ParticipantId.Text}.csv"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                string filePath = saveFileDialog.FileName;

                var originalCulture = Thread.CurrentThread.CurrentCulture;
                Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

                try
                {
                    StringBuilder sb = new StringBuilder();

                    // Get column names and append to the StringBuilder
                    IEnumerable<string> columnNames = _sliderDataTable.Columns.Cast<DataColumn>()
                        .Select(column => column.ColumnName);
                    sb.AppendLine(string.Join(",", columnNames));

                    // Iterate through rows and append each row's data
                    foreach (System.Data.DataRow row in _sliderDataTable.Rows)
                    {
                        IEnumerable<string> fields = row.ItemArray.Select(field => field.ToString());
                        sb.AppendLine(string.Join(",", fields));
                    }

                    // Write the content to the selected file
                    File.WriteAllText(filePath, sb.ToString());
                }
                catch (Exception ex)
                {
                    throw new IOException($"Failed to save data to {filePath}.", ex);
                }
                finally
                {
                    Thread.CurrentThread.CurrentCulture = originalCulture;
                }
            }
        }



        private void UpdateSliderValues(double items, double threshold)
        {
            Items_Value.Text = items.ToString();
            _currentItemCount = items;
            Threshold_Value.Text = threshold.ToString("F2");
            _currentThreshold = threshold;
        }

        private void IndicateVibration(double rawValue, double threshold)
        {
            bool shouldVibrate = ShouldVibrate(rawValue, threshold);
            Vibe_Label.Text = shouldVibrate ? "TRUE" : "FALSE";

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
                int items = (int)_currentItemCount;
                double threshold = _currentThreshold;

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

            double stepSize = trackWidth / (items - 1); // left is first and right is last, so -1
            double zoneWidth = stepSize * threshold;

            for (int i = 0; i < items; i++)
            {
                double zoneCenter = i * stepSize;
                double zoneStart = zoneCenter - zoneWidth;
                double zoneEnd = zoneCenter + zoneWidth;

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

        private void ExportDataTable(object sender, RoutedEventArgs e) => SaveDataTable();

        private void OnDeleteLastClicked(object sender, RoutedEventArgs e)
        {
            if (_sliderDataTable == null || _sliderDataTable.Rows.Count == 0)
            {
                MessageBoxEx.Show("The data table already is empty", "EMPTY TABLE", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            
            var result = MessageBoxEx.Show("The last data values collected will be deleted.", 
                "DELETE LAST DATA VALUES", MessageBoxButton.OKCancel, MessageBoxImage.Warning);

            if (result == MessageBoxResult.OK)
            {
                _sliderDataTable.Rows.RemoveAt(_sliderDataTable.Rows.Count - 1);
                _participantView?.QuestionnaireBackward();
            }
        }

        private void OnDeleteAllClicked(object sender, RoutedEventArgs e)
        {
            if (_sliderDataTable.Rows.Count == 0)
            {
                MessageBoxEx.Show("The data table already is empty", "EMPTY TABLE", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var result = MessageBoxEx.Show($"All {_sliderDataTable.Rows.Count.ToString()} data values collected will be deleted.", 
                $"DELETE {_sliderDataTable.Rows.Count.ToString()} DATA VALUES?", MessageBoxButton.OKCancel, MessageBoxImage.Warning);

            if (result == MessageBoxResult.OK)
            {
                _sliderDataTable.Rows.Clear();
                InitParticipantView();
            }

        }
    }
}