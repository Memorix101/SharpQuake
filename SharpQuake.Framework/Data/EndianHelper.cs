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
