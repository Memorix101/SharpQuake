using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SharpQuake.Framework
{
    [StructLayout( LayoutKind.Sequential, Pack = 1 )]
    public struct BspMipTexLump
    {
        public System.Int32 nummiptex;
        //[MarshalAs(UnmanagedType.ByValArray, SizeConst=4)]
        //public int[] dataofs; // [nummiptex]

        public static System.Int32 SizeInBytes = Marshal.SizeOf( typeof( BspMipTexLump ) );
    } // dmiptexlump_t
}
