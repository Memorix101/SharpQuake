using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SharpQuake.Framework
{
    // qsocket_t
    public class qsocket_t
    {
        public INetLanDriver LanDriver
        {
            get
            {
                return NetworkWrapper.GetLanDriver( this.landriver );
            }
        }

        public Double connecttime;
        public Double lastMessageTime;
        public Double lastSendTime;

        public Boolean disconnected;
        public Boolean canSend;
        public Boolean sendNext;

        public Int32 driver;
        public Int32 landriver;
        public Socket socket; // int	socket
        public Object driverdata; // void *driverdata

        public UInt32 ackSequence;
        public UInt32 sendSequence;
        public UInt32 unreliableSendSequence;

        public Int32 sendMessageLength;
        public Byte[] sendMessage; // byte sendMessage [NET_MAXMESSAGE]

        public UInt32 receiveSequence;
        public UInt32 unreliableReceiveSequence;

        public Int32 receiveMessageLength;
        public Byte[] receiveMessage; // byte receiveMessage [NET_MAXMESSAGE]

        public EndPoint addr; // qsockaddr	addr
        public String address; // char address[NET_NAMELEN]

        public void ClearBuffers( )
        {
            this.sendMessageLength = 0;
            this.receiveMessageLength = 0;
        }

        public Int32 Read( Byte[] buf, Int32 len, ref EndPoint ep )
        {
            return this.LanDriver.Read( this.socket, buf, len, ref ep );
        }

        public Int32 Write( Byte[] buf, Int32 len, EndPoint ep )
        {
            return this.LanDriver.Write( this.socket, buf, len, ep );
        }

        public qsocket_t( )
        {
            this.sendMessage = new Byte[NetworkDef.NET_MAXMESSAGE];
            this.receiveMessage = new Byte[NetworkDef.NET_MAXMESSAGE];
            disconnected = true;
        }
    }
}
