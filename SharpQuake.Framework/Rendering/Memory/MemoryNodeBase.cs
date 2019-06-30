using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;

namespace SharpQuake.Framework
{
    // commmon part of mnode_t and mleaf_t
    public class MemoryNodeBase
    {
        public Int32 contents;		// 0 for mnode_t and negative for mleaf_t
        public Int32 visframe;		// node needs to be traversed if current
        public Vector3 mins;
        public Vector3 maxs;
        //public float[] minmaxs; //[6];		// for bounding box culling
        public MemoryNode parent;

        //public mnodebase_t()
        //{
        //    this.minmaxs = new float[6];
        //}
    } // mnodebase_t
}
