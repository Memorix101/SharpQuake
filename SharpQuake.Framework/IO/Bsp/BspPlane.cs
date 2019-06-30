using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SharpQuake.Framework
{
    [StructLayout( LayoutKind.Sequential, Pack = 1 )]
    public struct BspPlane
    {
        [MarshalAs( UnmanagedType.ByValArray, SizeConst = 3 )]
        public System.Single[] normal; //[3];

        public System.Single dist;
        public System.Int32 type;		// PLANE_X - PLANE_ANYZ ?remove? trivial to regenerate

        public static System.Int32 SizeInBytes = Marshal.SizeOf( typeof( BspPlane ) );
    } // dplane_t
}
