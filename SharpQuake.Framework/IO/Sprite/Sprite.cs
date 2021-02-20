using System;
using System.Runtime.InteropServices;

namespace SharpQuake.Framework.IO.Sprite
{
	[StructLayout( LayoutKind.Sequential, Pack = 1 )]
    public struct dsprite_t
    {
        public Int32 ident;
        public Int32 version;
        public Int32 type;
        public Single boundingradius;
        public Int32 width;
        public Int32 height;
        public Int32 numframes;
        public Single beamlength;
        public SyncType synctype;

        public static Int32 SizeInBytes = Marshal.SizeOf( typeof( dsprite_t ) );
    } // dsprite_t;
}
