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

// quakedef.h

namespace SharpQuake
{
    internal struct entity_state_t
    {
        public static readonly entity_state_t Empty = new entity_state_t();
        public v3f origin;
        public v3f angles;
        public int modelindex;
        public int frame;
        public int colormap;
        public int skin;
        public int effects;
    }

    public static class QStats
    {
        //
        // stats are integers communicated to the client by the server
        //
        public static int MAX_CL_STATS = 32;

        public static int STAT_HEALTH = 0;
        public static int STAT_FRAGS = 1;
        public static int STAT_WEAPON = 2;
        public static int STAT_AMMO = 3;
        public static int STAT_ARMOR = 4;
        public static int STAT_WEAPONFRAME = 5;
        public static int STAT_SHELLS = 6;
        public static int STAT_NAILS = 7;
        public static int STAT_ROCKETS = 8;
        public static int STAT_CELLS = 9;
        public static int STAT_ACTIVEWEAPON = 10;
        public static int STAT_TOTALSECRETS = 11;
        public static int STAT_TOTALMONSTERS = 12;
        public static int STAT_SECRETS = 13;		// bumped on client side by svc_foundsecret
        public static int STAT_MONSTERS = 14;		// bumped by svc_killedmonster
    }

    public static class QItems
    {
        // stock defines

        public static int IT_SHOTGUN = 1;
        public static int IT_SUPER_SHOTGUN = 2;
        public static int IT_NAILGUN = 4;
        public static int IT_SUPER_NAILGUN = 8;
        public static int IT_GRENADE_LAUNCHER = 16;
        public static int IT_ROCKET_LAUNCHER = 32;
        public static int IT_LIGHTNING = 64;
        public static int IT_SUPER_LIGHTNING = 128;
        public static int IT_SHELLS = 256;
        public static int IT_NAILS = 512;
        public static int IT_ROCKETS = 1024;
        public static int IT_CELLS = 2048;
        public static int IT_AXE = 4096;
        public static int IT_ARMOR1 = 8192;
        public static int IT_ARMOR2 = 16384;
        public static int IT_ARMOR3 = 32768;
        public static int IT_SUPERHEALTH = 65536;
        public static int IT_KEY1 = 131072;
        public static int IT_KEY2 = 262144;
        public static int IT_INVISIBILITY = 524288;
        public static int IT_INVULNERABILITY = 1048576;
        public static int IT_SUIT = 2097152;
        public static int IT_QUAD = 4194304;
        public static int IT_SIGIL1 = (1<<28);
        public static int IT_SIGIL2 = (1<<29);
        public static int IT_SIGIL3 = (1<<30);
        public static int IT_SIGIL4 = (1<<31);

        //===========================================
        //rogue changed and added defines

        public static int RIT_SHELLS = 128;
        public static int RIT_NAILS = 256;
        public static int RIT_ROCKETS = 512;
        public static int RIT_CELLS = 1024;
        public static int RIT_AXE = 2048;
        public static int RIT_LAVA_NAILGUN = 4096;
        public static int RIT_LAVA_SUPER_NAILGUN = 8192;
        public static int RIT_MULTI_GRENADE = 16384;
        public static int RIT_MULTI_ROCKET = 32768;
        public static int RIT_PLASMA_GUN = 65536;
        public static int RIT_ARMOR1 = 8388608;
        public static int RIT_ARMOR2 = 16777216;
        public static int RIT_ARMOR3 = 33554432;
        public static int RIT_LAVA_NAILS = 67108864;
        public static int RIT_PLASMA_AMMO = 134217728;
        public static int RIT_MULTI_ROCKETS = 268435456;
        public static int RIT_SHIELD = 536870912;
        public static int RIT_ANTIGRAV = 1073741824;
        public static int RIT_SUPERHEALTH = -2147483648;// 2147483648;

        //MED 01/04/97 added hipnotic defines
        //===========================================
        //hipnotic added defines
        public static int HIT_PROXIMITY_GUN_BIT = 16;

        public static int HIT_MJOLNIR_BIT = 7;
        public static int HIT_LASER_CANNON_BIT = 23;
        public static int HIT_PROXIMITY_GUN = (1<<HIT_PROXIMITY_GUN_BIT);
        public static int HIT_MJOLNIR = (1<<HIT_MJOLNIR_BIT);
        public static int HIT_LASER_CANNON = (1<<HIT_LASER_CANNON_BIT);
        public static int HIT_WETSUIT = (1<<(23+2));
        public static int HIT_EMPATHY_SHIELDS = (1 << (23 + 3));
        //===========================================
    }

    internal static class QDef
    {
        public const float VERSION  = 1.09f;
        public const float CSQUAKE_VERSION = 1.20f;
        public const float GLQUAKE_VERSION  = 1.00f;
        public const float D3DQUAKE_VERSION = 0.01f;
        public const float WINQUAKE_VERSION = 0.996f;
        public const float LINUX_VERSION = 1.30f;
        public const float X11_VERSION = 1.10f;

        public const string GAMENAME = "Id1";		// directory to look in by default

        public const int MAX_NUM_ARGVS  = 50;

        // up / down
        public const int PITCH  = 0;

        // left / right
        public const int YAW = 1;

        // fall over
        public const int ROLL = 2;

        public const int MAX_QPATH = 64;			// max length of a quake game pathname
        public const int MAX_OSPATH = 128;			// max length of a filesystem pathname

        public const float ON_EPSILON = 0.1f;		// point on plane side epsilon

        public const int MAX_MSGLEN = 8000;		// max length of a reliable message
        public const int MAX_DATAGRAM = 1024;		// max length of unreliable message

        //
        // per-level limits
        //
        public const int MAX_EDICTS = 600;	//600 	// FIXME: ouch! ouch! ouch!

        public const int MAX_LIGHTSTYLES = 64;
        public const int MAX_MODELS = 1024;	//256		// these are sent over the net as bytes
        public const int MAX_SOUNDS = 256;			// so they cannot be blindly increased

        public const int SAVEGAME_COMMENT_LENGTH = 39;

        public const int MAX_STYLESTRING = 64;

        public const int MAX_SCOREBOARD = 16;
        public const int MAX_SCOREBOARDNAME = 32;

        public const int SOUND_CHANNELS = 8;

        public const double BACKFACE_EPSILON = 0.01;
    }

    // entity_state_t;

    // the host system specifies the base of the directory tree, the
    // command line parms passed to the program, and the amount of memory
    // available for the program to use
    internal class quakeparms_t
    {
        public string basedir;
        public string cachedir;		// for development over ISDN lines
        public string[] argv;

        public quakeparms_t()
        {
            this.basedir = String.Empty;
            this.cachedir = String.Empty;
        }
    }// quakeparms_t;
}
