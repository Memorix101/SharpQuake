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

using System.Runtime.InteropServices;

namespace SharpQuake
{
    [StructLayout( LayoutKind.Sequential, Pack = 1 )]
    internal struct lump_t
    {
        public System.Int32 fileofs, filelen;
    }

    [StructLayout( LayoutKind.Sequential, Pack = 1 )]
    internal struct dmodel_t
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public System.Single[] mins; // [3];

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public System.Single[] maxs; //[3];

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public System.Single[] origin; // [3];

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = bsp_file.MAX_MAP_HULLS)]
        public System.Int32[] headnode; //[MAX_MAP_HULLS];

        public System.Int32 visleafs;		// not including the solid leaf 0
        public System.Int32 firstface, numfaces;

        public static System.Int32 SizeInBytes = Marshal.SizeOf(typeof(dmodel_t));
    }

    [StructLayout( LayoutKind.Sequential, Pack = 1 )]
    internal struct dheader_t
    {
        public System.Int32 version;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = bsp_file.HEADER_LUMPS)]
        public lump_t[] lumps; //[HEADER_LUMPS];

        public static System.Int32 SizeInBytes = Marshal.SizeOf(typeof(dheader_t));
    }

    [StructLayout( LayoutKind.Sequential, Pack = 1 )]
    internal struct dmiptexlump_t
    {
        public System.Int32 nummiptex;
        //[MarshalAs(UnmanagedType.ByValArray, SizeConst=4)]
        //public int[] dataofs; // [nummiptex]

        public static System.Int32 SizeInBytes = Marshal.SizeOf(typeof(dmiptexlump_t));
    }

    [StructLayout( LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi )]
    internal struct miptex_t
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst=16)]
        public System.Byte[] name; //[16];

        public System.UInt32 width, height;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst=bsp_file.MIPLEVELS)]
        public System.UInt32[] offsets; //[MIPLEVELS];		// four mip maps stored

        public static System.Int32 SizeInBytes = Marshal.SizeOf(typeof(miptex_t));
    }

    [StructLayout( LayoutKind.Sequential, Pack = 1 )]
    internal struct dvertex_t
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst=3)]
        public System.Single[] point; //[3];

        public static System.Int32 SizeInBytes = Marshal.SizeOf(typeof(dvertex_t));
    }

    [StructLayout( LayoutKind.Sequential, Pack = 1 )]
    internal struct dplane_t
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst=3)]
        public System.Single[] normal; //[3];

        public System.Single dist;
        public System.Int32 type;		// PLANE_X - PLANE_ANYZ ?remove? trivial to regenerate

        public static System.Int32 SizeInBytes = Marshal.SizeOf(typeof(dplane_t));
    }

    // !!! if this is changed, it must be changed in asm_i386.h too !!!
    [StructLayout( LayoutKind.Sequential, Pack = 1 )]
    internal struct dnode_t
    {
        public System.Int32 planenum;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public System.Int16[] children;//[2];	// negative numbers are -(leafs+1), not nodes

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public System.Int16[] mins; //[3];		// for sphere culling

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public System.Int16[] maxs; //[3];

        public System.UInt16 firstface;
        public System.UInt16 numfaces;	// counting both sides

        public static System.Int32 SizeInBytes = Marshal.SizeOf(typeof(dnode_t));
    }

    [StructLayout( LayoutKind.Sequential, Pack = 1 )]
    internal struct dclipnode_t
    {
        public System.Int32 planenum;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst=2)]
        public System.Int16[] children; //[2];	// negative numbers are contents

        public static System.Int32 SizeInBytes = Marshal.SizeOf(typeof(dclipnode_t));
    }

    [StructLayout( LayoutKind.Sequential, Pack = 1 )]
    internal struct texinfo_t
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst=8)]
        public System.Single[] vecs; //[2][4];		// [s/t][xyz offset]

        public System.Int32 miptex;
        public System.Int32 flags;

        public static System.Int32 SizeInBytes = Marshal.SizeOf(typeof(texinfo_t));
    }

    // note that edge 0 is never used, because negative edge nums are used for
    // counterclockwise use of the edge in a face
    [StructLayout( LayoutKind.Sequential, Pack = 1 )]
    internal struct dedge_t
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst=2)]
        public System.UInt16[] v; //[2];		// vertex numbers

        public static System.Int32 SizeInBytes = Marshal.SizeOf(typeof(dedge_t));
    }

    [StructLayout( LayoutKind.Sequential, Pack = 1 )]
    internal struct dface_t
    {
        public System.Int16 planenum;
        public System.Int16 side;

        public System.Int32 firstedge;		// we must support > 64k edges
        public System.Int16 numedges;
        public System.Int16 texinfo;

        // lighting info
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = bsp_file.MAXLIGHTMAPS)]
        public System.Byte[] styles; //[MAXLIGHTMAPS];

        public System.Int32 lightofs;		// start of [numstyles*surfsize] samples

        public static System.Int32 SizeInBytes = Marshal.SizeOf(typeof(dface_t));
    }

    // leaf 0 is the generic CONTENTS_SOLID leaf, used for all solid areas
    // all other leafs need visibility info
    [StructLayout( LayoutKind.Sequential, Pack = 1 )]
    internal struct dleaf_t
    {
        public System.Int32 contents;
        public System.Int32 visofs;				// -1 = no visibility info

        [MarshalAs(UnmanagedType.ByValArray, SizeConst=3)]
        public System.Int16[] mins;//[3];			// for frustum culling

        [MarshalAs(UnmanagedType.ByValArray, SizeConst=3)]
        public System.Int16[] maxs;//[3];

        public System.UInt16 firstmarksurface;
        public System.UInt16 nummarksurfaces;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst=Ambients.NUM_AMBIENTS)]
        public System.Byte[] ambient_level; // [NUM_AMBIENTS];

        public static System.Int32 SizeInBytes = Marshal.SizeOf(typeof(dleaf_t));
    }

    internal static class bsp_file
    {
        // upper design bounds

        public const System.Int32 MAX_MAP_HULLS = 4;

        public const System.Int32 MAX_MAP_MODELS = 256;
        public const System.Int32 MAX_MAP_BRUSHES = 4096;
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

        public const System.Int32 BSPVERSION = 29;
        public const System.Int32 TOOLVERSION = 2;

        public const System.Int32 HEADER_LUMPS = 15;

        public const System.Int32 MIPLEVELS = 4;

        public const System.Int32 TEX_SPECIAL = 1;		// sky or slime, no lightmap or 256 subdivision
    }

    // lump_t;

    internal static class Lumps
    {
        public const System.Int32 LUMP_ENTITIES = 0;
        public const System.Int32 LUMP_PLANES = 1;
        public const System.Int32 LUMP_TEXTURES = 2;
        public const System.Int32 LUMP_VERTEXES = 3;
        public const System.Int32 LUMP_VISIBILITY = 4;
        public const System.Int32 LUMP_NODES = 5;
        public const System.Int32 LUMP_TEXINFO = 6;
        public const System.Int32 LUMP_FACES = 7;
        public const System.Int32 LUMP_LIGHTING = 8;
        public const System.Int32 LUMP_CLIPNODES = 9;
        public const System.Int32 LUMP_LEAFS = 10;
        public const System.Int32 LUMP_MARKSURFACES = 11;
        public const System.Int32 LUMP_EDGES = 12;
        public const System.Int32 LUMP_SURFEDGES = 13;
        public const System.Int32 LUMP_MODELS = 14;
    }

    // dmodel_t;

    // dheader_t;

    // dmiptexlump_t;

    // miptex_t;

    // dvertex_t;

    internal static class Planes
    {
        // 0-2 are axial planes
        public const System.Int32 PLANE_X = 0;

        public const System.Int32 PLANE_Y = 1;
        public const System.Int32 PLANE_Z = 2;

        // 3-5 are non-axial planes snapped to the nearest
        public const System.Int32 PLANE_ANYX = 3;

        public const System.Int32 PLANE_ANYY = 4;
        public const System.Int32 PLANE_ANYZ = 5;
    }

    // dplane_t;

    internal static class Contents
    {
        public const System.Int32 CONTENTS_EMPTY = -1;
        public const System.Int32 CONTENTS_SOLID = -2;
        public const System.Int32 CONTENTS_WATER = -3;
        public const System.Int32 CONTENTS_SLIME = -4;
        public const System.Int32 CONTENTS_LAVA = -5;
        public const System.Int32 CONTENTS_SKY = -6;
        public const System.Int32 CONTENTS_ORIGIN = -7;		// removed at csg time
        public const System.Int32 CONTENTS_CLIP = -8;		// changed to contents_solid

        public const System.Int32 CONTENTS_CURRENT_0 = -9;
        public const System.Int32 CONTENTS_CURRENT_90 = -10;
        public const System.Int32 CONTENTS_CURRENT_180 = -11;
        public const System.Int32 CONTENTS_CURRENT_270 = -12;
        public const System.Int32 CONTENTS_CURRENT_UP = -13;
        public const System.Int32 CONTENTS_CURRENT_DOWN = -14;
    }

    // dnode_t;

    // dclipnode_t;

    //texinfo_t;

    // dedge_t;

    // dface_t;

    internal static class Ambients
    {
        public const System.Int32 AMBIENT_WATER = 0;
        public const System.Int32 AMBIENT_SKY = 1;
        public const System.Int32 AMBIENT_SLIME = 2;
        public const System.Int32 AMBIENT_LAVA = 3;

        public const System.Int32 NUM_AMBIENTS = 4;		// automatic ambient sounds
    }

    //dleaf_t;
}
