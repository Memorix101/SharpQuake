using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SharpQuake.Framework.IO.Alias
{
    [StructLayout( LayoutKind.Sequential, Pack = 1 )]
    public struct daliasskingroup_t
    {
        public Int32 numskins;

        public static Int32 SizeInBytes = Marshal.SizeOf( typeof( daliasskingroup_t ) );
    } // daliasskingroup_t;
}
