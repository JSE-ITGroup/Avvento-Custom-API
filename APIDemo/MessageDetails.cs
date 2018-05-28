using nsConnectionData;
using Stt.Derivatives.Api;
using Stt.Derivatives.Api.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using  AvventoAPILibrary;

namespace AvventoAPILibrary
{
    public class MessageDetails
    {
        public readonly MITS_Header header;
        public readonly byte[] data;
        public MessageType headerType { get { return (MessageType)header.MessageType; } } 

        public MessageDetails(ConnectionData packet)
        {
            header = Utilities.OnPacket<MITS_Header>(packet.Data);
            data = Utilities.StripPacketHeader<MITS_Header>(packet);
        }

        public static bool IsCompressed(MessageType messageType)
        {
            switch (messageType)
            {
                case MessageType.MESSAGE_99_FUTURES_SCREEN_OPEN:
                case MessageType.MESSAGE_36_START_OF_DAY_DOWNLOAD:
                    return true;
            }
            return false;
        }

       
    }
}
