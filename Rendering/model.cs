/// <copyright>
///
/// Rewritten in C# by Yury Kiselev, 2010.
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
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using OpenTK;
using SharpQuake.Framework;

// gl_model.c -- model loading and caching

// models are the only shared resource between a client and server running
// on the same machine.

namespace SharpQuake
{
    /// <summary>
    /// Mod_functions
    /// </summary>
    public class Mod
    {
        public trivertx_t[][] PoseVerts
        {
            get
            {
                return _PoseVerts;
            }
        }

        public stvert_t[] STVerts
        {
            get
            {
                return _STVerts;
            }
        }

        public dtriangle_t[] Triangles
        {
            get
            {
                return _Triangles;
            }
        }

        public aliashdr_t Header
        {
            get
            {
                return _Header;
            }
        }

        public Model Model
        {
            get
            {
                return _LoadModel;
            }
        }

        public Single SubdivideSize
        {
            get
            {
                return _glSubDivideSize.Value;
            }
        }

        // Instance
        public Host Host
        {
            get;
            private set;
        }       

        private CVar _glSubDivideSize; // = { "gl_subdivide_size", "128", true };
        private Byte[] _Novis = new Byte[BspDef.MAX_MAP_LEAFS / 8]; // byte mod_novis[MAX_MAP_LEAFS/8]

        private Model[] _Known = new Model[ModelDef.MAX_MOD_KNOWN]; // mod_known
        private Int32 _NumKnown; // mod_numknown

        private Model _LoadModel; // loadmodel
        private aliashdr_t _Header; // pheader

        private stvert_t[] _STVerts = new stvert_t[ModelDef.MAXALIASVERTS]; // stverts
        private dtriangle_t[] _Triangles = new dtriangle_t[ModelDef.MAXALIASTRIS]; // triangles
        private Int32 _PoseNum; // posenum;
        private Byte[] _ModBase; // mod_base  - used by Brush model loading functions
        private trivertx_t[][] _PoseVerts = new trivertx_t[ModelDef.MAXALIASFRAMES][]; // poseverts
        private Byte[] _Decompressed = new Byte[BspDef.MAX_MAP_LEAFS / 8]; // static byte decompressed[] from Mod_DecompressVis()

        public Mod( Host host )
        {
            Host = host;
        }

        /// <summary>
        /// Mod_Init
        /// </summary>
        public void Initialise( )
        {
            if ( _glSubDivideSize == null )
            {
                _glSubDivideSize = new CVar( "gl_subdivide_size", "128", true );
            }

            for( var i = 0; i < _Known.Length; i++ )
                _Known[i] = new Model();

            Utilities.FillArray( _Novis, ( Byte ) 0xff );
        }

        /// <summary>
        /// Mod_ClearAll
        /// </summary>
        public void ClearAll()
        {
            for( var i = 0; i < _NumKnown; i++ )
            {
                var mod = _Known[i];

                if( mod.type != ModelType.mod_alias )
                    mod.needload = true;
            }
        }

        /// <summary>
        /// Mod_ForName
        /// Loads in a model for the given name
        /// </summary>
        public Model ForName( String name, Boolean crash )
        {
            var mod = FindName( name );

            return LoadModel( mod, crash );
        }

        /// <summary>
        /// Mod_Extradata
        /// handles caching
        /// </summary>
        public aliashdr_t GetExtraData( Model mod )
        {
            var r = Host.Cache.Check( mod.cache );
            if( r != null )
                return (aliashdr_t)r;

            LoadModel( mod, true );

            if( mod.cache.data == null )
                Utilities.Error( "Mod_Extradata: caching failed" );
            return (aliashdr_t)mod.cache.data;
        }

        /// <summary>
        /// Mod_TouchModel
        /// </summary>
        public void TouchModel( String name )
        {
            var mod = FindName( name );

            if( !mod.needload )
            {
                if( mod.type == ModelType.mod_alias )
                    Host.Cache.Check( mod.cache );
            }
        }

        /// <summary>
        /// Mod_PointInLeaf
        /// </summary>
        public MemoryLeaf PointInLeaf( ref Vector3 p, Model model )
        {
            if( model == null || model.nodes == null )
                Utilities.Error( "Mod_PointInLeaf: bad model" );

            MemoryLeaf result = null;
            MemoryNodeBase node = model.nodes[0];
            while( true )
            {
                if( node.contents < 0 )
                {
                    result = (MemoryLeaf)node;
                    break;
                }

                var n = (MemoryNode)node;
                var plane = n.plane;
                var d = Vector3.Dot( p, plane.normal ) - plane.dist;
                if( d > 0 )
                    node = n.children[0];
                else
                    node = n.children[1];
            }

            return result;
        }

        /// <summary>
        /// Mod_LeafPVS
        /// </summary>
        public Byte[] LeafPVS( MemoryLeaf leaf, Model model )
        {
            if( leaf == model.leafs[0] )
                return _Novis;

            return DecompressVis( leaf.compressed_vis, leaf.visofs, model );
        }

        // Mod_Print
        public void Print()
        {
            ConsoleWrapper.Print( "Cached models:\n" );
            for( var i = 0; i < _NumKnown; i++ )
            {
                var mod = _Known[i];
                ConsoleWrapper.Print( "{0}\n", mod.name );
            }
        }

        /// <summary>
        /// Mod_FindName
        /// </summary>
        public Model FindName( String name )
        {
            if( String.IsNullOrEmpty( name ) )
                Utilities.Error( "Mod_ForName: NULL name" );

            //
            // search the currently loaded models
            //
            var i = 0;
            Model mod;
            for( i = 0, mod = _Known[0]; i < _NumKnown; i++, mod = _Known[i] )
            {
                //mod = _Known[i];
                if( mod.name == name )
                    break;
            }

            if( i == _NumKnown )
            {
                if( _NumKnown == ModelDef.MAX_MOD_KNOWN )
                    Utilities.Error( "mod_numknown == MAX_MOD_KNOWN" );
                mod.name = name;
                mod.needload = true;
                _NumKnown++;
            }

            return mod;
        }

        /// <summary>
        /// Mod_LoadModel
        /// Loads a model into the cache
        /// </summary>
        public Model LoadModel( Model mod, Boolean crash )
        {
            if( !mod.needload )
            {
                if( mod.type == ModelType.mod_alias )
                {
                    if( Host.Cache.Check( mod.cache ) != null )
                        return mod;
                }
                else
                    return mod;		// not cached at all
            }

            //
            // load the file
            //
            var buf = FileSystem.LoadFile( mod.name );
            if( buf == null )
            {
                if( crash )
                    Utilities.Error( "Mod_NumForName: {0} not found", mod.name );
                return null;
            }

            //
            // allocate a new model
            //
            _LoadModel = mod;

            //
            // fill it in
            //

            // call the apropriate loader
            mod.needload = false;

            switch( BitConverter.ToUInt32( buf, 0 ) )// LittleLong(*(unsigned *)buf))
            {
                case ModelDef.IDPOLYHEADER:
                    LoadAliasModel( mod, buf );
                    break;

                case ModelDef.IDSPRITEHEADER:
                    LoadSpriteModel( mod, buf );
                    break;

                default:
                    LoadBrushModel( mod, buf );
                    break;
            }

            return mod;
        }

        /// <summary>
        /// Mod_LoadAliasModel
        /// </summary>
        public void LoadAliasModel( Model mod, Byte[] buffer )
        {
            var pinmodel = Utilities.BytesToStructure<mdl_t>( buffer, 0 );

            var version = EndianHelper.LittleLong( pinmodel.version );
            if( version != ModelDef.ALIAS_VERSION )
                Utilities.Error( "{0} has wrong version number ({1} should be {2})",
                    mod.name, version, ModelDef.ALIAS_VERSION );

            //
            // allocate space for a working header, plus all the data except the frames,
            // skin and group info
            //
            _Header = new aliashdr_t();

            mod.flags = EndianHelper.LittleLong( pinmodel.flags );

            //
            // endian-adjust and copy the data, starting with the alias model header
            //
            _Header.boundingradius = EndianHelper.LittleFloat( pinmodel.boundingradius );
            _Header.numskins = EndianHelper.LittleLong( pinmodel.numskins );
            _Header.skinwidth = EndianHelper.LittleLong( pinmodel.skinwidth );
            _Header.skinheight = EndianHelper.LittleLong( pinmodel.skinheight );

            if( _Header.skinheight > ModelDef.MAX_LBM_HEIGHT )
                Utilities.Error( "model {0} has a skin taller than {1}", mod.name, ModelDef.MAX_LBM_HEIGHT );

            _Header.numverts = EndianHelper.LittleLong( pinmodel.numverts );

            if( _Header.numverts <= 0 )
                Utilities.Error( "model {0} has no vertices", mod.name );

            if( _Header.numverts > ModelDef.MAXALIASVERTS )
                Utilities.Error( "model {0} has too many vertices", mod.name );

            _Header.numtris = EndianHelper.LittleLong( pinmodel.numtris );

            if( _Header.numtris <= 0 )
                Utilities.Error( "model {0} has no triangles", mod.name );

            _Header.numframes = EndianHelper.LittleLong( pinmodel.numframes );
            var numframes = _Header.numframes;
            if( numframes < 1 )
                Utilities.Error( "Mod_LoadAliasModel: Invalid # of frames: {0}\n", numframes );

            _Header.size = EndianHelper.LittleFloat( pinmodel.size ) * ModelDef.ALIAS_BASE_SIZE_RATIO;
            mod.synctype = (SyncType) EndianHelper.LittleLong( ( Int32 ) pinmodel.synctype );
            mod.numframes = _Header.numframes;

            _Header.scale = EndianHelper.LittleVector( Utilities.ToVector( ref pinmodel.scale ) );
            _Header.scale_origin = EndianHelper.LittleVector( Utilities.ToVector( ref pinmodel.scale_origin ) );
            _Header.eyeposition = EndianHelper.LittleVector( Utilities.ToVector( ref pinmodel.eyeposition ) );

            //
            // load the skins
            //
            var offset = LoadAllSkins( _Header.numskins, new ByteArraySegment( buffer, mdl_t.SizeInBytes ) );

            //
            // load base s and t vertices
            //
            var stvOffset = offset; // in bytes
            for( var i = 0; i < _Header.numverts; i++, offset += stvert_t.SizeInBytes )
            {
                _STVerts[i] = Utilities.BytesToStructure<stvert_t>( buffer, offset );

                _STVerts[i].onseam = EndianHelper.LittleLong( _STVerts[i].onseam );
                _STVerts[i].s = EndianHelper.LittleLong( _STVerts[i].s );
                _STVerts[i].t = EndianHelper.LittleLong( _STVerts[i].t );
            }

            //
            // load triangle lists
            //
            var triOffset = stvOffset + _Header.numverts * stvert_t.SizeInBytes;
            offset = triOffset;
            for( var i = 0; i < _Header.numtris; i++, offset += dtriangle_t.SizeInBytes )
            {
                _Triangles[i] = Utilities.BytesToStructure<dtriangle_t>( buffer, offset );
                _Triangles[i].facesfront = EndianHelper.LittleLong( _Triangles[i].facesfront );

                for( var j = 0; j < 3; j++ )
                    _Triangles[i].vertindex[j] = EndianHelper.LittleLong( _Triangles[i].vertindex[j] );
            }

            //
            // load the frames
            //
            _PoseNum = 0;
            var framesOffset = triOffset + _Header.numtris * dtriangle_t.SizeInBytes;

            _Header.frames = new maliasframedesc_t[_Header.numframes];

            for( var i = 0; i < numframes; i++ )
            {
                var frametype = (aliasframetype_t)BitConverter.ToInt32( buffer, framesOffset );
                framesOffset += 4;

                if( frametype == aliasframetype_t.ALIAS_SINGLE )
                {
                    framesOffset = LoadAliasFrame( new ByteArraySegment( buffer, framesOffset ), ref _Header.frames[i] );
                }
                else
                {
                    framesOffset = LoadAliasGroup( new ByteArraySegment( buffer, framesOffset ), ref _Header.frames[i] );
                }
            }

            _Header.numposes = _PoseNum;

            mod.type = ModelType.mod_alias;

            // FIXME: do this right
            mod.mins = -Vector3.One * 16.0f;
            mod.maxs = -mod.mins;

            //
            // build the draw lists
            //
            mesh.MakeAliasModelDisplayLists( mod, _Header );

            //
            // move the complete, relocatable alias model to the cache
            //
            mod.cache = Host.Cache.Alloc( aliashdr_t.SizeInBytes * _Header.frames.Length * maliasframedesc_t.SizeInBytes, null );
            if( mod.cache == null )
                return;
            mod.cache.data = _Header;
        }

        /// <summary>
        /// Mod_LoadSpriteModel
        /// </summary>
        public void LoadSpriteModel( Model mod, Byte[] buffer )
        {
            var pin = Utilities.BytesToStructure<dsprite_t>( buffer, 0 );

            var version = EndianHelper.LittleLong( pin.version );
            if( version != ModelDef.SPRITE_VERSION )
                Utilities.Error( "{0} has wrong version number ({1} should be {2})",
                    mod.name, version, ModelDef.SPRITE_VERSION );

            var numframes = EndianHelper.LittleLong( pin.numframes );

            var psprite = new msprite_t();

            // Uze: sprite models are not cached so
            mod.cache = new CacheUser();
            mod.cache.data = psprite;

            psprite.type = EndianHelper.LittleLong( pin.type );
            psprite.maxwidth = EndianHelper.LittleLong( pin.width );
            psprite.maxheight = EndianHelper.LittleLong( pin.height );
            psprite.beamlength = EndianHelper.LittleFloat( pin.beamlength );
            mod.synctype = (SyncType)EndianHelper.LittleLong( ( Int32 ) pin.synctype );
            psprite.numframes = numframes;

            mod.mins.X = mod.mins.Y = -psprite.maxwidth / 2;
            mod.maxs.X = mod.maxs.Y = psprite.maxwidth / 2;
            mod.mins.Z = -psprite.maxheight / 2;
            mod.maxs.Z = psprite.maxheight / 2;

            //
            // load the frames
            //
            if( numframes < 1 )
                Utilities.Error( "Mod_LoadSpriteModel: Invalid # of frames: {0}\n", numframes );

            mod.numframes = numframes;

            var frameOffset = dsprite_t.SizeInBytes;

            psprite.frames = new mspriteframedesc_t[numframes];

            for( var i = 0; i < numframes; i++ )
            {
                var frametype = (spriteframetype_t)BitConverter.ToInt32( buffer, frameOffset );
                frameOffset += 4;

                psprite.frames[i].type = frametype;

                if( frametype == spriteframetype_t.SPR_SINGLE )
                {
                    frameOffset = LoadSpriteFrame( new ByteArraySegment( buffer, frameOffset ), out psprite.frames[i].frameptr, i );
                }
                else
                {
                    frameOffset = LoadSpriteGroup( new ByteArraySegment( buffer, frameOffset ), out psprite.frames[i].frameptr, i );
                }
            }

            mod.type = ModelType.mod_sprite;
        }

        /// <summary>
        /// Mod_LoadBrushModel
        /// </summary>
        public void LoadBrushModel( Model mod, Byte[] buffer )
        {
            mod.type = ModelType.mod_brush;

            var header = Utilities.BytesToStructure<BspHeader>( buffer, 0 );

            var i = EndianHelper.LittleLong( header.version );
            if( i != BspDef.Q1_BSPVERSION && i != BspDef.HL_BSPVERSION )
                Utilities.Error( "Mod_LoadBrushModel: {0} has wrong version number ({1} should be {2})", mod.name, i, BspDef.Q1_BSPVERSION );

            header.version = i;

            // swap all the lumps
            _ModBase = buffer;

            for( i = 0; i < header.lumps.Length; i++ )
            {
                header.lumps[i].filelen = EndianHelper.LittleLong( header.lumps[i].filelen );
                header.lumps[i].fileofs = EndianHelper.LittleLong( header.lumps[i].fileofs );
            }

            // load into heap

            LoadVertexes( ref header.lumps[LumpsDef.LUMP_VERTEXES] );
            LoadEdges( ref header.lumps[LumpsDef.LUMP_EDGES] );
            LoadSurfEdges( ref header.lumps[LumpsDef.LUMP_SURFEDGES] );
            LoadTextures( ref header.lumps[LumpsDef.LUMP_TEXTURES] );
            LoadLighting( ref header.lumps[LumpsDef.LUMP_LIGHTING] );
            LoadPlanes( ref header.lumps[LumpsDef.LUMP_PLANES] );
            LoadTexInfo( ref header.lumps[LumpsDef.LUMP_TEXINFO] );
            LoadFaces( ref header.lumps[LumpsDef.LUMP_FACES] );
            LoadMarkSurfaces( ref header.lumps[LumpsDef.LUMP_MARKSURFACES] );
            LoadVisibility( ref header.lumps[LumpsDef.LUMP_VISIBILITY] );
            LoadLeafs( ref header.lumps[LumpsDef.LUMP_LEAFS] );
            LoadNodes( ref header.lumps[LumpsDef.LUMP_NODES] );
            LoadClipNodes( ref header.lumps[LumpsDef.LUMP_CLIPNODES] );
            LoadEntities( ref header.lumps[LumpsDef.LUMP_ENTITIES] );
            LoadSubModels( ref header.lumps[LumpsDef.LUMP_MODELS] );

            MakeHull0();

            mod.numframes = 2;	// regular and alternate animation

            //
            // set up the submodels (FIXME: this is confusing)
            //
            for( i = 0; i < mod.numsubmodels; i++ )
            {
                SetupSubModel( mod, ref mod.submodels[i] );

                if( i < mod.numsubmodels - 1 )
                {
                    // duplicate the basic information
                    var name = "*" + ( i + 1 ).ToString();
                    _LoadModel = FindName( name );
                    _LoadModel.CopyFrom( mod ); // *loadmodel = *mod;
                    _LoadModel.name = name; //strcpy (loadmodel->name, name);
                    mod = _LoadModel; //mod = loadmodel;
                }
            }
        }

        /// <summary>
        /// Mod_DecompressVis
        /// </summary>
        private Byte[] DecompressVis( Byte[] p, Int32 startIndex, Model model )
        {
            var row = ( model.numleafs + 7 ) >> 3;
            var offset = 0;

            if( p == null )
            {
                // no vis info, so make all visible
                while( row != 0 )
                {
                    _Decompressed[offset++] = 0xff;
                    row--;
                }
                return _Decompressed;
            }
            var srcOffset = startIndex;
            do
            {
                if( p[srcOffset] != 0 )// (*in)
                {
                    _Decompressed[offset++] = p[srcOffset++]; //  *out++ = *in++;
                    continue;
                }

                Int32 c = p[srcOffset + 1];// in[1];
                srcOffset += 2; // in += 2;
                while( c != 0 )
                {
                    _Decompressed[offset++] = 0; // *out++ = 0;
                    c--;
                }
            } while( offset < row ); // out - decompressed < row

            return _Decompressed;
        }

        private void SetupSubModel( Model mod, ref BspModel submodel )
        {
            mod.hulls[0].firstclipnode = submodel.headnode[0];
            for( var j = 1; j < BspDef.MAX_MAP_HULLS; j++ )
            {
                mod.hulls[j].firstclipnode = submodel.headnode[j];
                mod.hulls[j].lastclipnode = mod.numclipnodes - 1;
            }
            mod.firstmodelsurface = submodel.firstface;
            mod.nummodelsurfaces = submodel.numfaces;
            Utilities.Copy( submodel.maxs, out mod.maxs ); // mod.maxs = submodel.maxs;
            Utilities.Copy( submodel.mins, out mod.mins ); // mod.mins = submodel.mins;
            mod.radius = RadiusFromBounds( ref mod.mins, ref mod.maxs );
            mod.numleafs = submodel.visleafs;
        }

        /// <summary>
        /// Mod_LoadAllSkins
        /// </summary>
        /// <returns>Offset of next data block in source byte array</returns>
        private Int32 LoadAllSkins( Int32 numskins, ByteArraySegment data )
        {
            if( numskins < 1 || numskins > ModelDef.MAX_SKINS )
                Utilities.Error( "Mod_LoadAliasModel: Invalid # of skins: {0}\n", numskins );

            var offset = data.StartIndex;
            var skinOffset = data.StartIndex + daliasskintype_t.SizeInBytes; //  skin = (byte*)(pskintype + 1);
            var s = _Header.skinwidth * _Header.skinheight;

            var pskintype = Utilities.BytesToStructure<daliasskintype_t>( data.Data, offset );

            for( var i = 0; i < numskins; i++ )
            {
                if( pskintype.type == aliasskintype_t.ALIAS_SKIN_SINGLE )
                {
                    FloodFillSkin( new ByteArraySegment( data.Data, skinOffset ), _Header.skinwidth, _Header.skinheight );

                    // save 8 bit texels for the player model to remap
                    var texels = new Byte[s]; // Hunk_AllocName(s, loadname);
                    _Header.texels[i] = texels;// -(byte*)pheader;
                    Buffer.BlockCopy( data.Data, offset + daliasskintype_t.SizeInBytes, texels, 0, s );

                    // set offset to pixel data after daliasskintype_t block...
                    offset += daliasskintype_t.SizeInBytes;

                    var name = _LoadModel.name + "_" + i.ToString();
                    _Header.gl_texturenum[i, 0] =
                    _Header.gl_texturenum[i, 1] =
                    _Header.gl_texturenum[i, 2] =
                    _Header.gl_texturenum[i, 3] =
                        Host.DrawingContext.LoadTexture( name, _Header.skinwidth,
                        _Header.skinheight, new ByteArraySegment( data.Data, offset ), true, false ); // (byte*)(pskintype + 1)

                    // set offset to next daliasskintype_t block...
                    offset += s;
                    pskintype = Utilities.BytesToStructure<daliasskintype_t>( data.Data, offset );
                }
                else
                {
                    // animating skin group.  yuck.
                    offset += daliasskintype_t.SizeInBytes;
                    var pinskingroup = Utilities.BytesToStructure<daliasskingroup_t>( data.Data, offset );
                    var groupskins = EndianHelper.LittleLong( pinskingroup.numskins );
                    offset += daliasskingroup_t.SizeInBytes;
                    var pinskinintervals = Utilities.BytesToStructure<daliasskininterval_t>( data.Data, offset );

                    offset += daliasskininterval_t.SizeInBytes * groupskins;

                    pskintype = Utilities.BytesToStructure<daliasskintype_t>( data.Data, offset );
                    Int32 j;
                    for( j = 0; j < groupskins; j++ )
                    {
                        FloodFillSkin( new ByteArraySegment( data.Data, skinOffset ), _Header.skinwidth, _Header.skinheight );
                        if( j == 0 )
                        {
                            var texels = new Byte[s]; // Hunk_AllocName(s, loadname);
                            _Header.texels[i] = texels;// -(byte*)pheader;
                            Buffer.BlockCopy( data.Data, offset, texels, 0, s );
                        }

                        var name = String.Format( "{0}_{1}_{2}", _LoadModel.name, i, j );
                        _Header.gl_texturenum[i, j & 3] =
                            Host.DrawingContext.LoadTexture( name, _Header.skinwidth,
                            _Header.skinheight, new ByteArraySegment( data.Data, offset ), true, false ); //  (byte*)(pskintype)

                        offset += s;

                        pskintype = Utilities.BytesToStructure<daliasskintype_t>( data.Data, offset );
                    }
                    var k = j;
                    for( ; j < 4; j++ )
                        _Header.gl_texturenum[i, j & 3] = _Header.gl_texturenum[i, j - k];
                }
            }

            return offset;// (void*)pskintype;
        }

        /// <summary>
        /// Mod_LoadAliasFrame
        /// </summary>
        /// <returns>Offset of next data block in source byte array</returns>
        private Int32 LoadAliasFrame( ByteArraySegment pin, ref maliasframedesc_t frame )
        {
            var pdaliasframe = Utilities.BytesToStructure<daliasframe_t>( pin.Data, pin.StartIndex );

            frame.name = Utilities.GetString( pdaliasframe.name );
            frame.firstpose = _PoseNum;
            frame.numposes = 1;
            frame.bboxmin.Init();
            frame.bboxmax.Init();

            for( var i = 0; i < 3; i++ )
            {
                // these are byte values, so we don't have to worry about
                // endianness
                frame.bboxmin.v[i] = pdaliasframe.bboxmin.v[i];
                frame.bboxmax.v[i] = pdaliasframe.bboxmax.v[i];
            }

            var verts = new trivertx_t[_Header.numverts];
            var offset = pin.StartIndex + daliasframe_t.SizeInBytes; //pinframe = (trivertx_t*)(pdaliasframe + 1);
            for( var i = 0; i < verts.Length; i++, offset += trivertx_t.SizeInBytes )
            {
                verts[i] = Utilities.BytesToStructure<trivertx_t>( pin.Data, offset );
            }
            _PoseVerts[_PoseNum] = verts;
            _PoseNum++;

            return offset;
        }

        /// <summary>
        /// Mod_LoadAliasGroup
        /// </summary>
        /// <returns>Offset of next data block in source byte array</returns>
        private Int32 LoadAliasGroup( ByteArraySegment pin, ref maliasframedesc_t frame )
        {
            var offset = pin.StartIndex;
            var pingroup = Utilities.BytesToStructure<daliasgroup_t>( pin.Data, offset );
            var numframes = EndianHelper.LittleLong( pingroup.numframes );

            frame.Init();
            frame.firstpose = _PoseNum;
            frame.numposes = numframes;

            for( var i = 0; i < 3; i++ )
            {
                // these are byte values, so we don't have to worry about endianness
                frame.bboxmin.v[i] = pingroup.bboxmin.v[i];
                frame.bboxmin.v[i] = pingroup.bboxmax.v[i];
            }

            offset += daliasgroup_t.SizeInBytes;
            var pin_intervals = Utilities.BytesToStructure<daliasinterval_t>( pin.Data, offset ); // (daliasinterval_t*)(pingroup + 1);

            frame.interval = EndianHelper.LittleFloat( pin_intervals.interval );

            offset += numframes * daliasinterval_t.SizeInBytes;

            for( var i = 0; i < numframes; i++ )
            {
                var tris = new trivertx_t[_Header.numverts];
                var offset1 = offset + daliasframe_t.SizeInBytes;
                for( var j = 0; j < _Header.numverts; j++, offset1 += trivertx_t.SizeInBytes )
                {
                    tris[j] = Utilities.BytesToStructure<trivertx_t>( pin.Data, offset1 );
                }
                _PoseVerts[_PoseNum] = tris;
                _PoseNum++;

                offset += daliasframe_t.SizeInBytes + _Header.numverts * trivertx_t.SizeInBytes;
            }

            return offset;
        }

        /// <summary>
        ///
        /// </summary>
        /// <returns>Offset of next data block</returns>
        private Int32 LoadSpriteFrame( ByteArraySegment pin, out Object ppframe, Int32 framenum )
        {
            var pinframe = Utilities.BytesToStructure<dspriteframe_t>( pin.Data, pin.StartIndex );

            var width = EndianHelper.LittleLong( pinframe.width );
            var height = EndianHelper.LittleLong( pinframe.height );
            var size = width * height;

            var pspriteframe = new mspriteframe_t();

            ppframe = pspriteframe;

            pspriteframe.width = width;
            pspriteframe.height = height;
            var orgx = EndianHelper.LittleLong( pinframe.origin[0] );
            var orgy = EndianHelper.LittleLong( pinframe.origin[1] );

            pspriteframe.up = orgy;// origin[1];
            pspriteframe.down = orgy - height;
            pspriteframe.left = orgx;// origin[0];
            pspriteframe.right = width + orgx;// origin[0];

            var name = _LoadModel.name + "_" + framenum.ToString();
            pspriteframe.gl_texturenum = Host.DrawingContext.LoadTexture( name, width, height,
                new ByteArraySegment( pin.Data, pin.StartIndex + dspriteframe_t.SizeInBytes ), true, true ); //   (byte *)(pinframe + 1)

            return pin.StartIndex + dspriteframe_t.SizeInBytes + size;
        }

        /// <summary>
        /// Mod_LoadSpriteGroup
        /// </summary>
        private Int32 LoadSpriteGroup( ByteArraySegment pin, out Object ppframe, Int32 framenum )
        {
            var pingroup = Utilities.BytesToStructure<dspritegroup_t>( pin.Data, pin.StartIndex );

            var numframes = EndianHelper.LittleLong( pingroup.numframes );
            var pspritegroup = new mspritegroup_t();
            pspritegroup.numframes = numframes;
            pspritegroup.frames = new mspriteframe_t[numframes];
            ppframe = pspritegroup;// (mspriteframe_t*)pspritegroup;
            var poutintervals = new Single[numframes];
            pspritegroup.intervals = poutintervals;

            var offset = pin.StartIndex + dspritegroup_t.SizeInBytes;
            for( var i = 0; i < numframes; i++, offset += dspriteinterval_t.SizeInBytes )
            {
                var interval = Utilities.BytesToStructure<dspriteinterval_t>( pin.Data, offset );
                poutintervals[i] = EndianHelper.LittleFloat( interval.interval );
                if( poutintervals[i] <= 0 )
                    Utilities.Error( "Mod_LoadSpriteGroup: interval<=0" );
            }

            for( var i = 0; i < numframes; i++ )
            {
                Object tmp;
                offset = LoadSpriteFrame( new ByteArraySegment( pin.Data, offset ), out tmp, framenum * 100 + i );
                pspritegroup.frames[i] = (mspriteframe_t)tmp;
            }

            return offset;
        }

        /// <summary>
        /// Mod_LoadVertexes
        /// </summary>
        private void LoadVertexes( ref BspLump l )
        {
            if( ( l.filelen % BspVertex.SizeInBytes ) != 0 )
                Utilities.Error( "MOD_LoadBmodel: funny lump size in {0}", _LoadModel.name );

            var count = l.filelen / BspVertex.SizeInBytes;
            var verts = new MemoryVertex[count];

            _LoadModel.vertexes = verts;
            _LoadModel.numvertexes = count;

            for( Int32 i = 0, offset = l.fileofs; i < count; i++, offset += BspVertex.SizeInBytes )
            {
                var src = Utilities.BytesToStructure<BspVertex>( _ModBase, offset );
                verts[i].position = EndianHelper.LittleVector3( src.point );
            }
        }

        /// <summary>
        /// Mod_LoadEdges
        /// </summary>
        private void LoadEdges( ref BspLump l )
        {
            if( ( l.filelen % BspEdge.SizeInBytes ) != 0 )
                Utilities.Error( "MOD_LoadBmodel: funny lump size in {0}", _LoadModel.name );

            var count = l.filelen / BspEdge.SizeInBytes;

            // Uze: Why count + 1 ?????
            var edges = new MemoryEdge[count]; // out = Hunk_AllocName ( (count + 1) * sizeof(*out), loadname);
            _LoadModel.edges = edges;
            _LoadModel.numedges = count;

            for( Int32 i = 0, offset = l.fileofs; i < count; i++, offset += BspEdge.SizeInBytes )
            {
                var src = Utilities.BytesToStructure<BspEdge>( _ModBase, offset );
                edges[i].v = new UInt16[] {
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
            if( ( l.filelen % sizeof( Int32 ) ) != 0 )
                Utilities.Error( "MOD_LoadBmodel: funny lump size in {0}", _LoadModel.name );

            var count = l.filelen / sizeof( Int32 );
            var edges = new Int32[count];

            _LoadModel.surfedges = edges;
            _LoadModel.numsurfedges = count;

            for( Int32 i = 0, offset = l.fileofs; i < count; i++, offset += 4 )
            {
                var src = BitConverter.ToInt32( _ModBase, offset );
                edges[i] = src; // EndianHelper.LittleLong(in[i]);
            }
        }

        /// <summary>
        /// Mod_LoadTextures
        /// </summary>
        private void LoadTextures( ref BspLump l )
        {
            if( l.filelen == 0 )
            {
                _LoadModel.textures = null;
                return;
            }

            var m = Utilities.BytesToStructure<BspMipTexLump>( _ModBase, l.fileofs );// (dmiptexlump_t *)(mod_base + l.fileofs);

            m.nummiptex = EndianHelper.LittleLong( m.nummiptex );

            var dataofs = new Int32[m.nummiptex];

            Buffer.BlockCopy( _ModBase, l.fileofs + BspMipTexLump.SizeInBytes, dataofs, 0, dataofs.Length * sizeof( Int32 ) );

            _LoadModel.numtextures = m.nummiptex;
            _LoadModel.textures = new Texture[m.nummiptex]; // Hunk_AllocName (m->nummiptex * sizeof(*loadmodel->textures) , loadname);

            for( var i = 0; i < m.nummiptex; i++ )
            {
                dataofs[i] = EndianHelper.LittleLong( dataofs[i] );
                if( dataofs[i] == -1 )
                    continue;

                var mtOffset = l.fileofs + dataofs[i];
                var mt = Utilities.BytesToStructure<BspMipTex>( _ModBase, mtOffset ); //mt = (miptex_t *)((byte *)m + m.dataofs[i]);
                mt.width = ( UInt32 ) EndianHelper.LittleLong( ( Int32 ) mt.width );
                mt.height = ( UInt32 ) EndianHelper.LittleLong( ( Int32 ) mt.height );
                for( var j = 0; j < BspDef.MIPLEVELS; j++ )
                    mt.offsets[j] = ( UInt32 ) EndianHelper.LittleLong( ( Int32 ) mt.offsets[j] );

                if( ( mt.width & 15 ) != 0 || ( mt.height & 15 ) != 0 )
                    Utilities.Error( "Texture {0} is not 16 aligned", mt.name );

                var pixels = ( Int32 ) ( mt.width * mt.height / 64 * 85 );
                var tx = new Texture();// Hunk_AllocName(sizeof(texture_t) + pixels, loadname);
                _LoadModel.textures[i] = tx;

                tx.name = Utilities.GetString( mt.name );//   memcpy (tx->name, mt->name, sizeof(tx.name));

#warning Needs to fix TGA loading / come up with better image loading

                var tgaName = $"textures/{tx.name}.tga";

                var file = FileSystem.LoadFile( tgaName );

                if ( file != null )
                {
                    Byte[] tgapixels = null;

                    using ( var reader = new System.IO.BinaryReader( new MemoryStream( file ) ) )
                    {
                        var tga = new TgaLib.TgaImage( reader );
                        var source = tga.GetBitmap( );

                        var bmp = new System.Drawing.Bitmap( source.PixelWidth, source.PixelHeight, System.Drawing.Imaging.PixelFormat.Format32bppPArgb );
                        var data = bmp.LockBits(
                          new System.Drawing.Rectangle( System.Drawing.Point.Empty, bmp.Size ),
                          System.Drawing.Imaging.ImageLockMode.WriteOnly,
                          System.Drawing.Imaging.PixelFormat.Format24bppRgb );
                        source.CopyPixels(
                          System.Windows.Int32Rect.Empty,
                          data.Scan0,
                          data.Height * data.Stride,
                          data.Stride );
                        bmp.UnlockBits( data );

                        tx.width = ( UInt32 ) tga.Header.Width;
                        tx.height = ( UInt32 ) tga.Header.Height;

                        tx.scaleX = ( ( Single ) tx.width / ( Single ) mt.width );
                        tx.scaleY = ( ( Single ) tx.height / ( Single ) mt.height );

                        mt.width = tx.width;
                        mt.height = tx.height;

                        tx.rawBitmap = bmp;
                    }
                }
                else
                {
                    tx.width = mt.width;
                    tx.height = mt.height;
                    tx.scaleX = 1f;
                    tx.scaleY = 1f;

                    if ( mt.offsets[0] == 0 )
                        continue;

                    for ( var j = 0; j < BspDef.MIPLEVELS; j++ )
                        tx.offsets[j] = ( Int32 ) mt.offsets[j] - BspMipTex.SizeInBytes;

                    // the pixels immediately follow the structures
                    tx.pixels = new Byte[pixels];
    #warning BlockCopy tries to copy data over the bounds of _ModBase if certain mods are loaded. Needs proof fix!
                    if (mtOffset + BspMipTex.SizeInBytes + pixels <= _ModBase.Length)
                        Buffer.BlockCopy(_ModBase, mtOffset + BspMipTex.SizeInBytes, tx.pixels, 0, pixels);
                    else
                    {
                        Buffer.BlockCopy(_ModBase, mtOffset, tx.pixels, 0, pixels);
                        ConsoleWrapper.Print("Texture info of {0} truncated to fit in bounds of _ModBase\n", _LoadModel.name);
                    }
                }

                if ( tx.name != null && tx.name.StartsWith( "sky" ) )// !Q_strncmp(mt->name,"sky",3))
                    Host.RenderContext.InitSky( tx );
                else
                {
                    if ( tx.rawBitmap == null )
                    {
                        tx.gl_texturenum = Host.DrawingContext.LoadTexture( tx.name, ( Int32 ) tx.width, ( Int32 ) tx.height,
                            new ByteArraySegment( tx.pixels ), true, false, _LoadModel.name );
                    }
                    else
                    {
                        tx.gl_texturenum = Host.DrawingContext.LoadTexture( tx.name, ( Int32 ) tx.width, ( Int32 ) tx.height,
                            tx.rawBitmap, true, false, _LoadModel.name );
                    }
                }
            }

            //
            // sequence the animations
            //
            var anims = new Texture[10];
            var altanims = new Texture[10];

            for( var i = 0; i < m.nummiptex; i++ )
            {
                var tx = _LoadModel.textures[i];
                if( tx == null || !tx.name.StartsWith( "+" ) )// [0] != '+')
                    continue;
                if( tx.anim_next != null )
                    continue;	// allready sequenced

                // find the number of frames in the animation
                Array.Clear( anims, 0, anims.Length );
                Array.Clear( altanims, 0, altanims.Length );

                Int32 max = tx.name[1];
                var altmax = 0;
                if( max >= 'a' && max <= 'z' )
                    max -= 'a' - 'A';
                if( max >= '0' && max <= '9' )
                {
                    max -= '0';
                    altmax = 0;
                    anims[max] = tx;
                    max++;
                }
                else if( max >= 'A' && max <= 'J' )
                {
                    altmax = max - 'A';
                    max = 0;
                    altanims[altmax] = tx;
                    altmax++;
                }
                else
                    Utilities.Error( "Bad animating texture {0}", tx.name );

                for( var j = i + 1; j < m.nummiptex; j++ )
                {
                    var tx2 = _LoadModel.textures[j];
                    if( tx2 == null || !tx2.name.StartsWith( "+" ) )// tx2->name[0] != '+')
                        continue;
                    if( String.Compare( tx2.name, 2, tx.name, 2, Math.Min( tx.name.Length, tx2.name.Length ) ) != 0 )// strcmp (tx2->name+2, tx->name+2))
                        continue;

                    Int32 num = tx2.name[1];
                    if( num >= 'a' && num <= 'z' )
                        num -= 'a' - 'A';
                    if( num >= '0' && num <= '9' )
                    {
                        num -= '0';
                        anims[num] = tx2;
                        if( num + 1 > max )
                            max = num + 1;
                    }
                    else if( num >= 'A' && num <= 'J' )
                    {
                        num = num - 'A';
                        altanims[num] = tx2;
                        if( num + 1 > altmax )
                            altmax = num + 1;
                    }
                    else
                        Utilities.Error( "Bad animating texture {0}", tx2.name );
                }

                // link them all together
                for( var j = 0; j < max; j++ )
                {
                    var tx2 = anims[j];
                    if( tx2 == null )
                        Utilities.Error( "Missing frame {0} of {1}", j, tx.name );
                    tx2.anim_total = max * ModelDef.ANIM_CYCLE;
                    tx2.anim_min = j * ModelDef.ANIM_CYCLE;
                    tx2.anim_max = ( j + 1 ) * ModelDef.ANIM_CYCLE;
                    tx2.anim_next = anims[( j + 1 ) % max];
                    if( altmax != 0 )
                        tx2.alternate_anims = altanims[0];
                }
                for( var j = 0; j < altmax; j++ )
                {
                    var tx2 = altanims[j];
                    if( tx2 == null )
                        Utilities.Error( "Missing frame {0} of {1}", j, tx2.name );
                    tx2.anim_total = altmax * ModelDef.ANIM_CYCLE;
                    tx2.anim_min = j * ModelDef.ANIM_CYCLE;
                    tx2.anim_max = ( j + 1 ) * ModelDef.ANIM_CYCLE;
                    tx2.anim_next = altanims[( j + 1 ) % altmax];
                    if( max != 0 )
                        tx2.alternate_anims = anims[0];
                }
            }
        }

        /// <summary>
        /// Mod_LoadLighting
        /// </summary>
        private void LoadLighting( ref BspLump l )
        {
            if( l.filelen == 0 )
            {
                _LoadModel.lightdata = null;
                return;
            }
            _LoadModel.lightdata = new Byte[l.filelen]; // Hunk_AllocName(l->filelen, loadname);
            Buffer.BlockCopy( _ModBase, l.fileofs, _LoadModel.lightdata, 0, l.filelen );
        }

        /// <summary>
        /// Mod_LoadPlanes
        /// </summary>
        private void LoadPlanes( ref BspLump l )
        {
            if( ( l.filelen % BspPlane.SizeInBytes ) != 0 )
                Utilities.Error( "MOD_LoadBmodel: funny lump size in {0}", _LoadModel.name );

            var count = l.filelen / BspPlane.SizeInBytes;
            // Uze: Possible error! Why in original is out = Hunk_AllocName ( count*2*sizeof(*out), loadname)???
            var planes = new Plane[count];

            for( var i = 0; i < planes.Length; i++ )
                planes[i] = new Plane();

            _LoadModel.planes = planes;
            _LoadModel.numplanes = count;

            for( var i = 0; i < count; i++ )
            {
                var src = Utilities.BytesToStructure<BspPlane>( _ModBase, l.fileofs + i * BspPlane.SizeInBytes );
                var bits = 0;
                planes[i].normal = EndianHelper.LittleVector3( src.normal );
                if( planes[i].normal.X < 0 )
                    bits |= 1;
                if( planes[i].normal.Y < 0 )
                    bits |= 1 << 1;
                if( planes[i].normal.Z < 0 )
                    bits |= 1 << 2;
                planes[i].dist = EndianHelper.LittleFloat( src.dist );
                planes[i].type = ( Byte ) EndianHelper.LittleLong( src.type );
                planes[i].signbits = ( Byte ) bits;
            }
        }

        /// <summary>
        /// Mod_LoadTexinfo
        /// </summary>
        private void LoadTexInfo( ref BspLump l )
        {
            //in = (void *)(mod_base + l->fileofs);
            if( ( l.filelen % BspTextureInfo.SizeInBytes ) != 0 )
                Utilities.Error( "MOD_LoadBmodel: funny lump size in {0}", _LoadModel.name );

            var count = l.filelen / BspTextureInfo.SizeInBytes;
            var infos = new MemoryTextureInfo[count]; // out = Hunk_AllocName ( count*sizeof(*out), loadname);

            for( var i = 0; i < infos.Length; i++ )
                infos[i] = new MemoryTextureInfo();

            _LoadModel.texinfo = infos;
            _LoadModel.numtexinfo = count;

            for( var i = 0; i < count; i++ )//, in++, out++)
            {
                var src = Utilities.BytesToStructure<BspTextureInfo>( _ModBase, l.fileofs + i * BspTextureInfo.SizeInBytes );

                for( var j = 0; j < 2; j++ )
                    infos[i].vecs[j] = EndianHelper.LittleVector4( src.vecs, j * 4 );

                var len1 = infos[i].vecs[0].Length;
                var len2 = infos[i].vecs[1].Length;
                len1 = ( len1 + len2 ) / 2;
                if( len1 < 0.32 )
                    infos[i].mipadjust = 4;
                else if( len1 < 0.49 )
                    infos[i].mipadjust = 3;
                else if( len1 < 0.99 )
                    infos[i].mipadjust = 2;
                else
                    infos[i].mipadjust = 1;

                var miptex = EndianHelper.LittleLong( src.miptex );
                infos[i].flags = EndianHelper.LittleLong( src.flags );

                if( _LoadModel.textures == null )
                {
                    infos[i].texture = Host.RenderContext.NoTextureMip;	// checkerboard texture
                    infos[i].flags = 0;
                }
                else
                {
                    if( miptex >= _LoadModel.numtextures )
                        Utilities.Error( "miptex >= loadmodel->numtextures" );
                    infos[i].texture = _LoadModel.textures[miptex];
                    if( infos[i].texture == null )
                    {
                        infos[i].texture = Host.RenderContext.NoTextureMip; // texture not found
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
            if( ( l.filelen % BspFace.SizeInBytes ) != 0 )
                Utilities.Error( "MOD_LoadBmodel: funny lump size in {0}", _LoadModel.name );

            var count = l.filelen / BspFace.SizeInBytes;
            var dest = new MemorySurface[count];

            for( var i = 0; i < dest.Length; i++ )
                dest[i] = new MemorySurface();

            _LoadModel.surfaces = dest;
            _LoadModel.numsurfaces = count;
            var offset = l.fileofs;
            for( var surfnum = 0; surfnum < count; surfnum++, offset += BspFace.SizeInBytes )
            {
                var src = Utilities.BytesToStructure<BspFace>( _ModBase, offset );

                dest[surfnum].firstedge = EndianHelper.LittleLong( src.firstedge );
                dest[surfnum].numedges = EndianHelper.LittleShort( src.numedges );
                dest[surfnum].flags = 0;

                Int32 planenum = EndianHelper.LittleShort( src.planenum );
                Int32 side = EndianHelper.LittleShort( src.side );
                if( side != 0 )
                    dest[surfnum].flags |= SurfaceDef.SURF_PLANEBACK;

                dest[surfnum].plane = _LoadModel.planes[planenum];
                dest[surfnum].texinfo = _LoadModel.texinfo[EndianHelper.LittleShort( src.texinfo )];

                CalcSurfaceExtents( dest[surfnum] );

                // lighting info

                for( var i = 0; i < BspDef.MAXLIGHTMAPS; i++ )
                    dest[surfnum].styles[i] = src.styles[i];

                var i2 = EndianHelper.LittleLong( src.lightofs );
                if( i2 == -1 )
                    dest[surfnum].sample_base = null;
                else
                {
                    dest[surfnum].sample_base = _LoadModel.lightdata;
                    dest[surfnum].sampleofs = i2;
                }

                // set the drawing flags flag
                if( dest[surfnum].texinfo.texture.name != null )
                {
                    if( dest[surfnum].texinfo.texture.name.StartsWith( "sky" ) )	// sky
                    {
                        dest[surfnum].flags |= ( SurfaceDef.SURF_DRAWSKY | SurfaceDef.SURF_DRAWTILED );
                        Host.RenderContext.SubdivideSurface( dest[surfnum] );	// cut up polygon for warps
                        continue;
                    }

                    if( dest[surfnum].texinfo.texture.name.StartsWith( "*" ) )		// turbulent
                    {
                        dest[surfnum].flags |= ( SurfaceDef.SURF_DRAWTURB | SurfaceDef.SURF_DRAWTILED );
                        for( var i = 0; i < 2; i++ )
                        {
                            dest[surfnum].extents[i] = 16384;
                            dest[surfnum].texturemins[i] = -8192;
                        }
                        Host.RenderContext.SubdivideSurface( dest[surfnum] );	// cut up polygon for warps
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
            if( ( l.filelen % sizeof( Int16 ) ) != 0 )
                Utilities.Error( "MOD_LoadBmodel: funny lump size in {0}", _LoadModel.name );

            var count = l.filelen / sizeof( Int16 );
            var dest = new MemorySurface[count];

            _LoadModel.marksurfaces = dest;
            _LoadModel.nummarksurfaces = count;

            for( var i = 0; i < count; i++ )
            {
                Int32 j = BitConverter.ToInt16( _ModBase, l.fileofs + i * sizeof( Int16 ) );
                if( j >= _LoadModel.numsurfaces )
                    Utilities.Error( "Mod_ParseMarksurfaces: bad surface number" );
                dest[i] = _LoadModel.surfaces[j];
            }
        }

        /// <summary>
        /// Mod_LoadVisibility
        /// </summary>
        private void LoadVisibility( ref BspLump l )
        {
            if( l.filelen == 0 )
            {
                _LoadModel.visdata = null;
                return;
            }
            _LoadModel.visdata = new Byte[l.filelen];
            Buffer.BlockCopy( _ModBase, l.fileofs, _LoadModel.visdata, 0, l.filelen );
        }

        /// <summary>
        /// Mod_LoadLeafs
        /// </summary>
        private void LoadLeafs( ref BspLump l )
        {
            if( ( l.filelen % BspLeaf.SizeInBytes ) != 0 )
                Utilities.Error( "MOD_LoadBmodel: funny lump size in {0}", _LoadModel.name );

            var count = l.filelen / BspLeaf.SizeInBytes;
            var dest = new MemoryLeaf[count];

            for( var i = 0; i < dest.Length; i++ )
                dest[i] = new MemoryLeaf();

            _LoadModel.leafs = dest;
            _LoadModel.numleafs = count;

            for( Int32 i = 0, offset = l.fileofs; i < count; i++, offset += BspLeaf.SizeInBytes )
            {
                var src = Utilities.BytesToStructure<BspLeaf>( _ModBase, offset );

                dest[i].mins.X = EndianHelper.LittleShort( src.mins[0] );
                dest[i].mins.Y = EndianHelper.LittleShort( src.mins[1] );
                dest[i].mins.Z = EndianHelper.LittleShort( src.mins[2] );

                dest[i].maxs.X = EndianHelper.LittleShort( src.maxs[0] );
                dest[i].maxs.Y = EndianHelper.LittleShort( src.maxs[1] );
                dest[i].maxs.Z = EndianHelper.LittleShort( src.maxs[2] );

                var p = EndianHelper.LittleLong( src.contents );
                dest[i].contents = p;

                dest[i].marksurfaces = _LoadModel.marksurfaces;
                dest[i].firstmarksurface = EndianHelper.LittleShort( ( Int16 ) src.firstmarksurface );
                dest[i].nummarksurfaces = EndianHelper.LittleShort( ( Int16 ) src.nummarksurfaces );

                p = EndianHelper.LittleLong( src.visofs );
                if( p == -1 )
                    dest[i].compressed_vis = null;
                else
                {
                    dest[i].compressed_vis = _LoadModel.visdata; // loadmodel->visdata + p;
                    dest[i].visofs = p;
                }
                dest[i].efrags = null;

                for( var j = 0; j < 4; j++ )
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
            if( ( l.filelen % BspNode.SizeInBytes ) != 0 )
                Utilities.Error( "MOD_LoadBmodel: funny lump size in {0}", _LoadModel.name );

            var count = l.filelen / BspNode.SizeInBytes;
            var dest = new MemoryNode[count];

            for( var i = 0; i < dest.Length; i++ )
                dest[i] = new MemoryNode();

            _LoadModel.nodes = dest;
            _LoadModel.numnodes = count;

            for( Int32 i = 0, offset = l.fileofs; i < count; i++, offset += BspNode.SizeInBytes )
            {
                var src = Utilities.BytesToStructure<BspNode>( _ModBase, offset );

                dest[i].mins.X = EndianHelper.LittleShort( src.mins[0] );
                dest[i].mins.Y = EndianHelper.LittleShort( src.mins[1] );
                dest[i].mins.Z = EndianHelper.LittleShort( src.mins[2] );

                dest[i].maxs.X = EndianHelper.LittleShort( src.maxs[0] );
                dest[i].maxs.Y = EndianHelper.LittleShort( src.maxs[1] );
                dest[i].maxs.Z = EndianHelper.LittleShort( src.maxs[2] );

                var p = EndianHelper.LittleLong( src.planenum );
                dest[i].plane = _LoadModel.planes[p];

                dest[i].firstsurface = ( UInt16 ) EndianHelper.LittleShort( ( Int16 ) src.firstface );
                dest[i].numsurfaces = ( UInt16 ) EndianHelper.LittleShort( ( Int16 ) src.numfaces );

                for( var j = 0; j < 2; j++ )
                {
                    p = EndianHelper.LittleShort( src.children[j] );
                    if( p >= 0 )
                        dest[i].children[j] = _LoadModel.nodes[p];
                    else
                        dest[i].children[j] = _LoadModel.leafs[-1 - p];
                }
            }

            SetParent( _LoadModel.nodes[0], null );	// sets nodes and leafs
        }

        /// <summary>
        /// Mod_LoadClipnodes
        /// </summary>
        private void LoadClipNodes( ref BspLump l )
        {
            if( ( l.filelen % BspClipNode.SizeInBytes ) != 0 )
                Utilities.Error( "MOD_LoadBmodel: funny lump size in {0}", _LoadModel.name );

            var count = l.filelen / BspClipNode.SizeInBytes;
            var dest = new BspClipNode[count];

            _LoadModel.clipnodes = dest;
            _LoadModel.numclipnodes = count;

            var hull = _LoadModel.hulls[1];
            hull.clipnodes = dest;
            hull.firstclipnode = 0;
            hull.lastclipnode = count - 1;
            hull.planes = _LoadModel.planes;
            hull.clip_mins.X = -16;
            hull.clip_mins.Y = -16;
            hull.clip_mins.Z = -24;
            hull.clip_maxs.X = 16;
            hull.clip_maxs.Y = 16;
            hull.clip_maxs.Z = 32;

            hull = _LoadModel.hulls[2];
            hull.clipnodes = dest;
            hull.firstclipnode = 0;
            hull.lastclipnode = count - 1;
            hull.planes = _LoadModel.planes;
            hull.clip_mins.X = -32;
            hull.clip_mins.Y = -32;
            hull.clip_mins.Z = -24;
            hull.clip_maxs.X = 32;
            hull.clip_maxs.Y = 32;
            hull.clip_maxs.Z = 64;

            for( Int32 i = 0, offset = l.fileofs; i < count; i++, offset += BspClipNode.SizeInBytes )
            {
                var src = Utilities.BytesToStructure<BspClipNode>( _ModBase, offset );

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
            if( l.filelen == 0 )
            {
                _LoadModel.entities = null;
                return;
            }
            _LoadModel.entities = Encoding.ASCII.GetString( _ModBase, l.fileofs, l.filelen );
        }

        /// <summary>
        /// Mod_LoadSubmodels
        /// </summary>
        private void LoadSubModels( ref BspLump l )
        {
            if( ( l.filelen % BspModel.SizeInBytes ) != 0 )
                Utilities.Error( "MOD_LoadBmodel: funny lump size in {0}", _LoadModel.name );

            var count = l.filelen / BspModel.SizeInBytes;
            var dest = new BspModel[count];

            _LoadModel.submodels = dest;
            _LoadModel.numsubmodels = count;

            for( Int32 i = 0, offset = l.fileofs; i < count; i++, offset += BspModel.SizeInBytes )
            {
                var src = Utilities.BytesToStructure<BspModel>( _ModBase, offset );

                dest[i].mins = new Single[3];
                dest[i].maxs = new Single[3];
                dest[i].origin = new Single[3];

                for( var j = 0; j < 3; j++ )
                {
                    // spread the mins / maxs by a pixel
                    dest[i].mins[j] = EndianHelper.LittleFloat( src.mins[j] ) - 1;
                    dest[i].maxs[j] = EndianHelper.LittleFloat( src.maxs[j] ) + 1;
                    dest[i].origin[j] = EndianHelper.LittleFloat( src.origin[j] );
                }

                dest[i].headnode = new Int32[BspDef.MAX_MAP_HULLS];
                for( var j = 0; j < BspDef.MAX_MAP_HULLS; j++ )
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
        private void MakeHull0()
        {
            var hull = _LoadModel.hulls[0];
            var src = _LoadModel.nodes;
            var count = _LoadModel.numnodes;
            var dest = new BspClipNode[count];

            hull.clipnodes = dest;
            hull.firstclipnode = 0;
            hull.lastclipnode = count - 1;
            hull.planes = _LoadModel.planes;

            for( var i = 0; i < count; i++ )
            {
                dest[i].planenum = Array.IndexOf( _LoadModel.planes, src[i].plane ); // todo: optimize this
                dest[i].children = new Int16[2];
                for( var j = 0; j < 2; j++ )
                {
                    var child = src[i].children[j];
                    if( child.contents < 0 )
                        dest[i].children[j] = ( Int16 ) child.contents;
                    else
                        dest[i].children[j] = ( Int16 ) Array.IndexOf( _LoadModel.nodes, (MemoryNode)child ); // todo: optimize this
                }
            }
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
        /// CalcSurfaceExtents
        /// Fills in s->texturemins[] and s->extents[]
        /// </summary>
        private void CalcSurfaceExtents( MemorySurface s )
        {
            var mins = new Single[] { 999999, 999999 };
            var maxs = new Single[] { -99999, -99999 };

            var tex = s.texinfo;
            var v = _LoadModel.vertexes;

            for( var i = 0; i < s.numedges; i++ )
            {
                Int32 idx;
                var e = _LoadModel.surfedges[s.firstedge + i];
                if( e >= 0 )
                    idx = _LoadModel.edges[e].v[0];
                else
                    idx = _LoadModel.edges[-e].v[1];

                for( var j = 0; j < 2; j++ )
                {
                    var val = v[idx].position.X * tex.vecs[j].X +
                        v[idx].position.Y * tex.vecs[j].Y +
                        v[idx].position.Z * tex.vecs[j].Z +
                        tex.vecs[j].W;
                    if( val < mins[j] )
                        mins[j] = val;
                    if( val > maxs[j] )
                        maxs[j] = val;
                }
            }

            var bmins = new Int32[2];
            var bmaxs = new Int32[2];
            for( var i = 0; i < 2; i++ )
            {
                bmins[i] = ( Int32 ) Math.Floor( mins[i] / 16 );
                bmaxs[i] = ( Int32 ) Math.Ceiling( maxs[i] / 16 );

                s.texturemins[i] = ( Int16 ) ( bmins[i] * 16 );
                s.extents[i] = ( Int16 ) ( ( bmaxs[i] - bmins[i] ) * 16 );
                if( ( tex.flags & BspDef.TEX_SPECIAL ) == 0 && s.extents[i] > 512 )
                    Utilities.Error( "Bad surface extents" );
            }
        }

        /// <summary>
        /// Mod_SetParent
        /// </summary>
        private void SetParent( MemoryNodeBase node, MemoryNode parent )
        {
            node.parent = parent;
            if( node.contents < 0 )
                return;

            var n = (MemoryNode)node;
            SetParent( n.children[0], n );
            SetParent( n.children[1], n );
        }

        /// <summary>
        /// Mod_FloodFillSkin
        /// Fill background pixels so mipmapping doesn't have haloes - Ed
        /// </summary>
        private void FloodFillSkin( ByteArraySegment skin, Int32 skinwidth, Int32 skinheight )
        {
            var filler = new FloodFiller( skin, skinwidth, skinheight );
            filler.Perform();
        }
    }

    internal class FloodFiller
    {
        private struct floodfill_t
        {
            public Int16 x, y;
        } // floodfill_t;

        // must be a power of 2
        private const Int32 FLOODFILL_FIFO_SIZE = 0x1000;

        private const Int32 FLOODFILL_FIFO_MASK = FLOODFILL_FIFO_SIZE - 1;

        private ByteArraySegment _Skin;
        private floodfill_t[] _Fifo;
        private Int32 _Width;
        private Int32 _Height;

        //int _Offset;
        private Int32 _X;

        private Int32 _Y;
        private Int32 _Fdc;
        private Byte _FillColor;
        private Int32 _Inpt;

        public void Perform()
        {
            var filledcolor = 0;
            // attempt to find opaque black
            var t8to24 = MainWindow.Instance.Host.Video.Table8to24;
            for( var i = 0; i < 256; ++i )
                if( t8to24[i] == ( 255 << 0 ) ) // alpha 1.0
                {
                    filledcolor = i;
                    break;
                }

            // can't fill to filled color or to transparent color (used as visited marker)
            if( ( _FillColor == filledcolor ) || ( _FillColor == 255 ) )
            {
                return;
            }

            var outpt = 0;
            _Inpt = 0;
            _Fifo[_Inpt].x = 0;
            _Fifo[_Inpt].y = 0;
            _Inpt = ( _Inpt + 1 ) & FLOODFILL_FIFO_MASK;

            while( outpt != _Inpt )
            {
                _X = _Fifo[outpt].x;
                _Y = _Fifo[outpt].y;
                _Fdc = filledcolor;
                var offset = _X + _Width * _Y;

                outpt = ( outpt + 1 ) & FLOODFILL_FIFO_MASK;

                if( _X > 0 )
                    Step( offset - 1, -1, 0 );
                if( _X < _Width - 1 )
                    Step( offset + 1, 1, 0 );
                if( _Y > 0 )
                    Step( offset - _Width, 0, -1 );
                if( _Y < _Height - 1 )
                    Step( offset + _Width, 0, 1 );

                _Skin.Data[_Skin.StartIndex + offset] = ( Byte ) _Fdc;
            }
        }

        private void Step( Int32 offset, Int32 dx, Int32 dy )
        {
            var pos = _Skin.Data;
            var off = _Skin.StartIndex + offset;

            if( pos[off] == _FillColor )
            {
                pos[off] = 255;
                _Fifo[_Inpt].x = ( Int16 ) ( _X + dx );
                _Fifo[_Inpt].y = ( Int16 ) ( _Y + dy );
                _Inpt = ( _Inpt + 1 ) & FLOODFILL_FIFO_MASK;
            }
            else if( pos[off] != 255 )
                _Fdc = pos[off];
        }

        public FloodFiller( ByteArraySegment skin, Int32 skinwidth, Int32 skinheight )
        {
            _Skin = skin;
            _Width = skinwidth;
            _Height = skinheight;
            _Fifo = new floodfill_t[FLOODFILL_FIFO_SIZE];
            _FillColor = _Skin.Data[_Skin.StartIndex]; // *skin; // assume this is the pixel to fill
        }
    }
}
