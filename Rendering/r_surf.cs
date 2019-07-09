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
using System.Runtime.InteropServices;
using OpenTK.Graphics.OpenGL;
using SharpQuake.Framework;
using SharpQuake.Framework.Mathematics;
using SharpQuake.Game.Rendering.Memory;
using SharpQuake.Game.Rendering.Models;
using SharpQuake.Game.Rendering.Textures;
using SharpQuake.Game.World;
using SharpQuake.Renderer;
using SharpQuake.Renderer.OpenGL.Textures;
using SharpQuake.Renderer.Textures;

// gl_rsurf.c

namespace SharpQuake
{
    partial class render
    {
        private const Double COLINEAR_EPSILON = 0.001;

        //private Int32 _LightMapTextures; // lightmap_textures
        private Int32 _LightMapBytes; // lightmap_bytes		// 1, 2, or 4
        private MemoryVertex[] _CurrentVertBase; // r_pcurrentvertbase
        private Model _CurrentModel; // currentmodel
        //private System.Boolean[] _LightMapModified = new System.Boolean[RenderDef.MAX_LIGHTMAPS]; // lightmap_modified
        private GLPoly[] _LightMapPolys = new GLPoly[RenderDef.MAX_LIGHTMAPS]; // lightmap_polys
        //private glRect_t[] _LightMapRectChange = new glRect_t[RenderDef.MAX_LIGHTMAPS]; // lightmap_rectchange
        private UInt32[] _BlockLights = new UInt32[18 * 18]; // blocklights
        private Int32 _ColinElim; // nColinElim
        private MemorySurface _SkyChain; // skychain
        private MemorySurface _WaterChain; // waterchain
        private Entity _TempEnt = new Entity(); // for DrawWorld

        // the lightmap texture data needs to be kept in
        // main memory so texsubimage can update properly
        private Byte[] _LightMaps = new Byte[4 * RenderDef.MAX_LIGHTMAPS * RenderDef.BLOCK_WIDTH * RenderDef.BLOCK_HEIGHT]; // lightmaps

        private BaseTexture LightMapTexture
        {
            get;
            set;
        }

        /// <summary>
        /// GL_BuildLightmaps
        /// Builds the lightmap texture with all the surfaces from all brush models
        /// </summary>
        private void BuildLightMaps()
        {
            if ( LightMapTexture != null )
                Array.Clear( LightMapTexture.LightMapData, 0, LightMapTexture.LightMapData.Length );
            //memset (allocated, 0, sizeof(allocated));

            _FrameCount = 1;		// no dlightcache

            //if( _LightMapTextures == 0 )
            //   _LightMapTextures = Host.DrawingContext.GenerateTextureNumberRange( RenderDef.MAX_LIGHTMAPS );

            Host.DrawingContext.LightMapFormat = "GL_LUMINANCE";// GL_LUMINANCE;

            // default differently on the Permedia
            if ( Host.Screen.IsPermedia )
                Host.DrawingContext.LightMapFormat = "GL_RGBA";

            if( CommandLine.HasParam( "-lm_1" ) )
                Host.DrawingContext.LightMapFormat = "GL_LUMINANCE";

            if( CommandLine.HasParam( "-lm_a" ) )
                Host.DrawingContext.LightMapFormat = "GL_ALPHA";

            //if (CommandLine.HasParam("-lm_i"))
            //    Host.DrawingContext.LightMapFormat = PixelFormat.Intensity;

            //if (CommandLine.HasParam("-lm_2"))
            //    Host.DrawingContext.LightMapFormat = PixelFormat.Rgba4;

            if( CommandLine.HasParam( "-lm_4" ) )
                Host.DrawingContext.LightMapFormat = "GL_RGBA";

            switch( Host.DrawingContext.LightMapFormat )
            {
                case "GL_RGBA":
                    _LightMapBytes = 4;
                    break;

                //case PixelFormat.Rgba4:
                //_LightMapBytes = 2;
                //break;

                case "GL_LUMINANCE":
                //case PixelFormat.Intensity:
                case "GL_ALPHA":
                    _LightMapBytes = 1;
                    break;
            }

            var tempBuffer = new Int32[RenderDef.MAX_LIGHTMAPS, RenderDef.BLOCK_WIDTH];

            for ( var j = 1; j < QDef.MAX_MODELS; j++ )
            {
                var m = Host.Client.cl.model_precache[j];
                if( m == null )
                    break;

                if( m.name != null && m.name.StartsWith( "*" ) )
                    continue;

                _CurrentVertBase = m.vertexes;
                _CurrentModel = m;
                for( var i = 0; i < m.numsurfaces; i++ )
                {
                    CreateSurfaceLightmap( ref tempBuffer, m.surfaces[i] );
                    if( ( m.surfaces[i].flags & SurfaceDef.SURF_DRAWTURB ) != 0 )
                        continue;

                    if( ( m.surfaces[i].flags & SurfaceDef.SURF_DRAWSKY ) != 0 )
                        continue;

                    BuildSurfaceDisplayList( m.surfaces[i] );
                }
            }

            if( _glTexSort.Value == 0 )
                Host.DrawingContext.SelectTexture( MTexTarget.TEXTURE1_SGIS );

            LightMapTexture = BaseTexture.FromBuffer( Host.Video.Device, "_Lightmaps", new ByteArraySegment( _LightMaps ), 128, 128, false, false, isLightMap: true );

            LightMapTexture.Desc.LightMapBytes = _LightMapBytes;
            LightMapTexture.Desc.LightMapFormat = Host.DrawingContext.LightMapFormat;

            Array.Copy( tempBuffer, LightMapTexture.LightMapData, tempBuffer.Length );

            LightMapTexture.UploadLightmap( );

            if ( _glTexSort.Value == 0 )
                Host.DrawingContext.SelectTexture( MTexTarget.TEXTURE0_SGIS );
        }

        /// <summary>
        /// GL_CreateSurfaceLightmap
        /// </summary>
        private void CreateSurfaceLightmap( ref Int32[,] tempBuffer, MemorySurface surf )
        {
            if( ( surf.flags & ( SurfaceDef.SURF_DRAWSKY | SurfaceDef.SURF_DRAWTURB ) ) != 0 )
                return;

            var smax = ( surf.extents[0] >> 4 ) + 1;
            var tmax = ( surf.extents[1] >> 4 ) + 1;

            surf.lightmaptexturenum = AllocBlock( ref tempBuffer, smax, tmax, ref surf.light_s, ref surf.light_t );
            var offset = surf.lightmaptexturenum * _LightMapBytes * RenderDef.BLOCK_WIDTH * RenderDef.BLOCK_HEIGHT;
            offset += ( surf.light_t * RenderDef.BLOCK_WIDTH + surf.light_s ) * _LightMapBytes;
            BuildLightMap( surf, new ByteArraySegment( _LightMaps, offset ), RenderDef.BLOCK_WIDTH * _LightMapBytes );
        }

        /// <summary>
        /// BuildSurfaceDisplayList
        /// </summary>
        private void BuildSurfaceDisplayList( MemorySurface fa )
        {
            // reconstruct the polygon
            var pedges = _CurrentModel.edges;
            var lnumverts = fa.numedges;

            //
            // draw texture
            //
            var poly = new GLPoly();
            poly.AllocVerts( lnumverts );
            poly.next = fa.polys;
            poly.flags = fa.flags;
            fa.polys = poly;

            UInt16[] r_pedge_v;
            Vector3 vec;

            for( var i = 0; i < lnumverts; i++ )
            {
                var lindex = _CurrentModel.surfedges[fa.firstedge + i];
                if( lindex > 0 )
                {
                    r_pedge_v = pedges[lindex].v;
                    vec = _CurrentVertBase[r_pedge_v[0]].position;
                }
                else
                {
                    r_pedge_v = pedges[-lindex].v;
                    vec = _CurrentVertBase[r_pedge_v[1]].position;
                }
                var s = MathLib.DotProduct( ref vec, ref fa.texinfo.vecs[0] ) + fa.texinfo.vecs[0].W;
                s /= fa.texinfo.texture.width;

                var t = MathLib.DotProduct( ref vec, ref fa.texinfo.vecs[1] ) + fa.texinfo.vecs[1].W;
                t /= fa.texinfo.texture.height;

                poly.verts[i][0] = vec.X;
                poly.verts[i][1] = vec.Y;
                poly.verts[i][2] = vec.Z;
                poly.verts[i][3] = s;
                poly.verts[i][4] = t;

                //
                // lightmap texture coordinates
                //
                s = MathLib.DotProduct( ref vec, ref fa.texinfo.vecs[0] ) + fa.texinfo.vecs[0].W;
                s -= fa.texturemins[0];
                s += fa.light_s * 16;
                s += 8;
                s /= RenderDef.BLOCK_WIDTH * 16;

                t = MathLib.DotProduct( ref vec, ref fa.texinfo.vecs[1] ) + fa.texinfo.vecs[1].W;
                t -= fa.texturemins[1];
                t += fa.light_t * 16;
                t += 8;
                t /= RenderDef.BLOCK_HEIGHT * 16;

                poly.verts[i][5] = s;
                poly.verts[i][6] = t;
            }

            //
            // remove co-linear points - Ed
            //
            if( _glKeepTJunctions.Value == 0 && ( fa.flags & SurfaceDef.SURF_UNDERWATER ) == 0 )
            {
                for( var i = 0; i < lnumverts; ++i )
                {
                    if( IsCollinear( poly.verts[( i + lnumverts - 1 ) % lnumverts],
                        poly.verts[i],
                        poly.verts[( i + 1 ) % lnumverts] ) )
                    {
                        Int32 j;
                        for( j = i + 1; j < lnumverts; ++j )
                        {
                            //int k;
                            for( var k = 0; k < ModelDef.VERTEXSIZE; ++k )
                                poly.verts[j - 1][k] = poly.verts[j][k];
                        }
                        --lnumverts;
                        ++_ColinElim;
                        // retry next vertex next time, which is now current vertex
                        --i;
                    }
                }
            }
            poly.numverts = lnumverts;
        }

        private System.Boolean IsCollinear( Single[] prev, Single[] cur, Single[] next )
        {
            var v1 = new Vector3( cur[0] - prev[0], cur[1] - prev[1], cur[2] - prev[2] );
            MathLib.Normalize( ref v1 );
            var v2 = new Vector3( next[0] - prev[0], next[1] - prev[1], next[2] - prev[2] );
            MathLib.Normalize( ref v2 );
            v1 -= v2;
            return ( ( Math.Abs( v1.X ) <= COLINEAR_EPSILON ) &&
                ( Math.Abs( v1.Y ) <= COLINEAR_EPSILON ) &&
                ( Math.Abs( v1.Z ) <= COLINEAR_EPSILON ) );
        }

        // returns a texture number and the position inside it
        private Int32 AllocBlock( ref Int32[,] data, Int32 w, Int32 h, ref Int32 x, ref Int32 y )
        {
            for( var texnum = 0; texnum < RenderDef.MAX_LIGHTMAPS; texnum++ )
            {
                var best = RenderDef.BLOCK_HEIGHT;

                for( var i = 0; i < RenderDef.BLOCK_WIDTH - w; i++ )
                {
                    Int32 j = 0, best2 = 0;

                    for ( j = 0; j < w; j++ )
                    {
                        if ( data[texnum, i + j] >= best )
                            break;
                        if ( data[texnum, i + j] > best2 )
                            best2 = data[texnum, i + j];
                    }

                    if( j == w )
                    {
                        // this is a valid spot
                        x = i;
                        y = best = best2;
                    }
                }

                if( best + h > RenderDef.BLOCK_HEIGHT )
                    continue;

                for( var i = 0; i < w; i++ )
                    data[texnum, x + i] = best + h;

                return texnum;
            }

            Utilities.Error( "AllocBlock: full" );
            return 0; // shut up compiler
        }

        /// <summary>
        /// R_BuildLightMap
        /// Combine and scale multiple lightmaps into the 8.8 format in blocklights
        /// </summary>
        private void BuildLightMap( MemorySurface surf, ByteArraySegment dest, Int32 stride )
        {
            surf.cached_dlight = ( surf.dlightframe == _FrameCount );

            var smax = ( surf.extents[0] >> 4 ) + 1;
            var tmax = ( surf.extents[1] >> 4 ) + 1;
            var size = smax * tmax;

            var srcOffset = surf.sampleofs;
            var lightmap = surf.sample_base;// surf.samples;

            // set to full bright if no light data
            if( _FullBright.Value != 0 || Host.Client.cl.worldmodel.lightdata == null )
            {
                for( var i = 0; i < size; i++ )
                    _BlockLights[i] = 255 * 256;
            }
            else
            {
                // clear to no light
                for( var i = 0; i < size; i++ )
                    _BlockLights[i] = 0;

                // add all the lightmaps
                if( lightmap != null )
                    for( var maps = 0; maps < BspDef.MAXLIGHTMAPS && surf.styles[maps] != 255; maps++ )
                    {
                        var scale = _LightStyleValue[surf.styles[maps]];
                        surf.cached_light[maps] = scale;	// 8.8 fraction
                        for( var i = 0; i < size; i++ )
                            _BlockLights[i] += ( UInt32 ) ( lightmap[srcOffset + i] * scale );
                        srcOffset += size; // lightmap += size;	// skip to next lightmap
                    }

                // add all the dynamic lights
                if( surf.dlightframe == _FrameCount )
                    AddDynamicLights( surf );
            }
            // bound, invert, and shift
            //store:
            var blOffset = 0;
            var destOffset = dest.StartIndex;
            var data = dest.Data;
            switch( Host.DrawingContext.LightMapFormat )
            {
                case "GL_RGBA":
                    stride -= ( smax << 2 );
                    for( var i = 0; i < tmax; i++, destOffset += stride ) // dest += stride
                    {
                        for( var j = 0; j < smax; j++ )
                        {
                            var t = _BlockLights[blOffset++];// *bl++;
                            t >>= 7;
                            if( t > 255 )
                                t = 255;
                            data[destOffset + 3] = ( Byte ) ( 255 - t ); //dest[3] = 255 - t;
                            destOffset += 4;
                        }
                    }
                    break;

                case "GL_ALPHA":
                case "GL_LUMINANCE":
                    //case GL_INTENSITY:
                    for( var i = 0; i < tmax; i++, destOffset += stride )
                    {
                        for( var j = 0; j < smax; j++ )
                        {
                            var t = _BlockLights[blOffset++];// *bl++;
                            t >>= 7;
                            if( t > 255 )
                                t = 255;
                            data[destOffset + j] = ( Byte ) ( 255 - t ); // dest[j] = 255 - t;
                        }
                    }
                    break;

                default:
                    Utilities.Error( "Bad lightmap format" );
                    break;
            }
        }

        /// <summary>
        /// R_AddDynamicLights
        /// </summary>
        private void AddDynamicLights( MemorySurface surf )
        {
            var smax = ( surf.extents[0] >> 4 ) + 1;
            var tmax = ( surf.extents[1] >> 4 ) + 1;
            var tex = surf.texinfo;
            var dlights = Host.Client.DLights;

            for( var lnum = 0; lnum < ClientDef.MAX_DLIGHTS; lnum++ )
            {
                if( ( surf.dlightbits & ( 1 << lnum ) ) == 0 )
                    continue;		// not lit by this light

                var rad = dlights[lnum].radius;
                var dist = Vector3.Dot( dlights[lnum].origin, surf.plane.normal ) - surf.plane.dist;
                rad -= Math.Abs( dist );
                var minlight = dlights[lnum].minlight;
                if( rad < minlight )
                    continue;
                minlight = rad - minlight;

                var impact = dlights[lnum].origin - surf.plane.normal * dist;

                var local0 = Vector3.Dot( impact, tex.vecs[0].Xyz ) + tex.vecs[0].W;
                var local1 = Vector3.Dot( impact, tex.vecs[1].Xyz ) + tex.vecs[1].W;

                local0 -= surf.texturemins[0];
                local1 -= surf.texturemins[1];

                for( var t = 0; t < tmax; t++ )
                {
                    var td = ( Int32 ) ( local1 - t * 16 );
                    if( td < 0 )
                        td = -td;
                    for( var s = 0; s < smax; s++ )
                    {
                        var sd = ( Int32 ) ( local0 - s * 16 );
                        if( sd < 0 )
                            sd = -sd;
                        if( sd > td )
                            dist = sd + ( td >> 1 );
                        else
                            dist = td + ( sd >> 1 );
                        if( dist < minlight )
                            _BlockLights[t * smax + s] += ( UInt32 ) ( ( rad - dist ) * 256 );
                    }
                }
            }
        }

        /// <summary>
        /// R_DrawWaterSurfaces
        /// </summary>
        private void DrawWaterSurfaces()
        {
            if( _WaterAlpha.Value == 1.0f && _glTexSort.Value != 0 )
                return;

            //
            // go back to the world matrix
            //
            GL.LoadMatrix( ref _WorldMatrix );

            if ( _WaterAlpha.Value < 1.0 )
            {
                GL.Enable( EnableCap.Blend );
                GL.Color4( 1, 1, 1, _WaterAlpha.Value );
                GL.TexEnv( TextureEnvTarget.TextureEnv, TextureEnvParameter.TextureEnvMode, ( Int32 ) TextureEnvMode.Modulate );
            }

            if( _glTexSort.Value == 0 )
            {
                if( _WaterChain == null )
                    return;

                for( var s = _WaterChain; s != null; s = s.texturechain )
                {
                    s.texinfo.texture.texture.Bind( );
                    EmitWaterPolys( s );
                }
                _WaterChain = null;
            }
            else
            {
                for( var i = 0; i < Host.Client.cl.worldmodel.numtextures; i++ )
                {
                    var t = Host.Client.cl.worldmodel.textures[i];
                    if( t == null )
                        continue;

                    var s = t.texturechain;
                    if( s == null )
                        continue;

                    if( ( s.flags & SurfaceDef.SURF_DRAWTURB ) == 0 )
                        continue;

                    // set modulate mode explicitly

                    t.texture.Bind( );

                    for( ; s != null; s = s.texturechain )
                        EmitWaterPolys( s );

                    t.texturechain = null;
                }
            }

            if( _WaterAlpha.Value < 1.0 )
            {
                GL.TexEnv( TextureEnvTarget.TextureEnv, TextureEnvParameter.TextureEnvMode, ( Int32 ) TextureEnvMode.Replace );
                GL.Color4( 1f, 1, 1, 1 );
                GL.Disable( EnableCap.Blend );
            }
        }

        /// <summary>
        /// R_MarkLeaves
        /// </summary>
        private void MarkLeaves()
        {
            if( _OldViewLeaf == _ViewLeaf && _NoVis.Value == 0 )
                return;

            //if( _IsMirror )
            //  return;

            _VisFrameCount++;
            _OldViewLeaf = _ViewLeaf;

            Byte[] vis;
            if( _NoVis.Value != 0 )
            {
                vis = new Byte[4096];
                Utilities.FillArray<Byte>( vis, 0xff ); // todo: add count parameter?
                //memset(solid, 0xff, (cl.worldmodel->numleafs + 7) >> 3);
            }
            else
                vis = Host.Model.LeafPVS( _ViewLeaf, Host.Client.cl.worldmodel );

            var world = Host.Client.cl.worldmodel;
            for( var i = 0; i < world.numleafs; i++ )
            {
                if( vis[i >> 3] != 0 & ( 1 << ( i & 7 ) ) != 0 )
                {
                    MemoryNodeBase node = world.leafs[i + 1];
                    do
                    {
                        if( node.visframe == _VisFrameCount )
                            break;
                        node.visframe = _VisFrameCount;
                        node = node.parent;
                    } while( node != null );
                }
            }
        }

        /// <summary>
        /// R_DrawWorld
        /// </summary>
        private void DrawWorld()
        {
            _TempEnt.Clear();
            _TempEnt.model = Host.Client.cl.worldmodel;

            _ModelOrg = _RefDef.vieworg;
            _CurrentEntity = _TempEnt;
            Host.DrawingContext.CurrentTexture = -1;

            GL.Color3( 1f, 1, 1 );

            Array.Clear( _LightMapPolys, 0, _LightMapPolys.Length );

            RecursiveWorldNode( _TempEnt.model.nodes[0] );

            DrawTextureChains();

            BlendLightmaps();
        }

        /// <summary>
        /// R_BlendLightmaps
        /// </summary>
        private void BlendLightmaps()
        {
            if( _FullBright.Value != 0 )
                return;
            if( _glTexSort.Value == 0 )
                return;

            Host.Video.Device.SetZWrite( false ); // don't bother writing Z

            if( Host.DrawingContext.LightMapFormat == "GL_LUMINANCE" )
                GL.BlendFunc( BlendingFactor.Zero, BlendingFactor.OneMinusSrcColor );
            //else if (gl_lightmap_format == GL_INTENSITY)
            //{
            //    glTexEnvf(GL_TEXTURE_ENV, GL_TEXTURE_ENV_MODE, GL_MODULATE);
            //    glColor4f(0, 0, 0, 1);
            //    glBlendFunc(GL_SRC_ALPHA, GL_ONE_MINUS_SRC_ALPHA);
            //}

            if( _LightMap.Value == 0 )
            {
                GL.Enable( EnableCap.Blend );
            }
            
            for ( var i = 0; i < RenderDef.MAX_LIGHTMAPS; i++ )
            {
                var p = _LightMapPolys[i];
                if( p == null )
                    continue;

                LightMapTexture.BindLightmap( ( ( GLTextureDesc ) LightMapTexture.Desc ).TextureNumber + i );

                if( LightMapTexture.LightMapModified[i] )
                    CommitLightmap( i );

                for( ; p != null; p = p.chain )
                {
                    if ( ( p.flags & SurfaceDef.SURF_UNDERWATER ) != 0 )
                        Host.Video.Device.Graphics.DrawWaterPolyLightmap( p, Host.RealTime );
                    else
                        Host.Video.Device.Graphics.DrawPoly( p, isLightmap: true );
                }
            }

            GL.Disable( EnableCap.Blend );
            if( Host.DrawingContext.LightMapFormat == "GL_LUMINANCE" )
                GL.BlendFunc( BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha );
            //else if (gl_lightmap_format == GL_INTENSITY)
            //{
            //    glTexEnvf(GL_TEXTURE_ENV, GL_TEXTURE_ENV_MODE, GL_REPLACE);
            //    glColor4f(1, 1, 1, 1);
            //}

            Host.Video.Device.SetZWrite( true ); // back to normal Z buffering
        }

        private void DrawTextureChains()
        {
            if( _glTexSort.Value == 0 )
            {
                Host.Video.Device.DisableMultitexture();

                if( _SkyChain != null )
                {
                    DrawSkyChain( _SkyChain );
                    _SkyChain = null;
                }
                return;
            }
            var world = Host.Client.cl.worldmodel;
            for( var i = 0; i < world.numtextures; i++ )
            {
                var t = world.textures[i];
                if( t == null )
                    continue;

                var s = t.texturechain;
                if( s == null )
                    continue;

                if( i == _SkyTextureNum )
                    DrawSkyChain( s );
                //else if( i == _MirrorTextureNum && _MirrorAlpha.Value != 1.0f )
                //{
                //    MirrorChain( s );
                //    continue;
                //}
                else
                {
                    if( ( s.flags & SurfaceDef.SURF_DRAWTURB ) != 0 && _WaterAlpha.Value != 1.0f )
                        continue;	// draw translucent water later
                    for( ; s != null; s = s.texturechain )
                        RenderBrushPoly( s );
                }

                t.texturechain = null;
            }
        }

        /// <summary>
        /// R_RenderBrushPoly
        /// </summary>
        private void RenderBrushPoly( MemorySurface fa )
        {
            _BrushPolys++;

            if( ( fa.flags & SurfaceDef.SURF_DRAWSKY ) != 0 )
            {	// warp texture, no lightmaps
                EmitBothSkyLayers( fa );
                return;
            }

            var t = TextureAnimation( fa.texinfo.texture );
            t.texture.Bind( );

            if ( ( fa.flags & SurfaceDef.SURF_DRAWTURB ) != 0 )
            {	// warp texture, no lightmaps
                EmitWaterPolys( fa );
                return;
            }

            if( ( fa.flags & SurfaceDef.SURF_UNDERWATER ) != 0 )
                Host.Video.Device.Graphics.DrawWaterPoly( fa.polys, Host.RealTime );
            else
                Host.Video.Device.Graphics.DrawPoly( fa.polys, t.scaleX, t.scaleY );

            // add the poly to the proper lightmap chain

            fa.polys.chain = _LightMapPolys[fa.lightmaptexturenum];
            _LightMapPolys[fa.lightmaptexturenum] = fa.polys;

            // check for lightmap modification
            var modified = false;
            for( var maps = 0; maps < BspDef.MAXLIGHTMAPS && fa.styles[maps] != 255; maps++ )
                if( _LightStyleValue[fa.styles[maps]] != fa.cached_light[maps] )
                {
                    modified = true;
                    break;
                }

            if( modified ||
                fa.dlightframe == _FrameCount ||	// dynamic this frame
                fa.cached_dlight )			// dynamic previously
            {
                if( _Dynamic.Value != 0 )
                {
                    LightMapTexture.LightMapModified[fa.lightmaptexturenum] = true;
                    UpdateRect( fa, ref LightMapTexture.LightMapRectChange[fa.lightmaptexturenum] );
                    var offset = fa.lightmaptexturenum * _LightMapBytes * RenderDef.BLOCK_WIDTH * RenderDef.BLOCK_HEIGHT;
                    offset += fa.light_t * RenderDef.BLOCK_WIDTH * _LightMapBytes + fa.light_s * _LightMapBytes;
                    BuildLightMap( fa, new ByteArraySegment( _LightMaps, offset ), RenderDef.BLOCK_WIDTH * _LightMapBytes );
                }
            }
        }

        private void UpdateRect( MemorySurface fa, ref glRect_t theRect )
        {
            if( fa.light_t < theRect.t )
            {
                if( theRect.h != 0 )
                    theRect.h += ( Byte ) ( theRect.t - fa.light_t );
                theRect.t = ( Byte ) fa.light_t;
            }
            if( fa.light_s < theRect.l )
            {
                if( theRect.w != 0 )
                    theRect.w += ( Byte ) ( theRect.l - fa.light_s );
                theRect.l = ( Byte ) fa.light_s;
            }
            var smax = ( fa.extents[0] >> 4 ) + 1;
            var tmax = ( fa.extents[1] >> 4 ) + 1;
            if( ( theRect.w + theRect.l ) < ( fa.light_s + smax ) )
                theRect.w = ( Byte ) ( ( fa.light_s - theRect.l ) + smax );
            if( ( theRect.h + theRect.t ) < ( fa.light_t + tmax ) )
                theRect.h = ( Byte ) ( ( fa.light_t - theRect.t ) + tmax );
        }
        
        /// <summary>
        /// R_MirrorChain
        /// </summary>
        //private void MirrorChain( MemorySurface s )
        //{
        //    if( _IsMirror )
        //        return;
        //    _IsMirror = true;
        //    _MirrorPlane = s.plane;
        //}

        /// <summary>
        /// R_RecursiveWorldNode
        /// </summary>
        private void RecursiveWorldNode( MemoryNodeBase node )
        {
            if( node.contents == ContentsDef.CONTENTS_SOLID )
                return;		// solid

            if( node.visframe != _VisFrameCount )
                return;
            if( CullBox( ref node.mins, ref node.maxs ) )
                return;

            Int32 c;

            // if a leaf node, draw stuff
            if( node.contents < 0 )
            {
                var pleaf = (MemoryLeaf)node;
                var marks = pleaf.marksurfaces;
                var mark = pleaf.firstmarksurface;
                c = pleaf.nummarksurfaces;

                if( c != 0 )
                {
                    do
                    {
                        marks[mark].visframe = _FrameCount;
                        mark++;
                    } while( --c != 0 );
                }

                // deal with model fragments in this leaf
                if( pleaf.efrags != null )
                    StoreEfrags( pleaf.efrags );

                return;
            }

            // node is just a decision point, so go down the apropriate sides

            var n = (MemoryNode)node;

            // find which side of the node we are on
            var plane = n.plane;
            Double dot;

            switch( plane.type )
            {
                case PlaneDef.PLANE_X:
                    dot = _ModelOrg.X - plane.dist;
                    break;

                case PlaneDef.PLANE_Y:
                    dot = _ModelOrg.Y - plane.dist;
                    break;

                case PlaneDef.PLANE_Z:
                    dot = _ModelOrg.Z - plane.dist;
                    break;

                default:
                    dot = Vector3.Dot( _ModelOrg, plane.normal ) - plane.dist;
                    break;
            }

            var side = ( dot >= 0 ? 0 : 1 );

            // recurse down the children, front side first
            RecursiveWorldNode( n.children[side] );

            // draw stuff
            c = n.numsurfaces;

            if( c != 0 )
            {
                var surf = Host.Client.cl.worldmodel.surfaces;
                Int32 offset = n.firstsurface;

                if( dot < 0 - QDef.BACKFACE_EPSILON )
                    side = SurfaceDef.SURF_PLANEBACK;
                else if( dot > QDef.BACKFACE_EPSILON )
                    side = 0;

                for( ; c != 0; c--, offset++ )
                {
                    if( surf[offset].visframe != _FrameCount )
                        continue;

                    // don't backface underwater surfaces, because they warp
                    if( ( surf[offset].flags & SurfaceDef.SURF_UNDERWATER ) == 0 && ( ( dot < 0 ) ^ ( ( surf[offset].flags & SurfaceDef.SURF_PLANEBACK ) != 0 ) ) )
                        continue;		// wrong side

                    // if sorting by texture, just store it out
                    if( _glTexSort.Value != 0 )
                    {
                        //if( !_IsMirror || surf[offset].texinfo.texture != Host.Client.cl.worldmodel.textures[_MirrorTextureNum] )
                        //{
                            surf[offset].texturechain = surf[offset].texinfo.texture.texturechain;
                            surf[offset].texinfo.texture.texturechain = surf[offset];
                        //}
                    }
                    else if( ( surf[offset].flags & SurfaceDef.SURF_DRAWSKY ) != 0 )
                    {
                        surf[offset].texturechain = _SkyChain;
                        _SkyChain = surf[offset];
                    }
                    else if( ( surf[offset].flags & SurfaceDef.SURF_DRAWTURB ) != 0 )
                    {
                        surf[offset].texturechain = _WaterChain;
                        _WaterChain = surf[offset];
                    }
                    else
                        DrawSequentialPoly( surf[offset] );
                }
            }

            // recurse down the back side
            RecursiveWorldNode( n.children[side == 0 ? 1 : 0] );
        }

        /// <summary>
        /// R_DrawSequentialPoly
        /// Systems that have fast state and texture changes can
        /// just do everything as it passes with no need to sort
        /// </summary>
        private void DrawSequentialPoly( MemorySurface s )
        {
            //GL.Enable( EnableCap.Texture2D );
            //
            // normal lightmaped poly
            //
            if ( ( s.flags & ( SurfaceDef.SURF_DRAWSKY | SurfaceDef.SURF_DRAWTURB | SurfaceDef.SURF_UNDERWATER ) ) == 0 )
            {
                RenderDynamicLightmaps( s );
                var p = s.polys;
                var t = TextureAnimation( s.texinfo.texture );
                if( Host.Video.Device.Desc.SupportsMultiTexture )
                {
                    // Binds world to texture env 0
                    Host.DrawingContext.SelectTexture( MTexTarget.TEXTURE0_SGIS );
                    t.texture.Bind( );
                    GL.TexEnv( TextureEnvTarget.TextureEnv, TextureEnvParameter.TextureEnvMode, ( Int32 ) TextureEnvMode.Replace );

                    // Binds lightmap to texenv 1
                    Host.Video.Device.EnableMultitexture(); // Same as SelectTexture (TEXTURE1).
                    LightMapTexture.BindLightmap( ( ( GLTextureDesc ) LightMapTexture.Desc ).TextureNumber + s.lightmaptexturenum );
                    var i = s.lightmaptexturenum;
                    if( LightMapTexture.LightMapModified[i] )
                        CommitLightmap( i );

                    GL.TexEnv( TextureEnvTarget.TextureEnv, TextureEnvParameter.TextureEnvMode, ( Int32 ) TextureEnvMode.Blend );
                    GL.Begin( PrimitiveType.Polygon );
                    for( i = 0; i < p.numverts; i++ )
                    {
                        var v = p.verts[i];
                        GL.MultiTexCoord2( TextureUnit.Texture0, v[3], v[4] );
                        GL.MultiTexCoord2( TextureUnit.Texture1, v[5], v[6] );
                        GL.Vertex3( v );
                    }
                    GL.End();
                    return;
                }
                else
                {
                    t.texture.Bind( );
                    GL.Begin( PrimitiveType.Polygon );
                    for( var i = 0; i < p.numverts; i++ )
                    {
                        var v = p.verts[i];
                        GL.TexCoord2( v[3], v[4] );
                        GL.Vertex3( v );
                    }
                    GL.End();

                    LightMapTexture.BindLightmap( ( ( GLTextureDesc ) LightMapTexture.Desc ).TextureNumber + s.lightmaptexturenum );
                    GL.Enable( EnableCap.Blend );
                    GL.Begin( PrimitiveType.Polygon );
                    for( var i = 0; i < p.numverts; i++ )
                    {
                        var v = p.verts[i];
                        GL.TexCoord2( v[5], v[6] );
                        GL.Vertex3( v );
                    }
                    GL.End();

                    GL.Disable( EnableCap.Blend );
                }

                return;
            }

            //
            // subdivided water surface warp
            //

            if( ( s.flags & SurfaceDef.SURF_DRAWTURB ) != 0 )
            {
                Host.Video.Device.DisableMultitexture();
                s.texinfo.texture.texture.Bind( );
                EmitWaterPolys( s );
                return;
            }

            //
            // subdivided sky warp
            //
            if( ( s.flags & SurfaceDef.SURF_DRAWSKY ) != 0 )
            {
                Host.Video.Device.DisableMultitexture();
                SolidSkyTexture.Bind( );
                _SpeedScale = ( Single ) Host.RealTime * 8;
                _SpeedScale -= ( Int32 ) _SpeedScale & ~127;

                Host.Video.Device.Graphics.EmitSkyPolys( s.polys, Host.RenderContext.Origin, _SpeedScale );

                AlphaSkyTexture.Bind( );
                _SpeedScale = ( Single ) Host.RealTime * 16;
                _SpeedScale -= ( Int32 ) _SpeedScale & ~127;

                Host.Video.Device.Graphics.EmitSkyPolys( s.polys, Host.RenderContext.Origin, _SpeedScale, true );
                return;
            }

            //
            // underwater warped with lightmap
            //
            RenderDynamicLightmaps( s );
            if( Host.Video.Device.Desc.SupportsMultiTexture )
            {
                var t = TextureAnimation( s.texinfo.texture );
                Host.DrawingContext.SelectTexture( MTexTarget.TEXTURE0_SGIS );
                t.texture.Bind( );
                GL.TexEnv( TextureEnvTarget.TextureEnv, TextureEnvParameter.TextureEnvMode, ( Int32 ) TextureEnvMode.Replace );
                Host.Video.Device.EnableMultitexture();
                LightMapTexture.BindLightmap( ( ( GLTextureDesc ) LightMapTexture.Desc ).TextureNumber + s.lightmaptexturenum );
                var i = s.lightmaptexturenum;
                if( LightMapTexture.LightMapModified[i] )
                    CommitLightmap( i );

                GL.TexEnv( TextureEnvTarget.TextureEnv, TextureEnvParameter.TextureEnvMode, ( Int32 ) TextureEnvMode.Blend );
                GL.Begin( PrimitiveType.TriangleFan );
                var p = s.polys;
                var nv = new Single[3];
                for( i = 0; i < p.numverts; i++ )
                {
                    var v = p.verts[i];
                    GL.MultiTexCoord2( TextureUnit.Texture0, v[3], v[4] );
                    GL.MultiTexCoord2( TextureUnit.Texture1, v[5], v[6] );

                    nv[0] = ( Single ) ( v[0] + 8 * Math.Sin( v[1] * 0.05 + Host.RealTime ) * Math.Sin( v[2] * 0.05 + Host.RealTime ) );
                    nv[1] = ( Single ) ( v[1] + 8 * Math.Sin( v[0] * 0.05 + Host.RealTime ) * Math.Sin( v[2] * 0.05 + Host.RealTime ) );
                    nv[2] = v[2];

                    GL.Vertex3( nv );
                }
                GL.End();
            }
            else
            {
                var p = s.polys;

                var t = TextureAnimation( s.texinfo.texture );
                t.texture.Bind( );
                Host.Video.Device.Graphics.DrawWaterPoly( p, Host.RealTime );

                LightMapTexture.BindLightmap( ( ( GLTextureDesc ) LightMapTexture.Desc ).TextureNumber + s.lightmaptexturenum );
                Host.Video.Device.Graphics.DrawWaterPolyLightmap( p, Host.RealTime, true );
            }

            //GL.Disable( EnableCap.Texture2D );
        }

        private void CommitLightmap( Int32 i )
        {
            LightMapTexture.CommitLightmap( _LightMaps, i );
        }

        /// <summary>
        /// R_TextureAnimation
        /// Returns the proper texture for a given time and base texture
        /// </summary>
        private ModelTexture TextureAnimation( ModelTexture t )
        {
            if( _CurrentEntity.frame != 0 )
            {
                if( t.alternate_anims != null )
                    t = t.alternate_anims;
            }

            if( t.anim_total == 0 )
                return t;

            var reletive = ( Int32 ) ( Host.Client.cl.time * 10 ) % t.anim_total;
            var count = 0;
            while( t.anim_min > reletive || t.anim_max <= reletive )
            {
                t = t.anim_next;
                if( t == null )
                    Utilities.Error( "R_TextureAnimation: broken cycle" );
                if( ++count > 100 )
                    Utilities.Error( "R_TextureAnimation: infinite cycle" );
            }

            return t;
        }

        /// <summary>
        /// R_RenderDynamicLightmaps
        /// Multitexture
        /// </summary>
        private void RenderDynamicLightmaps( MemorySurface fa )
        {
            _BrushPolys++;

            if( ( fa.flags & ( SurfaceDef.SURF_DRAWSKY | SurfaceDef.SURF_DRAWTURB ) ) != 0 )
                return;

            fa.polys.chain = _LightMapPolys[fa.lightmaptexturenum];
            _LightMapPolys[fa.lightmaptexturenum] = fa.polys;

            // check for lightmap modification
            var flag = false;
            for( var maps = 0; maps < BspDef.MAXLIGHTMAPS && fa.styles[maps] != 255; maps++ )
                if( _LightStyleValue[fa.styles[maps]] != fa.cached_light[maps] )
                {
                    flag = true;
                    break;
                }

            if( flag ||
                fa.dlightframe == _FrameCount || // dynamic this frame
                fa.cached_dlight )	// dynamic previously
            {
                if( _Dynamic.Value != 0 )
                {
                    LightMapTexture.LightMapModified[fa.lightmaptexturenum] = true;
                    UpdateRect( fa, ref LightMapTexture.LightMapRectChange[fa.lightmaptexturenum] );
                    var offset = fa.lightmaptexturenum * _LightMapBytes * RenderDef.BLOCK_WIDTH * RenderDef.BLOCK_HEIGHT +
                        fa.light_t * RenderDef.BLOCK_WIDTH * _LightMapBytes + fa.light_s * _LightMapBytes;
                    BuildLightMap( fa, new ByteArraySegment( _LightMaps, offset ), RenderDef.BLOCK_WIDTH * _LightMapBytes );
                }
            }
        }

        /// <summary>
        /// R_DrawBrushModel
        /// </summary>
        private void DrawBrushModel( Entity e )
        {
            _CurrentEntity = e;
            Host.DrawingContext.CurrentTexture = -1;

            var clmodel = e.model;
            var rotated = false;
            Vector3 mins, maxs;
            if( e.angles.X != 0 || e.angles.Y != 0 || e.angles.Z != 0 )
            {
                rotated = true;
                mins = e.origin;
                mins.X -= clmodel.radius;
                mins.Y -= clmodel.radius;
                mins.Z -= clmodel.radius;
                maxs = e.origin;
                maxs.X += clmodel.radius;
                maxs.Y += clmodel.radius;
                maxs.Z += clmodel.radius;
            }
            else
            {
                mins = e.origin + clmodel.mins;
                maxs = e.origin + clmodel.maxs;
            }

            if( CullBox( ref mins, ref maxs ) )
                return;

            Array.Clear( _LightMapPolys, 0, _LightMapPolys.Length );
            _ModelOrg = _RefDef.vieworg - e.origin;
            if( rotated )
            {
                var temp = _ModelOrg;
                Vector3 forward, right, up;
                MathLib.AngleVectors( ref e.angles, out forward, out right, out up );
                _ModelOrg.X = Vector3.Dot( temp, forward );
                _ModelOrg.Y = -Vector3.Dot( temp, right );
                _ModelOrg.Z = Vector3.Dot( temp, up );
            }

            // calculate dynamic lighting for bmodel if it's not an
            // instanced model
            if( clmodel.firstmodelsurface != 0 && _glFlashBlend.Value == 0 )
            {
                for( var k = 0; k < ClientDef.MAX_DLIGHTS; k++ )
                {
                    if( ( Host.Client.DLights[k].die < Host.Client.cl.time ) || ( Host.Client.DLights[k].radius == 0 ) )
                        continue;

                    MarkLights( Host.Client.DLights[k], 1 << k, clmodel.nodes[clmodel.hulls[0].firstclipnode] );
                }
            }

            Host.Video.Device.PushMatrix( );
            e.angles.X = -e.angles.X;	// stupid quake bug
            Host.Video.Device.RotateForEntity( e.origin, e.angles );
            e.angles.X = -e.angles.X;	// stupid quake bug

            var surfOffset = clmodel.firstmodelsurface;
            var psurf = clmodel.surfaces; //[clmodel.firstmodelsurface];

            //
            // draw texture
            //
            for( var i = 0; i < clmodel.nummodelsurfaces; i++, surfOffset++ )
            {
                // find which side of the node we are on
                var pplane = psurf[surfOffset].plane;

                var dot = Vector3.Dot( _ModelOrg, pplane.normal ) - pplane.dist;

                // draw the polygon
                var planeBack = ( psurf[surfOffset].flags & SurfaceDef.SURF_PLANEBACK ) != 0;
                if( ( planeBack && ( dot < -QDef.BACKFACE_EPSILON ) ) || ( !planeBack && ( dot > QDef.BACKFACE_EPSILON ) ) )
                {
                    if( _glTexSort.Value != 0 )
                        RenderBrushPoly( psurf[surfOffset] );
                    else
                        DrawSequentialPoly( psurf[surfOffset] );
                }
            }

            BlendLightmaps();

            Host.Video.Device.PopMatrix( );
        }
    }

    //glRect_t;
}
