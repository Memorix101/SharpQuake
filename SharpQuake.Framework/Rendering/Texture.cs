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
    // Uze:
    // WARNING: texture_t changed!!!
    // in original Quake texture_t and it's data where allocated as one hunk
    // texture_t* ptex = Alloc_Hunk(sizeof(texture_t) + size_of_mip_level_0 + ...)
    // ptex->offset[0] = sizeof(texture_t)
    // ptex->offset[1] = ptex->offset[0] + size_of_mip_level_0 and so on
    // now there is field <pixels> and all offsets are just indices in this byte array
    public class Texture
    {
        public String name; // char[16];
        public UInt32 width, height;
        public Int32 gl_texturenum;
        public MemorySurface texturechain;	// for gl_texsort drawing
        public Int32 anim_total;				// total tenths in sequence ( 0 = no)
        public Int32 anim_min, anim_max;		// time for this frame min <=time< max
        public Texture anim_next;		// in the animation sequence
        public Texture alternate_anims;	// bmodels in frmae 1 use these
        public Int32[] offsets; //[MIPLEVELS];		// four mip maps stored
        public Byte[] pixels; // added by Uze
        public System.Drawing.Bitmap rawBitmap;
        public Single scaleX;
        public Single scaleY;

        public Texture( )
        {
            offsets = new Int32[BspDef.MIPLEVELS];
        }
    } //texture_t;
}
