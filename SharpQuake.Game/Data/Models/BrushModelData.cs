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
using System.Drawing;
using System.Linq;
using System.Text;
using SharpQuake.Framework;
using SharpQuake.Framework.IO.BSP;
using SharpQuake.Framework.IO.WAD;
using SharpQuake.Framework.Mathematics;
using SharpQuake.Game.Rendering.Memory;
using SharpQuake.Game.Rendering.Textures;

namespace SharpQuake.Game.Data.Models
{
	public class BrushModelData : ModelData
    {
        private Int32 Version
        {
            get;
            set;
        }

        private Int32 BaseOffset
        {
            get;
            set;
        }

        private Q1Header Q1Header
        {
            get;
            set;
        }

        private Q2Header Q2Header
        {
            get;
            set;
        }

        private Q3Header Q3Header
        {
            get;
            set;
        }

        //
        // brush model
        //
        public Int32 FirstModelSurface
        {
            get;
            set;
        }
        
        public Int32 NumModelSurfaces
        {
            get;
            set;
        }

        public Q1Model[] SubModels
        {
            get;
            set;
        }

        public Int32 NumSubModels
        {
            get;
            set;
        }

        public Plane[] Planes // mplane_t*
        {
            get;
            set;
        }

        public Int32 NumPlanes
        {
            get;
            set;
        }

        public Int32 NumLeafs      // number of visible leafs, not counting 0
        {
            get;
            set;
        }

        public MemoryLeaf[] Leaves // mleaf_t*
        {
            get;
            set;
        }

        public Int32 NumVertices
        {
            get;
            set;
        }

        public MemoryVertex[] Vertices // mvertex_t*
        {
            get;
            set;
        }

        public Int32 NumEdges
        {
            get;
            set;
        }

        public MemoryEdge[] Edges // medge_t*
        {
            get;
            set;
        }

        public Int32 NumNodes
        {
            get;
            set;
        }

        public MemoryNode[] Nodes // mnode_t *nodes;
        {
            get;
            set;
        }

        public Int32 NumTexInfo
        {
            get;
            set;
        }

        public MemoryTextureInfo[] TexInfo
        {
            get;
            set;
        }

        public Int32 NumSurfaces
        {
            get;
            set;
        }

        public MemorySurface[] Surfaces
        {
            get;
            set;
        }

        public Int32 NumSurfEdges
        {
            get;
            set;
        }

        public Int32[] SurfEdges // int *surfedges;
        {
            get;
            set;
        }

        public Int32 NumClipNodes
        {
            get;
            set;
        }

        public BspClipNode[] ClipNodes // public dclipnode_t* clipnodes;
        {
            get;
            set;
        }

        public Int32 NumMarkSurfaces
        {
            get;
            set;
        }

        public MemorySurface[] MarkSurfaces // msurface_t **marksurfaces;
        {
            get;
            set;
        }

        public BspHull[] Hulls // [MAX_MAP_HULLS];
        {
            get;
            set;
        }

        public Int32 NumTextures
        {
            get;
            set;
        }

        public ModelTexture[] Textures // texture_t	**textures;
        {
            get;
            set;
        }

        public Byte[] VisData // byte *visdata;
        {
            get;
            set;
        }

        public Byte[] LightData // byte		*lightdata;
        {
            get;
            set;
        }

        public String Entities // char		*entities
        {
            get;
            set;
        }

        private MemorySurface WarpFace
        {
            get;
            set;
        }

        private Single SubdivideSize
        {
            get;
            set;
        }

        private Byte[] _NoVis = new Byte[BspDef.MAX_MAP_LEAFS / 8]; // byte mod_novis[MAX_MAP_LEAFS/8]
        private Byte[] _Decompressed = new Byte[BspDef.MAX_MAP_LEAFS / 8]; // static byte decompressed[] from Mod_DecompressVis()
        
        public BrushModelData( Single subdivideSize, ModelTexture noTexture ) : base( noTexture )
        {
            Type = ModelType.Brush;

            SubdivideSize = subdivideSize;

            Hulls = new BspHull[BspDef.MAX_MAP_HULLS];

            for ( var i = 0; i < Hulls.Length; i++ )
                Hulls[i] = new BspHull( );

            Utilities.FillArray( _NoVis, ( Byte ) 0xff );
        }

        public override void Clear( )
        {
            base.Clear( );

            FirstModelSurface = 0;
            NumModelSurfaces = 0;

            NumSubModels = 0;
            SubModels = null;

            NumPlanes = 0;
            Planes = null;

            NumLeafs = 0;
            Leaves = null;

            NumVertices = 0;
            Vertices = null;

            NumEdges = 0;
            Edges = null;

            NumNodes = 0;
            Nodes = null;

            NumTexInfo = 0;
            TexInfo = null;

            NumSurfaces = 0;
            Surfaces = null;

            NumSurfEdges = 0;
            SurfEdges = null;

            NumClipNodes = 0;
            ClipNodes = null;

            NumMarkSurfaces = 0;
            MarkSurfaces = null;

            foreach ( var h in Hulls )
                h.Clear( );

            NumTextures = 0;
            Textures = null;

            VisData = null;
            LightData = null;
            Entities = null;
        }

        public override void CopyFrom( ModelData src )
        {
            base.CopyFrom( src );

            Type = ModelType.Brush;

            if ( !( src is BrushModelData ) )
                return;

            var brushSrc = ( BrushModelData ) src;

            FirstModelSurface = brushSrc.FirstModelSurface;
            NumModelSurfaces = brushSrc.NumModelSurfaces;

            NumSubModels = brushSrc.NumSubModels;
            SubModels = brushSrc.SubModels;

            NumPlanes = brushSrc.NumPlanes;
            Planes = brushSrc.Planes;

            NumLeafs = brushSrc.NumLeafs;
            Leaves = brushSrc.Leaves;

            NumVertices = brushSrc.NumVertices;
            Vertices = brushSrc.Vertices;

            NumEdges = brushSrc.NumEdges;
            Edges = brushSrc.Edges;

            NumNodes = brushSrc.NumNodes;
            Nodes = brushSrc.Nodes;

            NumTexInfo = brushSrc.NumTexInfo;
            TexInfo = brushSrc.TexInfo;

            NumSurfaces = brushSrc.NumSurfaces;
            Surfaces = brushSrc.Surfaces;

            NumSurfEdges = brushSrc.NumSurfEdges;
            SurfEdges = brushSrc.SurfEdges;

            NumClipNodes = brushSrc.NumClipNodes;
            ClipNodes = brushSrc.ClipNodes;

            NumMarkSurfaces = brushSrc.NumMarkSurfaces;
            MarkSurfaces = brushSrc.MarkSurfaces;

            for ( var i = 0; i < brushSrc.Hulls.Length; i++ )
            {
                Hulls[i].CopyFrom( brushSrc.Hulls[i] );
            }

            NumTextures = brushSrc.NumTextures;
            Textures = brushSrc.Textures;

            VisData = brushSrc.VisData;
            LightData = brushSrc.LightData;
            Entities = brushSrc.Entities;
        }

        public void Load( String name, Byte[] buffer, Action<ModelTexture> onCheckInitSkyTexture, Func<String, WadLumpBuffer> onCheckForTexture )
        {
            Name = name;
            Buffer = buffer;

            LoadHeader( );
            SwapLumps( );

            // load into heap
            if ( Version == BspDef.Q1_BSPVERSION || Version == BspDef.HL_BSPVERSION )
            {
                var lumps = Q1Header.lumps;
                LoadVertices( ref lumps[( Int32 ) Q1Lumps.Vertices] );
                LoadEdges( ref lumps[( Int32 ) Q1Lumps.Edges] );
                LoadSurfEdges( ref lumps[( Int32 ) Q1Lumps.SurfaceEdges] );
                LoadTextures( ref lumps[( Int32 ) Q1Lumps.Textures], onCheckInitSkyTexture, onCheckForTexture );
                LoadLighting( ref lumps[( Int32 ) Q1Lumps.Lighting] );
                LoadPlanes( ref lumps[( Int32 ) Q1Lumps.Planes] );
                LoadTexInfo( ref lumps[( Int32 ) Q1Lumps.TextureInfo] );
                LoadFaces( ref lumps[( Int32 ) Q1Lumps.Faces] );
                LoadMarkSurfaces( ref lumps[( Int32 ) Q1Lumps.MarkSurfaces] );
                LoadVisibility( ref lumps[( Int32 ) Q1Lumps.Visibility] );
                LoadLeafs( ref lumps[( Int32 ) Q1Lumps.Leaves] );
                LoadNodes( ref lumps[( Int32 ) Q1Lumps.Nodes] );
                LoadClipNodes( ref lumps[( Int32 ) Q1Lumps.ClipNodes] );
                LoadEntities( ref lumps[( Int32 ) Q1Lumps.Entities] );
                LoadSubModels( ref lumps[( Int32 ) Q1Lumps.Models] );
                MakeHull0( );
            }
            else if ( Version == BspDef.Q2_BSPVERSION )
            {
                var lumps = Q2Header.lumps;
                LoadEntities( ref lumps[( Int32 ) Q2Lumps.Entities] );
                LoadPlanes( ref lumps[( Int32 ) Q2Lumps.Planes] );
                LoadVertices( ref lumps[( Int32 ) Q2Lumps.Vertices] );
                LoadVisibility( ref lumps[( Int32 ) Q2Lumps.Visibility] );
                LoadNodes( ref lumps[( Int32 ) Q2Lumps.Nodes] );
                LoadTexInfo( ref lumps[( Int32 ) Q2Lumps.TextureInfo] );
                LoadFaces( ref lumps[( Int32 ) Q2Lumps.Faces] );
                LoadLighting( ref lumps[( Int32 ) Q2Lumps.Lighting] );
                LoadLeafs( ref lumps[( Int32 ) Q2Lumps.Leaves] );
                // LeafFaces
                // LeafBrushes
                LoadEdges( ref lumps[( Int32 ) Q2Lumps.Edges] );
                LoadSurfEdges( ref lumps[( Int32 ) Q2Lumps.SurfaceEdges] );
                LoadSubModels( ref lumps[( Int32 ) Q2Lumps.Models] );
                // Brushes
                // BrushSides
                // Pop
                // Areas
                // AreaPortals
                MakeHull0( );
            }
            else if ( Version == BspDef.Q3_BSPVERSION )
            {
                BaseOffset += Q3Header.SizeInBytes;

                var lumps = Q3Header.lumps;
                LoadEntities( ref lumps[( Int32 ) Q3Lumps.Entities] );
                LoadTextures( ref lumps[( Int32 ) Q3Lumps.Textures], onCheckInitSkyTexture, onCheckForTexture );
                //LoadPlanes( ref lumps[( Int32 ) Q3Lumps.Planes] );
               // LoadNodes( ref lumps[( Int32 ) Q3Lumps.Nodes] );
                //LoadLeafs( ref lumps[( Int32 ) Q3Lumps.Leaves] );
                // LeafFaces
                // LeafBrushes
                //LoadSubModels( ref lumps[( Int32 ) Q3Lumps.Models] );
                // Brushes
                // BrushSides
                //LoadVertices( ref lumps[( Int32 ) Q3Lumps.Vertices] );
                // Triangles
                // Effects
                //LoadFaces( ref lumps[( Int32 ) Q3Lumps.Faces] );
                // LightMaps
                // LightGrid
                // PVS
               // MakeHull0( );
            }

            FrameCount = 2;	// regular and alternate animation
        }

        private void LoadHeader( )
        {
            var v = BitConverter.ToInt32( Buffer.ToList( ).GetRange( 0, 4 ).ToArray( ), 0 );
            var bspVersion = EndianHelper.LittleLong( v );

            if ( v < 0 || v > 1000 ) // Hack for detecting quake 3
            {
                v = BitConverter.ToInt32( Buffer.ToList( ).GetRange( 4, 4 ).ToArray( ), 0 );
                bspVersion = EndianHelper.LittleLong( v );
            }

            if ( !BspDef.SUPPORTED_BSPS.Contains( bspVersion ) )
            {
                Utilities.Error( $"Mod_LoadBrushModel: {Name} has wrong version number ({bspVersion})" );
                return;
            }

            if ( bspVersion == BspDef.Q1_BSPVERSION || bspVersion == BspDef.HL_BSPVERSION )
            {
                var header = Utilities.BytesToStructure<Q1Header>( Buffer, 0 );
                header.version = EndianHelper.LittleLong( header.version );
                Q1Header = header;
            }
            else if ( bspVersion == BspDef.Q2_BSPVERSION )
            {
                var header = Utilities.BytesToStructure<Q2Header>( Buffer, 0 );
                header.version = EndianHelper.LittleLong( header.version );
                Q2Header = header;
            }
            else if ( bspVersion == BspDef.Q3_BSPVERSION )
            {
                var header = Utilities.BytesToStructure<Q3Header>( Buffer, 0 );
                header.version = EndianHelper.LittleLong( header.version );
                Q3Header = header;
            }

            Version = bspVersion;
        }

        private void SwapLumps( )
        {
            BspLump[] lumps = null;

            switch ( Version )
            {
                case BspDef.HL_BSPVERSION:
                case BspDef.Q1_BSPVERSION:
                    lumps = Q1Header.lumps;
                    break;

                case BspDef.Q2_BSPVERSION:
                    lumps = Q2Header.lumps;
                    break;

                case BspDef.Q3_BSPVERSION:
                    lumps = Q3Header.lumps;
                    break;
            }

            if ( lumps == null )
                return;

            for ( var i = 0; i < lumps.Length; i++ )
            {
                lumps[i].Length = EndianHelper.LittleLong( lumps[i].Length );
                lumps[i].Position = EndianHelper.LittleLong( lumps[i].Position );
            }
        }

        /// <summary>
        /// Mod_LoadVertexes
        /// </summary>
        private void LoadVertices( ref BspLump l )
        {
            var count = 0;

            if ( Version == BspDef.Q1_BSPVERSION || Version == BspDef.HL_BSPVERSION )
            {
                if ( ( l.Length % BspVertex.SizeInBytes ) != 0 )
                    Utilities.Error( $"MOD_LoadBmodel: funny lump size in {Name}" );

                count = l.Length / BspVertex.SizeInBytes;
            }
            else
            {
                var cc = ( Single ) l.Length / Q3Vertex.SizeInBytes;

                if ( ( ( BaseOffset + l.Length ) % Q3Vertex.SizeInBytes ) != 0 )
                    Utilities.Error( $"MOD_LoadBmodel: funny lump size in {Name}" );

                count = l.Length / Q3Vertex.SizeInBytes;
            }

            var verts = new MemoryVertex[count];

            Vertices = verts;
            NumVertices = count;

            for ( Int32 i = 0, offset = BaseOffset + l.Position; i < count; i++, offset += BspVertex.SizeInBytes )
            {
                if ( Version == BspDef.Q1_BSPVERSION || Version == BspDef.HL_BSPVERSION )
                {
                    var src = Utilities.BytesToStructure<BspVertex>( Buffer, offset );
                    verts[i].position = EndianHelper.LittleVector3( src.point );
                }
                else
                {
                    var src = Utilities.BytesToStructure<Q3Vertex>( Buffer, offset );
                    verts[i].position = EndianHelper.LittleVector3( src.origin );
                }
            }
        }

        /// <summary>
        /// Mod_LoadEdges
        /// </summary>
        private void LoadEdges( ref BspLump l )
        {
            if ( ( l.Length % BspEdge.SizeInBytes ) != 0 )
                Utilities.Error( $"MOD_LoadBmodel: funny lump size in {Name}" );

            var count = l.Length / BspEdge.SizeInBytes;

            // Uze: Why count + 1 ?????
            var e = new MemoryEdge[count]; // out = Hunk_AllocName ( (count + 1) * sizeof(*out), loadname);
            Edges = e;
            NumEdges = count;

            for ( Int32 i = 0, offset = l.Position; i < count; i++, offset += BspEdge.SizeInBytes )
            {
                var src = Utilities.BytesToStructure<BspEdge>( Buffer, offset );
                e[i].v = new UInt16[] {
                    (UInt16)EndianHelper.LittleShort((Int16)src.v[0]),
                    (UInt16)EndianHelper.LittleShort((Int16)src.v[1])
                };
            }
        }

        /// <summary>
        /// Mod_LoadSurfedges
        /// </summary>
        private void LoadSurfEdges( ref BspLump l )
        {
            if ( ( l.Length % sizeof( Int32 ) ) != 0 )
                Utilities.Error( $"MOD_LoadBmodel: funny lump size in {Name}" );

            var count = l.Length / sizeof( Int32 );
            var e = new Int32[count];

            SurfEdges = e;
            NumSurfEdges = count;

            for ( Int32 i = 0, offset = l.Position; i < count; i++, offset += 4 )
            {
                var src = BitConverter.ToInt32( Buffer, offset );
                e[i] = src; // EndianHelper.LittleLong(in[i]);
            }
        }

        /// <summary>
        /// Mod_LoadTextures
        /// </summary>
        private void LoadTextures( ref BspLump l, Action<ModelTexture> onCheckInitSkyTexture, Func<String, WadLumpBuffer> onCheckForTexture )
        {
            if ( l.Length == 0 )
            {
                Textures = null;
                return;
            }

            if ( Version == BspDef.Q3_BSPVERSION )
            {
                var count = l.Length / Q3Texture.SizeInBytes;
                var offset = BaseOffset + l.Position;
                for ( var i = 0; i < count; i++ )
                {
                    var tex = Utilities.BytesToStructure<Q3Texture>( Buffer, offset );
                    
                    offset += Q3Texture.SizeInBytes;
                }
            }
            else
            {
                var m = Utilities.BytesToStructure<BspMipTexLump>( Buffer, l.Position );// (dmiptexlump_t *)(mod_base + l.fileofs);

                m.nummiptex = EndianHelper.LittleLong( m.nummiptex );

                var dataofs = new Int32[m.nummiptex];

                System.Buffer.BlockCopy( Buffer, l.Position + BspMipTexLump.SizeInBytes, dataofs, 0, dataofs.Length * sizeof( Int32 ) );

                NumTextures = m.nummiptex;
                Textures = new ModelTexture[m.nummiptex]; // Hunk_AllocName (m->nummiptex * sizeof(*loadmodel->textures) , loadname);

                for ( var i = 0; i < m.nummiptex; i++ )
                {
                    dataofs[i] = EndianHelper.LittleLong( dataofs[i] );
                    if ( dataofs[i] == -1 )
                        continue;

                    var mtOffset = l.Position + dataofs[i];
					var mt = Utilities.BytesToStructure<WadMipTex>( Buffer, mtOffset ); //mt = (miptex_t *)((byte *)m + m.dataofs[i]);
					mt.width = ( UInt32 ) EndianHelper.LittleLong( ( Int32 ) mt.width );
					mt.height = ( UInt32 ) EndianHelper.LittleLong( ( Int32 ) mt.height );

					var tx = new ModelTexture( );// Hunk_AllocName(sizeof(texture_t) + pixels, loadname);
					tx.name = Utilities.GetString( mt.name );

					var texResult = onCheckForTexture( tx.name );

					if ( texResult?.Pixels != null )
					{
						var overrideTex = texResult.Pixels;
						var size = texResult.Size;

						mt.width = ( UInt32 ) size.Width;
						mt.height = ( UInt32 ) size.Height;
						tx.scaleX = 1f;
						tx.scaleY = 1f;

						tx.pixels = overrideTex;

						tx.width = mt.width;
						tx.height = mt.height;
						tx.localPalette = texResult.Palette;
					}
					else if ( Version == BspDef.Q1_BSPVERSION )
					{
						tx.scaleX = 1f;
						tx.scaleY = 1f;

						tx.width = mt.width;
						tx.height = mt.height;
						var pixels = ( Int32 ) ( mt.width * mt.height / 64 * 85 );

						// the pixels immediately follow the structures
						tx.pixels = new Byte[pixels];
#warning BlockCopy tries to copy data over the bounds of _ModBase if certain mods are loaded. Needs proof fix!
						if ( mtOffset + WadMipTex.SizeInBytes + pixels <= Buffer.Length )
							System.Buffer.BlockCopy( Buffer, mtOffset + WadMipTex.SizeInBytes, tx.pixels, 0, pixels );
						else
						{
							System.Buffer.BlockCopy( Buffer, mtOffset + WadMipTex.SizeInBytes, tx.pixels, 0, pixels );
							ConsoleWrapper.Print( $"Texture info of {Name} truncated to fit in bounds of _ModBase\n" );
						}
					}
					else
						continue;

					for ( var j = 0; j < BspDef.MIPLEVELS; j++ )
						mt.offsets[j] = ( UInt32 ) EndianHelper.LittleLong( ( Int32 ) mt.offsets[j] );

					Textures[i] = tx;

					if ( Version == BspDef.Q1_BSPVERSION && mt.offsets[0] == 0 )
						continue;

					for ( var j = 0; j < BspDef.MIPLEVELS; j++ )
						tx.offsets[j] = ( Int32 ) mt.offsets[j] - WadMipTex.SizeInBytes;

					onCheckInitSkyTexture( tx );

                    //if ( tx.name != null && tx.name.StartsWith( "sky" ) )// !Q_strncmp(mt->name,"sky",3))
                    //    Host.RenderContext.InitSky( tx );
                    //else
                    //    tx.texture = BaseTexture.FromBuffer( Host.Video.Device, tx.name, new ByteArraySegment( tx.pixels ),
                    //        ( Int32 ) tx.width, ( Int32 ) tx.height, true, false );
                }

                //
                // sequence the animations
                //
                var anims = new ModelTexture[10];
                var altanims = new ModelTexture[10];

                for ( var i = 0; i < m.nummiptex; i++ )
                {
                    var tx = Textures[i];
                    if ( tx == null || !tx.name.StartsWith( "+" ) )// [0] != '+')
                        continue;
                    if ( tx.anim_next != null )
                        continue;   // allready sequenced

                    // find the number of frames in the animation
                    Array.Clear( anims, 0, anims.Length );
                    Array.Clear( altanims, 0, altanims.Length );

                    Int32 max = tx.name[1];
                    var altmax = 0;
                    if ( max >= 'a' && max <= 'z' )
                        max -= 'a' - 'A';
                    if ( max >= '0' && max <= '9' )
                    {
                        max -= '0';
                        altmax = 0;
                        anims[max] = tx;
                        max++;
                    }
                    else if ( max >= 'A' && max <= 'J' )
                    {
                        altmax = max - 'A';
                        max = 0;
                        altanims[altmax] = tx;
                        altmax++;
                    }
                    else
                        Utilities.Error( "Bad animating texture {0}", tx.name );

                    for ( var j = i + 1; j < m.nummiptex; j++ )
                    {
                        var tx2 = Textures[j];
                        if ( tx2 == null || !tx2.name.StartsWith( "+" ) )// tx2->name[0] != '+')
                            continue;
                        if ( String.Compare( tx2.name, 2, tx.name, 2, Math.Min( tx.name.Length, tx2.name.Length ) ) != 0 )// strcmp (tx2->name+2, tx->name+2))
                            continue;

                        Int32 num = tx2.name[1];

                        if ( num >= 'a' && num <= 'z' )
                            num -= 'a' - 'A';

                        if ( num >= '0' && num <= '9' )
                        {
                            num -= '0';
                            anims[num] = tx2;
                            if ( num + 1 > max )
                                max = num + 1;
                        }
                        else if ( num >= 'A' && num <= 'J' )
                        {
                            num = num - 'A';
                            altanims[num] = tx2;
                            if ( num + 1 > altmax )
                                altmax = num + 1;
                        }
                        else
                            Utilities.Error( "Bad animating texture {0}", tx2.name );
                    }

                    // link them all together
                    for ( var j = 0; j < max; j++ )
                    {
                        var tx2 = anims[j];

                        if ( tx2 == null )
                            Utilities.Error( "Missing frame {0} of {1}", j, tx.name );

                        tx2.anim_total = max * ModelDef.ANIM_CYCLE;
                        tx2.anim_min = j * ModelDef.ANIM_CYCLE;
                        tx2.anim_max = ( j + 1 ) * ModelDef.ANIM_CYCLE;
                        tx2.anim_next = anims[( j + 1 ) % max];

                        if ( altmax != 0 )
                            tx2.alternate_anims = altanims[0];
                    }
                    for ( var j = 0; j < altmax; j++ )
                    {
                        var tx2 = altanims[j];

                        if ( tx2 == null )
                            Utilities.Error( "Missing frame {0} of {1}", j, tx2.name );

                        tx2.anim_total = altmax * ModelDef.ANIM_CYCLE;
                        tx2.anim_min = j * ModelDef.ANIM_CYCLE;
                        tx2.anim_max = ( j + 1 ) * ModelDef.ANIM_CYCLE;
                        tx2.anim_next = altanims[( j + 1 ) % altmax];

                        if ( max != 0 )
                            tx2.alternate_anims = anims[0];
                    }
                }
            }
        }

        /// <summary>
        /// Mod_LoadLighting
        /// </summary>
        private void LoadLighting( ref BspLump l )
        {
            if ( l.Length == 0 )
            {
                LightData = null;
                return;
            }

            LightData = new Byte[l.Length]; // Hunk_AllocName(l->filelen, loadname);
            System.Buffer.BlockCopy( Buffer, l.Position, LightData, 0, l.Length );
        }

        /// <summary>
        /// Mod_LoadPlanes
        /// </summary>
        private void LoadPlanes( ref BspLump l )
        {
            if ( ( l.Length % BspPlane.SizeInBytes ) != 0 )
                Utilities.Error( $"MOD_LoadBmodel: funny lump size in {Name}" );

            var count = l.Length / BspPlane.SizeInBytes;
            // Uze: Possible error! Why in original is out = Hunk_AllocName ( count*2*sizeof(*out), loadname)???
            var p = new Plane[count];

            for ( var i = 0; i < p.Length; i++ )
                p[i] = new Plane( );

            Planes = p;
            NumPlanes = count;

            for ( var i = 0; i < count; i++ )
            {
                var src = Utilities.BytesToStructure<BspPlane>( Buffer, l.Position + i * BspPlane.SizeInBytes );
                var bits = 0;
                p[i].normal = EndianHelper.LittleVector3( src.normal );

                if ( p[i].normal.X < 0 )
                    bits |= 1;

                if ( p[i].normal.Y < 0 )
                    bits |= 1 << 1;

                if ( p[i].normal.Z < 0 )
                    bits |= 1 << 2;

                p[i].dist = EndianHelper.LittleFloat( src.dist );
                p[i].type = ( Byte ) EndianHelper.LittleLong( src.type );
                p[i].signbits = ( Byte ) bits;
            }
        }

        /// <summary>
        /// Mod_LoadTexinfo
        /// </summary>
        private void LoadTexInfo( ref BspLump l )
        {
            //in = (void *)(mod_base + l->fileofs);
            if ( ( l.Length % BspTextureInfo.SizeInBytes ) != 0 )
                Utilities.Error( $"MOD_LoadBmodel: funny lump size in {Name}" );

            var count = l.Length / BspTextureInfo.SizeInBytes;
            var infos = new MemoryTextureInfo[count]; // out = Hunk_AllocName ( count*sizeof(*out), loadname);

            for ( var i = 0; i < infos.Length; i++ )
                infos[i] = new MemoryTextureInfo( );

            TexInfo = infos;
            NumTexInfo = count;

            for ( var i = 0; i < count; i++ )//, in++, out++)
            {
                var src = Utilities.BytesToStructure<BspTextureInfo>( Buffer, l.Position + i * BspTextureInfo.SizeInBytes );

                for ( var j = 0; j < 2; j++ )
                    infos[i].vecs[j] = EndianHelper.LittleVector4( src.vecs, j * 4 );

                var len1 = infos[i].vecs[0].Length;
                var len2 = infos[i].vecs[1].Length;
                len1 = ( len1 + len2 ) / 2;
                if ( len1 < 0.32 )
                    infos[i].mipadjust = 4;
                else if ( len1 < 0.49 )
                    infos[i].mipadjust = 3;
                else if ( len1 < 0.99 )
                    infos[i].mipadjust = 2;
                else
                    infos[i].mipadjust = 1;

                var miptex = EndianHelper.LittleLong( src.miptex );
                infos[i].flags = EndianHelper.LittleLong( src.flags );

                if ( Textures == null )
                {
                    infos[i].texture = NoTexture;//Host.RenderContext.NoTextureMip;	// checkerboard texture
                    infos[i].flags = 0;
                }
                else
                {
                    if ( miptex >= NumTextures )
                        Utilities.Error( "miptex >= loadmodel->numtextures" );

                    infos[i].texture = Textures[miptex];

                    if ( infos[i].texture == null )
                    {
                        infos[i].texture = NoTexture; //Host.RenderContext.NoTextureMip; // texture not found
                        infos[i].flags = 0;
                    }
                }
            }
        }

        /// <summary>
        /// Mod_LoadFaces
        /// </summary>
        private void LoadFaces( ref BspLump l )
        {
            if ( ( l.Length % BspFace.SizeInBytes ) != 0 )
                Utilities.Error( $"MOD_LoadBmodel: funny lump size in {Name}" );

            var count = l.Length / BspFace.SizeInBytes;
            var dest = new MemorySurface[count];

            for ( var i = 0; i < dest.Length; i++ )
                dest[i] = new MemorySurface( );

            Surfaces = dest;
            NumSurfaces = count;
            var offset = l.Position;

            for ( var surfnum = 0; surfnum < count; surfnum++, offset += BspFace.SizeInBytes )
            {
                var src = Utilities.BytesToStructure<BspFace>( Buffer, offset );

                dest[surfnum].firstedge = EndianHelper.LittleLong( src.firstedge );
                dest[surfnum].numedges = EndianHelper.LittleShort( src.numedges );
                dest[surfnum].flags = 0;

                Int32 planenum = EndianHelper.LittleShort( src.planenum );
                Int32 side = EndianHelper.LittleShort( src.side );

                if ( side != 0 )
                    dest[surfnum].flags |= ( Int32 ) Q1SurfaceFlags.PlaneBack;

                dest[surfnum].plane = Planes[planenum];
                dest[surfnum].texinfo = TexInfo[EndianHelper.LittleShort( src.texinfo )];

                CalcSurfaceExtents( dest[surfnum] );

                // lighting info

                for ( var i = 0; i < BspDef.MAXLIGHTMAPS; i++ )
                    dest[surfnum].styles[i] = src.styles[i];

                var i2 = EndianHelper.LittleLong( src.lightofs );

                if ( i2 == -1 )
                {
                    dest[surfnum].sample_base = null;
                }
                else
                {
                    dest[surfnum].sample_base = LightData;
                    dest[surfnum].sampleofs = i2;
                }

                // set the drawing flags flag
                if ( dest[surfnum].texinfo.texture.name != null )
                {
                    if ( dest[surfnum].texinfo.texture.name.StartsWith( "sky" ) )	// sky
                    {
                        dest[surfnum].flags |= ( ( Int32 ) Q1SurfaceFlags.Sky | ( Int32 ) Q1SurfaceFlags.Tiled );
                        SubdivideSurface( dest[surfnum] );	// cut up polygon for warps
                        continue;
                    }

                    if ( dest[surfnum].texinfo.texture.name.StartsWith( "*" ) )		// turbulent
                    {
                        dest[surfnum].flags |= ( ( Int32 ) Q1SurfaceFlags.Turbulence | ( Int32 ) Q1SurfaceFlags.Tiled );

                        for ( var i = 0; i < 2; i++ )
                        {
                            dest[surfnum].extents[i] = 16384;
                            dest[surfnum].texturemins[i] = -8192;
                        }

                        SubdivideSurface( dest[surfnum] );	// cut up polygon for warps
                        continue;
                    }
                }
            }
        }

        /// <summary>
        /// Mod_LoadMarksurfaces
        /// </summary>
        private void LoadMarkSurfaces( ref BspLump l )
        {
            if ( ( l.Length % sizeof( Int16 ) ) != 0 )
                Utilities.Error( $"MOD_LoadBmodel: funny lump size in {Name}" );

            var count = l.Length / sizeof( Int16 );
            var dest = new MemorySurface[count];

            MarkSurfaces = dest;
            NumMarkSurfaces = count;

            for ( var i = 0; i < count; i++ )
            {
                Int32 j = BitConverter.ToInt16( Buffer, l.Position + i * sizeof( Int16 ) );

                if ( j >= NumSurfaces )
                    Utilities.Error( "Mod_ParseMarksurfaces: bad surface number" );

                dest[i] = Surfaces[j];
            }
        }

        /// <summary>
        /// Mod_LoadVisibility
        /// </summary>
        private void LoadVisibility( ref BspLump l )
        {
            if ( l.Length == 0 )
            {
                VisData = null;
                return;
            }

            VisData = new Byte[l.Length];
            System.Buffer.BlockCopy( Buffer, l.Position, VisData, 0, l.Length );
        }

        /// <summary>
        /// Mod_LoadLeafs
        /// </summary>
        private void LoadLeafs( ref BspLump l )
        {
            if ( ( l.Length % BspLeaf.SizeInBytes ) != 0 )
                Utilities.Error( $"MOD_LoadBmodel: funny lump size in {Name}" );

            var count = l.Length / BspLeaf.SizeInBytes;
            var dest = new MemoryLeaf[count];

            for ( var i = 0; i < dest.Length; i++ )
                dest[i] = new MemoryLeaf( );

            Leaves = dest;
            NumLeafs = count;

            for ( Int32 i = 0, offset = l.Position; i < count; i++, offset += BspLeaf.SizeInBytes )
            {
                var src = Utilities.BytesToStructure<BspLeaf>( Buffer, offset );

                dest[i].mins.X = EndianHelper.LittleShort( src.mins[0] );
                dest[i].mins.Y = EndianHelper.LittleShort( src.mins[1] );
                dest[i].mins.Z = EndianHelper.LittleShort( src.mins[2] );

                dest[i].maxs.X = EndianHelper.LittleShort( src.maxs[0] );
                dest[i].maxs.Y = EndianHelper.LittleShort( src.maxs[1] );
                dest[i].maxs.Z = EndianHelper.LittleShort( src.maxs[2] );

                var p = EndianHelper.LittleLong( src.contents );
                dest[i].contents = p;

                dest[i].marksurfaces = MarkSurfaces;
                dest[i].firstmarksurface = EndianHelper.LittleShort( ( Int16 ) src.firstmarksurface );
                dest[i].nummarksurfaces = EndianHelper.LittleShort( ( Int16 ) src.nummarksurfaces );

                p = EndianHelper.LittleLong( src.visofs );

                if ( p == -1 )
                {
                    dest[i].compressed_vis = null;
                }
                else
                {
                    dest[i].compressed_vis = VisData; // loadmodel->visdata + p;
                    dest[i].visofs = p;
                }

                dest[i].efrags = null;

                for ( var j = 0; j < 4; j++ )
                    dest[i].ambient_sound_level[j] = src.ambient_level[j];

                // gl underwater warp
                // Uze: removed underwater warp as too ugly
                //if (dest[i].contents != Contents.CONTENTS_EMPTY)
                //{
                //    for (int j = 0; j < dest[i].nummarksurfaces; j++)
                //        dest[i].marksurfaces[dest[i].firstmarksurface + j].flags |= Surf.SURF_UNDERWATER;
                //}
            }
        }

        /// <summary>
        /// Mod_LoadNodes
        /// </summary>
        private void LoadNodes( ref BspLump l )
        {
            if ( ( l.Length % BspNode.SizeInBytes ) != 0 )
                Utilities.Error( $"MOD_LoadBmodel: funny lump size in {Name}" );

            var count = l.Length / BspNode.SizeInBytes;
            var dest = new MemoryNode[count];

            for ( var i = 0; i < dest.Length; i++ )
                dest[i] = new MemoryNode( );

            Nodes = dest;
            NumNodes = count;

            for ( Int32 i = 0, offset = l.Position; i < count; i++, offset += BspNode.SizeInBytes )
            {
                var src = Utilities.BytesToStructure<BspNode>( Buffer, offset );

                dest[i].mins.X = EndianHelper.LittleShort( src.mins[0] );
                dest[i].mins.Y = EndianHelper.LittleShort( src.mins[1] );
                dest[i].mins.Z = EndianHelper.LittleShort( src.mins[2] );

                dest[i].maxs.X = EndianHelper.LittleShort( src.maxs[0] );
                dest[i].maxs.Y = EndianHelper.LittleShort( src.maxs[1] );
                dest[i].maxs.Z = EndianHelper.LittleShort( src.maxs[2] );

                var p = EndianHelper.LittleLong( src.planenum );
                dest[i].plane = Planes[p];

                dest[i].firstsurface = ( UInt16 ) EndianHelper.LittleShort( ( Int16 ) src.firstface );
                dest[i].numsurfaces = ( UInt16 ) EndianHelper.LittleShort( ( Int16 ) src.numfaces );

                for ( var j = 0; j < 2; j++ )
                {
                    p = EndianHelper.LittleShort( src.children[j] );

                    if ( p >= 0 )
                        dest[i].children[j] = Nodes[p];
                    else
                        dest[i].children[j] = Leaves[-1 - p];
                }
            }

            SetParent( Nodes[0], null );	// sets nodes and leafs
        }

        /// <summary>
        /// Mod_LoadClipnodes
        /// </summary>
        private void LoadClipNodes( ref BspLump l )
        {
            if ( ( l.Length % BspClipNode.SizeInBytes ) != 0 )
                Utilities.Error( $"MOD_LoadBmodel: funny lump size in {Name}" );

            var count = l.Length / BspClipNode.SizeInBytes;
            var dest = new BspClipNode[count];

            ClipNodes = dest;
            NumClipNodes = count;

            var hull = Hulls[1];
            hull.clipnodes = dest;
            hull.firstclipnode = 0;
            hull.lastclipnode = count - 1;
            hull.planes = Planes;
            hull.clip_mins.X = -16;
            hull.clip_mins.Y = -16;
            hull.clip_mins.Z = -24;
            hull.clip_maxs.X = 16;
            hull.clip_maxs.Y = 16;
            hull.clip_maxs.Z = 32;

            hull = Hulls[2];
            hull.clipnodes = dest;
            hull.firstclipnode = 0;
            hull.lastclipnode = count - 1;
            hull.planes = Planes;
            hull.clip_mins.X = -32;
            hull.clip_mins.Y = -32;
            hull.clip_mins.Z = -24;
            hull.clip_maxs.X = 32;
            hull.clip_maxs.Y = 32;
            hull.clip_maxs.Z = 64;

            for ( Int32 i = 0, offset = l.Position; i < count; i++, offset += BspClipNode.SizeInBytes )
            {
                var src = Utilities.BytesToStructure<BspClipNode>( Buffer, offset );

                dest[i].planenum = EndianHelper.LittleLong( src.planenum ); // Uze: changed from LittleShort
                dest[i].children = new Int16[2];
                dest[i].children[0] = EndianHelper.LittleShort( src.children[0] );
                dest[i].children[1] = EndianHelper.LittleShort( src.children[1] );
            }
        }

        /// <summary>
        /// Mod_LoadEntities
        /// </summary>
        private void LoadEntities( ref BspLump l )
        {
            if ( l.Length == 0 )
            {
                Entities = null;
                return;
            }

            Entities = Encoding.ASCII.GetString( Buffer, BaseOffset + l.Position, l.Length );
        }

        /// <summary>
        /// Mod_LoadSubmodels
        /// </summary>
        private void LoadSubModels( ref BspLump l )
        {
            if ( ( l.Length % Q1Model.SizeInBytes ) != 0 )
                Utilities.Error( $"MOD_LoadBmodel: funny lump size in {Name}" );

            var count = l.Length / Q1Model.SizeInBytes;
            var dest = new Q1Model[count];

            SubModels = dest;
            NumSubModels = count;

            for ( Int32 i = 0, offset = l.Position; i < count; i++, offset += Q1Model.SizeInBytes )
            {
                var src = Utilities.BytesToStructure<Q1Model>( Buffer, offset );

                dest[i].mins = new Single[3];
                dest[i].maxs = new Single[3];
                dest[i].origin = new Single[3];

                for ( var j = 0; j < 3; j++ )
                {
                    // spread the mins / maxs by a pixel
                    dest[i].mins[j] = EndianHelper.LittleFloat( src.mins[j] ) - 1;
                    dest[i].maxs[j] = EndianHelper.LittleFloat( src.maxs[j] ) + 1;
                    dest[i].origin[j] = EndianHelper.LittleFloat( src.origin[j] );
                }

                dest[i].headnode = new Int32[BspDef.MAX_MAP_HULLS];
                for ( var j = 0; j < BspDef.MAX_MAP_HULLS; j++ )
                    dest[i].headnode[j] = EndianHelper.LittleLong( src.headnode[j] );

                dest[i].visleafs = EndianHelper.LittleLong( src.visleafs );
                dest[i].firstface = EndianHelper.LittleLong( src.firstface );
                dest[i].numfaces = EndianHelper.LittleLong( src.numfaces );
            }
        }

        /// <summary>
        /// Mod_MakeHull0
        /// Deplicate the drawing hull structure as a clipping hull
        /// </summary>
        private void MakeHull0( )
        {
            var hull = Hulls[0];
            var src = Nodes;
            var count = NumNodes;
            var dest = new BspClipNode[count];

            hull.clipnodes = dest;
            hull.firstclipnode = 0;
            hull.lastclipnode = count - 1;
            hull.planes = Planes;

            for ( var i = 0; i < count; i++ )
            {
                dest[i].planenum = Array.IndexOf( Planes, src[i].plane ); // todo: optimize this
                dest[i].children = new Int16[2];

                for ( var j = 0; j < 2; j++ )
                {
                    var child = src[i].children[j];
                    if ( child.contents < 0 )
                        dest[i].children[j] = ( Int16 ) child.contents;
                    else
                        dest[i].children[j] = ( Int16 ) Array.IndexOf( Nodes, ( MemoryNode ) child ); // todo: optimize this
                }
            }
        }

        /// <summary>
        /// Mod_SetParent
        /// </summary>
        private void SetParent( MemoryNodeBase node, MemoryNode parent )
        {
            node.parent = parent;

            if ( node.contents < 0 )
                return;

            var n = ( MemoryNode ) node;
            SetParent( n.children[0], n );
            SetParent( n.children[1], n );
        }

        /// <summary>
        /// CalcSurfaceExtents
        /// Fills in s->texturemins[] and s->extents[]
        /// </summary>
        private void CalcSurfaceExtents( MemorySurface s )
        {
            var mins = new Single[] { 999999, 999999 };
            var maxs = new Single[] { -99999, -99999 };

            var tex = s.texinfo;
            var v = Vertices;

            for ( var i = 0; i < s.numedges; i++ )
            {
                Int32 idx;
                var e = SurfEdges[s.firstedge + i];

                if ( e >= 0 )
                    idx = Edges[e].v[0];
                else
                    idx = Edges[-e].v[1];

                for ( var j = 0; j < 2; j++ )
                {
                    var val = v[idx].position.X * tex.vecs[j].X +
                        v[idx].position.Y * tex.vecs[j].Y +
                        v[idx].position.Z * tex.vecs[j].Z +
                        tex.vecs[j].W;
                    if ( val < mins[j] )
                        mins[j] = val;
                    if ( val > maxs[j] )
                        maxs[j] = val;
                }
            }

            var bmins = new Int32[2];
            var bmaxs = new Int32[2];

            for ( var i = 0; i < 2; i++ )
            {
                bmins[i] = ( Int32 ) Math.Floor( mins[i] / 16 );
                bmaxs[i] = ( Int32 ) Math.Ceiling( maxs[i] / 16 );

                s.texturemins[i] = ( Int16 ) ( bmins[i] * 16 );
                s.extents[i] = ( Int16 ) ( ( bmaxs[i] - bmins[i] ) * 16 );

			}

			var ssize = ( s.extents[0] >> 4 ) + 1;
			var tsize = ( s.extents[1] >> 4 ) + 1;

			if ( Version != BspDef.Q3_BSPVERSION && ( tex?.flags & BspDef.TEX_SPECIAL ) == 0 ) //&& s.extents[i] > 512
			{
				if ( ssize > 256 || tsize > 256 )
					Utilities.Error( "Bad surface extents" );
			}
		}

        /// <summary>
        /// GL_SubdivideSurface
        /// Breaks a polygon up along axial 64 unit boundaries
        /// so that turbulent and sky warps can be done reasonably.
        /// </summary>
        protected void SubdivideSurface( MemorySurface fa )
        {
            WarpFace = fa;

            //
            // convert edges back to a normal polygon
            //
            var numverts = 0;
            var verts = new Vector3[fa.numedges + 1]; // + 1 for wrap case

            for ( var i = 0; i < fa.numedges; i++ )
            {
                var lindex = SurfEdges[fa.firstedge + i];

                if ( lindex > 0 )
                    verts[numverts] = Vertices[Edges[lindex].v[0]].position;
                else
                    verts[numverts] = Vertices[Edges[-lindex].v[1]].position;

                numverts++;
            }

            SubdividePolygon( numverts, verts );
        }

        /// <summary>
        /// SubdividePolygon
        /// </summary>
        protected void SubdividePolygon( Int32 numverts, Vector3[] verts )
        {
            if ( numverts > 60 )
                Utilities.Error( "numverts = {0}", numverts );

            Vector3 mins, maxs;
            BoundPoly( numverts, verts, out mins, out maxs );

            var dist = new Single[64];
            for ( var i = 0; i < 3; i++ )
            {
                var m = ( MathLib.Comp( ref mins, i ) + MathLib.Comp( ref maxs, i ) ) * 0.5;
                m = SubdivideSize * Math.Floor( m / SubdivideSize + 0.5 );
                if ( MathLib.Comp( ref maxs, i ) - m < 8 )
                    continue;

                if ( m - MathLib.Comp( ref mins, i ) < 8 )
                    continue;

                for ( var j = 0; j < numverts; j++ )
                    dist[j] = ( Single ) ( MathLib.Comp( ref verts[j], i ) - m );

                var front = new Vector3[64];
                var back = new Vector3[64];

                // cut it

                // wrap cases
                dist[numverts] = dist[0];
                verts[numverts] = verts[0]; // Uze: source array must be at least numverts + 1 elements long

                Int32 f = 0, b = 0;
                for ( var j = 0; j < numverts; j++ )
                {
                    if ( dist[j] >= 0 )
                    {
                        front[f] = verts[j];
                        f++;
                    }
                    if ( dist[j] <= 0 )
                    {
                        back[b] = verts[j];
                        b++;
                    }
                    if ( dist[j] == 0 || dist[j + 1] == 0 )
                        continue;
                    if ( ( dist[j] > 0 ) != ( dist[j + 1] > 0 ) )
                    {
                        // clip point
                        var frac = dist[j] / ( dist[j] - dist[j + 1] );
                        front[f] = back[b] = verts[j] + ( verts[j + 1] - verts[j] ) * frac;
                        f++;
                        b++;
                    }
                }

                SubdividePolygon( f, front );
                SubdividePolygon( b, back );
                return;
            }

            var poly = new GLPoly( );
            poly.next = WarpFace.polys;
            WarpFace.polys = poly;
            poly.AllocVerts( numverts );
            for ( var i = 0; i < numverts; i++ )
            {
                Utilities.Copy( ref verts[i], poly.verts[i] );
                var s = Vector3.Dot( verts[i], WarpFace.texinfo.vecs[0].Xyz );
                var t = Vector3.Dot( verts[i], WarpFace.texinfo.vecs[1].Xyz );
                poly.verts[i][3] = s;
                poly.verts[i][4] = t;
            }
        }

        /// <summary>
        /// BoundPoly
        /// </summary>
        protected void BoundPoly( Int32 numverts, Vector3[] verts, out Vector3 mins, out Vector3 maxs )
        {
            mins = Vector3.One * 9999;
            maxs = Vector3.One * -9999;
            for ( var i = 0; i < numverts; i++ )
            {
                Vector3.ComponentMin( ref verts[i], ref mins, out mins );
                Vector3.ComponentMax( ref verts[i], ref maxs, out maxs );
            }
        }

        public void SetupSubModel( ref Q1Model submodel )
        {
            Hulls[0].firstclipnode = submodel.headnode[0];
            for ( var j = 1; j < BspDef.MAX_MAP_HULLS; j++ )
            {
                Hulls[j].firstclipnode = submodel.headnode[j];
                Hulls[j].lastclipnode = NumClipNodes - 1;
            }
            FirstModelSurface = submodel.firstface;
            NumModelSurfaces = submodel.numfaces;

            var mins = BoundsMin;
            var maxs = BoundsMax;

            Utilities.Copy( submodel.maxs, out maxs ); // mod.maxs = submodel.maxs;
            Utilities.Copy( submodel.mins, out mins ); // mod.mins = submodel.mins;
            Radius = RadiusFromBounds( ref mins, ref maxs );
            NumLeafs = submodel.visleafs;

            BoundsMin = mins;
            BoundsMax = maxs;
        }

        private Single RadiusFromBounds( ref Vector3 mins, ref Vector3 maxs )
        {
            Vector3 corner;

            corner.X = Math.Max( Math.Abs( mins.X ), Math.Abs( maxs.X ) );
            corner.Y = Math.Max( Math.Abs( mins.Y ), Math.Abs( maxs.Y ) );
            corner.Z = Math.Max( Math.Abs( mins.Z ), Math.Abs( maxs.Z ) );

            return corner.Length;
        }

        /// <summary>
        /// Mod_DecompressVis
        /// </summary>
        private Byte[] DecompressVis( Byte[] p, Int32 startIndex )
        {
            var row = ( NumLeafs + 7 ) >> 3;
            var offset = 0;

            if ( p == null )
            {
                // no vis info, so make all visible
                while ( row != 0 )
                {
                    _Decompressed[offset++] = 0xff;
                    row--;
                }
                return _Decompressed;
            }
            var srcOffset = startIndex;
            do
            {
                if ( p[srcOffset] != 0 )// (*in)
                {
                    _Decompressed[offset++] = p[srcOffset++]; //  *out++ = *in++;
                    continue;
                }

                Int32 c = p[srcOffset + 1];// in[1];
                srcOffset += 2; // in += 2;
                while ( c != 0 )
                {
                    _Decompressed[offset++] = 0; // *out++ = 0;
                    c--;
                }
            } while ( offset < row ); // out - decompressed < row

            return _Decompressed;
        }

        /// <summary>
        /// Mod_LeafPVS
        /// </summary>
        public Byte[] LeafPVS( MemoryLeaf leaf )
        {
            if ( leaf == Leaves[0] )
                return _NoVis;

            return DecompressVis( leaf.compressed_vis, leaf.visofs );
        }

        /// <summary>
        /// Mod_PointInLeaf
        /// </summary>
        public MemoryLeaf PointInLeaf( ref Vector3 p )
        {
            if ( Nodes == null )
                Utilities.Error( "Mod_PointInLeaf: bad model" );

            MemoryLeaf result = null;
            MemoryNodeBase node = Nodes[0];

            while ( true )
            {
                if ( node.contents < 0 )
                {
                    result = ( MemoryLeaf ) node;
                    break;
                }

                var n = ( MemoryNode ) node;
                var plane = n.plane;
                var d = Vector3.Dot( p, plane.normal ) - plane.dist;
                if ( d > 0 )
                    node = n.children[0];
                else
                    node = n.children[1];
            }

            return result;
        }
	}
}
