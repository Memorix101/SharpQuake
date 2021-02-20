using System;
using System.Runtime.InteropServices;

namespace SharpQuake.Framework.IO.Alias
{
	[StructLayout( LayoutKind.Sequential, Pack = 1 )]
    public struct daliasframe_t
    {
        public trivertx_t bboxmin;	// lightnormal isn't used
        public trivertx_t bboxmax;	// lightnormal isn't used
        [MarshalAs( UnmanagedType.ByValArray, SizeConst = 16 )]
        public Byte[] name; // char[16]	// frame name from grabbing

        public static Int32 SizeInBytes = Marshal.SizeOf( typeof( daliasframe_t ) );
    } // daliasframe_t;
}
