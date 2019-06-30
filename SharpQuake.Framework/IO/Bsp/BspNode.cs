using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SharpQuake.Framework
{
    // !!! if this is changed, it must be changed in asm_i386.h too !!!
    [StructLayout( LayoutKind.Sequential, Pack = 1 )]
    public struct BspNode
    {
        public System.Int32 planenum;

        [MarshalAs( UnmanagedType.ByValArray, SizeConst = 2 )]
        public System.Int16[] children;//[2];	// negative numbers are -(leafs+1), not nodes

        [MarshalAs( UnmanagedType.ByValArray, SizeConst = 3 )]
        public System.Int16[] mins; //[3];		// for sphere culling

        [MarshalAs( UnmanagedType.ByValArray, SizeConst = 3 )]
        public System.Int16[] maxs; //[3];

        public System.UInt16 firstface;
        public System.UInt16 numfaces;	// counting both sides

        public static System.Int32 SizeInBytes = Marshal.SizeOf( typeof( BspNode ) );
    } // dnode_t
}
