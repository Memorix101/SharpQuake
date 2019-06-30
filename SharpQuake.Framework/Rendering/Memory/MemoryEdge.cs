using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpQuake.Framework
{
    // !!! if this is changed, it must be changed in asm_draw.h too !!!
    public struct MemoryEdge
    {
        public UInt16[] v; // [2];
        //public uint cachededgeoffset;
    } //medge_t;
}
