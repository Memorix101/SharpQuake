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

    /*
    ==============================================================================

    BRUSH MODELS

    ==============================================================================
    */


    // plane_t structure
    // !!! if this is changed, it must be changed in asm_i386.h too !!!

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


    public struct maliasframedesc_t
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
    public class aliashdr_t
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
        public SyncType synctype;
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
        public SyncType synctype;
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
    public struct stvert_t
    {
        public Int32 onseam;
        public Int32 s;
        public Int32 t;

        public static Int32 SizeInBytes = Marshal.SizeOf(typeof(stvert_t));
    } // stvert_t;

    [StructLayout(LayoutKind.Sequential)]
    public struct dtriangle_t
    {
        public Int32 facesfront;
        [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.I4, SizeConst = 3)]
        public Int32[] vertindex; // int vertindex[3];

        public static Int32 SizeInBytes = Marshal.SizeOf(typeof(dtriangle_t));
    } // dtriangle_t;

    // This mirrors trivert_t in trilib.h, is present so Quake knows how to
    // load this data

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct trivertx_t
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
        public SyncType synctype;

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
