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
using SharpQuake.Renderer.Textures;
using OpenTK.Graphics.OpenGL;
using SharpQuake.Framework;
using System.Runtime.InteropServices;
using System.Linq;

namespace SharpQuake.Renderer.OpenGL.Textures
{
    public class GLTexture : BaseTexture
    {
        public static Int32 CurrentTextureNumber
        {
            get;
            set;
        }

        public static Int32 Texels
        {
            get;
            set;
        }

        static GLTexture()
        {
            CurrentTextureNumber = 1;
        }

        public GLTextureDesc GLDesc
        {
            get
            {
                return ( GLTextureDesc ) Desc;
            }
            set
            {
                Desc = ( BaseTextureDesc ) value;
            }
        }

        // gl_lightmap_format = 4
        public PixelInternalFormat SolidFormat = PixelInternalFormat.Three;

        // gl_solid_format = 3
        public PixelInternalFormat AlphaFormat = PixelInternalFormat.Four;


        public GLTexture( GLDevice device, GLTextureDesc desc ) : base( device, desc )
        {

        }

        public override void Initialise( ByteArraySegment buffer )
        {
            base.Initialise( buffer );

            if ( !Desc.IsLightMap )
            {
                GLDesc.TextureNumber = CurrentTextureNumber;
                GenerateTextureNumber( );
            }
        }

        public override void Initialise( UInt32[] buffer )
        {
            base.Initialise( buffer );

            if ( !Desc.IsLightMap )
            {
                GLDesc.TextureNumber = CurrentTextureNumber;
                GenerateTextureNumber( );
            }
        }

        public override void Bind( )
        {
            GL.BindTexture( TextureTarget.Texture2D, GLDesc.TextureNumber );
        }

        public override void Upload( System.Boolean resample )
        {
            if ( Desc.IsLightMap )
            {
                GLDesc.TextureNumber = CurrentTextureNumber;

                Bind( );
                UploadLightmap( );
            }
            else
            {
                Bind( );

                if ( Buffer32?.Length > 0 )
                    Upload32( Buffer32, Desc.HasAlpha, resample );
                else
                    Upload8( resample );
            }

            if ( Desc.IsLightMap )
                GenerateTextureNumber( );
        }

        // GL_Upload32
        protected override void Upload32( UInt32[] data, System.Boolean alpha, System.Boolean resample )
        {
            base.Upload32( data, alpha, resample );

            var filter = ( GLTextureFilter ) Device.GetTextureFilters( Desc.Filter );

            var samples = alpha ? AlphaFormat : SolidFormat;
            UInt32[] scaled;

            Texels += Desc.ScaledWidth * Desc.ScaledHeight;

            if ( Desc.ScaledWidth == Desc.Width && Desc.ScaledHeight == Desc.Height )
            {
                if ( !Desc.HasMipMap )
                {
                    var h2 = GCHandle.Alloc( data, GCHandleType.Pinned );
                    try
                    {
                        GL.TexImage2D( TextureTarget.Texture2D, 0, samples, Desc.ScaledWidth, Desc.ScaledHeight, 0,
                            PixelFormat.Rgba, PixelType.UnsignedByte, h2.AddrOfPinnedObject( ) );
                    }
                    finally
                    {
                        h2.Free( );
                    }
                    goto Done;
                }
                else
                {
                    scaled = new UInt32[Desc.ScaledWidth * Desc.ScaledHeight]; // uint[1024 * 512];
                    data.CopyTo( scaled, 0 );
                }
            }
            else if ( resample )
                Resample( data, Desc.Width, Desc.Height, out scaled, Desc.ScaledWidth, Desc.ScaledHeight );
            else
            {
                Desc.ScaledWidth = Desc.Width;
                Desc.ScaledHeight = Desc.Height;
                scaled = data;
            }

            var h = GCHandle.Alloc( scaled, GCHandleType.Pinned );
            try
            {
                var ptr = h.AddrOfPinnedObject( );
                GL.TexImage2D( TextureTarget.Texture2D, 0, samples, Desc.ScaledWidth, Desc.ScaledHeight, 0,
                    PixelFormat.Rgba, PixelType.UnsignedByte, ptr );
                var err = GL.GetError( ); // debug
                if ( Desc.HasMipMap )
                {
                    var miplevel = 0;
                    while ( Desc.ScaledWidth > 1 || Desc.ScaledHeight > 1 )
                    {
                        MipMap( scaled, Desc.ScaledWidth, Desc.ScaledHeight );
                        Desc.ScaledWidth >>= 1;
                        Desc.ScaledHeight >>= 1;
                        if ( Desc.ScaledWidth < 1 )
                            Desc.ScaledWidth = 1;
                        if ( Desc.ScaledHeight < 1 )
                            Desc.ScaledHeight = 1;
                        miplevel++;

                        GL.TexImage2D( TextureTarget.Texture2D, miplevel, samples, Desc.ScaledWidth, Desc.ScaledHeight, 0,
                            PixelFormat.Rgba, PixelType.UnsignedByte, ptr );
                    }
                }
            }
            finally
            {
                h.Free( );
            }

        Done:
            ;
            if ( !String.IsNullOrEmpty( Desc.BlendMode ) )
                Device.SetBlendMode( Desc.BlendMode );

            var min = filter.Minimise;
            var mag = filter.Maximise;

            if ( Desc.HasMipMap )
                ( ( GLDevice ) Device ).SetTextureFilters( min, mag );
            else
                ( ( GLDevice ) Device ).SetTextureFilters( ( TextureMinFilter ) mag, mag );
        }

        public override void UploadLightmap( )
        {
            var lightmaps = Buffer.Data;

            var handle = GCHandle.Alloc( lightmaps, GCHandleType.Pinned );
            try
            {
                var ptr = handle.AddrOfPinnedObject( );
                var lmAddr = ptr.ToInt64( );

                for ( var i = 0; i < RenderDef.MAX_LIGHTMAPS; i++ )
                {
                    if ( LightMapData[i, 0] == 0 )
                        break;		// no more used

                    LightMapModified[i] = false;
                    LightMapRectChange[i].l = RenderDef.BLOCK_WIDTH;
                    LightMapRectChange[i].t = RenderDef.BLOCK_HEIGHT;
                    LightMapRectChange[i].w = 0;
                    LightMapRectChange[i].h = 0;

                    GL.BindTexture( TextureTarget.Texture2D, GLDesc.TextureNumber + i );

                    Device.SetTextureFilters( "GL_LINEAR" );

                    var pixelFormat = ( GLPixelFormat ) Device.PixelFormats.Where( p => p.Name == Desc.LightMapFormat ).FirstOrDefault( );

                    var addr = lmAddr + i * RenderDef.BLOCK_WIDTH * RenderDef.BLOCK_HEIGHT * Desc.LightMapBytes;
                    GL.TexImage2D( TextureTarget.Texture2D, 0, ( PixelInternalFormat ) Desc.LightMapBytes,
                        RenderDef.BLOCK_WIDTH, RenderDef.BLOCK_HEIGHT, 0, pixelFormat.Value, PixelType.UnsignedByte, new IntPtr( addr ) );
                    GenerateTextureNumber( );
                }
            }
            finally
            {
                handle.Free( );
            }

            GenerateTextureNumber( );
        }

        public override void CommitLightmap( Byte[] data, Int32 i )
        {
            LightMapModified[i] = false;
            var theRect = LightMapRectChange[i];
            var handle = GCHandle.Alloc( data, GCHandleType.Pinned );

            var format = ( GLPixelFormat ) Device.PixelFormats.Where( p => p.Name == Desc.LightMapFormat ).FirstOrDefault( );

            try
            {
                var addr = handle.AddrOfPinnedObject( ).ToInt64( ) +
                    ( i * RenderDef.BLOCK_HEIGHT + theRect.t ) * RenderDef.BLOCK_WIDTH * Desc.LightMapBytes;
                GL.TexSubImage2D( TextureTarget.Texture2D, 0, 0, theRect.t,
                    RenderDef.BLOCK_WIDTH, theRect.h, format == null ? PixelFormat.Rgba : format.Value,
                    PixelType.UnsignedByte, new IntPtr( addr ) );
            }
            finally
            {
                handle.Free( );
            }
            theRect.l = RenderDef.BLOCK_WIDTH;
            theRect.t = RenderDef.BLOCK_HEIGHT;
            theRect.h = 0;
            theRect.w = 0;
            LightMapRectChange[i] = theRect;
        }

        public override void BindLightmap( Int32 number )
        {
            GL.BindTexture( TextureTarget.Texture2D, number );
        }

        public override void TranslateAndUpload( Byte[] original, Byte[] translate, Int32 inWidth, Int32 inHeight, Int32 maxWidth = 512, Int32 maxHeight = 256, Int32 mip = 0 )
        {
            // because this happens during gameplay, do it fast
            // instead of sending it through gl_upload 8
            Bind( );
            //Host.DrawingContext.Bind( _PlayerTextures + playernum );

            var scaled_width = ( Int32 ) ( maxWidth < 512 ? maxWidth : 512 );
            var scaled_height = ( Int32 ) ( maxHeight < 256 ? maxHeight : 256 );

            // allow users to crunch sizes down even more if they want
            scaled_width >>= ( Int32 ) mip;
            scaled_height >>= ( Int32 ) mip;

            UInt32 fracstep, frac;
            Int32 destOffset;

            var translate32 = new UInt32[256];
            for ( var i = 0; i < 256; i++ )
                translate32[i] = Device.Palette.Table8to24[translate[i]];

            var dest = new UInt32[512 * 256];
            destOffset = 0;
            fracstep = ( UInt32 ) ( inWidth * 0x10000 / scaled_width );
            for ( var i = 0; i < scaled_height; i++, destOffset += scaled_width )
            {
                var srcOffset = inWidth * ( i * inHeight / scaled_height );
                frac = fracstep >> 1;
                for ( var j = 0; j < scaled_width; j += 4 )
                {
                    dest[destOffset + j] = translate32[original[srcOffset + ( frac >> 16 )]];
                    frac += fracstep;
                    dest[destOffset + j + 1] = translate32[original[srcOffset + ( frac >> 16 )]];
                    frac += fracstep;
                    dest[destOffset + j + 2] = translate32[original[srcOffset + ( frac >> 16 )]];
                    frac += fracstep;
                    dest[destOffset + j + 3] = translate32[original[srcOffset + ( frac >> 16 )]];
                    frac += fracstep;
                }
            }
            var handle = GCHandle.Alloc( dest, GCHandleType.Pinned );
            try
            {
                GL.TexImage2D( TextureTarget.Texture2D, 0, PixelInternalFormat.Three, scaled_width, scaled_height, 0,
                     PixelFormat.Rgba, PixelType.UnsignedByte, handle.AddrOfPinnedObject( ) );
            }
            finally
            {
                handle.Free( );
            }
            GL.TexEnv( TextureEnvTarget.TextureEnv, TextureEnvParameter.TextureEnvMode, ( Int32 ) TextureEnvMode.Modulate );
            Device.SetTextureFilters( "GL_LINEAR" );
        }

        /// <summary>
        /// gets texture_extension_number++
        /// </summary>
        public static Int32 GenerateTextureNumber( )
        {
            return CurrentTextureNumber++;
        }
    }
}
