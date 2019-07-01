using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;

namespace SharpQuake.Framework
{
    public class Model
    {
        public String name; // char		name[MAX_QPATH];
        public Boolean needload;		// bmodels and sprites don't cache normally

        public ModelType type;
        public Int32 numframes;
        public SyncType synctype;

        public Int32 flags;

        //
        // volume occupied by the model graphics
        //		
        public Vector3 mins, maxs;
        public Single radius;

        //
        // solid volume for clipping 
        //
        public Boolean clipbox;
        public Vector3 clipmins, clipmaxs;

        //
        // brush model
        //
        public Int32 firstmodelsurface, nummodelsurfaces;

        public Int32 numsubmodels;
        public BspModel[] submodels;

        public Int32 numplanes;
        public Plane[] planes; // mplane_t*

        public Int32 numleafs;		// number of visible leafs, not counting 0
        public MemoryLeaf[] leafs; // mleaf_t*

        public Int32 numvertexes;
        public MemoryVertex[] vertexes; // mvertex_t*

        public Int32 numedges;
        public MemoryEdge[] edges; // medge_t*

        public Int32 numnodes;
        public MemoryNode[] nodes; // mnode_t *nodes;

        public Int32 numtexinfo;
        public MemoryTextureInfo[] texinfo;

        public Int32 numsurfaces;
        public MemorySurface[] surfaces;

        public Int32 numsurfedges;
        public Int32[] surfedges; // int *surfedges;

        public Int32 numclipnodes;
        public BspClipNode[] clipnodes; // public dclipnode_t* clipnodes;

        public Int32 nummarksurfaces;
        public MemorySurface[] marksurfaces; // msurface_t **marksurfaces;

        public BspHull[] hulls; // [MAX_MAP_HULLS];

        public Int32 numtextures;
        public Texture[] textures; // texture_t	**textures;

        public Byte[] visdata; // byte *visdata;
        public Byte[] lightdata; // byte		*lightdata;
        public String entities; // char		*entities

        //
        // additional model data
        //
        public CacheUser cache; // cache_user_t	cache		// only access through Mod_Extradata

        public Model( )
        {
            this.hulls = new BspHull[BspDef.MAX_MAP_HULLS];
            for ( var i = 0; i < this.hulls.Length; i++ )
                this.hulls[i] = new BspHull( );
        }

        public void Clear( )
        {
            this.name = null;
            this.needload = false;
            this.type = 0;
            this.numframes = 0;
            this.synctype = 0;
            this.flags = 0;
            this.mins = Vector3.Zero;
            this.maxs = Vector3.Zero;
            this.radius = 0;
            this.clipbox = false;
            this.clipmins = Vector3.Zero;
            this.clipmaxs = Vector3.Zero;
            this.firstmodelsurface = 0;
            this.nummodelsurfaces = 0;

            this.numsubmodels = 0;
            this.submodels = null;

            this.numplanes = 0;
            this.planes = null;

            this.numleafs = 0;
            this.leafs = null;

            this.numvertexes = 0;
            this.vertexes = null;

            this.numedges = 0;
            this.edges = null;

            this.numnodes = 0;
            this.nodes = null;

            this.numtexinfo = 0;
            this.texinfo = null;

            this.numsurfaces = 0;
            this.surfaces = null;

            this.numsurfedges = 0;
            this.surfedges = null;

            this.numclipnodes = 0;
            this.clipnodes = null;

            this.nummarksurfaces = 0;
            this.marksurfaces = null;

            foreach ( var h in this.hulls )
                h.Clear( );

            this.numtextures = 0;
            this.textures = null;

            this.visdata = null;
            this.lightdata = null;
            this.entities = null;

            this.cache = null;
        }

        public void CopyFrom( Model src )
        {
            this.name = src.name;
            this.needload = src.needload;
            this.type = src.type;
            this.numframes = src.numframes;
            this.synctype = src.synctype;
            this.flags = src.flags;
            this.mins = src.mins;
            this.maxs = src.maxs;
            this.radius = src.radius;
            this.clipbox = src.clipbox;
            this.clipmins = src.clipmins;
            this.clipmaxs = src.clipmaxs;
            this.firstmodelsurface = src.firstmodelsurface;
            this.nummodelsurfaces = src.nummodelsurfaces;

            this.numsubmodels = src.numsubmodels;
            this.submodels = src.submodels;

            this.numplanes = src.numplanes;
            this.planes = src.planes;

            this.numleafs = src.numleafs;
            this.leafs = src.leafs;

            this.numvertexes = src.numvertexes;
            this.vertexes = src.vertexes;

            this.numedges = src.numedges;
            this.edges = src.edges;

            this.numnodes = src.numnodes;
            this.nodes = src.nodes;

            this.numtexinfo = src.numtexinfo;
            this.texinfo = src.texinfo;

            this.numsurfaces = src.numsurfaces;
            this.surfaces = src.surfaces;

            this.numsurfedges = src.numsurfedges;
            this.surfedges = src.surfedges;

            this.numclipnodes = src.numclipnodes;
            this.clipnodes = src.clipnodes;

            this.nummarksurfaces = src.nummarksurfaces;
            this.marksurfaces = src.marksurfaces;

            for ( var i = 0; i < src.hulls.Length; i++ )
            {
                this.hulls[i].CopyFrom( src.hulls[i] );
            }

            this.numtextures = src.numtextures;
            this.textures = src.textures;

            this.visdata = src.visdata;
            this.lightdata = src.lightdata;
            this.entities = src.entities;

            this.cache = src.cache;
        }
    } //model_t;
}
