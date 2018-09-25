using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SpectrumAnalyzer.SerialComm
{
    public class iController
    {

        public delegate void OnSerialMessageReceiveHandler(object sende, SerialMessage rx_msg);
        public event OnSerialMessageReceiveHandler OnSerialMessageReceive;

        private SerialComm _serialComm;

        private Thread _thread;
        private volatile bool _shouldStop = false;

        private bool _isRunMode = false;

        private const int _MaxQueueDepth = 200;

        // This is a queue that will hold commands that tx messages that will be sent occationally.
        private Queue<SerialMessage> _cmdQueue = new Queue<SerialMessage>();

        public iController()
        {

            _serialComm = new SerialComm();

            _serialComm.OnSerialMessageReceive += new SerialComm.SerialReceiveMessageHandler(_serialComm_OnSerialMessageReceive);


            _thread = new Thread(Scheduler);
            _thread.Start();

        }

        public bool isRunMode()
        {
            return _isRunMode;
        }

        public bool Run(string com_port)
        {
            if (_serialComm.PortName() != com_port)
            {
                _serialComm.Close();

                if (!_serialComm.Open(com_port))
                {
                    return false;
                }
            }

            _isRunMode = true;
            return true;
        }

        public void Stop()
        {
            _isRunMode = false;
        }

        private void Scheduler()
        {
            SerialMessage tx_msg = new SerialMessage();

            while (!_shouldStop)
            {
                Thread.Sleep(10);
                // If needed, send an immediate command.
                while (_cmdQueue.Count > 0)
                {
                    _serialComm.Send(_cmdQueue.Dequeue());
                }

                if (_isRunMode)
                {
                    // Handle one message.
                    // TODO: Any periodic messaging.
                    _serialComm.SendText("about");
                }
            }

        }

        public void Terminate()
        {
            _isRunMode = false;
            _shouldStop = true;
            _thread.Join();
        }


        private void _serialComm_OnSerialMessageReceive(object sender, SerialMessage rx_msg)
        {
            if (OnSerialMessageReceive != null)
            {
                OnSerialMessageReceive(this, rx_msg);
            }
        }


    }
}
