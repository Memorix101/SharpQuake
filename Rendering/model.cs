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
    internal static class Mod
    {
        public static trivertx_t[][] PoseVerts
        {
            get
            {
                return _PoseVerts;
            }
        }

        public static stvert_t[] STVerts
        {
            get
            {
                return _STVerts;
            }
        }

        public static dtriangle_t[] Triangles
        {
            get
            {
                return _Triangles;
            }
        }

        public static aliashdr_t Header
        {
            get
            {
                return _Header;
            }
        }

        public static model_t Model
        {
            get
            {
                return _LoadModel;
            }
        }

        public static Single SubdivideSize
        {
            get
            {
                return _glSubDivideSize.Value;
            }
        }

        // modelgen.h
        public const Int32 ALIAS_VERSION = 6;

        public const Int32 IDPOLYHEADER = (('O' << 24) + ('P' << 16) + ('D' << 8) + 'I'); // little-endian "IDPO"

        // spritegn.h
        public const Int32 SPRITE_VERSION = 1;

        public const Int32 IDSPRITEHEADER = (('P' << 24) + ('S' << 16) + ('D' << 8) + 'I'); // little-endian "IDSP"

        public const Int32 VERTEXSIZE = 7;
        public const Int32 MAX_SKINS = 32;
        public const Int32 MAXALIASVERTS = 1024; //1024
        public const Int32 MAXALIASFRAMES = 256;
        public const Int32 MAXALIASTRIS = 2048;
        public const Int32 MAX_MOD_KNOWN = 512;

        public const Int32 MAX_LBM_HEIGHT = 480;

        private const Int32 ANIM_CYCLE = 2;

        private static Single ALIAS_BASE_SIZE_RATIO = ( 1.0f / 11.0f );

        private static CVar _glSubDivideSize; // = { "gl_subdivide_size", "128", true };
        private static Byte[] _Novis = new Byte[bsp_file.MAX_MAP_LEAFS / 8]; // byte mod_novis[MAX_MAP_LEAFS/8]

        private static model_t[] _Known = new model_t[MAX_MOD_KNOWN]; // mod_known
        private static Int32 _NumKnown; // mod_numknown

        private static model_t _LoadModel; // loadmodel
        private static aliashdr_t _Header; // pheader

        private static stvert_t[] _STVerts = new stvert_t[MAXALIASVERTS]; // stverts
        private static dtriangle_t[] _Triangles = new dtriangle_t[MAXALIASTRIS]; // triangles
        private static Int32 _PoseNum; // posenum;
        private static Byte[] _ModBase; // mod_base  - used by Brush model loading functions
        private static trivertx_t[][] _PoseVerts = new trivertx_t[MAXALIASFRAMES][]; // poseverts
        private static Byte[] _Decompressed = new Byte[bsp_file.MAX_MAP_LEAFS / 8]; // static byte decompressed[] from Mod_DecompressVis()

        /// <summary>
        /// Mod_Init
        /// </summary>
        public static void Init()
        {
            if( _glSubDivideSize == null )
            {
                _glSubDivideSize = new CVar( "gl_subdivide_size", "128", true );
            }

            for( var i = 0; i < _Known.Length; i++ )
                _Known[i] = new model_t();

            Utilities.FillArray( _Novis, ( Byte ) 0xff );
        }

        /// <summary>
        /// Mod_ClearAll
        /// </summary>
        public static void ClearAll()
        {
            for( var i = 0; i < _NumKnown; i++ )
            {
                model_t mod = _Known[i];

                if( mod.type != modtype_t.mod_alias )
                    mod.needload = true;
            }
        }

        /// <summary>
        /// Mod_ForName
        /// Loads in a model for the given name
        /// </summary>
        public static model_t ForName( String name, Boolean crash )
        {
            model_t mod = FindName( name );

            return LoadModel( mod, crash );
        }

        /// <summary>
        /// Mod_Extradata
        /// handles caching
        /// </summary>
        public static aliashdr_t GetExtraData( model_t mod )
        {
            var r = Cache.Check( mod.cache );
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
        public static void TouchModel( String name )
        {
            model_t mod = FindName( name );

            if( !mod.needload )
            {
                if( mod.type == modtype_t.mod_alias )
                    Cache.Check( mod.cache );
            }
        }

        /// <summary>
        /// Mod_PointInLeaf
        /// </summary>
        public static mleaf_t PointInLeaf( ref Vector3 p, model_t model )
        {
            if( model == null || model.nodes == null )
                Utilities.Error( "Mod_PointInLeaf: bad model" );

            mleaf_t result = null;
            mnodebase_t node = model.nodes[0];
            while( true )
            {
                if( node.contents < 0 )
                {
                    result = (mleaf_t)node;
                    break;
                }

                mnode_t n = (mnode_t)node;
                Plane plane = n.plane;
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
        public static Byte[] LeafPVS( mleaf_t leaf, model_t model )
        {
            if( leaf == model.leafs[0] )
                return _Novis;

            return DecompressVis( leaf.compressed_vis, leaf.visofs, model );
        }

        // Mod_Print
        public static void Print()
        {
            Con.Print( "Cached models:\n" );
            for( var i = 0; i < _NumKnown; i++ )
            {
                model_t mod = _Known[i];
                Con.Print( "{0}\n", mod.name );
            }
        }

        /// <summary>
        /// Mod_FindName
        /// </summary>
        public static model_t FindName( String name )
        {
            if( String.IsNullOrEmpty( name ) )
                Utilities.Error( "Mod_ForName: NULL name" );

            //
            // search the currently loaded models
            //
            var i = 0;
            model_t mod;
            for( i = 0, mod = _Known[0]; i < _NumKnown; i++, mod = _Known[i] )
            {
                //mod = _Known[i];
                if( mod.name == name )
                    break;
            }

            if( i == _NumKnown )
            {
                if( _NumKnown == MAX_MOD_KNOWN )
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
        public static model_t LoadModel( model_t mod, Boolean crash )
        {
            if( !mod.needload )
            {
                if( mod.type == modtype_t.mod_alias )
                {
                    if( Cache.Check( mod.cache ) != null )
                        return mod;
                }
                else
                    return mod;		// not cached at all
            }

            //
            // load the file
            //
            Byte[] buf = FileSystem.LoadFile( mod.name );
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
                case IDPOLYHEADER:
                    LoadAliasModel( mod, buf );
                    break;

                case IDSPRITEHEADER:
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
        public static void LoadAliasModel( model_t mod, Byte[] buffer )
        {
            mdl_t pinmodel = Utilities.BytesToStructure<mdl_t>( buffer, 0 );

            var version = EndianHelper.LittleLong( pinmodel.version );
            if( version != ALIAS_VERSION )
                Utilities.Error( "{0} has wrong version number ({1} should be {2})",
                    mod.name, version, ALIAS_VERSION );

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

            if( _Header.skinheight > MAX_LBM_HEIGHT )
                Utilities.Error( "model {0} has a skin taller than {1}", mod.name, MAX_LBM_HEIGHT );

            _Header.numverts = EndianHelper.LittleLong( pinmodel.numverts );

            if( _Header.numverts <= 0 )
                Utilities.Error( "model {0} has no vertices", mod.name );

            if( _Header.numverts > MAXALIASVERTS )
                Utilities.Error( "model {0} has too many vertices", mod.name );

            _Header.numtris = EndianHelper.LittleLong( pinmodel.numtris );

            if( _Header.numtris <= 0 )
                Utilities.Error( "model {0} has no triangles", mod.name );

            _Header.numframes = EndianHelper.LittleLong( pinmodel.numframes );
            var numframes = _Header.numframes;
            if( numframes < 1 )
                Utilities.Error( "Mod_LoadAliasModel: Invalid # of frames: {0}\n", numframes );

            _Header.size = EndianHelper.LittleFloat( pinmodel.size ) * ALIAS_BASE_SIZE_RATIO;
            mod.synctype = (synctype_t) EndianHelper.LittleLong( ( Int32 ) pinmodel.synctype );
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
                aliasframetype_t frametype = (aliasframetype_t)BitConverter.ToInt32( buffer, framesOffset );
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

            mod.type = modtype_t.mod_alias;

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
            mod.cache = Cache.Alloc( aliashdr_t.SizeInBytes * _Header.frames.Length * maliasframedesc_t.SizeInBytes, null );
            if( mod.cache == null )
                return;
            mod.cache.data = _Header;
        }

        /// <summary>
        /// Mod_LoadSpriteModel
        /// </summary>
        public static void LoadSpriteModel( model_t mod, Byte[] buffer )
        {
            dsprite_t pin = Utilities.BytesToStructure<dsprite_t>( buffer, 0 );

            var version = EndianHelper.LittleLong( pin.version );
            if( version != SPRITE_VERSION )
                Utilities.Error( "{0} has wrong version number ({1} should be {2})",
                    mod.name, version, SPRITE_VERSION );

            var numframes = EndianHelper.LittleLong( pin.numframes );

            msprite_t psprite = new msprite_t();

            // Uze: sprite models are not cached so
            mod.cache = new cache_user_t();
            mod.cache.data = psprite;

            psprite.type = EndianHelper.LittleLong( pin.type );
            psprite.maxwidth = EndianHelper.LittleLong( pin.width );
            psprite.maxheight = EndianHelper.LittleLong( pin.height );
            psprite.beamlength = EndianHelper.LittleFloat( pin.beamlength );
            mod.synctype = (synctype_t)EndianHelper.LittleLong( ( Int32 ) pin.synctype );
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
                spriteframetype_t frametype = (spriteframetype_t)BitConverter.ToInt32( buffer, frameOffset );
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

            mod.type = modtype_t.mod_sprite;
        }

        /// <summary>
        /// Mod_LoadBrushModel
        /// </summary>
        public static void LoadBrushModel( model_t mod, Byte[] buffer )
        {
            mod.type = modtype_t.mod_brush;

            dheader_t header = Utilities.BytesToStructure<dheader_t>( buffer, 0 );

            var i = EndianHelper.LittleLong( header.version );
            if( i != bsp_file.BSPVERSION )
                Utilities.Error( "Mod_LoadBrushModel: {0} has wrong version number ({1} should be {2})", mod.name, i, bsp_file.BSPVERSION );

            header.version = i;

            // swap all the lumps
            _ModBase = buffer;

            for( i = 0; i < header.lumps.Length; i++ )
            {
                header.lumps[i].filelen = EndianHelper.LittleLong( header.lumps[i].filelen );
                header.lumps[i].fileofs = EndianHelper.LittleLong( header.lumps[i].fileofs );
            }

            // load into heap

            LoadVertexes( ref header.lumps[Lumps.LUMP_VERTEXES] );
            LoadEdges( ref header.lumps[Lumps.LUMP_EDGES] );
            LoadSurfEdges( ref header.lumps[Lumps.LUMP_SURFEDGES] );
            LoadTextures( ref header.lumps[Lumps.LUMP_TEXTURES] );
            LoadLighting( ref header.lumps[Lumps.LUMP_LIGHTING] );
            LoadPlanes( ref header.lumps[Lumps.LUMP_PLANES] );
            LoadTexInfo( ref header.lumps[Lumps.LUMP_TEXINFO] );
            LoadFaces( ref header.lumps[Lumps.LUMP_FACES] );
            LoadMarkSurfaces( ref header.lumps[Lumps.LUMP_MARKSURFACES] );
            LoadVisibility( ref header.lumps[Lumps.LUMP_VISIBILITY] );
            LoadLeafs( ref header.lumps[Lumps.LUMP_LEAFS] );
            LoadNodes( ref header.lumps[Lumps.LUMP_NODES] );
            LoadClipNodes( ref header.lumps[Lumps.LUMP_CLIPNODES] );
            LoadEntities( ref header.lumps[Lumps.LUMP_ENTITIES] );
            LoadSubModels( ref header.lumps[Lumps.LUMP_MODELS] );

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
        private static Byte[] DecompressVis( Byte[] p, Int32 startIndex, model_t model )
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

        private static void SetupSubModel( model_t mod, ref dmodel_t submodel )
        {
            mod.hulls[0].firstclipnode = submodel.headnode[0];
            for( var j = 1; j < bsp_file.MAX_MAP_HULLS; j++ )
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
        private static Int32 LoadAllSkins( Int32 numskins, ByteArraySegment data )
        {
            if( numskins < 1 || numskins > MAX_SKINS )
                Utilities.Error( "Mod_LoadAliasModel: Invalid # of skins: {0}\n", numskins );

            var offset = data.StartIndex;
            var skinOffset = data.StartIndex + daliasskintype_t.SizeInBytes; //  skin = (byte*)(pskintype + 1);
            var s = _Header.skinwidth * _Header.skinheight;

            daliasskintype_t pskintype = Utilities.BytesToStructure<daliasskintype_t>( data.Data, offset );

            for( var i = 0; i < numskins; i++ )
            {
                if( pskintype.type == aliasskintype_t.ALIAS_SKIN_SINGLE )
                {
                    FloodFillSkin( new ByteArraySegment( data.Data, skinOffset ), _Header.skinwidth, _Header.skinheight );

                    // save 8 bit texels for the player model to remap
                    Byte[] texels = new Byte[s]; // Hunk_AllocName(s, loadname);
                    _Header.texels[i] = texels;// -(byte*)pheader;
                    Buffer.BlockCopy( data.Data, offset + daliasskintype_t.SizeInBytes, texels, 0, s );

                    // set offset to pixel data after daliasskintype_t block...
                    offset += daliasskintype_t.SizeInBytes;

                    var name = _LoadModel.name + "_" + i.ToString();
                    _Header.gl_texturenum[i, 0] =
                    _Header.gl_texturenum[i, 1] =
                    _Header.gl_texturenum[i, 2] =
                    _Header.gl_texturenum[i, 3] =
                        Drawer.LoadTexture( name, _Header.skinwidth,
                        _Header.skinheight, new ByteArraySegment( data.Data, offset ), true, false ); // (byte*)(pskintype + 1)

                    // set offset to next daliasskintype_t block...
                    offset += s;
                    pskintype = Utilities.BytesToStructure<daliasskintype_t>( data.Data, offset );
                }
                else
                {
                    // animating skin group.  yuck.
                    offset += daliasskintype_t.SizeInBytes;
                    daliasskingroup_t pinskingroup = Utilities.BytesToStructure<daliasskingroup_t>( data.Data, offset );
                    var groupskins = EndianHelper.LittleLong( pinskingroup.numskins );
                    offset += daliasskingroup_t.SizeInBytes;
                    daliasskininterval_t pinskinintervals = Utilities.BytesToStructure<daliasskininterval_t>( data.Data, offset );

                    offset += daliasskininterval_t.SizeInBytes * groupskins;

                    pskintype = Utilities.BytesToStructure<daliasskintype_t>( data.Data, offset );
                    Int32 j;
                    for( j = 0; j < groupskins; j++ )
                    {
                        FloodFillSkin( new ByteArraySegment( data.Data, skinOffset ), _Header.skinwidth, _Header.skinheight );
                        if( j == 0 )
                        {
                            Byte[] texels = new Byte[s]; // Hunk_AllocName(s, loadname);
                            _Header.texels[i] = texels;// -(byte*)pheader;
                            Buffer.BlockCopy( data.Data, offset, texels, 0, s );
                        }

                        var name = String.Format( "{0}_{1}_{2}", _LoadModel.name, i, j );
                        _Header.gl_texturenum[i, j & 3] =
                            Drawer.LoadTexture( name, _Header.skinwidth,
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
        private static Int32 LoadAliasFrame( ByteArraySegment pin, ref maliasframedesc_t frame )
        {
            daliasframe_t pdaliasframe = Utilities.BytesToStructure<daliasframe_t>( pin.Data, pin.StartIndex );

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

            trivertx_t[] verts = new trivertx_t[_Header.numverts];
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
        private static Int32 LoadAliasGroup( ByteArraySegment pin, ref maliasframedesc_t frame )
        {
            var offset = pin.StartIndex;
            daliasgroup_t pingroup = Utilities.BytesToStructure<daliasgroup_t>( pin.Data, offset );
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
            daliasinterval_t pin_intervals = Utilities.BytesToStructure<daliasinterval_t>( pin.Data, offset ); // (daliasinterval_t*)(pingroup + 1);

            frame.interval = EndianHelper.LittleFloat( pin_intervals.interval );

            offset += numframes * daliasinterval_t.SizeInBytes;

            for( var i = 0; i < numframes; i++ )
            {
                trivertx_t[] tris = new trivertx_t[_Header.numverts];
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
        private static Int32 LoadSpriteFrame( ByteArraySegment pin, out Object ppframe, Int32 framenum )
        {
            dspriteframe_t pinframe = Utilities.BytesToStructure<dspriteframe_t>( pin.Data, pin.StartIndex );

            var width = EndianHelper.LittleLong( pinframe.width );
            var height = EndianHelper.LittleLong( pinframe.height );
            var size = width * height;

            mspriteframe_t pspriteframe = new mspriteframe_t();

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
            pspriteframe.gl_texturenum = Drawer.LoadTexture( name, width, height,
                new ByteArraySegment( pin.Data, pin.StartIndex + dspriteframe_t.SizeInBytes ), true, true ); //   (byte *)(pinframe + 1)

            return pin.StartIndex + dspriteframe_t.SizeInBytes + size;
        }

        /// <summary>
        /// Mod_LoadSpriteGroup
        /// </summary>
        private static Int32 LoadSpriteGroup( ByteArraySegment pin, out Object ppframe, Int32 framenum )
        {
            dspritegroup_t pingroup = Utilities.BytesToStructure<dspritegroup_t>( pin.Data, pin.StartIndex );

            var numframes = EndianHelper.LittleLong( pingroup.numframes );
            mspritegroup_t pspritegroup = new mspritegroup_t();
            pspritegroup.numframes = numframes;
            pspritegroup.frames = new mspriteframe_t[numframes];
            ppframe = pspritegroup;// (mspriteframe_t*)pspritegroup;
            Single[] poutintervals = new Single[numframes];
            pspritegroup.intervals = poutintervals;

            var offset = pin.StartIndex + dspritegroup_t.SizeInBytes;
            for( var i = 0; i < numframes; i++, offset += dspriteinterval_t.SizeInBytes )
            {
                dspriteinterval_t interval = Utilities.BytesToStructure<dspriteinterval_t>( pin.Data, offset );
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
        private static void LoadVertexes( ref lump_t l )
        {
            if( ( l.filelen % dvertex_t.SizeInBytes ) != 0 )
                Utilities.Error( "MOD_LoadBmodel: funny lump size in {0}", _LoadModel.name );

            var count = l.filelen / dvertex_t.SizeInBytes;
            mvertex_t[] verts = new mvertex_t[count];

            _LoadModel.vertexes = verts;
            _LoadModel.numvertexes = count;

            for( Int32 i = 0, offset = l.fileofs; i < count; i++, offset += dvertex_t.SizeInBytes )
            {
                dvertex_t src = Utilities.BytesToStructure<dvertex_t>( _ModBase, offset );
                verts[i].position = EndianHelper.LittleVector3( src.point );
            }
        }

        /// <summary>
        /// Mod_LoadEdges
        /// </summary>
        private static void LoadEdges( ref lump_t l )
        {
            if( ( l.filelen % dedge_t.SizeInBytes ) != 0 )
                Utilities.Error( "MOD_LoadBmodel: funny lump size in {0}", _LoadModel.name );

            var count = l.filelen / dedge_t.SizeInBytes;

            // Uze: Why count + 1 ?????
            medge_t[] edges = new medge_t[count]; // out = Hunk_AllocName ( (count + 1) * sizeof(*out), loadname);
            _LoadModel.edges = edges;
            _LoadModel.numedges = count;

            for( Int32 i = 0, offset = l.fileofs; i < count; i++, offset += dedge_t.SizeInBytes )
            {
                dedge_t src = Utilities.BytesToStructure<dedge_t>( _ModBase, offset );
                edges[i].v = new UInt16[] {
                    (UInt16)EndianHelper.LittleShort((Int16)src.v[0]),
                    (UInt16)EndianHelper.LittleShort((Int16)src.v[1])
                };
            }
        }

        /// <summary>
        /// Mod_LoadSurfedges
        /// </summary>
        private static void LoadSurfEdges( ref lump_t l )
        {
            if( ( l.filelen % sizeof( Int32 ) ) != 0 )
                Utilities.Error( "MOD_LoadBmodel: funny lump size in {0}", _LoadModel.name );

            var count = l.filelen / sizeof( Int32 );
            Int32[] edges = new Int32[count];

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
        private static void LoadTextures( ref lump_t l )
        {
            if( l.filelen == 0 )
            {
                _LoadModel.textures = null;
                return;
            }

            dmiptexlump_t m = Utilities.BytesToStructure<dmiptexlump_t>( _ModBase, l.fileofs );// (dmiptexlump_t *)(mod_base + l.fileofs);

            m.nummiptex = EndianHelper.LittleLong( m.nummiptex );

            Int32[] dataofs = new Int32[m.nummiptex];

            Buffer.BlockCopy( _ModBase, l.fileofs + dmiptexlump_t.SizeInBytes, dataofs, 0, dataofs.Length * sizeof( Int32 ) );

            _LoadModel.numtextures = m.nummiptex;
            _LoadModel.textures = new texture_t[m.nummiptex]; // Hunk_AllocName (m->nummiptex * sizeof(*loadmodel->textures) , loadname);

            for( var i = 0; i < m.nummiptex; i++ )
            {
                dataofs[i] = EndianHelper.LittleLong( dataofs[i] );
                if( dataofs[i] == -1 )
                    continue;

                var mtOffset = l.fileofs + dataofs[i];
                miptex_t mt = Utilities.BytesToStructure<miptex_t>( _ModBase, mtOffset ); //mt = (miptex_t *)((byte *)m + m.dataofs[i]);
                mt.width = ( UInt32 ) EndianHelper.LittleLong( ( Int32 ) mt.width );
                mt.height = ( UInt32 ) EndianHelper.LittleLong( ( Int32 ) mt.height );
                for( var j = 0; j < bsp_file.MIPLEVELS; j++ )
                    mt.offsets[j] = ( UInt32 ) EndianHelper.LittleLong( ( Int32 ) mt.offsets[j] );

                if( ( mt.width & 15 ) != 0 || ( mt.height & 15 ) != 0 )
                    Utilities.Error( "Texture {0} is not 16 aligned", mt.name );

                var pixels = ( Int32 ) ( mt.width * mt.height / 64 * 85 );
                texture_t tx = new texture_t();// Hunk_AllocName(sizeof(texture_t) + pixels, loadname);
                _LoadModel.textures[i] = tx;

                tx.name = Utilities.GetString( mt.name );//   memcpy (tx->name, mt->name, sizeof(tx.name));

                var tgaName = $"textures/{tx.name}.tga";

                var file = FileSystem.LoadFile( tgaName );

                if ( file != null )
                {
                    Byte[] tgapixels = null;

                    using ( var reader = new System.IO.BinaryReader( new MemoryStream( file ) ) )
                    {
                        var tga = new TgaLib.TgaImage( reader );
                        System.Windows.Media.Imaging.BitmapSource source = tga.GetBitmap( );

                        var bmp = new System.Drawing.Bitmap( source.PixelWidth, source.PixelHeight, System.Drawing.Imaging.PixelFormat.Format32bppPArgb );
                        System.Drawing.Imaging.BitmapData data = bmp.LockBits(
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
                    tx.scaleX = 1f;
                    for ( var j = 0; j < bsp_file.MIPLEVELS; j++ )
                        tx.offsets[j] = ( Int32 ) mt.offsets[j] - miptex_t.SizeInBytes;
                    // the pixels immediately follow the structures
                    tx.pixels = new Byte[pixels];
    #warning BlockCopy tries to copy data over the bounds of _ModBase if certain mods are loaded. Needs proof fix!
                    if (mtOffset + miptex_t.SizeInBytes + pixels <= _ModBase.Length)
                        Buffer.BlockCopy(_ModBase, mtOffset + miptex_t.SizeInBytes, tx.pixels, 0, pixels);
                    else
                    {
                        Buffer.BlockCopy(_ModBase, mtOffset, tx.pixels, 0, pixels);
                        Con.Print("Texture info of {0} truncated to fit in bounds of _ModBase\n", _LoadModel.name);
                    }
                }

                if ( tx.name != null && tx.name.StartsWith( "sky" ) )// !Q_strncmp(mt->name,"sky",3))
                    render.InitSky( tx );
                else
                {
                    if ( tx.rawBitmap == null )
                    {
                        tx.gl_texturenum = Drawer.LoadTexture( tx.name, ( Int32 ) tx.width, ( Int32 ) tx.height,
                            new ByteArraySegment( tx.pixels ), true, false, _LoadModel.name );
                    }
                    else
                    {
                        tx.gl_texturenum = Drawer.LoadTexture( tx.name, ( Int32 ) tx.width, ( Int32 ) tx.height,
                            tx.rawBitmap, true, false, _LoadModel.name );
                    }
                }
            }

            //
            // sequence the animations
            //
            texture_t[] anims = new texture_t[10];
            texture_t[] altanims = new texture_t[10];

            for( var i = 0; i < m.nummiptex; i++ )
            {
                texture_t tx = _LoadModel.textures[i];
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
                    texture_t tx2 = _LoadModel.textures[j];
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
                    texture_t tx2 = anims[j];
                    if( tx2 == null )
                        Utilities.Error( "Missing frame {0} of {1}", j, tx.name );
                    tx2.anim_total = max * ANIM_CYCLE;
                    tx2.anim_min = j * ANIM_CYCLE;
                    tx2.anim_max = ( j + 1 ) * ANIM_CYCLE;
                    tx2.anim_next = anims[( j + 1 ) % max];
                    if( altmax != 0 )
                        tx2.alternate_anims = altanims[0];
                }
                for( var j = 0; j < altmax; j++ )
                {
                    texture_t tx2 = altanims[j];
                    if( tx2 == null )
                        Utilities.Error( "Missing frame {0} of {1}", j, tx2.name );
                    tx2.anim_total = altmax * ANIM_CYCLE;
                    tx2.anim_min = j * ANIM_CYCLE;
                    tx2.anim_max = ( j + 1 ) * ANIM_CYCLE;
                    tx2.anim_next = altanims[( j + 1 ) % altmax];
                    if( max != 0 )
                        tx2.alternate_anims = anims[0];
                }
            }
        }

        /// <summary>
        /// Mod_LoadLighting
        /// </summary>
        private static void LoadLighting( ref lump_t l )
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
        private static void LoadPlanes( ref lump_t l )
        {
            if( ( l.filelen % dplane_t.SizeInBytes ) != 0 )
                Utilities.Error( "MOD_LoadBmodel: funny lump size in {0}", _LoadModel.name );

            var count = l.filelen / dplane_t.SizeInBytes;
            // Uze: Possible error! Why in original is out = Hunk_AllocName ( count*2*sizeof(*out), loadname)???
            Plane[] planes = new Plane[count];

            for( var i = 0; i < planes.Length; i++ )
                planes[i] = new Plane();

            _LoadModel.planes = planes;
            _LoadModel.numplanes = count;

            for( var i = 0; i < count; i++ )
            {
                dplane_t src = Utilities.BytesToStructure<dplane_t>( _ModBase, l.fileofs + i * dplane_t.SizeInBytes );
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
        private static void LoadTexInfo( ref lump_t l )
        {
            //in = (void *)(mod_base + l->fileofs);
            if( ( l.filelen % texinfo_t.SizeInBytes ) != 0 )
                Utilities.Error( "MOD_LoadBmodel: funny lump size in {0}", _LoadModel.name );

            var count = l.filelen / texinfo_t.SizeInBytes;
            mtexinfo_t[] infos = new mtexinfo_t[count]; // out = Hunk_AllocName ( count*sizeof(*out), loadname);

            for( var i = 0; i < infos.Length; i++ )
                infos[i] = new mtexinfo_t();

            _LoadModel.texinfo = infos;
            _LoadModel.numtexinfo = count;

            for( var i = 0; i < count; i++ )//, in++, out++)
            {
                texinfo_t src = Utilities.BytesToStructure<texinfo_t>( _ModBase, l.fileofs + i * texinfo_t.SizeInBytes );

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
                    infos[i].texture = render.NoTextureMip;	// checkerboard texture
                    infos[i].flags = 0;
                }
                else
                {
                    if( miptex >= _LoadModel.numtextures )
                        Utilities.Error( "miptex >= loadmodel->numtextures" );
                    infos[i].texture = _LoadModel.textures[miptex];
                    if( infos[i].texture == null )
                    {
                        infos[i].texture = render.NoTextureMip; // texture not found
                        infos[i].flags = 0;
                    }
                }
            }
        }

        /// <summary>
        /// Mod_LoadFaces
        /// </summary>
        private static void LoadFaces( ref lump_t l )
        {
            if( ( l.filelen % dface_t.SizeInBytes ) != 0 )
                Utilities.Error( "MOD_LoadBmodel: funny lump size in {0}", _LoadModel.name );

            var count = l.filelen / dface_t.SizeInBytes;
            msurface_t[] dest = new msurface_t[count];

            for( var i = 0; i < dest.Length; i++ )
                dest[i] = new msurface_t();

            _LoadModel.surfaces = dest;
            _LoadModel.numsurfaces = count;
            var offset = l.fileofs;
            for( var surfnum = 0; surfnum < count; surfnum++, offset += dface_t.SizeInBytes )
            {
                dface_t src = Utilities.BytesToStructure<dface_t>( _ModBase, offset );

                dest[surfnum].firstedge = EndianHelper.LittleLong( src.firstedge );
                dest[surfnum].numedges = EndianHelper.LittleShort( src.numedges );
                dest[surfnum].flags = 0;

                Int32 planenum = EndianHelper.LittleShort( src.planenum );
                Int32 side = EndianHelper.LittleShort( src.side );
                if( side != 0 )
                    dest[surfnum].flags |= Surf.SURF_PLANEBACK;

                dest[surfnum].plane = _LoadModel.planes[planenum];
                dest[surfnum].texinfo = _LoadModel.texinfo[EndianHelper.LittleShort( src.texinfo )];

                CalcSurfaceExtents( dest[surfnum] );

                // lighting info

                for( var i = 0; i < bsp_file.MAXLIGHTMAPS; i++ )
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
                        dest[surfnum].flags |= ( Surf.SURF_DRAWSKY | Surf.SURF_DRAWTILED );
                        render.SubdivideSurface( dest[surfnum] );	// cut up polygon for warps
                        continue;
                    }

                    if( dest[surfnum].texinfo.texture.name.StartsWith( "*" ) )		// turbulent
                    {
                        dest[surfnum].flags |= ( Surf.SURF_DRAWTURB | Surf.SURF_DRAWTILED );
                        for( var i = 0; i < 2; i++ )
                        {
                            dest[surfnum].extents[i] = 16384;
                            dest[surfnum].texturemins[i] = -8192;
                        }
                        render.SubdivideSurface( dest[surfnum] );	// cut up polygon for warps
                        continue;
                    }
                }
            }
        }

        /// <summary>
        /// Mod_LoadMarksurfaces
        /// </summary>
        private static void LoadMarkSurfaces( ref lump_t l )
        {
            if( ( l.filelen % sizeof( Int16 ) ) != 0 )
                Utilities.Error( "MOD_LoadBmodel: funny lump size in {0}", _LoadModel.name );

            var count = l.filelen / sizeof( Int16 );
            msurface_t[] dest = new msurface_t[count];

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
        private static void LoadVisibility( ref lump_t l )
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
        private static void LoadLeafs( ref lump_t l )
        {
            if( ( l.filelen % dleaf_t.SizeInBytes ) != 0 )
                Utilities.Error( "MOD_LoadBmodel: funny lump size in {0}", _LoadModel.name );

            var count = l.filelen / dleaf_t.SizeInBytes;
            mleaf_t[] dest = new mleaf_t[count];

            for( var i = 0; i < dest.Length; i++ )
                dest[i] = new mleaf_t();

            _LoadModel.leafs = dest;
            _LoadModel.numleafs = count;

            for( Int32 i = 0, offset = l.fileofs; i < count; i++, offset += dleaf_t.SizeInBytes )
            {
                dleaf_t src = Utilities.BytesToStructure<dleaf_t>( _ModBase, offset );

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
        private static void LoadNodes( ref lump_t l )
        {
            if( ( l.filelen % dnode_t.SizeInBytes ) != 0 )
                Utilities.Error( "MOD_LoadBmodel: funny lump size in {0}", _LoadModel.name );

            var count = l.filelen / dnode_t.SizeInBytes;
            mnode_t[] dest = new mnode_t[count];

            for( var i = 0; i < dest.Length; i++ )
                dest[i] = new mnode_t();

            _LoadModel.nodes = dest;
            _LoadModel.numnodes = count;

            for( Int32 i = 0, offset = l.fileofs; i < count; i++, offset += dnode_t.SizeInBytes )
            {
                dnode_t src = Utilities.BytesToStructure<dnode_t>( _ModBase, offset );

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
        private static void LoadClipNodes( ref lump_t l )
        {
            if( ( l.filelen % dclipnode_t.SizeInBytes ) != 0 )
                Utilities.Error( "MOD_LoadBmodel: funny lump size in {0}", _LoadModel.name );

            var count = l.filelen / dclipnode_t.SizeInBytes;
            dclipnode_t[] dest = new dclipnode_t[count];

            _LoadModel.clipnodes = dest;
            _LoadModel.numclipnodes = count;

            hull_t hull = _LoadModel.hulls[1];
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

            for( Int32 i = 0, offset = l.fileofs; i < count; i++, offset += dclipnode_t.SizeInBytes )
            {
                dclipnode_t src = Utilities.BytesToStructure<dclipnode_t>( _ModBase, offset );

                dest[i].planenum = EndianHelper.LittleLong( src.planenum ); // Uze: changed from LittleShort
                dest[i].children = new Int16[2];
                dest[i].children[0] = EndianHelper.LittleShort( src.children[0] );
                dest[i].children[1] = EndianHelper.LittleShort( src.children[1] );
            }
        }

        /// <summary>
        /// Mod_LoadEntities
        /// </summary>
        private static void LoadEntities( ref lump_t l )
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
        private static void LoadSubModels( ref lump_t l )
        {
            if( ( l.filelen % dmodel_t.SizeInBytes ) != 0 )
                Utilities.Error( "MOD_LoadBmodel: funny lump size in {0}", _LoadModel.name );

            var count = l.filelen / dmodel_t.SizeInBytes;
            dmodel_t[] dest = new dmodel_t[count];

            _LoadModel.submodels = dest;
            _LoadModel.numsubmodels = count;

            for( Int32 i = 0, offset = l.fileofs; i < count; i++, offset += dmodel_t.SizeInBytes )
            {
                dmodel_t src = Utilities.BytesToStructure<dmodel_t>( _ModBase, offset );

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

                dest[i].headnode = new Int32[bsp_file.MAX_MAP_HULLS];
                for( var j = 0; j < bsp_file.MAX_MAP_HULLS; j++ )
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
        private static void MakeHull0()
        {
            hull_t hull = _LoadModel.hulls[0];
            mnode_t[] src = _LoadModel.nodes;
            var count = _LoadModel.numnodes;
            dclipnode_t[] dest = new dclipnode_t[count];

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
                    mnodebase_t child = src[i].children[j];
                    if( child.contents < 0 )
                        dest[i].children[j] = ( Int16 ) child.contents;
                    else
                        dest[i].children[j] = ( Int16 ) Array.IndexOf( _LoadModel.nodes, (mnode_t)child ); // todo: optimize this
                }
            }
        }

        private static Single RadiusFromBounds( ref Vector3 mins, ref Vector3 maxs )
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
        private static void CalcSurfaceExtents( msurface_t s )
        {
            Single[] mins = new Single[] { 999999, 999999 };
            Single[] maxs = new Single[] { -99999, -99999 };

            mtexinfo_t tex = s.texinfo;
            mvertex_t[] v = _LoadModel.vertexes;

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

            Int32[] bmins = new Int32[2];
            Int32[] bmaxs = new Int32[2];
            for( var i = 0; i < 2; i++ )
            {
                bmins[i] = ( Int32 ) Math.Floor( mins[i] / 16 );
                bmaxs[i] = ( Int32 ) Math.Ceiling( maxs[i] / 16 );

                s.texturemins[i] = ( Int16 ) ( bmins[i] * 16 );
                s.extents[i] = ( Int16 ) ( ( bmaxs[i] - bmins[i] ) * 16 );
                if( ( tex.flags & bsp_file.TEX_SPECIAL ) == 0 && s.extents[i] > 512 )
                    Utilities.Error( "Bad surface extents" );
            }
        }

        /// <summary>
        /// Mod_SetParent
        /// </summary>
        private static void SetParent( mnodebase_t node, mnode_t parent )
        {
            node.parent = parent;
            if( node.contents < 0 )
                return;

            mnode_t n = (mnode_t)node;
            SetParent( n.children[0], n );
            SetParent( n.children[1], n );
        }

        /// <summary>
        /// Mod_FloodFillSkin
        /// Fill background pixels so mipmapping doesn't have haloes - Ed
        /// </summary>
        private static void FloodFillSkin( ByteArraySegment skin, Int32 skinwidth, Int32 skinheight )
        {
            FloodFiller filler = new FloodFiller( skin, skinwidth, skinheight );
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
            UInt32[] t8to24 = vid.Table8to24;
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
            Byte[] pos = _Skin.Data;
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
