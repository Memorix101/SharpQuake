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

namespace SharpQuake
{
    internal delegate void PollHandler( object arg );

    internal static class Net
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

        public static int MessagesSent
        {
            get
            {
                return _MessagesSent;
            }
        }

        public static int MessagesReceived
        {
            get
            {
                return _MessagesReceived;
            }
        }

        public static int UnreliableMessagesSent
        {
            get
            {
                return _UnreliableMessagesSent;
            }
        }

        public static int UnreliableMessagesReceived
        {
            get
            {
                return _UnreliableMessagesReceived;
            }
        }

        public static string HostName
        {
            get
            {
                return _HostName.String;
            }
        }

        public static string MyTcpIpAddress
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

        public static int DefaultHostPort
        {
            get
            {
                return _DefHostPort;
            }
        }

        public static bool TcpIpAvailable
        {
            get
            {
                return NetTcpIp.Instance.IsInitialized;
            }
        }

        public static hostcache_t[] HostCache
        {
            get
            {
                return _HostCache;
            }
        }

        public static int DriverLevel
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

        public static bool SlistInProgress
        {
            get
            {
                return _SlistInProgress;
            }
        }

        public static double Time
        {
            get
            {
                return _Time;
            }
        }

        public const int NET_PROTOCOL_VERSION = 3;

        public const int HOSTCACHESIZE = 8;

        public const int NET_NAMELEN = 64;

        public const int NET_MAXMESSAGE = 8192;

        public const int NET_HEADERSIZE = 2 * sizeof(uint);

        public const int NET_DATAGRAMSIZE = QDef.MAX_DATAGRAM + NET_HEADERSIZE;

        public static int HostPort;

        public static int ActiveConnections;

        public static MsgWriter Message;

        // sizebuf_t net_message
        public static MsgReader Reader;

        public static int HostCacheCount;

        public static bool SlistSilent;

        // slistSilent
        public static bool SlistLocal = true;

        public static int LanDriverLevel;

        private static readonly PollProcedure _SlistSendProcedure = new PollProcedure( null, 0.0, SlistSend, null );

        private static readonly PollProcedure _SlistPollProcedure = new PollProcedure( null, 0.0, SlistPoll, null );

        private static INetDriver[] _Drivers;

        // net_driver_t net_drivers[MAX_NET_DRIVERS];
        private static INetLanDriver[] _LanDrivers;

        // net_landriver_t	net_landrivers[MAX_NET_DRIVERS]
        private static bool _IsRecording;

        // recording
        private static int _DefHostPort = 26000;

        // int	DEFAULTnet_hostport = 26000;
        // net_hostport;
        private static bool _IsListening;

        // static qboolean	listening = false;
        private static List<qsocket_t> _FreeSockets;

        // net_freeSockets
        private static List<qsocket_t> _ActiveSockets;

        // net_activeSockets
        // net_activeconnections
        private static double _Time;

        private static string _MyTcpIpAddress;

        // char my_tcpip_address[NET_NAMELEN];
        private static int _MessagesSent = 0;

        // reads from net_message
        private static int _MessagesReceived = 0;

        // net_time
        private static int _UnreliableMessagesSent = 0;

        private static int _UnreliableMessagesReceived = 0;

        private static Cvar _MessageTimeout;

        // = { "net_messagetimeout", "300" };
        private static Cvar _HostName;

        private static PollProcedure _PollProcedureList;

        private static hostcache_t[] _HostCache = new hostcache_t[HOSTCACHESIZE];

        private static bool _SlistInProgress;

        // slistInProgress
        // slistLocal
        private static int _SlistLastShown;

        // slistLastShown
        private static double _SlistStartTime;

        private static int _DriverLevel;

        private static VcrRecord _VcrConnect = new VcrRecord();

        // vcrConnect
        private static VcrRecord2 _VcrGetMessage = new VcrRecord2();

        // vcrGetMessage
        private static VcrRecord2 _VcrSendMessage = new VcrRecord2();

        // vcrSendMessage
        // NET_Init (void)
        public static void Init()
        {
            for( int i2 = 0; i2 < _HostCache.Length; i2++ )
                _HostCache[i2] = new hostcache_t();

            if( _Drivers == null )
            {
                if( Common.HasParam( "-playback" ) )
                {
                    _Drivers = new INetDriver[]
                    {
                        new NetVcr()
                    };
                }
                else
                {
                    _Drivers = new INetDriver[]
                    {
                        new NetLoop(),
                        NetDatagram.Instance
                    };
                }
            }

            if( _LanDrivers == null )
            {
                _LanDrivers = new INetLanDriver[]
                {
                    NetTcpIp.Instance
                };
            }

            if( Common.HasParam( "-record" ) )
                _IsRecording = true;

            int i = Common.CheckParm( "-port" );
            if( i == 0 )
                i = Common.CheckParm( "-udpport" );
            if( i == 0 )
                i = Common.CheckParm( "-ipxport" );

            if( i > 0 )
            {
                if( i < Common.Argc - 1 )
                    _DefHostPort = Common.atoi( Common.Argv( i + 1 ) );
                else
                    Sys.Error( "Net.Init: you must specify a number after -port!" );
            }
            HostPort = _DefHostPort;

            if( Common.HasParam( "-listen" ) || Client.cls.state == cactive_t.ca_dedicated )
                _IsListening = true;
            int numsockets = Server.svs.maxclientslimit;
            if( Client.cls.state != cactive_t.ca_dedicated )
                numsockets++;

            _FreeSockets = new List<qsocket_t>( numsockets );
            _ActiveSockets = new List<qsocket_t>( numsockets );

            for( i = 0; i < numsockets; i++ )
                _FreeSockets.Add( new qsocket_t() );

            SetNetTime();

            // allocate space for network message buffer
            Message = new MsgWriter( NET_MAXMESSAGE ); // SZ_Alloc (&net_message, NET_MAXMESSAGE);
            Reader = new MsgReader( Net.Message );

            if( _MessageTimeout == null )
            {
                _MessageTimeout = new Cvar( "net_messagetimeout", "300" );
                _HostName = new Cvar( "hostname", "UNNAMED" );
            }

            Cmd.Add( "slist", Slist_f );
            Cmd.Add( "listen", Listen_f );
            Cmd.Add( "maxplayers", MaxPlayers_f );
            Cmd.Add( "port", Port_f );

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

                qsocket_t ret = Net.Driver.CheckNewConnections();
                if( ret != null )
                {
                    if( _IsRecording )
                    {
                        _VcrConnect.time = Host.Time;
                        _VcrConnect.op = VcrOp.VCR_OP_CONNECT;
                        _VcrConnect.session = 1; // (long)ret; // Uze: todo: make it work on 64bit systems
                        byte[] buf = Sys.StructureToBytes( ref _VcrConnect );
                        Host.VcrWriter.Write( buf, 0, buf.Length );
                        buf = Encoding.ASCII.GetBytes( ret.address );
                        int count = Math.Min( buf.Length, NET_NAMELEN );
                        int extra = NET_NAMELEN - count;
                        Host.VcrWriter.Write( buf, 0, count );
                        for( int i = 0; i < extra; i++ )
                            Host.VcrWriter.Write( (byte)0 );
                    }
                    return ret;
                }
            }

            if( _IsRecording )
            {
                _VcrConnect.time = Host.Time;
                _VcrConnect.op = VcrOp.VCR_OP_CONNECT;
                _VcrConnect.session = 0;
                byte[] buf = Sys.StructureToBytes( ref _VcrConnect );
                Host.VcrWriter.Write( buf, 0, buf.Length );
            }

            return null;
        }

        // hostcache
        // hostCacheCount
        /// <summary>
        /// NET_Connect
        /// called by client to connect to a host.  Returns -1 if not able to connect
        /// </summary>
        public static qsocket_t Connect( string host )
        {
            int numdrivers = _Drivers.Length;// net_numdrivers;

            SetNetTime();

            if( String.IsNullOrEmpty( host ) )
                host = null;

            if( host != null )
            {
                if( Common.SameText( host, "local" ) )
                {
                    numdrivers = 1;
                    goto JustDoIt;
                }

                if( HostCacheCount > 0 )
                {
                    foreach( hostcache_t hc in _HostCache )
                    {
                        if( Common.SameText( hc.name, host ) )
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
                if( Common.SameText( host, hc.name ) )
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
        public static bool CanSendMessage( qsocket_t sock )
        {
            if( sock == null )
                return false;

            if( sock.disconnected )
                return false;

            SetNetTime();

            bool r = _Drivers[sock.driver].CanSendMessage( sock );

            if( _IsRecording )
            {
                _VcrSendMessage.time = Host.Time;
                _VcrSendMessage.op = VcrOp.VCR_OP_CANSENDMESSAGE;
                _VcrSendMessage.session = 1; // (long)sock; Uze: todo: do something?
                _VcrSendMessage.ret = r ? 1 : 0;
                byte[] buf = Sys.StructureToBytes( ref _VcrSendMessage );
                Host.VcrWriter.Write( buf, 0, buf.Length );
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
        public static int GetMessage( qsocket_t sock )
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

            int ret = _Drivers[sock.driver].GetMessage( sock );

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
                    _VcrGetMessage.time = Host.Time;
                    _VcrGetMessage.op = VcrOp.VCR_OP_GETMESSAGE;
                    _VcrGetMessage.session = 1;// (long)sock; Uze todo: write somethisng meaningful
                    _VcrGetMessage.ret = ret;
                    byte[] buf = Sys.StructureToBytes( ref _VcrGetMessage );
                    Host.VcrWriter.Write( buf, 0, buf.Length );
                    Host.VcrWriter.Write( Net.Message.Length );
                    Host.VcrWriter.Write( Net.Message.Data, 0, Net.Message.Length );
                }
            }
            else
            {
                if( _IsRecording )
                {
                    _VcrGetMessage.time = Host.Time;
                    _VcrGetMessage.op = VcrOp.VCR_OP_GETMESSAGE;
                    _VcrGetMessage.session = 1; // (long)sock; Uze todo: fix this
                    _VcrGetMessage.ret = ret;
                    byte[] buf = Sys.StructureToBytes( ref _VcrGetMessage );
                    Host.VcrWriter.Write( buf, 0, buf.Length );
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
        public static int SendMessage( qsocket_t sock, MsgWriter data )
        {
            if( sock == null )
                return -1;

            if( sock.disconnected )
            {
                Con.Print( "NET_SendMessage: disconnected socket\n" );
                return -1;
            }

            SetNetTime();

            int r = _Drivers[sock.driver].SendMessage( sock, data );
            if( r == 1 && sock.driver != 0 )
                _MessagesSent++;

            if( _IsRecording )
            {
                _VcrSendMessage.time = Host.Time;
                _VcrSendMessage.op = VcrOp.VCR_OP_SENDMESSAGE;
                _VcrSendMessage.session = 1; // (long)sock; Uze: todo: do something?
                _VcrSendMessage.ret = r;
                byte[] buf = Sys.StructureToBytes( ref _VcrSendMessage );
                Host.VcrWriter.Write( buf, 0, buf.Length );
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
        public static int SendUnreliableMessage( qsocket_t sock, MsgWriter data )
        {
            if( sock == null )
                return -1;

            if( sock.disconnected )
            {
                Con.Print( "NET_SendMessage: disconnected socket\n" );
                return -1;
            }

            SetNetTime();

            int r = _Drivers[sock.driver].SendUnreliableMessage( sock, data );
            if( r == 1 && sock.driver != 0 )
                _UnreliableMessagesSent++;

            if( _IsRecording )
            {
                _VcrSendMessage.time = Host.Time;
                _VcrSendMessage.op = VcrOp.VCR_OP_SENDMESSAGE;
                _VcrSendMessage.session = 1;// (long)sock; Uze todo: ???????
                _VcrSendMessage.ret = r;
                byte[] buf = Sys.StructureToBytes( ref _VcrSendMessage );
                Host.VcrWriter.Write( buf );
            }

            return r;
        }

        /// <summary>
        /// NET_SendToAll
        /// This is a reliable *blocking* send to all attached clients.
        /// </summary>
        public static int SendToAll( MsgWriter data, int blocktime )
        {
            bool[] state1 = new bool[QDef.MAX_SCOREBOARD];
            bool[] state2 = new bool[QDef.MAX_SCOREBOARD];

            int count = 0;
            for( int i = 0; i < Server.svs.maxclients; i++ )
            {
                Host.HostClient = Server.svs.clients[i];
                if( Host.HostClient.netconnection == null )
                    continue;

                if( Host.HostClient.active )
                {
                    if( Host.HostClient.netconnection.driver == 0 )
                    {
                        SendMessage( Host.HostClient.netconnection, data );
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

            double start = Sys.GetFloatTime();
            while( count > 0 )
            {
                count = 0;
                for( int i = 0; i < Server.svs.maxclients; i++ )
                {
                    Host.HostClient = Server.svs.clients[i];
                    if( !state1[i] )
                    {
                        if( CanSendMessage( Host.HostClient.netconnection ) )
                        {
                            state1[i] = true;
                            SendMessage( Host.HostClient.netconnection, data );
                        }
                        else
                        {
                            GetMessage( Host.HostClient.netconnection );
                        }
                        count++;
                        continue;
                    }

                    if( !state2[i] )
                    {
                        if( CanSendMessage( Host.HostClient.netconnection ) )
                        {
                            state2[i] = true;
                        }
                        else
                        {
                            GetMessage( Host.HostClient.netconnection );
                        }
                        count++;
                        continue;
                    }
                }
                if( ( Sys.GetFloatTime() - start ) > blocktime )
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
                Sys.Error( "NET_FreeQSocket: not active\n" );

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
        public static double SetNetTime()
        {
            _Time = Sys.GetFloatTime();
            return _Time;
        }

        /// <summary>
        /// NET_Slist_f
        /// </summary>
        public static void Slist_f()
        {
            if( _SlistInProgress )
                return;

            if( !Net.SlistSilent )
            {
                Con.Print( "Looking for Quake servers...\n" );
                PrintSlistHeader();
            }

            _SlistInProgress = true;
            _SlistStartTime = Sys.GetFloatTime();

            SchedulePollProcedure( _SlistSendProcedure, 0.0 );
            SchedulePollProcedure( _SlistPollProcedure, 0.1 );

            Net.HostCacheCount = 0;
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

            if( Net.ActiveConnections >= Server.svs.maxclients )
                return null;

            // get one from free list
            int i = _FreeSockets.Count - 1;
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
            int i;
            for( i = _SlistLastShown; i < HostCacheCount; i++ )
            {
                hostcache_t hc = _HostCache[i];
                if( hc.maxusers != 0 )
                    Con.Print( "{0,-15} {1,-15}\n {2,2}/{3,2}\n", Common.Copy( hc.name, 15 ), Common.Copy( hc.map, 15 ), hc.users, hc.maxusers );
                else
                    Con.Print( "{0,-15} {1,-15}\n", Common.Copy( hc.name, 15 ), Common.Copy( hc.map, 15 ) );
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
        private static void SchedulePollProcedure( PollProcedure proc, double timeOffset )
        {
            proc.nextTime = Sys.GetFloatTime() + timeOffset;
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
            if( Cmd.Argc != 2 )
            {
                Con.Print( "\"listen\" is \"{0}\"\n", _IsListening ? 1 : 0 );
                return;
            }

            _IsListening = ( Common.atoi( Cmd.Argv( 1 ) ) != 0 );

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
            if( Cmd.Argc != 2 )
            {
                Con.Print( "\"maxplayers\" is \"%u\"\n", Server.svs.maxclients );
                return;
            }

            if( Server.sv.active )
            {
                Con.Print( "maxplayers can not be changed while a server is running.\n" );
                return;
            }

            int n = Common.atoi( Cmd.Argv( 1 ) );
            if( n < 1 )
                n = 1;
            if( n > Server.svs.maxclientslimit )
            {
                n = Server.svs.maxclientslimit;
                Con.Print( "\"maxplayers\" set to \"{0}\"\n", n );
            }

            if( n == 1 && _IsListening )
                Cbuf.AddText( "listen 0\n" );

            if( n > 1 && !_IsListening )
                Cbuf.AddText( "listen 1\n" );

            Server.svs.maxclients = n;
            if( n == 1 )
                Cvar.Set( "deathmatch", "0" );
            else
                Cvar.Set( "deathmatch", "1" );
        }

        // NET_Port_f
        private static void Port_f()
        {
            if( Cmd.Argc != 2 )
            {
                Con.Print( "\"port\" is \"{0}\"\n", HostPort );
                return;
            }

            int n = Common.atoi( Cmd.Argv( 1 ) );
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
        private static void SlistSend( object arg )
        {
            for( _DriverLevel = 0; _DriverLevel < _Drivers.Length; _DriverLevel++ )
            {
                if( !Net.SlistLocal && _DriverLevel == 0 )
                    continue;
                if( !_Drivers[_DriverLevel].IsInitialized )
                    continue;

                _Drivers[_DriverLevel].SearchForHosts( true );
            }

            if( ( Sys.GetFloatTime() - _SlistStartTime ) < 0.5 )
                SchedulePollProcedure( _SlistSendProcedure, 0.75 );
        }

        /// <summary>
        /// Slist_Poll
        /// </summary>
        private static void SlistPoll( object arg )
        {
            for( _DriverLevel = 0; _DriverLevel < _Drivers.Length; _DriverLevel++ )
            {
                if( !Net.SlistLocal && _DriverLevel == 0 )
                    continue;
                if( !_Drivers[_DriverLevel].IsInitialized )
                    continue;

                _Drivers[_DriverLevel].SearchForHosts( false );
            }

            if( !Net.SlistSilent )
                PrintSlist();

            if( ( Sys.GetFloatTime() - _SlistStartTime ) < 1.5 )
            {
                SchedulePollProcedure( _SlistPollProcedure, 0.1 );
                return;
            }

            if( !Net.SlistSilent )
                PrintSlistTrailer();

            _SlistInProgress = false;
            Net.SlistSilent = false;
            Net.SlistLocal = true;
        }

        [StructLayout( LayoutKind.Sequential, Pack = 1 )]
        private class VcrRecord2 : VcrRecord
        {
            public int ret;
            // Uze: int len - removed
        } //vcrGetMessage;
    }

    /// <summary>
    /// NetHeader flags
    /// </summary>
    internal static class NetFlags
    {
        public const int NETFLAG_LENGTH_MASK = 0x0000ffff;
        public const int NETFLAG_DATA = 0x00010000;
        public const int NETFLAG_ACK = 0x00020000;
        public const int NETFLAG_NAK = 0x00040000;
        public const int NETFLAG_EOM = 0x00080000;
        public const int NETFLAG_UNRELIABLE = 0x00100000;
        public const int NETFLAG_CTL = -2147483648;// 0x80000000;
    }

    internal static class CCReq
    {
        public const int CCREQ_CONNECT = 0x01;
        public const int CCREQ_SERVER_INFO = 0x02;
        public const int CCREQ_PLAYER_INFO = 0x03;
        public const int CCREQ_RULE_INFO = 0x04;
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
        public const int CCREP_ACCEPT = 0x81;
        public const int CCREP_REJECT = 0x82;
        public const int CCREP_SERVER_INFO = 0x83;
        public const int CCREP_PLAYER_INFO = 0x84;
        public const int CCREP_RULE_INFO = 0x85;
    }

    // qsocket_t
    internal class qsocket_t
    {
        public INetLanDriver LanDriver
        {
            get
            {
                return Net.LanDrivers[this.landriver];
            }
        }

        public double connecttime;
        public double lastMessageTime;
        public double lastSendTime;

        public bool disconnected;
        public bool canSend;
        public bool sendNext;

        public int driver;
        public int landriver;
        public Socket socket; // int	socket
        public object driverdata; // void *driverdata

        public uint ackSequence;
        public uint sendSequence;
        public uint unreliableSendSequence;

        public int sendMessageLength;
        public byte[] sendMessage; // byte sendMessage [NET_MAXMESSAGE]

        public uint receiveSequence;
        public uint unreliableReceiveSequence;

        public int receiveMessageLength;
        public byte[] receiveMessage; // byte receiveMessage [NET_MAXMESSAGE]

        public EndPoint addr; // qsockaddr	addr
        public string address; // char address[NET_NAMELEN]

        public void ClearBuffers()
        {
            this.sendMessageLength = 0;
            this.receiveMessageLength = 0;
        }

        public int Read( byte[] buf, int len, ref EndPoint ep )
        {
            return this.LanDriver.Read( this.socket, buf, len, ref ep );
        }

        public int Write( byte[] buf, int len, EndPoint ep )
        {
            return this.LanDriver.Write( this.socket, buf, len, ep );
        }

        public qsocket_t()
        {
            this.sendMessage = new byte[Net.NET_MAXMESSAGE];
            this.receiveMessage = new byte[Net.NET_MAXMESSAGE];
            disconnected = true;
        }
    }

    internal class PollProcedure
    {
        public PollProcedure next;
        public double nextTime;
        public PollHandler procedure; // void (*procedure)();
        public object arg; // void *arg

        public PollProcedure( PollProcedure next, double nextTime, PollHandler handler, object arg )
        {
            this.next = next;
            this.nextTime = nextTime;
            this.procedure = handler;
            this.arg = arg;
        }
    }

    internal class hostcache_t
    {
        public string name; //[16];
        public string map; //[16];
        public string cname; //[32];
        public int users;
        public int maxusers;
        public int driver;
        public int ldriver;
        public EndPoint addr; // qsockaddr ?????
    }

    // struct net_driver_t
    internal interface INetDriver
    {
        string Name
        {
            get;
        }

        bool IsInitialized
        {
            get;
        }

        void Init();

        void Listen( bool state );

        void SearchForHosts( bool xmit );

        qsocket_t Connect( string host );

        qsocket_t CheckNewConnections();

        int GetMessage( qsocket_t sock );

        int SendMessage( qsocket_t sock, MsgWriter data );

        int SendUnreliableMessage( qsocket_t sock, MsgWriter data );

        bool CanSendMessage( qsocket_t sock );

        bool CanSendUnreliableMessage( qsocket_t sock );

        void Close( qsocket_t sock );

        void Shutdown();
    } //net_driver_t;

    // struct net_landriver_t
    internal interface INetLanDriver
    {
        string Name
        {
            get;
        }

        bool IsInitialized
        {
            get;
        }

        Socket ControlSocket
        {
            get;
        }

        bool Init();

        void Shutdown();

        void Listen( bool state );

        Socket OpenSocket( int port );

        int CloseSocket( Socket socket );

        int Connect( Socket socket, EndPoint addr );

        Socket CheckNewConnections();

        int Read( Socket socket, byte[] buf, int len, ref EndPoint ep );

        int Write( Socket socket, byte[] buf, int len, EndPoint ep );

        int Broadcast( Socket socket, byte[] buf, int len );

        string GetNameFromAddr( EndPoint addr );

        EndPoint GetAddrFromName( string name );

        int AddrCompare( EndPoint addr1, EndPoint addr2 );

        int GetSocketPort( EndPoint addr );

        int SetSocketPort( EndPoint addr, int port );
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
