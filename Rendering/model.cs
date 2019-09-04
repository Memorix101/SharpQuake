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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SharpQuake.Framework;
using SharpQuake.Framework.IO;
using SharpQuake.Framework.Mathematics;
using SharpQuake.Framework.Rendering;
using SharpQuake.Game.Rendering.Memory;
using SharpQuake.Game.Rendering.Models;
using SharpQuake.Game.Rendering.Textures;
using SharpQuake.Renderer.Textures;

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

        public List<BaseTexture> SkinTextures
        {
            get;
            private set;
        }

        public List<BaseTexture> SpriteTextures
        {
            get;
            private set;
        }

        private CVar _glSubDivideSize; // = { "gl_subdivide_size", "128", true };
        
        private List<Model> _Known = new List<Model>( ModelDef.MAX_MOD_KNOWN ); // mod_known

        private Model _LoadModel; // loadmodel
        private aliashdr_t _Header; // pheader

        private stvert_t[] _STVerts = new stvert_t[ModelDef.MAXALIASVERTS]; // stverts
        private dtriangle_t[] _Triangles = new dtriangle_t[ModelDef.MAXALIASTRIS]; // triangles
        private Int32 _PoseNum; // posenum;
        private trivertx_t[][] _PoseVerts = new trivertx_t[ModelDef.MAXALIASFRAMES][]; // poseverts
        
        public Mod( Host host )
        {
            Host = host;
        }

        /// <summary>
        /// Mod_Init
        /// </summary>
        public void Initialise( )
        {
            SkinTextures = new List<BaseTexture>( );
            SpriteTextures = new List<BaseTexture>( );

            if ( _glSubDivideSize == null )
                _glSubDivideSize = new CVar( "gl_subdivide_size", "128", true );
        }

        /// <summary>
        /// Mod_ClearAll
        /// </summary>
        public void ClearAll( )
        {
            for ( var i = 0; i < _Known.Count; i++ )
            {
                var mod = _Known[i];

                if ( mod.Type != ModelType.mod_alias )
                    mod.IsLoadRequired = true;
            }
        }

        /// <summary>
        /// Mod_ForName
        /// Loads in a model for the given name
        /// </summary>
        public Model ForName( String name, Boolean crash, Boolean isBrush = false )
        {
            var mod = FindName( name, isBrush );

            return LoadModel( mod, crash );
        }

        /// <summary>
        /// Mod_Extradata
        /// handles caching
        /// </summary>
        public aliashdr_t GetExtraData( Model mod )
        {
            var r = Host.Cache.Check( mod.cache );

            if ( r != null )
                return ( aliashdr_t ) r;

            LoadModel( mod, true );

            if ( mod.cache.data == null )
                Utilities.Error( "Mod_Extradata: caching failed" );

            return ( aliashdr_t ) mod.cache.data;
        }

        /// <summary>
        /// Mod_TouchModel
        /// </summary>
        public void TouchModel( String name )
        {
            var mod = FindName( name, true );

            if ( !mod.IsLoadRequired )
            {
                if ( mod.Type == ModelType.mod_alias )
                    Host.Cache.Check( mod.cache );
            }
        } 

        // Mod_Print
        public void Print( )
        {
            ConsoleWrapper.Print( "Cached models:\n" );

            for ( var i = 0; i < _Known.Count; i++ )
            {
                var mod = _Known[i];
                ConsoleWrapper.Print( "{0}\n", mod.Name );
            }
        }

        /// <summary>
        /// Mod_FindName
        /// </summary>
        public Model FindName( String name, Boolean isBrush = false )
        {
            if ( String.IsNullOrEmpty( name ) )
                Utilities.Error( "Mod_ForName: NULL name" );

            var mod = _Known.Where( m => m.Name == name ).FirstOrDefault( );

            if ( mod == null )
            {
                if ( _Known.Count == ModelDef.MAX_MOD_KNOWN )
                    Utilities.Error( "mod_numknown == MAX_MOD_KNOWN" );

                mod = name.ToLower().Contains( ".bsp" ) || isBrush ? new BrushModel( Host.Model.SubdivideSize, Host.RenderContext.NoTextureMip ) : new Model( Host.RenderContext.NoTextureMip );
                mod.Name = name;
                mod.IsLoadRequired = true;
                _Known.Add( mod );
            }

            return mod;
        }

        /// <summary>
        /// Mod_LoadModel
        /// Loads a model into the cache
        /// </summary>
        public Model LoadModel( Model mod, Boolean crash )
        {
            if ( !mod.IsLoadRequired )
            {
                if ( mod.Type == ModelType.mod_alias )
                {
                    if ( Host.Cache.Check( mod.cache ) != null )
                        return mod;
                }
                else
                    return mod;		// not cached at all
            }

            //
            // load the file
            //
            var buf = FileSystem.LoadFile( mod.Name );
            if ( buf == null )
            {
                if ( crash )
                    Utilities.Error( "Mod_NumForName: {0} not found", mod.Name );
                return null;
            }

            //
            // allocate a new model
            //
            _LoadModel = mod;

            mod.IsLoadRequired = false;

            switch ( BitConverter.ToUInt32( buf, 0 ) )// LittleLong(*(unsigned *)buf))
            {
                case ModelDef.IDPOLYHEADER:
                    LoadAliasModel( mod, buf );
                    break;

                case ModelDef.IDSPRITEHEADER:
                    LoadSpriteModel( mod, buf );
                    break;

                default:
                    LoadBrushModel( ( BrushModel ) mod, buf );
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
            if ( version != ModelDef.ALIAS_VERSION )
                Utilities.Error( "{0} has wrong version number ({1} should be {2})",
                    mod.Name, version, ModelDef.ALIAS_VERSION );

            //
            // allocate space for a working header, plus all the data except the frames,
            // skin and group info
            //
            _Header = new aliashdr_t( );

            mod.Flags = EndianHelper.LittleLong( pinmodel.flags );

            //
            // endian-adjust and copy the data, starting with the alias model header
            //
            _Header.boundingradius = EndianHelper.LittleFloat( pinmodel.boundingradius );
            _Header.numskins = EndianHelper.LittleLong( pinmodel.numskins );
            _Header.skinwidth = EndianHelper.LittleLong( pinmodel.skinwidth );
            _Header.skinheight = EndianHelper.LittleLong( pinmodel.skinheight );

            if ( _Header.skinheight > ModelDef.MAX_LBM_HEIGHT )
                Utilities.Error( "model {0} has a skin taller than {1}", mod.Name, ModelDef.MAX_LBM_HEIGHT );

            _Header.numverts = EndianHelper.LittleLong( pinmodel.numverts );

            if ( _Header.numverts <= 0 )
                Utilities.Error( "model {0} has no vertices", mod.Name );

            if ( _Header.numverts > ModelDef.MAXALIASVERTS )
                Utilities.Error( "model {0} has too many vertices", mod.Name );

            _Header.numtris = EndianHelper.LittleLong( pinmodel.numtris );

            if ( _Header.numtris <= 0 )
                Utilities.Error( "model {0} has no triangles", mod.Name );

            _Header.numframes = EndianHelper.LittleLong( pinmodel.numframes );
            var numframes = _Header.numframes;
            if ( numframes < 1 )
                Utilities.Error( "Mod_LoadAliasModel: Invalid # of frames: {0}\n", numframes );

            _Header.size = EndianHelper.LittleFloat( pinmodel.size ) * ModelDef.ALIAS_BASE_SIZE_RATIO;
            mod.SyncType = ( SyncType ) EndianHelper.LittleLong( ( Int32 ) pinmodel.synctype );
            mod.FrameCount = _Header.numframes;

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
            for ( var i = 0; i < _Header.numverts; i++, offset += stvert_t.SizeInBytes )
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
            for ( var i = 0; i < _Header.numtris; i++, offset += dtriangle_t.SizeInBytes )
            {
                _Triangles[i] = Utilities.BytesToStructure<dtriangle_t>( buffer, offset );
                _Triangles[i].facesfront = EndianHelper.LittleLong( _Triangles[i].facesfront );

                for ( var j = 0; j < 3; j++ )
                    _Triangles[i].vertindex[j] = EndianHelper.LittleLong( _Triangles[i].vertindex[j] );
            }

            //
            // load the frames
            //
            _PoseNum = 0;
            var framesOffset = triOffset + _Header.numtris * dtriangle_t.SizeInBytes;

            _Header.frames = new maliasframedesc_t[_Header.numframes];

            for ( var i = 0; i < numframes; i++ )
            {
                var frametype = ( aliasframetype_t ) BitConverter.ToInt32( buffer, framesOffset );
                framesOffset += 4;

                if ( frametype == aliasframetype_t.ALIAS_SINGLE )
                {
                    framesOffset = LoadAliasFrame( new ByteArraySegment( buffer, framesOffset ), ref _Header.frames[i] );
                }
                else
                {
                    framesOffset = LoadAliasGroup( new ByteArraySegment( buffer, framesOffset ), ref _Header.frames[i] );
                }
            }

            _Header.numposes = _PoseNum;

            mod.Type = ModelType.mod_alias;

            // FIXME: do this right
            mod.BoundsMin = -Vector3.One * 16.0f;
            mod.BoundsMax = -mod.BoundsMin;

            //
            // build the draw lists
            //
            mesh.MakeAliasModelDisplayLists( mod, _Header );

            //
            // move the complete, relocatable alias model to the cache
            //
            mod.cache = Host.Cache.Alloc( aliashdr_t.SizeInBytes * _Header.frames.Length * maliasframedesc_t.SizeInBytes, null );
            if ( mod.cache == null )
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

            if ( version != ModelDef.SPRITE_VERSION )
                Utilities.Error( "{0} has wrong version number ({1} should be {2})",
                    mod.Name, version, ModelDef.SPRITE_VERSION );

            var numframes = EndianHelper.LittleLong( pin.numframes );

            var psprite = new msprite_t( );

            // Uze: sprite models are not cached so
            mod.cache = new CacheUser( );
            mod.cache.data = psprite;

            psprite.type = EndianHelper.LittleLong( pin.type );
            psprite.maxwidth = EndianHelper.LittleLong( pin.width );
            psprite.maxheight = EndianHelper.LittleLong( pin.height );
            psprite.beamlength = EndianHelper.LittleFloat( pin.beamlength );
            mod.SyncType = ( SyncType ) EndianHelper.LittleLong( ( Int32 ) pin.synctype );
            psprite.numframes = numframes;

            var mins = mod.BoundsMin;
            var maxs = mod.BoundsMax;
            mins.X = mins.Y = -psprite.maxwidth / 2;
            maxs.X = maxs.Y = psprite.maxwidth / 2;
            mins.Z = -psprite.maxheight / 2;
            maxs.Z = psprite.maxheight / 2;
            mod.BoundsMin = mod.BoundsMin;

            //
            // load the frames
            //
            if ( numframes < 1 )
                Utilities.Error( "Mod_LoadSpriteModel: Invalid # of frames: {0}\n", numframes );

            mod.FrameCount = numframes;

            var frameOffset = dsprite_t.SizeInBytes;

            psprite.frames = new mspriteframedesc_t[numframes];

            for ( var i = 0; i < numframes; i++ )
            {
                var frametype = ( spriteframetype_t ) BitConverter.ToInt32( buffer, frameOffset );
                frameOffset += 4;

                psprite.frames[i].type = frametype;

                if ( frametype == spriteframetype_t.SPR_SINGLE )
                {
                    frameOffset = LoadSpriteFrame( new ByteArraySegment( buffer, frameOffset ), out psprite.frames[i].frameptr, i );
                }
                else
                {
                    frameOffset = LoadSpriteGroup( new ByteArraySegment( buffer, frameOffset ), out psprite.frames[i].frameptr, i );
                }
            }

            mod.Type = ModelType.mod_sprite;
        }

        /// <summary>
        /// Mod_LoadBrushModel
        /// </summary>
        public void LoadBrushModel( BrushModel mod, Byte[] buffer )
        {
            mod.Load( mod.Name, buffer, ( tx ) => 
            {
                if ( tx.name != null && tx.name.StartsWith( "sky" ) )// !Q_strncmp(mt->name,"sky",3))
                    Host.RenderContext.InitSky( tx );
                else
                    tx.texture = BaseTexture.FromBuffer( Host.Video.Device, tx.name, new ByteArraySegment( tx.pixels ),
                        ( Int32 ) tx.width, ( Int32 ) tx.height, true, false );
            } );

            //
            // set up the submodels (FIXME: this is confusing)
            //
            for ( var i = 0; i < mod.NumSubModels; i++ )
            {
                mod.SetupSubModel( ref mod.SubModels[i] );

                if ( i < mod.NumSubModels - 1 )
                {
                    // duplicate the basic information
                    var name = "*" + ( i + 1 ).ToString( );
                    _LoadModel = FindName( name, true );
                    _LoadModel.CopyFrom( mod ); // *loadmodel = *mod;
                    _LoadModel.Name = name; //strcpy (loadmodel->name, name);
                    mod = ( BrushModel ) _LoadModel; //mod = loadmodel;
                }
            }
        }        
        
        /// <summary>
        /// Mod_LoadAllSkins
        /// </summary>
        /// <returns>Offset of next data block in source byte array</returns>
        private Int32 LoadAllSkins( Int32 numskins, ByteArraySegment data )
        {
            if ( numskins < 1 || numskins > ModelDef.MAX_SKINS )
                Utilities.Error( "Mod_LoadAliasModel: Invalid # of skins: {0}\n", numskins );

            var offset = data.StartIndex;
            var skinOffset = data.StartIndex + daliasskintype_t.SizeInBytes; //  skin = (byte*)(pskintype + 1);
            var s = _Header.skinwidth * _Header.skinheight;

            var pskintype = Utilities.BytesToStructure<daliasskintype_t>( data.Data, offset );

            for ( var i = 0; i < numskins; i++ )
            {
                if ( pskintype.type == aliasskintype_t.ALIAS_SKIN_SINGLE )
                {
                    FloodFillSkin( new ByteArraySegment( data.Data, skinOffset ), _Header.skinwidth, _Header.skinheight );

                    // save 8 bit texels for the player model to remap
                    var texels = new Byte[s]; // Hunk_AllocName(s, loadname);
                    _Header.texels[i] = texels;// -(byte*)pheader;
                    Buffer.BlockCopy( data.Data, offset + daliasskintype_t.SizeInBytes, texels, 0, s );

                    // set offset to pixel data after daliasskintype_t block...
                    offset += daliasskintype_t.SizeInBytes;

                    var name = _LoadModel.Name + "_" + i.ToString( );
                    var texture = BaseTexture.FromBuffer( Host.Video.Device, name,
                        new ByteArraySegment( data.Data, offset ), _Header.skinwidth, _Header.skinheight, true, false );

                    var index = SkinTextures.Count;

                    SkinTextures.Add( texture );

                    _Header.gl_texturenum[i, 0] =
                    _Header.gl_texturenum[i, 1] =
                    _Header.gl_texturenum[i, 2] =
                    _Header.gl_texturenum[i, 3] = index;
                    // Host.DrawingContext.LoadTexture( name, _Header.skinwidth,
                    //_Header.skinheight, new ByteArraySegment( data.Data, offset ), true, false ); // (byte*)(pskintype + 1)

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
                    for ( j = 0; j < groupskins; j++ )
                    {
                        FloodFillSkin( new ByteArraySegment( data.Data, skinOffset ), _Header.skinwidth, _Header.skinheight );
                        if ( j == 0 )
                        {
                            var texels = new Byte[s]; // Hunk_AllocName(s, loadname);
                            _Header.texels[i] = texels;// -(byte*)pheader;
                            Buffer.BlockCopy( data.Data, offset, texels, 0, s );
                        }

                        var name = String.Format( "{0}_{1}_{2}", _LoadModel.Name, i, j );

                        var texture = BaseTexture.FromBuffer( Host.Video.Device, name,
                            new ByteArraySegment( data.Data, offset ), _Header.skinwidth, _Header.skinheight, true, false );

                        var index = SkinTextures.Count;

                        SkinTextures.Add( texture );

                        _Header.gl_texturenum[i, j & 3] = index;// //  (byte*)(pskintype)

                        offset += s;

                        pskintype = Utilities.BytesToStructure<daliasskintype_t>( data.Data, offset );
                    }
                    var k = j;
                    for ( ; j < 4; j++ )
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
            frame.bboxmin.Init( );
            frame.bboxmax.Init( );

            for ( var i = 0; i < 3; i++ )
            {
                // these are byte values, so we don't have to worry about
                // endianness
                frame.bboxmin.v[i] = pdaliasframe.bboxmin.v[i];
                frame.bboxmax.v[i] = pdaliasframe.bboxmax.v[i];
            }

            var verts = new trivertx_t[_Header.numverts];
            var offset = pin.StartIndex + daliasframe_t.SizeInBytes; //pinframe = (trivertx_t*)(pdaliasframe + 1);
            for ( var i = 0; i < verts.Length; i++, offset += trivertx_t.SizeInBytes )
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

            frame.Init( );
            frame.firstpose = _PoseNum;
            frame.numposes = numframes;

            for ( var i = 0; i < 3; i++ )
            {
                // these are byte values, so we don't have to worry about endianness
                frame.bboxmin.v[i] = pingroup.bboxmin.v[i];
                frame.bboxmin.v[i] = pingroup.bboxmax.v[i];
            }

            offset += daliasgroup_t.SizeInBytes;
            var pin_intervals = Utilities.BytesToStructure<daliasinterval_t>( pin.Data, offset ); // (daliasinterval_t*)(pingroup + 1);

            frame.interval = EndianHelper.LittleFloat( pin_intervals.interval );

            offset += numframes * daliasinterval_t.SizeInBytes;

            for ( var i = 0; i < numframes; i++ )
            {
                var tris = new trivertx_t[_Header.numverts];
                var offset1 = offset + daliasframe_t.SizeInBytes;
                for ( var j = 0; j < _Header.numverts; j++, offset1 += trivertx_t.SizeInBytes )
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

            var pspriteframe = new mspriteframe_t( );

            ppframe = pspriteframe;

            pspriteframe.width = width;
            pspriteframe.height = height;
            var orgx = EndianHelper.LittleLong( pinframe.origin[0] );
            var orgy = EndianHelper.LittleLong( pinframe.origin[1] );

            pspriteframe.up = orgy;// origin[1];
            pspriteframe.down = orgy - height;
            pspriteframe.left = orgx;// origin[0];
            pspriteframe.right = width + orgx;// origin[0];

            var name = _LoadModel.Name + "_" + framenum.ToString( );
            var index = SpriteTextures.Count;
            var texture = BaseTexture.FromBuffer( Host.Video.Device, name, new ByteArraySegment( pin.Data, pin.StartIndex + dspriteframe_t.SizeInBytes ), width, height, true, true );
            SpriteTextures.Add( texture );
            pspriteframe.gl_texturenum = index;


            //Host.DrawingContext.LoadTexture( name, width, height,
            //new ByteArraySegment( pin.Data, pin.StartIndex + dspriteframe_t.SizeInBytes ), true, true ); //   (byte *)(pinframe + 1)

            return pin.StartIndex + dspriteframe_t.SizeInBytes + size;
        }

        /// <summary>
        /// Mod_LoadSpriteGroup
        /// </summary>
        private Int32 LoadSpriteGroup( ByteArraySegment pin, out Object ppframe, Int32 framenum )
        {
            var pingroup = Utilities.BytesToStructure<dspritegroup_t>( pin.Data, pin.StartIndex );

            var numframes = EndianHelper.LittleLong( pingroup.numframes );
            var pspritegroup = new mspritegroup_t( );
            pspritegroup.numframes = numframes;
            pspritegroup.frames = new mspriteframe_t[numframes];
            ppframe = pspritegroup;// (mspriteframe_t*)pspritegroup;
            var poutintervals = new Single[numframes];
            pspritegroup.intervals = poutintervals;

            var offset = pin.StartIndex + dspritegroup_t.SizeInBytes;
            for ( var i = 0; i < numframes; i++, offset += dspriteinterval_t.SizeInBytes )
            {
                var interval = Utilities.BytesToStructure<dspriteinterval_t>( pin.Data, offset );
                poutintervals[i] = EndianHelper.LittleFloat( interval.interval );
                if ( poutintervals[i] <= 0 )
                    Utilities.Error( "Mod_LoadSpriteGroup: interval<=0" );
            }

            for ( var i = 0; i < numframes; i++ )
            {
                Object tmp;
                offset = LoadSpriteFrame( new ByteArraySegment( pin.Data, offset ), out tmp, framenum * 100 + i );
                pspritegroup.frames[i] = ( mspriteframe_t ) tmp;
            }

            return offset;
        }

        /// <summary>
        /// Mod_FloodFillSkin
        /// Fill background pixels so mipmapping doesn't have haloes - Ed
        /// </summary>
        private void FloodFillSkin( ByteArraySegment skin, Int32 skinwidth, Int32 skinheight )
        {
            var filler = new FloodFiller( skin, skinwidth, skinheight );
            filler.Perform( Host.Video.Device.Palette.Table8to24 );
        }
    }
}
