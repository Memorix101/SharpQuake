using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SharpQuake.Framework
{
    // leaf 0 is the generic CONTENTS_SOLID leaf, used for all solid areas
    // all other leafs need visibility info
    [StructLayout( LayoutKind.Sequential, Pack = 1 )]
    public struct BspLeaf
    {
        public System.Int32 contents;
        public System.Int32 visofs;				// -1 = no visibility info

        [MarshalAs( UnmanagedType.ByValArray, SizeConst = 3 )]
        public System.Int16[] mins;//[3];			// for frustum culling

        [MarshalAs( UnmanagedType.ByValArray, SizeConst = 3 )]
        public System.Int16[] maxs;//[3];

        public System.UInt16 firstmarksurface;
        public System.UInt16 nummarksurfaces;

        [MarshalAs( UnmanagedType.ByValArray, SizeConst = AmbientDef.NUM_AMBIENTS )]
        public System.Byte[] ambient_level; // [NUM_AMBIENTS];

        public static System.Int32 SizeInBytes = Marshal.SizeOf( typeof( BspLeaf ) );
    } // dleaf_t
}
