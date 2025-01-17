using System.Globalization;
using System.IO.Ports;
using System.Windows;

namespace TSI
{
    public partial class ParticipantView : Window
    {
        private List<QuestionnaireItem> _items;
        private int _currentIndex = 0;
        public SerialPort _arduinoPort;
        public event Action<double, double> OnItemSentToArduino;

        public ParticipantView(List<QuestionnaireItem> items, SerialPort arduinoPort)
        {
            InitializeComponent();
            _items = items;
            _arduinoPort = arduinoPort;
            LoadQuestionnaireItem(_currentIndex);
        }

        private void LoadQuestionnaireItem(int index)
        {
            if (index >= 0 && index < _items.Count)
            {
                var item = _items[index];

                TitleText.Text = !string.IsNullOrWhiteSpace(item.Title) ? item.Title : "";
                TitleText.Visibility = string.IsNullOrWhiteSpace(item.Title) ? Visibility.Hidden : Visibility.Visible;

                DescriptionText.Text = !string.IsNullOrWhiteSpace(item.Description) ? item.Description : "";
                DescriptionText.Visibility = string.IsNullOrWhiteSpace(item.Description) ? Visibility.Hidden : Visibility.Visible;

                LeftLabel.Text = !string.IsNullOrWhiteSpace(item.LabelLeft) ? item.LabelLeft : "";
                LeftLabel.Visibility = string.IsNullOrWhiteSpace(item.LabelLeft) ? Visibility.Hidden : Visibility.Visible;

                RightLabel.Text = !string.IsNullOrWhiteSpace(item.LabelRight) ? item.LabelRight : "";
                RightLabel.Visibility = string.IsNullOrWhiteSpace(item.LabelRight) ? Visibility.Hidden : Visibility.Visible;

                SendItemToArduino(item);
            }

        }

        private void SendItemToArduino(QuestionnaireItem item)
        {
            if (_arduinoPort != null && _arduinoPort.IsOpen)
            {
                var thresh = item.Threshold.ToString(CultureInfo.InvariantCulture);
                string message = $"{item.ItemCount}:{thresh}";
                if (message.Length > 29)
                    throw new ArgumentException("message too long for arduino! max 29 characters, but got " +
                                                message.Length);
                _arduinoPort.WriteLine(message);
                OnItemSentToArduino?.Invoke(item.ItemCount, item.Threshold);
            }
            else
            {
                MessageBox.Show("Connection to Arduino not available.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void QuestionnaireForward()
        {
            if (_currentIndex < _items.Count)
            {
                LoadQuestionnaireItem(_currentIndex);
                _currentIndex++;
            }
            else
            {
                MessageBox.Show("Keine weiteren Items vorhanden.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
}