using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SharpQuake
{
    [StructLayout( LayoutKind.Explicit )]
    public struct Union4B
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

        public static readonly Union4B Empty = new Union4B( 0, 0, 0, 0 );

        public Union4B( Byte b0, Byte b1, Byte b2, Byte b3 )
        {
            // Shut up compiler
            this.ui0 = 0;
            this.i0 = 0;
            this.f0 = 0;
            this.s0 = 0;
            this.s1 = 0;
            this.us0 = 0;
            this.us1 = 0;
            this.b0 = b0;
            this.b1 = b1;
            this.b2 = b2;
            this.b3 = b3;
        }
    }
}
