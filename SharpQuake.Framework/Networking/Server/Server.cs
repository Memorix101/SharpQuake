using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpQuake.Framework
{
    public enum server_state_t
    {
        Loading,
        Active
    }

    //=============================================================================

    // server_state_t;

    public class server_t
    {
        public Boolean active;             // false if only a net client
        public Boolean paused;
        public Boolean loadgame;           // handle connections specially
        public Double time;
        public Int32 lastcheck;           // used by PF_checkclient
        public Double lastchecktime;
        public String name;// char		name[64];			// map name
        public String modelname;// char		modelname[64];		// maps/<name>.bsp, for model_precache[0]
        public Model worldmodel;
        public String[] model_precache; //[MAX_MODELS];	// NULL terminated
        public Model[] models; //[MAX_MODELS];
        public String[] sound_precache; //[MAX_SOUNDS];	// NULL terminated
        public String[] lightstyles; // [MAX_LIGHTSTYLES];
        public Int32 num_edicts;
        public Int32 max_edicts;
        public MemoryEdict[] edicts;        // can NOT be array indexed, because

        // edict_t is variable sized, but can
        // be used to reference the world ent
        public server_state_t state;			// some actions are only valid during load

        public MessageWriter datagram;
        public MessageWriter reliable_datagram; // copied to all clients at end of frame
        public MessageWriter signon;

        public void Clear( )
        {
            this.active = false;
            this.paused = false;
            this.loadgame = false;
            this.time = 0;
            this.lastcheck = 0;
            this.lastchecktime = 0;
            this.name = null;
            this.modelname = null;
            this.worldmodel = null;
            Array.Clear( this.model_precache, 0, this.model_precache.Length );
            Array.Clear( this.models, 0, this.models.Length );
            Array.Clear( this.sound_precache, 0, this.sound_precache.Length );
            Array.Clear( this.lightstyles, 0, this.lightstyles.Length );
            this.num_edicts = 0;
            this.max_edicts = 0;
            this.edicts = null;
            this.state = 0;
            this.datagram.Clear( );
            this.reliable_datagram.Clear( );
            this.signon.Clear( );
            GC.Collect( );
        }

        public server_t( )
        {
            this.model_precache = new String[QDef.MAX_MODELS];
            this.models = new Model[QDef.MAX_MODELS];
            this.sound_precache = new String[QDef.MAX_SOUNDS];
            this.lightstyles = new String[QDef.MAX_LIGHTSTYLES];
            this.datagram = new MessageWriter( QDef.MAX_DATAGRAM );
            this.reliable_datagram = new MessageWriter( QDef.MAX_DATAGRAM );
            this.signon = new MessageWriter( 8192 );
        }
    }// server_t;
}
