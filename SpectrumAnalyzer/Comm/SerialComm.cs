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
        //public class LEDModes
        //{
        //    public byte OFF = 0x00;
        //    public byte WHITE = 0x00;
        //    public byte COLOR = 0x00;
        //    public byte COLOR_PULSE = 0x00;
        //    public byte RAINBOW_CYCLE      = 0x00;
        //    public byte WHITE_OVER_RAINBOW = 0x00;
        //};

        private SerialPort _serialPort = null;
        private Thread _threadSerial;
        private string _msg;
        private Queue<string> _tx_msgs = new Queue<string>();
        private Queue<string> _rx_msgs = new Queue<string>();

        private Queue<byte[]> _tx = new Queue<byte[]>();


        private byte[] _rx_one_byte = new byte[1];
        private byte[] _rx_queue = new byte[SerialMessage.MAX_MSG_SIZE];
        private int _rx_next_get = 0;
        private int _rx_next_put = 0;
        private int _rx_next_preamble_get = 0;
        private SerialMessage _rx_msg = new SerialMessage();
        private int _rx_msg_index = 0;
        private int _rx_expected_length = 0;
        private ushort _crc_calculated;
        private ushort _crc_embedded;
        private byte[] _tx_buffer = new byte[SerialMessage.MAX_MSG_SIZE];

        private enum RxState
        {
            LookForPreamble,
            LookForLength,
            LookForCommand,
            LookForBody,
            LookForCrcMsb,
            LookForCrcLsb
        };

        RxState _rx_state = RxState.LookForPreamble;

        private static bool run = true;  // Flag to stop all created SerialThreads.

        public SerialComm()
        {
            _threadSerial = new Thread(new ThreadStart(SerialThread));
            _threadSerial.Start();
        }

        public static void Stop()
        {
            run = false;
        }

        public void Send(string txt)
        {
            _tx_msgs.Enqueue(txt);
        }

        public void Send(byte[] data)
        {
            _serialPort.Write(data, 0, 1);
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
                _serialPort.BaudRate = 500000;
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

            return rtnStatus;
        }

        public void CloseComPort()
        {
            if (_serialPort != null)
            {
                Console.WriteLine(_serialPort.PortName + " Closed");

                _serialPort.Close();
                _serialPort.DataReceived -= _serialPort_DataReceived;
                _serialPort = null;
            }
        }

        public void Send(SerialMessage tx_msg)
        {
            int index = 0;
            ushort crc = 0xFFFF;

            if (_serialPort == null)
            {
                return;
            }
            else if (!_serialPort.IsOpen)
            {
                return;
            }

            // Fill the header.
            _tx_buffer[index++] = tx_msg.preamble;
            _tx_buffer[index++] = tx_msg.dataLength;
            _tx_buffer[index++] = tx_msg.command;

            // Body if it exists.
            for (int i = 0; i < tx_msg.dataLength; i++)
            {
                _tx_buffer[index++] = tx_msg.data[i];
            }

            // Calc CRC across message up to CRC field.
            for (int i = 0; i < (tx_msg.dataLength + 3); i++)
            {
                crc = CRC16.addcrc(crc, _tx_buffer[i]);
            }

            // Fill in CRC.
            tx_msg.crc = crc;
            _tx_buffer[index++] = (byte)(crc & 0x00FF);
            _tx_buffer[index++] = (byte)(crc >> 8);

            // Send the message.
            try
            {
                Console.WriteLine("Transmitting...");
                for (int i = 0; i < tx_msg.dataLength + 5; i++)
                {
                    Console.WriteLine("TX: " + String.Format("{0,10:X}", _tx_buffer[i]));
                    _serialPort.Write(_tx_buffer, i, 1);
                    Thread.Sleep(10);
                }

                //_serialPort.Write(_tx_buffer, 0, tx_msg.dataLength + 5);
            }
            catch (Exception)
            {
                throw;
            }
        }

        private void SerialThread()
        {
            while (run)
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
