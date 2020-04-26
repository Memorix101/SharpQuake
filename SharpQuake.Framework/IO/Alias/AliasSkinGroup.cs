using System;
using System.Runtime.InteropServices;

namespace SharpQuake.Framework.IO.Alias
{
	[StructLayout( LayoutKind.Sequential, Pack = 1 )]
    public struct daliasskingroup_t
    {
        public Int32 numskins;

        public static Int32 SizeInBytes = Marshal.SizeOf( typeof( daliasskingroup_t ) );
    } // daliasskingroup_t;
}
