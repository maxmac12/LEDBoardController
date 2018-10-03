using System;
using System.Linq;
using System.Windows.Media;
using SpectrumAnalyzer.Controls;
using SpectrumAnalyzer.Singleton;
using System.IO.Ports;
using System.Windows.Input;
using System.Windows;
using System.Threading;
using System.Text;
using System.Collections.Generic;
using System.Windows.Threading;
using System.Text.RegularExpressions;
using SpectrumAnalyzer.Comm;

namespace SpectrumAnalyzer
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private SerialComm _serialComm;
        private Color _color;

        public MainWindow()
        {
            InitializeComponent();
            Init_ComboBoxComPort();
            Closed += OnClosed;
            _serialComm = new SerialComm();
        }

        private void Init_ComboBoxComPort()
        {
            Refresh_ComboBoxComPort();

            string lastPort = Properties.Settings.Default.txtComPort;

            if (comboBoxComPort.Items.Contains(lastPort))
            {
                comboBoxComPort.SelectedIndex = comboBoxComPort.Items.IndexOf(lastPort);
            }
            else
            {
                comboBoxComPort.Text = "Select COM Port";
            }
        }

        private void Refresh_ComboBoxComPort()
        {
            comboBoxComPort.Items.Clear();

            string[] portNames = SerialPort.GetPortNames();

            // Populate the combo box drop down with all available COM ports.
            foreach (string s in portNames)
            {
                comboBoxComPort.Items.Add(s);
            }

            if (comboBoxComPort.Items.Count > 0)
            {
                comboBoxComPort.SelectedIndex = 0;
            }
            else
            {
                comboBoxComPort.Text = "No Available COM Ports";
            }
        }

        private static void OnClosed(object sender, EventArgs eventArgs)
        {
            ViewModelLocator.Instance.AnalyzerViewModel.Stop();
        }

        private void BlurryColorPicker_OnColorChanged(object sender, Color color)
        {
            _serialComm.Send("mode color " + color.R + " " + color.G + " " + color.B);

            foreach (var audioSpectrum in Spectrum.Children.OfType<AudioSpectrum>())
            {
                audioSpectrum.ForegroundPitched = new SolidColorBrush(color);
            }

            foreach (var audioSpectrum in Reflection.Children.OfType<AudioSpectrum>())
            {
                audioSpectrum.ForegroundPitched = new SolidColorBrush(color);
            }
        }

        private void comboBoxComPort_Selected(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            Refresh_ComboBoxComPort();
        }

        private void comboBoxComPort_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            Properties.Settings.Default.txtComPort = comboBoxComPort.Text;
        }

        private void buttonConnectDisconnect_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (buttonConnectDisconnect.Content.Equals("Connect"))
            {
                if (_serialComm.OpenComPort(comboBoxComPort.Text))
                {
                    buttonConnectDisconnect.Content = "Disconnect";
                    comboBoxComPort.IsEnabled = false;
                }
            }
            else
            {
                buttonConnectDisconnect.Content = "Connect";
                comboBoxComPort.IsEnabled = true;
                _serialComm.CloseComPort();
            }
        }

        private void buttonOff_Click(object sender, RoutedEventArgs e)
        {
            _serialComm.Send("mode off");
        }

        private void buttonWhite_Click(object sender, RoutedEventArgs e)
        {
            _serialComm.Send("mode white");
        }

        private void buttonRainbow_Click(object sender, RoutedEventArgs e)
        {
            _serialComm.Send("mode rainbow");
        }

        private void buttonWRainbow_Click(object sender, RoutedEventArgs e)
        {
            _serialComm.Send("mode wrainbow");
        }

        private void buttonPulse_Click(object sender, RoutedEventArgs e)
        {
            _serialComm.Send("mode pulse");
        }
    }
}
