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

namespace SpectrumAnalyzer
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        ConsoleContent dc = new ConsoleContent();

        private SerialPort _serialPort = null;
        private bool _msg_received;
        private string _msg;
        private Queue<string> _rx_msgs = new Queue<string>();

        private byte[] STX = new byte[] { 0x02 };
        private byte[] ETX = new byte[] { 0x03 };

        public MainWindow()
        {
            InitializeComponent();
            Init_ComboBoxComPort();
            Closed += OnClosed;
            DataContext = dc;
        }

        void ConsoleInput_KeyDown(object sender, KeyEventArgs e)
        {
            int strLength = ConsoleInput.Text.Length;
            Console.Write(e.Key);

            if (strLength > 0)
            {
                SendText(ConsoleInput.Text.Substring(strLength - 1, 1));
            }

            if (e.Key == Key.Enter)
            {
                string cmd = ConsoleInput.Text;

                //dc.RunCommand();
                ConsoleInput.Clear();
                ConsoleInput.Focus();
                Scroller.ScrollToBottom();

                SendText("\r");
            }
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
                comboBoxComPort.Text = "No COM Port";
            }
        }

        private static void OnClosed(object sender, EventArgs eventArgs)
        {
            ViewModelLocator.Instance.AnalyzerViewModel.Stop();
        }

        private void OpenComPort(string com_port)
        {
            if (_serialPort == null)
            {
                _serialPort = new SerialPort();
            }

            _serialPort.BaudRate = 115200;
            _serialPort.DataBits = 8;
            _serialPort.Handshake = Handshake.None;
            _serialPort.Parity = Parity.None;
            _serialPort.PortName = com_port;
            _serialPort.StopBits = StopBits.One;
            _serialPort.ReadTimeout = 500;
            _serialPort.WriteTimeout = 500;

            // TODO: Add exception handling.
            _serialPort.Open();

            _serialPort.DataReceived += new SerialDataReceivedEventHandler(_serialPort_DataReceived);

            dc.ConsoleOutput.Add(_serialPort.PortName + " Connected");
            Scroller.ScrollToBottom();
        }

        private void CloseComPort()
        {
            if (_serialPort != null)
            {
                dc.ConsoleOutput.Add(_serialPort.PortName + " Closed");
                Scroller.ScrollToBottom();

                _serialPort.Close();
                _serialPort.DataReceived -= _serialPort_DataReceived;
                _serialPort = null;


            }
        }

        private void SendText(string text)
        {
            if (_serialPort != null)
            {
                if (_serialPort.IsOpen)
                {
                    try
                    {
                        // Send the binary data out the port
                        byte[] bytes = Encoding.UTF8.GetBytes(text);

                        _serialPort.Write(bytes, 0, bytes.Length);
                    }
                    catch
                    {

                    }
                }
                else
                {
                }
            }
        }

        private void BlurryColorPicker_OnColorChanged(object sender, Color color)
        {
            foreach (var audioSpectrum in Spectrum.Children.OfType<AudioSpectrum>())
                audioSpectrum.ForegroundPitched = new SolidColorBrush(color);

            foreach (var audioSpectrum in Reflection.Children.OfType<AudioSpectrum>())
                audioSpectrum.ForegroundPitched = new SolidColorBrush(color);
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
                buttonConnectDisconnect.Content = "Disconnect";
                comboBoxComPort.IsEnabled = false;
                OpenComPort(comboBoxComPort.Text);
            }
            else
            {
                buttonConnectDisconnect.Content = "Connect";
                comboBoxComPort.IsEnabled = true;
                CloseComPort();
            }
        }

        void _serialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            byte[] _rx_one_byte = new byte[1];
            for (; ; )
            {

                if (_serialPort == null)
                {
                    break;
                }
                else if (!_serialPort.IsOpen)
                {
                    break;
                }
                else if (_serialPort.BytesToRead == 0)
                {
                    break;
                }
                else
                {

                    // Get one byte of received data.
                    _serialPort.Read(_rx_one_byte, 0, _rx_one_byte.Length);

                    _msg += Encoding.UTF8.GetString(_rx_one_byte);

                    if (_msg.Contains("\r\n"))
                    {
                        // Found end of received message.
                        _msg_received = true;
                        break;
                    }
                }
            }

            if (_msg_received)
            {
                string result = Regex.Replace(_msg, @"\r\n?|\n", "");

                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
                {
                    dc.ConsoleOutput.Add(result);
                    Scroller.ScrollToBottom();
                }));

                _msg_received = false;
                _msg = "";
            }
        }
    }
}
