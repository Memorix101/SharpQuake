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
    public class GLPoly
    {
        public GLPoly next;
        public GLPoly chain;
        public Int32 numverts;
        public Int32 flags;			// for SURF_UNDERWATER
        /// <summary>
        /// Changed! Original Quake glpoly_t has 4 vertex inplace and others immidiately after this struct
        /// Now all vertices are in verts array of size [numverts,VERTEXSIZE]
        /// </summary>
        public Single[][] verts; //[4][VERTEXSIZE];	// variable sized (xyz s1t1 s2t2)

        public void Clear( )
        {
            this.next = null;
            this.chain = null;
            this.numverts = 0;
            this.flags = 0;
            this.verts = null;
        }

        public void AllocVerts( Int32 count )
        {
            this.numverts = count;
            this.verts = new Single[count][];
            for ( var i = 0; i < count; i++ )
                this.verts[i] = new Single[ModelDef.VERTEXSIZE];
        }
    } //glpoly_t;
}
