using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SharpQuake.Framework.IO.Alias
{
    public enum aliasskintype_t
    {
        ALIAS_SKIN_SINGLE = 0,
        ALIAS_SKIN_GROUP
    } // aliasskintype_t;

    [StructLayout( LayoutKind.Sequential, Pack = 1 )]
    public struct daliasskintype_t
    {
        public aliasskintype_t type;

        public static Int32 SizeInBytes = Marshal.SizeOf( typeof( daliasskintype_t ) );
    } //daliasskintype_t;
}
