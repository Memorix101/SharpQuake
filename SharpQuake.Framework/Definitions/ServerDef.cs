﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpQuake.Framework
{
    public static class ServerDef
    {
        public const Int32 NUM_PING_TIMES = 16;
        public const Int32 NUM_SPAWN_PARMS = 16;
    }


    // edict->movetype values
    public static class Movetypes
    {
        public const Int32 MOVETYPE_NONE = 0;		// never moves
        public const Int32 MOVETYPE_ANGLENOCLIP = 1;
        public const Int32 MOVETYPE_ANGLECLIP = 2;
        public const Int32 MOVETYPE_WALK = 3;		// gravity
        public const Int32 MOVETYPE_STEP = 4;		// gravity, special edge handling
        public const Int32 MOVETYPE_FLY = 5;
        public const Int32 MOVETYPE_TOSS = 6;		// gravity
        public const Int32 MOVETYPE_PUSH = 7;		// no clip to world, push and crush
        public const Int32 MOVETYPE_NOCLIP = 8;
        public const Int32 MOVETYPE_FLYMISSILE = 9;		// extra size to monsters
        public const Int32 MOVETYPE_BOUNCE = 10;
#if QUAKE2
        public const int MOVETYPE_BOUNCEMISSILE = 11;		// bounce w/o gravity
        public const int MOVETYPE_FOLLOW = 12;		// track movement of aiment
#endif
    }

    // edict->solid values
    public static class Solids
    {
        public const Int32 SOLID_NOT = 0;		// no interaction with other objects
        public const Int32 SOLID_TRIGGER = 1;		// touch on edge, but not blocking
        public const Int32 SOLID_BBOX = 2;		// touch on edge, block
        public const Int32 SOLID_SLIDEBOX = 3;		// touch on edge, but not an onground
        public const Int32 SOLID_BSP = 4;		// bsp clip, touch on edge, block
    }

    // edict->deadflag values
    public static class DeadFlags
    {
        public const Int32 DEAD_NO = 0;
        public const Int32 DEAD_DYING = 1;
        public const Int32 DEAD_DEAD = 2;
    }

    public static class Damages
    {
        public const Int32 DAMAGE_NO = 0;
        public const Int32 DAMAGE_YES = 1;
        public const Int32 DAMAGE_AIM = 2;
    }

    // edict->flags
    public static class EdictFlags
    {
        public const Int32 FL_FLY = 1;
        public const Int32 FL_SWIM = 2;

        //public const int FL_GLIMPSE	=			4;
        public const Int32 FL_CONVEYOR = 4;

        public const Int32 FL_CLIENT = 8;
        public const Int32 FL_INWATER = 16;
        public const Int32 FL_MONSTER = 32;
        public const Int32 FL_GODMODE = 64;
        public const Int32 FL_NOTARGET = 128;
        public const Int32 FL_ITEM = 256;
        public const Int32 FL_ONGROUND = 512;
        public const Int32 FL_PARTIALGROUND = 1024;	// not all corners are valid
        public const Int32 FL_WATERJUMP = 2048;	// player jumping out of water
        public const Int32 FL_JUMPRELEASED = 4096;    // for jump debouncing
#if QUAKE2
        public const int FL_FLASHLIGHT = 8192;
        public const int FL_ARCHIVE_OVERRIDE = 1048576;
#endif
    }

    public static class SpawnFlags
    {
        public const Int32 SPAWNFLAG_NOT_EASY = 256;
        public const Int32 SPAWNFLAG_NOT_MEDIUM = 512;
        public const Int32 SPAWNFLAG_NOT_HARD = 1024;
        public const Int32 SPAWNFLAG_NOT_DEATHMATCH = 2048;
    }
}