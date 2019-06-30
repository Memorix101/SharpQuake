using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpQuake.Framework
{
    public class glpoly_t
    {
        public glpoly_t next;
        public glpoly_t chain;
        public Int32 numverts;
        public Int32 flags;			// for SURF_UNDERWATER
        /// <summary>
        /// Changed! Original Quake glpoly_t has 4 vertex inplace and others immidiately after this struct
        /// Now all vertices are in verts array of size [numverts,VERTEXSIZE]
        /// </summary>
        public Single[][] verts; //[4][VERTEXSIZE];	// variable sized (xyz s1t1 s2t2)

        public void Clear( )
        {
            this.next = null;
            this.chain = null;
            this.numverts = 0;
            this.flags = 0;
            this.verts = null;
        }

        public void AllocVerts( Int32 count )
        {
            this.numverts = count;
            this.verts = new Single[count][];
            for ( var i = 0; i < count; i++ )
                this.verts[i] = new Single[ModelDef.VERTEXSIZE];
        }
    } //glpoly_t;
}
