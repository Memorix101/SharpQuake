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
using System.Runtime.InteropServices;

namespace SharpQuake
{
    internal class net_datagram : INetDriver
    {
        [StructLayout( LayoutKind.Sequential, Pack = 1 )]
        private struct PacketHeader
        {
            public Int32 length;
            public Int32 sequence;

            public static Int32 SizeInBytes = Marshal.SizeOf(typeof(PacketHeader));
        }

        public static net_datagram Instance
        {
            get
            {
                return _Singletone;
            }
        }

        private static net_datagram _Singletone = new net_datagram();

        private Int32 _DriverLevel;
        private Boolean _IsInitialized;
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
            if( Command.Argc == 1 )
            {
                Con.Print( "unreliable messages sent   = %i\n", net.UnreliableMessagesSent );
                Con.Print( "unreliable messages recv   = %i\n", net.UnreliableMessagesReceived );
                Con.Print( "reliable messages sent     = %i\n", net.MessagesSent );
                Con.Print( "reliable messages received = %i\n", net.MessagesReceived );
                Con.Print( "packetsSent                = %i\n", packetsSent );
                Con.Print( "packetsReSent              = %i\n", packetsReSent );
                Con.Print( "packetsReceived            = %i\n", packetsReceived );
                Con.Print( "receivedDuplicateCount     = %i\n", receivedDuplicateCount );
                Con.Print( "shortPacketCount           = %i\n", shortPacketCount );
                Con.Print( "droppedDatagrams           = %i\n", droppedDatagrams );
            }
            else if( Command.Argv( 1 ) == "*" )
            {
                foreach( qsocket_t s in net.ActiveSockets )
                    PrintStats( s );

                foreach( qsocket_t s in net.FreeSockets )
                    PrintStats( s );
            }
            else
            {
                qsocket_t sock = null;
                var cmdAddr = Command.Argv( 1 );

                foreach( qsocket_t s in net.ActiveSockets )
                    if( Common.SameText( s.address, cmdAddr ) )
                    {
                        sock = s;
                        break;
                    }

                if( sock == null )
                    foreach( qsocket_t s in net.FreeSockets )
                        if( Common.SameText( s.address, cmdAddr ) )
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
            Con.Print( "canSend = {0:4}   \n", s.canSend );
            Con.Print( "sendSeq = {0:4}   ", s.sendSequence );
            Con.Print( "recvSeq = {0:4}   \n", s.receiveSequence );
            Con.Print( "\n" );
        }

        private net_datagram()
        {
            _PacketBuffer = new Byte[net.NET_DATAGRAMSIZE];
        }

        #region INetDriver Members

        public String Name
        {
            get
            {
                return "Datagram";
            }
        }

        public Boolean IsInitialized
        {
            get
            {
                return _IsInitialized;
            }
        }

        public void Init()
        {
            _DriverLevel = Array.IndexOf( net.Drivers, this );
            Command.Add( "net_stats", Stats_f );

            if( Common.HasParam( "-nolan" ) )
                return;

            foreach( INetLanDriver driver in net.LanDrivers )
            {
                driver.Init();
            }

#if BAN_TEST
	        Cmd_AddCommand ("ban", NET_Ban_f);
#endif
            //Cmd.Add("test", Test_f);
            //Cmd.Add("test2", Test2_f);

            _IsInitialized = true;
        }

        /// <summary>
        /// Datagram_Listen
        /// </summary>
        public void Listen( Boolean state )
        {
            foreach( INetLanDriver drv in net.LanDrivers )
            {
                if( drv.IsInitialized )
                    drv.Listen( state );
            }
        }

        /// <summary>
        /// Datagram_SearchForHosts
        /// </summary>
        public void SearchForHosts( Boolean xmit )
        {
            for( net.LanDriverLevel = 0; net.LanDriverLevel < net.LanDrivers.Length; net.LanDriverLevel++ )
            {
                if( net.HostCacheCount == net.HOSTCACHESIZE )
                    break;
                if( net.LanDrivers[net.LanDriverLevel].IsInitialized )
                    InternalSearchForHosts( xmit );
            }
        }

        /// <summary>
        /// Datagram_Connect
        /// </summary>
        public qsocket_t Connect( String host )
        {
            qsocket_t ret = null;

            for( net.LanDriverLevel = 0; net.LanDriverLevel < net.LanDrivers.Length; net.LanDriverLevel++ )
                if( net.LanDrivers[net.LanDriverLevel].IsInitialized )
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

            for( net.LanDriverLevel = 0; net.LanDriverLevel < net.LanDrivers.Length; net.LanDriverLevel++ )
                if( net.LanDriver.IsInitialized )
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
            Socket acceptsock = net.LanDriver.CheckNewConnections();
            if( acceptsock == null )
                return null;

            EndPoint clientaddr = new IPEndPoint( IPAddress.Any, 0 );
            net.Message.FillFrom( acceptsock, ref clientaddr );

            if( net.Message.Length < sizeof( Int32 ) )
                return null;

            net.Reader.Reset();
            var control = Common.BigLong( net.Reader.ReadLong() );
            if( control == -1 )
                return null;
            if( ( control & ( ~NetFlags.NETFLAG_LENGTH_MASK ) ) != NetFlags.NETFLAG_CTL )
                return null;
            if( ( control & NetFlags.NETFLAG_LENGTH_MASK ) != net.Message.Length )
                return null;

            var command = net.Reader.ReadByte();
            if( command == CCReq.CCREQ_SERVER_INFO )
            {
                var tmp = net.Reader.ReadString();
                if( tmp != "QUAKE" )
                    return null;

                net.Message.Clear();

                // save space for the header, filled in later
                net.Message.WriteLong( 0 );
                net.Message.WriteByte( CCRep.CCREP_SERVER_INFO );
                EndPoint newaddr = acceptsock.LocalEndPoint; //dfunc.GetSocketAddr(acceptsock, &newaddr);
                net.Message.WriteString( newaddr.ToString() ); // dfunc.AddrToString(&newaddr));
                net.Message.WriteString( net.HostName );
                net.Message.WriteString( server.sv.name );
                net.Message.WriteByte( net.ActiveConnections );
                net.Message.WriteByte( server.svs.maxclients );
                net.Message.WriteByte( net.NET_PROTOCOL_VERSION );
                Common.WriteInt( net.Message.Data, 0, Common.BigLong( NetFlags.NETFLAG_CTL |
                    ( net.Message.Length & NetFlags.NETFLAG_LENGTH_MASK ) ) );
                net.LanDriver.Write( acceptsock, net.Message.Data, net.Message.Length, clientaddr );
                net.Message.Clear();
                return null;
            }

            if( command == CCReq.CCREQ_PLAYER_INFO )
            {
                var playerNumber = net.Reader.ReadByte();
                Int32 clientNumber, activeNumber = -1;
                client_t client = null;
                for( clientNumber = 0; clientNumber < server.svs.maxclients; clientNumber++ )
                {
                    client = server.svs.clients[clientNumber];
                    if( client.active )
                    {
                        activeNumber++;
                        if( activeNumber == playerNumber )
                            break;
                    }
                }
                if( clientNumber == server.svs.maxclients )
                    return null;

                net.Message.Clear();
                // save space for the header, filled in later
                net.Message.WriteLong( 0 );
                net.Message.WriteByte( CCRep.CCREP_PLAYER_INFO );
                net.Message.WriteByte( playerNumber );
                net.Message.WriteString( client.name );
                net.Message.WriteLong( client.colors );
                net.Message.WriteLong( ( Int32 ) client.edict.v.frags );
                net.Message.WriteLong( ( Int32 ) ( net.Time - client.netconnection.connecttime ) );
                net.Message.WriteString( client.netconnection.address );
                Common.WriteInt( net.Message.Data, 0, Common.BigLong( NetFlags.NETFLAG_CTL |
                    ( net.Message.Length & NetFlags.NETFLAG_LENGTH_MASK ) ) );
                net.LanDriver.Write( acceptsock, net.Message.Data, net.Message.Length, clientaddr );
                net.Message.Clear();

                return null;
            }

            if( command == CCReq.CCREQ_RULE_INFO )
            {
                // find the search start location
                var prevCvarName = net.Reader.ReadString();
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
                net.Message.Clear();

                // save space for the header, filled in later
                net.Message.WriteLong( 0 );
                net.Message.WriteByte( CCRep.CCREP_RULE_INFO );
                if( var != null )
                {
                    net.Message.WriteString( var.Name );
                    net.Message.WriteString( var.String );
                }
                Common.WriteInt( net.Message.Data, 0, Common.BigLong( NetFlags.NETFLAG_CTL |
                    ( net.Message.Length & NetFlags.NETFLAG_LENGTH_MASK ) ) );
                net.LanDriver.Write( acceptsock, net.Message.Data, net.Message.Length, clientaddr );
                net.Message.Clear();

                return null;
            }

            if( command != CCReq.CCREQ_CONNECT )
                return null;

            if( net.Reader.ReadString() != "QUAKE" )
                return null;

            if( net.Reader.ReadByte() != net.NET_PROTOCOL_VERSION )
            {
                net.Message.Clear();
                // save space for the header, filled in later
                net.Message.WriteLong( 0 );
                net.Message.WriteByte( CCRep.CCREP_REJECT );
                net.Message.WriteString( "Incompatible version.\n" );
                Common.WriteInt( net.Message.Data, 0, Common.BigLong( NetFlags.NETFLAG_CTL |
                    ( net.Message.Length & NetFlags.NETFLAG_LENGTH_MASK ) ) );
                net.LanDriver.Write( acceptsock, net.Message.Data, net.Message.Length, clientaddr );
                net.Message.Clear();
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
            foreach( qsocket_t s in net.ActiveSockets )
            {
                if( s.driver != net.DriverLevel )
                    continue;

                var ret = net.LanDriver.AddrCompare( clientaddr, s.addr );
                if( ret >= 0 )
                {
                    // is this a duplicate connection reqeust?
                    if( ret == 0 && net.Time - s.connecttime < 2.0 )
                    {
                        // yes, so send a duplicate reply
                        net.Message.Clear();
                        // save space for the header, filled in later
                        net.Message.WriteLong( 0 );
                        net.Message.WriteByte( CCRep.CCREP_ACCEPT );
                        EndPoint newaddr = s.socket.LocalEndPoint; //dfunc.GetSocketAddr(s.socket, &newaddr);
                        net.Message.WriteLong( net.LanDriver.GetSocketPort( newaddr ) );
                        Common.WriteInt( net.Message.Data, 0, Common.BigLong( NetFlags.NETFLAG_CTL |
                            ( net.Message.Length & NetFlags.NETFLAG_LENGTH_MASK ) ) );
                        net.LanDriver.Write( acceptsock, net.Message.Data, net.Message.Length, clientaddr );
                        net.Message.Clear();
                        return null;
                    }
                    // it's somebody coming back in from a crash/disconnect
                    // so close the old qsocket and let their retry get them back in
                    net.Close( s );
                    return null;
                }
            }

            // allocate a QSocket
            qsocket_t sock = net.NewSocket();
            if( sock == null )
            {
                // no room; try to let him know
                net.Message.Clear();
                // save space for the header, filled in later
                net.Message.WriteLong( 0 );
                net.Message.WriteByte( CCRep.CCREP_REJECT );
                net.Message.WriteString( "Server is full.\n" );
                Common.WriteInt( net.Message.Data, 0, Common.BigLong( NetFlags.NETFLAG_CTL |
                    ( net.Message.Length & NetFlags.NETFLAG_LENGTH_MASK ) ) );
                net.LanDriver.Write( acceptsock, net.Message.Data, net.Message.Length, clientaddr );
                net.Message.Clear();
                return null;
            }

            // allocate a network socket
            Socket newsock = net.LanDriver.OpenSocket( 0 );
            if( newsock == null )
            {
                net.FreeSocket( sock );
                return null;
            }

            // connect to the client
            if( net.LanDriver.Connect( newsock, clientaddr ) == -1 )
            {
                net.LanDriver.CloseSocket( newsock );
                net.FreeSocket( sock );
                return null;
            }

            // everything is allocated, just fill in the details
            sock.socket = newsock;
            sock.landriver = net.LanDriverLevel;
            sock.addr = clientaddr;
            sock.address = clientaddr.ToString();

            // send him back the info about the server connection he has been allocated
            net.Message.Clear();
            // save space for the header, filled in later
            net.Message.WriteLong( 0 );
            net.Message.WriteByte( CCRep.CCREP_ACCEPT );
            EndPoint newaddr2 = newsock.LocalEndPoint;// dfunc.GetSocketAddr(newsock, &newaddr);
            net.Message.WriteLong( net.LanDriver.GetSocketPort( newaddr2 ) );
            Common.WriteInt( net.Message.Data, 0, Common.BigLong( NetFlags.NETFLAG_CTL |
                ( net.Message.Length & NetFlags.NETFLAG_LENGTH_MASK ) ) );
            net.LanDriver.Write( acceptsock, net.Message.Data, net.Message.Length, clientaddr );
            net.Message.Clear();

            return sock;
        }

        public Int32 GetMessage( qsocket_t sock )
        {
            if( !sock.canSend )
                if( ( net.Time - sock.lastSendTime ) > 1.0 )
                    ReSendMessage( sock );

            var ret = 0;
            EndPoint readaddr = new IPEndPoint( IPAddress.Any, 0 );
            while( true )
            {
                var length = sock.Read( _PacketBuffer, net.NET_DATAGRAMSIZE, ref readaddr );
                if( length == 0 )
                    break;

                if( length == -1 )
                {
                    Con.Print( "Read error\n" );
                    return -1;
                }

                if( sock.LanDriver.AddrCompare( readaddr, sock.addr ) != 0 )
                {
#if DEBUG
                    Con.DPrint("Forged packet received\n");
                    Con.DPrint("Expected: {0}\n", StrAddr(sock.addr));
                    Con.DPrint("Received: {0}\n", StrAddr(readaddr));
#endif
                    continue;
                }

                if( length < net.NET_HEADERSIZE )
                {
                    shortPacketCount++;
                    continue;
                }

                PacketHeader header = sys.BytesToStructure<PacketHeader>( _PacketBuffer, 0 );

                length = Common.BigLong( header.length );
                var flags = length & ( ~NetFlags.NETFLAG_LENGTH_MASK );
                length &= NetFlags.NETFLAG_LENGTH_MASK;

                if( ( flags & NetFlags.NETFLAG_CTL ) != 0 )
                    continue;

                var sequence = ( UInt32 ) Common.BigLong( header.sequence );
                packetsReceived++;

                if( ( flags & NetFlags.NETFLAG_UNRELIABLE ) != 0 )
                {
                    if( sequence < sock.unreliableReceiveSequence )
                    {
                        Con.DPrint( "Got a stale datagram\n" );
                        ret = 0;
                        break;
                    }
                    if( sequence != sock.unreliableReceiveSequence )
                    {
                        var count = ( Int32 ) ( sequence - sock.unreliableReceiveSequence );
                        droppedDatagrams += count;
                        Con.DPrint( "Dropped {0} datagram(s)\n", count );
                    }
                    sock.unreliableReceiveSequence = sequence + 1;

                    length -= net.NET_HEADERSIZE;

                    net.Message.FillFrom( _PacketBuffer, PacketHeader.SizeInBytes, length );

                    ret = 2;
                    break;
                }

                if( ( flags & NetFlags.NETFLAG_ACK ) != 0 )
                {
                    if( sequence != ( sock.sendSequence - 1 ) )
                    {
                        Con.DPrint( "Stale ACK received\n" );
                        continue;
                    }
                    if( sequence == sock.ackSequence )
                    {
                        sock.ackSequence++;
                        if( sock.ackSequence != sock.sendSequence )
                            Con.DPrint( "ack sequencing error\n" );
                    }
                    else
                    {
                        Con.DPrint( "Duplicate ACK received\n" );
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
                    header.length = Common.BigLong( net.NET_HEADERSIZE | NetFlags.NETFLAG_ACK );
                    header.sequence = Common.BigLong( ( Int32 ) sequence );

                    sys.StructureToBytes( ref header, _PacketBuffer, 0 );
                    sock.Write( _PacketBuffer, net.NET_HEADERSIZE, readaddr );

                    if( sequence != sock.receiveSequence )
                    {
                        receivedDuplicateCount++;
                        continue;
                    }
                    sock.receiveSequence++;

                    length -= net.NET_HEADERSIZE;

                    if( ( flags & NetFlags.NETFLAG_EOM ) != 0 )
                    {
                        net.Message.Clear();
                        net.Message.FillFrom( sock.receiveMessage, 0, sock.receiveMessageLength );
                        net.Message.AppendFrom( _PacketBuffer, PacketHeader.SizeInBytes, length );
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
                sys.Error("Datagram_SendMessage: zero length message\n");

            if (data.Length > net.NET_MAXMESSAGE)
                sys.Error("Datagram_SendMessage: message too big {0}\n", data.Length);

            if (!sock.canSend)
                sys.Error("SendMessage: called with canSend == false\n");
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
            var packetLen = net.NET_HEADERSIZE + dataLen;

            PacketHeader header;
            header.length = Common.BigLong( packetLen | NetFlags.NETFLAG_DATA | eom );
            header.sequence = Common.BigLong( ( Int32 ) sock.sendSequence++ );
            sys.StructureToBytes( ref header, _PacketBuffer, 0 );
            Buffer.BlockCopy( data.Data, 0, _PacketBuffer, PacketHeader.SizeInBytes, dataLen );

            sock.canSend = false;

            if( sock.Write( _PacketBuffer, packetLen, sock.addr ) == -1 )
                return -1;

            sock.lastSendTime = net.Time;
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
                sys.Error("Datagram_SendUnreliableMessage: zero length message\n");

            if (data.Length > QDef.MAX_DATAGRAM)
                sys.Error("Datagram_SendUnreliableMessage: message too big {0}\n", data.Length);
#endif

            packetLen = net.NET_HEADERSIZE + data.Length;

            PacketHeader header;
            header.length = Common.BigLong( packetLen | NetFlags.NETFLAG_UNRELIABLE );
            header.sequence = Common.BigLong( ( Int32 ) sock.unreliableSendSequence++ );
            sys.StructureToBytes( ref header, _PacketBuffer, 0 );
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
            foreach( INetLanDriver driver in net.LanDrivers )
            {
                if( driver.IsInitialized )
                    driver.Shutdown();
            }

            _IsInitialized = false;
        }

        /// <summary>
        /// _Datagram_SearchForHosts
        /// </summary>
        private void InternalSearchForHosts( Boolean xmit )
        {
            EndPoint myaddr = net.LanDriver.ControlSocket.LocalEndPoint;
            if( xmit )
            {
                net.Message.Clear();
                // save space for the header, filled in later
                net.Message.WriteLong( 0 );
                net.Message.WriteByte( CCReq.CCREQ_SERVER_INFO );
                net.Message.WriteString( "QUAKE" );
                net.Message.WriteByte( net.NET_PROTOCOL_VERSION );
                Common.WriteInt( net.Message.Data, 0, Common.BigLong( NetFlags.NETFLAG_CTL |
                    ( net.Message.Length & NetFlags.NETFLAG_LENGTH_MASK ) ) );
                net.LanDriver.Broadcast( net.LanDriver.ControlSocket, net.Message.Data, net.Message.Length );
                net.Message.Clear();
            }

            EndPoint readaddr = new IPEndPoint( IPAddress.Any, 0 );
            while( true )
            {
                net.Message.FillFrom( net.LanDriver.ControlSocket, ref readaddr );
                if( net.Message.IsEmpty )
                    break;
                if( net.Message.Length < sizeof( Int32 ) )
                    continue;

                // don't answer our own query
                if( net.LanDriver.AddrCompare( readaddr, myaddr ) >= 0 )
                    continue;

                // is the cache full?
                if( net.HostCacheCount == net.HOSTCACHESIZE )
                    continue;

                net.Reader.Reset();
                var control = Common.BigLong( net.Reader.ReadLong() );// BigLong(*((int *)net_message.data));
                //MSG_ReadLong();
                if( control == -1 )
                    continue;
                if( ( control & ( ~NetFlags.NETFLAG_LENGTH_MASK ) ) != NetFlags.NETFLAG_CTL )
                    continue;
                if( ( control & NetFlags.NETFLAG_LENGTH_MASK ) != net.Message.Length )
                    continue;

                if( net.Reader.ReadByte() != CCRep.CCREP_SERVER_INFO )
                    continue;

                EndPoint _hostIP = readaddr;

                readaddr = net.LanDriver.GetAddrFromName( net.Reader.ReadString() );
                Int32 n;
                // search the cache for this server
                for( n = 0; n < net.HostCacheCount; n++ )
                    if( net.LanDriver.AddrCompare( readaddr, net.HostCache[n].addr ) == 0 )
                        break;

                // is it already there?
                if( n < net.HostCacheCount )
                    continue;

                // add it
                net.HostCacheCount++;
                hostcache_t hc = net.HostCache[n];
                hc.name = net.Reader.ReadString();
                hc.map = net.Reader.ReadString();
                hc.users = net.Reader.ReadByte();
                hc.maxusers = net.Reader.ReadByte();
                if( net.Reader.ReadByte() != net.NET_PROTOCOL_VERSION )
                {
                    hc.cname = hc.name;
                    hc.name = "*" + hc.name;
                }
                //IPEndPoint ep = (IPEndPoint)readaddr;
                //hc.addr = new IPEndPoint( ep.Address, ep.Port );
                String[] ip = readaddr.ToString().Split(':'); //readaddr.ToString()
                IPAddress _ipAddress;
                Int32 _port;
                IPAddress.TryParse(ip[0].ToString(), out _ipAddress);
                Int32.TryParse(ip[1].ToString(), out _port);
                hc.addr = new IPEndPoint(_ipAddress, _port);
                hc.driver = net.DriverLevel;
                hc.ldriver = net.LanDriverLevel;
                hc.cname = _hostIP.ToString(); //readaddr.ToString();

                // check for a name conflict
                for( var i = 0; i < net.HostCacheCount; i++ )
                {
                    if( i == n )
                        continue;
                    hostcache_t hc2 = net.HostCache[i];
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
            EndPoint sendaddr = net.LanDriver.GetAddrFromName( host );
            if( sendaddr == null )
                return null;

            Socket newsock = net.LanDriver.OpenSocket( 0 );
            if( newsock == null )
                return null;

            qsocket_t sock = net.NewSocket();
            if( sock == null )
                goto ErrorReturn2;
            sock.socket = newsock;
            sock.landriver = net.LanDriverLevel;

            // connect to the host
            if( net.LanDriver.Connect( newsock, sendaddr ) == -1 )
                goto ErrorReturn;

            // send the connection request
            Con.Print( "Connecting to " + sendaddr + "\n" );
            Con.Print( "trying...\n" );
            Scr.UpdateScreen();
            var start_time = net.Time;
            var ret = 0;
            for( var reps = 0; reps < 3; reps++ )
            {
                net.Message.Clear();
                // save space for the header, filled in later
                net.Message.WriteLong( 0 );
                net.Message.WriteByte( CCReq.CCREQ_CONNECT );
                net.Message.WriteString( "QUAKE" );
                net.Message.WriteByte( net.NET_PROTOCOL_VERSION );
                Common.WriteInt( net.Message.Data, 0, Common.BigLong( NetFlags.NETFLAG_CTL |
                    ( net.Message.Length & NetFlags.NETFLAG_LENGTH_MASK ) ) );
                //*((int *)net_message.data) = BigLong(NETFLAG_CTL | (net_message.cursize & NETFLAG_LENGTH_MASK));
                net.LanDriver.Write( newsock, net.Message.Data, net.Message.Length, sendaddr );
                net.Message.Clear();
                EndPoint readaddr = new IPEndPoint( IPAddress.Any, 0 );
                do
                {
                    ret = net.Message.FillFrom( newsock, ref readaddr );
                    // if we got something, validate it
                    if( ret > 0 )
                    {
                        // is it from the right place?
                        if( sock.LanDriver.AddrCompare( readaddr, sendaddr ) != 0 )
                        {
#if DEBUG
                            Con.Print("wrong reply address\n");
                            Con.Print("Expected: {0}\n", StrAddr(sendaddr));
                            Con.Print("Received: {0}\n", StrAddr(readaddr));
                            Scr.UpdateScreen();
#endif
                            ret = 0;
                            continue;
                        }

                        if( ret < sizeof( Int32 ) )
                        {
                            ret = 0;
                            continue;
                        }

                        net.Reader.Reset();

                        var control = Common.BigLong( net.Reader.ReadLong() );// BigLong(*((int *)net_message.data));
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
                while( ( ret == 0 ) && ( net.SetNetTime() - start_time ) < 2.5 );
                if( ret > 0 )
                    break;
                Con.Print( "still trying...\n" );
                Scr.UpdateScreen();
                start_time = net.SetNetTime();
            }

            var reason = String.Empty;
            if( ret == 0 )
            {
                reason = "No Response";
                Con.Print( "{0}\n", reason );
                menu.ReturnReason = reason;
                goto ErrorReturn;
            }

            if( ret == -1 )
            {
                reason = "Network Error";
                Con.Print( "{0}\n", reason );
                menu.ReturnReason = reason;
                goto ErrorReturn;
            }

            ret = net.Reader.ReadByte();
            if( ret == CCRep.CCREP_REJECT )
            {
                reason = net.Reader.ReadString();
                Con.Print( reason );
                menu.ReturnReason = reason;
                goto ErrorReturn;
            }

            if( ret == CCRep.CCREP_ACCEPT )
            {
                IPEndPoint ep = (IPEndPoint)sendaddr;
                sock.addr = new IPEndPoint( ep.Address, ep.Port );
                net.LanDriver.SetSocketPort( sock.addr, net.Reader.ReadLong() );
            }
            else
            {
                reason = "Bad Response";
                Con.Print( "{0}\n", reason );
                menu.ReturnReason = reason;
                goto ErrorReturn;
            }

            sock.address = net.LanDriver.GetNameFromAddr( sendaddr );

            Con.Print( "Connection accepted\n" );
            sock.lastMessageTime = net.SetNetTime();

            // switch the connection to the specified address
            if( net.LanDriver.Connect( newsock, sock.addr ) == -1 )
            {
                reason = "Connect to Game failed";
                Con.Print( "{0}\n", reason );
                menu.ReturnReason = reason;
                goto ErrorReturn;
            }

            menu.ReturnOnError = false;
            return sock;

ErrorReturn:
            net.FreeSocket( sock );
ErrorReturn2:
            net.LanDriver.CloseSocket( newsock );
            if( menu.ReturnOnError && menu.ReturnMenu != null )
            {
                menu.ReturnMenu.Show();
                menu.ReturnOnError = false;
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
            var packetLen = net.NET_HEADERSIZE + dataLen;

            PacketHeader header;
            header.length = Common.BigLong( packetLen | ( NetFlags.NETFLAG_DATA | eom ) );
            header.sequence = Common.BigLong( ( Int32 ) sock.sendSequence++ );
            sys.StructureToBytes( ref header, _PacketBuffer, 0 );
            Buffer.BlockCopy( sock.sendMessage, 0, _PacketBuffer, PacketHeader.SizeInBytes, dataLen );

            sock.sendNext = false;

            if( sock.Write( _PacketBuffer, packetLen, sock.addr ) == -1 )
                return -1;

            sock.lastSendTime = net.Time;
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
            var packetLen = net.NET_HEADERSIZE + dataLen;

            PacketHeader header;
            header.length = Common.BigLong( packetLen | ( NetFlags.NETFLAG_DATA | eom ) );
            header.sequence = Common.BigLong( ( Int32 ) ( sock.sendSequence - 1 ) );
            sys.StructureToBytes( ref header, _PacketBuffer, 0 );
            Buffer.BlockCopy( sock.sendMessage, 0, _PacketBuffer, PacketHeader.SizeInBytes, dataLen );

            sock.sendNext = false;

            if( sock.Write( _PacketBuffer, packetLen, sock.addr ) == -1 )
                return -1;

            sock.lastSendTime = net.Time;
            packetsReSent++;
            return 1;
        }

        #endregion INetDriver Members
    }
}
