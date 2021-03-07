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

using SharpQuake.Framework;
using SharpQuake.Framework.IO;
using SharpQuake.Game.Data.Models;
using SharpQuake.Game.World;
using System;
using System.Collections.Generic;

// cl_parse.c

namespace SharpQuake
{
	partial class client
	{
		private const String ConsoleBar = "\n\n\u001D\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001F\n\n";

		private static String[] _SvcStrings = new String[]
		{
			"svc_bad",
			"svc_nop",
			"svc_disconnect",
			"svc_updatestat",
			"svc_version",		// [long] server version
	        "svc_setview",		// [short] entity number
	        "svc_sound",			// <see code>
	        "svc_time",			// [float] server time
	        "svc_print",			// [string] null terminated string
	        "svc_stufftext",		// [string] stuffed into client's console buffer
						        // the string should be \n terminated
	        "svc_setangle",		// [vec3] set the view angle to this absolute value

	        "svc_serverinfo",		// [long] version
						        // [string] signon string
						        // [string]..[0]model cache [string]...[0]sounds cache
						        // [string]..[0]item cache
	        "svc_lightstyle",		// [byte] [string]
	        "svc_updatename",		// [byte] [string]
	        "svc_updatefrags",	// [byte] [short]
	        "svc_clientdata",		// <shortbits + data>
	        "svc_stopsound",		// <see code>
	        "svc_updatecolors",	// [byte] [byte]
	        "svc_particle",		// [vec3] <variable>
	        "svc_damage",			// [byte] impact [byte] blood [vec3] from

	        "svc_spawnstatic",
			"OBSOLETE svc_spawnbinary",
			"svc_spawnbaseline",

			"svc_temp_entity",		// <variable>
	        "svc_setpause",
			"svc_signonnum",
			"svc_centerprint",
			"svc_killedmonster",
			"svc_foundsecret",
			"svc_spawnstaticsound",
			"svc_intermission",
			"svc_finale",			// [string] music [string] text
	        "svc_cdtrack",			// [byte] track [byte] looptrack
	        "svc_sellscreen",
			"svc_cutscene"
		};

		private Int32[] _BitCounts = new Int32[16]; // bitcounts
		private Object _MsgState; // used by KeepaliveMessage function
		private Single _LastMsg; // static float lastmsg from CL_KeepaliveMessage
		delegate void ProcessMessageDelegate();

		private Dictionary<Int32, ProcessMessageDelegate> MessageDelegates;

		private void MessageCommandNoOperation( )
		{
		}

		private void MessageCommandServerTime( )
		{
			cl.mtime[1] = cl.mtime[0];
			cl.mtime[0] = Host.Network.Reader.ReadFloat();
		}

		private void MessageCommandClientData()
		{
			var i = Host.Network.Reader.ReadShort();
			ParseClientData( i );
		}

		private void MessageCommandVersion()
		{
			var i = Host.Network.Reader.ReadLong();

			if ( i != ProtocolDef.PROTOCOL_VERSION )
				Host.Error( "CL_ParseServerMessage: Server is protocol {0} instead of {1}\n", i, ProtocolDef.PROTOCOL_VERSION );
		}

		private void MessageCommandDisconnect()
		{
			Host.EndGame( "Server disconnected\n" );
		}

		private void MessageCommandPrint()
		{
			Host.Console.Print( Host.Network.Reader.ReadString() );
		}

		private void MessageCommandCentrePrint()
		{
			Host.Screen.CenterPrint( Host.Network.Reader.ReadString() );
		}

		private void MessageCommandStuffText()
		{
			Host.Commands.Buffer.Append( Host.Network.Reader.ReadString() );
		}

		private void MessageCommandDamage()
		{
			Host.View.ParseDamage();
		}

		private void MessageCommandServerInfo()
		{
			ParseServerInfo();
			Host.Screen.vid.recalc_refdef = true;   // leave intermission full screen
		}

		private void MessageCommandSetAngle()
		{
			cl.viewangles.X = Host.Network.Reader.ReadAngle();
			cl.viewangles.Y = Host.Network.Reader.ReadAngle();
			cl.viewangles.Z = Host.Network.Reader.ReadAngle();
		}

		private void MessageCommandSetView()
		{
			cl.viewentity = Host.Network.Reader.ReadShort();
		}

		private void MessageCommandLightStyle()
		{
			var i = Host.Network.Reader.ReadByte();

			if ( i >= QDef.MAX_LIGHTSTYLES )
				Utilities.Error( "svc_lightstyle > MAX_LIGHTSTYLES" );

			_LightStyle[i].map = Host.Network.Reader.ReadString();
		}

		private void MessageCommandSound()
		{
			ParseStartSoundPacket();
		}

		private void MessageCommandStopSound()
		{
			var i = Host.Network.Reader.ReadShort();
			Host.Sound.StopSound( i >> 3, i & 7 );
		}

		private void MessageCommandUpdateName()
		{
			Host.Hud.Changed();

			var i = Host.Network.Reader.ReadByte();

			if ( i >= cl.maxclients )
				Host.Error( "CL_ParseServerMessage: svc_updatename > MAX_SCOREBOARD" );

			cl.scores[i].name = Host.Network.Reader.ReadString();
		}

		private void MessageCommandUpdateFrags()
		{
			Host.Hud.Changed();

			var i = Host.Network.Reader.ReadByte();

			if ( i >= cl.maxclients )
				Host.Error( "CL_ParseServerMessage: svc_updatefrags > MAX_SCOREBOARD" );

			cl.scores[i].frags = Host.Network.Reader.ReadShort();
		}

		private void MessageCommandUpdateColours()
		{
			Host.Hud.Changed();

			var i = Host.Network.Reader.ReadByte();

			if ( i >= cl.maxclients )
				Host.Error( "CL_ParseServerMessage: svc_updatecolors > MAX_SCOREBOARD" );

			cl.scores[i].colors = Host.Network.Reader.ReadByte();
			NewTranslation( i );
		}

		private void MessageCommandParticle()
		{
			Host.RenderContext.Particles.ParseParticleEffect( Host.Client.cl.time, Host.Network.Reader );
		}

		private void MessageCommandSpawnBaseline()
		{
			var i = Host.Network.Reader.ReadShort();
			// must use CL_EntityNum() to force cl.num_entities up
			ParseBaseline( EntityNum( i ) );
		}

		private void MessageCommandSpawnStatic()
		{
			ParseStatic();
		}

		private void MessageCommandTempEntity()
		{
			ParseTempEntity();
		}

		private void MessageCommandSetPause()
		{
			cl.paused = Host.Network.Reader.ReadByte() != 0;

			if ( cl.paused )
			{
				Host.CDAudio.Pause();
			}
			else
			{
				Host.CDAudio.Resume();
			}
		}

		private void MessageCommandSignOnNum()
		{
			var i = Host.Network.Reader.ReadByte();

			if ( i <= cls.signon )
				Host.Error( "Received signon {0} when at {1}", i, cls.signon );

			cls.signon = i;
			SignonReply();
		}

		private void MessageCommandKilledMonster()
		{
			cl.stats[QStatsDef.STAT_MONSTERS]++;
		}

		private void MessageCommandFoundSecret()
		{
			cl.stats[QStatsDef.STAT_SECRETS]++;
		}

		private void MessageCommandUpdateStat()
		{
			var i = Host.Network.Reader.ReadByte();

			if ( i < 0 || i >= QStatsDef.MAX_CL_STATS )
				Utilities.Error( "svc_updatestat: {0} is invalid", i );

			cl.stats[i] = Host.Network.Reader.ReadLong();
		}

		private void MessageCommandSpawnStaticSound()
		{
			ParseStaticSound();
		}

		private void MessageCommandCDTrack()
		{
			cl.cdtrack = Host.Network.Reader.ReadByte();
			cl.looptrack = Host.Network.Reader.ReadByte();

			if ( ( cls.demoplayback || cls.demorecording ) && ( cls.forcetrack != -1 ) )
				Host.CDAudio.Play( ( Byte ) cls.forcetrack, true );
			else
				Host.CDAudio.Play( ( Byte ) cl.cdtrack, true );
		}

		private void MessageCommandIntermission()
		{
			cl.intermission = 1;
			cl.completed_time = ( Int32 ) cl.time;
			Host.Screen.vid.recalc_refdef = true;   // go to full screen
		}

		private void MessageCommandFinale()
		{
			cl.intermission = 2;
			cl.completed_time = ( Int32 ) cl.time;
			Host.Screen.vid.recalc_refdef = true;   // go to full screen
			Host.Screen.CenterPrint( Host.Network.Reader.ReadString() );
		}

		private void MessageCommandCutScene()
		{
			cl.intermission = 3;
			cl.completed_time = ( Int32 ) cl.time;
			Host.Screen.vid.recalc_refdef = true;   // go to full screen
			Host.Screen.CenterPrint( Host.Network.Reader.ReadString() );
		}

		private void MessageCommandSellScreen()
		{
			Host.Commands.ExecuteString( "help", CommandSource.Command );
		}

		protected void InitialiseMessageDelegates()
		{
			MessageDelegates = new Dictionary<Int32, ProcessMessageDelegate>
			{
				{ ProtocolDef.svc_nop, MessageCommandNoOperation },
				{ ProtocolDef.svc_time, MessageCommandServerTime },
				{ ProtocolDef.svc_clientdata, MessageCommandClientData },
				{ ProtocolDef.svc_version, MessageCommandVersion },
				{ ProtocolDef.svc_disconnect, MessageCommandDisconnect },
				{ ProtocolDef.svc_print, MessageCommandPrint },
				{ ProtocolDef.svc_centerprint, MessageCommandCentrePrint },
				{ ProtocolDef.svc_stufftext, MessageCommandStuffText },
				{ ProtocolDef.svc_damage, MessageCommandDamage },
				{ ProtocolDef.svc_serverinfo, MessageCommandServerInfo },
				{ ProtocolDef.svc_setangle, MessageCommandSetAngle },
				{ ProtocolDef.svc_setview, MessageCommandSetView },
				{ ProtocolDef.svc_lightstyle, MessageCommandLightStyle },
				{ ProtocolDef.svc_sound, MessageCommandSound },
				{ ProtocolDef.svc_stopsound, MessageCommandStopSound },
				{ ProtocolDef.svc_updatename, MessageCommandUpdateName },
				{ ProtocolDef.svc_updatefrags, MessageCommandUpdateFrags },
				{ ProtocolDef.svc_updatecolors, MessageCommandUpdateColours },
				{ ProtocolDef.svc_particle, MessageCommandParticle },
				{ ProtocolDef.svc_spawnbaseline, MessageCommandSpawnBaseline },
				{ ProtocolDef.svc_spawnstatic, MessageCommandSpawnStatic },
				{ ProtocolDef.svc_temp_entity, MessageCommandTempEntity },
				{ ProtocolDef.svc_setpause, MessageCommandSetPause },
				{ ProtocolDef.svc_signonnum, MessageCommandSignOnNum },
				{ ProtocolDef.svc_killedmonster, MessageCommandKilledMonster },
				{ ProtocolDef.svc_foundsecret, MessageCommandFoundSecret },
				{ ProtocolDef.svc_updatestat, MessageCommandUpdateStat },
				{ ProtocolDef.svc_spawnstaticsound, MessageCommandSpawnStaticSound },
				{ ProtocolDef.svc_cdtrack, MessageCommandCDTrack },
				{ ProtocolDef.svc_intermission, MessageCommandIntermission },
				{ ProtocolDef.svc_finale, MessageCommandFinale },
				{ ProtocolDef.svc_cutscene, MessageCommandCutScene },
				{ ProtocolDef.svc_sellscreen, MessageCommandSellScreen }
			};
		}

		private void ProcessMessageCommand( Int32 cmd )
		{
			if ( MessageDelegates.ContainsKey( cmd ) )
				MessageDelegates[cmd]();
			else
				Host.Error( "CL_ParseServerMessage: Illegible server message\n" );
		}

		/// <summary>
		/// CL_ParseServerMessage
		/// </summary>
		private void ParseServerMessage( )
		{
			// If recording demos, copy the message out
			if ( Host.Cvars.ShowNet.Get<Int32>() == 1 )
				Host.Console.Print( "{0} ", Host.Network.Message.Length );
			else if ( Host.Cvars.ShowNet.Get<Int32>() == 2 )
				Host.Console.Print( "------------------\n" );

			cl.onground = false;    // unless the server says otherwise

			// Parse the message
			Host.Network.Reader.Reset();
			while ( true )
			{
				if ( Host.Network.Reader.IsBadRead )
					Host.Error( "CL_ParseServerMessage: Bad server message" );

				var cmd = Host.Network.Reader.ReadByte();
				if ( cmd == -1 )
				{
					ShowNet( "END OF MESSAGE" );
					return; // end of message
				}

				// if the high bit of the command byte is set, it is a fast update
				if ( ( cmd & 128 ) != 0 )
				{
					ShowNet( "fast update" );
					ParseUpdate( cmd & 127 );
					continue;
				}

				ShowNet( _SvcStrings[cmd] );

				// other commands
				ProcessMessageCommand( cmd );
			}
		}

		private void ShowNet( String s )
		{
			if ( Host.Cvars.ShowNet.Get<Int32>() == 2 )
				Host.Console.Print( "{0,3}:{1}\n", Host.Network.Reader.Position - 1, s );
		}

		/// <summary>
		/// CL_ParseUpdate
		///
		/// Parse an entity update message from the server
		/// If an entities model or origin changes from frame to frame, it must be
		/// relinked.  Other attributes can change without relinking.
		/// </summary>
		private void ParseUpdate( Int32 bits )
		{
			Int32 i;

			if ( cls.signon == ClientDef.SIGNONS - 1 )
			{
				// first update is the final signon stage
				cls.signon = ClientDef.SIGNONS;
				SignonReply();
			}

			if ( ( bits & ProtocolDef.U_MOREBITS ) != 0 )
			{
				i = Host.Network.Reader.ReadByte();
				bits |= ( i << 8 );
			}

			Int32 num;

			if ( ( bits & ProtocolDef.U_LONGENTITY ) != 0 )
				num = Host.Network.Reader.ReadShort();
			else
				num = Host.Network.Reader.ReadByte();

			var ent = EntityNum( num );
			for ( i = 0; i < 16; i++ )
				if ( ( bits & ( 1 << i ) ) != 0 )
					_BitCounts[i]++;

			var forcelink = false;
			if ( ent.msgtime != cl.mtime[1] )
				forcelink = true;   // no previous frame to lerp from

			ent.msgtime = cl.mtime[0];
			Int32 modnum;
			if ( ( bits & ProtocolDef.U_MODEL ) != 0 )
			{
				modnum = Host.Network.Reader.ReadByte();
				if ( modnum >= QDef.MAX_MODELS )
					Host.Error( "CL_ParseModel: bad modnum" );
			}
			else
				modnum = ent.baseline.modelindex;

			var model = cl.model_precache[modnum];
			if ( model != ent.model )
			{
				ent.model = model;
				// automatic animation (torches, etc) can be either all together
				// or randomized
				if ( model != null )
				{
					if ( model.SyncType == SyncType.ST_RAND )
						ent.syncbase = ( Single ) ( MathLib.Random() & 0x7fff ) / 0x7fff;
					else
						ent.syncbase = 0;
				}
				else
					forcelink = true;   // hack to make null model players work

				if ( num > 0 && num <= cl.maxclients )
					Host.RenderContext.TranslatePlayerSkin( num - 1 );
			}

			if ( ( bits & ProtocolDef.U_FRAME ) != 0 )
				ent.frame = Host.Network.Reader.ReadByte();
			else
				ent.frame = ent.baseline.frame;

			if ( ( bits & ProtocolDef.U_COLORMAP ) != 0 )
				i = Host.Network.Reader.ReadByte();
			else
				i = ent.baseline.colormap;
			if ( i == 0 )
				ent.colormap = Host.Screen.vid.colormap;
			else
			{
				if ( i > cl.maxclients )
					Utilities.Error( "i >= cl.maxclients" );
				ent.colormap = cl.scores[i - 1].translations;
			}

			Int32 skin;
			if ( ( bits & ProtocolDef.U_SKIN ) != 0 )
				skin = Host.Network.Reader.ReadByte();
			else
				skin = ent.baseline.skin;
			if ( skin != ent.skinnum )
			{
				ent.skinnum = skin;
				if ( num > 0 && num <= cl.maxclients )
					Host.RenderContext.TranslatePlayerSkin( num - 1 );
			}

			if ( ( bits & ProtocolDef.U_EFFECTS ) != 0 )
				ent.effects = Host.Network.Reader.ReadByte();
			else
				ent.effects = ent.baseline.effects;

			// shift the known values for interpolation
			ent.msg_origins[1] = ent.msg_origins[0];
			ent.msg_angles[1] = ent.msg_angles[0];

			if ( ( bits & ProtocolDef.U_ORIGIN1 ) != 0 )
				ent.msg_origins[0].X = Host.Network.Reader.ReadCoord();
			else
				ent.msg_origins[0].X = ent.baseline.origin.x;
			if ( ( bits & ProtocolDef.U_ANGLE1 ) != 0 )
				ent.msg_angles[0].X = Host.Network.Reader.ReadAngle();
			else
				ent.msg_angles[0].X = ent.baseline.angles.x;

			if ( ( bits & ProtocolDef.U_ORIGIN2 ) != 0 )
				ent.msg_origins[0].Y = Host.Network.Reader.ReadCoord();
			else
				ent.msg_origins[0].Y = ent.baseline.origin.y;
			if ( ( bits & ProtocolDef.U_ANGLE2 ) != 0 )
				ent.msg_angles[0].Y = Host.Network.Reader.ReadAngle();
			else
				ent.msg_angles[0].Y = ent.baseline.angles.y;

			if ( ( bits & ProtocolDef.U_ORIGIN3 ) != 0 )
				ent.msg_origins[0].Z = Host.Network.Reader.ReadCoord();
			else
				ent.msg_origins[0].Z = ent.baseline.origin.z;
			if ( ( bits & ProtocolDef.U_ANGLE3 ) != 0 )
				ent.msg_angles[0].Z = Host.Network.Reader.ReadAngle();
			else
				ent.msg_angles[0].Z = ent.baseline.angles.z;

			if ( ( bits & ProtocolDef.U_NOLERP ) != 0 )
				ent.forcelink = true;

			if ( forcelink )
			{   // didn't have an update last message
				ent.msg_origins[1] = ent.msg_origins[0];
				ent.origin = ent.msg_origins[0];
				ent.msg_angles[1] = ent.msg_angles[0];
				ent.angles = ent.msg_angles[0];
				ent.forcelink = true;
			}
		}

		/// <summary>
		/// CL_ParseClientdata
		/// Server information pertaining to this client only
		/// </summary>
		private void ParseClientData( Int32 bits )
		{
			if ( ( bits & ProtocolDef.SU_VIEWHEIGHT ) != 0 )
				cl.viewheight = Host.Network.Reader.ReadChar();
			else
				cl.viewheight = ProtocolDef.DEFAULT_VIEWHEIGHT;

			if ( ( bits & ProtocolDef.SU_IDEALPITCH ) != 0 )
				cl.idealpitch = Host.Network.Reader.ReadChar();
			else
				cl.idealpitch = 0;

			cl.mvelocity[1] = cl.mvelocity[0];
			for ( var i = 0; i < 3; i++ )
			{
				if ( ( bits & ( ProtocolDef.SU_PUNCH1 << i ) ) != 0 )
					MathLib.SetComp( ref cl.punchangle, i, Host.Network.Reader.ReadChar() );
				else
					MathLib.SetComp( ref cl.punchangle, i, 0 );
				if ( ( bits & ( ProtocolDef.SU_VELOCITY1 << i ) ) != 0 )
					MathLib.SetComp( ref cl.mvelocity[0], i, Host.Network.Reader.ReadChar() * 16 );
				else
					MathLib.SetComp( ref cl.mvelocity[0], i, 0 );
			}

			// [always sent]	if (bits & SU_ITEMS)
			var i2 = Host.Network.Reader.ReadLong();

			if ( cl.items != i2 )
			{   // set flash times
				Host.Hud.Changed();
				for ( var j = 0; j < 32; j++ )
					if ( ( i2 & ( 1 << j ) ) != 0 && ( cl.items & ( 1 << j ) ) == 0 )
						cl.item_gettime[j] = ( Single ) cl.time;
				cl.items = i2;
			}

			cl.onground = ( bits & ProtocolDef.SU_ONGROUND ) != 0;
			cl.inwater = ( bits & ProtocolDef.SU_INWATER ) != 0;

			if ( ( bits & ProtocolDef.SU_WEAPONFRAME ) != 0 )
				cl.stats[QStatsDef.STAT_WEAPONFRAME] = Host.Network.Reader.ReadByte();
			else
				cl.stats[QStatsDef.STAT_WEAPONFRAME] = 0;

			if ( ( bits & ProtocolDef.SU_ARMOR ) != 0 )
				i2 = Host.Network.Reader.ReadByte();
			else
				i2 = 0;
			if ( cl.stats[QStatsDef.STAT_ARMOR] != i2 )
			{
				cl.stats[QStatsDef.STAT_ARMOR] = i2;
				Host.Hud.Changed();
			}

			if ( ( bits & ProtocolDef.SU_WEAPON ) != 0 )
				i2 = Host.Network.Reader.ReadByte();
			else
				i2 = 0;
			if ( cl.stats[QStatsDef.STAT_WEAPON] != i2 )
			{
				cl.stats[QStatsDef.STAT_WEAPON] = i2;
				Host.Hud.Changed();
			}

			i2 = Host.Network.Reader.ReadShort();
			if ( cl.stats[QStatsDef.STAT_HEALTH] != i2 )
			{
				cl.stats[QStatsDef.STAT_HEALTH] = i2;
				Host.Hud.Changed();
			}

			i2 = Host.Network.Reader.ReadByte();
			if ( cl.stats[QStatsDef.STAT_AMMO] != i2 )
			{
				cl.stats[QStatsDef.STAT_AMMO] = i2;
				Host.Hud.Changed();
			}

			for ( i2 = 0; i2 < 4; i2++ )
			{
				var j = Host.Network.Reader.ReadByte();
				if ( cl.stats[QStatsDef.STAT_SHELLS + i2] != j )
				{
					cl.stats[QStatsDef.STAT_SHELLS + i2] = j;
					Host.Hud.Changed();
				}
			}

			i2 = Host.Network.Reader.ReadByte();

			// Change
			if ( MainWindow.Common.GameKind == GameKind.StandardQuake )
			{
				if ( cl.stats[QStatsDef.STAT_ACTIVEWEAPON] != i2 )
				{
					cl.stats[QStatsDef.STAT_ACTIVEWEAPON] = i2;
					Host.Hud.Changed();
				}
			}
			else
			{
				if ( cl.stats[QStatsDef.STAT_ACTIVEWEAPON] != ( 1 << i2 ) )
				{
					cl.stats[QStatsDef.STAT_ACTIVEWEAPON] = ( 1 << i2 );
					Host.Hud.Changed();
				}
			}
		}

		/// <summary>
		/// CL_ParseServerInfo
		/// </summary>
		private void ParseServerInfo( )
		{
			Host.Console.DPrint( "Serverinfo packet received.\n" );

			//
			// wipe the client_state_t struct
			//
			ClearState();

			// parse protocol version number
			var i = Host.Network.Reader.ReadLong();
			if ( i != ProtocolDef.PROTOCOL_VERSION )
			{
				Host.Console.Print( "Server returned version {0}, not {1}", i, ProtocolDef.PROTOCOL_VERSION );
				return;
			}

			// parse maxclients
			cl.maxclients = Host.Network.Reader.ReadByte();
			if ( cl.maxclients < 1 || cl.maxclients > QDef.MAX_SCOREBOARD )
			{
				Host.Console.Print( "Bad maxclients ({0}) from server\n", cl.maxclients );
				return;
			}
			cl.scores = new scoreboard_t[cl.maxclients];// Hunk_AllocName (cl.maxclients*sizeof(*cl.scores), "scores");
			for ( i = 0; i < cl.scores.Length; i++ )
				cl.scores[i] = new scoreboard_t();

			// parse gametype
			cl.gametype = Host.Network.Reader.ReadByte();

			// parse signon message
			var str = Host.Network.Reader.ReadString();
			cl.levelname = Utilities.Copy( str, 40 );

			// seperate the printfs so the server message can have a color
			Host.Console.Print( ConsoleBar );
			Host.Console.Print( "{0}{1}\n", ( Char ) 2, str );

			//
			// first we go through and touch all of the precache data that still
			// happens to be in the cache, so precaching something else doesn't
			// needlessly purge it
			//

			// precache models
			Array.Clear( cl.model_precache, 0, cl.model_precache.Length );
			Int32 nummodels;
			var model_precache = new String[QDef.MAX_MODELS];
			for ( nummodels = 1; ; nummodels++ )
			{
				str = Host.Network.Reader.ReadString();
				if ( String.IsNullOrEmpty( str ) )
					break;

				if ( nummodels == QDef.MAX_MODELS )
				{
					Host.Console.Print( "Server sent too many model precaches\n" );
					return;
				}
				model_precache[nummodels] = str;
				Host.Model.TouchModel( str );
			}

			// precache sounds
			Array.Clear( cl.sound_precache, 0, cl.sound_precache.Length );
			Int32 numsounds;
			var sound_precache = new String[QDef.MAX_SOUNDS];
			for ( numsounds = 1; ; numsounds++ )
			{
				str = Host.Network.Reader.ReadString();
				if ( String.IsNullOrEmpty( str ) )
					break;
				if ( numsounds == QDef.MAX_SOUNDS )
				{
					Host.Console.Print( "Server sent too many sound precaches\n" );
					return;
				}
				sound_precache[numsounds] = str;
				Host.Sound.TouchSound( str );
			}

			//
			// now we try to load everything else until a cache allocation fails
			//
			for ( i = 1; i < nummodels; i++ )
			{
				var name = model_precache[i];
				var n = name.ToLower();
				var type = ModelType.Sprite;

				if ( n.StartsWith( "*" ) && !n.Contains( ".mdl" ) || n.Contains( ".bsp" ) )
					type = ModelType.Brush;
				else if ( n.Contains( ".mdl" ) )
					type = ModelType.Alias;
				else
					type = ModelType.Sprite;

				if ( name == "progs/player.mdl" )
				{

				}
				cl.model_precache[i] = Host.Model.ForName( name, false, type );
				if ( cl.model_precache[i] == null )
				{
					Host.Console.Print( "Model {0} not found\n", name );
					return;
				}
				KeepaliveMessage();
			}

			Host.Sound.BeginPrecaching();
			for ( i = 1; i < numsounds; i++ )
			{
				cl.sound_precache[i] = Host.Sound.PrecacheSound( sound_precache[i] );
				KeepaliveMessage();
			}
			Host.Sound.EndPrecaching();

			// local state
			cl.worldmodel = ( BrushModelData ) cl.model_precache[1];
			_Entities[0].model = cl.model_precache[1];

			Host.RenderContext.NewMap();

			Host.NoClipAngleHack = false; // noclip is turned off at start

			GC.Collect();
		}

		// CL_ParseStartSoundPacket
		private void ParseStartSoundPacket( )
		{
			var field_mask = Host.Network.Reader.ReadByte();
			Int32 volume;
			Single attenuation;

			if ( ( field_mask & ProtocolDef.SND_VOLUME ) != 0 )
				volume = Host.Network.Reader.ReadByte();
			else
				volume = snd.DEFAULT_SOUND_PACKET_VOLUME;

			if ( ( field_mask & ProtocolDef.SND_ATTENUATION ) != 0 )
				attenuation = Host.Network.Reader.ReadByte() / 64.0f;
			else
				attenuation = snd.DEFAULT_SOUND_PACKET_ATTENUATION;

			var channel = Host.Network.Reader.ReadShort();
			var sound_num = Host.Network.Reader.ReadByte();

			var ent = channel >> 3;
			channel &= 7;

			if ( ent > QDef.MAX_EDICTS )
				Host.Error( "CL_ParseStartSoundPacket: ent = {0}", ent );

			var pos = Host.Network.Reader.ReadCoords();
			Host.Sound.StartSound( ent, channel, cl.sound_precache[sound_num], ref pos, volume / 255.0f, attenuation );
		}

		// CL_NewTranslation
		private void NewTranslation( Int32 slot )
		{
			if ( slot > cl.maxclients )
				Utilities.Error( "CL_NewTranslation: slot > cl.maxclients" );

			var dest = cl.scores[slot].translations;
			var source = Host.Screen.vid.colormap;
			Array.Copy( source, dest, dest.Length );

			var top = cl.scores[slot].colors & 0xf0;
			var bottom = ( cl.scores[slot].colors & 15 ) << 4;

			Host.RenderContext.TranslatePlayerSkin( slot );

			for ( Int32 i = 0, offset = 0; i < Vid.VID_GRADES; i++ )//, dest += 256, source+=256)
			{
				if ( top < 128 )    // the artists made some backwards ranges.  sigh.
					Buffer.BlockCopy( source, offset + top, dest, offset + render.TOP_RANGE, 16 );  //memcpy (dest + Render.TOP_RANGE, source + top, 16);
				else
					for ( var j = 0; j < 16; j++ )
						dest[offset + render.TOP_RANGE + j] = source[offset + top + 15 - j];

				if ( bottom < 128 )
					Buffer.BlockCopy( source, offset + bottom, dest, offset + render.BOTTOM_RANGE, 16 ); // memcpy(dest + Render.BOTTOM_RANGE, source + bottom, 16);
				else
					for ( var j = 0; j < 16; j++ )
						dest[offset + render.BOTTOM_RANGE + j] = source[offset + bottom + 15 - j];

				offset += 256;
			}
		}

		/// <summary>
		/// CL_EntityNum
		///
		/// This error checks and tracks the total number of entities
		/// </summary>
		/// <param name="num"></param>
		/// <returns></returns>
		private Entity EntityNum( Int32 num )
		{
			if ( num >= cl.num_entities )
			{
				if ( num >= QDef.MAX_EDICTS )
					Host.Error( "CL_EntityNum: %i is an invalid number", num );
				while ( cl.num_entities <= num )
				{
					_Entities[cl.num_entities].colormap = Host.Screen.vid.colormap;
					cl.num_entities++;
				}
			}

			return _Entities[num];
		}

		/// <summary>
		/// CL_ParseBaseline
		/// </summary>
		/// <param name="ent"></param>
		private void ParseBaseline( Entity ent )
		{
			ent.baseline.modelindex = Host.Network.Reader.ReadByte();
			ent.baseline.frame = Host.Network.Reader.ReadByte();
			ent.baseline.colormap = Host.Network.Reader.ReadByte();
			ent.baseline.skin = Host.Network.Reader.ReadByte();
			ent.baseline.origin.x = Host.Network.Reader.ReadCoord();
			ent.baseline.angles.x = Host.Network.Reader.ReadAngle();
			ent.baseline.origin.y = Host.Network.Reader.ReadCoord();
			ent.baseline.angles.y = Host.Network.Reader.ReadAngle();
			ent.baseline.origin.z = Host.Network.Reader.ReadCoord();
			ent.baseline.angles.z = Host.Network.Reader.ReadAngle();
		}

		/// <summary>
		/// CL_ParseStatic
		/// </summary>
		private void ParseStatic( )
		{
			var i = cl.num_statics;
			if ( i >= ClientDef.MAX_STATIC_ENTITIES )
				Host.Error( "Too many static entities" );

			var ent = _StaticEntities[i];
			cl.num_statics++;
			ParseBaseline( ent );

			// copy it to the current state
			ent.model = cl.model_precache[ent.baseline.modelindex];
			ent.frame = ent.baseline.frame;
			ent.colormap = Host.Screen.vid.colormap;
			ent.skinnum = ent.baseline.skin;
			ent.effects = ent.baseline.effects;
			ent.origin = Utilities.ToVector( ref ent.baseline.origin );
			ent.angles = Utilities.ToVector( ref ent.baseline.angles );
			Host.RenderContext.AddEfrags( ent );
		}

		/// <summary>
		/// CL_ParseStaticSound
		/// </summary>
		private void ParseStaticSound( )
		{
			var org = Host.Network.Reader.ReadCoords();
			var sound_num = Host.Network.Reader.ReadByte();
			var vol = Host.Network.Reader.ReadByte();
			var atten = Host.Network.Reader.ReadByte();

			Host.Sound.StaticSound( cl.sound_precache[sound_num], ref org, vol, atten );
		}

		/// <summary>
		/// CL_KeepaliveMessage
		/// When the client is taking a long time to load stuff, send keepalive messages
		/// so the server doesn't disconnect.
		/// </summary>
		private void KeepaliveMessage( )
		{
			if ( Host.Server.IsActive )
				return; // no need if server is local
			if ( cls.demoplayback )
				return;

			// read messages from server, should just be nops
			Host.Network.Message.SaveState( ref _MsgState );

			Int32 ret;
			do
			{
				ret = GetMessage();
				switch ( ret )
				{
					default:
						Host.Error( "CL_KeepaliveMessage: CL_GetMessage failed" );
						break;

					case 0:
						break;  // nothing waiting

					case 1:
						Host.Error( "CL_KeepaliveMessage: received a message" );
						break;

					case 2:
						if ( Host.Network.Reader.ReadByte() != ProtocolDef.svc_nop )
							Host.Error( "CL_KeepaliveMessage: datagram wasn't a nop" );
						break;
				}
			} while ( ret != 0 );

			Host.Network.Message.RestoreState( _MsgState );

			// check time
			var time = ( Single ) Timer.GetFloatTime();
			if ( time - _LastMsg < 5 )
				return;

			_LastMsg = time;

			// write out a nop
			Host.Console.Print( "--> client to server keepalive\n" );

			cls.message.WriteByte( ProtocolDef.clc_nop );
			Host.Network.SendMessage( cls.netcon, cls.message );
			cls.message.Clear();
		}
	}
}
