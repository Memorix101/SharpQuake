using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SharpQuake.Framework
{
    [StructLayout( LayoutKind.Sequential, Pack = 1 )]
    public struct BspVertex
    {
        [MarshalAs( UnmanagedType.ByValArray, SizeConst = 3 )]
        public System.Single[] point; //[3];

        public static System.Int32 SizeInBytes = Marshal.SizeOf( typeof( BspVertex ) );
    } // dvertex_t
}
