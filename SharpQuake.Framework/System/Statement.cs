using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

using string_t = System.Int32;

namespace SharpQuake.Framework
{
    [StructLayout( LayoutKind.Sequential, Pack = 1 )]
    public struct Statement
    {
        public UInt16 op;
        public Int16 a, b, c;

        public static string_t SizeInBytes = Marshal.SizeOf( typeof( Statement ) );

        public void SwapBytes( )
        {
            this.op = ( UInt16 ) EndianHelper.LittleShort( ( Int16 ) this.op );
            this.a = EndianHelper.LittleShort( this.a );
            this.b = EndianHelper.LittleShort( this.b );
            this.c = EndianHelper.LittleShort( this.c );
        }
    } // dstatement_t
}
