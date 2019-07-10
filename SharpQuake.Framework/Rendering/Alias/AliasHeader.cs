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
using SharpQuake.Framework.Mathematics;

namespace SharpQuake.Framework
{
    [StructLayout( LayoutKind.Sequential, Pack = 1 )]
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
        [MarshalAs( UnmanagedType.ByValArray, SizeConst = ( ModelDef.MAX_SKINS * 4 ) )]
        public Int32[,] gl_texturenum; // int gl_texturenum[MAX_SKINS][4];
        /// <summary>
        /// Changed from integers (offsets from this header start) to objects to hold pointers to arrays of byte
        /// </summary>
        [MarshalAs( UnmanagedType.ByValArray, SizeConst = ModelDef.MAX_SKINS )]
        public Object[] texels; // int texels[MAX_SKINS];	// only for player skins
        public maliasframedesc_t[] frames; // maliasframedesc_t	frames[1];	// variable sized

        public static Int32 SizeInBytes = Marshal.SizeOf( typeof( aliashdr_t ) );

        public aliashdr_t( )
        {
            this.gl_texturenum = new Int32[ModelDef.MAX_SKINS, 4];//[];
            this.texels = new Object[ModelDef.MAX_SKINS];
        }
    } // aliashdr_t;
}
