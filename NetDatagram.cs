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
    internal class NetDatagram : INetDriver
    {
        [StructLayout( LayoutKind.Sequential, Pack = 1 )]
        private struct PacketHeader
        {
            public int length;
            public int sequence;

            public static int SizeInBytes = Marshal.SizeOf(typeof(PacketHeader));
        }

        public static NetDatagram Instance
        {
            get
            {
                return _Singletone;
            }
        }

        private static NetDatagram _Singletone = new NetDatagram();

        private int _DriverLevel;
        private bool _IsInitialized;
        private byte[] _PacketBuffer;

        // statistic counters
        private int packetsSent;

        private int packetsReSent;
        private int packetsReceived;
        private int receivedDuplicateCount;
        private int shortPacketCount;
        private int droppedDatagrams;
        //

        private static string StrAddr( EndPoint ep )
        {
            return ep.ToString();
        }

        // NET_Stats_f
        private void Stats_f()
        {
            if( Cmd.Argc == 1 )
            {
                Con.Print( "unreliable messages sent   = %i\n", Net.UnreliableMessagesSent );
                Con.Print( "unreliable messages recv   = %i\n", Net.UnreliableMessagesReceived );
                Con.Print( "reliable messages sent     = %i\n", Net.MessagesSent );
                Con.Print( "reliable messages received = %i\n", Net.MessagesReceived );
                Con.Print( "packetsSent                = %i\n", packetsSent );
                Con.Print( "packetsReSent              = %i\n", packetsReSent );
                Con.Print( "packetsReceived            = %i\n", packetsReceived );
                Con.Print( "receivedDuplicateCount     = %i\n", receivedDuplicateCount );
                Con.Print( "shortPacketCount           = %i\n", shortPacketCount );
                Con.Print( "droppedDatagrams           = %i\n", droppedDatagrams );
            }
            else if( Cmd.Argv( 1 ) == "*" )
            {
                foreach( qsocket_t s in Net.ActiveSockets )
                    PrintStats( s );

                foreach( qsocket_t s in Net.FreeSockets )
                    PrintStats( s );
            }
            else
            {
                qsocket_t sock = null;
                string cmdAddr = Cmd.Argv( 1 );

                foreach( qsocket_t s in Net.ActiveSockets )
                    if( Common.SameText( s.address, cmdAddr ) )
                    {
                        sock = s;
                        break;
                    }

                if( sock == null )
                    foreach( qsocket_t s in Net.FreeSockets )
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

        private NetDatagram()
        {
            _PacketBuffer = new byte[Net.NET_DATAGRAMSIZE];
        }

        #region INetDriver Members

        public string Name
        {
            get
            {
                return "Datagram";
            }
        }

        public bool IsInitialized
        {
            get
            {
                return _IsInitialized;
            }
        }

        public void Init()
        {
            _DriverLevel = Array.IndexOf( Net.Drivers, this );
            Cmd.Add( "net_stats", Stats_f );

            if( Common.HasParam( "-nolan" ) )
                return;

            foreach( INetLanDriver driver in Net.LanDrivers )
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
        public void Listen( bool state )
        {
            foreach( INetLanDriver drv in Net.LanDrivers )
            {
                if( drv.IsInitialized )
                    drv.Listen( state );
            }
        }

        /// <summary>
        /// Datagram_SearchForHosts
        /// </summary>
        public void SearchForHosts( bool xmit )
        {
            for( Net.LanDriverLevel = 0; Net.LanDriverLevel < Net.LanDrivers.Length; Net.LanDriverLevel++ )
            {
                if( Net.HostCacheCount == Net.HOSTCACHESIZE )
                    break;
                if( Net.LanDrivers[Net.LanDriverLevel].IsInitialized )
                    InternalSearchForHosts( xmit );
            }
        }

        /// <summary>
        /// Datagram_Connect
        /// </summary>
        public qsocket_t Connect( string host )
        {
            qsocket_t ret = null;

            for( Net.LanDriverLevel = 0; Net.LanDriverLevel < Net.LanDrivers.Length; Net.LanDriverLevel++ )
                if( Net.LanDrivers[Net.LanDriverLevel].IsInitialized )
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

            for( Net.LanDriverLevel = 0; Net.LanDriverLevel < Net.LanDrivers.Length; Net.LanDriverLevel++ )
                if( Net.LanDriver.IsInitialized )
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
            Socket acceptsock = Net.LanDriver.CheckNewConnections();
            if( acceptsock == null )
                return null;

            EndPoint clientaddr = new IPEndPoint( IPAddress.Any, 0 );
            Net.Message.FillFrom( acceptsock, ref clientaddr );

            if( Net.Message.Length < sizeof( int ) )
                return null;

            Net.Reader.Reset();
            int control = Common.BigLong( Net.Reader.ReadLong() );
            if( control == -1 )
                return null;
            if( ( control & ( ~NetFlags.NETFLAG_LENGTH_MASK ) ) != NetFlags.NETFLAG_CTL )
                return null;
            if( ( control & NetFlags.NETFLAG_LENGTH_MASK ) != Net.Message.Length )
                return null;

            int command = Net.Reader.ReadByte();
            if( command == CCReq.CCREQ_SERVER_INFO )
            {
                string tmp = Net.Reader.ReadString();
                if( tmp != "QUAKE" )
                    return null;

                Net.Message.Clear();

                // save space for the header, filled in later
                Net.Message.WriteLong( 0 );
                Net.Message.WriteByte( CCRep.CCREP_SERVER_INFO );
                EndPoint newaddr = acceptsock.LocalEndPoint; //dfunc.GetSocketAddr(acceptsock, &newaddr);
                Net.Message.WriteString( newaddr.ToString() ); // dfunc.AddrToString(&newaddr));
                Net.Message.WriteString( Net.HostName );
                Net.Message.WriteString( Server.sv.name );
                Net.Message.WriteByte( Net.ActiveConnections );
                Net.Message.WriteByte( Server.svs.maxclients );
                Net.Message.WriteByte( Net.NET_PROTOCOL_VERSION );
                Common.WriteInt( Net.Message.Data, 0, Common.BigLong( NetFlags.NETFLAG_CTL |
                    ( Net.Message.Length & NetFlags.NETFLAG_LENGTH_MASK ) ) );
                Net.LanDriver.Write( acceptsock, Net.Message.Data, Net.Message.Length, clientaddr );
                Net.Message.Clear();
                return null;
            }

            if( command == CCReq.CCREQ_PLAYER_INFO )
            {
                int playerNumber = Net.Reader.ReadByte();
                int clientNumber, activeNumber = -1;
                client_t client = null;
                for( clientNumber = 0; clientNumber < Server.svs.maxclients; clientNumber++ )
                {
                    client = Server.svs.clients[clientNumber];
                    if( client.active )
                    {
                        activeNumber++;
                        if( activeNumber == playerNumber )
                            break;
                    }
                }
                if( clientNumber == Server.svs.maxclients )
                    return null;

                Net.Message.Clear();
                // save space for the header, filled in later
                Net.Message.WriteLong( 0 );
                Net.Message.WriteByte( CCRep.CCREP_PLAYER_INFO );
                Net.Message.WriteByte( playerNumber );
                Net.Message.WriteString( client.name );
                Net.Message.WriteLong( client.colors );
                Net.Message.WriteLong( (int)client.edict.v.frags );
                Net.Message.WriteLong( (int)( Net.Time - client.netconnection.connecttime ) );
                Net.Message.WriteString( client.netconnection.address );
                Common.WriteInt( Net.Message.Data, 0, Common.BigLong( NetFlags.NETFLAG_CTL |
                    ( Net.Message.Length & NetFlags.NETFLAG_LENGTH_MASK ) ) );
                Net.LanDriver.Write( acceptsock, Net.Message.Data, Net.Message.Length, clientaddr );
                Net.Message.Clear();

                return null;
            }

            if( command == CCReq.CCREQ_RULE_INFO )
            {
                // find the search start location
                string prevCvarName = Net.Reader.ReadString();
                Cvar var;
                if( !String.IsNullOrEmpty( prevCvarName ) )
                {
                    var = Cvar.Find( prevCvarName );
                    if( var == null )
                        return null;
                    var = var.Next;
                }
                else
                    var = Cvar.First;

                // search for the next server cvar
                while( var != null )
                {
                    if( var.IsServer )
                        break;
                    var = var.Next;
                }

                // send the response
                Net.Message.Clear();

                // save space for the header, filled in later
                Net.Message.WriteLong( 0 );
                Net.Message.WriteByte( CCRep.CCREP_RULE_INFO );
                if( var != null )
                {
                    Net.Message.WriteString( var.Name );
                    Net.Message.WriteString( var.String );
                }
                Common.WriteInt( Net.Message.Data, 0, Common.BigLong( NetFlags.NETFLAG_CTL |
                    ( Net.Message.Length & NetFlags.NETFLAG_LENGTH_MASK ) ) );
                Net.LanDriver.Write( acceptsock, Net.Message.Data, Net.Message.Length, clientaddr );
                Net.Message.Clear();

                return null;
            }

            if( command != CCReq.CCREQ_CONNECT )
                return null;

            if( Net.Reader.ReadString() != "QUAKE" )
                return null;

            if( Net.Reader.ReadByte() != Net.NET_PROTOCOL_VERSION )
            {
                Net.Message.Clear();
                // save space for the header, filled in later
                Net.Message.WriteLong( 0 );
                Net.Message.WriteByte( CCRep.CCREP_REJECT );
                Net.Message.WriteString( "Incompatible version.\n" );
                Common.WriteInt( Net.Message.Data, 0, Common.BigLong( NetFlags.NETFLAG_CTL |
                    ( Net.Message.Length & NetFlags.NETFLAG_LENGTH_MASK ) ) );
                Net.LanDriver.Write( acceptsock, Net.Message.Data, Net.Message.Length, clientaddr );
                Net.Message.Clear();
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
            foreach( qsocket_t s in Net.ActiveSockets )
            {
                if( s.driver != Net.DriverLevel )
                    continue;

                int ret = Net.LanDriver.AddrCompare( clientaddr, s.addr );
                if( ret >= 0 )
                {
                    // is this a duplicate connection reqeust?
                    if( ret == 0 && Net.Time - s.connecttime < 2.0 )
                    {
                        // yes, so send a duplicate reply
                        Net.Message.Clear();
                        // save space for the header, filled in later
                        Net.Message.WriteLong( 0 );
                        Net.Message.WriteByte( CCRep.CCREP_ACCEPT );
                        EndPoint newaddr = s.socket.LocalEndPoint; //dfunc.GetSocketAddr(s.socket, &newaddr);
                        Net.Message.WriteLong( Net.LanDriver.GetSocketPort( newaddr ) );
                        Common.WriteInt( Net.Message.Data, 0, Common.BigLong( NetFlags.NETFLAG_CTL |
                            ( Net.Message.Length & NetFlags.NETFLAG_LENGTH_MASK ) ) );
                        Net.LanDriver.Write( acceptsock, Net.Message.Data, Net.Message.Length, clientaddr );
                        Net.Message.Clear();
                        return null;
                    }
                    // it's somebody coming back in from a crash/disconnect
                    // so close the old qsocket and let their retry get them back in
                    Net.Close( s );
                    return null;
                }
            }

            // allocate a QSocket
            qsocket_t sock = Net.NewSocket();
            if( sock == null )
            {
                // no room; try to let him know
                Net.Message.Clear();
                // save space for the header, filled in later
                Net.Message.WriteLong( 0 );
                Net.Message.WriteByte( CCRep.CCREP_REJECT );
                Net.Message.WriteString( "Server is full.\n" );
                Common.WriteInt( Net.Message.Data, 0, Common.BigLong( NetFlags.NETFLAG_CTL |
                    ( Net.Message.Length & NetFlags.NETFLAG_LENGTH_MASK ) ) );
                Net.LanDriver.Write( acceptsock, Net.Message.Data, Net.Message.Length, clientaddr );
                Net.Message.Clear();
                return null;
            }

            // allocate a network socket
            Socket newsock = Net.LanDriver.OpenSocket( 0 );
            if( newsock == null )
            {
                Net.FreeSocket( sock );
                return null;
            }

            // connect to the client
            if( Net.LanDriver.Connect( newsock, clientaddr ) == -1 )
            {
                Net.LanDriver.CloseSocket( newsock );
                Net.FreeSocket( sock );
                return null;
            }

            // everything is allocated, just fill in the details
            sock.socket = newsock;
            sock.landriver = Net.LanDriverLevel;
            sock.addr = clientaddr;
            sock.address = clientaddr.ToString();

            // send him back the info about the server connection he has been allocated
            Net.Message.Clear();
            // save space for the header, filled in later
            Net.Message.WriteLong( 0 );
            Net.Message.WriteByte( CCRep.CCREP_ACCEPT );
            EndPoint newaddr2 = newsock.LocalEndPoint;// dfunc.GetSocketAddr(newsock, &newaddr);
            Net.Message.WriteLong( Net.LanDriver.GetSocketPort( newaddr2 ) );
            Common.WriteInt( Net.Message.Data, 0, Common.BigLong( NetFlags.NETFLAG_CTL |
                ( Net.Message.Length & NetFlags.NETFLAG_LENGTH_MASK ) ) );
            Net.LanDriver.Write( acceptsock, Net.Message.Data, Net.Message.Length, clientaddr );
            Net.Message.Clear();

            return sock;
        }

        public int GetMessage( qsocket_t sock )
        {
            if( !sock.canSend )
                if( ( Net.Time - sock.lastSendTime ) > 1.0 )
                    ReSendMessage( sock );

            int ret = 0;
            EndPoint readaddr = new IPEndPoint( IPAddress.Any, 0 );
            while( true )
            {
                int length = sock.Read( _PacketBuffer, Net.NET_DATAGRAMSIZE, ref readaddr );
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

                if( length < Net.NET_HEADERSIZE )
                {
                    shortPacketCount++;
                    continue;
                }

                PacketHeader header = Sys.BytesToStructure<PacketHeader>( _PacketBuffer, 0 );

                length = Common.BigLong( header.length );
                int flags = length & ( ~NetFlags.NETFLAG_LENGTH_MASK );
                length &= NetFlags.NETFLAG_LENGTH_MASK;

                if( ( flags & NetFlags.NETFLAG_CTL ) != 0 )
                    continue;

                uint sequence = (uint)Common.BigLong( header.sequence );
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
                        int count = (int)( sequence - sock.unreliableReceiveSequence );
                        droppedDatagrams += count;
                        Con.DPrint( "Dropped {0} datagram(s)\n", count );
                    }
                    sock.unreliableReceiveSequence = sequence + 1;

                    length -= Net.NET_HEADERSIZE;

                    Net.Message.FillFrom( _PacketBuffer, PacketHeader.SizeInBytes, length );

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
                    header.length = Common.BigLong( Net.NET_HEADERSIZE | NetFlags.NETFLAG_ACK );
                    header.sequence = Common.BigLong( (int)sequence );

                    Sys.StructureToBytes( ref header, _PacketBuffer, 0 );
                    sock.Write( _PacketBuffer, Net.NET_HEADERSIZE, readaddr );

                    if( sequence != sock.receiveSequence )
                    {
                        receivedDuplicateCount++;
                        continue;
                    }
                    sock.receiveSequence++;

                    length -= Net.NET_HEADERSIZE;

                    if( ( flags & NetFlags.NETFLAG_EOM ) != 0 )
                    {
                        Net.Message.Clear();
                        Net.Message.FillFrom( sock.receiveMessage, 0, sock.receiveMessageLength );
                        Net.Message.AppendFrom( _PacketBuffer, PacketHeader.SizeInBytes, length );
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
        public int SendMessage( qsocket_t sock, MsgWriter data )
        {
#if DEBUG
            if (data.IsEmpty)
                Sys.Error("Datagram_SendMessage: zero length message\n");

            if (data.Length > Net.NET_MAXMESSAGE)
                Sys.Error("Datagram_SendMessage: message too big {0}\n", data.Length);

            if (!sock.canSend)
                Sys.Error("SendMessage: called with canSend == false\n");
#endif
            Buffer.BlockCopy( data.Data, 0, sock.sendMessage, 0, data.Length );
            sock.sendMessageLength = data.Length;

            int dataLen, eom;
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
            int packetLen = Net.NET_HEADERSIZE + dataLen;

            PacketHeader header;
            header.length = Common.BigLong( packetLen | NetFlags.NETFLAG_DATA | eom );
            header.sequence = Common.BigLong( (int)sock.sendSequence++ );
            Sys.StructureToBytes( ref header, _PacketBuffer, 0 );
            Buffer.BlockCopy( data.Data, 0, _PacketBuffer, PacketHeader.SizeInBytes, dataLen );

            sock.canSend = false;

            if( sock.Write( _PacketBuffer, packetLen, sock.addr ) == -1 )
                return -1;

            sock.lastSendTime = Net.Time;
            packetsSent++;
            return 1;
        }

        /// <summary>
        /// Datagram_SendUnreliableMessage
        /// </summary>
        public int SendUnreliableMessage( qsocket_t sock, MsgWriter data )
        {
            int packetLen;

#if DEBUG
            if (data.IsEmpty)
                Sys.Error("Datagram_SendUnreliableMessage: zero length message\n");

            if (data.Length > QDef.MAX_DATAGRAM)
                Sys.Error("Datagram_SendUnreliableMessage: message too big {0}\n", data.Length);
#endif

            packetLen = Net.NET_HEADERSIZE + data.Length;

            PacketHeader header;
            header.length = Common.BigLong( packetLen | NetFlags.NETFLAG_UNRELIABLE );
            header.sequence = Common.BigLong( (int)sock.unreliableSendSequence++ );
            Sys.StructureToBytes( ref header, _PacketBuffer, 0 );
            Buffer.BlockCopy( data.Data, 0, _PacketBuffer, PacketHeader.SizeInBytes, data.Length );

            if( sock.Write( _PacketBuffer, packetLen, sock.addr ) == -1 )
                return -1;

            packetsSent++;
            return 1;
        }

        /// <summary>
        /// Datagram_CanSendMessage
        /// </summary>
        public bool CanSendMessage( qsocket_t sock )
        {
            if( sock.sendNext )
                SendMessageNext( sock );

            return sock.canSend;
        }

        /// <summary>
        /// Datagram_CanSendUnreliableMessage
        /// </summary>
        public bool CanSendUnreliableMessage( qsocket_t sock )
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
            foreach( INetLanDriver driver in Net.LanDrivers )
            {
                if( driver.IsInitialized )
                    driver.Shutdown();
            }

            _IsInitialized = false;
        }

        /// <summary>
        /// _Datagram_SearchForHosts
        /// </summary>
        private void InternalSearchForHosts( bool xmit )
        {
            EndPoint myaddr = Net.LanDriver.ControlSocket.LocalEndPoint;
            if( xmit )
            {
                Net.Message.Clear();
                // save space for the header, filled in later
                Net.Message.WriteLong( 0 );
                Net.Message.WriteByte( CCReq.CCREQ_SERVER_INFO );
                Net.Message.WriteString( "QUAKE" );
                Net.Message.WriteByte( Net.NET_PROTOCOL_VERSION );
                Common.WriteInt( Net.Message.Data, 0, Common.BigLong( NetFlags.NETFLAG_CTL |
                    ( Net.Message.Length & NetFlags.NETFLAG_LENGTH_MASK ) ) );
                Net.LanDriver.Broadcast( Net.LanDriver.ControlSocket, Net.Message.Data, Net.Message.Length );
                Net.Message.Clear();
            }

            EndPoint readaddr = new IPEndPoint( IPAddress.Any, 0 );
            while( true )
            {
                Net.Message.FillFrom( Net.LanDriver.ControlSocket, ref readaddr );
                if( Net.Message.IsEmpty )
                    break;
                if( Net.Message.Length < sizeof( int ) )
                    continue;

                // don't answer our own query
                if( Net.LanDriver.AddrCompare( readaddr, myaddr ) >= 0 )
                    continue;

                // is the cache full?
                if( Net.HostCacheCount == Net.HOSTCACHESIZE )
                    continue;

                Net.Reader.Reset();
                int control = Common.BigLong( Net.Reader.ReadLong() );// BigLong(*((int *)net_message.data));
                //MSG_ReadLong();
                if( control == -1 )
                    continue;
                if( ( control & ( ~NetFlags.NETFLAG_LENGTH_MASK ) ) != NetFlags.NETFLAG_CTL )
                    continue;
                if( ( control & NetFlags.NETFLAG_LENGTH_MASK ) != Net.Message.Length )
                    continue;

                if( Net.Reader.ReadByte() != CCRep.CCREP_SERVER_INFO )
                    continue;

                readaddr = Net.LanDriver.GetAddrFromName( Net.Reader.ReadString() );
                int n;
                // search the cache for this server
                for( n = 0; n < Net.HostCacheCount; n++ )
                    if( Net.LanDriver.AddrCompare( readaddr, Net.HostCache[n].addr ) == 0 )
                        break;

                // is it already there?
                if( n < Net.HostCacheCount )
                    continue;

                // add it
                Net.HostCacheCount++;
                hostcache_t hc = Net.HostCache[n];
                hc.name = Net.Reader.ReadString();
                hc.map = Net.Reader.ReadString();
                hc.users = Net.Reader.ReadByte();
                hc.maxusers = Net.Reader.ReadByte();
                if( Net.Reader.ReadByte() != Net.NET_PROTOCOL_VERSION )
                {
                    hc.cname = hc.name;
                    hc.name = "*" + hc.name;
                }
                IPEndPoint ep = (IPEndPoint)readaddr;
                hc.addr = new IPEndPoint( ep.Address, ep.Port );
                hc.driver = Net.DriverLevel;
                hc.ldriver = Net.LanDriverLevel;
                hc.cname = readaddr.ToString();

                // check for a name conflict
                for( int i = 0; i < Net.HostCacheCount; i++ )
                {
                    if( i == n )
                        continue;
                    hostcache_t hc2 = Net.HostCache[i];
                    if( hc.name == hc2.name )
                    {
                        i = hc.name.Length;
                        if( i < 15 && hc.name[i - 1] > '8' )
                        {
                            hc.name = hc.name.Substring( 0, i ) + '0';
                        }
                        else
                            hc.name = hc.name.Substring( 0, i - 1 ) + (char)( hc.name[i - 1] + 1 );
                        i = 0;// -1;
                    }
                }
            }
        }

        /// <summary>
        /// _Datagram_Connect
        /// </summary>
        private qsocket_t InternalConnect( string host )
        {
            // see if we can resolve the host name
            EndPoint sendaddr = Net.LanDriver.GetAddrFromName( host );
            if( sendaddr == null )
                return null;

            Socket newsock = Net.LanDriver.OpenSocket( 0 );
            if( newsock == null )
                return null;

            qsocket_t sock = Net.NewSocket();
            if( sock == null )
                goto ErrorReturn2;
            sock.socket = newsock;
            sock.landriver = Net.LanDriverLevel;

            // connect to the host
            if( Net.LanDriver.Connect( newsock, sendaddr ) == -1 )
                goto ErrorReturn;

            // send the connection request
            Con.Print( "Connecting to " + sendaddr + "\n" );
            Con.Print( "trying...\n" );
            Scr.UpdateScreen();
            double start_time = Net.Time;
            int ret = 0;
            for( int reps = 0; reps < 3; reps++ )
            {
                Net.Message.Clear();
                // save space for the header, filled in later
                Net.Message.WriteLong( 0 );
                Net.Message.WriteByte( CCReq.CCREQ_CONNECT );
                Net.Message.WriteString( "QUAKE" );
                Net.Message.WriteByte( Net.NET_PROTOCOL_VERSION );
                Common.WriteInt( Net.Message.Data, 0, Common.BigLong( NetFlags.NETFLAG_CTL |
                    ( Net.Message.Length & NetFlags.NETFLAG_LENGTH_MASK ) ) );
                //*((int *)net_message.data) = BigLong(NETFLAG_CTL | (net_message.cursize & NETFLAG_LENGTH_MASK));
                Net.LanDriver.Write( newsock, Net.Message.Data, Net.Message.Length, sendaddr );
                Net.Message.Clear();
                EndPoint readaddr = new IPEndPoint( IPAddress.Any, 0 );
                do
                {
                    ret = Net.Message.FillFrom( newsock, ref readaddr );
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

                        if( ret < sizeof( int ) )
                        {
                            ret = 0;
                            continue;
                        }

                        Net.Reader.Reset();

                        int control = Common.BigLong( Net.Reader.ReadLong() );// BigLong(*((int *)net_message.data));
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
                while( ( ret == 0 ) && ( Net.SetNetTime() - start_time ) < 2.5 );
                if( ret > 0 )
                    break;
                Con.Print( "still trying...\n" );
                Scr.UpdateScreen();
                start_time = Net.SetNetTime();
            }

            string reason = String.Empty;
            if( ret == 0 )
            {
                reason = "No Response";
                Con.Print( "{0}\n", reason );
                Menu.ReturnReason = reason;
                goto ErrorReturn;
            }

            if( ret == -1 )
            {
                reason = "Network Error";
                Con.Print( "{0}\n", reason );
                Menu.ReturnReason = reason;
                goto ErrorReturn;
            }

            ret = Net.Reader.ReadByte();
            if( ret == CCRep.CCREP_REJECT )
            {
                reason = Net.Reader.ReadString();
                Con.Print( reason );
                Menu.ReturnReason = reason;
                goto ErrorReturn;
            }

            if( ret == CCRep.CCREP_ACCEPT )
            {
                IPEndPoint ep = (IPEndPoint)sendaddr;
                sock.addr = new IPEndPoint( ep.Address, ep.Port );
                Net.LanDriver.SetSocketPort( sock.addr, Net.Reader.ReadLong() );
            }
            else
            {
                reason = "Bad Response";
                Con.Print( "{0}\n", reason );
                Menu.ReturnReason = reason;
                goto ErrorReturn;
            }

            sock.address = Net.LanDriver.GetNameFromAddr( sendaddr );

            Con.Print( "Connection accepted\n" );
            sock.lastMessageTime = Net.SetNetTime();

            // switch the connection to the specified address
            if( Net.LanDriver.Connect( newsock, sock.addr ) == -1 )
            {
                reason = "Connect to Game failed";
                Con.Print( "{0}\n", reason );
                Menu.ReturnReason = reason;
                goto ErrorReturn;
            }

            Menu.ReturnOnError = false;
            return sock;

ErrorReturn:
            Net.FreeSocket( sock );
ErrorReturn2:
            Net.LanDriver.CloseSocket( newsock );
            if( Menu.ReturnOnError && Menu.ReturnMenu != null )
            {
                Menu.ReturnMenu.Show();
                Menu.ReturnOnError = false;
            }
            return null;
        }

        /// <summary>
        /// SendMessageNext
        /// </summary>
        private int SendMessageNext( qsocket_t sock )
        {
            int dataLen;
            int eom;
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
            int packetLen = Net.NET_HEADERSIZE + dataLen;

            PacketHeader header;
            header.length = Common.BigLong( packetLen | ( NetFlags.NETFLAG_DATA | eom ) );
            header.sequence = Common.BigLong( (int)sock.sendSequence++ );
            Sys.StructureToBytes( ref header, _PacketBuffer, 0 );
            Buffer.BlockCopy( sock.sendMessage, 0, _PacketBuffer, PacketHeader.SizeInBytes, dataLen );

            sock.sendNext = false;

            if( sock.Write( _PacketBuffer, packetLen, sock.addr ) == -1 )
                return -1;

            sock.lastSendTime = Net.Time;
            packetsSent++;
            return 1;
        }

        /// <summary>
        /// ReSendMessage
        /// </summary>
        private int ReSendMessage( qsocket_t sock )
        {
            int dataLen, eom;
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
            int packetLen = Net.NET_HEADERSIZE + dataLen;

            PacketHeader header;
            header.length = Common.BigLong( packetLen | ( NetFlags.NETFLAG_DATA | eom ) );
            header.sequence = Common.BigLong( (int)( sock.sendSequence - 1 ) );
            Sys.StructureToBytes( ref header, _PacketBuffer, 0 );
            Buffer.BlockCopy( sock.sendMessage, 0, _PacketBuffer, PacketHeader.SizeInBytes, dataLen );

            sock.sendNext = false;

            if( sock.Write( _PacketBuffer, packetLen, sock.addr ) == -1 )
                return -1;

            sock.lastSendTime = Net.Time;
            packetsReSent++;
            return 1;
        }

        #endregion INetDriver Members
    }
}
