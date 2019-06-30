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
    class mplane_t
    {
        public Vector3 normal;
        public Single dist;
        public Byte type;			// for texture axis selection and fast side tests
        public Byte signbits;		// signx + signy<<1 + signz<<1
    } //mplane_t;


    // Uze:
    // WARNING: texture_t changed!!!
    // in original Quake texture_t and it's data where allocated as one hunk
    // texture_t* ptex = Alloc_Hunk(sizeof(texture_t) + size_of_mip_level_0 + ...)
    // ptex->offset[0] = sizeof(texture_t)
    // ptex->offset[1] = ptex->offset[0] + size_of_mip_level_0 and so on
    // now there is field <pixels> and all offsets are just indices in this byte array
    class texture_t
    {
        public String name; // char[16];
        public UInt32 width, height;
        public Int32 gl_texturenum;
        public msurface_t texturechain;	// for gl_texsort drawing
        public Int32 anim_total;				// total tenths in sequence ( 0 = no)
        public Int32 anim_min, anim_max;		// time for this frame min <=time< max
        public texture_t anim_next;		// in the animation sequence
        public texture_t alternate_anims;	// bmodels in frmae 1 use these
        public Int32[] offsets; //[MIPLEVELS];		// four mip maps stored
        public Byte[] pixels; // added by Uze
        public System.Drawing.Bitmap rawBitmap;
        public Single scaleX;
        public Single scaleY;

        public texture_t()
        {
            offsets = new Int32[bsp_file.MIPLEVELS];
        }
    } //texture_t;

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

    class mtexinfo_t
    {
        public Vector4[] vecs; //public float[][] vecs; //[2][4];
        public Single mipadjust;
        public texture_t texture;
        public Int32 flags;

        public mtexinfo_t()
        {
            vecs = new Vector4[2];// float[2][] { new float[4], new float[4] };
        }
    } //mtexinfo_t;

    class glpoly_t
    {
        public glpoly_t next;
        public glpoly_t chain;
        public Int32 numverts;
        public Int32 flags;			// for SURF_UNDERWATER
        /// <summary>
        /// Changed! Original Quake glpoly_t has 4 vertex inplace and others immidiately after this struct
        /// Now all vertices are in verts array of size [numverts,VERTEXSIZE]
        /// </summary>
        public Single[][] verts; //[4][VERTEXSIZE];	// variable sized (xyz s1t1 s2t2)

        public void Clear()
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
            for ( var i = 0; i < count; i++)
                this.verts[i] = new Single[Mod.VERTEXSIZE];
        }
    } //glpoly_t;

    class msurface_t
    {
        public Int32 visframe;		// should be drawn when node is crossed

        public mplane_t plane;
        public Int32 flags;

        public Int32 firstedge;	// look up in model->surfedges[], negative numbers
        public Int32 numedges;	// are backwards edges

        public Int16[] texturemins; //[2];
        public Int16[] extents; //[2];

        public Int32 light_s, light_t;	// gl lightmap coordinates

        public glpoly_t polys;			// multiple if warped
        public msurface_t texturechain;

        public mtexinfo_t texinfo;

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

        public msurface_t()
        {
            texturemins = new Int16[2];
            extents = new Int16[2];
            styles = new Byte[bsp_file.MAXLIGHTMAPS];
            cached_light = new Int32[bsp_file.MAXLIGHTMAPS];
            // samples is allocated when needed
        }
    } //msurface_t;

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
        public mplane_t plane;
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
            this.ambient_sound_level = new Byte[Ambients.NUM_AMBIENTS];
        }
    } //mleaf_t;

    // !!! if this is changed, it must be changed in asm_i386.h too !!!
    class hull_t
    {
        public dclipnode_t[] clipnodes;
        public mplane_t[] planes;
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
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = (Mod.MAX_SKINS * 4))]
        public Int32[,] gl_texturenum; // int gl_texturenum[MAX_SKINS][4];
        /// <summary>
        /// Changed from integers (offsets from this header start) to objects to hold pointers to arrays of byte
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = Mod.MAX_SKINS)]
        public Object[] texels; // int texels[MAX_SKINS];	// only for player skins
        public maliasframedesc_t[] frames; // maliasframedesc_t	frames[1];	// variable sized

        public static Int32 SizeInBytes = Marshal.SizeOf(typeof(aliashdr_t));

        public aliashdr_t()
        {
            this.gl_texturenum = new Int32[Mod.MAX_SKINS, 4];//[];
            this.texels = new Object[Mod.MAX_SKINS];
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
        public dmodel_t[] submodels;

        public Int32 numplanes;
        public mplane_t[] planes; // mplane_t*

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
        public dclipnode_t[] clipnodes; // public dclipnode_t* clipnodes;

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
        public cache_user_t cache; // cache_user_t	cache		// only access through Mod_Extradata

        public model_t()
        {
            this.hulls = new hull_t[bsp_file.MAX_MAP_HULLS];
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
        public v3f scale;
        public v3f scale_origin;
        public Single boundingradius;
        public v3f eyeposition;
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
