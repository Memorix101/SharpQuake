using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;

namespace SharpQuake.Framework
{
    //
    // in memory representation
    //
    // !!! if this is changed, it must be changed in asm_draw.h too !!!
    public struct MemoryVertex
    {
        public Vector3 position;
    } // mvertex_t;
}
