using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        public const int MAX_BODY_SIZE = 249;  // TODO: Determine needed size.
        public const int MAX_MSG_SIZE = MAX_BODY_SIZE + 5;  // Includes header and CRC.


        // Definitions for the LED Command designations.
        public static class LedCommand
        {
            public const byte LED_STAT  = 0x00;
            public const byte LED_ABOUT = 0x01;
            public const byte LED_MODE  = 0x02;
        }

        // Properties.
        public byte preamble { get; set; }
        public byte length { get; set; }
        public byte command { get; set; }
        public byte[] data;
        public ushort crc { get; set; }

        // Instance Constructor. 
        public SerialMessage()
        {
            preamble = 0xEA;
            data = new byte[MAX_BODY_SIZE];
        }
    }
}
