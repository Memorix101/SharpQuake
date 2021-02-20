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

namespace SharpQuake.Framework
{
    public static class SwapHelper
    {
        public static Int16 ShortSwap( Int16 l )
        {
            Byte b1, b2;

            b1 = ( Byte ) ( l & 255 );
            b2 = ( Byte ) ( ( l >> 8 ) & 255 );

            return ( Int16 ) ( ( b1 << 8 ) + b2 );
        }

        public static Int32 LongSwap( Int32 l )
        {
            Byte b1, b2, b3, b4;

            b1 = ( Byte ) ( l & 255 );
            b2 = ( Byte ) ( ( l >> 8 ) & 255 );
            b3 = ( Byte ) ( ( l >> 16 ) & 255 );
            b4 = ( Byte ) ( ( l >> 24 ) & 255 );

            return ( ( Int32 ) b1 << 24 ) + ( ( Int32 ) b2 << 16 ) + ( ( Int32 ) b3 << 8 ) + b4;
        }

        public static Single FloatSwap( Single f )
        {
            var bytes = BitConverter.GetBytes( f );
            var bytes2 = new Byte[4];

            bytes2[0] = bytes[3];
            bytes2[1] = bytes[2];
            bytes2[2] = bytes[1];
            bytes2[3] = bytes[0];

            return BitConverter.ToSingle( bytes2, 0 );
        }

        public static void Swap4b( Byte[] buff, Int32 offset )
        {
            Byte b1, b2, b3, b4;

            b1 = buff[offset + 0];
            b2 = buff[offset + 1];
            b3 = buff[offset + 2];
            b4 = buff[offset + 3];

            buff[offset + 0] = b4;
            buff[offset + 1] = b3;
            buff[offset + 2] = b2;
            buff[offset + 3] = b1;
        }
    }
}
