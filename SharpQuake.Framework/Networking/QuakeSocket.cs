/// <copyright>
///
/// SharpQuakeEvolved changes by optimus-code, 2019
/// 
/// Based on SharpQuake (Quake Rewritten in C# by Yury Kiselev, 2010.)
///
/// Copyright (C) 1996-1997 Id Software, Inc.
///
/// This program is free software; you can redistribute it and/or
/// modify it under the terms of the GNU General Public License
/// as published by the Free Software Foundation; either version 2
/// of the License, or (at your option) any later version.
///
/// This program is distributed in the hope that it will be useful,
/// but WITHOUT ANY WARRANTY; without even the implied warranty of
/// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
///
/// See the GNU General Public License for more details.
///
/// You should have received a copy of the GNU General Public License
/// along with this program; if not, write to the Free Software
/// Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
/// </copyright>

using System;
using System.Net;
using System.Net.Sockets;

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
