using System;
using System.Runtime.InteropServices;

namespace SharpQuake.Framework.IO.Alias
{
	public enum aliasskintype_t
    {
        ALIAS_SKIN_SINGLE = 0,
        ALIAS_SKIN_GROUP
    } // aliasskintype_t;

    [StructLayout( LayoutKind.Sequential, Pack = 1 )]
    public struct daliasskintype_t
    {
        public aliasskintype_t type;

        public static Int32 SizeInBytes = Marshal.SizeOf( typeof( daliasskintype_t ) );
    } //daliasskintype_t;
}
