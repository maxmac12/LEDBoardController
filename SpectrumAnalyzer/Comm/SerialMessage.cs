using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SpectrumAnalyzer.Enums;

namespace SpectrumAnalyzer.Comm
{
    /*
     * Defines a generic message of the following format:
     * 
     * Description:               Size (byte(s)):
     * Preamble                         1
     * Message Length (in bytes)        1
     * Message Command                  1
     * Message Data                     Message Length - 5
     * CRC-16                           2
     * 
     * Note(s):
     *      - The Message Length is the size of the the entire packet, therefore the size of the Message Data does
     *        not include the size of the Preamble, Message Length, Message Command, and CRC-16.
     *      - The term "message header", or simply "header", may be used as shorthand in various documentation as short hand
     *        to refer to the Preamble, Message Length, and Message Command portion of the message.
     */
    public class SerialMessage
    {
        // Define various message element max sizes.
        public const int MAX_MSG_SIZE = 13;  // Includes header and CRC.

        public static class Commands
        {
            public const byte MODE_CMD = 0x00;
            public const byte COLOR_CMD = 0x01;
            public const byte SPECTRUM_CMD = 0x02;
        }

        public class LEDModes
        {
            public const byte MODE_OFF = 0x00;
            public const byte MODE_WHITE = 0x01;
            public const byte MODE_COLOR = 0x02;
            public const byte MODE_PULSE = 0x03;
            public const byte MODE_RAINBOW = 0x04;
            public const byte MODE_WRAINBOW = 0x05;
        }

        // Properties.
        public byte preamble { get; set; }
        public byte dataLength { get; set; }
        public byte command { get; set; }
        public byte[] data;
        public ushort crc { get; set; }

        // Instance Constructor. 
        public SerialMessage()
        {
            preamble = 0xEE;
            data = new byte[MAX_MSG_SIZE];
        }
    }
}
