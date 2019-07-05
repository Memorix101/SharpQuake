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

// protocol.h -- communications protocols

namespace SharpQuake
{
    public static class ProtocolDef
    {
        public const System.Int32 PROTOCOL_VERSION = 15;

        // if the high bit of the servercmd is set, the low bits are fast update flags:
        public const System.Int32 U_MOREBITS = (1 << 0);

        public const System.Int32 U_ORIGIN1 = (1 << 1);
        public const System.Int32 U_ORIGIN2 = (1 << 2);
        public const System.Int32 U_ORIGIN3 = (1 << 3);
        public const System.Int32 U_ANGLE2 = (1 << 4);
        public const System.Int32 U_NOLERP = (1 << 5);	// don't interpolate movement
        public const System.Int32 U_FRAME = (1 << 6);
        public const System.Int32 U_SIGNAL = (1 << 7);		// just differentiates from other updates

        // svc_update can pass all of the fast update bits, plus more
        public const System.Int32 U_ANGLE1 = (1 << 8);

        public const System.Int32 U_ANGLE3 = (1 << 9);
        public const System.Int32 U_MODEL = (1 << 10);
        public const System.Int32 U_COLORMAP = (1 << 11);
        public const System.Int32 U_SKIN = (1 << 12);
        public const System.Int32 U_EFFECTS = (1 << 13);
        public const System.Int32 U_LONGENTITY = (1 << 14);

        public const System.Int32 SU_VIEWHEIGHT = (1 << 0);
        public const System.Int32 SU_IDEALPITCH = (1 << 1);
        public const System.Int32 SU_PUNCH1 = (1 << 2);
        public const System.Int32 SU_PUNCH2 = (1 << 3);
        public const System.Int32 SU_PUNCH3 = (1 << 4);
        public const System.Int32 SU_VELOCITY1 = (1 << 5);
        public const System.Int32 SU_VELOCITY2 = (1 << 6);
        public const System.Int32 SU_VELOCITY3 = (1 << 7);

        //define	SU_AIMENT		(1<<8)  AVAILABLE BIT
        public const System.Int32 SU_ITEMS = (1 << 9);

        public const System.Int32 SU_ONGROUND = (1 << 10);		// no data follows, the bit is it
        public const System.Int32 SU_INWATER = (1 << 11);		// no data follows, the bit is it
        public const System.Int32 SU_WEAPONFRAME = (1 << 12);
        public const System.Int32 SU_ARMOR = (1 << 13);
        public const System.Int32 SU_WEAPON = (1 << 14);

        // a sound with no channel is a local only sound
        public const System.Int32 SND_VOLUME = (1 << 0);		// a byte

        public const System.Int32 SND_ATTENUATION = (1 << 1);		// a byte
        public const System.Int32 SND_LOOPING = (1 << 2);		// a long

        // defaults for clientinfo messages
        public const System.Int32 DEFAULT_VIEWHEIGHT = 22;

        // game types sent by serverinfo
        // these determine which intermission screen plays
        public const System.Int32 GAME_COOP = 0;

        public const System.Int32 GAME_DEATHMATCH = 1;

        //==================
        // note that there are some defs.qc that mirror to these numbers
        // also related to svc_strings[] in cl_parse
        //==================

        //
        // server to client
        //
        public const System.Int32 svc_bad = 0;

        public const System.Int32 svc_nop = 1;
        public const System.Int32 svc_disconnect = 2;
        public const System.Int32 svc_updatestat = 3;	// [byte] [long]
        public const System.Int32 svc_version = 4;	// [long] server version
        public const System.Int32 svc_setview = 5;	// [short] entity number
        public const System.Int32 svc_sound = 6;	// <see code>
        public const System.Int32 svc_time = 7;	// [float] server time
        public const System.Int32 svc_print = 8;	// [string] null terminated string
        public const System.Int32 svc_stufftext = 9;	// [string] stuffed into client's console buffer

        // the string should be \n terminated
        public const System.Int32 svc_setangle = 10;	// [angle3] set the view angle to this absolute value

        public const System.Int32 svc_serverinfo = 11;	// [long] version

        // [string] signon string
        // [string]..[0]model cache
        // [string]...[0]sounds cache
        public const System.Int32 svc_lightstyle = 12;	// [byte] [string]

        public const System.Int32 svc_updatename = 13;	// [byte] [string]
        public const System.Int32 svc_updatefrags = 14;	// [byte] [short]
        public const System.Int32 svc_clientdata = 15;	// <shortbits + data>
        public const System.Int32 svc_stopsound = 16;	// <see code>
        public const System.Int32 svc_updatecolors = 17;	// [byte] [byte]
        public const System.Int32 svc_particle = 18;	// [vec3] <variable>
        public const System.Int32 svc_damage = 19;

        public const System.Int32 svc_spawnstatic = 20;

        //	svc_spawnbinary		21
        public const System.Int32 svc_spawnbaseline = 22;

        public const System.Int32 svc_temp_entity = 23;

        public const System.Int32 svc_setpause = 24;	// [byte] on / off
        public const System.Int32 svc_signonnum = 25;	// [byte]  used for the signon sequence

        public const System.Int32 svc_centerprint = 26;	// [string] to put in center of the screen

        public const System.Int32 svc_killedmonster = 27;
        public const System.Int32 svc_foundsecret = 28;

        public const System.Int32 svc_spawnstaticsound = 29;	// [coord3] [byte] samp [byte] vol [byte] aten

        public const System.Int32 svc_intermission = 30;		// [string] music
        public const System.Int32 svc_finale = 31;		// [string] music [string] text

        public const System.Int32 svc_cdtrack = 32;		// [byte] track [byte] looptrack
        public const System.Int32 svc_sellscreen = 33;

        public const System.Int32 svc_cutscene = 34;

        //
        // client to server
        //
        public const System.Int32 clc_bad = 0;

        public const System.Int32 clc_nop = 1;
        public const System.Int32 clc_disconnect = 2;
        public const System.Int32 clc_move = 3;			// [usercmd_t]
        public const System.Int32 clc_stringcmd = 4;		// [string] message

        //
        // temp entity events
        //
        public const System.Int32 TE_SPIKE = 0;

        public const System.Int32 TE_SUPERSPIKE = 1;
        public const System.Int32 TE_GUNSHOT = 2;
        public const System.Int32 TE_EXPLOSION = 3;
        public const System.Int32 TE_TAREXPLOSION = 4;
        public const System.Int32 TE_LIGHTNING1 = 5;
        public const System.Int32 TE_LIGHTNING2 = 6;
        public const System.Int32 TE_WIZSPIKE = 7;
        public const System.Int32 TE_KNIGHTSPIKE = 8;
        public const System.Int32 TE_LIGHTNING3 = 9;
        public const System.Int32 TE_LAVASPLASH = 10;
        public const System.Int32 TE_TELEPORT = 11;
        public const System.Int32 TE_EXPLOSION2 = 12;

        // PGM 01/21/97
        public const System.Int32 TE_BEAM = 13;

        // PGM 01/21/97

#if QUAKE2
        public const int TE_IMPLOSION	=	14;
        public const int TE_RAILTRAIL	=	15;
#endif
    }
}
