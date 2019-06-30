using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SharpQuake.Framework
{
    [StructLayout( LayoutKind.Sequential, Pack = 1 )]
    public struct BspClipNode
    {
        public System.Int32 planenum;

        [MarshalAs( UnmanagedType.ByValArray, SizeConst = 2 )]
        public System.Int16[] children; //[2];	// negative numbers are contents

        public static System.Int32 SizeInBytes = Marshal.SizeOf( typeof( BspClipNode ) );
    } // dclipnode_t
}
