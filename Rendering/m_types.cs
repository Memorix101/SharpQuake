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
using System.Runtime.InteropServices;
using OpenTK;
using SharpQuake.Framework;

// gl_model.h
// modelgen.h
// spritegn.h

namespace SharpQuake
{
    // d*_t structures are on-disk representations
    // m*_t structures are in-memory

    // entity effects
    static class EntityEffects
    {
        public static Int32 EF_BRIGHTFIELD = 1;
        public static Int32 EF_MUZZLEFLASH = 2;
        public static Int32 EF_BRIGHTLIGHT = 4;
        public static Int32 EF_DIMLIGHT = 8;
#if QUAKE2
        public static int EF_DARKLIGHT = 16;
        public static int EF_DARKFIELD = 32;
        public static int EF_LIGHT = 64;
        public static int EF_NODRAW = 128;
#endif
    }


    /*
    ==============================================================================

    BRUSH MODELS

    ==============================================================================
    */


    //
    // in memory representation
    //
    // !!! if this is changed, it must be changed in asm_draw.h too !!!
    struct mvertex_t
    {
        public Vector3 position;
    } // mvertex_t;

    static class Side
    {
        public const Int32 SIDE_FRONT = 0;
        public const Int32 SIDE_BACK = 1;
        public const Int32 SIDE_ON = 2;
    }


    // plane_t structure
    // !!! if this is changed, it must be changed in asm_i386.h too !!!
   


    

    static class Surf
    {
        public const Int32 SURF_PLANEBACK = 2;
        public const Int32 SURF_DRAWSKY = 4;
        public const Int32 SURF_DRAWSPRITE = 8;
        public const Int32 SURF_DRAWTURB = 0x10;
        public const Int32 SURF_DRAWTILED = 0x20;
        public const Int32 SURF_DRAWBACKGROUND = 0x40;
        public const Int32 SURF_UNDERWATER = 0x80;
    }

    // !!! if this is changed, it must be changed in asm_draw.h too !!!
    struct medge_t
    {
        public UInt16[] v; // [2];
        //public uint cachededgeoffset;
    } //medge_t;

    

    

    

    // commmon part of mnode_t and mleaf_t
    class mnodebase_t
    {
        public Int32 contents;		// 0 for mnode_t and negative for mleaf_t
        public Int32 visframe;		// node needs to be traversed if current
        public Vector3 mins;
        public Vector3 maxs;
        //public float[] minmaxs; //[6];		// for bounding box culling
        public mnode_t parent;

        //public mnodebase_t()
        //{
        //    this.minmaxs = new float[6];
        //}
    }

    class mnode_t : mnodebase_t
    {
        // node specific
        public Plane plane;
        public mnodebase_t[] children; //[2];	

        public UInt16 firstsurface;
        public UInt16 numsurfaces;

        public mnode_t()
        {
            this.children = new mnodebase_t[2];
        }
    } //mnode_t;


    class mleaf_t : mnodebase_t
    {
        // leaf specific
        /// <summary>
        /// loadmodel->visdata
        /// Use in pair with visofs!
        /// </summary>
        public Byte[] compressed_vis; // byte*
        public Int32 visofs; // added by Uze
        public efrag_t efrags;

        /// <summary>
        /// loadmodel->marksurfaces
        /// </summary>
        public msurface_t[] marksurfaces;
        public Int32 firstmarksurface; // msurface_t	**firstmarksurface;
        public Int32 nummarksurfaces;
        //public int key;			// BSP sequence number for leaf's contents
        public Byte[] ambient_sound_level; // [NUM_AMBIENTS];

        public mleaf_t()
        {
            this.ambient_sound_level = new Byte[AmbientDef.NUM_AMBIENTS];
        }
    } //mleaf_t;

    // !!! if this is changed, it must be changed in asm_i386.h too !!!
    class hull_t
    {
        public BspClipNode[] clipnodes;
        public Plane[] planes;
        public Int32 firstclipnode;
        public Int32 lastclipnode;
        public Vector3 clip_mins;
        public Vector3 clip_maxs;

        public void Clear()
        {
            this.clipnodes = null;
            this.planes = null;
            this.firstclipnode = 0;
            this.lastclipnode = 0;
            this.clip_mins = Vector3.Zero;
            this.clip_maxs = Vector3.Zero;
        }

        public void CopyFrom(hull_t src)
        {
            this.clipnodes = src.clipnodes;
            this.planes = src.planes;
            this.firstclipnode = src.firstclipnode;
            this.lastclipnode = src.lastclipnode;
            this.clip_mins = src.clip_mins;
            this.clip_maxs = src.clip_maxs;
        }
    } // hull_t;

    // FIXME: shorten these?
    class mspriteframe_t
    {
        public Int32 width;
        public Int32 height;
        public Single up, down, left, right;
        public Int32 gl_texturenum;
    } //mspriteframe_t;

    class mspritegroup_t
    {
        public Int32 numframes;
        public Single[] intervals; // float*
        public mspriteframe_t[] frames; // mspriteframe_t	*frames[1];
    } //mspritegroup_t;

    struct mspriteframedesc_t
    {
        public spriteframetype_t type;
        public Object frameptr; // mspriteframe_t or mspritegroup_t
    } // mspriteframedesc_t;

    class msprite_t
    {
        public Int32 type;
        public Int32 maxwidth;
        public Int32 maxheight;
        public Int32 numframes;
        public Single beamlength;		// remove?
        //void				*cachespot;		// remove?
        public mspriteframedesc_t[] frames; // mspriteframedesc_t	frames[1];
    } // msprite_t;


    struct maliasframedesc_t
    {
        public Int32 firstpose;
        public Int32 numposes;
        public Single interval;
        public trivertx_t bboxmin;
        public trivertx_t bboxmax;
        //public int frame;
        public String name; // char				name[16];

        public static Int32 SizeInBytes = Marshal.SizeOf(typeof(maliasframedesc_t));

        public void Init()
        {
            this.bboxmin.Init();
            this.bboxmax.Init();
        }
    } //maliasframedesc_t;

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    class aliashdr_t
    {
        public Int32 ident;
        public Int32 version;
        public Vector3 scale;
        public Vector3 scale_origin;
        public Single boundingradius;
        public Vector3 eyeposition;
        public Int32 numskins;
        public Int32 skinwidth;
        public Int32 skinheight;
        public Int32 numverts;
        public Int32 numtris;
        public Int32 numframes;
        public synctype_t synctype;
        public Int32 flags;
        public Single size;

        public Int32 numposes;
        public Int32 poseverts;
        /// <summary>
        /// Changed from int offset from this header to posedata to
        /// trivertx_t array
        /// </summary>
        public trivertx_t[] posedata;	// numposes*poseverts trivert_t
        /// <summary>
        /// Changed from int offset from this header to commands data
        /// to commands array
        /// </summary>
        public Int32[] commands;	// gl command list with embedded s/t
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = (ModelDef.MAX_SKINS * 4))]
        public Int32[,] gl_texturenum; // int gl_texturenum[MAX_SKINS][4];
        /// <summary>
        /// Changed from integers (offsets from this header start) to objects to hold pointers to arrays of byte
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = ModelDef.MAX_SKINS)]
        public Object[] texels; // int texels[MAX_SKINS];	// only for player skins
        public maliasframedesc_t[] frames; // maliasframedesc_t	frames[1];	// variable sized

        public static Int32 SizeInBytes = Marshal.SizeOf(typeof(aliashdr_t));

        public aliashdr_t()
        {
            this.gl_texturenum = new Int32[ModelDef.MAX_SKINS, 4];//[];
            this.texels = new Object[ModelDef.MAX_SKINS];
        }
    } // aliashdr_t;

    //===================================================================

    //
    // Whole model
    //

    enum modtype_t
    {
        mod_brush, mod_sprite, mod_alias
    } // modtype_t;

    static class EF
    {
        public const Int32 EF_ROCKET = 1;			// leave a trail
        public const Int32 EF_GRENADE = 2;			// leave a trail
        public const Int32 EF_GIB = 4;			// leave a trail
        public const Int32 EF_ROTATE = 8;			// rotate (bonus items)
        public const Int32 EF_TRACER = 16;			// green split trail
        public const Int32 EF_ZOMGIB = 32;			// small blood trail
        public const Int32 EF_TRACER2 = 64;			// orange split trail + rotate
        public const Int32 EF_TRACER3 = 128;			// purple trail
    }

    class model_t
    {
        public String name; // char		name[MAX_QPATH];
        public Boolean needload;		// bmodels and sprites don't cache normally

        public modtype_t type;
        public Int32 numframes;
        public synctype_t synctype;

        public Int32 flags;

        //
        // volume occupied by the model graphics
        //		
        public Vector3 mins, maxs;
        public Single radius;

        //
        // solid volume for clipping 
        //
        public Boolean clipbox;
        public Vector3 clipmins, clipmaxs;

        //
        // brush model
        //
        public Int32 firstmodelsurface, nummodelsurfaces;

        public Int32 numsubmodels;
        public BspModel[] submodels;

        public Int32 numplanes;
        public Plane[] planes; // mplane_t*

        public Int32 numleafs;		// number of visible leafs, not counting 0
        public mleaf_t[] leafs; // mleaf_t*

        public Int32 numvertexes;
        public mvertex_t[] vertexes; // mvertex_t*

        public Int32 numedges;
        public medge_t[] edges; // medge_t*

        public Int32 numnodes;
        public mnode_t[] nodes; // mnode_t *nodes;

        public Int32 numtexinfo;
        public mtexinfo_t[] texinfo;

        public Int32 numsurfaces;
        public msurface_t[] surfaces;

        public Int32 numsurfedges;
        public Int32[] surfedges; // int *surfedges;

        public Int32 numclipnodes;
        public BspClipNode[] clipnodes; // public dclipnode_t* clipnodes;

        public Int32 nummarksurfaces;
        public msurface_t[] marksurfaces; // msurface_t **marksurfaces;

        public hull_t[] hulls; // [MAX_MAP_HULLS];

        public Int32 numtextures;
        public texture_t[] textures; // texture_t	**textures;

        public Byte[] visdata; // byte *visdata;
        public Byte[] lightdata; // byte		*lightdata;
        public String entities; // char		*entities

        //
        // additional model data
        //
        public CacheUser cache; // cache_user_t	cache		// only access through Mod_Extradata

        public model_t()
        {
            this.hulls = new hull_t[BspDef.MAX_MAP_HULLS];
            for ( var i = 0; i < this.hulls.Length; i++)
                this.hulls[i] = new hull_t();
        }

        public void Clear()
        {
            this.name = null;
            this.needload = false;
            this.type = 0;
            this.numframes = 0;
            this.synctype = 0;
            this.flags = 0;
            this.mins = Vector3.Zero;
            this.maxs = Vector3.Zero;
            this.radius = 0;
            this.clipbox = false;
            this.clipmins = Vector3.Zero;
            this.clipmaxs = Vector3.Zero;
            this.firstmodelsurface = 0;
            this.nummodelsurfaces = 0;

            this.numsubmodels = 0;
            this.submodels = null;

            this.numplanes = 0;
            this.planes = null;

            this.numleafs = 0;
            this.leafs = null;

            this.numvertexes = 0;
            this.vertexes = null;

            this.numedges = 0;
            this.edges = null;

            this.numnodes = 0;
            this.nodes = null;

            this.numtexinfo = 0;
            this.texinfo = null;

            this.numsurfaces = 0;
            this.surfaces = null;

            this.numsurfedges = 0;
            this.surfedges = null;

            this.numclipnodes = 0;
            this.clipnodes = null;

            this.nummarksurfaces = 0;
            this.marksurfaces = null;

            foreach (hull_t h in this.hulls)
                h.Clear();

            this.numtextures = 0;
            this.textures = null;

            this.visdata = null;
            this.lightdata = null;
            this.entities = null;

            this.cache = null;
        }

        public void CopyFrom(model_t src)
        {
            this.name = src.name;
            this.needload = src.needload;
            this.type = src.type;
            this.numframes = src.numframes;
            this.synctype = src.synctype;
            this.flags = src.flags;
            this.mins = src.mins;
            this.maxs = src.maxs;
            this.radius = src.radius;
            this.clipbox = src.clipbox;
            this.clipmins = src.clipmins;
            this.clipmaxs = src.clipmaxs;
            this.firstmodelsurface = src.firstmodelsurface;
            this.nummodelsurfaces = src.nummodelsurfaces;

            this.numsubmodels = src.numsubmodels;
            this.submodels = src.submodels;

            this.numplanes = src.numplanes;
            this.planes = src.planes;

            this.numleafs = src.numleafs;
            this.leafs = src.leafs;

            this.numvertexes = src.numvertexes;
            this.vertexes = src.vertexes;

            this.numedges = src.numedges;
            this.edges = src.edges;

            this.numnodes = src.numnodes;
            this.nodes = src.nodes;

            this.numtexinfo = src.numtexinfo;
            this.texinfo = src.texinfo;

            this.numsurfaces = src.numsurfaces;
            this.surfaces = src.surfaces;

            this.numsurfedges = src.numsurfedges;
            this.surfedges = src.surfedges;

            this.numclipnodes = src.numclipnodes;
            this.clipnodes = src.clipnodes;

            this.nummarksurfaces = src.nummarksurfaces;
            this.marksurfaces = src.marksurfaces;

            for ( var i = 0; i < src.hulls.Length; i++)
            {
                this.hulls[i].CopyFrom(src.hulls[i]);
            }

            this.numtextures = src.numtextures;
            this.textures = src.textures;

            this.visdata = src.visdata;
            this.lightdata = src.lightdata;
            this.entities = src.entities;

            this.cache = src.cache;
        }
    } //model_t;

    //
    // modelgen.h: header file for model generation program
    //

    // *********************************************************
    // * This file must be identical in the modelgen directory *
    // * and in the Quake directory, because it's used to      *
    // * pass data from one to the other via model files.      *
    // *********************************************************

    //#define ALIAS_ONSEAM				0x0020

    // must match definition in spritegn.h
    enum synctype_t
    {
        ST_SYNC = 0, ST_RAND
    } // synctype_t;


    enum aliasframetype_t
    {
        ALIAS_SINGLE = 0, ALIAS_GROUP
    } // aliasframetype_t;

    enum aliasskintype_t
    {
        ALIAS_SKIN_SINGLE = 0, ALIAS_SKIN_GROUP
    } // aliasskintype_t;

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct mdl_t
    {
        public Int32 ident;
        public Int32 version;
        public Vector3f scale;
        public Vector3f scale_origin;
        public Single boundingradius;
        public Vector3f eyeposition;
        public Int32 numskins;
        public Int32 skinwidth;
        public Int32 skinheight;
        public Int32 numverts;
        public Int32 numtris;
        public Int32 numframes;
        public synctype_t synctype;
        public Int32 flags;
        public Single size;

        public static readonly Int32 SizeInBytes = Marshal.SizeOf(typeof(mdl_t));

        //static mdl_t()
        //{
        //    mdl_t.SizeInBytes = Marshal.SizeOf(typeof(mdl_t));
        //}
    } // mdl_t;

    // TODO: could be shorts

    [StructLayout(LayoutKind.Sequential, Pack=1)]
    struct stvert_t
    {
        public Int32 onseam;
        public Int32 s;
        public Int32 t;

        public static Int32 SizeInBytes = Marshal.SizeOf(typeof(stvert_t));
    } // stvert_t;

    [StructLayout(LayoutKind.Sequential)]
    struct dtriangle_t
    {
        public Int32 facesfront;
        [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.I4, SizeConst = 3)]
        public Int32[] vertindex; // int vertindex[3];

        public static Int32 SizeInBytes = Marshal.SizeOf(typeof(dtriangle_t));
    } // dtriangle_t;

    // This mirrors trivert_t in trilib.h, is present so Quake knows how to
    // load this data

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct trivertx_t
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public Byte[] v; // [3];
        public Byte lightnormalindex;

        public static Int32 SizeInBytes = Marshal.SizeOf(typeof(trivertx_t));

        /// <summary>
        /// Call only for manually created instances
        /// </summary>
        public void Init()
        {
            if (this.v == null)
                this.v = new Byte[3];
        }
    } // trivertx_t;

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct daliasframe_t
    {
        public trivertx_t bboxmin;	// lightnormal isn't used
        public trivertx_t bboxmax;	// lightnormal isn't used
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public Byte[] name; // char[16]	// frame name from grabbing

        public static Int32 SizeInBytes = Marshal.SizeOf(typeof(daliasframe_t));
    } // daliasframe_t;

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct daliasgroup_t
    {
        public Int32 numframes;
        public trivertx_t bboxmin;	// lightnormal isn't used
        public trivertx_t bboxmax;	// lightnormal isn't used

        public static Int32 SizeInBytes = Marshal.SizeOf(typeof(daliasgroup_t));
    } // daliasgroup_t;

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct daliasskingroup_t
    {
        public Int32 numskins;

        public static Int32 SizeInBytes = Marshal.SizeOf(typeof(daliasskingroup_t));
    } // daliasskingroup_t;

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct daliasinterval_t
    {
        public Single interval;

        public static Int32 SizeInBytes = Marshal.SizeOf(typeof(daliasinterval_t));
    } // daliasinterval_t;

    [StructLayout(LayoutKind.Sequential, Pack=1)]
    struct daliasskininterval_t
    {
        public Single interval;

        public static Int32 SizeInBytes = Marshal.SizeOf(typeof(daliasskininterval_t));
    } // daliasskininterval_t;

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct daliasframetype_t
    {
        public aliasframetype_t type;
    } // daliasframetype_t;

    [StructLayout(LayoutKind.Sequential, Pack=1)]
    struct daliasskintype_t
    {
        public aliasskintype_t type;

        public static Int32 SizeInBytes = Marshal.SizeOf(typeof(daliasskintype_t));
    } //daliasskintype_t;


    //
    // spritegn.h
    //

    [StructLayout(LayoutKind.Sequential, Pack=1)]
    struct dsprite_t
    {
        public Int32 ident;
        public Int32 version;
        public Int32 type;
        public Single boundingradius;
        public Int32 width;
        public Int32 height;
        public Int32 numframes;
        public Single beamlength;
        public synctype_t synctype;

        public static Int32 SizeInBytes = Marshal.SizeOf(typeof(dsprite_t));
    } // dsprite_t;

    static class SPR
    {
        public const Int32 SPR_VP_PARALLEL_UPRIGHT = 0;
        public const Int32 SPR_FACING_UPRIGHT = 1;
        public const Int32 SPR_VP_PARALLEL = 2;
        public const Int32 SPR_ORIENTED = 3;
        public const Int32 SPR_VP_PARALLEL_ORIENTED = 4;
    }

    [StructLayout(LayoutKind.Sequential, Pack=1)]
    struct dspriteframe_t
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public Int32[] origin; // [2];
        public Int32 width;
        public Int32 height;

        public static Int32 SizeInBytes = Marshal.SizeOf(typeof(dspriteframe_t));
    } // dspriteframe_t;

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct dspritegroup_t
    {
        public Int32 numframes;

        public static Int32 SizeInBytes = Marshal.SizeOf(typeof(dspritegroup_t));
    } // dspritegroup_t;

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct dspriteinterval_t
    {
        public Single interval;

        public static Int32 SizeInBytes = Marshal.SizeOf(typeof(dspriteinterval_t));
    } // dspriteinterval_t;

    enum spriteframetype_t
    {
        SPR_SINGLE = 0, SPR_GROUP
    } // spriteframetype_t;

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct dspriteframetype_t
    {
        public spriteframetype_t type;
    } // dspriteframetype_t;
}
