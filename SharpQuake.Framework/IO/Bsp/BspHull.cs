using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;

namespace SharpQuake.Framework
{
    // !!! if this is changed, it must be changed in asm_i386.h too !!!
    public class BspHull
    {
        public BspClipNode[] clipnodes;
        public Plane[] planes;
        public Int32 firstclipnode;
        public Int32 lastclipnode;
        public Vector3 clip_mins;
        public Vector3 clip_maxs;

        public void Clear( )
        {
            this.clipnodes = null;
            this.planes = null;
            this.firstclipnode = 0;
            this.lastclipnode = 0;
            this.clip_mins = Vector3.Zero;
            this.clip_maxs = Vector3.Zero;
        }

        public void CopyFrom( BspHull src )
        {
            this.clipnodes = src.clipnodes;
            this.planes = src.planes;
            this.firstclipnode = src.firstclipnode;
            this.lastclipnode = src.lastclipnode;
            this.clip_mins = src.clip_mins;
            this.clip_maxs = src.clip_maxs;
        }
    } // hull_t;
}
