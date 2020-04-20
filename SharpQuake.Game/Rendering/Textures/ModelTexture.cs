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
using SharpQuake.Game.Rendering.Memory;
using SharpQuake.Renderer.Textures;

namespace SharpQuake.Game.Rendering.Textures
{
    // Uze:
    // WARNING: texture_t changed!!!
    // in original Quake texture_t and it's data where allocated as one hunk
    // texture_t* ptex = Alloc_Hunk(sizeof(texture_t) + size_of_mip_level_0 + ...)
    // ptex->offset[0] = sizeof(texture_t)
    // ptex->offset[1] = ptex->offset[0] + size_of_mip_level_0 and so on
    // now there is field <pixels> and all offsets are just indices in this byte array
    public class ModelTexture
    {
        public String name; // char[16];
        public UInt32 width, height;
        public BaseTexture texture;
        public MemorySurface texturechain;	// for gl_texsort drawing
        public Int32 anim_total;				// total tenths in sequence ( 0 = no)
        public Int32 anim_min, anim_max;		// time for this frame min <=time< max
        public ModelTexture anim_next;		// in the animation sequence
        public ModelTexture alternate_anims;	// bmodels in frmae 1 use these
        public Int32[] offsets; //[MIPLEVELS];		// four mip maps stored
        public Byte[] pixels; // added by Uze
        public Single scaleX;
        public Single scaleY;
		public Byte[] localPalette;

		public BaseTexture Texture
        {
            get;
            set;
        }

        public ModelTexture( )
        {
            offsets = new Int32[BspDef.MIPLEVELS];
        }
    } //texture_t;
}
