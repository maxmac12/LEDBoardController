using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO.Ports;

namespace SpectrumAnalyzer.SerialComm
{
    public class SerialComm
    {
        //This object will contain information about the triggered event.
        public delegate void SerialReceiveMessageHandler(object sender, SerialMessage rx_msg);
        public event SerialReceiveMessageHandler OnSerialMessageReceive;

        public const int _maxComPorts = 30;

        private SerialPort _serialPort = null;
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

        private static int _wait_for_response = 0;

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

        public bool Open(string com_port)
        {
            try
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
                _serialPort.WriteTimeout = 500;
                _serialPort.DataReceived += new SerialDataReceivedEventHandler(_serialPort_DataReceived);
            }
            catch
            {
                return false;
            }

            try
            {
                _serialPort.Open();
            }
            catch
            {
                return false; 
            }

            return true;
        }

        public void SendText(string text)
        {
            _serialPort.Write(text);
        }

        public bool Open(int com_port)
        {
            string com_port_str = "COM" + com_port;
            return this.Open(com_port_str);
        }

        public string PortName()
        {
            if (_serialPort == null)
            {
                return "";
            }
            else if (_serialPort.IsOpen)
            {
                return _serialPort.PortName;
            }
            else
            {
                return "";
            }
        }

        public bool IsOpen()
        {
            if (_serialPort == null)
            {
                return false;
            }
            else if (_serialPort.IsOpen)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private void ThreadProc(Object stateInfo)
        {
            // Attempt to close serial port
            if (_serialPort != null)
            {
                if (_serialPort.IsOpen == true)
                {
                    try
                    {
                        _serialPort.Close();
                    }
                    catch (NullReferenceException)
                    {
                        // Ignore this one and move on.
                    }
                    catch (Exception)
                    {
                        throw;
                    }
                }
            }
        }

        public bool Close()
        {
            if (_serialPort == null)
            {
                return true;
            }
            else if (_serialPort.IsOpen)
            {
                try
                {
                    // Remove the handler for received data.
                    _serialPort.DataReceived -= _serialPort_DataReceived;
                    // Wait to make sure that any pending serial receive data is processed or thread deadlock will occure.
                    Thread.Sleep(100);
                    // Do the actual close on a worker thread or we might get access violation.
                    ThreadPool.QueueUserWorkItem(new WaitCallback(ThreadProc));
                    // Wait here for the work item to finish.
                    while (_serialPort.IsOpen)
                    {
                        Thread.Sleep(10);
                    }
                    // Dispose of the serial handler instance.  Some on the interwebs say that this has fixed some bugs
                    // when using some types of USB/Serial dongles.  We will create a new instance the next time.
                    _serialPort = null;
                    // Do some final delay to let stuff settle.
                    Thread.Sleep(100);
                }
                catch
                {
                    return false;
                }
                return true;
            }
            else
            {
                return true;
            }

        }

        public void Send(SerialMessage tx_msg)
        {
            int indx = 0;
            ushort crc = 0xFFFF;

            if (_serialPort == null)
            {
                return;
            }
            else if (!_serialPort.IsOpen)
            {
                return;
            }

            // Wait here if the previous message has not yet received a response...
            while (_wait_for_response-- > 0)
            {
                Thread.Sleep(5);
            }

            // Check if the port is available again as it may have gone away while waiting.
            if (_serialPort == null)
            {
                return;
            }
            else if (!_serialPort.IsOpen)
            {
                return;
            }

            // Set the limit to wait next time.
            _wait_for_response = 10;

            // Fill the header.
            _tx_buffer[indx++] = tx_msg.preamble;
            _tx_buffer[indx++] = tx_msg.length;
            _tx_buffer[indx++] = tx_msg.command;

            // Body if it exists.
            for (int i = 0; i < (tx_msg.length - 1); i++)
            {
                _tx_buffer[indx++] = tx_msg.data[i];
            }

            // Calc CRC across message up to CRC field.
            for (int i = 0; i < (tx_msg.length + 4); i++)
            {
                crc = CRC16.addcrc(crc, _tx_buffer[i]);
            }

            // Fill in CRC.
            tx_msg.crc = crc;
            _tx_buffer[indx++] = (byte)(crc >> 8);
            _tx_buffer[indx++] = (byte)(crc & 0x00FF);

            // Send the message.
            try
            {
                _serialPort.Write(_tx_buffer, 0, tx_msg.length + 6);
            }
            catch (TimeoutException)
            {
                // I see this exception during DFCU discovery for some COM ports.
            }
            catch (Exception)
            {
                throw;
            }
        }

        void _serialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            for (;;)
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
                    if (_serialPort.Read(_rx_one_byte, 0, 1) > 0)
                    {
                        // Save this byte in the queue.
                        _rx_queue[_rx_next_put] = _rx_one_byte[0];
                        // Advance index to where next byte will go.
                        _rx_next_put = (_rx_next_put + 1) % SerialMessage.MAX_MSG_SIZE;
                        // Loop as long as we have not consumed all the data.  Note that if we loose sync
                        // we may go back in the queue one byte beyond where the last preamble was and look
                        // for another preample.
                        while (_rx_next_get != _rx_next_put)
                        {
                            // Parse out the RS-485 message...
                            switch (_rx_state)
                            {
                                case RxState.LookForPreamble:
                                    if (_rx_queue[_rx_next_get] == 0xEAu)
                                    {
                                        // Save byte in local copy.
                                        //_rx_message[_rx_message_index++] = _rx_queue[_rx_next_get];
                                        _rx_msg.preamble = _rx_queue[_rx_next_get];
                                        // Start the CRC.
                                        _crc_calculated = 0xFFFF;
                                        _crc_calculated = CRC16.addcrc(_crc_calculated, _rx_queue[_rx_next_get]);
                                        // Save where we would start looking if we lost sync.
                                        _rx_next_preamble_get = _rx_next_get;
                                        // Preamble found - start parsing message.
                                        _rx_state = RxState.LookForLength;
                                    }
                                    break;

                                case RxState.LookForLength:
                                    // Get the length embedded in message.
                                    _rx_expected_length = (int)_rx_queue[_rx_next_get];
                                    // Save byte in local copy.
                                    _rx_msg.length = _rx_queue[_rx_next_get];
                                    // Continue to accumulate the CRC.
                                    _crc_calculated = CRC16.addcrc(_crc_calculated, _rx_queue[_rx_next_get]);
                                    // Next state.
                                    _rx_state = RxState.LookForCommand;
                                    break;

                                case RxState.LookForCommand:
                                    // Save byte in local copy.
                                    _rx_msg.command = _rx_queue[_rx_next_get];
                                    // Continue to accumulate the CRC.
                                    _crc_calculated = CRC16.addcrc(_crc_calculated, _rx_queue[_rx_next_get]);
                                    // Decrement count of length.
                                    _rx_expected_length--;
                                    // Is there more of the body (payload) of message to get?
                                    if (_rx_expected_length > 0)
                                    {
                                        // Index for the message body.
                                        _rx_msg_index = 0;
                                        // Next state.
                                        _rx_state = RxState.LookForBody;
                                    }
                                    else
                                    {
                                        // Next state.
                                        _rx_state = RxState.LookForCrcMsb;
                                    }
                                    break;

                                case RxState.LookForBody:
                                    // Make sure we don't get too much data.
                                    if (_rx_msg_index < SerialMessage.MAX_BODY_SIZE)
                                    {
                                        // Save byte in local copy.
                                        _rx_msg.data[_rx_msg_index++] = _rx_queue[_rx_next_get];
                                        // Continue to accumulate the CRC.
                                        _crc_calculated = CRC16.addcrc(_crc_calculated, _rx_queue[_rx_next_get]);
                                        // Decrement count of length.
                                        _rx_expected_length--;
                                        // Is there more of the body (payload) of message to get?
                                        if (_rx_expected_length > 0)
                                        {
                                            // Stay in this state.
                                        }
                                        else
                                        {
                                            // Next state.
                                            _rx_state = RxState.LookForCrcMsb;
                                        }
                                    }
                                    else
                                    {
                                        // Bail out and try again.
                                        _rx_next_get = _rx_next_preamble_get;
                                        // Next state.
                                        _rx_state = RxState.LookForPreamble;
                                    }
                                    break;

                                case RxState.LookForCrcMsb:
                                    // Extract the MSB of the embedded CRC.
                                    _crc_embedded = (ushort)((int)_rx_queue[_rx_next_get] << 8);
                                    // Next state.
                                    _rx_state = RxState.LookForCrcLsb;
                                    break;

                                case RxState.LookForCrcLsb:
                                    // Factor in the LSB of the embedded CRC.
                                    _crc_embedded = (ushort)((int)_crc_embedded + (int)_rx_queue[_rx_next_get]);
                                    // Save byte in local copy.
                                    _rx_msg.crc = _crc_embedded;
                                    // Check the CRC.
                                    if (_crc_calculated != _crc_embedded)
                                    {
                                        // Failure so start looking for the preamble where we left off last time.
                                        _rx_next_get = _rx_next_preamble_get;
                                        // Next state.
                                        _rx_state = RxState.LookForPreamble;
                                    }
                                    else
                                    {
                                        // Likely valid message.  Report it.
                                        if (OnSerialMessageReceive != null)
                                        {
                                            // Clear count so next message will be sent immediately.
                                            _wait_for_response = 0;
                                            // Send the received message up for processing.
                                            OnSerialMessageReceive(this, _rx_msg);
                                        }
                                        // Next state.
                                        _rx_state = RxState.LookForPreamble;
                                    }
                                    break;
                            }
                            // Advance index to where we get next byte.
                            _rx_next_get = (_rx_next_get + 1) % SerialMessage.MAX_MSG_SIZE;
                        }
                    }

                }

            }
        }
    }
}
