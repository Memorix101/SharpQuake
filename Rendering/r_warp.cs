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
using OpenTK;
using OpenTK.Graphics.OpenGL;
using SharpQuake.Framework;

// gl_warp.c

namespace SharpQuake
{
    partial class render
    {
        private const Double TURBSCALE = ( 256.0 / ( 2 * Math.PI ) );

        // turbsin
        private static Single[] _TurbSin = new Single[]
        {
            0f, 0.19633f, 0.392541f, 0.588517f, 0.784137f, 0.979285f, 1.17384f, 1.3677f,
            1.56072f, 1.75281f, 1.94384f, 2.1337f, 2.32228f, 2.50945f, 2.69512f, 2.87916f,
            3.06147f, 3.24193f, 3.42044f, 3.59689f, 3.77117f, 3.94319f, 4.11282f, 4.27998f,
            4.44456f, 4.60647f, 4.76559f, 4.92185f, 5.07515f, 5.22538f, 5.37247f, 5.51632f,
            5.65685f, 5.79398f, 5.92761f, 6.05767f, 6.18408f, 6.30677f, 6.42566f, 6.54068f,
            6.65176f, 6.75883f, 6.86183f, 6.9607f, 7.05537f, 7.14579f, 7.23191f, 7.31368f,
            7.39104f, 7.46394f, 7.53235f, 7.59623f, 7.65552f, 7.71021f, 7.76025f, 7.80562f,
            7.84628f, 7.88222f, 7.91341f, 7.93984f, 7.96148f, 7.97832f, 7.99036f, 7.99759f,
            8f, 7.99759f, 7.99036f, 7.97832f, 7.96148f, 7.93984f, 7.91341f, 7.88222f,
            7.84628f, 7.80562f, 7.76025f, 7.71021f, 7.65552f, 7.59623f, 7.53235f, 7.46394f,
            7.39104f, 7.31368f, 7.23191f, 7.14579f, 7.05537f, 6.9607f, 6.86183f, 6.75883f,
            6.65176f, 6.54068f, 6.42566f, 6.30677f, 6.18408f, 6.05767f, 5.92761f, 5.79398f,
            5.65685f, 5.51632f, 5.37247f, 5.22538f, 5.07515f, 4.92185f, 4.76559f, 4.60647f,
            4.44456f, 4.27998f, 4.11282f, 3.94319f, 3.77117f, 3.59689f, 3.42044f, 3.24193f,
            3.06147f, 2.87916f, 2.69512f, 2.50945f, 2.32228f, 2.1337f, 1.94384f, 1.75281f,
            1.56072f, 1.3677f, 1.17384f, 0.979285f, 0.784137f, 0.588517f, 0.392541f, 0.19633f,
            9.79717e-16f, -0.19633f, -0.392541f, -0.588517f, -0.784137f, -0.979285f, -1.17384f, -1.3677f,
            -1.56072f, -1.75281f, -1.94384f, -2.1337f, -2.32228f, -2.50945f, -2.69512f, -2.87916f,
            -3.06147f, -3.24193f, -3.42044f, -3.59689f, -3.77117f, -3.94319f, -4.11282f, -4.27998f,
            -4.44456f, -4.60647f, -4.76559f, -4.92185f, -5.07515f, -5.22538f, -5.37247f, -5.51632f,
            -5.65685f, -5.79398f, -5.92761f, -6.05767f, -6.18408f, -6.30677f, -6.42566f, -6.54068f,
            -6.65176f, -6.75883f, -6.86183f, -6.9607f, -7.05537f, -7.14579f, -7.23191f, -7.31368f,
            -7.39104f, -7.46394f, -7.53235f, -7.59623f, -7.65552f, -7.71021f, -7.76025f, -7.80562f,
            -7.84628f, -7.88222f, -7.91341f, -7.93984f, -7.96148f, -7.97832f, -7.99036f, -7.99759f,
            -8f, -7.99759f, -7.99036f, -7.97832f, -7.96148f, -7.93984f, -7.91341f, -7.88222f,
            -7.84628f, -7.80562f, -7.76025f, -7.71021f, -7.65552f, -7.59623f, -7.53235f, -7.46394f,
            -7.39104f, -7.31368f, -7.23191f, -7.14579f, -7.05537f, -6.9607f, -6.86183f, -6.75883f,
            -6.65176f, -6.54068f, -6.42566f, -6.30677f, -6.18408f, -6.05767f, -5.92761f, -5.79398f,
            -5.65685f, -5.51632f, -5.37247f, -5.22538f, -5.07515f, -4.92185f, -4.76559f, -4.60647f,
            -4.44456f, -4.27998f, -4.11282f, -3.94319f, -3.77117f, -3.59689f, -3.42044f, -3.24193f,
            -3.06147f, -2.87916f, -2.69512f, -2.50945f, -2.32228f, -2.1337f, -1.94384f, -1.75281f,
            -1.56072f, -1.3677f, -1.17384f, -0.979285f, -0.784137f, -0.588517f, -0.392541f, -0.19633f
        };

        private static Int32 _SolidSkyTexture; // solidskytexture
        private static Int32 _AlphaSkyTexture; // alphaskytexture

        private static msurface_t _WarpFace; // used by SubdivideSurface()

        /// <summary>
        /// R_InitSky
        /// called at level load
        /// A sky texture is 256*128, with the right side being a masked overlay
        /// </summary>
        public static void InitSky( texture_t mt )
        {
            Byte[] src = mt.pixels;
            var offset = mt.offsets[0];

            // make an average value for the back to avoid
            // a fringe on the top level
            const Int32 size = 128 * 128;
            UInt32[] trans = new UInt32[size];
            UInt32[] v8to24 = vid.Table8to24;
            var r = 0;
            var g = 0;
            var b = 0;
            Union4b rgba = Union4b.Empty;
            for( var i = 0; i < 128; i++ )
                for( var j = 0; j < 128; j++ )
                {
                    Int32 p = src[offset + i * 256 + j + 128];
                    rgba.ui0 = v8to24[p];
                    trans[( i * 128 ) + j] = rgba.ui0;
                    r += rgba.b0;
                    g += rgba.b1;
                    b += rgba.b2;
                }

            rgba.b0 = ( Byte ) ( r / size );
            rgba.b1 = ( Byte ) ( g / size );
            rgba.b2 = ( Byte ) ( b / size );
            rgba.b3 = 0;

            var transpix = rgba.ui0;

            if( _SolidSkyTexture == 0 )
                _SolidSkyTexture = Drawer.GenerateTextureNumber();
            Drawer.Bind( _SolidSkyTexture );
            GL.TexImage2D( TextureTarget.Texture2D, 0, Drawer.SolidFormat, 128, 128, 0, PixelFormat.Rgba, PixelType.UnsignedByte, trans );
            Drawer.SetTextureFilters( TextureMinFilter.Linear, TextureMagFilter.Linear );

            for( var i = 0; i < 128; i++ )
                for( var j = 0; j < 128; j++ )
                {
                    Int32 p = src[offset + i * 256 + j];
                    if( p == 0 )
                        trans[( i * 128 ) + j] = transpix;
                    else
                        trans[( i * 128 ) + j] = v8to24[p];
                }

            if( _AlphaSkyTexture == 0 )
                _AlphaSkyTexture = Drawer.GenerateTextureNumber();
            Drawer.Bind( _AlphaSkyTexture );
            GL.TexImage2D( TextureTarget.Texture2D, 0, Drawer.AlphaFormat, 128, 128, 0, PixelFormat.Rgba, PixelType.UnsignedByte, trans );
            Drawer.SetTextureFilters( TextureMinFilter.Linear, TextureMagFilter.Linear );
        }

        /// <summary>
        /// GL_SubdivideSurface
        /// Breaks a polygon up along axial 64 unit boundaries
        /// so that turbulent and sky warps can be done reasonably.
        /// </summary>
        public static void SubdivideSurface( msurface_t fa )
        {
            _WarpFace = fa;

            //
            // convert edges back to a normal polygon
            //
            var numverts = 0;
            Vector3[] verts = new Vector3[fa.numedges + 1]; // + 1 for wrap case
            model_t loadmodel = Mod.Model;
            for( var i = 0; i < fa.numedges; i++ )
            {
                var lindex = loadmodel.surfedges[fa.firstedge + i];

                if( lindex > 0 )
                    verts[numverts] = loadmodel.vertexes[loadmodel.edges[lindex].v[0]].position;
                else
                    verts[numverts] = loadmodel.vertexes[loadmodel.edges[-lindex].v[1]].position;

                numverts++;
            }

            SubdividePolygon( numverts, verts );
        }

        /// <summary>
        /// SubdividePolygon
        /// </summary>
        private static void SubdividePolygon( Int32 numverts, Vector3[] verts )
        {
            if( numverts > 60 )
                Utilities.Error( "numverts = {0}", numverts );

            Vector3 mins, maxs;
            BoundPoly( numverts, verts, out mins, out maxs );

            Single[] dist = new Single[64];
            for( var i = 0; i < 3; i++ )
            {
                var m = ( MathLib.Comp( ref mins, i ) + MathLib.Comp( ref maxs, i ) ) * 0.5;
                m = Mod.SubdivideSize * Math.Floor( m / Mod.SubdivideSize + 0.5 );
                if( MathLib.Comp( ref maxs, i ) - m < 8 )
                    continue;

                if( m - MathLib.Comp( ref mins, i ) < 8 )
                    continue;

                for( var j = 0; j < numverts; j++ )
                    dist[j] = ( Single ) ( MathLib.Comp( ref verts[j], i ) - m );

                Vector3[] front = new Vector3[64];
                Vector3[] back = new Vector3[64];

                // cut it

                // wrap cases
                dist[numverts] = dist[0];
                verts[numverts] = verts[0]; // Uze: source array must be at least numverts + 1 elements long

                Int32 f = 0, b = 0;
                for( var j = 0; j < numverts; j++ )
                {
                    if( dist[j] >= 0 )
                    {
                        front[f] = verts[j];
                        f++;
                    }
                    if( dist[j] <= 0 )
                    {
                        back[b] = verts[j];
                        b++;
                    }
                    if( dist[j] == 0 || dist[j + 1] == 0 )
                        continue;
                    if( ( dist[j] > 0 ) != ( dist[j + 1] > 0 ) )
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

            glpoly_t poly = new glpoly_t();
            poly.next = _WarpFace.polys;
            _WarpFace.polys = poly;
            poly.AllocVerts( numverts );
            for( var i = 0; i < numverts; i++ )
            {
                Common.Copy( ref verts[i], poly.verts[i] );
                var s = Vector3.Dot( verts[i], _WarpFace.texinfo.vecs[0].Xyz );
                var t = Vector3.Dot( verts[i], _WarpFace.texinfo.vecs[1].Xyz );
                poly.verts[i][3] = s;
                poly.verts[i][4] = t;
            }
        }

        /// <summary>
        /// BoundPoly
        /// </summary>
        private static void BoundPoly( Int32 numverts, Vector3[] verts, out Vector3 mins, out Vector3 maxs )
        {
            mins = Vector3.One * 9999;
            maxs = Vector3.One * -9999;
            for( var i = 0; i < numverts; i++ )
            {
                Vector3.ComponentMin( ref verts[i], ref mins, out mins );
                Vector3.ComponentMax( ref verts[i], ref maxs, out maxs );
            }
        }

        /// <summary>
        /// EmitWaterPolys
        /// Does a water warp on the pre-fragmented glpoly_t chain
        /// </summary>
        private static void EmitWaterPolys( msurface_t fa )
        {
            for( glpoly_t p = fa.polys; p != null; p = p.next )
            {
                GL.Begin( PrimitiveType.Polygon );
                for( var i = 0; i < p.numverts; i++ )
                {
                    Single[] v = p.verts[i];
                    var os = v[3];
                    var ot = v[4];

                    var s = os + _TurbSin[( Int32 ) ( ( ot * 0.125 + host.RealTime ) * TURBSCALE ) & 255];
                    s *= ( 1.0f / 64 );

                    var t = ot + _TurbSin[( Int32 ) ( ( os * 0.125 + host.RealTime ) * TURBSCALE ) & 255];
                    t *= ( 1.0f / 64 );

                    GL.TexCoord2( s, t );
                    GL.Vertex3( v );
                }
                GL.End();
            }
        }

        /// <summary>
        /// EmitSkyPolys
        /// </summary>
        private static void EmitSkyPolys( msurface_t fa )
        {
            for( glpoly_t p = fa.polys; p != null; p = p.next )
            {
                GL.Begin( PrimitiveType.Polygon );
                for( var i = 0; i < p.numverts; i++ )
                {
                    Single[] v = p.verts[i];
                    Vector3 dir = new Vector3( v[0] - render.Origin.X, v[1] - render.Origin.Y, v[2] - render.Origin.Z );
                    dir.Z *= 3; // flatten the sphere

                    dir.Normalize();
                    dir *= 6 * 63;

                    var s = ( _SpeedScale + dir.X ) / 128.0f;
                    var t = ( _SpeedScale + dir.Y ) / 128.0f;

                    GL.TexCoord2( s, t );
                    GL.Vertex3( v );
                }
                GL.End();
            }
        }

        /// <summary>
        /// R_DrawSkyChain
        /// </summary>
        private static void DrawSkyChain( msurface_t s )
        {
            DisableMultitexture();

            // used when gl_texsort is on
            Drawer.Bind( _SolidSkyTexture );
            _SpeedScale = ( Single ) host.RealTime * 8;
            _SpeedScale -= ( Int32 ) _SpeedScale & ~127;

            for( msurface_t fa = s; fa != null; fa = fa.texturechain )
                EmitSkyPolys( fa );

            GL.Enable( EnableCap.Blend );
            Drawer.Bind( _AlphaSkyTexture );
            _SpeedScale = ( Single ) host.RealTime * 16;
            _SpeedScale -= ( Int32 ) _SpeedScale & ~127;

            for( msurface_t fa = s; fa != null; fa = fa.texturechain )
                EmitSkyPolys( fa );

            GL.Disable( EnableCap.Blend );
        }

        /// <summary>
        /// EmitBothSkyLayers
        /// Does a sky warp on the pre-fragmented glpoly_t chain
        /// This will be called for brushmodels, the world
        /// will have them chained together.
        /// </summary>
        private static void EmitBothSkyLayers( msurface_t fa )
        {
            DisableMultitexture();

            Drawer.Bind( _SolidSkyTexture );
            _SpeedScale = ( Single ) host.RealTime * 8;
            _SpeedScale -= ( Int32 ) _SpeedScale & ~127;

            EmitSkyPolys( fa );

            GL.Enable( EnableCap.Blend );
            Drawer.Bind( _AlphaSkyTexture );
            _SpeedScale = ( Single ) host.RealTime * 16;
            _SpeedScale -= ( Int32 ) _SpeedScale & ~127;

            EmitSkyPolys( fa );

            GL.Disable( EnableCap.Blend );
        }
    }
}
