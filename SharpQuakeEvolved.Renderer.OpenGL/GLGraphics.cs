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
using SharpQuake.Renderer.OpenGL.Textures;
using SharpQuake.Renderer.Textures;
using OpenTK.Graphics.OpenGL;
using System.Drawing;
using SharpQuake.Framework.Mathematics;
using SharpQuake.Framework;
using System.Runtime.InteropServices;

namespace SharpQuake.Renderer.OpenGL
{
    public class GLGraphics : BaseGraphics
    {

        public GLGraphics( GLDevice device ) : base( device )
        {
        }

        public override void Fill( Int32 x, Int32 y, Int32 width, Int32 height, Color colour )
        {
            GL.Disable( EnableCap.Texture2D );
            GL.Color3( colour );
            GL.Begin( PrimitiveType.Quads );
            GL.Vertex2( x, y );
            GL.Vertex2( x + width, y );
            GL.Vertex2( x + width, y + height );
            GL.Vertex2( x, y + height );
            GL.End( );
            GL.Color3( 1f, 1f, 1f );
            GL.Enable( EnableCap.Texture2D );
        }

        public override void DrawTexture2D( BaseTexture texture, RectangleF sourceRect, Rectangle destRect, Color? colour = null, System.Boolean hasAlpha = false )
        {
            if ( hasAlpha )
            {
                GL.Disable( EnableCap.AlphaTest );
                GL.Enable( EnableCap.Blend );
            }

            GL.Enable( EnableCap.Texture2D );

            texture.Bind( );

            GL.Begin( PrimitiveType.Quads );            

            if ( colour.HasValue )
            {
                if ( hasAlpha )
                    GL.Color4( colour.Value );
                else
                    GL.Color3( colour.Value );
            }
            else
            {
                if ( hasAlpha )
                    GL.Color4( 1f, 1f, 1f, 1f );
                else
                    GL.Color3( 1f, 1f, 1f );
            }

            GL.TexCoord2( sourceRect.X, sourceRect.Y );
            GL.Vertex2( destRect.X, destRect.Y );
            GL.TexCoord2( sourceRect.X + sourceRect.Width, sourceRect.Y );
            GL.Vertex2( destRect.X + destRect.Width, destRect.Y );
            GL.TexCoord2( sourceRect.X + sourceRect.Width, sourceRect.Y + sourceRect.Height );
            GL.Vertex2( destRect.X + destRect.Width, destRect.Y + destRect.Height );
            GL.TexCoord2( sourceRect.X, sourceRect.Y + sourceRect.Height );
            GL.Vertex2( destRect.X, destRect.Y + destRect.Height );
            GL.End( );

            GL.Color3( 1f, 1f, 1f );

            if ( hasAlpha )
            {
                GL.Enable( EnableCap.AlphaTest );
                GL.Disable( EnableCap.Blend );
            }

            GL.Disable( EnableCap.Texture2D );
        }
        
        public override void BeginParticles( BaseTexture texture )
        {
            base.BeginParticles( texture );

            texture.Bind( );

            GL.Enable( EnableCap.Blend );
            GL.Enable( EnableCap.Texture2D );
            GL.TexEnv( TextureEnvTarget.TextureEnv, TextureEnvParameter.TextureEnvMode, ( Int32 ) TextureEnvMode.Modulate );
            GL.Begin( PrimitiveType.Triangles );
        }

        public override void DrawParticle( Single colour, Vector3 up, Vector3 right, Vector3 origin, Single scale )
        {
            // Uze todo: check if this is correct
            var c = Device.Palette.Table8to24[( Byte ) colour];
            GL.Color4( ( Byte ) ( c & 0xff ), ( Byte ) ( ( c >> 8 ) & 0xff ), ( Byte ) ( ( c >> 16 ) & 0xff ), ( Byte ) ( ( c >> 24 ) & 0xff ) );
            GL.TexCoord2( 0f, 0 );
            GL.Vertex3( origin.X, origin.Y, origin.Z );
            GL.TexCoord2( 1f, 0 );
            var v = origin + up * scale;
            GL.Vertex3( v.X, v.Y, v.Z );
            GL.TexCoord2( 0f, 1 );
            v = origin + right * scale;
            GL.Vertex3( v.X, v.Y, v.Z );
        }

        public override void EndParticles( )
        {
            GL.End( );
            GL.Disable( EnableCap.Blend );
            GL.Disable( EnableCap.Texture2D );
            GL.TexEnv( TextureEnvTarget.TextureEnv, TextureEnvParameter.TextureEnvMode, ( Int32 ) TextureEnvMode.Replace );

            base.EndParticles( );
        }

        /// <summary>
        /// EmitSkyPolys
        /// </summary>
        public override void EmitSkyPolys( GLPoly polys, Vector3 origin, Single speed, System.Boolean blend = false )
        {
            GL.Color3( 1f, 1f, 1f );

            if ( blend )
                GL.Enable( EnableCap.Blend );

            GL.Enable( EnableCap.Texture2D );

            for ( var p = polys; p != null; p = p.next )
            {
                GL.Begin( PrimitiveType.Polygon );
                for ( var i = 0; i < p.numverts; i++ )
                {
                    var v = p.verts[i];
                    var dir = new Vector3( v[0] - origin.X, v[1] - origin.Y, v[2] - origin.Z );
                    dir.Z *= 3; // flatten the sphere

                    dir.Normalize( );
                    dir *= 6 * 63;

                    var s = ( speed + dir.X ) / 128.0f;
                    var t = ( speed + dir.Y ) / 128.0f;

                    GL.TexCoord2( s, t );
                    GL.Vertex3( v );
                }
                GL.End( );
            }

            GL.Disable( EnableCap.Texture2D );

            if ( blend )
                GL.Disable( EnableCap.Blend );
        }

        public override void DrawPoly( GLPoly p, Single scaleX = 1f, Single scaleY = 1f, System.Boolean isLightmap = false )
        {
            GL.Color3( 1f, 1f, 1f );
            GL.Enable( EnableCap.Texture2D );
            GL.Begin( PrimitiveType.Polygon );
            for ( var i = 0; i < p.numverts; i++ )
            {
                var v = p.verts[i];
                if ( isLightmap )
                    GL.TexCoord2( v[5], v[6] );
                else
                    GL.TexCoord2( v[3] * scaleX, v[4] * scaleY );

                GL.Vertex3( v );
            }
            GL.End( );
            GL.Disable( EnableCap.Texture2D );
        }

        /// <summary>
        /// EmitWaterPolys
        /// </summary>
        public override void EmitWaterPolys( ref Single[] turbSin, Double time, Double turbScale, GLPoly polys )
        {
            GL.Color3( 1f, 1f, 1f );

            GL.Enable( EnableCap.Texture2D );

            for ( var p = polys; p != null; p = p.next )
            {
                GL.Begin( PrimitiveType.Polygon );
                for ( var i = 0; i < p.numverts; i++ )
                {
                    var v = p.verts[i];
                    var os = v[3];
                    var ot = v[4];

                    var s = os + turbSin[( Int32 ) ( ( ot * 0.125 + time ) * turbScale ) & 255];
                    s *= ( 1.0f / 64 );

                    var t = ot + turbSin[( Int32 ) ( ( os * 0.125 + time ) * turbScale ) & 255];
                    t *= ( 1.0f / 64 );

                    GL.TexCoord2( s, t );
                    GL.Vertex3( v );
                }
                GL.End( );
            }
            GL.Disable( EnableCap.Texture2D );
        }

        public override void DrawWaterPoly( GLPoly p, Double time )
        {
            GL.Color3( 1f, 1f, 1f );

            Device.DisableMultitexture( );
            GL.Enable( EnableCap.Texture2D );

            var nv = new Single[3];
            GL.Begin( PrimitiveType.TriangleFan );
            for ( var i = 0; i < p.numverts; i++ )
            {
                var v = p.verts[i];

                GL.TexCoord2( v[3], v[4] );

                nv[0] = ( Single ) ( v[0] + 8 * Math.Sin( v[1] * 0.05 + time ) * Math.Sin( v[2] * 0.05 + time ) );
                nv[1] = ( Single ) ( v[1] + 8 * Math.Sin( v[0] * 0.05 + time ) * Math.Sin( v[2] * 0.05 + time ) );
                nv[2] = v[2];

                GL.Vertex3( nv );
            }
            GL.End( );
            GL.Disable( EnableCap.Texture2D );
        }

        public override void DrawWaterPolyLightmap( GLPoly p, Double time, System.Boolean blend = false )
        {
            GL.Color3( 1f, 1f, 1f );

            if ( blend )
                GL.Enable( EnableCap.Blend );

            Device.DisableMultitexture( );

            GL.Enable( EnableCap.Texture2D );
            var nv = new Single[3];
            GL.Begin( PrimitiveType.TriangleFan );

            for ( var i = 0; i < p.numverts; i++ )
            {
                var v = p.verts[i];
                GL.TexCoord2( v[5], v[6] );

                nv[0] = ( Single ) ( v[0] + 8 * Math.Sin( v[1] * 0.05 + time ) * Math.Sin( v[2] * 0.05 + time ) );
                nv[1] = ( Single ) ( v[1] + 8 * Math.Sin( v[0] * 0.05 + time ) * Math.Sin( v[2] * 0.05 + time ) );
                nv[2] = v[2];

                GL.Vertex3( nv );
            }
            GL.End( );
            GL.Disable( EnableCap.Texture2D );

            if ( blend )
                GL.Disable( EnableCap.Blend );
        }

        /// <summary>
        /// Draw_TransPicTranslate
        /// Only used for the player color selection menu
        /// </summary>
        public override void DrawTransTranslate( BaseTexture texture, Int32 x, Int32 y, Int32 width, Int32 height, Byte[] translation )
        {
            texture.Bind( );

            var c = width * height;
            var destOffset = 0;
            var trans = new UInt32[64 * 64];

            for ( var v = 0; v < 64; v++, destOffset += 64 )
            {
                var srcOffset = ( ( v * height ) >> 6 ) * width;
                for ( var u = 0; u < 64; u++ )
                {
                    UInt32 p = texture.Buffer.Data[srcOffset + ( ( u * width ) >> 6 )];
                    if ( p == 255 )
                        trans[destOffset + u] = p;
                    else
                        trans[destOffset + u] = Device.Palette.Table8to24[translation[p]];
                }
            }

            var handle = GCHandle.Alloc( trans, GCHandleType.Pinned );
            try
            {
                GL.TexImage2D( TextureTarget.Texture2D, 0, PixelInternalFormat.Four, 64, 64, 0,
                    PixelFormat.Rgba, PixelType.UnsignedByte, handle.AddrOfPinnedObject( ) );
            }
            finally
            {
                handle.Free( );
            }

            Device.SetTextureFilters( "GL_LINEAR" );

            GL.Color3( 1f, 1, 1 );
            GL.Enable( EnableCap.Texture2D );
            GL.Begin( PrimitiveType.Quads );
            GL.TexCoord2( 0f, 0 );
            GL.Vertex2( ( Single ) x, y );
            GL.TexCoord2( 1f, 0 );
            GL.Vertex2( ( Single ) x + width, y );
            GL.TexCoord2( 1f, 1 );
            GL.Vertex2( ( Single ) x + width, y + height );
            GL.TexCoord2( 0f, 1 );
            GL.Vertex2( ( Single ) x, y + height );
            GL.End( );
            GL.Disable( EnableCap.Texture2D );
        }

        public override void PolyBlend( Color4 colour )
        {
            Device.DisableMultitexture( );

            GL.Disable( EnableCap.AlphaTest );
            GL.Enable( EnableCap.Blend );
            GL.Disable( EnableCap.DepthTest );
            GL.Disable( EnableCap.Texture2D );

            GL.LoadIdentity( );

            GL.Rotate( -90f, 1, 0, 0 );	    // put Z going up
            GL.Rotate( 90f, 0, 0, 1 );	    // put Z going up

            GL.Color4( colour.R, colour.G, colour.B, colour.A );
            GL.Begin( PrimitiveType.Quads );
            GL.Vertex3( 10f, 100, 100 );
            GL.Vertex3( 10f, -100, 100 );
            GL.Vertex3( 10f, -100, -100 );
            GL.Vertex3( 10f, 100, -100 );
            GL.End( );

            GL.Disable( EnableCap.Blend );
            GL.Enable( EnableCap.Texture2D );
            GL.Enable( EnableCap.AlphaTest );
        }

        public override void FadeScreen( )
        {
            GL.Enable( EnableCap.Blend );
            GL.Disable( EnableCap.Texture2D );

            GL.Color4( 0, 0, 0, 0.8f );
            GL.Begin( PrimitiveType.Quads );

            GL.Vertex2( 0f, 0f );
            GL.Vertex2( Device.Desc.ActualWidth, 0f );
            GL.Vertex2( ( Single ) Device.Desc.ActualWidth, ( Single ) Device.Desc.ActualHeight );
            GL.Vertex2( 0f, Device.Desc.ActualHeight );

            GL.End( );
            GL.Color4( 1f, 1f, 1f, 1f );
            GL.Enable( EnableCap.Texture2D );
            GL.Disable( EnableCap.Blend );
        }
    }
}
