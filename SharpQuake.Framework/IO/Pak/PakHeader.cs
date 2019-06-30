using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SharpQuake.Framework
{
    [StructLayout( LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi )]
    public struct PakHeader
    {
        [MarshalAs( UnmanagedType.ByValArray, SizeConst = 4 )]
        public Byte[] id; // [4];

        [MarshalAs( UnmanagedType.I4, SizeConst = 4 )]
        public Int32 dirofs;

        [MarshalAs( UnmanagedType.I4, SizeConst = 4 )]
        public Int32 dirlen;
    } // dpackheader_t
}
