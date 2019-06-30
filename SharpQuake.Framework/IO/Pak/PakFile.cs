using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SharpQuake.Framework
{
    //
    // on disk
    //
    [StructLayout( LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi )]
    public struct PakFile
    {
        [MarshalAs( UnmanagedType.ByValArray, SizeConst = 56 )]
        public Byte[] name; // [56];

        public Int32 filepos, filelen;
    } // dpackfile_t
}
