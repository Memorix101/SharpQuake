using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SharpQuake.Framework
{
    [StructLayout( LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi )]
    public class WadLumpInfo
    {
        public Int32 filepos;
        public Int32 disksize;
        public Int32 size;                   // uncompressed
        public Byte type;
        public Byte compression;
        private Byte pad1, pad2;

        [MarshalAs( UnmanagedType.ByValArray, SizeConst = 16 )]
        public Byte[] name; //[16];				// must be null terminated
    } // lumpinfo_t;
}
