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
using System.Runtime.InteropServices;
using SharpQuake.Framework;

namespace SharpQuake
{
    internal class net_datagram : INetDriver
    {
        public static net_datagram Instance
        {
            get
            {
                return _Singletone;
            }
        }

        private static net_datagram _Singletone = new net_datagram();

        private Int32 _DriverLevel;
        private Boolean _IsInitialised;
        private Byte[] _PacketBuffer;

        // statistic counters
        private Int32 packetsSent;

        private Int32 packetsReSent;
        private Int32 packetsReceived;
        private Int32 receivedDuplicateCount;
        private Int32 shortPacketCount;
        private Int32 droppedDatagrams;
        //

        private static String StrAddr( EndPoint ep )
        {
            return ep.ToString();
        }

        // NET_Stats_f
        private void Stats_f()
        {
            if( Host.Command.Argc == 1 )
            {
                Host.Console.Print( "unreliable messages sent   = %i\n", Host.Network.UnreliableMessagesSent );
                Host.Console.Print( "unreliable messages recv   = %i\n", Host.Network.UnreliableMessagesReceived );
                Host.Console.Print( "reliable messages sent     = %i\n", Host.Network.MessagesSent );
                Host.Console.Print( "reliable messages received = %i\n", Host.Network.MessagesReceived );
                Host.Console.Print( "packetsSent                = %i\n", packetsSent );
                Host.Console.Print( "packetsReSent              = %i\n", packetsReSent );
                Host.Console.Print( "packetsReceived            = %i\n", packetsReceived );
                Host.Console.Print( "receivedDuplicateCount     = %i\n", receivedDuplicateCount );
                Host.Console.Print( "shortPacketCount           = %i\n", shortPacketCount );
                Host.Console.Print( "droppedDatagrams           = %i\n", droppedDatagrams );
            }
            else if( Host.Command.Argv( 1 ) == "*" )
            {
                foreach( var s in Host.Network.ActiveSockets )
                    PrintStats( s );

                foreach( var s in Host.Network.FreeSockets )
                    PrintStats( s );
            }
            else
            {
                qsocket_t sock = null;
                var cmdAddr = Host.Command.Argv( 1 );

                foreach( var s in Host.Network.ActiveSockets )
                    if( Utilities.SameText( s.address, cmdAddr ) )
                    {
                        sock = s;
                        break;
                    }

                if( sock == null )
                    foreach( var s in Host.Network.FreeSockets )
                        if( Utilities.SameText( s.address, cmdAddr ) )
                        {
                            sock = s;
                            break;
                        }
                if( sock == null )
                    return;
                PrintStats( sock );
            }
        }

        // PrintStats(qsocket_t* s)
        private void PrintStats( qsocket_t s )
        {
            Host.Console.Print( "canSend = {0:4}   \n", s.canSend );
            Host.Console.Print( "sendSeq = {0:4}   ", s.sendSequence );
            Host.Console.Print( "recvSeq = {0:4}   \n", s.receiveSequence );
            Host.Console.Print( "\n" );
        }

        private net_datagram()
        {
            _PacketBuffer = new Byte[NetworkDef.NET_DATAGRAMSIZE];
        }

        #region INetDriver Members

        public String Name
        {
            get
            {
                return "Datagram";
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

            _DriverLevel = Array.IndexOf( Host.Network.Drivers, this );
            Host.Command.Add( "net_stats", Stats_f );

            if( CommandLine.HasParam( "-nolan" ) )
                return;

            foreach ( var driver in Host.Network.LanDrivers )
            {
                if ( driver is net_tcp_ip )
                {
                    var tcpIP = ( ( net_tcp_ip ) driver );

                    tcpIP.HostName = CVar.GetString( "hostname" );
                    tcpIP.HostPort = Host.Network.HostPort;
                }

                driver.Initialise( );

                if ( driver is net_tcp_ip )
                {
                    var tcpIP = ( ( net_tcp_ip ) driver );

                    Host.Network.MyTcpIpAddress = tcpIP.HostAddress;

                    CVar.Set( "hostname", tcpIP.HostName );
                }
            }

#if BAN_TEST
	        Cmd_AddCommand ("ban", NET_Ban_f);
#endif
            //Cmd.Add("test", Test_f);
            //Cmd.Add("test2", Test2_f);

            _IsInitialised = true;
        }

        /// <summary>
        /// Datagram_Listen
        /// </summary>
        public void Listen( Boolean state )
        {
            foreach( var drv in Host.Network.LanDrivers )
            {
                if( drv.IsInitialised )
                    drv.Listen( state );
            }
        }

        /// <summary>
        /// Datagram_SearchForHosts
        /// </summary>
        public void SearchForHosts( Boolean xmit )
        {
            for( Host.Network.LanDriverLevel = 0; Host.Network.LanDriverLevel < Host.Network.LanDrivers.Length; Host.Network.LanDriverLevel++ )
            {
                if( Host.Network.HostCacheCount == NetworkDef.HOSTCACHESIZE )
                    break;
                if( Host.Network.LanDrivers[Host.Network.LanDriverLevel].IsInitialised )
                    InternalSearchForHosts( xmit );
            }
        }

        /// <summary>
        /// Datagram_Connect
        /// </summary>
        public qsocket_t Connect( String host )
        {
            qsocket_t ret = null;

            for( Host.Network.LanDriverLevel = 0; Host.Network.LanDriverLevel < Host.Network.LanDrivers.Length; Host.Network.LanDriverLevel++ )
                if( Host.Network.LanDrivers[Host.Network.LanDriverLevel].IsInitialised )
                {
                    ret = InternalConnect( host );
                    if( ret != null )
                        break;
                }
            return ret;
        }

        /// <summary>
        /// Datagram_CheckNewConnections
        /// </summary>
        public qsocket_t CheckNewConnections()
        {
            qsocket_t ret = null;

            for( Host.Network.LanDriverLevel = 0; Host.Network.LanDriverLevel < Host.Network.LanDrivers.Length; Host.Network.LanDriverLevel++ )
                if( Host.Network.LanDriver.IsInitialised )
                {
                    ret = InternalCheckNewConnections();
                    if( ret != null )
                        break;
                }
            return ret;
        }

        /// <summary>
        /// _Datagram_CheckNewConnections
        /// </summary>
        public qsocket_t InternalCheckNewConnections()
        {
            var acceptsock = Host.Network.LanDriver.CheckNewConnections();
            if( acceptsock == null )
                return null;

            EndPoint clientaddr = new IPEndPoint( IPAddress.Any, 0 );
            Host.Network.Message.FillFrom( Host.Network, acceptsock, ref clientaddr );

            if( Host.Network.Message.Length < sizeof( Int32 ) )
                return null;

            Host.Network.Reader.Reset();
            var control = EndianHelper.BigLong( Host.Network.Reader.ReadLong() );
            if( control == -1 )
                return null;
            if( ( control & ( ~NetFlags.NETFLAG_LENGTH_MASK ) ) != NetFlags.NETFLAG_CTL )
                return null;
            if( ( control & NetFlags.NETFLAG_LENGTH_MASK ) != Host.Network.Message.Length )
                return null;

            var command = Host.Network.Reader.ReadByte();
            if( command == CCReq.CCREQ_SERVER_INFO )
            {
                var tmp = Host.Network.Reader.ReadString();
                if( tmp != "QUAKE" )
                    return null;

                Host.Network.Message.Clear();

                // save space for the header, filled in later
                Host.Network.Message.WriteLong( 0 );
                Host.Network.Message.WriteByte( CCRep.CCREP_SERVER_INFO );
                var newaddr = acceptsock.LocalEndPoint; //dfunc.GetSocketAddr(acceptsock, &newaddr);
                Host.Network.Message.WriteString( newaddr.ToString() ); // dfunc.AddrToString(&newaddr));
                Host.Network.Message.WriteString( Host.Network.HostName );
                Host.Network.Message.WriteString( Host.Server.sv.name );
                Host.Network.Message.WriteByte( Host.Network.ActiveConnections );
                Host.Network.Message.WriteByte( Host.Server.svs.maxclients );
                Host.Network.Message.WriteByte( NetworkDef.NET_PROTOCOL_VERSION );
                Utilities.WriteInt( Host.Network.Message.Data, 0, EndianHelper.BigLong( NetFlags.NETFLAG_CTL |
                    ( Host.Network.Message.Length & NetFlags.NETFLAG_LENGTH_MASK ) ) );
                Host.Network.LanDriver.Write( acceptsock, Host.Network.Message.Data, Host.Network.Message.Length, clientaddr );
                Host.Network.Message.Clear();
                return null;
            }

            if( command == CCReq.CCREQ_PLAYER_INFO )
            {
                var playerNumber = Host.Network.Reader.ReadByte();
                Int32 clientNumber, activeNumber = -1;
                client_t client = null;
                for( clientNumber = 0; clientNumber < Host.Server.svs.maxclients; clientNumber++ )
                {
                    client = Host.Server.svs.clients[clientNumber];
                    if( client.active )
                    {
                        activeNumber++;
                        if( activeNumber == playerNumber )
                            break;
                    }
                }
                if( clientNumber == Host.Server.svs.maxclients )
                    return null;

                Host.Network.Message.Clear();
                // save space for the header, filled in later
                Host.Network.Message.WriteLong( 0 );
                Host.Network.Message.WriteByte( CCRep.CCREP_PLAYER_INFO );
                Host.Network.Message.WriteByte( playerNumber );
                Host.Network.Message.WriteString( client.name );
                Host.Network.Message.WriteLong( client.colors );
                Host.Network.Message.WriteLong( ( Int32 ) client.edict.v.frags );
                Host.Network.Message.WriteLong( ( Int32 ) ( Host.Network.Time - client.netconnection.connecttime ) );
                Host.Network.Message.WriteString( client.netconnection.address );
                Utilities.WriteInt( Host.Network.Message.Data, 0, EndianHelper.BigLong( NetFlags.NETFLAG_CTL |
                    ( Host.Network.Message.Length & NetFlags.NETFLAG_LENGTH_MASK ) ) );
                Host.Network.LanDriver.Write( acceptsock, Host.Network.Message.Data, Host.Network.Message.Length, clientaddr );
                Host.Network.Message.Clear();

                return null;
            }

            if( command == CCReq.CCREQ_RULE_INFO )
            {
                // find the search start location
                var prevCvarName = Host.Network.Reader.ReadString();
                CVar var;
                if( !String.IsNullOrEmpty( prevCvarName ) )
                {
                    var = CVar.Find( prevCvarName );
                    if( var == null )
                        return null;
                    var = var.Next;
                }
                else
                    var = CVar.First;

                // search for the next server cvar
                while( var != null )
                {
                    if( var.IsServer )
                        break;
                    var = var.Next;
                }

                // send the response
                Host.Network.Message.Clear();

                // save space for the header, filled in later
                Host.Network.Message.WriteLong( 0 );
                Host.Network.Message.WriteByte( CCRep.CCREP_RULE_INFO );
                if( var != null )
                {
                    Host.Network.Message.WriteString( var.Name );
                    Host.Network.Message.WriteString( var.String );
                }
                Utilities.WriteInt( Host.Network.Message.Data, 0, EndianHelper.BigLong( NetFlags.NETFLAG_CTL |
                    ( Host.Network.Message.Length & NetFlags.NETFLAG_LENGTH_MASK ) ) );
                Host.Network.LanDriver.Write( acceptsock, Host.Network.Message.Data, Host.Network.Message.Length, clientaddr );
                Host.Network.Message.Clear();

                return null;
            }

            if( command != CCReq.CCREQ_CONNECT )
                return null;

            if( Host.Network.Reader.ReadString() != "QUAKE" )
                return null;

            if( Host.Network.Reader.ReadByte() != NetworkDef.NET_PROTOCOL_VERSION )
            {
                Host.Network.Message.Clear();
                // save space for the header, filled in later
                Host.Network.Message.WriteLong( 0 );
                Host.Network.Message.WriteByte( CCRep.CCREP_REJECT );
                Host.Network.Message.WriteString( "Incompatible version.\n" );
                Utilities.WriteInt( Host.Network.Message.Data, 0, EndianHelper.BigLong( NetFlags.NETFLAG_CTL |
                    ( Host.Network.Message.Length & NetFlags.NETFLAG_LENGTH_MASK ) ) );
                Host.Network.LanDriver.Write( acceptsock, Host.Network.Message.Data, Host.Network.Message.Length, clientaddr );
                Host.Network.Message.Clear();
                return null;
            }

#if BAN_TEST
            // check for a ban
            if (clientaddr.sa_family == AF_INET)
            {
                unsigned long testAddr;
                testAddr = ((struct sockaddr_in *)&clientaddr)->sin_addr.s_addr;
                if ((testAddr & banMask) == banAddr)
                {
                    SZ_Clear(&net_message);
                    // save space for the header, filled in later
                    MSG_WriteLong(&net_message, 0);
                    MSG_WriteByte(&net_message, CCREP_REJECT);
                    MSG_WriteString(&net_message, "You have been banned.\n");
                    *((int *)net_message.data) = BigLong(NETFLAG_CTL | (net_message.cursize & NETFLAG_LENGTH_MASK));
                    dfunc.Write (acceptsock, net_message.data, net_message.cursize, &clientaddr);
                    SZ_Clear(&net_message);
                    return NULL;
                }
            }
#endif

            // see if this guy is already connected
            foreach( var s in Host.Network.ActiveSockets )
            {
                if( s.driver != Host.Network.DriverLevel )
                    continue;

                var ret = Host.Network.LanDriver.AddrCompare( clientaddr, s.addr );
                if( ret >= 0 )
                {
                    // is this a duplicate connection reqeust?
                    if( ret == 0 && Host.Network.Time - s.connecttime < 2.0 )
                    {
                        // yes, so send a duplicate reply
                        Host.Network.Message.Clear();
                        // save space for the header, filled in later
                        Host.Network.Message.WriteLong( 0 );
                        Host.Network.Message.WriteByte( CCRep.CCREP_ACCEPT );
                        var newaddr = s.socket.LocalEndPoint; //dfunc.GetSocketAddr(s.socket, &newaddr);
                        Host.Network.Message.WriteLong( Host.Network.LanDriver.GetSocketPort( newaddr ) );
                        Utilities.WriteInt( Host.Network.Message.Data, 0, EndianHelper.BigLong( NetFlags.NETFLAG_CTL |
                            ( Host.Network.Message.Length & NetFlags.NETFLAG_LENGTH_MASK ) ) );
                        Host.Network.LanDriver.Write( acceptsock, Host.Network.Message.Data, Host.Network.Message.Length, clientaddr );
                        Host.Network.Message.Clear();
                        return null;
                    }
                    // it's somebody coming back in from a crash/disconnect
                    // so close the old qsocket and let their retry get them back in
                    Host.Network.Close( s );
                    return null;
                }
            }

            // allocate a QSocket
            var sock = Host.Network.NewSocket();
            if( sock == null )
            {
                // no room; try to let him know
                Host.Network.Message.Clear();
                // save space for the header, filled in later
                Host.Network.Message.WriteLong( 0 );
                Host.Network.Message.WriteByte( CCRep.CCREP_REJECT );
                Host.Network.Message.WriteString( "Server is full.\n" );
                Utilities.WriteInt( Host.Network.Message.Data, 0, EndianHelper.BigLong( NetFlags.NETFLAG_CTL |
                    ( Host.Network.Message.Length & NetFlags.NETFLAG_LENGTH_MASK ) ) );
                Host.Network.LanDriver.Write( acceptsock, Host.Network.Message.Data, Host.Network.Message.Length, clientaddr );
                Host.Network.Message.Clear();
                return null;
            }

            // allocate a network socket
            var newsock = Host.Network.LanDriver.OpenSocket( 0 );
            if( newsock == null )
            {
                Host.Network.FreeSocket( sock );
                return null;
            }

            // connect to the client
            if( Host.Network.LanDriver.Connect( newsock, clientaddr ) == -1 )
            {
                Host.Network.LanDriver.CloseSocket( newsock );
                Host.Network.FreeSocket( sock );
                return null;
            }

            // everything is allocated, just fill in the details
            sock.socket = newsock;
            sock.landriver = Host.Network.LanDriverLevel;
            sock.addr = clientaddr;
            sock.address = clientaddr.ToString();

            // send him back the info about the server connection he has been allocated
            Host.Network.Message.Clear();
            // save space for the header, filled in later
            Host.Network.Message.WriteLong( 0 );
            Host.Network.Message.WriteByte( CCRep.CCREP_ACCEPT );
            var newaddr2 = newsock.LocalEndPoint;// dfunc.GetSocketAddr(newsock, &newaddr);
            Host.Network.Message.WriteLong( Host.Network.LanDriver.GetSocketPort( newaddr2 ) );
            Utilities.WriteInt( Host.Network.Message.Data, 0, EndianHelper.BigLong( NetFlags.NETFLAG_CTL |
                ( Host.Network.Message.Length & NetFlags.NETFLAG_LENGTH_MASK ) ) );
            Host.Network.LanDriver.Write( acceptsock, Host.Network.Message.Data, Host.Network.Message.Length, clientaddr );
            Host.Network.Message.Clear();

            return sock;
        }

        public Int32 GetMessage( qsocket_t sock )
        {
            if( !sock.canSend )
                if( ( Host.Network.Time - sock.lastSendTime ) > 1.0 )
                    ReSendMessage( sock );

            var ret = 0;
            EndPoint readaddr = new IPEndPoint( IPAddress.Any, 0 );
            while( true )
            {
                var length = sock.Read( _PacketBuffer, NetworkDef.NET_DATAGRAMSIZE, ref readaddr );
                if( length == 0 )
                    break;

                if( length == -1 )
                {
                    Host.Console.Print( "Read error\n" );
                    return -1;
                }

                if( sock.LanDriver.AddrCompare( readaddr, sock.addr ) != 0 )
                {
#if DEBUG
                    Host.Console.DPrint("Forged packet received\n");
                    Host.Console.DPrint("Expected: {0}\n", StrAddr(sock.addr));
                    Host.Console.DPrint("Received: {0}\n", StrAddr(readaddr));
#endif
                    continue;
                }

                if( length < NetworkDef.NET_HEADERSIZE )
                {
                    shortPacketCount++;
                    continue;
                }

                var header = Utilities.BytesToStructure<PacketHeader>( _PacketBuffer, 0 );

                length = EndianHelper.BigLong( header.length );
                var flags = length & ( ~NetFlags.NETFLAG_LENGTH_MASK );
                length &= NetFlags.NETFLAG_LENGTH_MASK;

                if( ( flags & NetFlags.NETFLAG_CTL ) != 0 )
                    continue;

                var sequence = ( UInt32 ) EndianHelper.BigLong( header.sequence );
                packetsReceived++;

                if( ( flags & NetFlags.NETFLAG_UNRELIABLE ) != 0 )
                {
                    if( sequence < sock.unreliableReceiveSequence )
                    {
                        Host.Console.DPrint( "Got a stale datagram\n" );
                        ret = 0;
                        break;
                    }
                    if( sequence != sock.unreliableReceiveSequence )
                    {
                        var count = ( Int32 ) ( sequence - sock.unreliableReceiveSequence );
                        droppedDatagrams += count;
                        Host.Console.DPrint( "Dropped {0} datagram(s)\n", count );
                    }
                    sock.unreliableReceiveSequence = sequence + 1;

                    length -= NetworkDef.NET_HEADERSIZE;

                    Host.Network.Message.FillFrom( _PacketBuffer, PacketHeader.SizeInBytes, length );

                    ret = 2;
                    break;
                }

                if( ( flags & NetFlags.NETFLAG_ACK ) != 0 )
                {
                    if( sequence != ( sock.sendSequence - 1 ) )
                    {
                        Host.Console.DPrint( "Stale ACK received\n" );
                        continue;
                    }
                    if( sequence == sock.ackSequence )
                    {
                        sock.ackSequence++;
                        if( sock.ackSequence != sock.sendSequence )
                            Host.Console.DPrint( "ack sequencing error\n" );
                    }
                    else
                    {
                        Host.Console.DPrint( "Duplicate ACK received\n" );
                        continue;
                    }
                    sock.sendMessageLength -= QDef.MAX_DATAGRAM;
                    if( sock.sendMessageLength > 0 )
                    {
                        Buffer.BlockCopy( sock.sendMessage, QDef.MAX_DATAGRAM, sock.sendMessage, 0, sock.sendMessageLength );
                        sock.sendNext = true;
                    }
                    else
                    {
                        sock.sendMessageLength = 0;
                        sock.canSend = true;
                    }
                    continue;
                }

                if( ( flags & NetFlags.NETFLAG_DATA ) != 0 )
                {
                    header.length = EndianHelper.BigLong( NetworkDef.NET_HEADERSIZE | NetFlags.NETFLAG_ACK );
                    header.sequence = EndianHelper.BigLong( ( Int32 ) sequence );

                    Utilities.StructureToBytes( ref header, _PacketBuffer, 0 );
                    sock.Write( _PacketBuffer, NetworkDef.NET_HEADERSIZE, readaddr );

                    if( sequence != sock.receiveSequence )
                    {
                        receivedDuplicateCount++;
                        continue;
                    }
                    sock.receiveSequence++;

                    length -= NetworkDef.NET_HEADERSIZE;

                    if( ( flags & NetFlags.NETFLAG_EOM ) != 0 )
                    {
                        Host.Network.Message.Clear();
                        Host.Network.Message.FillFrom( sock.receiveMessage, 0, sock.receiveMessageLength );
                        Host.Network.Message.AppendFrom( _PacketBuffer, PacketHeader.SizeInBytes, length );
                        sock.receiveMessageLength = 0;

                        ret = 1;
                        break;
                    }

                    Buffer.BlockCopy( _PacketBuffer, PacketHeader.SizeInBytes, sock.receiveMessage, sock.receiveMessageLength, length );
                    sock.receiveMessageLength += length;
                    continue;
                }
            }

            if( sock.sendNext )
                SendMessageNext( sock );

            return ret;
        }

        /// <summary>
        /// Datagram_SendMessage
        /// </summary>
        public Int32 SendMessage( qsocket_t sock, MessageWriter data )
        {
#if DEBUG
            if (data.IsEmpty)
                Utilities.Error("Datagram_SendMessage: zero length message\n");

            if (data.Length > NetworkDef.NET_MAXMESSAGE)
                Utilities.Error("Datagram_SendMessage: message too big {0}\n", data.Length);

            if (!sock.canSend)
                Utilities.Error("SendMessage: called with canSend == false\n");
#endif
            Buffer.BlockCopy( data.Data, 0, sock.sendMessage, 0, data.Length );
            sock.sendMessageLength = data.Length;

            Int32 dataLen, eom;
            if( data.Length <= QDef.MAX_DATAGRAM )
            {
                dataLen = data.Length;
                eom = NetFlags.NETFLAG_EOM;
            }
            else
            {
                dataLen = QDef.MAX_DATAGRAM;
                eom = 0;
            }
            var packetLen = NetworkDef.NET_HEADERSIZE + dataLen;

            PacketHeader header;
            header.length = EndianHelper.BigLong( packetLen | NetFlags.NETFLAG_DATA | eom );
            header.sequence = EndianHelper.BigLong( ( Int32 ) sock.sendSequence++ );
            Utilities.StructureToBytes( ref header, _PacketBuffer, 0 );
            Buffer.BlockCopy( data.Data, 0, _PacketBuffer, PacketHeader.SizeInBytes, dataLen );

            sock.canSend = false;

            if( sock.Write( _PacketBuffer, packetLen, sock.addr ) == -1 )
                return -1;

            sock.lastSendTime = Host.Network.Time;
            packetsSent++;
            return 1;
        }

        /// <summary>
        /// Datagram_SendUnreliableMessage
        /// </summary>
        public Int32 SendUnreliableMessage( qsocket_t sock, MessageWriter data )
        {
            Int32 packetLen;

#if DEBUG
            if (data.IsEmpty)
                Utilities.Error("Datagram_SendUnreliableMessage: zero length message\n");

            if (data.Length > QDef.MAX_DATAGRAM)
                Utilities.Error("Datagram_SendUnreliableMessage: message too big {0}\n", data.Length);
#endif

            packetLen = NetworkDef.NET_HEADERSIZE + data.Length;

            PacketHeader header;
            header.length = EndianHelper.BigLong( packetLen | NetFlags.NETFLAG_UNRELIABLE );
            header.sequence = EndianHelper.BigLong( ( Int32 ) sock.unreliableSendSequence++ );
            Utilities.StructureToBytes( ref header, _PacketBuffer, 0 );
            Buffer.BlockCopy( data.Data, 0, _PacketBuffer, PacketHeader.SizeInBytes, data.Length );

            if( sock.Write( _PacketBuffer, packetLen, sock.addr ) == -1 )
                return -1;

            packetsSent++;
            return 1;
        }

        /// <summary>
        /// Datagram_CanSendMessage
        /// </summary>
        public Boolean CanSendMessage( qsocket_t sock )
        {
            if( sock.sendNext )
                SendMessageNext( sock );

            return sock.canSend;
        }

        /// <summary>
        /// Datagram_CanSendUnreliableMessage
        /// </summary>
        public Boolean CanSendUnreliableMessage( qsocket_t sock )
        {
            return true;
        }

        /// <summary>
        /// Datagram_Close
        /// </summary>
        public void Close( qsocket_t sock )
        {
            sock.LanDriver.CloseSocket( sock.socket );
        }

        /// <summary>
        /// Datagram_Shutdown
        /// </summary>
        public void Shutdown()
        {
            //
            // shutdown the lan drivers
            //
            foreach( var driver in Host.Network.LanDrivers )
            {
                if( driver.IsInitialised )
                    driver.Dispose();
            }

            _IsInitialised = false;
        }

        /// <summary>
        /// _Datagram_SearchForHosts
        /// </summary>
        private void InternalSearchForHosts( Boolean xmit )
        {
            var myaddr = Host.Network.LanDriver.ControlSocket.LocalEndPoint;
            if( xmit )
            {
                Host.Network.Message.Clear();
                // save space for the header, filled in later
                Host.Network.Message.WriteLong( 0 );
                Host.Network.Message.WriteByte( CCReq.CCREQ_SERVER_INFO );
                Host.Network.Message.WriteString( "QUAKE" );
                Host.Network.Message.WriteByte( NetworkDef.NET_PROTOCOL_VERSION );
                Utilities.WriteInt( Host.Network.Message.Data, 0, EndianHelper.BigLong( NetFlags.NETFLAG_CTL |
                    ( Host.Network.Message.Length & NetFlags.NETFLAG_LENGTH_MASK ) ) );
                Host.Network.LanDriver.Broadcast( Host.Network.LanDriver.ControlSocket, Host.Network.Message.Data, Host.Network.Message.Length );
                Host.Network.Message.Clear();
            }

            EndPoint readaddr = new IPEndPoint( IPAddress.Any, 0 );
            while( true )
            {
                Host.Network.Message.FillFrom( Host.Network, Host.Network.LanDriver.ControlSocket, ref readaddr );
                if( Host.Network.Message.IsEmpty )
                    break;
                if( Host.Network.Message.Length < sizeof( Int32 ) )
                    continue;

                // don't answer our own query
                if( Host.Network.LanDriver.AddrCompare( readaddr, myaddr ) >= 0 )
                    continue;

                // is the cache full?
                if( Host.Network.HostCacheCount == NetworkDef.HOSTCACHESIZE )
                    continue;

                Host.Network.Reader.Reset();
                var control = EndianHelper.BigLong( Host.Network.Reader.ReadLong() );// BigLong(*((int *)net_message.data));
                //MSG_ReadLong();
                if( control == -1 )
                    continue;
                if( ( control & ( ~NetFlags.NETFLAG_LENGTH_MASK ) ) != NetFlags.NETFLAG_CTL )
                    continue;
                if( ( control & NetFlags.NETFLAG_LENGTH_MASK ) != Host.Network.Message.Length )
                    continue;

                if( Host.Network.Reader.ReadByte() != CCRep.CCREP_SERVER_INFO )
                    continue;

                var _hostIP = readaddr;

                readaddr = Host.Network.LanDriver.GetAddrFromName( Host.Network.Reader.ReadString() );
                Int32 n;
                // search the cache for this server
                for( n = 0; n < Host.Network.HostCacheCount; n++ )
                    if( Host.Network.LanDriver.AddrCompare( readaddr, Host.Network.HostCache[n].addr ) == 0 )
                        break;

                // is it already there?
                if( n < Host.Network.HostCacheCount )
                    continue;

                // add it
                Host.Network.HostCacheCount++;
                var hc = Host.Network.HostCache[n];
                hc.name = Host.Network.Reader.ReadString();
                hc.map = Host.Network.Reader.ReadString();
                hc.users = Host.Network.Reader.ReadByte();
                hc.maxusers = Host.Network.Reader.ReadByte();
                if( Host.Network.Reader.ReadByte() != NetworkDef.NET_PROTOCOL_VERSION )
                {
                    hc.cname = hc.name;
                    hc.name = "*" + hc.name;
                }
                //IPEndPoint ep = (IPEndPoint)readaddr;
                //hc.addr = new IPEndPoint( ep.Address, ep.Port );
                var ip = readaddr.ToString().Split(':'); //readaddr.ToString()
                IPAddress _ipAddress;
                Int32 _port;
                IPAddress.TryParse(ip[0].ToString(), out _ipAddress);
                Int32.TryParse(ip[1].ToString(), out _port);
                hc.addr = new IPEndPoint(_ipAddress, _port);
                hc.driver = Host.Network.DriverLevel;
                hc.ldriver = Host.Network.LanDriverLevel;
                hc.cname = _hostIP.ToString(); //readaddr.ToString();

                // check for a name conflict
                for( var i = 0; i < Host.Network.HostCacheCount; i++ )
                {
                    if( i == n )
                        continue;
                    var hc2 = Host.Network.HostCache[i];
                    if( hc.name == hc2.name )
                    {
                        i = hc.name.Length;
                        if( i < 15 && hc.name[i - 1] > '8' )
                        {
                            hc.name = hc.name.Substring( 0, i ) + '0';
                        }
                        else
                            hc.name = hc.name.Substring( 0, i - 1 ) + ( Char ) ( hc.name[i - 1] + 1 );
                        i = 0;// -1;
                    }
                }
            }
        }

        /// <summary>
        /// _Datagram_Connect
        /// </summary>
        private qsocket_t InternalConnect( String host )
        {
            // see if we can resolve the host name
            var sendaddr = Host.Network.LanDriver.GetAddrFromName( host );
            if( sendaddr == null )
                return null;

            var newsock = Host.Network.LanDriver.OpenSocket( 0 );
            if( newsock == null )
                return null;

            var sock = Host.Network.NewSocket();
            if( sock == null )
                goto ErrorReturn2;
            sock.socket = newsock;
            sock.landriver = Host.Network.LanDriverLevel;

            // connect to the host
            if( Host.Network.LanDriver.Connect( newsock, sendaddr ) == -1 )
                goto ErrorReturn;

            // send the connection request
            Host.Console.Print( "Connecting to " + sendaddr + "\n" );
            Host.Console.Print( "trying...\n" );
            Host.Screen.UpdateScreen();
            var start_time = Host.Network.Time;
            var ret = 0;
            for( var reps = 0; reps < 3; reps++ )
            {
                Host.Network.Message.Clear();
                // save space for the header, filled in later
                Host.Network.Message.WriteLong( 0 );
                Host.Network.Message.WriteByte( CCReq.CCREQ_CONNECT );
                Host.Network.Message.WriteString( "QUAKE" );
                Host.Network.Message.WriteByte( NetworkDef.NET_PROTOCOL_VERSION );
                Utilities.WriteInt( Host.Network.Message.Data, 0, EndianHelper.BigLong( NetFlags.NETFLAG_CTL |
                    ( Host.Network.Message.Length & NetFlags.NETFLAG_LENGTH_MASK ) ) );
                //*((int *)net_message.data) = BigLong(NETFLAG_CTL | (net_message.cursize & NETFLAG_LENGTH_MASK));
                Host.Network.LanDriver.Write( newsock, Host.Network.Message.Data, Host.Network.Message.Length, sendaddr );
                Host.Network.Message.Clear();
                EndPoint readaddr = new IPEndPoint( IPAddress.Any, 0 );
                do
                {
                    ret = Host.Network.Message.FillFrom( Host.Network, newsock, ref readaddr );
                    // if we got something, validate it
                    if( ret > 0 )
                    {
                        // is it from the right place?
                        if( sock.LanDriver.AddrCompare( readaddr, sendaddr ) != 0 )
                        {
#if DEBUG
                            Host.Console.Print("wrong reply address\n");
                            Host.Console.Print("Expected: {0}\n", StrAddr(sendaddr));
                            Host.Console.Print("Received: {0}\n", StrAddr(readaddr));
                            Host.Screen.UpdateScreen();
#endif
                            ret = 0;
                            continue;
                        }

                        if( ret < sizeof( Int32 ) )
                        {
                            ret = 0;
                            continue;
                        }

                        Host.Network.Reader.Reset();

                        var control = EndianHelper.BigLong( Host.Network.Reader.ReadLong() );// BigLong(*((int *)net_message.data));
                        //MSG_ReadLong();
                        if( control == -1 )
                        {
                            ret = 0;
                            continue;
                        }
                        if( ( control & ( ~NetFlags.NETFLAG_LENGTH_MASK ) ) != NetFlags.NETFLAG_CTL )
                        {
                            ret = 0;
                            continue;
                        }
                        if( ( control & NetFlags.NETFLAG_LENGTH_MASK ) != ret )
                        {
                            ret = 0;
                            continue;
                        }
                    }
                }
                while( ( ret == 0 ) && ( Host.Network.SetNetTime() - start_time ) < 2.5 );
                if( ret > 0 )
                    break;
                Host.Console.Print( "still trying...\n" );
                Host.Screen.UpdateScreen();
                start_time = Host.Network.SetNetTime();
            }

            var reason = String.Empty;
            if( ret == 0 )
            {
                reason = "No Response";
                Host.Console.Print( "{0}\n", reason );
                Host.Menu.ReturnReason = reason;
                goto ErrorReturn;
            }

            if( ret == -1 )
            {
                reason = "Network Error";
                Host.Console.Print( "{0}\n", reason );
                Host.Menu.ReturnReason = reason;
                goto ErrorReturn;
            }

            ret = Host.Network.Reader.ReadByte();
            if( ret == CCRep.CCREP_REJECT )
            {
                reason = Host.Network.Reader.ReadString();
                Host.Console.Print( reason );
                Host.Menu.ReturnReason = reason;
                goto ErrorReturn;
            }

            if( ret == CCRep.CCREP_ACCEPT )
            {
                var ep = (IPEndPoint)sendaddr;
                sock.addr = new IPEndPoint( ep.Address, ep.Port );
                Host.Network.LanDriver.SetSocketPort( sock.addr, Host.Network.Reader.ReadLong() );
            }
            else
            {
                reason = "Bad Response";
                Host.Console.Print( "{0}\n", reason );
                Host.Menu.ReturnReason = reason;
                goto ErrorReturn;
            }

            sock.address = Host.Network.LanDriver.GetNameFromAddr( sendaddr );

            Host.Console.Print( "Connection accepted\n" );
            sock.lastMessageTime = Host.Network.SetNetTime();

            // switch the connection to the specified address
            if( Host.Network.LanDriver.Connect( newsock, sock.addr ) == -1 )
            {
                reason = "Connect to Game failed";
                Host.Console.Print( "{0}\n", reason );
                Host.Menu.ReturnReason = reason;
                goto ErrorReturn;
            }

            Host.Menu.ReturnOnError = false;
            return sock;

ErrorReturn:
            Host.Network.FreeSocket( sock );
ErrorReturn2:
            Host.Network.LanDriver.CloseSocket( newsock );
            if( Host.Menu.ReturnOnError && Host.Menu.ReturnMenu != null )
            {
                Host.Menu.ReturnMenu.Show( Host );
                Host.Menu.ReturnOnError = false;
            }
            return null;
        }

        /// <summary>
        /// SendMessageNext
        /// </summary>
        private Int32 SendMessageNext( qsocket_t sock )
        {
            Int32 dataLen;
            Int32 eom;
            if( sock.sendMessageLength <= QDef.MAX_DATAGRAM )
            {
                dataLen = sock.sendMessageLength;
                eom = NetFlags.NETFLAG_EOM;
            }
            else
            {
                dataLen = QDef.MAX_DATAGRAM;
                eom = 0;
            }
            var packetLen = NetworkDef.NET_HEADERSIZE + dataLen;

            PacketHeader header;
            header.length = EndianHelper.BigLong( packetLen | ( NetFlags.NETFLAG_DATA | eom ) );
            header.sequence = EndianHelper.BigLong( ( Int32 ) sock.sendSequence++ );
            Utilities.StructureToBytes( ref header, _PacketBuffer, 0 );
            Buffer.BlockCopy( sock.sendMessage, 0, _PacketBuffer, PacketHeader.SizeInBytes, dataLen );

            sock.sendNext = false;

            if( sock.Write( _PacketBuffer, packetLen, sock.addr ) == -1 )
                return -1;

            sock.lastSendTime = Host.Network.Time;
            packetsSent++;
            return 1;
        }

        /// <summary>
        /// ReSendMessage
        /// </summary>
        private Int32 ReSendMessage( qsocket_t sock )
        {
            Int32 dataLen, eom;
            if( sock.sendMessageLength <= QDef.MAX_DATAGRAM )
            {
                dataLen = sock.sendMessageLength;
                eom = NetFlags.NETFLAG_EOM;
            }
            else
            {
                dataLen = QDef.MAX_DATAGRAM;
                eom = 0;
            }
            var packetLen = NetworkDef.NET_HEADERSIZE + dataLen;

            PacketHeader header;
            header.length = EndianHelper.BigLong( packetLen | ( NetFlags.NETFLAG_DATA | eom ) );
            header.sequence = EndianHelper.BigLong( ( Int32 ) ( sock.sendSequence - 1 ) );
            Utilities.StructureToBytes( ref header, _PacketBuffer, 0 );
            Buffer.BlockCopy( sock.sendMessage, 0, _PacketBuffer, PacketHeader.SizeInBytes, dataLen );

            sock.sendNext = false;

            if( sock.Write( _PacketBuffer, packetLen, sock.addr ) == -1 )
                return -1;

            sock.lastSendTime = Host.Network.Time;
            packetsReSent++;
            return 1;
        }

        #endregion INetDriver Members
    }
}
