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
using SharpQuake.Framework.IO;
using SharpQuake.Framework.IO.BSP;
using SharpQuake.Framework.IO.Input;
using SharpQuake.Framework.Mathematics;
using SharpQuake.Game.Data.Models;
using SharpQuake.Game.Networking.Server;
using SharpQuake.Game.Rendering;
using SharpQuake.Game.Rendering.Memory;

namespace SharpQuake
{
	partial class server
	{
		private Int32 _FatBytes; // fatbytes
		private Byte[] _FatPvs = new Byte[BspDef.MAX_MAP_LEAFS / 8]; // fatpvs

		// Instances
		private Host Host
		{
			get;
			set;
		}

		// SV_Init
		public void Initialise( )
		{
			for ( var i = 0; i < _BoxClipNodes.Length; i++ )
			{
				_BoxClipNodes[i].children = new Int16[2];
			}
			for ( var i = 0; i < _BoxPlanes.Length; i++ )
			{
				_BoxPlanes[i] = new Plane();
			}
			for ( var i = 0; i < _AreaNodes.Length; i++ )
			{
				_AreaNodes[i] = new areanode_t();
			}

			if ( Host.Cvars.Friction == null )
			{
				Host.Cvars.Friction = Host.CVars.Add( "sv_friction", 4f, ClientVariableFlags.Server );
				Host.Cvars.EdgeFriction = Host.CVars.Add( "edgefriction", 2f );
				Host.Cvars.StopSpeed = Host.CVars.Add( "sv_stopspeed", 100f );
				Host.Cvars.Gravity = Host.CVars.Add( "sv_gravity", 800f, ClientVariableFlags.Server );
				Host.Cvars.MaxVelocity = Host.CVars.Add( "sv_maxvelocity", 2000f );
				Host.Cvars.NoStep = Host.CVars.Add( "sv_nostep", false );
				Host.Cvars.MaxSpeed = Host.CVars.Add( "sv_maxspeed", 320f, ClientVariableFlags.Server );
				Host.Cvars.Accelerate = Host.CVars.Add( "sv_accelerate", 10f );
				Host.Cvars.Aim = Host.CVars.Add( "sv_aim", 0.93f );
				Host.Cvars.IdealPitchScale = Host.CVars.Add( "sv_idealpitchscale", 0.8f );
			}

			for ( var i = 0; i < QDef.MAX_MODELS; i++ )
				_LocalModels[i] = "*" + i.ToString();
		}

		/// <summary>
		/// SV_StartParticle
		/// Make sure the event gets sent to all clients
		/// </summary>
		public void StartParticle( ref Vector3 org, ref Vector3 dir, Int32 color, Int32 count )
		{
			if ( sv.datagram.Length > QDef.MAX_DATAGRAM - 16 )
				return;

			sv.datagram.WriteByte( ProtocolDef.svc_particle );
			sv.datagram.WriteCoord( org.X );
			sv.datagram.WriteCoord( org.Y );
			sv.datagram.WriteCoord( org.Z );

			var max = Vector3.One * 127;
			var min = Vector3.One * -128;
			var v = Vector3.Clamp( dir * 16, min, max );
			sv.datagram.WriteChar( ( Int32 ) v.X );
			sv.datagram.WriteChar( ( Int32 ) v.Y );
			sv.datagram.WriteChar( ( Int32 ) v.Z );
			sv.datagram.WriteByte( count );
			sv.datagram.WriteByte( color );
		}

		/// <summary>
		/// SV_StartSound
		/// Each entity can have eight independant sound sources, like voice,
		/// weapon, feet, etc.
		///
		/// Channel 0 is an auto-allocate channel, the others override anything
		/// allready running on that entity/channel pair.
		///
		/// An attenuation of 0 will play full volume everywhere in the level.
		/// Larger attenuations will drop off.  (max 4 attenuation)
		/// </summary>
		public void StartSound( MemoryEdict entity, Int32 channel, String sample, Int32 volume, Single attenuation )
		{
			if ( volume < 0 || volume > 255 )
				Utilities.Error( "SV_StartSound: volume = {0}", volume );

			if ( attenuation < 0 || attenuation > 4 )
				Utilities.Error( "SV_StartSound: attenuation = {0}", attenuation );

			if ( channel < 0 || channel > 7 )
				Utilities.Error( "SV_StartSound: channel = {0}", channel );

			if ( sv.datagram.Length > QDef.MAX_DATAGRAM - 16 )
				return;

			// find precache number for sound
			Int32 sound_num;
			for ( sound_num = 1; sound_num < QDef.MAX_SOUNDS && sv.sound_precache[sound_num] != null; sound_num++ )
				if ( sample == sv.sound_precache[sound_num] )
					break;

			if ( sound_num == QDef.MAX_SOUNDS || String.IsNullOrEmpty( sv.sound_precache[sound_num] ) )
			{
				Host.Console.Print( "SV_StartSound: {0} not precacheed\n", sample );
				return;
			}

			var ent = NumForEdict( entity );

			channel = ( ent << 3 ) | channel;

			var field_mask = 0;
			if ( volume != snd.DEFAULT_SOUND_PACKET_VOLUME )
				field_mask |= ProtocolDef.SND_VOLUME;
			if ( attenuation != snd.DEFAULT_SOUND_PACKET_ATTENUATION )
				field_mask |= ProtocolDef.SND_ATTENUATION;

			// directed messages go only to the entity the are targeted on
			sv.datagram.WriteByte( ProtocolDef.svc_sound );
			sv.datagram.WriteByte( field_mask );
			if ( ( field_mask & ProtocolDef.SND_VOLUME ) != 0 )
				sv.datagram.WriteByte( volume );
			if ( ( field_mask & ProtocolDef.SND_ATTENUATION ) != 0 )
				sv.datagram.WriteByte( ( Int32 ) ( attenuation * 64 ) );
			sv.datagram.WriteShort( channel );
			sv.datagram.WriteByte( sound_num );
			Vector3f v;
			MathLib.VectorAdd( ref entity.v.mins, ref entity.v.maxs, out v );
			MathLib.VectorMA( ref entity.v.origin, 0.5f, ref v, out v );
			sv.datagram.WriteCoord( v.x );
			sv.datagram.WriteCoord( v.y );
			sv.datagram.WriteCoord( v.z );
		}

		/// <summary>
		/// SV_DropClient
		/// Called when the player is getting totally kicked off the host
		/// if (crash = true), don't bother sending signofs
		/// </summary>
		public void DropClient( Boolean crash )
		{
			var client = Host.HostClient;

			if ( !crash )
			{
				// send any final messages (don't check for errors)
				if ( Host.Network.CanSendMessage( client.netconnection ) )
				{
					var msg = client.message;
					msg.WriteByte( ProtocolDef.svc_disconnect );
					Host.Network.SendMessage( client.netconnection, msg );
				}

				if ( client.edict != null && client.spawned )
				{
					// call the prog function for removing a client
					// this will set the body to a dead frame, among other things
					var saveSelf = Host.Programs.GlobalStruct.self;
					Host.Programs.GlobalStruct.self = EdictToProg( client.edict );
					Host.Programs.Execute( Host.Programs.GlobalStruct.ClientDisconnect );
					Host.Programs.GlobalStruct.self = saveSelf;
				}

				Host.Console.DPrint( "Client {0} removed\n", client.name );
			}

			// break the net connection
			Host.Network.Close( client.netconnection );
			client.netconnection = null;

			// free the client (the body stays around)
			client.active = false;
			client.name = null;
			client.old_frags = -999999;
			Host.Network.ActiveConnections--;

			// send notification to all clients
			for ( var i = 0; i < svs.maxclients; i++ )
			{
				var cl = svs.clients[i];
				if ( !cl.active )
					continue;

				cl.message.WriteByte( ProtocolDef.svc_updatename );
				cl.message.WriteByte( Host.ClientNum );
				cl.message.WriteString( "" );
				cl.message.WriteByte( ProtocolDef.svc_updatefrags );
				cl.message.WriteByte( Host.ClientNum );
				cl.message.WriteShort( 0 );
				cl.message.WriteByte( ProtocolDef.svc_updatecolors );
				cl.message.WriteByte( Host.ClientNum );
				cl.message.WriteByte( 0 );
			}
		}

		/// <summary>
		/// SV_SendClientMessages
		/// </summary>
		private void SendClientMessages( )
		{
			// update frags, names, etc
			UpdateToReliableMessages();

			// build individual updates
			for ( var i = 0; i < svs.maxclients; i++ )
			{
				Host.HostClient = svs.clients[i];

				if ( !Host.HostClient.active )
					continue;

				if ( Host.HostClient.spawned )
				{
					if ( !SendClientDatagram( Host.HostClient ) )
						continue;
				}
				else
				{
					// the player isn't totally in the game yet
					// send small keepalive messages if too much time has passed
					// send a full message when the next signon stage has been requested
					// some other message data (name changes, etc) may accumulate
					// between signon stages
					if ( !Host.HostClient.sendsignon )
					{
						if ( Host.RealTime - Host.HostClient.last_message > 5 )
							SendNop( Host.HostClient );
						continue;   // don't send out non-signon messages
					}
				}

				// check for an overflowed message.  Should only happen
				// on a very fucked up connection that backs up a lot, then
				// changes level
				if ( Host.HostClient.message.IsOveflowed )
				{
					DropClient( true );
					Host.HostClient.message.IsOveflowed = false;
					continue;
				}

				if ( Host.HostClient.message.Length > 0 || Host.HostClient.dropasap )
				{
					if ( !Host.Network.CanSendMessage( Host.HostClient.netconnection ) )
						continue;

					if ( Host.HostClient.dropasap )
						DropClient( false );    // went to another level
					else
					{
						if ( Host.Network.SendMessage( Host.HostClient.netconnection, Host.HostClient.message ) == -1 )
							DropClient( true ); // if the message couldn't send, kick off
						Host.HostClient.message.Clear();
						Host.HostClient.last_message = Host.RealTime;
						Host.HostClient.sendsignon = false;
					}
				}
			}

			// clear muzzle flashes
			CleanupEnts();
		}

		/// <summary>
		/// The start of server frame
		/// </summary>
		public void Frame()
		{
			// set the time and clear the general datagram
			ClearDatagram();

			// check for new clients
			CheckForNewClients();

			// read client messages
			RunClients();

			// move things around and think
			// always pause in single player if in console or menus
			if ( !sv.paused && ( svs.maxclients > 1 || Host.Keyboard.Destination == KeyDestination.key_game ) )
				Physics();

			// send all messages to the clients
			SendClientMessages();
		}

		/// <summary>
		/// SV_ClearDatagram
		/// </summary>
		private void ClearDatagram( )
		{
			sv.datagram.Clear();
		}

		/// <summary>
		/// SV_ModelIndex
		/// </summary>
		public Int32 ModelIndex( String name )
		{
			if ( String.IsNullOrEmpty( name ) )
				return 0;

			Int32 i;
			for ( i = 0; i < QDef.MAX_MODELS && sv.model_precache[i] != null; i++ )
				if ( sv.model_precache[i] == name )
					return i;

			if ( i == QDef.MAX_MODELS || String.IsNullOrEmpty( sv.model_precache[i] ) )
				Utilities.Error( "SV_ModelIndex: model {0} not precached", name );
			return i;
		}

		/// <summary>
		/// SV_ClientPrintf
		/// Sends text across to be displayed
		/// FIXME: make this just a stuffed echo?
		/// </summary>
		public void ClientPrint( String fmt, params Object[] args )
		{
			var tmp = String.Format( fmt, args );
			Host.HostClient.message.WriteByte( ProtocolDef.svc_print );
			Host.HostClient.message.WriteString( tmp );
		}

		/// <summary>
		/// SV_BroadcastPrint
		/// </summary>
		public void BroadcastPrint( String fmt, params Object[] args )
		{
			var tmp = args.Length > 0 ? String.Format( fmt, args ) : fmt;
			for ( var i = 0; i < svs.maxclients; i++ )
				if ( svs.clients[i].active && svs.clients[i].spawned )
				{
					var msg = svs.clients[i].message;
					msg.WriteByte( ProtocolDef.svc_print );
					msg.WriteString( tmp );
				}
		}

		private void WriteClientDamageMessage( MemoryEdict ent, MessageWriter msg )
		{
			if ( ent.v.dmg_take != 0 || ent.v.dmg_save != 0 )
			{
				var other = ProgToEdict( ent.v.dmg_inflictor );
				msg.WriteByte( ProtocolDef.svc_damage );
				msg.WriteByte( ( Int32 ) ent.v.dmg_save );
				msg.WriteByte( ( Int32 ) ent.v.dmg_take );
				msg.WriteCoord( other.v.origin.x + 0.5f * ( other.v.mins.x + other.v.maxs.x ) );
				msg.WriteCoord( other.v.origin.y + 0.5f * ( other.v.mins.y + other.v.maxs.y ) );
				msg.WriteCoord( other.v.origin.z + 0.5f * ( other.v.mins.z + other.v.maxs.z ) );

				ent.v.dmg_take = 0;
				ent.v.dmg_save = 0;
			}
		}

		private void WriteClientWeapons( MemoryEdict ent, MessageWriter msg )
		{
			if ( MainWindow.Common.GameKind == GameKind.StandardQuake )
			{
				msg.WriteByte( ( Int32 ) ent.v.weapon );
			}
			else
			{
				for ( var i = 0; i < 32; i++ )
				{
					if ( ( ( ( Int32 ) ent.v.weapon ) & ( 1 << i ) ) != 0 )
					{
						msg.WriteByte( i );
						break;
					}
				}
			}
		}

		private void WriteClientHeader( MessageWriter msg, Int32 bits )
		{
			msg.WriteByte( ProtocolDef.svc_clientdata );
			msg.WriteShort( bits );
		}

		private void WriteClientAmmo( MemoryEdict ent, MessageWriter msg )
		{
			msg.WriteByte( ( Int32 ) ent.v.currentammo );
			msg.WriteByte( ( Int32 ) ent.v.ammo_shells );
			msg.WriteByte( ( Int32 ) ent.v.ammo_nails );
			msg.WriteByte( ( Int32 ) ent.v.ammo_rockets );
			msg.WriteByte( ( Int32 ) ent.v.ammo_cells );
		}

		private void WriteClientFixAngle( MemoryEdict ent, MessageWriter msg )
		{
			if ( ent.v.fixangle != 0 )
			{
				msg.WriteByte( ProtocolDef.svc_setangle );
				msg.WriteAngle( ent.v.angles.x );
				msg.WriteAngle( ent.v.angles.y );
				msg.WriteAngle( ent.v.angles.z );
				ent.v.fixangle = 0;
			}
		}

		private void WriteClientView( MemoryEdict ent, MessageWriter msg, Int32 bits )
		{
			if ( ( bits & ProtocolDef.SU_VIEWHEIGHT ) != 0 )
				msg.WriteChar( ( Int32 ) ent.v.view_ofs.z );

			if ( ( bits & ProtocolDef.SU_IDEALPITCH ) != 0 )
				msg.WriteChar( ( Int32 ) ent.v.idealpitch );
		}

		private void WriteClientPunches( MemoryEdict ent, MessageWriter msg, Int32 bits )
		{
			if ( ( bits & ProtocolDef.SU_PUNCH1 ) != 0 )
				msg.WriteChar( ( Int32 ) ent.v.punchangle.x );
			if ( ( bits & ProtocolDef.SU_VELOCITY1 ) != 0 )
				msg.WriteChar( ( Int32 ) ( ent.v.velocity.x / 16 ) );

			if ( ( bits & ProtocolDef.SU_PUNCH2 ) != 0 )
				msg.WriteChar( ( Int32 ) ent.v.punchangle.y );
			if ( ( bits & ProtocolDef.SU_VELOCITY2 ) != 0 )
				msg.WriteChar( ( Int32 ) ( ent.v.velocity.y / 16 ) );

			if ( ( bits & ProtocolDef.SU_PUNCH3 ) != 0 )
				msg.WriteChar( ( Int32 ) ent.v.punchangle.z );
			if ( ( bits & ProtocolDef.SU_VELOCITY3 ) != 0 )
				msg.WriteChar( ( Int32 ) ( ent.v.velocity.z / 16 ) );
		}

		private void WriteClientItems( MemoryEdict ent, MessageWriter msg, Int32 items, Int32 bits )
		{
			msg.WriteLong( items );

			if ( ( bits & ProtocolDef.SU_WEAPONFRAME ) != 0 )
				msg.WriteByte( ( Int32 ) ent.v.weaponframe );
			if ( ( bits & ProtocolDef.SU_ARMOR ) != 0 )
				msg.WriteByte( ( Int32 ) ent.v.armorvalue );
			if ( ( bits & ProtocolDef.SU_WEAPON ) != 0 )
				msg.WriteByte( ModelIndex( Host.Programs.GetString( ent.v.weaponmodel ) ) );
		}

		private void WriteClientHealth( MemoryEdict ent, MessageWriter msg )
		{
			msg.WriteShort( ( Int32 ) ent.v.health );
		}

		private Int32 GenerateClientBits( MemoryEdict ent, out Int32 items )
		{
			var bits = 0;

			if ( ent.v.view_ofs.z != ProtocolDef.DEFAULT_VIEWHEIGHT )
				bits |= ProtocolDef.SU_VIEWHEIGHT;

			if ( ent.v.idealpitch != 0 )
				bits |= ProtocolDef.SU_IDEALPITCH;

			// stuff the sigil bits into the high bits of items for sbar, or else
			// mix in items2
			var val = Host.Programs.GetEdictFieldFloat( ent, "items2", 0 );

			if ( val != 0 )
				items = ( Int32 ) ent.v.items | ( ( Int32 ) val << 23 );
			else
				items = ( Int32 ) ent.v.items | ( ( Int32 ) Host.Programs.GlobalStruct.serverflags << 28 );

			bits |= ProtocolDef.SU_ITEMS;

			if ( ( ( Int32 ) ent.v.flags & EdictFlags.FL_ONGROUND ) != 0 )
				bits |= ProtocolDef.SU_ONGROUND;

			if ( ent.v.waterlevel >= 2 )
				bits |= ProtocolDef.SU_INWATER;

			if ( ent.v.punchangle.x != 0 )
				bits |= ProtocolDef.SU_PUNCH1;
			if ( ent.v.punchangle.y != 0 )
				bits |= ProtocolDef.SU_PUNCH2;
			if ( ent.v.punchangle.z != 0 )
				bits |= ProtocolDef.SU_PUNCH3;

			if ( ent.v.velocity.x != 0 )
				bits |= ProtocolDef.SU_VELOCITY1;
			if ( ent.v.velocity.y != 0 )
				bits |= ProtocolDef.SU_VELOCITY2;
			if ( ent.v.velocity.z != 0 )
				bits |= ProtocolDef.SU_VELOCITY3;

			if ( ent.v.weaponframe != 0 )
				bits |= ProtocolDef.SU_WEAPONFRAME;

			if ( ent.v.armorvalue != 0 )
				bits |= ProtocolDef.SU_ARMOR;

			//	if (ent.v.weapon)
			bits |= ProtocolDef.SU_WEAPON;

			return bits;
		}

		/// <summary>
		/// SV_WriteClientdataToMessage
		/// </summary>
		public void WriteClientDataToMessage( MemoryEdict ent, MessageWriter msg )
		{
			//
			// send a damage message
			//
			WriteClientDamageMessage( ent, msg );

			//
			// send the current viewpos offset from the view entity
			//
			SetIdealPitch();        // how much to look up / down ideally

			// a fixangle might get lost in a dropped packet.  Oh well.
			WriteClientFixAngle( ent, msg );

			var bits = GenerateClientBits( ent, out var items );

			// send the data
			WriteClientHeader( msg, bits );
			WriteClientView( ent, msg, bits );
			WriteClientPunches( ent, msg, bits );

			// always sent
			WriteClientItems( ent, msg, items, bits );
			WriteClientHealth( ent, msg );
			WriteClientAmmo( ent, msg );
			WriteClientWeapons( ent, msg );
		}

		/// <summary>
		/// SV_CheckForNewClients
		/// </summary>
		private void CheckForNewClients( )
		{
			//
			// check for new connections
			//
			while ( true )
			{
				var ret = Host.Network.CheckNewConnections();
				if ( ret == null )
					break;

				//
				// init a new client structure
				//
				Int32 i;
				for ( i = 0; i < svs.maxclients; i++ )
					if ( !svs.clients[i].active )
						break;
				if ( i == svs.maxclients )
					Utilities.Error( "Host_CheckForNewClients: no free clients" );

				svs.clients[i].netconnection = ret;
				ConnectClient( i );

				Host.Network.ActiveConnections++;
			}
		}

		/// <summary>
		/// SV_SaveSpawnparms
		/// Grabs the current state of each client for saving across the
		/// transition to another level
		/// </summary>
		public void SaveSpawnparms( )
		{
			svs.serverflags = ( Int32 ) Host.Programs.GlobalStruct.serverflags;

			for ( var i = 0; i < svs.maxclients; i++ )
			{
				Host.HostClient = svs.clients[i];
				if ( !Host.HostClient.active )
					continue;

				// call the progs to get default spawn parms for the new client
				Host.Programs.GlobalStruct.self = EdictToProg( Host.HostClient.edict );
				Host.Programs.Execute( Host.Programs.GlobalStruct.SetChangeParms );
				AssignGlobalSpawnparams( Host.HostClient );
			}
		}

		/// <summary>
		/// SV_SpawnServer
		/// </summary>
		public void SpawnServer( String server )
		{
			// let's not have any servers with no name
			if ( String.IsNullOrEmpty( Host.Network.HostName ) )
				Host.CVars.Set( "hostname", "UNNAMED" );

			Host.Screen.CenterTimeOff = 0;

			Host.Console.DPrint( "SpawnServer: {0}\n", server );
			svs.changelevel_issued = false;     // now safe to issue another

			//
			// tell all connected clients that we are going to a new level
			//
			if ( sv.active )
			{
				SendReconnect();
			}

			//
			// make cvars consistant
			//
			if ( Host.Cvars.Coop.Get<Boolean>() )
				Host.CVars.Set( "deathmatch", 0 );

			Host.CurrentSkill = ( Int32 ) ( Host.Cvars.Skill.Get<Int32>() + 0.5 );
			if ( Host.CurrentSkill < 0 )
				Host.CurrentSkill = 0;
			if ( Host.CurrentSkill > 3 )
				Host.CurrentSkill = 3;

			Host.CVars.Set( "skill", Host.CurrentSkill );

			//
			// set up the new server
			//
			Host.ClearMemory();

			sv.Clear();

			sv.name = server;

			// load progs to get entity field count
			Host.Programs.LoadProgs();

			// allocate server memory
			sv.max_edicts = QDef.MAX_EDICTS;

			sv.edicts = new MemoryEdict[sv.max_edicts];
			for ( var i = 0; i < sv.edicts.Length; i++ )
			{
				sv.edicts[i] = new MemoryEdict();
			}

			// leave slots at start for clients only
			sv.num_edicts = svs.maxclients + 1;
			MemoryEdict ent;
			for ( var i = 0; i < svs.maxclients; i++ )
			{
				ent = EdictNum( i + 1 );
				svs.clients[i].edict = ent;
			}

			sv.state = server_state_t.Loading;
			sv.paused = false;
			sv.time = 1.0;
			sv.modelname = String.Format( "maps/{0}.bsp", server );
			sv.worldmodel = ( BrushModelData ) Host.Model.ForName( sv.modelname, false, ModelType.Brush );
			if ( sv.worldmodel == null )
			{
				Host.Console.Print( "Couldn't spawn server {0}\n", sv.modelname );
				sv.active = false;
				return;
			}
			sv.models[1] = sv.worldmodel;

			//
			// clear world interaction links
			//
			ClearWorld();

			sv.sound_precache[0] = String.Empty;
			sv.model_precache[0] = String.Empty;

			sv.model_precache[1] = sv.modelname;
			for ( var i = 1; i < sv.worldmodel.NumSubModels; i++ )
			{
				sv.model_precache[1 + i] = _LocalModels[i];
				sv.models[i + 1] = Host.Model.ForName( _LocalModels[i], false, ModelType.Brush );
			}

			//
			// load the rest of the entities
			//
			ent = EdictNum( 0 );
			ent.Clear();
			ent.v.model = Host.Programs.StringOffset( sv.worldmodel.Name );
			if ( ent.v.model == -1 )
			{
				ent.v.model = Host.Programs.NewString( sv.worldmodel.Name );
			}
			ent.v.modelindex = 1;       // world model
			ent.v.solid = Solids.SOLID_BSP;
			ent.v.movetype = Movetypes.MOVETYPE_PUSH;

			if ( Host.Cvars.Coop.Get<Boolean>() )
				Host.Programs.GlobalStruct.coop = 1; //coop.value;
			else
				Host.Programs.GlobalStruct.deathmatch = Host.Cvars.Deathmatch.Get<Int32>();

			var offset = Host.Programs.NewString( sv.name );
			Host.Programs.GlobalStruct.mapname = offset;

			// serverflags are for cross level information (sigils)
			Host.Programs.GlobalStruct.serverflags = svs.serverflags;

			Host.Programs.LoadFromFile( sv.worldmodel.Entities );

			sv.active = true;

			// all setup is completed, any further precache statements are errors
			sv.state = server_state_t.Active;

			// run two frames to allow everything to settle
			Host.FrameTime = 0.1;
			Physics();
			Physics();

			// create a baseline for more efficient communications
			CreateBaseline();

			// send serverinfo to all connected clients
			for ( var i = 0; i < svs.maxclients; i++ )
			{
				Host.HostClient = svs.clients[i];
				if ( Host.HostClient.active )
					SendServerInfo( Host.HostClient );
			}

			GC.Collect();
			Host.Console.DPrint( "Server spawned.\n" );
		}

		/// <summary>
		/// SV_CleanupEnts
		/// </summary>
		private void CleanupEnts( )
		{
			for ( var i = 1; i < sv.num_edicts; i++ )
			{
				var ent = sv.edicts[i];
				ent.v.effects = ( Int32 ) ent.v.effects & ~EntityEffects.EF_MUZZLEFLASH;
			}
		}

		/// <summary>
		/// SV_SendNop
		/// Send a nop message without trashing or sending the accumulated client
		/// message buffer
		/// </summary>
		private void SendNop( client_t client )
		{
			var msg = new MessageWriter( 4 );
			msg.WriteChar( ProtocolDef.svc_nop );

			if ( Host.Network.SendUnreliableMessage( client.netconnection, msg ) == -1 )
				DropClient( true ); // if the message couldn't send, kick off
			client.last_message = Host.RealTime;
		}

		/// <summary>
		/// SV_SendClientDatagram
		/// </summary>
		private Boolean SendClientDatagram( client_t client )
		{
			var msg = new MessageWriter( QDef.MAX_DATAGRAM ); // Uze todo: make static?

			msg.WriteByte( ProtocolDef.svc_time );
			msg.WriteFloat( ( Single ) sv.time );

			// add the client specific data to the datagram
			WriteClientDataToMessage( client.edict, msg );

			WriteEntitiesToClient( client.edict, msg );

			// copy the server datagram if there is space
			if ( msg.Length + sv.datagram.Length < msg.Capacity )
				msg.Write( sv.datagram.Data, 0, sv.datagram.Length );

			// send the datagram
			if ( Host.Network.SendUnreliableMessage( client.netconnection, msg ) == -1 )
			{
				DropClient( true );// if the message couldn't send, kick off
				return false;
			}

			return true;
		}

		private Int32 SetupEntityBits( Int32 e, MemoryEdict ent )
		{
			var bits = 0;
			Vector3f miss;
			MathLib.VectorSubtract( ref ent.v.origin, ref ent.baseline.origin, out miss );
			if ( miss.x < -0.1f || miss.x > 0.1f )
				bits |= ProtocolDef.U_ORIGIN1;
			if ( miss.y < -0.1f || miss.y > 0.1f )
				bits |= ProtocolDef.U_ORIGIN2;
			if ( miss.z < -0.1f || miss.z > 0.1f )
				bits |= ProtocolDef.U_ORIGIN3;

			if ( ent.v.angles.x != ent.baseline.angles.x )
				bits |= ProtocolDef.U_ANGLE1;

			if ( ent.v.angles.y != ent.baseline.angles.y )
				bits |= ProtocolDef.U_ANGLE2;

			if ( ent.v.angles.z != ent.baseline.angles.z )
				bits |= ProtocolDef.U_ANGLE3;

			if ( ent.v.movetype == Movetypes.MOVETYPE_STEP )
				bits |= ProtocolDef.U_NOLERP;   // don't mess up the step animation

			if ( ent.baseline.colormap != ent.v.colormap )
				bits |= ProtocolDef.U_COLORMAP;

			if ( ent.baseline.skin != ent.v.skin )
				bits |= ProtocolDef.U_SKIN;

			if ( ent.baseline.frame != ent.v.frame )
				bits |= ProtocolDef.U_FRAME;

			if ( ent.baseline.effects != ent.v.effects )
				bits |= ProtocolDef.U_EFFECTS;

			if ( ent.baseline.modelindex != ent.v.modelindex )
				bits |= ProtocolDef.U_MODEL;

			if ( e >= 256 )
				bits |= ProtocolDef.U_LONGENTITY;

			if ( bits >= 256 )
				bits |= ProtocolDef.U_MOREBITS;

			return bits;
		}

		private void WriteEntityBytes( Int32 bits, Int32 e, MemoryEdict ent, MessageWriter msg )
		{
			msg.WriteByte( bits | ProtocolDef.U_SIGNAL );

			if ( ( bits & ProtocolDef.U_MOREBITS ) != 0 )
				msg.WriteByte( bits >> 8 );
			if ( ( bits & ProtocolDef.U_LONGENTITY ) != 0 )
				msg.WriteShort( e );
			else
				msg.WriteByte( e );

			if ( ( bits & ProtocolDef.U_MODEL ) != 0 )
				msg.WriteByte( ( Int32 ) ent.v.modelindex );
			if ( ( bits & ProtocolDef.U_FRAME ) != 0 )
				msg.WriteByte( ( Int32 ) ent.v.frame );
			if ( ( bits & ProtocolDef.U_COLORMAP ) != 0 )
				msg.WriteByte( ( Int32 ) ent.v.colormap );
			if ( ( bits & ProtocolDef.U_SKIN ) != 0 )
				msg.WriteByte( ( Int32 ) ent.v.skin );
			if ( ( bits & ProtocolDef.U_EFFECTS ) != 0 )
				msg.WriteByte( ( Int32 ) ent.v.effects );
			if ( ( bits & ProtocolDef.U_ORIGIN1 ) != 0 )
				msg.WriteCoord( ent.v.origin.x );
			if ( ( bits & ProtocolDef.U_ANGLE1 ) != 0 )
				msg.WriteAngle( ent.v.angles.x );
			if ( ( bits & ProtocolDef.U_ORIGIN2 ) != 0 )
				msg.WriteCoord( ent.v.origin.y );
			if ( ( bits & ProtocolDef.U_ANGLE2 ) != 0 )
				msg.WriteAngle( ent.v.angles.y );
			if ( ( bits & ProtocolDef.U_ORIGIN3 ) != 0 )
				msg.WriteCoord( ent.v.origin.z );
			if ( ( bits & ProtocolDef.U_ANGLE3 ) != 0 )
				msg.WriteAngle( ent.v.angles.z );
		}

		/// <summary>
		/// SV_WriteEntitiesToClient
		/// </summary>
		private void WriteEntitiesToClient( MemoryEdict clent, MessageWriter msg )
		{
			// find the client's PVS
			var org = Utilities.ToVector( ref clent.v.origin ) + Utilities.ToVector( ref clent.v.view_ofs );
			var pvs = FatPVS( ref org );

			// send over all entities (except the client) that touch the pvs
			for ( var e = 1; e < sv.num_edicts; e++ )
			{
				var ent = sv.edicts[e];
				// ignore if not touching a PV leaf
				if ( ent != clent ) // clent is ALLWAYS sent
				{
					// ignore ents without visible models
					var mname = Host.Programs.GetString( ent.v.model );
					if ( String.IsNullOrEmpty( mname ) )
						continue;

					Int32 i;
					for ( i = 0; i < ent.num_leafs; i++ )
						if ( ( pvs[ent.leafnums[i] >> 3] & ( 1 << ( ent.leafnums[i] & 7 ) ) ) != 0 )
							break;

					if ( i == ent.num_leafs )
						continue;       // not visible
				}

				if ( msg.Capacity - msg.Length < 16 )
				{
					Host.Console.Print( "packet overflow\n" );
					return;
				}

				// Send an update
				var bits = SetupEntityBits( e, ent );

				// Write the message
				WriteEntityBytes( bits, e, ent, msg );
			}
		}

		/// <summary>
		/// SV_FatPVS
		/// Calculates a PVS that is the inclusive or of all leafs within 8 pixels of the
		/// given point.
		/// </summary>
		private Byte[] FatPVS( ref Vector3 org )
		{
			_FatBytes = ( sv.worldmodel.NumLeafs + 31 ) >> 3;
			Array.Clear( _FatPvs, 0, _FatPvs.Length );
			AddToFatPVS( ref org, sv.worldmodel.Nodes[0] );
			return _FatPvs;
		}

		/// <summary>
		/// SV_AddToFatPVS
		/// The PVS must include a small area around the client to allow head bobbing
		/// or other small motion on the client side.  Otherwise, a bob might cause an
		/// entity that should be visible to not show up, especially when the bob
		/// crosses a waterline.
		/// </summary>
		private void AddToFatPVS( ref Vector3 org, MemoryNodeBase node )
		{
			while ( true )
			{
				// if this is a leaf, accumulate the pvs bits
				if ( node.contents < 0 )
				{
					if ( node.contents != ( Int32 ) Q1Contents.Solid )
					{
						var pvs = sv.worldmodel.LeafPVS( ( MemoryLeaf ) node );
						for ( var i = 0; i < _FatBytes; i++ )
							_FatPvs[i] |= pvs[i];
					}
					return;
				}

				var n = ( MemoryNode ) node;
				var plane = n.plane;
				var d = Vector3.Dot( org, plane.normal ) - plane.dist;
				if ( d > 8 )
					node = n.children[0];
				else if ( d < -8 )
					node = n.children[1];
				else
				{   // go down both
					AddToFatPVS( ref org, n.children[0] );
					node = n.children[1];
				}
			}
		}

		/// <summary>
		/// SV_UpdateToReliableMessages
		/// </summary>
		private void UpdateToReliableMessages( )
		{
			// check for changes to be sent over the reliable streams
			for ( var i = 0; i < svs.maxclients; i++ )
			{
				Host.HostClient = svs.clients[i];
				if ( Host.HostClient.old_frags != Host.HostClient.edict.v.frags )
				{
					for ( var j = 0; j < svs.maxclients; j++ )
					{
						var client = svs.clients[j];
						if ( !client.active )
							continue;

						client.message.WriteByte( ProtocolDef.svc_updatefrags );
						client.message.WriteByte( i );
						client.message.WriteShort( ( Int32 ) Host.HostClient.edict.v.frags );
					}

					Host.HostClient.old_frags = ( Int32 ) Host.HostClient.edict.v.frags;
				}
			}

			for ( var j = 0; j < svs.maxclients; j++ )
			{
				var client = svs.clients[j];
				if ( !client.active )
					continue;
				client.message.Write( sv.reliable_datagram.Data, 0, sv.reliable_datagram.Length );
			}

			sv.reliable_datagram.Clear();
		}

		/// <summary>
		/// SV_ConnectClient
		/// Initializes a client_t for a new net connection.  This will only be called
		/// once for a player each game, not once for each level change.
		/// </summary>
		private void ConnectClient( Int32 clientnum )
		{
			var client = svs.clients[clientnum];

			Host.Console.DPrint( "Client {0} connected\n", client.netconnection.address );

			var edictnum = clientnum + 1;
			var ent = EdictNum( edictnum );

			// set up the client_t
			var netconnection = client.netconnection;

			var spawn_parms = new Single[ServerDef.NUM_SPAWN_PARMS];
			if ( sv.loadgame )
			{
				Array.Copy( client.spawn_parms, spawn_parms, spawn_parms.Length );
			}

			client.Clear();
			client.netconnection = netconnection;
			client.name = "unconnected";
			client.active = true;
			client.spawned = false;
			client.edict = ent;
			client.message.AllowOverflow = true; // we can catch it
			client.privileged = false;

			if ( sv.loadgame )
			{
				Array.Copy( spawn_parms, client.spawn_parms, spawn_parms.Length );
			}
			else
			{
				// call the progs to get default spawn parms for the new client
				Host.Programs.Execute( Host.Programs.GlobalStruct.SetNewParms );

				AssignGlobalSpawnparams( client );
			}

			SendServerInfo( client );
		}

		private void AssignGlobalSpawnparams( client_t client )
		{
			client.spawn_parms[0] = Host.Programs.GlobalStruct.parm1;
			client.spawn_parms[1] = Host.Programs.GlobalStruct.parm2;
			client.spawn_parms[2] = Host.Programs.GlobalStruct.parm3;
			client.spawn_parms[3] = Host.Programs.GlobalStruct.parm4;

			client.spawn_parms[4] = Host.Programs.GlobalStruct.parm5;
			client.spawn_parms[5] = Host.Programs.GlobalStruct.parm6;
			client.spawn_parms[6] = Host.Programs.GlobalStruct.parm7;
			client.spawn_parms[7] = Host.Programs.GlobalStruct.parm8;

			client.spawn_parms[8] = Host.Programs.GlobalStruct.parm9;
			client.spawn_parms[9] = Host.Programs.GlobalStruct.parm10;
			client.spawn_parms[10] = Host.Programs.GlobalStruct.parm11;
			client.spawn_parms[11] = Host.Programs.GlobalStruct.parm12;

			client.spawn_parms[12] = Host.Programs.GlobalStruct.parm13;
			client.spawn_parms[13] = Host.Programs.GlobalStruct.parm14;
			client.spawn_parms[14] = Host.Programs.GlobalStruct.parm15;
			client.spawn_parms[15] = Host.Programs.GlobalStruct.parm16;
		}

		/// <summary>
		/// SV_SendServerinfo
		/// Sends the first message from the server to a connected client.
		/// This will be sent on the initial connection and upon each server load.
		/// </summary>
		private void SendServerInfo( client_t client )
		{
			var writer = client.message;

			writer.WriteByte( ProtocolDef.svc_print );
			writer.WriteString( String.Format( "{0}\nVERSION {1,4:F2} SERVER ({2} CRC)", ( Char ) 2, QDef.VERSION, Host.Programs.Crc ) );

			writer.WriteByte( ProtocolDef.svc_serverinfo );
			writer.WriteLong( ProtocolDef.PROTOCOL_VERSION );
			writer.WriteByte( svs.maxclients );

			if ( !Host.Cvars.Coop.Get<Boolean>() && Host.Cvars.Deathmatch.Get<Int32>() != 0 )
				writer.WriteByte( ProtocolDef.GAME_DEATHMATCH );
			else
				writer.WriteByte( ProtocolDef.GAME_COOP );

			var message = Host.Programs.GetString( sv.edicts[0].v.message );

			writer.WriteString( message );

			for ( var i = 1; i < sv.model_precache.Length; i++ )
			{
				var tmp = sv.model_precache[i];
				if ( String.IsNullOrEmpty( tmp ) )
					break;
				writer.WriteString( tmp );
			}
			writer.WriteByte( 0 );

			for ( var i = 1; i < sv.sound_precache.Length; i++ )
			{
				var tmp = sv.sound_precache[i];
				if ( tmp == null )
					break;
				writer.WriteString( tmp );
			}
			writer.WriteByte( 0 );

			// send music
			writer.WriteByte( ProtocolDef.svc_cdtrack );
			writer.WriteByte( ( Int32 ) sv.edicts[0].v.sounds );
			writer.WriteByte( ( Int32 ) sv.edicts[0].v.sounds );

			// set view
			writer.WriteByte( ProtocolDef.svc_setview );
			writer.WriteShort( NumForEdict( client.edict ) );

			writer.WriteByte( ProtocolDef.svc_signonnum );
			writer.WriteByte( 1 );

			client.sendsignon = true;
			client.spawned = false;     // need prespawn, spawn, etc
		}

		/// <summary>
		/// SV_SendReconnect
		/// Tell all the clients that the server is changing levels
		/// </summary>
		private void SendReconnect( )
		{
			var msg = new MessageWriter( 128 );

			msg.WriteChar( ProtocolDef.svc_stufftext );
			msg.WriteString( "reconnect\n" );
			Host.Network.SendToAll( msg, 5 );

			if ( Host.Client.cls.state != cactive_t.ca_dedicated )
				Host.Commands.ExecuteString( "reconnect\n", CommandSource.Command );
		}

		/// <summary>
		/// SV_CreateBaseline
		/// </summary>
		private void CreateBaseline( )
		{
			for ( var entnum = 0; entnum < sv.num_edicts; entnum++ )
			{
				// get the current server version
				var svent = EdictNum( entnum );
				if ( svent.free )
					continue;
				if ( entnum > svs.maxclients && svent.v.modelindex == 0 )
					continue;

				//
				// create entity baseline
				//
				svent.baseline.origin = svent.v.origin;
				svent.baseline.angles = svent.v.angles;
				svent.baseline.frame = ( Int32 ) svent.v.frame;
				svent.baseline.skin = ( Int32 ) svent.v.skin;
				if ( entnum > 0 && entnum <= svs.maxclients )
				{
					svent.baseline.colormap = entnum;
					svent.baseline.modelindex = ModelIndex( "progs/player.mdl" );
				}
				else
				{
					svent.baseline.colormap = 0;
					svent.baseline.modelindex = ModelIndex( Host.Programs.GetString( svent.v.model ) );
				}

				//
				// add to the message
				//
				sv.signon.WriteByte( ProtocolDef.svc_spawnbaseline );
				sv.signon.WriteShort( entnum );

				sv.signon.WriteByte( svent.baseline.modelindex );
				sv.signon.WriteByte( svent.baseline.frame );
				sv.signon.WriteByte( svent.baseline.colormap );
				sv.signon.WriteByte( svent.baseline.skin );

				sv.signon.WriteCoord( svent.baseline.origin.x );
				sv.signon.WriteAngle( svent.baseline.angles.x );
				sv.signon.WriteCoord( svent.baseline.origin.y );
				sv.signon.WriteAngle( svent.baseline.angles.y );
				sv.signon.WriteCoord( svent.baseline.origin.z );
				sv.signon.WriteAngle( svent.baseline.angles.z );
			}
		}
	}
}
