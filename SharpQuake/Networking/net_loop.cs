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
using SharpQuake.Framework;
using SharpQuake.Game.Client;

namespace SharpQuake
{
    internal class net_loop : INetDriver
    {
        private Boolean _IsInitialised;
        private Boolean _LocalConnectPending; // localconnectpending
        private qsocket_t _Client; // loop_client
        private qsocket_t _Server; // loop_server

        #region INetDriver Members

        public String Name
        {
            get
            {
                return "Loopback";
            }
        }

        public Boolean IsInitialised
        {
            get
            {
                return _IsInitialised;
            }
        }

        // CHANGE
        private Host Host
        {
            get;
            set;
        }

        public void Initialise( Object host )
        {
            Host = ( Host ) host;

            if( Host.Client.cls.state == cactive_t.ca_dedicated )
                return;// -1;

            _IsInitialised = true;
        }

        public void Listen( Boolean state )
        {
            // nothig to do
        }

        public void SearchForHosts( Boolean xmit )
        {
            if( !Host.Server.sv.active )
                return;

            Host.Network.HostCacheCount = 1;
            if( Host.Network.HostName == "UNNAMED" )
                Host.Network.HostCache[0].name = "local";
            else
                Host.Network.HostCache[0].name = Host.Network.HostName;

            Host.Network.HostCache[0].map = Host.Server.sv.name;
            Host.Network.HostCache[0].users = Host.Network.ActiveConnections;
            Host.Network.HostCache[0].maxusers = Host.Server.svs.maxclients;
            Host.Network.HostCache[0].driver = Host.Network.DriverLevel;
            Host.Network.HostCache[0].cname = "local";
        }

        public qsocket_t Connect( String host )
        {
            if( host != "local" )
                return null;

            _LocalConnectPending = true;

            if( _Client == null )
            {
                _Client = Host.Network.NewSocket();
                if( _Client == null )
                {
                    Host.Console.Print( "Loop_Connect: no qsocket available\n" );
                    return null;
                }
                _Client.address = "localhost";
            }
            _Client.ClearBuffers();
            _Client.canSend = true;

            if( _Server == null )
            {
                _Server = Host.Network.NewSocket();
                if( _Server == null )
                {
                    Host.Console.Print( "Loop_Connect: no qsocket available\n" );
                    return null;
                }
                _Server.address = "LOCAL";
            }
            _Server.ClearBuffers();
            _Server.canSend = true;

            _Client.driverdata = _Server;
            _Server.driverdata = _Client;

            return _Client;
        }

        public qsocket_t CheckNewConnections()
        {
            if( !_LocalConnectPending )
                return null;

            _LocalConnectPending = false;
            _Server.ClearBuffers();
            _Server.canSend = true;
            _Client.ClearBuffers();
            _Client.canSend = true;
            return _Server;
        }

        public Int32 GetMessage( qsocket_t sock )
        {
            if( sock.receiveMessageLength == 0 )
                return 0;

            Int32 ret = sock.receiveMessage[0];
            var length = sock.receiveMessage[1] + ( sock.receiveMessage[2] << 8 );

            // alignment byte skipped here
            Host.Network.Message.Clear();
            Host.Network.Message.FillFrom( sock.receiveMessage, 4, length );

            length = IntAlign( length + 4 );
            sock.receiveMessageLength -= length;

            if( sock.receiveMessageLength > 0 )
                Array.Copy( sock.receiveMessage, length, sock.receiveMessage, 0, sock.receiveMessageLength );

            if( sock.driverdata != null && ret == 1 )
                ( (qsocket_t)sock.driverdata ).canSend = true;

            return ret;
        }

        public Int32 SendMessage( qsocket_t sock, MessageWriter data )
        {
            if( sock.driverdata == null )
                return -1;

            var sock2 = (qsocket_t)sock.driverdata;

            if( ( sock2.receiveMessageLength + data.Length + 4 ) > NetworkDef.NET_MAXMESSAGE )
                Utilities.Error( "Loop_SendMessage: overflow\n" );

            // message type
            var offset = sock2.receiveMessageLength;
            sock2.receiveMessage[offset++] = 1;

            // length
            sock2.receiveMessage[offset++] = ( Byte ) ( data.Length & 0xff );
            sock2.receiveMessage[offset++] = ( Byte ) ( data.Length >> 8 );

            // align
            offset++;

            // message
            Buffer.BlockCopy( data.Data, 0, sock2.receiveMessage, offset, data.Length );
            sock2.receiveMessageLength = IntAlign( sock2.receiveMessageLength + data.Length + 4 );

            sock.canSend = false;
            return 1;
        }

        public Int32 SendUnreliableMessage( qsocket_t sock, MessageWriter data )
        {
            if( sock.driverdata == null )
                return -1;

            var sock2 = (qsocket_t)sock.driverdata;

            if( ( sock2.receiveMessageLength + data.Length + sizeof( Byte ) + sizeof( Int16 ) ) > NetworkDef.NET_MAXMESSAGE )
                return 0;

            var offset = sock2.receiveMessageLength;

            // message type
            sock2.receiveMessage[offset++] = 2;

            // length
            sock2.receiveMessage[offset++] = ( Byte ) ( data.Length & 0xff );
            sock2.receiveMessage[offset++] = ( Byte ) ( data.Length >> 8 );

            // align
            offset++;

            // message
            Buffer.BlockCopy( data.Data, 0, sock2.receiveMessage, offset, data.Length );
            sock2.receiveMessageLength = IntAlign( sock2.receiveMessageLength + data.Length + 4 );

            return 1;
        }

        public Boolean CanSendMessage( qsocket_t sock )
        {
            if( sock.driverdata == null )
                return false;
            return sock.canSend;
        }

        public Boolean CanSendUnreliableMessage( qsocket_t sock )
        {
            return true;
        }

        public void Close( qsocket_t sock )
        {
            if( sock.driverdata != null )
                ( (qsocket_t)sock.driverdata ).driverdata = null;

            sock.ClearBuffers();
            sock.canSend = true;
            if( sock == _Client )
                _Client = null;
            else
                _Server = null;
        }

        public void Shutdown()
        {
            _IsInitialised = false;
        }

        private Int32 IntAlign( Int32 value )
        {
            return ( value + ( sizeof( Int32 ) - 1 ) ) & ( ~( sizeof( Int32 ) - 1 ) );
        }

        #endregion INetDriver Members
    }
}
