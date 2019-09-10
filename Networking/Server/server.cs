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
using SharpQuake.Game.Networking.Server;

namespace SharpQuake
{
    public partial class server
    {
        public server_t sv
        {
            get
            {
                return _Server;
            }
        }

        public server_static_t svs
        {
            get
            {
                return _ServerStatic;
            }
        }

        public Boolean IsActive
        {
            get
            {
                return _Server.active;
            }
        }

        public Single Gravity
        {
            get
            {
                return _Gravity.Get<Single>( );
            }
        }

        public Boolean IsLoading
        {
            get
            {
                return _Server.state == server_state_t.Loading;
            }
        }

        public Single Aim
        {
            get
            {
                return _Aim.Get<Single>( );
            }
        }

        private ClientVariable _Friction;// = { "sv_friction", "4", false, true };
        private ClientVariable _EdgeFriction;// = { "edgefriction", "2" };
        private ClientVariable _StopSpeed;// = { "sv_stopspeed", "100" };
        private ClientVariable _Gravity;// = { "sv_gravity", "800", false, true };
        private ClientVariable _MaxVelocity;// = { "sv_maxvelocity", "2000" };
        private ClientVariable _NoStep;// = { "sv_nostep", "0" };
        private ClientVariable _MaxSpeed;// = { "sv_maxspeed", "320", false, true };
        private ClientVariable _Accelerate;// = { "sv_accelerate", "10" };
        private ClientVariable _Aim;// = { "sv_aim", "0.93" };
        private ClientVariable _IdealPitchScale;// = { "sv_idealpitchscale", "0.8" };

        private server_t _Server;
        private server_static_t _ServerStatic;

        private String[] _LocalModels = new String[QDef.MAX_MODELS]; //[MAX_MODELS][5];	// inline model names for precache

        /// <summary>
        /// EDICT_NUM
        /// </summary>
        public MemoryEdict EdictNum( Int32 n )
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
        public MemoryEdict AllocEdict()
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
        public void FreeEdict( MemoryEdict ed )
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
        public Int32 EdictToProg( MemoryEdict e )
        {
            return Array.IndexOf( _Server.edicts, e ); // todo: optimize this
        }

        /// <summary>
        /// PROG_TO_EDICT(e)
        /// Offset in bytes!
        /// </summary>
        public MemoryEdict ProgToEdict( Int32 e )
        {
            if( e < 0 || e > sv.edicts.Length )
                Utilities.Error( "ProgToEdict: Bad prog!" );
            return sv.edicts[e];
        }

        /// <summary>
        /// NUM_FOR_EDICT
        /// </summary>
        public Int32 NumForEdict( MemoryEdict e )
        {
            var i = Array.IndexOf( sv.edicts, e ); // todo: optimize this

            if( i < 0 )
                Utilities.Error( "NUM_FOR_EDICT: bad pointer" );
            return i;
        }

        public server( Host host )
        {
            Host = host;

            _Server = new server_t();
            _ServerStatic = new server_static_t();
        }
    }    
}
