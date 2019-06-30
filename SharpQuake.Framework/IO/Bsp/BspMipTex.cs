using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SharpQuake.Framework
{
    [StructLayout( LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi )]
    public struct BspMipTex
    {
        [MarshalAs( UnmanagedType.ByValArray, SizeConst = 16 )]
        public System.Byte[] name; //[16];

        public System.UInt32 width, height;

        [MarshalAs( UnmanagedType.ByValArray, SizeConst = BspDef.MIPLEVELS )]
        public System.UInt32[] offsets; //[MIPLEVELS];		// four mip maps stored

        public static System.Int32 SizeInBytes = Marshal.SizeOf( typeof( BspMipTex ) );
    } // miptex_t
}
