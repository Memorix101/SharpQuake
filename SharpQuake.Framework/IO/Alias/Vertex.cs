using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SharpQuake.Framework.IO.Alias
{
    [StructLayout( LayoutKind.Sequential, Pack = 1 )]
    public struct stvert_t
    {
        public Int32 onseam;
        public Int32 s;
        public Int32 t;

        public static Int32 SizeInBytes = Marshal.SizeOf( typeof( stvert_t ) );
    } // stvert_t;

}
