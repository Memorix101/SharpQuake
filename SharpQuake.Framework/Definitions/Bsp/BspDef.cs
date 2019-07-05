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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpQuake.Framework
{
    public static class BspDef
    {
        // upper design bounds

        public const System.Int32 MAX_MAP_HULLS = 4;

        public const System.Int32 MAX_MAP_MODELS = 256;
        public const System.Int32 MAX_MAP_BRUSHES = 32768; // 4096
        public const System.Int32 MAX_MAP_ENTITIES = 1024;
        public const System.Int32 MAX_MAP_ENTSTRING = 65536;

        public const System.Int32 MAX_MAP_PLANES = 32767;
        public const System.Int32 MAX_MAP_NODES = 32767;		// because negative shorts are contents
        public const System.Int32 MAX_MAP_CLIPNODES = 32767;		//
        public const System.Int32 MAX_MAP_LEAFS = 8192;
        public const System.Int32 MAX_MAP_VERTS = 65535;
        public const System.Int32 MAX_MAP_FACES = 65535;
        public const System.Int32 MAX_MAP_MARKSURFACES = 65535;
        public const System.Int32 MAX_MAP_TEXINFO = 4096;
        public const System.Int32 MAX_MAP_EDGES = 256000;
        public const System.Int32 MAX_MAP_SURFEDGES = 512000;
        public const System.Int32 MAX_MAP_TEXTURES = 512;
        public const System.Int32 MAX_MAP_MIPTEX = 0x200000;
        public const System.Int32 MAX_MAP_LIGHTING = 0x100000;
        public const System.Int32 MAX_MAP_VISIBILITY = 0x100000;

        public const System.Int32 MAX_MAP_PORTALS = 65536;

        // key / value pair sizes

        public const System.Int32 MAX_KEY = 32;
        public const System.Int32 MAX_VALUE = 1024;

        public const System.Int32 MAXLIGHTMAPS = 4;

        public const System.Int32 Q1_BSPVERSION = 29;
        public const System.Int32 HL_BSPVERSION = 30;

        public const System.Int32 TOOLVERSION = 2;

        public const System.Int32 HEADER_LUMPS = 15;

        public const System.Int32 MIPLEVELS = 4;

        public const System.Int32 TEX_SPECIAL = 1;		// sky or slime, no lightmap or 256 subdivision
    } // bsp_file
}
