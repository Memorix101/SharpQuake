using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SharpQuake.Framework.IO.Sprite
{
    [StructLayout( LayoutKind.Sequential, Pack = 1 )]
    public struct dspriteinterval_t
    {
        public Single interval;

        public static Int32 SizeInBytes = Marshal.SizeOf( typeof( dspriteinterval_t ) );
    } // dspriteinterval_t;
}
