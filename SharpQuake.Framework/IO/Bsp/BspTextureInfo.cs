using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SharpQuake.Framework
{
    [StructLayout( LayoutKind.Sequential, Pack = 1 )]
    public struct BspTextureInfo
    {
        [MarshalAs( UnmanagedType.ByValArray, SizeConst = 8 )]
        public System.Single[] vecs; //[2][4];		// [s/t][xyz offset]

        public System.Int32 miptex;
        public System.Int32 flags;

        public static System.Int32 SizeInBytes = Marshal.SizeOf( typeof( BspTextureInfo ) );
    } // texinfo_t
}
