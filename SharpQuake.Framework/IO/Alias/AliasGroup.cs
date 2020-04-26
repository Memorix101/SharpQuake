using System;
using System.Runtime.InteropServices;

namespace SharpQuake.Framework.IO.Alias
{
	[StructLayout( LayoutKind.Sequential, Pack = 1 )]
    public struct daliasgroup_t
    {
        public Int32 numframes;
        public trivertx_t bboxmin;	// lightnormal isn't used
        public trivertx_t bboxmax;	// lightnormal isn't used

        public static Int32 SizeInBytes = Marshal.SizeOf( typeof( daliasgroup_t ) );
    } // daliasgroup_t;
}
