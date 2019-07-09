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
using SharpQuake.Renderer.Textures;

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
               
        private MemorySurface _WarpFace; // used by SubdivideSurface()

        private BaseTexture SolidSkyTexture
        {
            get;
            set;
        }

        private BaseTexture AlphaSkyTexture
        {
            get;
            set;
        }

        /// <summary>
        /// R_InitSky
        /// called at level load
        /// A sky texture is 256*128, with the right side being a masked overlay
        /// </summary>
        public void InitSky( ModelTexture mt )
        {
            var src = mt.pixels;
            var offset = mt.offsets[0];

            // make an average value for the back to avoid
            // a fringe on the top level
            const Int32 size = 128 * 128;
            var trans = new UInt32[size];
            var v8to24 = Host.Video.Table8to24;
            var r = 0;
            var g = 0;
            var b = 0;
            var rgba = Union4b.Empty;
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

            SolidSkyTexture = BaseTexture.FromBuffer( Host.Video.Device, "_SolidSkyTexture", trans, 128, 128, false, false, "GL_LINEAR" );

            for ( var i = 0; i < 128; i++ )
                for( var j = 0; j < 128; j++ )
                {
                    Int32 p = src[offset + i * 256 + j];
                    if( p == 0 )
                        trans[( i * 128 ) + j] = transpix;
                    else
                        trans[( i * 128 ) + j] = v8to24[p];
                }

            AlphaSkyTexture = BaseTexture.FromBuffer( Host.Video.Device, "_AlphaSkyTexture", trans, 128, 128, false, true, "GL_LINEAR" );
        }

        /// <summary>
        /// GL_SubdivideSurface
        /// Breaks a polygon up along axial 64 unit boundaries
        /// so that turbulent and sky warps can be done reasonably.
        /// </summary>
        public void SubdivideSurface( MemorySurface fa )
        {
            _WarpFace = fa;

            //
            // convert edges back to a normal polygon
            //
            var numverts = 0;
            var verts = new Vector3[fa.numedges + 1]; // + 1 for wrap case
            var loadmodel = Host.Model.Model;
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
        private void SubdividePolygon( Int32 numverts, Vector3[] verts )
        {
            if( numverts > 60 )
                Utilities.Error( "numverts = {0}", numverts );

            Vector3 mins, maxs;
            BoundPoly( numverts, verts, out mins, out maxs );

            var dist = new Single[64];
            for( var i = 0; i < 3; i++ )
            {
                var m = ( MathLib.Comp( ref mins, i ) + MathLib.Comp( ref maxs, i ) ) * 0.5;
                m = Host.Model.SubdivideSize * Math.Floor( m / Host.Model.SubdivideSize + 0.5 );
                if( MathLib.Comp( ref maxs, i ) - m < 8 )
                    continue;

                if( m - MathLib.Comp( ref mins, i ) < 8 )
                    continue;

                for( var j = 0; j < numverts; j++ )
                    dist[j] = ( Single ) ( MathLib.Comp( ref verts[j], i ) - m );

                var front = new Vector3[64];
                var back = new Vector3[64];

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

            var poly = new GLPoly();
            poly.next = _WarpFace.polys;
            _WarpFace.polys = poly;
            poly.AllocVerts( numverts );
            for( var i = 0; i < numverts; i++ )
            {
                Utilities.Copy( ref verts[i], poly.verts[i] );
                var s = Vector3.Dot( verts[i], _WarpFace.texinfo.vecs[0].Xyz );
                var t = Vector3.Dot( verts[i], _WarpFace.texinfo.vecs[1].Xyz );
                poly.verts[i][3] = s;
                poly.verts[i][4] = t;
            }
        }

        /// <summary>
        /// BoundPoly
        /// </summary>
        private void BoundPoly( Int32 numverts, Vector3[] verts, out Vector3 mins, out Vector3 maxs )
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
        private void EmitWaterPolys( MemorySurface fa )
        {
            Host.Video.Device.Graphics.EmitWaterPolys( ref _TurbSin, Host.RealTime, TURBSCALE, fa.polys );
        }

        /// <summary>
        /// R_DrawSkyChain
        /// </summary>
        private void DrawSkyChain( MemorySurface s )
        {
            Host.Video.Device.DisableMultitexture();

            SolidSkyTexture.Bind( );

            // used when gl_texsort is on
            _SpeedScale = ( Single ) Host.RealTime * 8;
            _SpeedScale -= ( Int32 ) _SpeedScale & ~127;

            for( var fa = s; fa != null; fa = fa.texturechain )
                Host.Video.Device.Graphics.EmitSkyPolys( fa.polys, Host.RenderContext.Origin, _SpeedScale );

            AlphaSkyTexture.Bind( );
            _SpeedScale = ( Single ) Host.RealTime * 16;
            _SpeedScale -= ( Int32 ) _SpeedScale & ~127;

            for( var fa = s; fa != null; fa = fa.texturechain )
                Host.Video.Device.Graphics.EmitSkyPolys( fa.polys, Host.RenderContext.Origin, _SpeedScale, true );
        }

        /// <summary>
        /// EmitBothSkyLayers
        /// Does a sky warp on the pre-fragmented glpoly_t chain
        /// This will be called for brushmodels, the world
        /// will have them chained together.
        /// </summary>
        private void EmitBothSkyLayers( MemorySurface fa )
        {
            Host.Video.Device.DisableMultitexture();

            SolidSkyTexture.Bind( );
            _SpeedScale = ( Single ) Host.RealTime * 8;
            _SpeedScale -= ( Int32 ) _SpeedScale & ~127;

            Host.Video.Device.Graphics.EmitSkyPolys( fa.polys, Host.RenderContext.Origin, _SpeedScale );

            AlphaSkyTexture.Bind( );
            _SpeedScale = ( Single ) Host.RealTime * 16;
            _SpeedScale -= ( Int32 ) _SpeedScale & ~127;

            Host.Video.Device.Graphics.EmitSkyPolys( fa.polys, Host.RenderContext.Origin, _SpeedScale, true );
        }
    }
}
