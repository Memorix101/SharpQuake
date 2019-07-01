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

using OpenTK;
using SharpQuake.Framework;

// world.h

namespace SharpQuake
{
    public struct plane_t
    {
        public Vector3 normal;
        public System.Single dist;
    } // plane_t;

    internal static class Move
    {
        public const System.Int32 MOVE_NORMAL = 0;
        public const System.Int32 MOVE_NOMONSTERS = 1;
        public const System.Int32 MOVE_MISSILE = 2;
    }

    public class trace_t
    {
        public System.Boolean allsolid;	// if true, plane is not valid
        public System.Boolean startsolid;	// if true, the initial point was in a solid area
        public System.Boolean inopen, inwater;
        public System.Single fraction;		// time completed, 1.0 = didn't hit anything
        public Vector3 endpos;			// final position
        public plane_t plane;			// surface normal at impact
        public MemoryEdict ent;			// entity the surface is on

        public void CopyFrom( trace_t src )
        {
            this.allsolid = src.allsolid;
            this.startsolid = src.startsolid;
            this.inopen = src.inopen;
            this.inwater = src.inwater;
            this.fraction = src.fraction;
            this.endpos = src.endpos;
            this.plane = src.plane;
            this.ent = src.ent;
        }
    } // trace_t;
}
