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
using OpenTK;
using SharpQuake.Framework;

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

        private static Int32[] _BitCounts = new Int32[16]; // bitcounts
        private static Object _MsgState; // used by KeepaliveMessage function
        private static Single _LastMsg; // static float lastmsg from CL_KeepaliveMessage

        /// <summary>
        /// CL_ParseServerMessage
        /// </summary>
        private static void ParseServerMessage()
        {
            //
            // if recording demos, copy the message out
            //
            if( _ShowNet.Value == 1 )
                Host.Console.Print( "{0} ", net.Message.Length );
            else if( _ShowNet.Value == 2 )
                Host.Console.Print( "------------------\n" );

            cl.onground = false;	// unless the server says otherwise

            //
            // parse the message
            //
            net.Reader.Reset();
            Int32 i;
            while( true )
            {
                if( net.Reader.IsBadRead )
                    Host.Error( "CL_ParseServerMessage: Bad server message" );

                var cmd = net.Reader.ReadByte();
                if( cmd == -1 )
                {
                    ShowNet( "END OF MESSAGE" );
                    return;	// end of message
                }

                // if the high bit of the command byte is set, it is a fast update
                if( ( cmd & 128 ) != 0 )
                {
                    ShowNet( "fast update" );
                    ParseUpdate( cmd & 127 );
                    continue;
                }

                ShowNet( _SvcStrings[cmd] );

                // other commands
                switch( cmd )
                {
                    default:
                        Host.Error( "CL_ParseServerMessage: Illegible server message\n" );
                        break;

                    case protocol.svc_nop:
                        break;

                    case protocol.svc_time:
                        cl.mtime[1] = cl.mtime[0];
                        cl.mtime[0] = net.Reader.ReadFloat();
                        break;

                    case protocol.svc_clientdata:
                        i = net.Reader.ReadShort();
                        ParseClientData( i );
                        break;

                    case protocol.svc_version:
                        i = net.Reader.ReadLong();
                        if( i != protocol.PROTOCOL_VERSION )
                            Host.Error( "CL_ParseServerMessage: Server is protocol {0} instead of {1}\n", i, protocol.PROTOCOL_VERSION );
                        break;

                    case protocol.svc_disconnect:
                        Host.EndGame( "Server disconnected\n" );
                        break;

                    case protocol.svc_print:
                        Host.Console.Print( net.Reader.ReadString() );
                        break;

                    case protocol.svc_centerprint:
                        Scr.CenterPrint( net.Reader.ReadString() );
                        break;

                    case protocol.svc_stufftext:
                        Host.CommandBuffer.AddText( net.Reader.ReadString() );
                        break;

                    case protocol.svc_damage:
                        Host.View.ParseDamage();
                        break;

                    case protocol.svc_serverinfo:
                        ParseServerInfo();
                        Scr.vid.recalc_refdef = true;	// leave intermission full screen
                        break;

                    case protocol.svc_setangle:
                        cl.viewangles.X = net.Reader.ReadAngle();
                        cl.viewangles.Y = net.Reader.ReadAngle();
                        cl.viewangles.Z = net.Reader.ReadAngle();
                        break;

                    case protocol.svc_setview:
                        cl.viewentity = net.Reader.ReadShort();
                        break;

                    case protocol.svc_lightstyle:
                        i = net.Reader.ReadByte();
                        if( i >= QDef.MAX_LIGHTSTYLES )
                            Utilities.Error( "svc_lightstyle > MAX_LIGHTSTYLES" );
                        _LightStyle[i].map = net.Reader.ReadString();
                        break;

                    case protocol.svc_sound:
                        ParseStartSoundPacket();
                        break;

                    case protocol.svc_stopsound:
                        i = net.Reader.ReadShort();
                        snd.StopSound( i >> 3, i & 7 );
                        break;

                    case protocol.svc_updatename:
                        sbar.Changed();
                        i = net.Reader.ReadByte();
                        if( i >= cl.maxclients )
                            Host.Error( "CL_ParseServerMessage: svc_updatename > MAX_SCOREBOARD" );
                        cl.scores[i].name = net.Reader.ReadString();
                        break;

                    case protocol.svc_updatefrags:
                        sbar.Changed();
                        i = net.Reader.ReadByte();
                        if( i >= cl.maxclients )
                            Host.Error( "CL_ParseServerMessage: svc_updatefrags > MAX_SCOREBOARD" );
                        cl.scores[i].frags = net.Reader.ReadShort();
                        break;

                    case protocol.svc_updatecolors:
                        sbar.Changed();
                        i = net.Reader.ReadByte();
                        if( i >= cl.maxclients )
                            Host.Error( "CL_ParseServerMessage: svc_updatecolors > MAX_SCOREBOARD" );
                        cl.scores[i].colors = net.Reader.ReadByte();
                        NewTranslation( i );
                        break;

                    case protocol.svc_particle:
                        render.ParseParticleEffect();
                        break;

                    case protocol.svc_spawnbaseline:
                        i = net.Reader.ReadShort();
                        // must use CL_EntityNum() to force cl.num_entities up
                        ParseBaseline( EntityNum( i ) );
                        break;

                    case protocol.svc_spawnstatic:
                        ParseStatic();
                        break;

                    case protocol.svc_temp_entity:
                        ParseTempEntity();
                        break;

                    case protocol.svc_setpause:
                    {
                        cl.paused = net.Reader.ReadByte() != 0;

                        if( cl.paused )
                        {
                            cd_audio.Pause();
                        }
                        else
                        {
                            cd_audio.Resume();
                        }
                    }
                    break;

                    case protocol.svc_signonnum:
                        i = net.Reader.ReadByte();
                        if( i <= cls.signon )
                            Host.Error( "Received signon {0} when at {1}", i, cls.signon );
                        cls.signon = i;
                        SignonReply();
                        break;

                    case protocol.svc_killedmonster:
                        cl.stats[QStatsDef.STAT_MONSTERS]++;
                        break;

                    case protocol.svc_foundsecret:
                        cl.stats[QStatsDef.STAT_SECRETS]++;
                        break;

                    case protocol.svc_updatestat:
                        i = net.Reader.ReadByte();
                        if( i < 0 || i >= QStatsDef.MAX_CL_STATS )
                            Utilities.Error( "svc_updatestat: {0} is invalid", i );
                        cl.stats[i] = net.Reader.ReadLong();
                        break;

                    case protocol.svc_spawnstaticsound:
                        ParseStaticSound();
                        break;

                    case protocol.svc_cdtrack:
                        cl.cdtrack = net.Reader.ReadByte();
                        cl.looptrack = net.Reader.ReadByte();
                        if( ( cls.demoplayback || cls.demorecording ) && ( cls.forcetrack != -1 ) )
                            cd_audio.Play( ( Byte ) cls.forcetrack, true );
                        else
                            cd_audio.Play( ( Byte ) cl.cdtrack, true );
                        break;

                    case protocol.svc_intermission:
                        cl.intermission = 1;
                        cl.completed_time = ( Int32 ) cl.time;
                        Scr.vid.recalc_refdef = true;	// go to full screen
                        break;

                    case protocol.svc_finale:
                        cl.intermission = 2;
                        cl.completed_time = ( Int32 ) cl.time;
                        Scr.vid.recalc_refdef = true;	// go to full screen
                        Scr.CenterPrint( net.Reader.ReadString() );
                        break;

                    case protocol.svc_cutscene:
                        cl.intermission = 3;
                        cl.completed_time = ( Int32 ) cl.time;
                        Scr.vid.recalc_refdef = true;	// go to full screen
                        Scr.CenterPrint( net.Reader.ReadString() );
                        break;

                    case protocol.svc_sellscreen:
                        Host.Command.ExecuteString( "help", CommandSource.src_command );
                        break;
                }
            }
        }

        private static void ShowNet( String s )
        {
            if( _ShowNet.Value == 2 )
                Host.Console.Print( "{0,3}:{1}\n", net.Reader.Position - 1, s );
        }

        /// <summary>
        /// CL_ParseUpdate
        ///
        /// Parse an entity update message from the server
        /// If an entities model or origin changes from frame to frame, it must be
        /// relinked.  Other attributes can change without relinking.
        /// </summary>
        private static void ParseUpdate( Int32 bits )
        {
            Int32 i;

            if( cls.signon == SIGNONS - 1 )
            {
                // first update is the final signon stage
                cls.signon = SIGNONS;
                SignonReply();
            }

            if( ( bits & protocol.U_MOREBITS ) != 0 )
            {
                i = net.Reader.ReadByte();
                bits |= ( i << 8 );
            }

            Int32 num;

            if( ( bits & protocol.U_LONGENTITY ) != 0 )
                num = net.Reader.ReadShort();
            else
                num = net.Reader.ReadByte();

            Entity ent = EntityNum( num );
            for( i = 0; i < 16; i++ )
                if( ( bits & ( 1 << i ) ) != 0 )
                    _BitCounts[i]++;

            var forcelink = false;
            if( ent.msgtime != cl.mtime[1] )
                forcelink = true;	// no previous frame to lerp from

            ent.msgtime = cl.mtime[0];
            Int32 modnum;
            if( ( bits & protocol.U_MODEL ) != 0 )
            {
                modnum = net.Reader.ReadByte();
                if( modnum >= QDef.MAX_MODELS )
                    Host.Error( "CL_ParseModel: bad modnum" );
            }
            else
                modnum = ent.baseline.modelindex;

            Model model = cl.model_precache[modnum];
            if( model != ent.model )
            {
                ent.model = model;
                // automatic animation (torches, etc) can be either all together
                // or randomized
                if( model != null )
                {
                    if( model.synctype == SyncType.ST_RAND )
                        ent.syncbase = ( Single ) ( MathLib.Random() & 0x7fff ) / 0x7fff;
                    else
                        ent.syncbase = 0;
                }
                else
                    forcelink = true;	// hack to make null model players work

                if( num > 0 && num <= cl.maxclients )
                    render.TranslatePlayerSkin( num - 1 );
            }

            if( ( bits & protocol.U_FRAME ) != 0 )
                ent.frame = net.Reader.ReadByte();
            else
                ent.frame = ent.baseline.frame;

            if( ( bits & protocol.U_COLORMAP ) != 0 )
                i = net.Reader.ReadByte();
            else
                i = ent.baseline.colormap;
            if( i == 0 )
                ent.colormap = Scr.vid.colormap;
            else
            {
                if( i > cl.maxclients )
                    Utilities.Error( "i >= cl.maxclients" );
                ent.colormap = cl.scores[i - 1].translations;
            }

            Int32 skin;
            if( ( bits & protocol.U_SKIN ) != 0 )
                skin = net.Reader.ReadByte();
            else
                skin = ent.baseline.skin;
            if( skin != ent.skinnum )
            {
                ent.skinnum = skin;
                if( num > 0 && num <= cl.maxclients )
                    render.TranslatePlayerSkin( num - 1 );
            }

            if( ( bits & protocol.U_EFFECTS ) != 0 )
                ent.effects = net.Reader.ReadByte();
            else
                ent.effects = ent.baseline.effects;

            // shift the known values for interpolation
            ent.msg_origins[1] = ent.msg_origins[0];
            ent.msg_angles[1] = ent.msg_angles[0];

            if( ( bits & protocol.U_ORIGIN1 ) != 0 )
                ent.msg_origins[0].X = net.Reader.ReadCoord();
            else
                ent.msg_origins[0].X = ent.baseline.origin.x;
            if( ( bits & protocol.U_ANGLE1 ) != 0 )
                ent.msg_angles[0].X = net.Reader.ReadAngle();
            else
                ent.msg_angles[0].X = ent.baseline.angles.x;

            if( ( bits & protocol.U_ORIGIN2 ) != 0 )
                ent.msg_origins[0].Y = net.Reader.ReadCoord();
            else
                ent.msg_origins[0].Y = ent.baseline.origin.y;
            if( ( bits & protocol.U_ANGLE2 ) != 0 )
                ent.msg_angles[0].Y = net.Reader.ReadAngle();
            else
                ent.msg_angles[0].Y = ent.baseline.angles.y;

            if( ( bits & protocol.U_ORIGIN3 ) != 0 )
                ent.msg_origins[0].Z = net.Reader.ReadCoord();
            else
                ent.msg_origins[0].Z = ent.baseline.origin.z;
            if( ( bits & protocol.U_ANGLE3 ) != 0 )
                ent.msg_angles[0].Z = net.Reader.ReadAngle();
            else
                ent.msg_angles[0].Z = ent.baseline.angles.z;

            if( ( bits & protocol.U_NOLERP ) != 0 )
                ent.forcelink = true;

            if( forcelink )
            {	// didn't have an update last message
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
        private static void ParseClientData( Int32 bits )
        {
            if( ( bits & protocol.SU_VIEWHEIGHT ) != 0 )
                cl.viewheight = net.Reader.ReadChar();
            else
                cl.viewheight = protocol.DEFAULT_VIEWHEIGHT;

            if( ( bits & protocol.SU_IDEALPITCH ) != 0 )
                cl.idealpitch = net.Reader.ReadChar();
            else
                cl.idealpitch = 0;

            cl.mvelocity[1] = cl.mvelocity[0];
            for( var i = 0; i < 3; i++ )
            {
                if( ( bits & ( protocol.SU_PUNCH1 << i ) ) != 0 )
                    MathLib.SetComp( ref cl.punchangle, i, net.Reader.ReadChar() );
                else
                    MathLib.SetComp( ref cl.punchangle, i, 0 );
                if( ( bits & ( protocol.SU_VELOCITY1 << i ) ) != 0 )
                    MathLib.SetComp( ref cl.mvelocity[0], i, net.Reader.ReadChar() * 16 );
                else
                    MathLib.SetComp( ref cl.mvelocity[0], i, 0 );
            }

            // [always sent]	if (bits & SU_ITEMS)
            var i2 = net.Reader.ReadLong();

            if( cl.items != i2 )
            {	// set flash times
                sbar.Changed();
                for( var j = 0; j < 32; j++ )
                    if( ( i2 & ( 1 << j ) ) != 0 && ( cl.items & ( 1 << j ) ) == 0 )
                        cl.item_gettime[j] = ( Single ) cl.time;
                cl.items = i2;
            }

            cl.onground = ( bits & protocol.SU_ONGROUND ) != 0;
            cl.inwater = ( bits & protocol.SU_INWATER ) != 0;

            if( ( bits & protocol.SU_WEAPONFRAME ) != 0 )
                cl.stats[QStatsDef.STAT_WEAPONFRAME] = net.Reader.ReadByte();
            else
                cl.stats[QStatsDef.STAT_WEAPONFRAME] = 0;

            if( ( bits & protocol.SU_ARMOR ) != 0 )
                i2 = net.Reader.ReadByte();
            else
                i2 = 0;
            if( cl.stats[QStatsDef.STAT_ARMOR] != i2 )
            {
                cl.stats[QStatsDef.STAT_ARMOR] = i2;
                sbar.Changed();
            }

            if( ( bits & protocol.SU_WEAPON ) != 0 )
                i2 = net.Reader.ReadByte();
            else
                i2 = 0;
            if( cl.stats[QStatsDef.STAT_WEAPON] != i2 )
            {
                cl.stats[QStatsDef.STAT_WEAPON] = i2;
                sbar.Changed();
            }

            i2 = net.Reader.ReadShort();
            if( cl.stats[QStatsDef.STAT_HEALTH] != i2 )
            {
                cl.stats[QStatsDef.STAT_HEALTH] = i2;
                sbar.Changed();
            }

            i2 = net.Reader.ReadByte();
            if( cl.stats[QStatsDef.STAT_AMMO] != i2 )
            {
                cl.stats[QStatsDef.STAT_AMMO] = i2;
                sbar.Changed();
            }

            for( i2 = 0; i2 < 4; i2++ )
            {
                var j = net.Reader.ReadByte();
                if( cl.stats[QStatsDef.STAT_SHELLS + i2] != j )
                {
                    cl.stats[QStatsDef.STAT_SHELLS + i2] = j;
                    sbar.Changed();
                }
            }

            i2 = net.Reader.ReadByte();

            // Change
            if( MainWindow.Common.GameKind == GameKind.StandardQuake )
            {
                if( cl.stats[QStatsDef.STAT_ACTIVEWEAPON] != i2 )
                {
                    cl.stats[QStatsDef.STAT_ACTIVEWEAPON] = i2;
                    sbar.Changed();
                }
            }
            else
            {
                if( cl.stats[QStatsDef.STAT_ACTIVEWEAPON] != ( 1 << i2 ) )
                {
                    cl.stats[QStatsDef.STAT_ACTIVEWEAPON] = ( 1 << i2 );
                    sbar.Changed();
                }
            }
        }

        /// <summary>
        /// CL_ParseServerInfo
        /// </summary>
        private static void ParseServerInfo()
        {
            Host.Console.DPrint( "Serverinfo packet received.\n" );

            //
            // wipe the client_state_t struct
            //
            ClearState();

            // parse protocol version number
            var i = net.Reader.ReadLong();
            if( i != protocol.PROTOCOL_VERSION )
            {
                Host.Console.Print( "Server returned version {0}, not {1}", i, protocol.PROTOCOL_VERSION );
                return;
            }

            // parse maxclients
            cl.maxclients = net.Reader.ReadByte();
            if( cl.maxclients < 1 || cl.maxclients > QDef.MAX_SCOREBOARD )
            {
                Host.Console.Print( "Bad maxclients ({0}) from server\n", cl.maxclients );
                return;
            }
            cl.scores = new scoreboard_t[cl.maxclients];// Hunk_AllocName (cl.maxclients*sizeof(*cl.scores), "scores");
            for( i = 0; i < cl.scores.Length; i++ )
                cl.scores[i] = new scoreboard_t();

            // parse gametype
            cl.gametype = net.Reader.ReadByte();

            // parse signon message
            var str = net.Reader.ReadString();
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
            String[] model_precache = new String[QDef.MAX_MODELS];
            for( nummodels = 1; ; nummodels++ )
            {
                str = net.Reader.ReadString();
                if( String.IsNullOrEmpty( str ) )
                    break;

                if( nummodels == QDef.MAX_MODELS )
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
            String[] sound_precache = new String[QDef.MAX_SOUNDS];
            for( numsounds = 1; ; numsounds++ )
            {
                str = net.Reader.ReadString();
                if( String.IsNullOrEmpty( str ) )
                    break;
                if( numsounds == QDef.MAX_SOUNDS )
                {
                    Host.Console.Print( "Server sent too many sound precaches\n" );
                    return;
                }
                sound_precache[numsounds] = str;
                snd.TouchSound( str );
            }

            //
            // now we try to load everything else until a cache allocation fails
            //
            for( i = 1; i < nummodels; i++ )
            {
                cl.model_precache[i] = Host.Model.ForName( model_precache[i], false );
                if( cl.model_precache[i] == null )
                {
                    Host.Console.Print( "Model {0} not found\n", model_precache[i] );
                    return;
                }
                KeepaliveMessage();
            }

            snd.BeginPrecaching();
            for( i = 1; i < numsounds; i++ )
            {
                cl.sound_precache[i] = snd.PrecacheSound( sound_precache[i] );
                KeepaliveMessage();
            }
            snd.EndPrecaching();

            // local state
            _Entities[0].model = cl.worldmodel = cl.model_precache[1];

            render.NewMap();

            Host.NoClipAngleHack = false; // noclip is turned off at start

            GC.Collect();
        }

        // CL_ParseStartSoundPacket
        private static void ParseStartSoundPacket()
        {
            var field_mask = net.Reader.ReadByte();
            Int32 volume;
            Single attenuation;

            if( ( field_mask & protocol.SND_VOLUME ) != 0 )
                volume = net.Reader.ReadByte();
            else
                volume = snd.DEFAULT_SOUND_PACKET_VOLUME;

            if( ( field_mask & protocol.SND_ATTENUATION ) != 0 )
                attenuation = net.Reader.ReadByte() / 64.0f;
            else
                attenuation = snd.DEFAULT_SOUND_PACKET_ATTENUATION;

            var channel = net.Reader.ReadShort();
            var sound_num = net.Reader.ReadByte();

            var ent = channel >> 3;
            channel &= 7;

            if( ent > QDef.MAX_EDICTS )
                Host.Error( "CL_ParseStartSoundPacket: ent = {0}", ent );

            Vector3 pos = net.Reader.ReadCoords();
            snd.StartSound( ent, channel, cl.sound_precache[sound_num], ref pos, volume / 255.0f, attenuation );
        }

        // CL_NewTranslation
        private static void NewTranslation( Int32 slot )
        {
            if( slot > cl.maxclients )
                Utilities.Error( "CL_NewTranslation: slot > cl.maxclients" );

            Byte[] dest = cl.scores[slot].translations;
            Byte[] source = Scr.vid.colormap;
            Array.Copy( source, dest, dest.Length );

            var top = cl.scores[slot].colors & 0xf0;
            var bottom = ( cl.scores[slot].colors & 15 ) << 4;

            render.TranslatePlayerSkin( slot );

            for( Int32 i = 0, offset = 0; i < vid.VID_GRADES; i++ )//, dest += 256, source+=256)
            {
                if( top < 128 )	// the artists made some backwards ranges.  sigh.
                    Buffer.BlockCopy( source, offset + top, dest, offset + render.TOP_RANGE, 16 );  //memcpy (dest + Render.TOP_RANGE, source + top, 16);
                else
                    for( var j = 0; j < 16; j++ )
                        dest[offset + render.TOP_RANGE + j] = source[offset + top + 15 - j];

                if( bottom < 128 )
                    Buffer.BlockCopy( source, offset + bottom, dest, offset + render.BOTTOM_RANGE, 16 ); // memcpy(dest + Render.BOTTOM_RANGE, source + bottom, 16);
                else
                    for( var j = 0; j < 16; j++ )
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
        private static Entity EntityNum( Int32 num )
        {
            if( num >= cl.num_entities )
            {
                if( num >= QDef.MAX_EDICTS )
                    Host.Error( "CL_EntityNum: %i is an invalid number", num );
                while( cl.num_entities <= num )
                {
                    _Entities[cl.num_entities].colormap = Scr.vid.colormap;
                    cl.num_entities++;
                }
            }

            return _Entities[num];
        }

        /// <summary>
        /// CL_ParseBaseline
        /// </summary>
        /// <param name="ent"></param>
        private static void ParseBaseline( Entity ent )
        {
            ent.baseline.modelindex = net.Reader.ReadByte();
            ent.baseline.frame = net.Reader.ReadByte();
            ent.baseline.colormap = net.Reader.ReadByte();
            ent.baseline.skin = net.Reader.ReadByte();
            ent.baseline.origin.x = net.Reader.ReadCoord();
            ent.baseline.angles.x = net.Reader.ReadAngle();
            ent.baseline.origin.y = net.Reader.ReadCoord();
            ent.baseline.angles.y = net.Reader.ReadAngle();
            ent.baseline.origin.z = net.Reader.ReadCoord();
            ent.baseline.angles.z = net.Reader.ReadAngle();
        }

        /// <summary>
        /// CL_ParseStatic
        /// </summary>
        private static void ParseStatic()
        {
            var i = cl.num_statics;
            if( i >= MAX_STATIC_ENTITIES )
                Host.Error( "Too many static entities" );

            Entity ent = _StaticEntities[i];
            cl.num_statics++;
            ParseBaseline( ent );

            // copy it to the current state
            ent.model = cl.model_precache[ent.baseline.modelindex];
            ent.frame = ent.baseline.frame;
            ent.colormap = Scr.vid.colormap;
            ent.skinnum = ent.baseline.skin;
            ent.effects = ent.baseline.effects;
            ent.origin = Utilities.ToVector( ref ent.baseline.origin );
            ent.angles = Utilities.ToVector( ref ent.baseline.angles );
            render.AddEfrags( ent );
        }

        /// <summary>
        /// CL_ParseStaticSound
        /// </summary>
        private static void ParseStaticSound()
        {
            Vector3 org = net.Reader.ReadCoords();
            var sound_num = net.Reader.ReadByte();
            var vol = net.Reader.ReadByte();
            var atten = net.Reader.ReadByte();

            snd.StaticSound( cl.sound_precache[sound_num], ref org, vol, atten );
        }

        /// <summary>
        /// CL_KeepaliveMessage
        /// When the client is taking a long time to load stuff, send keepalive messages
        /// so the server doesn't disconnect.
        /// </summary>
        private static void KeepaliveMessage()
        {
            if( server.IsActive )
                return;	// no need if server is local
            if( cls.demoplayback )
                return;

            // read messages from server, should just be nops
            net.Message.SaveState( ref _MsgState );

            Int32 ret;
            do
            {
                ret = GetMessage();
                switch( ret )
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
                        if( net.Reader.ReadByte() != protocol.svc_nop )
                            Host.Error( "CL_KeepaliveMessage: datagram wasn't a nop" );
                        break;
                }
            } while( ret != 0 );

            net.Message.RestoreState( _MsgState );

            // check time
            var time = ( Single ) Timer.GetFloatTime();
            if( time - _LastMsg < 5 )
                return;

            _LastMsg = time;

            // write out a nop
            Host.Console.Print( "--> client to server keepalive\n" );

            cls.message.WriteByte( protocol.clc_nop );
            net.SendMessage( cls.netcon, cls.message );
            cls.message.Clear();
        }
    }
}
