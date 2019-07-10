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
using System.Runtime.InteropServices;
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

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct dspriteframetype_t
    {
        public spriteframetype_t type;
    } // dspriteframetype_t;
}
