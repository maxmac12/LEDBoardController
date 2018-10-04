using System;
using System.Linq;
using System.Windows.Media;
using SpectrumAnalyzer.Controls;
using SpectrumAnalyzer.Comm;
using SpectrumAnalyzer.Singleton;
using System.IO.Ports;
using System.Windows;


namespace SpectrumAnalyzer
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private SerialComm _serialComm;

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
            SerialComm.Stop();
        }

        private void BlurryColorPicker_OnColorChanged(object sender, Color color)
        {
            foreach (var audioSpectrum in Spectrum.Children.OfType<AudioSpectrum>())
            {
                audioSpectrum.ForegroundPitched = new SolidColorBrush(color);
            }

            foreach (var audioSpectrum in Reflection.Children.OfType<AudioSpectrum>())
            {
                audioSpectrum.ForegroundPitched = new SolidColorBrush(color);
            }

            _serialComm.SetColor(color);
            _serialComm.SetMode(SerialComm.LEDModes.COLOR);
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
            _serialComm.SetMode(SerialComm.LEDModes.OFF);
        }

        private void buttonWhite_Click(object sender, RoutedEventArgs e)
        {
            _serialComm.SetMode(SerialComm.LEDModes.WHITE);

        }

        private void buttonRainbow_Click(object sender, RoutedEventArgs e)
        {
            _serialComm.SetMode(SerialComm.LEDModes.RAINBOW_CYCLE);
        }

        private void buttonWRainbow_Click(object sender, RoutedEventArgs e)
        {
            _serialComm.SetMode(SerialComm.LEDModes.WHITE_OVER_RAINBOW);
        }

        private void buttonColor_Click(object sender, RoutedEventArgs e)
        {
            _serialComm.SetMode(SerialComm.LEDModes.COLOR);
        }

        private void buttonPulse_Click(object sender, RoutedEventArgs e)
        {
            _serialComm.SetMode(SerialComm.LEDModes.COLOR_PULSE);
        }

        private void buttonSpectrum_Click(object sender, RoutedEventArgs e)
        {
            _serialComm.SetMode(SerialComm.LEDModes.SPECTRUM);
        }
    }
}
