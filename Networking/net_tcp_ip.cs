/// <copyright>
///
/// Rewritten in C# by Yury Kiselev, 2010.
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

namespace SharpQuake
{
    internal class net_tcp_ip : INetLanDriver
    {
        public static net_tcp_ip Instance
        {
            get
            {
                return _Singletone;
            }
        }

        private const int WSAEWOULDBLOCK = 10035;
        private const int WSAECONNREFUSED = 10061;

        private static net_tcp_ip _Singletone = new net_tcp_ip();

        private bool _IsInitialized;
        private IPAddress _MyAddress; // unsigned long myAddr
        private Socket _ControlSocket; // int net_controlsocket;
        private Socket _BroadcastSocket; // net_broadcastsocket
        private EndPoint _BroadcastAddress; // qsockaddr broadcastaddr
        private Socket _AcceptSocket; // net_acceptsocket

        private net_tcp_ip()
        {
        }

        #region INetLanDriver Members

        public string Name
        {
            get
            {
                return "TCP/IP";
            }
        }

        public bool IsInitialized
        {
            get
            {
                return _IsInitialized;
            }
        }

        public Socket ControlSocket
        {
            get
            {
                return _ControlSocket;
            }
        }

        /// <summary>
        /// UDP_Init
        /// </summary>
        public bool Init()
        {
            _IsInitialized = false;

            if( Common.HasParam( "-noudp" ) )
                return false;

            // determine my name
            string hostName;
            try
            {
                hostName = Dns.GetHostName();
            }
            catch( SocketException se )
            {
                Con.DPrint( "Cannot get host name: {0}\n", se.Message );
                return false;
            }

            // if the quake hostname isn't set, set it to the machine name
            if( net.HostName == "UNNAMED" )
            {
                IPAddress addr;
                if( !IPAddress.TryParse( hostName, out addr ) )
                {
                    int i = hostName.IndexOf( '.' );
                    if( i != -1 )
                    {
                        hostName = hostName.Substring( 0, i );
                    }
                }
                CVar.Set( "hostname", hostName );
            }

            int i2 = Common.CheckParm( "-ip" );
            if( i2 > 0 )
            {
                if( i2 < Common.Argc - 1 )
                {
                    string ipaddr = Common.Argv( i2 + 1 );
                    if( !IPAddress.TryParse( ipaddr, out _MyAddress ) )
                        sys.Error( "{0} is not a valid IP address!", ipaddr );
                    net.MyTcpIpAddress = ipaddr;
                }
                else
                {
                    sys.Error( "Net.Init: you must specify an IP address after -ip" );
                }
            }
            else
            {
                _MyAddress = IPAddress.Any;
                net.MyTcpIpAddress = "INADDR_ANY";
            }

            _ControlSocket = OpenSocket( 0 );
            if( _ControlSocket == null )
            {
                Con.Print( "TCP/IP: Unable to open control socket\n" );
                return false;
            }

            _BroadcastAddress = new IPEndPoint( IPAddress.Broadcast, net.HostPort );

            _IsInitialized = true;
            Con.Print( "TCP/IP Initialized\n" );
            return true;
        }

        public void Shutdown()
        {
            Listen( false );
            CloseSocket( _ControlSocket );
        }

        /// <summary>
        /// UDP_Listen
        /// </summary>
        public void Listen( bool state )
        {
            // enable listening
            if( state )
            {
                if( _AcceptSocket == null )
                {
                    _AcceptSocket = OpenSocket( net.HostPort );
                    if( _AcceptSocket == null )
                        sys.Error( "UDP_Listen: Unable to open accept socket\n" );
                }
            }
            else
            {
                // disable listening
                if( _AcceptSocket != null )
                {
                    CloseSocket( _AcceptSocket );
                    _AcceptSocket = null;
                }
            }
        }

        public Socket OpenSocket( int port )
        {
            Socket result = null;
            try
            {
                result = new Socket( AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp );
                result.Blocking = false;
                EndPoint ep = new IPEndPoint( _MyAddress, port );
                result.Bind( ep );
            }
            catch( Exception ex )
            {
                if( result != null )
                {
                    result.Close();
                    result = null;
                }
                Con.Print( "Unable to create socket: " + ex.Message );
            }

            return result;
        }

        public int CloseSocket( Socket socket )
        {
            if( socket == _BroadcastSocket )
                _BroadcastSocket = null;

            socket.Close();
            return 0;
        }

        public int Connect( Socket socket, EndPoint addr )
        {
            return 0;
        }

        public string GetNameFromAddr( EndPoint addr )
        {
            try
            {
                IPHostEntry entry = Dns.GetHostEntry( ( (IPEndPoint)addr ).Address );
                return entry.HostName;
            }
            catch( SocketException )
            {
            }
            return String.Empty;
        }

        public EndPoint GetAddrFromName( string name )
        {
            try
            {
                IPAddress addr;
                int i = name.IndexOf( ':' );
                string saddr;
                int port = net.HostPort;
                if( i != -1 )
                {
                    saddr = name.Substring( 0, i );
                    int p;
                    if( int.TryParse( name.Substring( i + 1 ), out p ) )
                        port = p;
                }
                else
                    saddr = name;

                if( IPAddress.TryParse( saddr, out addr ) )
                {
                    return new IPEndPoint( addr, port );
                }
                IPHostEntry entry = Dns.GetHostEntry( name );
                foreach( IPAddress addr2 in entry.AddressList )
                {
                    return new IPEndPoint( addr2, port );
                }
            }
            catch( SocketException )
            {
            }
            return null;
        }

        public int AddrCompare( EndPoint addr1, EndPoint addr2 )
        {
            if( addr1.AddressFamily != addr2.AddressFamily )
                return -1;

            IPEndPoint ep1 = addr1 as IPEndPoint;
            IPEndPoint ep2 = addr2 as IPEndPoint;

            if( ep1 == null || ep2 == null )
                return -1;

            if( !ep1.Address.Equals( ep2.Address ) )
                return -1;

            if( ep1.Port != ep2.Port )
                return 1;

            return 0;
        }

        public int GetSocketPort( EndPoint addr )
        {
            return ( (IPEndPoint)addr ).Port;
        }

        public int SetSocketPort( EndPoint addr, int port )
        {
            ( (IPEndPoint)addr ).Port = port;
            return 0;
        }

        public Socket CheckNewConnections()
        {
            if( _AcceptSocket == null )
                return null;

            if( _AcceptSocket.Available > 0 )
                return _AcceptSocket;

            return null;
        }

        public int Read( Socket socket, byte[] buf, int len, ref EndPoint ep )
        {
            int ret = 0;
            try
            {
                ret = socket.ReceiveFrom( buf, len, SocketFlags.None, ref ep );
            }
            catch( SocketException se )
            {
                if( se.ErrorCode == WSAEWOULDBLOCK || se.ErrorCode == WSAECONNREFUSED )
                    ret = 0;
                else
                    ret = -1;
            }
            return ret;
        }

        public int Write( Socket socket, byte[] buf, int len, EndPoint ep )
        {
            int ret = 0;
            try
            {
                ret = socket.SendTo( buf, len, SocketFlags.None, ep );
            }
            catch( SocketException se )
            {
                if( se.ErrorCode == WSAEWOULDBLOCK )
                    ret = 0;
                else
                    ret = -1;
            }
            return ret;
        }

        public int Broadcast( Socket socket, byte[] buf, int len )
        {
            if( socket != _BroadcastSocket )
            {
                if( _BroadcastSocket != null )
                    sys.Error( "Attempted to use multiple broadcasts sockets\n" );
                try
                {
                    socket.EnableBroadcast = true;
                }
                catch( SocketException se )
                {
                    Con.Print( "Unable to make socket broadcast capable: {0}\n", se.Message );
                    return -1;
                }
            }

            return Write( socket, buf, len, _BroadcastAddress );
        }

        #endregion INetLanDriver Members
    }
}
