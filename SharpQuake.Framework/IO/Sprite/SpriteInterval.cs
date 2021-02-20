using System;
using System.Runtime.InteropServices;

namespace SharpQuake.Framework.IO.Sprite
{
	[StructLayout( LayoutKind.Sequential, Pack = 1 )]
    public struct dspriteinterval_t
    {
        public Single interval;

        public static Int32 SizeInBytes = Marshal.SizeOf( typeof( dspriteinterval_t ) );
    } // dspriteinterval_t;
}
