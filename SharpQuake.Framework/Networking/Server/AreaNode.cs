using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpQuake.Framework
{
    public class areanode_t
    {
        public Int32 axis;		// -1 = leaf node
        public Single dist;
        public areanode_t[] children; // [2];
        public Link trigger_edicts;
        public Link solid_edicts;

        public void Clear( )
        {
            this.axis = 0;
            this.dist = 0;
            this.children[0] = null;
            this.children[1] = null;
            this.trigger_edicts.ClearToNulls( );
            this.solid_edicts.ClearToNulls( );
        }

        public areanode_t( )
        {
            this.children = new areanode_t[2];
            this.trigger_edicts = new Link( this );
            this.solid_edicts = new Link( this );
        }
    } //areanode_t;
}
