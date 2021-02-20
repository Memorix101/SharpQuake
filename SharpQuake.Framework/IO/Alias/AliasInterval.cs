using System;
using System.Runtime.InteropServices;

namespace SharpQuake.Framework.IO.Alias
{
	[StructLayout( LayoutKind.Sequential, Pack = 1 )]
    public struct daliasinterval_t
    {
        public Single interval;

        public static Int32 SizeInBytes = Marshal.SizeOf( typeof( daliasinterval_t ) );
    } // daliasinterval_t;
}
