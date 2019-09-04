using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SharpQuake.Framework.IO.Alias
{
    [StructLayout( LayoutKind.Sequential, Pack = 1 )]
    public struct daliasframe_t
    {
        public trivertx_t bboxmin;	// lightnormal isn't used
        public trivertx_t bboxmax;	// lightnormal isn't used
        [MarshalAs( UnmanagedType.ByValArray, SizeConst = 16 )]
        public Byte[] name; // char[16]	// frame name from grabbing

        public static Int32 SizeInBytes = Marshal.SizeOf( typeof( daliasframe_t ) );
    } // daliasframe_t;
}
