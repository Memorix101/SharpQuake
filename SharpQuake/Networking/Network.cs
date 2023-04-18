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
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using SharpQuake.Framework;
using SharpQuake.Framework.IO;
using SharpQuake.Game.Client;

namespace SharpQuake
{
	internal delegate void PollHandler( Object arg );

	public class Network
	{
		public INetDriver[] Drivers
		{
			get
			{
				return _Drivers;
			}
		}

		public INetLanDriver[] LanDrivers
		{
			get
			{
				return _LanDrivers;
			}
		}

		public IEnumerable<qsocket_t> ActiveSockets
		{
			get
			{
				return _ActiveSockets;
			}
		}

		public IEnumerable<qsocket_t> FreeSockets
		{
			get
			{
				return _ActiveSockets;
			}
		}

		public Int32 MessagesSent
		{
			get
			{
				return _MessagesSent;
			}
		}

		public Int32 MessagesReceived
		{
			get
			{
				return _MessagesReceived;
			}
		}

		public Int32 UnreliableMessagesSent
		{
			get
			{
				return _UnreliableMessagesSent;
			}
		}

		public Int32 UnreliableMessagesReceived
		{
			get
			{
				return _UnreliableMessagesReceived;
			}
		}

		public String HostName
		{
			get
			{
				return Host.Cvars.HostName.Get<String>();
			}
		}

		public String MyTcpIpAddress
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

		public Int32 DefaultHostPort
		{
			get
			{
				return _DefHostPort;
			}
		}

		public Boolean TcpIpAvailable
		{
			get
			{
				return net_tcp_ip.Instance.IsInitialised;
			}
		}

		public hostcache_t[] HostCache
		{
			get
			{
				return _HostCache;
			}
		}

		public Int32 DriverLevel
		{
			get
			{
				return _DriverLevel;
			}
		}

		public INetLanDriver LanDriver
		{
			get
			{
				return _LanDrivers[LanDriverLevel];
			}
		}

		public INetDriver Driver
		{
			get
			{
				return _Drivers[_DriverLevel];
			}
		}

		public Boolean SlistInProgress
		{
			get
			{
				return _SlistInProgress;
			}
		}

		public Double Time
		{
			get
			{
				return _Time;
			}
		}


		public Int32 HostPort;

		public Int32 ActiveConnections;

		public MessageWriter Message;

		// sizebuf_t net_message
		public MessageReader Reader;

		public Int32 HostCacheCount;

		public Boolean SlistSilent;

		// slistSilent
		public Boolean SlistLocal = true;

		public Int32 LanDriverLevel;

		private PollProcedure _SlistSendProcedure;
		private PollProcedure _SlistPollProcedure;

		private INetDriver[] _Drivers;

		// net_driver_t net_drivers[MAX_NET_DRIVERS];
		private INetLanDriver[] _LanDrivers;

		// net_landriver_t	net_landrivers[MAX_NET_DRIVERS]
		private Boolean _IsRecording;

		// recording
		private Int32 _DefHostPort = 26000;

		// int	DEFAULTnet_hostport = 26000;
		// net_hostport;
		private Boolean _IsListening;

		// qboolean	listening = false;
		private List<qsocket_t> _FreeSockets;

		// net_freeSockets
		private List<qsocket_t> _ActiveSockets;

		// net_activeSockets
		// net_activeconnections
		private Double _Time;

		private String _MyTcpIpAddress;

		// char my_tcpip_address[NET_NAMELEN];
		private Int32 _MessagesSent = 0;

		// reads from net_message
		private Int32 _MessagesReceived = 0;

		// net_time
		private Int32 _UnreliableMessagesSent = 0;

		private Int32 _UnreliableMessagesReceived = 0;

		private PollProcedure _PollProcedureList;

		private hostcache_t[] _HostCache = new hostcache_t[NetworkDef.HOSTCACHESIZE];

		private Boolean _SlistInProgress;

		// slistInProgress
		// slistLocal
		private Int32 _SlistLastShown;

		// slistLastShown
		private Double _SlistStartTime;

		private Int32 _DriverLevel;

		private VcrRecord _VcrConnect = new VcrRecord();

		// vcrConnect
		private VcrRecord2 _VcrGetMessage = new VcrRecord2();

		// vcrGetMessage
		private VcrRecord2 _VcrSendMessage = new VcrRecord2();

		public Network( Host host )
		{
			Host = host;

			_SlistSendProcedure = new PollProcedure( null, 0.0, SlistSend, null );
			_SlistPollProcedure = new PollProcedure( null, 0.0, SlistPoll, null );

			// Temporary workaround will sort out soon
			NetworkWrapper.OnGetLanDriver += ( index ) =>
			{
				return LanDrivers[index];
			};
		}

		// CHANGE
		private Host Host
		{
			get;
			set;
		}

		// vcrSendMessage
		// NET_Init (void)
		public void Initialise( )
		{
			for ( var i2 = 0; i2 < _HostCache.Length; i2++ )
				_HostCache[i2] = new hostcache_t();

			if ( _Drivers == null )
			{
				if ( CommandLine.HasParam( "-playback" ) )
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

			if ( _LanDrivers == null )
			{
				_LanDrivers = new INetLanDriver[]
				{
					net_tcp_ip.Instance
				};
			}

			if ( CommandLine.HasParam( "-record" ) )
				_IsRecording = true;

			var i = CommandLine.CheckParm( "-port" );
			if ( i == 0 )
				i = CommandLine.CheckParm( "-udpport" );
			if ( i == 0 )
				i = CommandLine.CheckParm( "-ipxport" );

			if ( i > 0 )
			{
				if ( i < CommandLine.Argc - 1 )
					_DefHostPort = MathLib.atoi( CommandLine.Argv( i + 1 ) );
				else
					Utilities.Error( "Net.Init: you must specify a number after -port!" );
			}
			HostPort = _DefHostPort;

			if ( CommandLine.HasParam( "-listen" ) || Host.Client.cls.state == cactive_t.ca_dedicated )
				_IsListening = true;
			var numsockets = Host.Server.svs.maxclientslimit;
			if ( Host.Client.cls.state != cactive_t.ca_dedicated )
				numsockets++;

			_FreeSockets = new List<qsocket_t>( numsockets );
			_ActiveSockets = new List<qsocket_t>( numsockets );

			for ( i = 0; i < numsockets; i++ )
				_FreeSockets.Add( new qsocket_t() );

			SetNetTime();

			// allocate space for network message buffer
			Message = new MessageWriter( NetworkDef.NET_MAXMESSAGE ); // SZ_Alloc (&net_message, NET_MAXMESSAGE);
			Reader = new MessageReader( Message );

			if ( Host.Cvars.MessageTimeout == null )
			{
				Host.Cvars.MessageTimeout = Host.CVars.Add( "net_messagetimeout", 300 );
				Host.Cvars.HostName = Host.CVars.Add( "hostname", "UNNAMED" );
			}

			Host.Commands.Add( "slist", Slist_f );
			Host.Commands.Add( "listen", Listen_f );
			Host.Commands.Add( "maxplayers", MaxPlayers_f );
			Host.Commands.Add( "port", Port_f );

			// initialize all the drivers
			_DriverLevel = 0;
			foreach ( var driver in _Drivers )
			{
				driver.Initialise( Host );
				if ( driver.IsInitialised && _IsListening )
				{
					driver.Listen( true );
				}
				_DriverLevel++;
			}

			//if (*my_ipx_address)
			//    Con_DPrintf("IPX address %s\n", my_ipx_address);
			if ( !String.IsNullOrEmpty( _MyTcpIpAddress ) )
				Host.Console.DPrint( "TCP/IP address {0}\n", _MyTcpIpAddress );
		}

		// net_driverlevel
		// net_landriverlevel
		/// <summary>
		/// NET_Shutdown
		/// </summary>
		public void Shutdown( )
		{
			SetNetTime();

			if ( _ActiveSockets != null )
			{
				var tmp = _ActiveSockets.ToArray();
				foreach ( var sock in tmp )
					Close( sock );
			}

			//
			// shutdown the drivers
			//
			if ( _Drivers != null )
			{
				for ( _DriverLevel = 0; _DriverLevel < _Drivers.Length; _DriverLevel++ )
				{
					if ( _Drivers[_DriverLevel].IsInitialised )
						_Drivers[_DriverLevel].Shutdown();
				}
			}
		}

		// slistStartTime
		/// <summary>
		/// NET_CheckNewConnections
		/// </summary>
		/// <returns></returns>
		public qsocket_t CheckNewConnections( )
		{
			SetNetTime();

			for ( _DriverLevel = 0; _DriverLevel < _Drivers.Length; _DriverLevel++ )
			{
				if ( !_Drivers[_DriverLevel].IsInitialised )
					continue;

				if ( _DriverLevel > 0 && !_IsListening )
					continue;

				var ret = Driver.CheckNewConnections();
				if ( ret != null )
				{
					if ( _IsRecording )
					{
						_VcrConnect.time = Host.Time;
						_VcrConnect.op = VcrOp.VCR_OP_CONNECT;
						_VcrConnect.session = 1; // (long)ret; // Uze: todo: make it work on 64bit systems
						var buf = Utilities.StructureToBytes( ref _VcrConnect );
						Host.VcrWriter.Write( buf, 0, buf.Length );
						buf = Encoding.ASCII.GetBytes( ret.address );
						var count = Math.Min( buf.Length, NetworkDef.NET_NAMELEN );
						var extra = NetworkDef.NET_NAMELEN - count;
						Host.VcrWriter.Write( buf, 0, count );
						for ( var i = 0; i < extra; i++ )
							Host.VcrWriter.Write( ( Byte ) 0 );
					}
					return ret;
				}
			}

			if ( _IsRecording )
			{
				_VcrConnect.time = Host.Time;
				_VcrConnect.op = VcrOp.VCR_OP_CONNECT;
				_VcrConnect.session = 0;
				var buf = Utilities.StructureToBytes( ref _VcrConnect );
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
		public qsocket_t Connect( String host )
		{
			var numdrivers = _Drivers.Length;// net_numdrivers;

			SetNetTime();

			if ( String.IsNullOrEmpty( host ) )
				host = null;

			if ( host != null )
			{
				if ( Utilities.SameText( host, "local" ) )
				{
					numdrivers = 1;
					goto JustDoIt;
				}

				if ( HostCacheCount > 0 )
				{
					foreach ( var hc in _HostCache )
					{
						if ( Utilities.SameText( hc.name, host ) )
						{
							host = hc.cname;
							goto JustDoIt;
						}
					}
				}
			}

			SlistSilent = ( host != null );
			Slist_f( null );

			while ( _SlistInProgress )
				Poll();

			if ( host == null )
			{
				if ( HostCacheCount != 1 )
					return null;
				host = _HostCache[0].cname;
				Host.Console.Print( "Connecting to...\n{0} @ {1}\n\n", _HostCache[0].name, host );
			}

			_DriverLevel = 0;
			foreach ( var hc in _HostCache )
			{
				if ( Utilities.SameText( host, hc.name ) )
				{
					host = hc.cname;
					break;
				}
				_DriverLevel++;
			}

		JustDoIt:
			_DriverLevel = 0;
			foreach ( var drv in _Drivers )
			{
				if ( !drv.IsInitialised )
					continue;
				var ret = drv.Connect( host );
				if ( ret != null )
					return ret;
				_DriverLevel++;
			}

			if ( host != null )
			{
				Host.Console.Print( "\n" );
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
		public Boolean CanSendMessage( qsocket_t sock )
		{
			if ( sock == null )
				return false;

			if ( sock.disconnected )
				return false;

			SetNetTime();

			var r = _Drivers[sock.driver].CanSendMessage( sock );

			if ( _IsRecording )
			{
				_VcrSendMessage.time = Host.Time;
				_VcrSendMessage.op = VcrOp.VCR_OP_CANSENDMESSAGE;
				_VcrSendMessage.session = 1; // (long)sock; Uze: todo: do something?
				_VcrSendMessage.ret = r ? 1 : 0;
				var buf = Utilities.StructureToBytes( ref _VcrSendMessage );
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
		public Int32 GetMessage( qsocket_t sock )
		{
			//int ret;

			if ( sock == null )
				return -1;

			if ( sock.disconnected )
			{
				Host.Console.Print( "NET_GetMessage: disconnected socket\n" );
				return -1;
			}

			SetNetTime();

			var ret = _Drivers[sock.driver].GetMessage( sock );

			// see if this connection has timed out
			if ( ret == 0 && sock.driver != 0 )
			{
				if ( _Time - sock.lastMessageTime > Host.Cvars.MessageTimeout.Get<Int32>() )
				{
					Close( sock );
					return -1;
				}
			}

			if ( ret > 0 )
			{
				if ( sock.driver != 0 )
				{
					sock.lastMessageTime = _Time;
					if ( ret == 1 )
						_MessagesReceived++;
					else if ( ret == 2 )
						_UnreliableMessagesReceived++;
				}

				if ( _IsRecording )
				{
					_VcrGetMessage.time = Host.Time;
					_VcrGetMessage.op = VcrOp.VCR_OP_GETMESSAGE;
					_VcrGetMessage.session = 1;// (long)sock; Uze todo: write somethisng meaningful
					_VcrGetMessage.ret = ret;
					var buf = Utilities.StructureToBytes( ref _VcrGetMessage );
					Host.VcrWriter.Write( buf, 0, buf.Length );
					Host.VcrWriter.Write( Message.Length );
					Host.VcrWriter.Write( Message.Data, 0, Message.Length );
				}
			}
			else
			{
				if ( _IsRecording )
				{
					_VcrGetMessage.time = Host.Time;
					_VcrGetMessage.op = VcrOp.VCR_OP_GETMESSAGE;
					_VcrGetMessage.session = 1; // (long)sock; Uze todo: fix this
					_VcrGetMessage.ret = ret;
					var buf = Utilities.StructureToBytes( ref _VcrGetMessage );
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
		public Int32 SendMessage( qsocket_t sock, MessageWriter data )
		{
			if ( sock == null )
				return -1;

			if ( sock.disconnected )
			{
				Host.Console.Print( "NET_SendMessage: disconnected socket\n" );
				return -1;
			}

			SetNetTime();

			var r = _Drivers[sock.driver].SendMessage( sock, data );
			if ( r == 1 && sock.driver != 0 )
				_MessagesSent++;

			if ( _IsRecording )
			{
				_VcrSendMessage.time = Host.Time;
				_VcrSendMessage.op = VcrOp.VCR_OP_SENDMESSAGE;
				_VcrSendMessage.session = 1; // (long)sock; Uze: todo: do something?
				_VcrSendMessage.ret = r;
				var buf = Utilities.StructureToBytes( ref _VcrSendMessage );
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
		public Int32 SendUnreliableMessage( qsocket_t sock, MessageWriter data )
		{
			if ( sock == null )
				return -1;

			if ( sock.disconnected )
			{
				Host.Console.Print( "NET_SendMessage: disconnected socket\n" );
				return -1;
			}

			SetNetTime();

			var r = _Drivers[sock.driver].SendUnreliableMessage( sock, data );
			if ( r == 1 && sock.driver != 0 )
				_UnreliableMessagesSent++;

			if ( _IsRecording )
			{
				_VcrSendMessage.time = Host.Time;
				_VcrSendMessage.op = VcrOp.VCR_OP_SENDMESSAGE;
				_VcrSendMessage.session = 1;// (long)sock; Uze todo: ???????
				_VcrSendMessage.ret = r;
				var buf = Utilities.StructureToBytes( ref _VcrSendMessage );
				Host.VcrWriter.Write( buf );
			}

			return r;
		}

		/// <summary>
		/// NET_SendToAll
		/// This is a reliable *blocking* send to all attached clients.
		/// </summary>
		public Int32 SendToAll( MessageWriter data, Int32 blocktime )
		{
			var state1 = new Boolean[QDef.MAX_SCOREBOARD];
			var state2 = new Boolean[QDef.MAX_SCOREBOARD];

			var count = 0;
			for ( var i = 0; i < Host.Server.svs.maxclients; i++ )
			{
				Host.HostClient = Host.Server.svs.clients[i];
				if ( Host.HostClient.netconnection == null )
					continue;

				if ( Host.HostClient.active )
				{
					if ( Host.HostClient.netconnection.driver == 0 )
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

			var start = Timer.GetFloatTime();
			while ( count > 0 )
			{
				count = 0;
				for ( var i = 0; i < Host.Server.svs.maxclients; i++ )
				{
					Host.HostClient = Host.Server.svs.clients[i];
					if ( !state1[i] )
					{
						if ( CanSendMessage( Host.HostClient.netconnection ) )
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

					if ( !state2[i] )
					{
						if ( CanSendMessage( Host.HostClient.netconnection ) )
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
				if ( ( Timer.GetFloatTime() - start ) > blocktime )
					break;
			}
			return count;
		}

		/// <summary>
		/// NET_Close
		/// </summary>
		public void Close( qsocket_t sock )
		{
			if ( sock == null )
				return;

			if ( sock.disconnected )
				return;

			SetNetTime();

			// call the driver_Close function
			_Drivers[sock.driver].Close( sock );

			FreeSocket( sock );
		}

		/// <summary>
		/// NET_FreeQSocket
		/// </summary>
		public void FreeSocket( qsocket_t sock )
		{
			// remove it from active list
			if ( !_ActiveSockets.Remove( sock ) )
				Utilities.Error( "NET_FreeQSocket: not active\n" );

			// add it to free list
			_FreeSockets.Add( sock );
			sock.disconnected = true;
		}

		/// <summary>
		/// NET_Poll
		/// </summary>
		public void Poll( )
		{
			SetNetTime();

			for ( var pp = _PollProcedureList; pp != null; pp = pp.next )
			{
				if ( pp.nextTime > _Time )
					break;

				_PollProcedureList = pp.next;
				pp.procedure( pp.arg );
			}
		}

		// double SetNetTime
		public Double SetNetTime( )
		{
			_Time = Timer.GetFloatTime();
			return _Time;
		}

		/// <summary>
		/// NET_Slist_f
		/// </summary>
		public void Slist_f( CommandMessage msg )
		{
			if ( _SlistInProgress )
				return;

			if ( !SlistSilent )
			{
				Host.Console.Print( "Looking for Quake servers...\n" );
				PrintSlistHeader();
			}

			_SlistInProgress = true;
			_SlistStartTime = Timer.GetFloatTime();

			SchedulePollProcedure( _SlistSendProcedure, 0.0 );
			SchedulePollProcedure( _SlistPollProcedure, 0.1 );

			HostCacheCount = 0;
		}

		/// <summary>
		/// NET_NewQSocket
		/// Called by drivers when a new communications endpoint is required
		/// The sequence and buffer fields will be filled in properly
		/// </summary>
		public qsocket_t NewSocket( )
		{
			if ( _FreeSockets.Count == 0 )
				return null;

			if ( ActiveConnections >= Host.Server.svs.maxclients )
				return null;

			// get one from free list
			var i = _FreeSockets.Count - 1;
			var sock = _FreeSockets[i];
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
		private void PrintSlistHeader( )
		{
			Host.Console.Print( "Server          Map             Users\n" );
			Host.Console.Print( "--------------- --------------- -----\n" );
			_SlistLastShown = 0;
		}

		// = { "hostname", "UNNAMED" };
		private void PrintSlist( )
		{
			Int32 i;
			for ( i = _SlistLastShown; i < HostCacheCount; i++ )
			{
				var hc = _HostCache[i];
				if ( hc.maxusers != 0 )
					Host.Console.Print( "{0,-15} {1,-15}\n {2,2}/{3,2}\n", Utilities.Copy( hc.name, 15 ), Utilities.Copy( hc.map, 15 ), hc.users, hc.maxusers );
				else
					Host.Console.Print( "{0,-15} {1,-15}\n", Utilities.Copy( hc.name, 15 ), Utilities.Copy( hc.map, 15 ) );
			}
			_SlistLastShown = i;
		}

		private void PrintSlistTrailer( )
		{
			if ( HostCacheCount != 0 )
				Host.Console.Print( "== end list ==\n\n" );
			else
				Host.Console.Print( "No Quake servers found.\n\n" );
		}

		/// <summary>
		/// SchedulePollProcedure
		/// </summary>
		private void SchedulePollProcedure( PollProcedure proc, Double timeOffset )
		{
			proc.nextTime = Timer.GetFloatTime() + timeOffset;
			PollProcedure pp, prev;
			for ( pp = _PollProcedureList, prev = null; pp != null; pp = pp.next )
			{
				if ( pp.nextTime >= proc.nextTime )
					break;
				prev = pp;
			}

			if ( prev == null )
			{
				proc.next = _PollProcedureList;
				_PollProcedureList = proc;
				return;
			}

			proc.next = pp;
			prev.next = proc;
		}

		// NET_Listen_f
		private void Listen_f( CommandMessage msg )
		{
			if ( msg.Parameters == null || msg.Parameters.Length != 1 )
			{
				Host.Console.Print( "\"listen\" is \"{0}\"\n", _IsListening ? 1 : 0 );
				return;
			}

			_IsListening = ( MathLib.atoi( msg.Parameters[0] ) != 0 );

			foreach ( var driver in _Drivers )
			{
				if ( driver.IsInitialised )
				{
					driver.Listen( _IsListening );
				}
			}
		}

		// MaxPlayers_f
		private void MaxPlayers_f( CommandMessage msg )
		{
			if ( msg.Parameters == null || msg.Parameters.Length != 1 )
			{
				Host.Console.Print( $"\"maxplayers\" is \"{Host.Server.svs.maxclients}\"\n" );
				return;
			}

			if ( Host.Server.sv.active )
			{
				Host.Console.Print( "maxplayers can not be changed while a server is running.\n" );
				return;
			}

			var n = MathLib.atoi( msg.Parameters[0] );
			if ( n < 1 )
				n = 1;
			if ( n > Host.Server.svs.maxclientslimit )
			{
				n = Host.Server.svs.maxclientslimit;
				Host.Console.Print( "\"maxplayers\" set to \"{0}\"\n", n );
			}

			if ( n == 1 && _IsListening )
				Host.Commands.Buffer.Append( "listen 0\n" );

			if ( n > 1 && !_IsListening )
				Host.Commands.Buffer.Append( "listen 1\n" );

			Host.Server.svs.maxclients = n;
			if ( n == 1 )
				Host.CVars.Set( "deathmatch", 0 );
			else
				Host.CVars.Set( "deathmatch", 1 );
		}

		// NET_Port_f
		private void Port_f( CommandMessage msg )
		{
			if ( msg.Parameters == null || msg.Parameters.Length != 1 )
			{
				Host.Console.Print( $"\"port\" is \"{HostPort}\"\n" );
				return;
			}

			var n = MathLib.atoi( msg.Parameters[0] );
			if ( n < 1 || n > 65534 )
			{
				Host.Console.Print( "Bad value, must be between 1 and 65534\n" );
				return;
			}

			_DefHostPort = n;
			HostPort = n;

			if ( _IsListening )
			{
				// force a change to the new port
				Host.Commands.Buffer.Append( "listen 0\n" );
				Host.Commands.Buffer.Append( "listen 1\n" );
			}
		}

		/// <summary>
		/// Slist_Send
		/// </summary>
		private void SlistSend( Object arg )
		{
			for ( _DriverLevel = 0; _DriverLevel < _Drivers.Length; _DriverLevel++ )
			{
				if ( !SlistLocal && _DriverLevel == 0 )
					continue;
				if ( !_Drivers[_DriverLevel].IsInitialised )
					continue;

				_Drivers[_DriverLevel].SearchForHosts( true );
			}

			if ( ( Timer.GetFloatTime() - _SlistStartTime ) < 0.5 )
				SchedulePollProcedure( _SlistSendProcedure, 0.75 );
		}

		/// <summary>
		/// Slist_Poll
		/// </summary>
		private void SlistPoll( Object arg )
		{
			for ( _DriverLevel = 0; _DriverLevel < _Drivers.Length; _DriverLevel++ )
			{
				if ( !SlistLocal && _DriverLevel == 0 )
					continue;
				if ( !_Drivers[_DriverLevel].IsInitialised )
					continue;

				_Drivers[_DriverLevel].SearchForHosts( false );
			}

			if ( !SlistSilent )
				PrintSlist();

			if ( ( Timer.GetFloatTime() - _SlistStartTime ) < 1.5 )
			{
				SchedulePollProcedure( _SlistPollProcedure, 0.1 );
				return;
			}

			if ( !SlistSilent )
				PrintSlistTrailer();

			_SlistInProgress = false;
			SlistSilent = false;
			SlistLocal = true;
		}

		[StructLayout( LayoutKind.Sequential, Pack = 1 )]
		private class VcrRecord2 : VcrRecord
		{
			public Int32 ret;
			// Uze: int len - removed
		} //vcrGetMessage;

		// Temporary fix to support pulling messagereader/writer from main code


	}

	public static class MessageWriterExtensions
	{
		public static Int32 FillFrom( this MessageWriter writer, Network network, Socket socket, ref EndPoint ep )
		{
			writer.Clear();
			var result = network.LanDriver.Read( socket, writer._Buffer, writer._Buffer.Length, ref ep );
			if ( result >= 0 )
				writer._Count = result;
			return result;
		}
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
			procedure = handler;
			this.arg = arg;
		}
	}






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
