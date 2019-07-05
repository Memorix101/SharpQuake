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
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SharpQuake.Framework
{
    // leaf 0 is the generic CONTENTS_SOLID leaf, used for all solid areas
    // all other leafs need visibility info
    [StructLayout( LayoutKind.Sequential, Pack = 1 )]
    public struct BspLeaf
    {
        public System.Int32 contents;
        public System.Int32 visofs;				// -1 = no visibility info

        [MarshalAs( UnmanagedType.ByValArray, SizeConst = 3 )]
        public System.Int16[] mins;//[3];			// for frustum culling

        [MarshalAs( UnmanagedType.ByValArray, SizeConst = 3 )]
        public System.Int16[] maxs;//[3];

        public System.UInt16 firstmarksurface;
        public System.UInt16 nummarksurfaces;

        [MarshalAs( UnmanagedType.ByValArray, SizeConst = AmbientDef.NUM_AMBIENTS )]
        public System.Byte[] ambient_level; // [NUM_AMBIENTS];

        public static System.Int32 SizeInBytes = Marshal.SizeOf( typeof( BspLeaf ) );
    } // dleaf_t
}
