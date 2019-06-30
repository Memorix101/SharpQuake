using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SharpQuake.Framework
{
    [StructLayout( LayoutKind.Explicit, Size = ( 4 * 28 ) )]
    public struct PadInt28
    {
        //int pad[28];
    }
}
