using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SharpQuake.Framework
{
    // note that edge 0 is never used, because negative edge nums are used for
    // counterclockwise use of the edge in a face
    [StructLayout( LayoutKind.Sequential, Pack = 1 )]
    public struct BspEdge
    {
        [MarshalAs( UnmanagedType.ByValArray, SizeConst = 2 )]
        public System.UInt16[] v; //[2];		// vertex numbers

        public static System.Int32 SizeInBytes = Marshal.SizeOf( typeof( BspEdge ) );
    } // dedge_t
}
