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
    internal static class protocol
    {
        public const int PROTOCOL_VERSION = 15;

        // if the high bit of the servercmd is set, the low bits are fast update flags:
        public const int U_MOREBITS = (1 << 0);

        public const int U_ORIGIN1 = (1 << 1);
        public const int U_ORIGIN2 = (1 << 2);
        public const int U_ORIGIN3 = (1 << 3);
        public const int U_ANGLE2 = (1 << 4);
        public const int U_NOLERP = (1 << 5);	// don't interpolate movement
        public const int U_FRAME = (1 << 6);
        public const int U_SIGNAL = (1 << 7);		// just differentiates from other updates

        // svc_update can pass all of the fast update bits, plus more
        public const int U_ANGLE1 = (1 << 8);

        public const int U_ANGLE3 = (1 << 9);
        public const int U_MODEL = (1 << 10);
        public const int U_COLORMAP = (1 << 11);
        public const int U_SKIN = (1 << 12);
        public const int U_EFFECTS = (1 << 13);
        public const int U_LONGENTITY = (1 << 14);

        public const int SU_VIEWHEIGHT = (1 << 0);
        public const int SU_IDEALPITCH = (1 << 1);
        public const int SU_PUNCH1 = (1 << 2);
        public const int SU_PUNCH2 = (1 << 3);
        public const int SU_PUNCH3 = (1 << 4);
        public const int SU_VELOCITY1 = (1 << 5);
        public const int SU_VELOCITY2 = (1 << 6);
        public const int SU_VELOCITY3 = (1 << 7);

        //define	SU_AIMENT		(1<<8)  AVAILABLE BIT
        public const int SU_ITEMS = (1 << 9);

        public const int SU_ONGROUND = (1 << 10);		// no data follows, the bit is it
        public const int SU_INWATER = (1 << 11);		// no data follows, the bit is it
        public const int SU_WEAPONFRAME = (1 << 12);
        public const int SU_ARMOR = (1 << 13);
        public const int SU_WEAPON = (1 << 14);

        // a sound with no channel is a local only sound
        public const int SND_VOLUME = (1 << 0);		// a byte

        public const int SND_ATTENUATION = (1 << 1);		// a byte
        public const int SND_LOOPING = (1 << 2);		// a long

        // defaults for clientinfo messages
        public const int DEFAULT_VIEWHEIGHT = 22;

        // game types sent by serverinfo
        // these determine which intermission screen plays
        public const int GAME_COOP = 0;

        public const int GAME_DEATHMATCH = 1;

        //==================
        // note that there are some defs.qc that mirror to these numbers
        // also related to svc_strings[] in cl_parse
        //==================

        //
        // server to client
        //
        public const int svc_bad = 0;

        public const int svc_nop = 1;
        public const int svc_disconnect = 2;
        public const int svc_updatestat = 3;	// [byte] [long]
        public const int svc_version = 4;	// [long] server version
        public const int svc_setview = 5;	// [short] entity number
        public const int svc_sound = 6;	// <see code>
        public const int svc_time = 7;	// [float] server time
        public const int svc_print = 8;	// [string] null terminated string
        public const int svc_stufftext = 9;	// [string] stuffed into client's console buffer

        // the string should be \n terminated
        public const int svc_setangle = 10;	// [angle3] set the view angle to this absolute value

        public const int svc_serverinfo = 11;	// [long] version

        // [string] signon string
        // [string]..[0]model cache
        // [string]...[0]sounds cache
        public const int svc_lightstyle = 12;	// [byte] [string]

        public const int svc_updatename = 13;	// [byte] [string]
        public const int svc_updatefrags = 14;	// [byte] [short]
        public const int svc_clientdata = 15;	// <shortbits + data>
        public const int svc_stopsound = 16;	// <see code>
        public const int svc_updatecolors = 17;	// [byte] [byte]
        public const int svc_particle = 18;	// [vec3] <variable>
        public const int svc_damage = 19;

        public const int svc_spawnstatic = 20;

        //	svc_spawnbinary		21
        public const int svc_spawnbaseline = 22;

        public const int svc_temp_entity = 23;

        public const int svc_setpause = 24;	// [byte] on / off
        public const int svc_signonnum = 25;	// [byte]  used for the signon sequence

        public const int svc_centerprint = 26;	// [string] to put in center of the screen

        public const int svc_killedmonster = 27;
        public const int svc_foundsecret = 28;

        public const int svc_spawnstaticsound = 29;	// [coord3] [byte] samp [byte] vol [byte] aten

        public const int svc_intermission = 30;		// [string] music
        public const int svc_finale = 31;		// [string] music [string] text

        public const int svc_cdtrack = 32;		// [byte] track [byte] looptrack
        public const int svc_sellscreen = 33;

        public const int svc_cutscene = 34;

        //
        // client to server
        //
        public const int clc_bad = 0;

        public const int clc_nop = 1;
        public const int clc_disconnect = 2;
        public const int clc_move = 3;			// [usercmd_t]
        public const int clc_stringcmd = 4;		// [string] message

        //
        // temp entity events
        //
        public const int TE_SPIKE = 0;

        public const int TE_SUPERSPIKE = 1;
        public const int TE_GUNSHOT = 2;
        public const int TE_EXPLOSION = 3;
        public const int TE_TAREXPLOSION = 4;
        public const int TE_LIGHTNING1 = 5;
        public const int TE_LIGHTNING2 = 6;
        public const int TE_WIZSPIKE = 7;
        public const int TE_KNIGHTSPIKE = 8;
        public const int TE_LIGHTNING3 = 9;
        public const int TE_LAVASPLASH = 10;
        public const int TE_TELEPORT = 11;
        public const int TE_EXPLOSION2 = 12;

        // PGM 01/21/97
        public const int TE_BEAM = 13;

        // PGM 01/21/97

#if QUAKE2
        public const int TE_IMPLOSION	=	14;
        public const int TE_RAILTRAIL	=	15;
#endif
    }
}
