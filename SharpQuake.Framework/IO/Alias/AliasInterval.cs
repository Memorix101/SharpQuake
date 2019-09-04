using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SharpQuake.Framework.IO.Alias
{
    [StructLayout( LayoutKind.Sequential, Pack = 1 )]
    public struct daliasinterval_t
    {
        public Single interval;

        public static Int32 SizeInBytes = Marshal.SizeOf( typeof( daliasinterval_t ) );
    } // daliasinterval_t;
}
