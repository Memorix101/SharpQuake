using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpQuake
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
            Byte[] bytes = BitConverter.GetBytes( f );
            Byte[] bytes2 = new Byte[4];

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
