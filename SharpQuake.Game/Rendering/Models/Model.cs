/// <copyright>
///
/// SharpQuakeEvolved changes by optimus-code, 2019
/// 
/// Based on SharpQuake (Quake Rewritten in C# by Yury Kiselev, 2010.)
///
/// Copyright (C) 1996-1997 Id Software, Inc.
///
/// This program is free software; you can redistribute it and/or
/// modify it under the terms of the GNU General Public License
/// as published by the Free Software Foundation; either version 2
/// of the License, or (at your option) any later version.
///
/// This program is distributed in the hope that it will be useful,
/// but WITHOUT ANY WARRANTY; without even the implied warranty of
/// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
///
/// See the GNU General Public License for more details.
///
/// You should have received a copy of the GNU General Public License
/// along with this program; if not, write to the Free Software
/// Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
/// </copyright>

using System;
using SharpQuake.Framework;
using SharpQuake.Framework.Mathematics;
using SharpQuake.Game.Rendering.Memory;
using SharpQuake.Game.Rendering.Textures;

namespace SharpQuake.Game.Rendering.Models
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
        public ModelTexture[] textures; // texture_t	**textures;

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
