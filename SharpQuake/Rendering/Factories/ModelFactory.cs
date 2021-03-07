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
using SharpQuake.Framework;
using SharpQuake.Framework.IO;
using SharpQuake.Game.Data.Models;
using SharpQuake.Renderer.Textures;

// gl_model.c -- model loading and caching

// models are the only shared resource between a client and server running
// on the same machine.

namespace SharpQuake
{
	/// <summary>
	/// Mod_functions
	/// </summary>
	public class ModelFactory
    {
        public Single SubdivideSize
        {
            get
            {
                return _glSubDivideSize.Get<Int32>( );
            }
        }

        public Host Host
        {
            get;
            private set;
        }

        private AliasModelBuilder AliasModelBuilder
		{
            get;
            set;
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

        private ClientVariable _glSubDivideSize
        {
            get;
            set;
        }

        private List<ModelData> ModelCache
        {
            get;
            set;
        }

        private ModelData CurrentModel
        {
            get;
            set;
        }

        public ModelFactory( Host host )
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
            ModelCache = new List<ModelData>( ModelDef.MAX_MOD_KNOWN );
            AliasModelBuilder = new AliasModelBuilder();

            if ( _glSubDivideSize == null )
                _glSubDivideSize = Host.CVars.Add( "gl_subdivide_size", 128, ClientVariableFlags.Archive );
        }

        /// <summary>
        /// Mod_ClearAll
        /// </summary>
        public void ClearAll( )
        {
            for ( var i = 0; i < ModelCache.Count; i++ )
            {
                var mod = ModelCache[i];

                if ( mod.Type != ModelType.Alias )
                    mod.IsLoadRequired = true;
            }
        }

        /// <summary>
        /// Mod_ForName
        /// Loads in a model for the given name
        /// </summary>
        public ModelData ForName( String name, Boolean crash, ModelType type )
        {
            var mod = FindName( name, type );

            return Load( mod, crash, type );
        }

        /// <summary>
        /// Mod_Extradata
        /// handles caching
        /// </summary>
        public aliashdr_t GetExtraData( ModelData mod )
        {
            var r = Host.Cache.Check( mod.cache );

            if ( r != null )
                return ( aliashdr_t ) r;

            Load( mod, true, ModelType.Alias );

            if ( mod.cache.data == null )
                Utilities.Error( "Mod_Extradata: caching failed" );

            return ( aliashdr_t ) mod.cache.data;
        }

        /// <summary>
        /// Mod_TouchModel
        /// </summary>
        public void TouchModel( String name )
        {
            ModelType type;

            var n = name.ToLower( );

            if ( n.StartsWith( "*" ) && !n.Contains( ".mdl" ) || n.Contains( ".bsp" ) )
                type = ModelType.Brush;
            else if ( n.Contains( ".mdl" ) )
                type = ModelType.Alias;
            else
                type = ModelType.Sprite;

            var mod = FindName( name, type );

            if ( !mod.IsLoadRequired )
            {
                if ( mod.Type == ModelType.Alias )
                    Host.Cache.Check( mod.cache );
            }
        } 

        // Mod_Print
        public void Print( CommandMessage msg )
        {
            var names = String.Join( "\n", ModelCache.Select( m => m.Name ) );
            ConsoleWrapper.Print( $"Cached models:\n{names}\n" );
        }

        /// <summary>
        /// Mod_FindName
        /// </summary>
        private ModelData FindName( String name, ModelType type )
        {
            if ( String.IsNullOrEmpty( name ) )
                Utilities.Error( "Mod_ForName: NULL name" );

            var mod = ModelCache.Where( m => m.Name == name ).FirstOrDefault( );

            if ( mod == null )
            {
                if ( ModelCache.Count == ModelDef.MAX_MOD_KNOWN )
                    Utilities.Error( "mod_numknown == MAX_MOD_KNOWN" );

                switch ( type )
                {
                    case ModelType.Brush:
                        mod = new BrushModelData( Host.Model.SubdivideSize, Host.RenderContext.NoTextureMip );
                        break;

                    case ModelType.Sprite:
                        mod = new AliasModelData( Host.RenderContext.NoTextureMip );
                        break;

                    case ModelType.Alias:
                        mod = new SpriteModelData( Host.RenderContext.NoTextureMip );
                        break;
                }

                mod.Name = name;
                mod.IsLoadRequired = true;
                ModelCache.Add( mod );
            }

            return mod;
        }

        /// <summary>
        /// Mod_LoadModel
        /// Loads a model into the cache
        /// </summary>
        private ModelData Load( ModelData mod, Boolean crash, ModelType type )
        {
            var name = mod.Name;

            if ( mod.Type != type )
            {
                ModelData newMod = null;

                switch ( type )
                {
                    case ModelType.Brush:
                        newMod = new BrushModelData( Host.Model.SubdivideSize, Host.RenderContext.NoTextureMip );
                        newMod.CopyFrom( mod );
                        break;

                    case ModelType.Alias:
                        newMod = new AliasModelData( Host.RenderContext.NoTextureMip );
                        newMod.CopyFrom( mod );
                        break;

                    case ModelType.Sprite:
                        newMod = new SpriteModelData( Host.RenderContext.NoTextureMip );
                        newMod.CopyFrom( mod );
                        break;
                }

                newMod.Name = mod.Name;

                ModelCache.RemoveAll( k => k.Name == name );

                mod = newMod;

                ModelCache.Add( mod );
            }

            if ( !mod.IsLoadRequired )
            {
                if ( mod.Type == ModelType.Alias )
                {
                    if ( Host.Cache.Check( mod.cache ) != null )
                        return mod;
                }
                else
                    return mod;		// not cached at all
            }

            // Load the file
            var buf = FileSystem.LoadFile( mod.Name );

            if ( buf == null )
            {
                if ( crash )
                    Utilities.Error( "Mod_NumForName: {0} not found", mod.Name );

                return null;
            }

            // Allocate a new model
            Allocate( mod, buf );

            return mod;
        }

        private void Allocate( ModelData mod, Byte[] buf )
        {
            CurrentModel = mod;

            mod.IsLoadRequired = false;

            switch ( BitConverter.ToUInt32( buf, 0 ) )
            {
                case ModelDef.IDPOLYHEADER:
                    LoadAlias( ( AliasModelData ) mod, buf );
                    break;

                case ModelDef.IDSPRITEHEADER:
                    LoadSprite( ( SpriteModelData ) mod, buf );
                    break;

                default:
                    LoadBrush( ( BrushModelData ) mod, buf );
                    break;
            }
        }

        /// <summary>
        /// Mod_LoadAliasModel
        /// </summary>
        private void LoadAlias( AliasModelData mod, Byte[] buffer )
        {
            mod.Load( Host.Video.Device.Palette.Table8to24, mod.Name, buffer, ( n, b, h ) => 
            {
                var texture = ( Renderer.OpenGL.Textures.GLTexture ) BaseTexture.FromBuffer( Host.Video.Device, n,
                        b, h.skinwidth, h.skinheight, true, false );

                SkinTextures.Add( texture );

                return texture.GLDesc.TextureNumber;
            }, ( model, header ) => 
            {
                // Build the draw lists
                AliasModelBuilder.MakeDisplayLists( model );

                // Move the complete, relocatable alias model to the cache
                mod.cache = Host.Cache.Alloc( aliashdr_t.SizeInBytes * header.frames.Length * maliasframedesc_t.SizeInBytes, null );

                if ( mod.cache == null )
                    return;

                mod.cache.data = header;
            } );
        }

        /// <summary>
        /// Mod_LoadSpriteModel
        /// </summary>
        private void LoadSprite( SpriteModelData mod, Byte[] buffer )
        {
            mod.Load( mod.Name, buffer, ( name, buf, width, height ) =>
            {
                var texture = ( Renderer.OpenGL.Textures.GLTexture ) BaseTexture.FromBuffer( Host.Video.Device, name,
                        buf, width, height, hasMipMap: true, hasAlpha: true );

                SpriteTextures.Add( texture );

                return texture.GLDesc.TextureNumber;
            } );
        }

        /// <summary>
        /// Mod_LoadBrushModel
        /// </summary>
        private void LoadBrush( BrushModelData mod, Byte[] buffer )
        {
            mod.Load( mod.Name, buffer, ( tx ) => 
            {
                if ( tx.name != null && tx.name.StartsWith( "sky" ) )// !Q_strncmp(mt->name,"sky",3))
                    Host.RenderContext.WarpableTextures.InitSky( tx );
                else
                {   
                    tx.texture = BaseTexture.FromBuffer( Host.Video.Device, tx.name, new ByteArraySegment( tx.pixels ),
                     ( Int32 ) tx.width, ( Int32 ) tx.height, true, true );
                }
            }, ( textureFile ) =>             
            {
				var lowerName = textureFile.ToLower( );

				if ( Host.WadTextures.ContainsKey( lowerName ) )
				{
					var wadFile = Host.WadTextures[lowerName];
					var wad = Host.WadFiles[wadFile];

					return wad.GetLumpBuffer( textureFile );
				}

				return null;
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
                    CurrentModel = FindName( name, ModelType.Brush );
                    CurrentModel.CopyFrom( mod ); // *loadmodel = *mod;
                    CurrentModel.Name = name; //strcpy (loadmodel->name, name);
                    mod = ( BrushModelData ) CurrentModel; //mod = loadmodel;
                }
            }
        }
    }
}
