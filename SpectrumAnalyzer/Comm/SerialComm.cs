using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO.Ports;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Threading;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace SpectrumAnalyzer.Comm
{
    public class SerialComm
    {
        private SerialPort _serialPort = null;
        private Thread _threadSerial;
        private string _msg;
        private Queue<string> _tx_msgs = new Queue<string>();
        private Queue<string> _rx_msgs = new Queue<string>();

        public SerialComm()
        {
            _threadSerial = new Thread(new ThreadStart(SerialThread));
            _threadSerial.Start();
        }

        public void Send(string txt)
        {
            _tx_msgs.Enqueue(txt);
        }

        public bool OpenComPort(string com_port)
        {
            bool rtnStatus = false;

            if (_serialPort == null)
            {
                _serialPort = new SerialPort();
            }

            try
            {
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

                Console.WriteLine(_serialPort.PortName + " Connected");
                rtnStatus = true;
            }
            catch
            {
                Console.WriteLine("COM Port Could Not Be Opened");
            }

            //Scroller.ScrollToBottom();

            return rtnStatus;
        }

        public void CloseComPort()
        {
            if (_serialPort != null)
            {
                Console.WriteLine(_serialPort.PortName + " Closed");
                //Scroller.ScrollToBottom();

                _serialPort.Close();
                _serialPort.DataReceived -= _serialPort_DataReceived;
                _serialPort = null;
            }
        }

        private void SerialThread()
        {
            while (true)
            {
                if (_tx_msgs.Count > 0)
                {
                    string next_msg = _tx_msgs.Dequeue() + "\r";

                    if (_serialPort != null)
                    {
                        if (_serialPort.IsOpen)
                        {
                            try
                            {
                                // Send the binary data out the port
                                byte[] bytes = Encoding.UTF8.GetBytes(next_msg);

                                foreach (byte next in bytes)
                                {
                                    byte[] data = new byte[] { next };
                                    _serialPort.Write(data, 0, 1);

                                    Thread.Sleep(50);  // Wait for the device to process each byte.
                                }
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

                Thread.Sleep(500);  // Wait for the device to process any commands.

                if (_rx_msgs.Count > 0)
                {
                    Console.WriteLine(_rx_msgs.Dequeue());
                }
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
                        string result = Regex.Replace(_msg, @"\r\n?|\n", "");

                        // Don't display extra line breaks.
                        if (result.Length > 0)
                        {
                            _rx_msgs.Enqueue(_msg);
                        }

                        _msg = "";
                    }
                }
            }
        }
    }
}
