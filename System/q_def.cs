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

//define	PARANOID			// speed sapping error checking

using System;
using System.Security.Cryptography.X509Certificates;

// quakedef.h

namespace SharpQuake
{
    internal struct entity_state_t
    {
        public static readonly entity_state_t Empty = new entity_state_t();
        public v3f origin;
        public v3f angles;
        public Int32 modelindex;
        public Int32 frame;
        public Int32 colormap;
        public Int32 skin;
        public Int32 effects;
    }

    public static class QStats
    {
        //
        // stats are integers communicated to the client by the server
        //
        public static Int32 MAX_CL_STATS = 32;

        public static Int32 STAT_HEALTH = 0;
        public static Int32 STAT_FRAGS = 1;
        public static Int32 STAT_WEAPON = 2;
        public static Int32 STAT_AMMO = 3;
        public static Int32 STAT_ARMOR = 4;
        public static Int32 STAT_WEAPONFRAME = 5;
        public static Int32 STAT_SHELLS = 6;
        public static Int32 STAT_NAILS = 7;
        public static Int32 STAT_ROCKETS = 8;
        public static Int32 STAT_CELLS = 9;
        public static Int32 STAT_ACTIVEWEAPON = 10;
        public static Int32 STAT_TOTALSECRETS = 11;
        public static Int32 STAT_TOTALMONSTERS = 12;
        public static Int32 STAT_SECRETS = 13;		// bumped on client side by svc_foundsecret
        public static Int32 STAT_MONSTERS = 14;		// bumped by svc_killedmonster
    }

    public static class QItems
    {
        // stock defines

        public static Int32 IT_SHOTGUN = 1;
        public static Int32 IT_SUPER_SHOTGUN = 2;
        public static Int32 IT_NAILGUN = 4;
        public static Int32 IT_SUPER_NAILGUN = 8;
        public static Int32 IT_GRENADE_LAUNCHER = 16;
        public static Int32 IT_ROCKET_LAUNCHER = 32;
        public static Int32 IT_LIGHTNING = 64;
        public static Int32 IT_SUPER_LIGHTNING = 128;
        public static Int32 IT_SHELLS = 256;
        public static Int32 IT_NAILS = 512;
        public static Int32 IT_ROCKETS = 1024;
        public static Int32 IT_CELLS = 2048;
        public static Int32 IT_AXE = 4096;
        public static Int32 IT_ARMOR1 = 8192;
        public static Int32 IT_ARMOR2 = 16384;
        public static Int32 IT_ARMOR3 = 32768;
        public static Int32 IT_SUPERHEALTH = 65536;
        public static Int32 IT_KEY1 = 131072;
        public static Int32 IT_KEY2 = 262144;
        public static Int32 IT_INVISIBILITY = 524288;
        public static Int32 IT_INVULNERABILITY = 1048576;
        public static Int32 IT_SUIT = 2097152;
        public static Int32 IT_QUAD = 4194304;
        public static Int32 IT_SIGIL1 = (1 << 28);
        public static Int32 IT_SIGIL2 = (1 << 29);
        public static Int32 IT_SIGIL3 = (1 << 30);
        public static Int32 IT_SIGIL4 = (1 << 31);

        //===========================================
        //rogue changed and added defines

        public static Int32 RIT_SHELLS = 128;
        public static Int32 RIT_NAILS = 256;
        public static Int32 RIT_ROCKETS = 512;
        public static Int32 RIT_CELLS = 1024;
        public static Int32 RIT_AXE = 2048;
        public static Int32 RIT_LAVA_NAILGUN = 4096;
        public static Int32 RIT_LAVA_SUPER_NAILGUN = 8192;
        public static Int32 RIT_MULTI_GRENADE = 16384;
        public static Int32 RIT_MULTI_ROCKET = 32768;
        public static Int32 RIT_PLASMA_GUN = 65536;
        public static Int32 RIT_ARMOR1 = 8388608;
        public static Int32 RIT_ARMOR2 = 16777216;
        public static Int32 RIT_ARMOR3 = 33554432;
        public static Int32 RIT_LAVA_NAILS = 67108864;
        public static Int32 RIT_PLASMA_AMMO = 134217728;
        public static Int32 RIT_MULTI_ROCKETS = 268435456;
        public static Int32 RIT_SHIELD = 536870912;
        public static Int32 RIT_ANTIGRAV = 1073741824;
        public static Int32 RIT_SUPERHEALTH = -2147483648;// 2147483648;

        //MED 01/04/97 added hipnotic defines
        //===========================================
        //hipnotic added defines
        public static Int32 HIT_PROXIMITY_GUN_BIT = 16;

        public static Int32 HIT_MJOLNIR_BIT = 7;
        public static Int32 HIT_LASER_CANNON_BIT = 23;
        public static Int32 HIT_PROXIMITY_GUN = (1 << HIT_PROXIMITY_GUN_BIT);
        public static Int32 HIT_MJOLNIR = (1 << HIT_MJOLNIR_BIT);
        public static Int32 HIT_LASER_CANNON = (1 << HIT_LASER_CANNON_BIT);
        public static Int32 HIT_WETSUIT = (1 << (23 + 2));
        public static Int32 HIT_EMPATHY_SHIELDS = (1 << (23 + 3));
        //===========================================
    }

    internal static class QDef
    {
        public const Single VERSION = 1.09f;
        public const Single CSQUAKE_VERSION = 1.20f;
        public const Single GLQUAKE_VERSION = 1.00f;
        public const Single D3DQUAKE_VERSION = 0.01f;
        public const Single WINQUAKE_VERSION = 0.996f;
        public const Single LINUX_VERSION = 1.30f;
        public const Single X11_VERSION = 1.10f;

        public const String GAMENAME = "Id1";		// directory to look in by default

        public const Int32 MAX_NUM_ARGVS = 50;

        // up / down
        public const Int32 PITCH = 0;

        // left / right
        public const Int32 YAW = 1;

        // fall over
        public const Int32 ROLL = 2;

        public const Int32 MAX_QPATH = 64;			// max length of a quake game pathname
        public const Int32 MAX_OSPATH = 128;			// max length of a filesystem pathname

        public const Single ON_EPSILON = 0.1f;		// point on plane side epsilon

        public const Int32 MAX_MSGLEN = 8000;		// max length of a reliable message
        public const Int32 MAX_DATAGRAM = 1024;		// max length of unreliable message

        //
        // per-level limits
        //
        public const Int32 MAX_EDICTS = 600;	//600 	// FIXME: ouch! ouch! ouch!

        public const Int32 MAX_LIGHTSTYLES = 64;
        public const Int32 MAX_MODELS = 256;	//256		// these are sent over the net as bytes
        public const Int32 MAX_SOUNDS = 256;			// so they cannot be blindly increased

        public const Int32 SAVEGAME_COMMENT_LENGTH = 39;

        public const Int32 MAX_STYLESTRING = 64;

        public const Int32 MAX_SCOREBOARD = 16;
        public const Int32 MAX_SCOREBOARDNAME = 32;

        public const Int32 SOUND_CHANNELS = 8;

        public const Double BACKFACE_EPSILON = 0.01;
    }

    internal static class qparam
    {
        public static String globalbasedir;
        public static String globalgameid;
    }

    // entity_state_t;

    // the host system specifies the base of the directory tree, the
    // command line parms passed to the program, and the amount of memory
    // available for the program to use
    internal class quakeparms_t
    {
        public String basedir;
        public String cachedir;		// for development over ISDN lines
        public String[] argv;

        public quakeparms_t()
        {
            this.basedir = String.Empty;
            this.cachedir = String.Empty;
        }
    }// quakeparms_t;
}
