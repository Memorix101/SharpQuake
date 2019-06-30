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
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using SharpQuake.Framework;

namespace SharpQuake
{
    internal delegate void PollHandler( Object arg );

    internal static class net
    {
        public static INetDriver[] Drivers
        {
            get
            {
                return _Drivers;
            }
        }

        public static INetLanDriver[] LanDrivers
        {
            get
            {
                return _LanDrivers;
            }
        }

        public static IEnumerable<qsocket_t> ActiveSockets
        {
            get
            {
                return _ActiveSockets;
            }
        }

        public static IEnumerable<qsocket_t> FreeSockets
        {
            get
            {
                return _ActiveSockets;
            }
        }

        public static Int32 MessagesSent
        {
            get
            {
                return _MessagesSent;
            }
        }

        public static Int32 MessagesReceived
        {
            get
            {
                return _MessagesReceived;
            }
        }

        public static Int32 UnreliableMessagesSent
        {
            get
            {
                return _UnreliableMessagesSent;
            }
        }

        public static Int32 UnreliableMessagesReceived
        {
            get
            {
                return _UnreliableMessagesReceived;
            }
        }

        public static String HostName
        {
            get
            {
                return _HostName.String;
            }
        }

        public static String MyTcpIpAddress
        {
            get
            {
                return _MyTcpIpAddress;
            }
            set
            {
                _MyTcpIpAddress = value;
            }
        }

        public static Int32 DefaultHostPort
        {
            get
            {
                return _DefHostPort;
            }
        }

        public static Boolean TcpIpAvailable
        {
            get
            {
                return net_tcp_ip.Instance.IsInitialized;
            }
        }

        public static hostcache_t[] HostCache
        {
            get
            {
                return _HostCache;
            }
        }

        public static Int32 DriverLevel
        {
            get
            {
                return _DriverLevel;
            }
        }

        public static INetLanDriver LanDriver
        {
            get
            {
                return _LanDrivers[LanDriverLevel];
            }
        }

        public static INetDriver Driver
        {
            get
            {
                return _Drivers[_DriverLevel];
            }
        }

        public static Boolean SlistInProgress
        {
            get
            {
                return _SlistInProgress;
            }
        }

        public static Double Time
        {
            get
            {
                return _Time;
            }
        }

        public const Int32 NET_PROTOCOL_VERSION = 3;

        public const Int32 HOSTCACHESIZE = 8;

        public const Int32 NET_NAMELEN = 64;

        public const Int32 NET_MAXMESSAGE = 8192;

        public const Int32 NET_HEADERSIZE = 2 * sizeof( UInt32 );

        public const Int32 NET_DATAGRAMSIZE = QDef.MAX_DATAGRAM + NET_HEADERSIZE;

        public static Int32 HostPort;

        public static Int32 ActiveConnections;

        public static MessageWriter Message;

        // sizebuf_t net_message
        public static MessageReader Reader;

        public static Int32 HostCacheCount;

        public static Boolean SlistSilent;

        // slistSilent
        public static Boolean SlistLocal = true;

        public static Int32 LanDriverLevel;

        private static readonly PollProcedure _SlistSendProcedure = new PollProcedure( null, 0.0, SlistSend, null );

        private static readonly PollProcedure _SlistPollProcedure = new PollProcedure( null, 0.0, SlistPoll, null );

        private static INetDriver[] _Drivers;

        // net_driver_t net_drivers[MAX_NET_DRIVERS];
        private static INetLanDriver[] _LanDrivers;

        // net_landriver_t	net_landrivers[MAX_NET_DRIVERS]
        private static Boolean _IsRecording;

        // recording
        private static Int32 _DefHostPort = 26000;

        // int	DEFAULTnet_hostport = 26000;
        // net_hostport;
        private static Boolean _IsListening;

        // static qboolean	listening = false;
        private static List<qsocket_t> _FreeSockets;

        // net_freeSockets
        private static List<qsocket_t> _ActiveSockets;

        // net_activeSockets
        // net_activeconnections
        private static Double _Time;

        private static String _MyTcpIpAddress;

        // char my_tcpip_address[NET_NAMELEN];
        private static Int32 _MessagesSent = 0;

        // reads from net_message
        private static Int32 _MessagesReceived = 0;

        // net_time
        private static Int32 _UnreliableMessagesSent = 0;

        private static Int32 _UnreliableMessagesReceived = 0;

        private static CVar _MessageTimeout;

        // = { "net_messagetimeout", "300" };
        private static CVar _HostName;

        private static PollProcedure _PollProcedureList;

        private static hostcache_t[] _HostCache = new hostcache_t[HOSTCACHESIZE];

        private static Boolean _SlistInProgress;

        // slistInProgress
        // slistLocal
        private static Int32 _SlistLastShown;

        // slistLastShown
        private static Double _SlistStartTime;

        private static Int32 _DriverLevel;

        private static VcrRecord _VcrConnect = new VcrRecord();

        // vcrConnect
        private static VcrRecord2 _VcrGetMessage = new VcrRecord2();

        // vcrGetMessage
        private static VcrRecord2 _VcrSendMessage = new VcrRecord2();

        // vcrSendMessage
        // NET_Init (void)
        public static void Init()
        {
            for( var i2 = 0; i2 < _HostCache.Length; i2++ )
                _HostCache[i2] = new hostcache_t();

            if( _Drivers == null )
            {
                if( CommandLine.HasParam( "-playback" ) )
                {
                    _Drivers = new INetDriver[]
                    {
                        new net_vcr()
                    };
                }
                else
                {
                    _Drivers = new INetDriver[]
                    {
                        new net_loop(),
                        net_datagram.Instance
                    };
                }
            }

            if( _LanDrivers == null )
            {
                _LanDrivers = new INetLanDriver[]
                {
                    net_tcp_ip.Instance
                };
            }

            if( CommandLine.HasParam( "-record" ) )
                _IsRecording = true;

            var i = CommandLine.CheckParm( "-port" );
            if( i == 0 )
                i = CommandLine.CheckParm( "-udpport" );
            if( i == 0 )
                i = CommandLine.CheckParm( "-ipxport" );

            if( i > 0 )
            {
                if( i < CommandLine.Argc - 1 )
                    _DefHostPort = MathLib.atoi( CommandLine.Argv( i + 1 ) );
                else
                    Utilities.Error( "Net.Init: you must specify a number after -port!" );
            }
            HostPort = _DefHostPort;

            if( CommandLine.HasParam( "-listen" ) || client.cls.state == cactive_t.ca_dedicated )
                _IsListening = true;
            var numsockets = server.svs.maxclientslimit;
            if( client.cls.state != cactive_t.ca_dedicated )
                numsockets++;

            _FreeSockets = new List<qsocket_t>( numsockets );
            _ActiveSockets = new List<qsocket_t>( numsockets );

            for( i = 0; i < numsockets; i++ )
                _FreeSockets.Add( new qsocket_t() );

            SetNetTime();

            // allocate space for network message buffer
            Message = new MessageWriter( NET_MAXMESSAGE ); // SZ_Alloc (&net_message, NET_MAXMESSAGE);
            Reader = new MessageReader( net.Message );

            if( _MessageTimeout == null )
            {
                _MessageTimeout = new CVar( "net_messagetimeout", "300" );
                _HostName = new CVar( "hostname", "UNNAMED" );
            }

            Command.Add( "slist", Slist_f );
            Command.Add( "listen", Listen_f );
            Command.Add( "maxplayers", MaxPlayers_f );
            Command.Add( "port", Port_f );

            // initialize all the drivers
            _DriverLevel = 0;
            foreach( INetDriver driver in _Drivers )
            {
                driver.Init();
                if( driver.IsInitialized && _IsListening )
                {
                    driver.Listen( true );
                }
                _DriverLevel++;
            }

            //if (*my_ipx_address)
            //    Con_DPrintf("IPX address %s\n", my_ipx_address);
            if( !String.IsNullOrEmpty( _MyTcpIpAddress ) )
                Con.DPrint( "TCP/IP address {0}\n", _MyTcpIpAddress );
        }

        // net_driverlevel
        // net_landriverlevel
        /// <summary>
        /// NET_Shutdown
        /// </summary>
        public static void Shutdown()
        {
            SetNetTime();

            if( _ActiveSockets != null )
            {
                qsocket_t[] tmp = _ActiveSockets.ToArray();
                foreach( qsocket_t sock in tmp )
                    Close( sock );
            }

            //
            // shutdown the drivers
            //
            if( _Drivers != null )
            {
                for( _DriverLevel = 0; _DriverLevel < _Drivers.Length; _DriverLevel++ )
                {
                    if( _Drivers[_DriverLevel].IsInitialized )
                        _Drivers[_DriverLevel].Shutdown();
                }
            }
        }

        // slistStartTime
        /// <summary>
        /// NET_CheckNewConnections
        /// </summary>
        /// <returns></returns>
        public static qsocket_t CheckNewConnections()
        {
            SetNetTime();

            for( _DriverLevel = 0; _DriverLevel < _Drivers.Length; _DriverLevel++ )
            {
                if( !_Drivers[_DriverLevel].IsInitialized )
                    continue;

                if( _DriverLevel > 0 && !_IsListening )
                    continue;

                qsocket_t ret = net.Driver.CheckNewConnections();
                if( ret != null )
                {
                    if( _IsRecording )
                    {
                        _VcrConnect.time = host.Time;
                        _VcrConnect.op = VcrOp.VCR_OP_CONNECT;
                        _VcrConnect.session = 1; // (long)ret; // Uze: todo: make it work on 64bit systems
                        Byte[] buf = Utilities.StructureToBytes( ref _VcrConnect );
                        host.VcrWriter.Write( buf, 0, buf.Length );
                        buf = Encoding.ASCII.GetBytes( ret.address );
                        var count = Math.Min( buf.Length, NET_NAMELEN );
                        var extra = NET_NAMELEN - count;
                        host.VcrWriter.Write( buf, 0, count );
                        for( var i = 0; i < extra; i++ )
                            host.VcrWriter.Write( ( Byte ) 0 );
                    }
                    return ret;
                }
            }

            if( _IsRecording )
            {
                _VcrConnect.time = host.Time;
                _VcrConnect.op = VcrOp.VCR_OP_CONNECT;
                _VcrConnect.session = 0;
                Byte[] buf = Utilities.StructureToBytes( ref _VcrConnect );
                host.VcrWriter.Write( buf, 0, buf.Length );
            }

            return null;
        }

        // hostcache
        // hostCacheCount
        /// <summary>
        /// NET_Connect
        /// called by client to connect to a host.  Returns -1 if not able to connect
        /// </summary>
        public static qsocket_t Connect( String host )
        {
            var numdrivers = _Drivers.Length;// net_numdrivers;

            SetNetTime();

            if( String.IsNullOrEmpty( host ) )
                host = null;

            if( host != null )
            {
                if( Utilities.SameText( host, "local" ) )
                {
                    numdrivers = 1;
                    goto JustDoIt;
                }

                if( HostCacheCount > 0 )
                {
                    foreach( hostcache_t hc in _HostCache )
                    {
                        if( Utilities.SameText( hc.name, host ) )
                        {
                            host = hc.cname;
                            goto JustDoIt;
                        }
                    }
                }
            }

            SlistSilent = ( host != null );
            Slist_f();

            while( _SlistInProgress )
                Poll();

            if( host == null )
            {
                if( HostCacheCount != 1 )
                    return null;
                host = _HostCache[0].cname;
                Con.Print( "Connecting to...\n{0} @ {1}\n\n", _HostCache[0].name, host );
            }

            _DriverLevel = 0;
            foreach( hostcache_t hc in _HostCache )
            {
                if( Utilities.SameText( host, hc.name ) )
                {
                    host = hc.cname;
                    break;
                }
                _DriverLevel++;
            }

JustDoIt:
            _DriverLevel = 0;
            foreach( INetDriver drv in _Drivers )
            {
                if( !drv.IsInitialized )
                    continue;
                qsocket_t ret = drv.Connect( host );
                if( ret != null )
                    return ret;
                _DriverLevel++;
            }

            if( host != null )
            {
                Con.Print( "\n" );
                PrintSlistHeader();
                PrintSlist();
                PrintSlistTrailer();
            }

            return null;
        }

        /// <summary>
        /// NET_CanSendMessage
        /// Returns true or false if the given qsocket can currently accept a
        /// message to be transmitted.
        /// </summary>
        public static Boolean CanSendMessage( qsocket_t sock )
        {
            if( sock == null )
                return false;

            if( sock.disconnected )
                return false;

            SetNetTime();

            var r = _Drivers[sock.driver].CanSendMessage( sock );

            if( _IsRecording )
            {
                _VcrSendMessage.time = host.Time;
                _VcrSendMessage.op = VcrOp.VCR_OP_CANSENDMESSAGE;
                _VcrSendMessage.session = 1; // (long)sock; Uze: todo: do something?
                _VcrSendMessage.ret = r ? 1 : 0;
                Byte[] buf = Utilities.StructureToBytes( ref _VcrSendMessage );
                host.VcrWriter.Write( buf, 0, buf.Length );
            }

            return r;
        }

        /// <summary>
        /// NET_GetMessage
        /// returns data in net_message sizebuf
        /// returns 0 if no data is waiting
        /// returns 1 if a message was received
        /// returns 2 if an unreliable message was received
        /// returns -1 if the connection died
        /// </summary>
        public static Int32 GetMessage( qsocket_t sock )
        {
            //int ret;

            if( sock == null )
                return -1;

            if( sock.disconnected )
            {
                Con.Print( "NET_GetMessage: disconnected socket\n" );
                return -1;
            }

            SetNetTime();

            var ret = _Drivers[sock.driver].GetMessage( sock );

            // see if this connection has timed out
            if( ret == 0 && sock.driver != 0 )
            {
                if( _Time - sock.lastMessageTime > _MessageTimeout.Value )
                {
                    Close( sock );
                    return -1;
                }
            }

            if( ret > 0 )
            {
                if( sock.driver != 0 )
                {
                    sock.lastMessageTime = _Time;
                    if( ret == 1 )
                        _MessagesReceived++;
                    else if( ret == 2 )
                        _UnreliableMessagesReceived++;
                }

                if( _IsRecording )
                {
                    _VcrGetMessage.time = host.Time;
                    _VcrGetMessage.op = VcrOp.VCR_OP_GETMESSAGE;
                    _VcrGetMessage.session = 1;// (long)sock; Uze todo: write somethisng meaningful
                    _VcrGetMessage.ret = ret;
                    Byte[] buf = Utilities.StructureToBytes( ref _VcrGetMessage );
                    host.VcrWriter.Write( buf, 0, buf.Length );
                    host.VcrWriter.Write( net.Message.Length );
                    host.VcrWriter.Write( net.Message.Data, 0, net.Message.Length );
                }
            }
            else
            {
                if( _IsRecording )
                {
                    _VcrGetMessage.time = host.Time;
                    _VcrGetMessage.op = VcrOp.VCR_OP_GETMESSAGE;
                    _VcrGetMessage.session = 1; // (long)sock; Uze todo: fix this
                    _VcrGetMessage.ret = ret;
                    Byte[] buf = Utilities.StructureToBytes( ref _VcrGetMessage );
                    host.VcrWriter.Write( buf, 0, buf.Length );
                }
            }

            return ret;
        }

        /// <summary>
        /// NET_SendMessage
        /// Try to send a complete length+message unit over the reliable stream.
        /// returns 0 if the message cannot be delivered reliably, but the connection
        /// is still considered valid
        /// returns 1 if the message was sent properly
        /// returns -1 if the connection died
        /// </summary>
        public static Int32 SendMessage( qsocket_t sock, MessageWriter data )
        {
            if( sock == null )
                return -1;

            if( sock.disconnected )
            {
                Con.Print( "NET_SendMessage: disconnected socket\n" );
                return -1;
            }

            SetNetTime();

            var r = _Drivers[sock.driver].SendMessage( sock, data );
            if( r == 1 && sock.driver != 0 )
                _MessagesSent++;

            if( _IsRecording )
            {
                _VcrSendMessage.time = host.Time;
                _VcrSendMessage.op = VcrOp.VCR_OP_SENDMESSAGE;
                _VcrSendMessage.session = 1; // (long)sock; Uze: todo: do something?
                _VcrSendMessage.ret = r;
                Byte[] buf = Utilities.StructureToBytes( ref _VcrSendMessage );
                host.VcrWriter.Write( buf, 0, buf.Length );
            }

            return r;
        }

        /// <summary>
        /// NET_SendUnreliableMessage
        /// returns 0 if the message connot be delivered reliably, but the connection
        ///		is still considered valid
        /// returns 1 if the message was sent properly
        /// returns -1 if the connection died
        /// </summary>
        public static Int32 SendUnreliableMessage( qsocket_t sock, MessageWriter data )
        {
            if( sock == null )
                return -1;

            if( sock.disconnected )
            {
                Con.Print( "NET_SendMessage: disconnected socket\n" );
                return -1;
            }

            SetNetTime();

            var r = _Drivers[sock.driver].SendUnreliableMessage( sock, data );
            if( r == 1 && sock.driver != 0 )
                _UnreliableMessagesSent++;

            if( _IsRecording )
            {
                _VcrSendMessage.time = host.Time;
                _VcrSendMessage.op = VcrOp.VCR_OP_SENDMESSAGE;
                _VcrSendMessage.session = 1;// (long)sock; Uze todo: ???????
                _VcrSendMessage.ret = r;
                Byte[] buf = Utilities.StructureToBytes( ref _VcrSendMessage );
                host.VcrWriter.Write( buf );
            }

            return r;
        }

        /// <summary>
        /// NET_SendToAll
        /// This is a reliable *blocking* send to all attached clients.
        /// </summary>
        public static Int32 SendToAll( MessageWriter data, Int32 blocktime )
        {
            Boolean[] state1 = new Boolean[QDef.MAX_SCOREBOARD];
            Boolean[] state2 = new Boolean[QDef.MAX_SCOREBOARD];

            var count = 0;
            for( var i = 0; i < server.svs.maxclients; i++ )
            {
                host.HostClient = server.svs.clients[i];
                if( host.HostClient.netconnection == null )
                    continue;

                if( host.HostClient.active )
                {
                    if( host.HostClient.netconnection.driver == 0 )
                    {
                        SendMessage( host.HostClient.netconnection, data );
                        state1[i] = true;
                        state2[i] = true;
                        continue;
                    }
                    count++;
                    state1[i] = false;
                    state2[i] = false;
                }
                else
                {
                    state1[i] = true;
                    state2[i] = true;
                }
            }

            var start = Timer.GetFloatTime();
            while( count > 0 )
            {
                count = 0;
                for( var i = 0; i < server.svs.maxclients; i++ )
                {
                    host.HostClient = server.svs.clients[i];
                    if( !state1[i] )
                    {
                        if( CanSendMessage( host.HostClient.netconnection ) )
                        {
                            state1[i] = true;
                            SendMessage( host.HostClient.netconnection, data );
                        }
                        else
                        {
                            GetMessage( host.HostClient.netconnection );
                        }
                        count++;
                        continue;
                    }

                    if( !state2[i] )
                    {
                        if( CanSendMessage( host.HostClient.netconnection ) )
                        {
                            state2[i] = true;
                        }
                        else
                        {
                            GetMessage( host.HostClient.netconnection );
                        }
                        count++;
                        continue;
                    }
                }
                if( ( Timer.GetFloatTime() - start ) > blocktime )
                    break;
            }
            return count;
        }

        /// <summary>
        /// NET_Close
        /// </summary>
        public static void Close( qsocket_t sock )
        {
            if( sock == null )
                return;

            if( sock.disconnected )
                return;

            SetNetTime();

            // call the driver_Close function
            _Drivers[sock.driver].Close( sock );

            FreeSocket( sock );
        }

        /// <summary>
        /// NET_FreeQSocket
        /// </summary>
        public static void FreeSocket( qsocket_t sock )
        {
            // remove it from active list
            if( !_ActiveSockets.Remove( sock ) )
                Utilities.Error( "NET_FreeQSocket: not active\n" );

            // add it to free list
            _FreeSockets.Add( sock );
            sock.disconnected = true;
        }

        /// <summary>
        /// NET_Poll
        /// </summary>
        public static void Poll()
        {
            SetNetTime();

            for( PollProcedure pp = _PollProcedureList; pp != null; pp = pp.next )
            {
                if( pp.nextTime > _Time )
                    break;

                _PollProcedureList = pp.next;
                pp.procedure( pp.arg );
            }
        }

        // double SetNetTime
        public static Double SetNetTime()
        {
            _Time = Timer.GetFloatTime();
            return _Time;
        }

        /// <summary>
        /// NET_Slist_f
        /// </summary>
        public static void Slist_f()
        {
            if( _SlistInProgress )
                return;

            if( !net.SlistSilent )
            {
                Con.Print( "Looking for Quake servers...\n" );
                PrintSlistHeader();
            }

            _SlistInProgress = true;
            _SlistStartTime = Timer.GetFloatTime();

            SchedulePollProcedure( _SlistSendProcedure, 0.0 );
            SchedulePollProcedure( _SlistPollProcedure, 0.1 );

            net.HostCacheCount = 0;
        }

        /// <summary>
        /// NET_NewQSocket
        /// Called by drivers when a new communications endpoint is required
        /// The sequence and buffer fields will be filled in properly
        /// </summary>
        public static qsocket_t NewSocket()
        {
            if( _FreeSockets.Count == 0 )
                return null;

            if( net.ActiveConnections >= server.svs.maxclients )
                return null;

            // get one from free list
            var i = _FreeSockets.Count - 1;
            qsocket_t sock = _FreeSockets[i];
            _FreeSockets.RemoveAt( i );

            // add it to active list
            _ActiveSockets.Add( sock );

            sock.disconnected = false;
            sock.connecttime = _Time;
            sock.address = "UNSET ADDRESS";
            sock.driver = _DriverLevel;
            sock.socket = null;
            sock.driverdata = null;
            sock.canSend = true;
            sock.sendNext = false;
            sock.lastMessageTime = _Time;
            sock.ackSequence = 0;
            sock.sendSequence = 0;
            sock.unreliableSendSequence = 0;
            sock.sendMessageLength = 0;
            sock.receiveSequence = 0;
            sock.unreliableReceiveSequence = 0;
            sock.receiveMessageLength = 0;

            return sock;
        }

        // pollProcedureList
        private static void PrintSlistHeader()
        {
            Con.Print( "Server          Map             Users\n" );
            Con.Print( "--------------- --------------- -----\n" );
            _SlistLastShown = 0;
        }

        // = { "hostname", "UNNAMED" };
        private static void PrintSlist()
        {
            Int32 i;
            for( i = _SlistLastShown; i < HostCacheCount; i++ )
            {
                hostcache_t hc = _HostCache[i];
                if( hc.maxusers != 0 )
                    Con.Print( "{0,-15} {1,-15}\n {2,2}/{3,2}\n", Utilities.Copy( hc.name, 15 ), Utilities.Copy( hc.map, 15 ), hc.users, hc.maxusers );
                else
                    Con.Print( "{0,-15} {1,-15}\n", Utilities.Copy( hc.name, 15 ), Utilities.Copy( hc.map, 15 ) );
            }
            _SlistLastShown = i;
        }

        private static void PrintSlistTrailer()
        {
            if( HostCacheCount != 0 )
                Con.Print( "== end list ==\n\n" );
            else
                Con.Print( "No Quake servers found.\n\n" );
        }

        /// <summary>
        /// SchedulePollProcedure
        /// </summary>
        private static void SchedulePollProcedure( PollProcedure proc, Double timeOffset )
        {
            proc.nextTime = Timer.GetFloatTime() + timeOffset;
            PollProcedure pp, prev;
            for( pp = _PollProcedureList, prev = null; pp != null; pp = pp.next )
            {
                if( pp.nextTime >= proc.nextTime )
                    break;
                prev = pp;
            }

            if( prev == null )
            {
                proc.next = _PollProcedureList;
                _PollProcedureList = proc;
                return;
            }

            proc.next = pp;
            prev.next = proc;
        }

        // NET_Listen_f
        private static void Listen_f()
        {
            if( Command.Argc != 2 )
            {
                Con.Print( "\"listen\" is \"{0}\"\n", _IsListening ? 1 : 0 );
                return;
            }

            _IsListening = ( MathLib.atoi( Command.Argv( 1 ) ) != 0 );

            foreach( INetDriver driver in _Drivers )
            {
                if( driver.IsInitialized )
                {
                    driver.Listen( _IsListening );
                }
            }
        }

        // MaxPlayers_f
        private static void MaxPlayers_f()
        {
            if( Command.Argc != 2 )
            {
                Con.Print( "\"maxplayers\" is \"%u\"\n", server.svs.maxclients );
                return;
            }

            if( server.sv.active )
            {
                Con.Print( "maxplayers can not be changed while a server is running.\n" );
                return;
            }

            var n = MathLib.atoi( Command.Argv( 1 ) );
            if( n < 1 )
                n = 1;
            if( n > server.svs.maxclientslimit )
            {
                n = server.svs.maxclientslimit;
                Con.Print( "\"maxplayers\" set to \"{0}\"\n", n );
            }

            if( n == 1 && _IsListening )
                Cbuf.AddText( "listen 0\n" );

            if( n > 1 && !_IsListening )
                Cbuf.AddText( "listen 1\n" );

            server.svs.maxclients = n;
            if( n == 1 )
                CVar.Set( "deathmatch", "0" );
            else
                CVar.Set( "deathmatch", "1" );
        }

        // NET_Port_f
        private static void Port_f()
        {
            if( Command.Argc != 2 )
            {
                Con.Print( "\"port\" is \"{0}\"\n", HostPort );
                return;
            }

            var n = MathLib.atoi( Command.Argv( 1 ) );
            if( n < 1 || n > 65534 )
            {
                Con.Print( "Bad value, must be between 1 and 65534\n" );
                return;
            }

            _DefHostPort = n;
            HostPort = n;

            if( _IsListening )
            {
                // force a change to the new port
                Cbuf.AddText( "listen 0\n" );
                Cbuf.AddText( "listen 1\n" );
            }
        }

        /// <summary>
        /// Slist_Send
        /// </summary>
        private static void SlistSend( Object arg )
        {
            for( _DriverLevel = 0; _DriverLevel < _Drivers.Length; _DriverLevel++ )
            {
                if( !net.SlistLocal && _DriverLevel == 0 )
                    continue;
                if( !_Drivers[_DriverLevel].IsInitialized )
                    continue;

                _Drivers[_DriverLevel].SearchForHosts( true );
            }

            if( ( Timer.GetFloatTime() - _SlistStartTime ) < 0.5 )
                SchedulePollProcedure( _SlistSendProcedure, 0.75 );
        }

        /// <summary>
        /// Slist_Poll
        /// </summary>
        private static void SlistPoll( Object arg )
        {
            for( _DriverLevel = 0; _DriverLevel < _Drivers.Length; _DriverLevel++ )
            {
                if( !net.SlistLocal && _DriverLevel == 0 )
                    continue;
                if( !_Drivers[_DriverLevel].IsInitialized )
                    continue;

                _Drivers[_DriverLevel].SearchForHosts( false );
            }

            if( !net.SlistSilent )
                PrintSlist();

            if( ( Timer.GetFloatTime() - _SlistStartTime ) < 1.5 )
            {
                SchedulePollProcedure( _SlistPollProcedure, 0.1 );
                return;
            }

            if( !net.SlistSilent )
                PrintSlistTrailer();

            _SlistInProgress = false;
            net.SlistSilent = false;
            net.SlistLocal = true;
        }

        [StructLayout( LayoutKind.Sequential, Pack = 1 )]
        private class VcrRecord2 : VcrRecord
        {
            public Int32 ret;
            // Uze: int len - removed
        } //vcrGetMessage;
    }

    /// <summary>
    /// NetHeader flags
    /// </summary>
    internal static class NetFlags
    {
        public const Int32 NETFLAG_LENGTH_MASK = 0x0000ffff;
        public const Int32 NETFLAG_DATA = 0x00010000;
        public const Int32 NETFLAG_ACK = 0x00020000;
        public const Int32 NETFLAG_NAK = 0x00040000;
        public const Int32 NETFLAG_EOM = 0x00080000;
        public const Int32 NETFLAG_UNRELIABLE = 0x00100000;
        public const Int32 NETFLAG_CTL = -2147483648;// 0x80000000;
    }

    internal static class CCReq
    {
        public const Int32 CCREQ_CONNECT = 0x01;
        public const Int32 CCREQ_SERVER_INFO = 0x02;
        public const Int32 CCREQ_PLAYER_INFO = 0x03;
        public const Int32 CCREQ_RULE_INFO = 0x04;
    }

    //	note:
    //		There are two address forms used above.  The short form is just a
    //		port number.  The address that goes along with the port is defined as
    //		"whatever address you receive this reponse from".  This lets us use
    //		the host OS to solve the problem of multiple host addresses (possibly
    //		with no routing between them); the host will use the right address
    //		when we reply to the inbound connection request.  The long from is
    //		a full address and port in a string.  It is used for returning the
    //		address of a server that is not running locally.
    internal static class CCRep
    {
        public const Int32 CCREP_ACCEPT = 0x81;
        public const Int32 CCREP_REJECT = 0x82;
        public const Int32 CCREP_SERVER_INFO = 0x83;
        public const Int32 CCREP_PLAYER_INFO = 0x84;
        public const Int32 CCREP_RULE_INFO = 0x85;
    }

    // qsocket_t
    internal class qsocket_t
    {
        public INetLanDriver LanDriver
        {
            get
            {
                return net.LanDrivers[this.landriver];
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

        public void ClearBuffers()
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

        public qsocket_t()
        {
            this.sendMessage = new Byte[net.NET_MAXMESSAGE];
            this.receiveMessage = new Byte[net.NET_MAXMESSAGE];
            disconnected = true;
        }
    }

    internal class PollProcedure
    {
        public PollProcedure next;
        public Double nextTime;
        public PollHandler procedure; // void (*procedure)();
        public Object arg; // void *arg

        public PollProcedure( PollProcedure next, Double nextTime, PollHandler handler, Object arg )
        {
            this.next = next;
            this.nextTime = nextTime;
            this.procedure = handler;
            this.arg = arg;
        }
    }

    internal class hostcache_t
    {
        public String name; //[16];
        public String map; //[16];
        public String cname; //[32];
        public Int32 users;
        public Int32 maxusers;
        public Int32 driver;
        public Int32 ldriver;
        public EndPoint addr; // qsockaddr ?????
    }

    // struct net_driver_t
    internal interface INetDriver
    {
        String Name
        {
            get;
        }

        Boolean IsInitialized
        {
            get;
        }

        void Init();

        void Listen( Boolean state );

        void SearchForHosts( Boolean xmit );

        qsocket_t Connect( String host );

        qsocket_t CheckNewConnections();

        Int32 GetMessage( qsocket_t sock );

        Int32 SendMessage( qsocket_t sock, MessageWriter data );

        Int32 SendUnreliableMessage( qsocket_t sock, MessageWriter data );

        Boolean CanSendMessage( qsocket_t sock );

        Boolean CanSendUnreliableMessage( qsocket_t sock );

        void Close( qsocket_t sock );

        void Shutdown();
    } //net_driver_t;

    // struct net_landriver_t
    internal interface INetLanDriver
    {
        String Name
        {
            get;
        }

        Boolean IsInitialized
        {
            get;
        }

        Socket ControlSocket
        {
            get;
        }

        Boolean Init();

        void Shutdown();

        void Listen( Boolean state );

        Socket OpenSocket( Int32 port );

        Int32 CloseSocket( Socket socket );

        Int32 Connect( Socket socket, EndPoint addr );

        Socket CheckNewConnections();

        Int32 Read( Socket socket, Byte[] buf, Int32 len, ref EndPoint ep );

        Int32 Write( Socket socket, Byte[] buf, Int32 len, EndPoint ep );

        Int32 Broadcast( Socket socket, Byte[] buf, Int32 len );

        String GetNameFromAddr( EndPoint addr );

        EndPoint GetAddrFromName( String name );

        Int32 AddrCompare( EndPoint addr1, EndPoint addr2 );

        Int32 GetSocketPort( EndPoint addr );

        Int32 SetSocketPort( EndPoint addr, Int32 port );
    } //net_landriver_t;

    // PollProcedure;

    //hostcache_t;
    // This is the network info/connection protocol.  It is used to find Quake
    // servers, get info about them, and connect to them.  Once connected, the
    // Quake game protocol (documented elsewhere) is used.
    //
    //
    // General notes:
    //	game_name is currently always "QUAKE", but is there so this same protocol
    //		can be used for future games as well; can you say Quake2?
    //
    // CCREQ_CONNECT
    //		string	game_name				"QUAKE"
    //		byte	net_protocol_version	NET_PROTOCOL_VERSION
    //
    // CCREQ_SERVER_INFO
    //		string	game_name				"QUAKE"
    //		byte	net_protocol_version	NET_PROTOCOL_VERSION
    //
    // CCREQ_PLAYER_INFO
    //		byte	player_number
    //
    // CCREQ_RULE_INFO
    //		string	rule
    //
    //
    //
    // CCREP_ACCEPT
    //		long	port
    //
    // CCREP_REJECT
    //		string	reason
    //
    // CCREP_SERVER_INFO
    //		string	server_address
    //		string	host_name
    //		string	level_name
    //		byte	current_players
    //		byte	max_players
    //		byte	protocol_version	NET_PROTOCOL_VERSION
    //
    // CCREP_PLAYER_INFO
    //		byte	player_number
    //		string	name
    //		long	colors
    //		long	frags
    //		long	connect_time
    //		string	address
    //
    // CCREP_RULE_INFO
    //		string	rule
    //		string	value
}
