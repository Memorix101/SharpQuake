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

    // TODO: could be shorts
    
    //
    // spritegn.h
    //
    

    static class SPR
    {
        public const Int32 SPR_VP_PARALLEL_UPRIGHT = 0;
        public const Int32 SPR_FACING_UPRIGHT = 1;
        public const Int32 SPR_VP_PARALLEL = 2;
        public const Int32 SPR_ORIENTED = 3;
        public const Int32 SPR_VP_PARALLEL_ORIENTED = 4;
    }

    


   

}
