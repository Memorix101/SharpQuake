using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SharpQuake.Framework
{
    [StructLayout( LayoutKind.Sequential, Pack = 1 )]
    public struct BspHeader
    {
        public System.Int32 version;

        [MarshalAs( UnmanagedType.ByValArray, SizeConst = BspDef.HEADER_LUMPS )]
        public BspLump[] lumps; //[HEADER_LUMPS];

        public static System.Int32 SizeInBytes = Marshal.SizeOf( typeof( BspHeader ) );
    } // dheader_t
}
