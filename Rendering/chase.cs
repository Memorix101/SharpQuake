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

// chase.c -- chase camera code

namespace SharpQuake
{
    /// <summary>
    /// Chase_functions
    /// </summary>
    internal static class chase
    {
        /// <summary>
        /// chase_active.value != 0
        /// </summary>
        public static Boolean IsActive
        {
            get
            {
                return ( _Active.Value != 0 );
            }
        }

        private static CVar _Back;// = { "chase_back", "100" };
        private static CVar _Up;// = { "chase_up", "16" };
        private static CVar _Right;// = { "chase_right", "0" };
        private static CVar _Active;// = { "chase_active", "0" };
        private static Vector3 _Dest;

        // Chase_Init
        public static void Init()
        {
            if( _Back == null )
            {
                _Back = new CVar( "chase_back", "100" );
                _Up = new CVar( "chase_up", "16" );
                _Right = new CVar( "chase_right", "0" );
                _Active = new CVar( "chase_active", "0" );
            }
        }

        // Chase_Reset
        public static void Reset()
        {
            // for respawning and teleporting
            //	start position 12 units behind head
        }

        // Chase_Update
        public static void Update()
        {
            // if can't see player, reset
            Vector3 forward, up, right;
            MathLib.AngleVectors( ref client.cl.viewangles, out forward, out right, out up );

            // calc exact destination
            _Dest = render.RefDef.vieworg - forward * _Back.Value - right * _Right.Value;
            _Dest.Z = render.RefDef.vieworg.Z + _Up.Value;

            // find the spot the player is looking at
            Vector3 dest = render.RefDef.vieworg + forward * 4096;

            Vector3 stop;
            TraceLine( ref render.RefDef.vieworg, ref dest, out stop );

            // calculate pitch to look at the same spot from camera
            stop -= render.RefDef.vieworg;
            Single dist;
            Vector3.Dot( ref stop, ref forward, out dist );
            if( dist < 1 )
                dist = 1;

            render.RefDef.viewangles.X = ( Single ) ( -Math.Atan( stop.Z / dist ) / Math.PI * 180.0 );
            //r_refdef.viewangles[PITCH] = -atan(stop[2] / dist) / M_PI * 180;

            // move towards destination
            render.RefDef.vieworg = _Dest; //VectorCopy(chase_dest, r_refdef.vieworg);
        }

        private static void TraceLine( ref Vector3 start, ref Vector3 end, out Vector3 impact )
        {
            trace_t trace = new trace_t();

            server.RecursiveHullCheck( client.cl.worldmodel.hulls[0], 0, 0, 1, ref start, ref end, trace );

            impact = trace.endpos; // VectorCopy(trace.endpos, impact);
        }
    }
}
