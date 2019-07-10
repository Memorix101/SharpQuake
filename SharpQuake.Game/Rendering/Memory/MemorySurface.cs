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

namespace SharpQuake.Game.Rendering.Memory
{
    public class MemorySurface
    {
        public Int32 visframe;		// should be drawn when node is crossed

        public Plane plane;
        public Int32 flags;

        public Int32 firstedge;	// look up in model->surfedges[], negative numbers
        public Int32 numedges;	// are backwards edges

        public Int16[] texturemins; //[2];
        public Int16[] extents; //[2];

        public Int32 light_s, light_t;	// gl lightmap coordinates

        public GLPoly polys;			// multiple if warped
        public MemorySurface texturechain;

        public MemoryTextureInfo texinfo;

        // lighting info
        public Int32 dlightframe;
        public Int32 dlightbits;

        public Int32 lightmaptexturenum;
        public Byte[] styles; //[MAXLIGHTMAPS];
        public Int32[] cached_light; //[MAXLIGHTMAPS];	// values currently used in lightmap
        public Boolean cached_dlight;				// true if dynamic light in cache
        /// <summary>
        /// Former "samples" field. Use in pair with sampleofs field!!!
        /// </summary>
        public Byte[] sample_base;		// [numstyles*surfsize]
        public Int32 sampleofs; // added by Uze. In original Quake samples = loadmodel->lightdata + offset;
        // now samples = loadmodel->lightdata;

        public MemorySurface( )
        {
            texturemins = new Int16[2];
            extents = new Int16[2];
            styles = new Byte[BspDef.MAXLIGHTMAPS];
            cached_light = new Int32[BspDef.MAXLIGHTMAPS];
            // samples is allocated when needed
        }
    } //msurface_t;
}
