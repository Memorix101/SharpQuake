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
    } // bsp_file
}
