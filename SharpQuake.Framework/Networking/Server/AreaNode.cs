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

namespace SharpQuake.Framework
{
    public class areanode_t
    {
        public Int32 axis;		// -1 = leaf node
        public Single dist;
        public areanode_t[] children; // [2];
        public Link trigger_edicts;
        public Link solid_edicts;

        public void Clear( )
        {
            axis = 0;
            dist = 0;
            children[0] = null;
            children[1] = null;
            trigger_edicts.ClearToNulls( );
            solid_edicts.ClearToNulls( );
        }

        public areanode_t( )
        {
            children = new areanode_t[2];
            trigger_edicts = new Link( this );
            solid_edicts = new Link( this );
        }
    } //areanode_t;
}
