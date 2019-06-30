using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;

namespace SharpQuake.Framework
{
    public class Entity
    {
        public System.Boolean forcelink;		// model changed
        public Int32 update_type;
        public EntityState baseline;		// to fill in defaults in updates
        public Double msgtime;		// time of last update
        public Vector3[] msg_origins; //[2];	// last two updates (0 is newest)
        public Vector3 origin;
        public Vector3[] msg_angles; //[2];	// last two updates (0 is newest)
        public Vector3 angles;
        public Model model;			// NULL = no model
        public EFrag efrag;			// linked list of efrags
        public Int32 frame;
        public Single syncbase;		// for client-side animations
        public Byte[] colormap;
        public Int32 effects;		// light, particals, etc
        public Int32 skinnum;		// for Alias models
        public Int32 visframe;		// last frame this entity was
        //  found in an active leaf

        public Int32 dlightframe;	// dynamic lighting
        public Int32 dlightbits;

        // FIXME: could turn these into a union
        public Int32 trivial_accept;

        public MemoryNode topnode;		// for bmodels, first world node
        //  that splits bmodel, or NULL if
        //  not split

        public void Clear( )
        {
            this.forcelink = false;
            this.update_type = 0;

            this.baseline = EntityState.Empty;

            this.msgtime = 0;
            this.msg_origins[0] = Vector3.Zero;
            this.msg_origins[1] = Vector3.Zero;

            this.origin = Vector3.Zero;
            this.msg_angles[0] = Vector3.Zero;
            this.msg_angles[1] = Vector3.Zero;
            this.angles = Vector3.Zero;
            this.model = null;
            this.efrag = null;
            this.frame = 0;
            this.syncbase = 0;
            this.colormap = null;
            this.effects = 0;
            this.skinnum = 0;
            this.visframe = 0;

            this.dlightframe = 0;
            this.dlightbits = 0;

            this.trivial_accept = 0;
            this.topnode = null;
        }

        public Entity( )
        {
            msg_origins = new Vector3[2];
            msg_angles = new Vector3[2];
        }
    } // entity_t;
}
