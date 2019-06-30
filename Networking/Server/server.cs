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

namespace SharpQuake
{
    internal enum server_state_t
    {
        Loading, Active
    }

    static partial class server
    {
        public static server_t sv
        {
            get
            {
                return _Server;
            }
        }

        public static server_static_t svs
        {
            get
            {
                return _ServerStatic;
            }
        }

        public static Boolean IsActive
        {
            get
            {
                return _Server.active;
            }
        }

        public static Single Gravity
        {
            get
            {
                return _Gravity.Value;
            }
        }

        public static Boolean IsLoading
        {
            get
            {
                return _Server.state == server_state_t.Loading;
            }
        }

        public static Single Aim
        {
            get
            {
                return _Aim.Value;
            }
        }

        private static CVar _Friction;// = { "sv_friction", "4", false, true };
        private static CVar _EdgeFriction;// = { "edgefriction", "2" };
        private static CVar _StopSpeed;// = { "sv_stopspeed", "100" };
        private static CVar _Gravity;// = { "sv_gravity", "800", false, true };
        private static CVar _MaxVelocity;// = { "sv_maxvelocity", "2000" };
        private static CVar _NoStep;// = { "sv_nostep", "0" };
        private static CVar _MaxSpeed;// = { "sv_maxspeed", "320", false, true };
        private static CVar _Accelerate;// = { "sv_accelerate", "10" };
        private static CVar _Aim;// = { "sv_aim", "0.93" };
        private static CVar _IdealPitchScale;// = { "sv_idealpitchscale", "0.8" };

        private static server_t _Server;
        private static server_static_t _ServerStatic;

        private static String[] _LocalModels = new String[QDef.MAX_MODELS]; //[MAX_MODELS][5];	// inline model names for precache

        /// <summary>
        /// EDICT_NUM
        /// </summary>
        public static MemoryEdict EdictNum( Int32 n )
        {
            if( n < 0 || n >= _Server.max_edicts )
                Utilities.Error( "EDICT_NUM: bad number {0}", n );
            return _Server.edicts[n];
        }

        /// <summary>
        /// ED_Alloc
        /// Either finds a free edict, or allocates a new one.
        /// Try to avoid reusing an entity that was recently freed, because it
        /// can cause the client to think the entity morphed into something else
        /// instead of being removed and recreated, which can cause interpolated
        /// angles and bad trails.
        /// </summary>
        public static MemoryEdict AllocEdict()
        {
            MemoryEdict e;
            Int32 i;
            for( i = svs.maxclients + 1; i < sv.num_edicts; i++ )
            {
                e = EdictNum( i );

                // the first couple seconds of server time can involve a lot of
                // freeing and allocating, so relax the replacement policy
                if( e.free && ( e.freetime < 2 || sv.time - e.freetime > 0.5 ) )
                {
                    e.Clear();
                    return e;
                }
            }

            if( i == QDef.MAX_EDICTS )
                Utilities.Error( "ED_Alloc: no free edicts" );

            sv.num_edicts++;
            e = EdictNum( i );
            e.Clear();

            return e;
        }

        /// <summary>
        /// ED_Free
        /// Marks the edict as free
        /// FIXME: walk all entities and NULL out references to this entity
        /// </summary>
        public static void FreeEdict( MemoryEdict ed )
        {
            UnlinkEdict( ed );		// unlink from world bsp

            ed.free = true;
            ed.v.model = 0;
            ed.v.takedamage = 0;
            ed.v.modelindex = 0;
            ed.v.colormap = 0;
            ed.v.skin = 0;
            ed.v.frame = 0;
            ed.v.origin = default( Vector3f );
            ed.v.angles = default( Vector3f );
            ed.v.nextthink = -1;
            ed.v.solid = 0;

            ed.freetime = ( Single ) sv.time;
        }

        /// <summary>
        /// EDICT_TO_PROG(e)
        /// </summary>
        public static Int32 EdictToProg( MemoryEdict e )
        {
            return Array.IndexOf( _Server.edicts, e ); // todo: optimize this
        }

        /// <summary>
        /// PROG_TO_EDICT(e)
        /// Offset in bytes!
        /// </summary>
        public static MemoryEdict ProgToEdict( Int32 e )
        {
            if( e < 0 || e > sv.edicts.Length )
                Utilities.Error( "ProgToEdict: Bad prog!" );
            return sv.edicts[e];
        }

        /// <summary>
        /// NUM_FOR_EDICT
        /// </summary>
        public static Int32 NumForEdict( MemoryEdict e )
        {
            var i = Array.IndexOf( sv.edicts, e ); // todo: optimize this

            if( i < 0 )
                Utilities.Error( "NUM_FOR_EDICT: bad pointer" );
            return i;
        }

        static server()
        {
            _Server = new server_t();
            _ServerStatic = new server_static_t();
        }
    }

    // edict->movetype values
    internal static class Movetypes
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
    internal static class Solids
    {
        public const Int32 SOLID_NOT = 0;		// no interaction with other objects
        public const Int32 SOLID_TRIGGER = 1;		// touch on edge, but not blocking
        public const Int32 SOLID_BBOX = 2;		// touch on edge, block
        public const Int32 SOLID_SLIDEBOX = 3;		// touch on edge, but not an onground
        public const Int32 SOLID_BSP = 4;		// bsp clip, touch on edge, block
    }

    // edict->deadflag values
    internal static class DeadFlags
    {
        public const Int32 DEAD_NO = 0;
        public const Int32 DEAD_DYING = 1;
        public const Int32 DEAD_DEAD = 2;
    }

    internal static class Damages
    {
        public const Int32 DAMAGE_NO = 0;
        public const Int32 DAMAGE_YES = 1;
        public const Int32 DAMAGE_AIM = 2;
    }

    // edict->flags
    internal static class EdictFlags
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

    internal static class SpawnFlags
    {
        public const Int32 SPAWNFLAG_NOT_EASY = 256;
        public const Int32 SPAWNFLAG_NOT_MEDIUM = 512;
        public const Int32 SPAWNFLAG_NOT_HARD = 1024;
        public const Int32 SPAWNFLAG_NOT_DEATHMATCH = 2048;
    }

    internal class areanode_t
    {
        public Int32 axis;		// -1 = leaf node
        public Single dist;
        public areanode_t[] children; // [2];
        public Link trigger_edicts;
        public Link solid_edicts;

        public void Clear()
        {
            this.axis = 0;
            this.dist = 0;
            this.children[0] = null;
            this.children[1] = null;
            this.trigger_edicts.ClearToNulls();
            this.solid_edicts.ClearToNulls();
        }

        public areanode_t()
        {
            this.children = new areanode_t[2];
            this.trigger_edicts = new Link( this );
            this.solid_edicts = new Link( this );
        }
    } //areanode_t;

    internal class server_static_t
    {
        public Int32 maxclients;
        public Int32 maxclientslimit;
        public client_t[] clients; // [maxclients]
        public Int32 serverflags;     // episode completion information
        public Boolean changelevel_issued;	// cleared when at SV_SpawnServer
    }// server_static_t;

    //=============================================================================

    // server_state_t;

    internal class server_t
    {
        public Boolean active;             // false if only a net client
        public Boolean paused;
        public Boolean loadgame;           // handle connections specially
        public Double time;
        public Int32 lastcheck;           // used by PF_checkclient
        public Double lastchecktime;
        public String name;// char		name[64];			// map name
        public String modelname;// char		modelname[64];		// maps/<name>.bsp, for model_precache[0]
        public Model worldmodel;
        public String[] model_precache; //[MAX_MODELS];	// NULL terminated
        public Model[] models; //[MAX_MODELS];
        public String[] sound_precache; //[MAX_SOUNDS];	// NULL terminated
        public String[] lightstyles; // [MAX_LIGHTSTYLES];
        public Int32 num_edicts;
        public Int32 max_edicts;
        public MemoryEdict[] edicts;        // can NOT be array indexed, because

        // edict_t is variable sized, but can
        // be used to reference the world ent
        public server_state_t state;			// some actions are only valid during load

        public MessageWriter datagram;
        public MessageWriter reliable_datagram; // copied to all clients at end of frame
        public MessageWriter signon;

        public void Clear()
        {
            this.active = false;
            this.paused = false;
            this.loadgame = false;
            this.time = 0;
            this.lastcheck = 0;
            this.lastchecktime = 0;
            this.name = null;
            this.modelname = null;
            this.worldmodel = null;
            Array.Clear( this.model_precache, 0, this.model_precache.Length );
            Array.Clear( this.models, 0, this.models.Length );
            Array.Clear( this.sound_precache, 0, this.sound_precache.Length );
            Array.Clear( this.lightstyles, 0, this.lightstyles.Length );
            this.num_edicts = 0;
            this.max_edicts = 0;
            this.edicts = null;
            this.state = 0;
            this.datagram.Clear();
            this.reliable_datagram.Clear();
            this.signon.Clear();
            GC.Collect();
        }

        public server_t()
        {
            this.model_precache = new String[QDef.MAX_MODELS];
            this.models = new Model[QDef.MAX_MODELS];
            this.sound_precache = new String[QDef.MAX_SOUNDS];
            this.lightstyles = new String[QDef.MAX_LIGHTSTYLES];
            this.datagram = new MessageWriter( QDef.MAX_DATAGRAM );
            this.reliable_datagram = new MessageWriter( QDef.MAX_DATAGRAM );
            this.signon = new MessageWriter( 8192 );
        }
    }// server_t;

    
}
