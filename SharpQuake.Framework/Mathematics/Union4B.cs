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

namespace SharpQuake.Framework
{
    [StructLayout( LayoutKind.Explicit )]
    public struct Union4b
    {
        [FieldOffset( 0 )]
        public UInt32 ui0;

        [FieldOffset( 0 )]
        public Int32 i0;

        [FieldOffset( 0 )]
        public Single f0;

        [FieldOffset( 0 )]
        public Int16 s0;

        [FieldOffset( 2 )]
        public Int16 s1;

        [FieldOffset( 0 )]
        public UInt16 us0;

        [FieldOffset( 2 )]
        public UInt16 us1;

        [FieldOffset( 0 )]
        public Byte b0;

        [FieldOffset( 1 )]
        public Byte b1;

        [FieldOffset( 2 )]
        public Byte b2;

        [FieldOffset( 3 )]
        public Byte b3;

        public static readonly Union4b Empty = new Union4b( 0, 0, 0, 0 );

        public Union4b( Byte b0, Byte b1, Byte b2, Byte b3 )
        {
            // Shut up compiler
            ui0 = 0;
            i0 = 0;
            f0 = 0;
            s0 = 0;
            s1 = 0;
            us0 = 0;
            us1 = 0;
            this.b0 = b0;
            this.b1 = b1;
            this.b2 = b2;
            this.b3 = b3;
        }
    }
}
