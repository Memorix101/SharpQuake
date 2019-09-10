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

namespace SharpQuake.Framework.IO.BSP
{
    [StructLayout( LayoutKind.Sequential, Pack = 1 )]
    public struct Q3Face
    {
        public Int32 textureIndex;
        public Int32 effectIndex;
        public Q3FaceType type;

        public Int32 firstVertex;
        public Int32 numVertices;

        public Int32 firstElement;
        public Int32 numElements;

        public Int32 lightMapIndex;

        [MarshalAs( UnmanagedType.ByValArray, SizeConst = 2 )]
        public Int32[] lightMapBase;

        [MarshalAs( UnmanagedType.ByValArray, SizeConst = 2 )]
        public Int32[] lightMapSize;

        [MarshalAs( UnmanagedType.ByValArray, SizeConst = 3 )]
        public Single[] origin; // Used for lightmaps and flares

        [MarshalAs( UnmanagedType.ByValArray, SizeConst = 3 )]
        public Single[] vec1; // lm vectors or mins

        [MarshalAs( UnmanagedType.ByValArray, SizeConst = 3 )]
        public Single[] vec2; // lm vectors or maxs

        [MarshalAs( UnmanagedType.ByValArray, SizeConst = 3 )]
        public Single[] normal; // for flat faces

        [MarshalAs( UnmanagedType.ByValArray, SizeConst = 2 )]
        public Int32[] patchSize;

        public static Int32 SizeInBytes = Marshal.SizeOf( typeof( Q3Face ) );
    }
}
