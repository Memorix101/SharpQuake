using System;
using System.Runtime.InteropServices;

namespace SharpQuake.Framework.IO.Sprite
{
	[StructLayout( LayoutKind.Sequential, Pack = 1 )]
    public struct dspritegroup_t
    {
        public Int32 numframes;

        public static Int32 SizeInBytes = Marshal.SizeOf( typeof( dspritegroup_t ) );
    } // dspritegroup_t;
}
