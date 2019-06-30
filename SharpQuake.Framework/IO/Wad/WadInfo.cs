using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SharpQuake.Framework
{
    [StructLayout( LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi )]
    public struct WadInfo
    {
        [MarshalAs( UnmanagedType.ByValArray, SizeConst = 4 )]
        public Byte[] identification; // [4];		// should be WAD2 or 2DAW

        public Int32 numlumps;
        public Int32 infotableofs;
    } // wadinfo_t
}
