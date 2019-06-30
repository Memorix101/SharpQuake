using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SharpQuake.Framework
{
    [StructLayout( LayoutKind.Sequential, Pack = 1 )]
    public struct BspFace
    {
        public System.Int16 planenum;
        public System.Int16 side;

        public System.Int32 firstedge;		// we must support > 64k edges
        public System.Int16 numedges;
        public System.Int16 texinfo;

        // lighting info
        [MarshalAs( UnmanagedType.ByValArray, SizeConst = BspDef.MAXLIGHTMAPS )]
        public System.Byte[] styles; //[MAXLIGHTMAPS];

        public System.Int32 lightofs;		// start of [numstyles*surfsize] samples

        public static System.Int32 SizeInBytes = Marshal.SizeOf( typeof( BspFace ) );
    } // dface_t
}
