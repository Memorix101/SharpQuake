using System;
using System.Runtime.InteropServices;

namespace SharpQuake.Framework.IO.Alias
{
	[StructLayout( LayoutKind.Sequential )]
    public struct dtriangle_t
    {
        public Int32 facesfront;
        [MarshalAs( UnmanagedType.ByValArray, ArraySubType = UnmanagedType.I4, SizeConst = 3 )]
        public Int32[] vertindex; // int vertindex[3];

        public static Int32 SizeInBytes = Marshal.SizeOf( typeof( dtriangle_t ) );
    } // dtriangle_t;
}
