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

using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace SharpQuake
{
    static class BspFile
    {
        // upper design bounds

        public const int MAX_MAP_HULLS = 4;

        public const int MAX_MAP_MODELS = 256;
        public const int MAX_MAP_BRUSHES = 4096;
        public const int MAX_MAP_ENTITIES = 1024;
        public const int MAX_MAP_ENTSTRING = 65536;

        public const int MAX_MAP_PLANES = 32767;
        public const int MAX_MAP_NODES = 32767;		// because negative shorts are contents
        public const int MAX_MAP_CLIPNODES = 32767;		//
        public const int MAX_MAP_LEAFS = 8192;
        public const int MAX_MAP_VERTS = 65535;
        public const int MAX_MAP_FACES = 65535;
        public const int MAX_MAP_MARKSURFACES = 65535;
        public const int MAX_MAP_TEXINFO = 4096;
        public const int MAX_MAP_EDGES = 256000;
        public const int MAX_MAP_SURFEDGES = 512000;
        public const int MAX_MAP_TEXTURES = 512;
        public const int MAX_MAP_MIPTEX = 0x200000;
        public const int MAX_MAP_LIGHTING = 0x100000;
        public const int MAX_MAP_VISIBILITY = 0x100000;

        public const int MAX_MAP_PORTALS = 65536;

        // key / value pair sizes

        public const int MAX_KEY = 32;
        public const int MAX_VALUE = 1024;

        public const int MAXLIGHTMAPS = 4;

        public const int BSPVERSION = 29;
        public const int TOOLVERSION = 2;

        public const int HEADER_LUMPS = 15;

        public const int MIPLEVELS = 4;

        public const int TEX_SPECIAL = 1;		// sky or slime, no lightmap or 256 subdivision
    }


    [StructLayout(LayoutKind.Sequential, Pack=1)]
    struct lump_t
    {
	    public int fileofs, filelen;
    } // lump_t;

    static class Lumps
    {
        public const int LUMP_ENTITIES = 0;
        public const int LUMP_PLANES = 1;
        public const int LUMP_TEXTURES = 2;
        public const int LUMP_VERTEXES = 3;
        public const int LUMP_VISIBILITY = 4;
        public const int LUMP_NODES = 5;
        public const int LUMP_TEXINFO = 6;
        public const int LUMP_FACES = 7;
        public const int LUMP_LIGHTING = 8;
        public const int LUMP_CLIPNODES = 9;
        public const int LUMP_LEAFS = 10;
        public const int LUMP_MARKSURFACES = 11;
        public const int LUMP_EDGES = 12;
        public const int LUMP_SURFEDGES = 13;
        public const int LUMP_MODELS = 14;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct dmodel_t
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public float[] mins; // [3];
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public float[] maxs; //[3];
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public float[] origin; // [3];
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = BspFile.MAX_MAP_HULLS)]
        public int[] headnode; //[MAX_MAP_HULLS];
        public int visleafs;		// not including the solid leaf 0
        public int firstface, numfaces;

        public static int SizeInBytes = Marshal.SizeOf(typeof(dmodel_t));
    } // dmodel_t;

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct dheader_t
    {
        public int version;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = BspFile.HEADER_LUMPS)]
        public lump_t[] lumps; //[HEADER_LUMPS];

        public static int SizeInBytes = Marshal.SizeOf(typeof(dheader_t));
    } // dheader_t;

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct dmiptexlump_t
    {
	    public int nummiptex;
        //[MarshalAs(UnmanagedType.ByValArray, SizeConst=4)]
	    //public int[] dataofs; // [nummiptex]

        public static int SizeInBytes = Marshal.SizeOf(typeof(dmiptexlump_t));
    } // dmiptexlump_t;

    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    struct miptex_t
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst=16)]
	    public byte[] name; //[16];
	    public uint width, height;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst=BspFile.MIPLEVELS)]
	    public uint[] offsets; //[MIPLEVELS];		// four mip maps stored

        public static int SizeInBytes = Marshal.SizeOf(typeof(miptex_t));
    } // miptex_t;

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct dvertex_t
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst=3)]
	    public float[] point; //[3];

        public static int SizeInBytes = Marshal.SizeOf(typeof(dvertex_t));
    } // dvertex_t;


    static class Planes
    {
        // 0-2 are axial planes
        public const int PLANE_X = 0;
        public const int PLANE_Y = 1;
        public const int PLANE_Z = 2;

        // 3-5 are non-axial planes snapped to the nearest
        public const int PLANE_ANYX = 3;
        public const int PLANE_ANYY = 4;
        public const int PLANE_ANYZ = 5;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct dplane_t
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst=3)]
	    public float[] normal; //[3];
	    public float dist;
	    public int type;		// PLANE_X - PLANE_ANYZ ?remove? trivial to regenerate

        public static int SizeInBytes = Marshal.SizeOf(typeof(dplane_t));
    } // dplane_t;


    static class Contents
    {
        public const int CONTENTS_EMPTY = -1;
        public const int CONTENTS_SOLID = -2;
        public const int CONTENTS_WATER = -3;
        public const int CONTENTS_SLIME = -4;
        public const int CONTENTS_LAVA = -5;
        public const int CONTENTS_SKY = -6;
        public const int CONTENTS_ORIGIN = -7;		// removed at csg time
        public const int CONTENTS_CLIP = -8;		// changed to contents_solid

        public const int CONTENTS_CURRENT_0 = -9;
        public const int CONTENTS_CURRENT_90 = -10;
        public const int CONTENTS_CURRENT_180 = -11;
        public const int CONTENTS_CURRENT_270 = -12;
        public const int CONTENTS_CURRENT_UP = -13;
        public const int CONTENTS_CURRENT_DOWN = -14;
    }

    // !!! if this is changed, it must be changed in asm_i386.h too !!!
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct dnode_t
    {
        public int planenum;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public short[] children;//[2];	// negative numbers are -(leafs+1), not nodes
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public short[] mins; //[3];		// for sphere culling
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public short[] maxs; //[3];
        public ushort firstface;
        public ushort numfaces;	// counting both sides

        public static int SizeInBytes = Marshal.SizeOf(typeof(dnode_t));
    } // dnode_t;

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct dclipnode_t
    {
	    public int planenum;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst=2)]
	    public short[] children; //[2];	// negative numbers are contents

        public static int SizeInBytes = Marshal.SizeOf(typeof(dclipnode_t));
    } // dclipnode_t;


    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct texinfo_t
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst=8)]
	    public float[] vecs; //[2][4];		// [s/t][xyz offset]
	    public int miptex;
	    public int flags;

        public static int SizeInBytes = Marshal.SizeOf(typeof(texinfo_t));
    } //texinfo_t;
    

    // note that edge 0 is never used, because negative edge nums are used for
    // counterclockwise use of the edge in a face
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct dedge_t
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst=2)]
	    public ushort[] v; //[2];		// vertex numbers

        public static int SizeInBytes = Marshal.SizeOf(typeof(dedge_t));
    } // dedge_t;


    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct dface_t
    {
        public short planenum;
        public short side;

        public int firstedge;		// we must support > 64k edges
        public short numedges;
        public short texinfo;

        // lighting info
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = BspFile.MAXLIGHTMAPS)]
        public byte[] styles; //[MAXLIGHTMAPS];
        public int lightofs;		// start of [numstyles*surfsize] samples

        public static int SizeInBytes = Marshal.SizeOf(typeof(dface_t));
    } // dface_t;


    static class Ambients
    {
        public const int AMBIENT_WATER = 0;
        public const int AMBIENT_SKY = 1;
        public const int AMBIENT_SLIME = 2;
        public const int AMBIENT_LAVA = 3;

        public const int NUM_AMBIENTS = 4;		// automatic ambient sounds
    }

    // leaf 0 is the generic CONTENTS_SOLID leaf, used for all solid areas
    // all other leafs need visibility info
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct dleaf_t
    {
	    public int contents;
	    public int visofs;				// -1 = no visibility info

        [MarshalAs(UnmanagedType.ByValArray, SizeConst=3)]
	    public short[] mins;//[3];			// for frustum culling
        [MarshalAs(UnmanagedType.ByValArray, SizeConst=3)]
	    public short[] maxs;//[3];

	    public ushort firstmarksurface;
	    public ushort nummarksurfaces;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst=Ambients.NUM_AMBIENTS)]
	    public byte[] ambient_level; // [NUM_AMBIENTS];

        public static int SizeInBytes = Marshal.SizeOf(typeof(dleaf_t));
    } //dleaf_t;

}
