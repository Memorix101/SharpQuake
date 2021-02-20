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
using SharpQuake.Framework.Mathematics;

namespace SharpQuake.Framework
{
    // MSG_ReadXxx() functions
    public class MessageReader
    {
        /// <summary>
        /// msg_badread
        /// </summary>
        public Boolean IsBadRead
        {
            get
            {
                return _IsBadRead;
            }
        }

        /// <summary>
        /// msg_readcount
        /// </summary>
        public Int32 Position
        {
            get
            {
                return _Count;
            }
        }

        private MessageWriter _Source;
        private Boolean _IsBadRead;
        private Int32 _Count;
        private Union4b _Val;
        private Char[] _Tmp;

        /// <summary>
        /// MSG_BeginReading
        /// </summary>
        public void Reset( )
        {
            _IsBadRead = false;
            _Count = 0;
        }

        /// <summary>
        /// MSG_ReadChar
        /// reads sbyte
        /// </summary>
        public Int32 ReadChar( )
        {
            if ( !HasRoom( 1 ) )
                return -1;

            return ( SByte ) _Source.Data[_Count++];
        }

        // MSG_ReadByte (void)
        public Int32 ReadByte( )
        {
            if ( !HasRoom( 1 ) )
                return -1;

            return ( Byte ) _Source.Data[_Count++];
        }

        // MSG_ReadShort (void)
        public Int32 ReadShort( )
        {
            if ( !HasRoom( 2 ) )
                return -1;

            Int32 c = ( Int16 ) ( _Source.Data[_Count + 0] + ( _Source.Data[_Count + 1] << 8 ) );
            _Count += 2;
            return c;
        }

        // MSG_ReadLong (void)
        public Int32 ReadLong( )
        {
            if ( !HasRoom( 4 ) )
                return -1;

            var c = _Source.Data[_Count + 0] +
                ( _Source.Data[_Count + 1] << 8 ) +
                ( _Source.Data[_Count + 2] << 16 ) +
                ( _Source.Data[_Count + 3] << 24 );

            _Count += 4;
            return c;
        }

        // MSG_ReadFloat (void)
        public Single ReadFloat( )
        {
            if ( !HasRoom( 4 ) )
                return 0;

            _Val.b0 = _Source.Data[_Count + 0];
            _Val.b1 = _Source.Data[_Count + 1];
            _Val.b2 = _Source.Data[_Count + 2];
            _Val.b3 = _Source.Data[_Count + 3];

            _Count += 4;

            _Val.i0 = EndianHelper.LittleLong( _Val.i0 );
            return _Val.f0;
        }

        // char *MSG_ReadString (void)
        public String ReadString( )
        {
            var l = 0;
            do
            {
                var c = ReadChar( );
                if ( c == -1 || c == 0 )
                    break;
                _Tmp[l] = ( Char ) c;
                l++;
            } while ( l < _Tmp.Length - 1 );

            return new String( _Tmp, 0, l );
        }

        // float MSG_ReadCoord (void)
        public Single ReadCoord( )
        {
            return ReadShort( ) * ( 1.0f / 8 );
        }

        // float MSG_ReadAngle (void)
        public Single ReadAngle( )
        {
            return ReadChar( ) * ( 360.0f / 256 );
        }

        public Vector3 ReadCoords( )
        {
            Vector3 result;
            result.X = ReadCoord( );
            result.Y = ReadCoord( );
            result.Z = ReadCoord( );
            return result;
        }

        public Vector3 ReadAngles( )
        {
            Vector3 result;
            result.X = ReadAngle( );
            result.Y = ReadAngle( );
            result.Z = ReadAngle( );
            return result;
        }

        private Boolean HasRoom( Int32 bytes )
        {
            if ( _Count + bytes > _Source.Length )
            {
                _IsBadRead = true;
                return false;
            }
            return true;
        }

        public MessageReader( MessageWriter source )
        {
            _Source = source;
            _Val = Union4b.Empty;
            _Tmp = new Char[2048];
        }
    }
}
