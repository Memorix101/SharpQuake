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
using SharpQuake.Framework.Mathematics;

namespace SharpQuake.Framework
{
    public class Plane
    {
        public Vector3 normal;
        public Single dist;
        public Byte type;			// for texture axis selection and fast side tests
        public Byte signbits;       // signx + signy<<1 + signz<<1


        public Int32 SignbitsForPlane()
        {
            // for fast box on planeside test
            var bits = 0;
            if ( normal.X < 0 )
                bits |= 1 << 0;
            if ( normal.Y < 0 )
                bits |= 1 << 1;
            if ( normal.Z < 0 )
                bits |= 1 << 2;
            return bits;
        }
    } //mplane_t;
}
