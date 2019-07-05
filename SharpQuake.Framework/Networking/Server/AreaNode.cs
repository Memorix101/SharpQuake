/// <copyright>
///
/// SharpQuakeEvolved changes by optimus-code, 2019
/// 
/// Based on SharpQuake Rewritten in C# by Yury Kiselev, 2010.
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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            this.axis = 0;
            this.dist = 0;
            this.children[0] = null;
            this.children[1] = null;
            this.trigger_edicts.ClearToNulls( );
            this.solid_edicts.ClearToNulls( );
        }

        public areanode_t( )
        {
            this.children = new areanode_t[2];
            this.trigger_edicts = new Link( this );
            this.solid_edicts = new Link( this );
        }
    } //areanode_t;
}
