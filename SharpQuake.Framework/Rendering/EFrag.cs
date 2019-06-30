using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpQuake.Framework
{
    public class EFrag
    {
        public MemoryLeaf leaf;
        public EFrag leafnext;
        public Entity entity;
        public EFrag entnext;

        public void Clear( )
        {
            this.leaf = null;
            this.leafnext = null;
            this.entity = null;
            this.entnext = null;
        }
    } // efrag_t;
}
