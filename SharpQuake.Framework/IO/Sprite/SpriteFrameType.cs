using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SharpQuake.Framework.IO.Sprite
{
    [StructLayout( LayoutKind.Sequential, Pack = 1 )]
    public struct dspriteframetype_t
    {
        public spriteframetype_t type;
    } // dspriteframetype_t;
}
