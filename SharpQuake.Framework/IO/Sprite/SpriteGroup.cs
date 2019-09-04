using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SharpQuake.Framework.IO.Sprite
{
    [StructLayout( LayoutKind.Sequential, Pack = 1 )]
    public struct dspritegroup_t
    {
        public Int32 numframes;

        public static Int32 SizeInBytes = Marshal.SizeOf( typeof( dspritegroup_t ) );
    } // dspritegroup_t;
}
