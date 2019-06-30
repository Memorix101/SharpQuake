using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpQuake.Framework
{
    public class MemoryNode : MemoryNodeBase
    {
        // node specific
        public Plane plane;
        public MemoryNodeBase[] children; //[2];	

        public UInt16 firstsurface;
        public UInt16 numsurfaces;

        public MemoryNode( )
        {
            this.children = new MemoryNodeBase[2];
        }
    } //mnode_t;
}
