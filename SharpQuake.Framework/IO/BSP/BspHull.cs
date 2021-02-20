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
    // !!! if this is changed, it must be changed in asm_i386.h too !!!
    public class BspHull
    {
        public BspClipNode[] clipnodes;
        public Plane[] planes;
        public Int32 firstclipnode;
        public Int32 lastclipnode;
        public Vector3 clip_mins;
        public Vector3 clip_maxs;

        public void Clear( )
        {
            clipnodes = null;
            planes = null;
            firstclipnode = 0;
            lastclipnode = 0;
            clip_mins = Vector3.Zero;
            clip_maxs = Vector3.Zero;
        }

        public void CopyFrom( BspHull src )
        {
            clipnodes = src.clipnodes;
            planes = src.planes;
            firstclipnode = src.firstclipnode;
            lastclipnode = src.lastclipnode;
            clip_mins = src.clip_mins;
            clip_maxs = src.clip_maxs;
        }
    } // hull_t;
}
