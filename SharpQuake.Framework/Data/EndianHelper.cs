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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;

namespace SharpQuake.Framework
{
    public static class EndianHelper
    {
        private static IByteOrderConverter _Converter;

        public static IByteOrderConverter Converter
        {
            get
            {
                return _Converter;
            }
        }

        public static Boolean IsBigEndian
        {
            get
            {
                return !BitConverter.IsLittleEndian;
            }
        }

        static EndianHelper( )
        {
            // set the byte swapping variables in a portable manner
            if ( BitConverter.IsLittleEndian )
            {
                _Converter = new LittleEndianConverter( );
            }
            else
            {
                _Converter = new BigEndianConverter( );
            }
        }

        public static Int16 BigShort( Int16 l )
        {
            return _Converter.BigShort( l );
        }

        public static Int16 LittleShort( Int16 l )
        {
            return _Converter.LittleShort( l );
        }

        public static Int32 BigLong( Int32 l )
        {
            return _Converter.BigLong( l );
        }

        public static Int32 LittleLong( Int32 l )
        {
            return _Converter.LittleLong( l );
        }

        public static Single BigFloat( Single l )
        {
            return _Converter.BigFloat( l );
        }

        public static Single LittleFloat( Single l )
        {
            return _Converter.LittleFloat( l );
        }

        public static Vector3 LittleVector( Vector3 src )
        {
            return new Vector3( _Converter.LittleFloat( src.X ),
                _Converter.LittleFloat( src.Y ), _Converter.LittleFloat( src.Z ) );
        }

        public static Vector3 LittleVector3( Single[] src )
        {
            return new Vector3( _Converter.LittleFloat( src[0] ),
                _Converter.LittleFloat( src[1] ), _Converter.LittleFloat( src[2] ) );
        }

        public static Vector4 LittleVector4( Single[] src, Int32 offset )
        {
            return new Vector4( _Converter.LittleFloat( src[offset + 0] ),
                _Converter.LittleFloat( src[offset + 1] ),
                _Converter.LittleFloat( src[offset + 2] ),
                _Converter.LittleFloat( src[offset + 3] ) );
        }

    }
}
