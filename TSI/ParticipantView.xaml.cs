﻿using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace TSI
{
    public partial class ParticipantView : Window
    {
        private List<QuestionnaireItem> _items;
        private int _currentIndex = 0;
        private SerialPort _arduinoPort;
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

                FinishButton.Visibility = index == _items.Count - 1 ? Visibility.Visible : Visibility.Hidden;
                ContinueButton.Visibility = index == _items.Count - 1 ? Visibility.Hidden : Visibility.Visible;

                SendItemToArduino(item);
            }

        }

        private void SendItemToArduino(QuestionnaireItem item)
        {
            if (_arduinoPort != null && _arduinoPort.IsOpen)
            {
                string message = $"{item.ItemCount}:{item.Threshold}";
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
            if (_currentIndex < _items.Count - 1)
            {
                _currentIndex++;
                LoadQuestionnaireItem(_currentIndex);
            }
            else
            {
                MessageBox.Show("Keine weiteren Items vorhanden.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void DebugContinueButton_Click(object sender, RoutedEventArgs e) => QuestionnaireForward();

        private void ContinueButton_Click(object sender, RoutedEventArgs e) => MessageBox.Show("Weiter zur nächsten Aktion.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);

        private void FinishButton_Click(object sender, RoutedEventArgs e)
        {
            _currentIndex = 0;
            // MessageBox.Show("Fragebogen abgeschlossen.", "Abschluss", MessageBoxButton.OK, MessageBoxImage.Information);
            this.Close();
        }
    }
}