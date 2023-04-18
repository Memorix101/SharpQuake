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

using SharpQuake.Framework;
using SharpQuake.Framework.IO.BSP;
using SharpQuake.Framework.Mathematics;
using SharpQuake.Game.Data.Models;
using SharpQuake.Game.Rendering.Memory;
using SharpQuake.Renderer;
using SharpQuake.Renderer.OpenGL.Textures;
using SharpQuake.Renderer.Textures;
using System;
using System.Linq;

namespace SharpQuake.Rendering.Environment
{
    public class Lighting
    {
		// the lightmap texture data needs to be kept in
		// main memory so texsubimage can update properly
		public Byte[] LightMaps
		{
			get;
			private set;
		} = new Byte[4 * RenderDef.MAX_LIGHTMAPS * RenderDef.BLOCK_WIDTH * RenderDef.BLOCK_HEIGHT];
		
		// blocklights
		private UInt32[] BlockLights
        {
			get;
			set;
        } = new UInt32[18 * 18]; 

		// d_lightstylevalue
		public Int32[] LightStyles
        {
            get;
            private set;
        }

		public BaseTexture LightMapTexture
		{
			get;
			private set;
		}

		// r_framecount - used for dlight push checking
		public Int32 FrameCount
		{
			get;
			set;
		}

		// the lightmap texture data needs to be kept in
		// main memory so texsubimage can update properly
		private Int32 _LightMapBytes; // lightmap_bytes		// 1, 2, or 4


		private Int32 _DlightFrameCount; // r_dlightframecount
		private Plane _LightPlane; // lightplane
								   // lightspot
		public Vector3 LightSpot
        {
			get;
			private set;
        }

		private readonly Host _host;

        public Lighting( Host host )
        {
            _host = host;

            LightStyles = new Int32[256];
        }

        /// <summary>
        /// Reset lighting to normal light value
        /// </summary>
        public void Reset()
        {
            for ( var i = 0; i < 256; i++ )
                LightStyles[i] = 264;		// normal light value
        }


        /// <summary>
        /// R_AnimateLight
        /// </summary>
        public void UpdateAnimations( )
        {
            //
            // light animations
            // 'm' is normal light, 'a' is no light, 'z' is double bright
            var i = ( Int32 ) ( _host.Client.cl.time * 10 );
            for ( var j = 0; j < QDef.MAX_LIGHTSTYLES; j++ )
            {
                if ( String.IsNullOrEmpty( _host.Client.LightStyle[j].map ) )
                {
                    LightStyles[j] = 256;
                    continue;
                }
                var map = _host.Client.LightStyle[j].map;
                var k = i % map.Length;
                k = map[k] - 'a';
                k = k * 22;
                LightStyles[j] = k;
            }
        }


		/// <summary>
		/// GL_BuildLightmaps
		/// Builds the lightmap texture with all the surfaces from all brush models
		/// </summary>
		public void BuildLightMaps( )
		{
			if ( LightMapTexture != null )
				Array.Clear( LightMapTexture.LightMapData, 0, LightMapTexture.LightMapData.Length );
			//memset (allocated, 0, sizeof(allocated));

			FrameCount = 1;        // no dlightcache

			//if( _LightMapTextures == 0 )
			//   _LightMapTextures = _host.DrawingContext.GenerateTextureNumberRange( RenderDef.MAX_LIGHTMAPS );

			_host.DrawingContext.LightMapFormat = "GL_LUMINANCE";

			// default differently on the Permedia
			if ( _host.Screen.IsPermedia )
				_host.DrawingContext.LightMapFormat = "GL_RGBA";

			if ( CommandLine.HasParam( "-lm_1" ) )
				_host.DrawingContext.LightMapFormat = "GL_LUMINANCE";

			if ( CommandLine.HasParam( "-lm_a" ) )
				_host.DrawingContext.LightMapFormat = "GL_ALPHA";

			//if (CommandLine.HasParam("-lm_i"))
			//    _host.DrawingContext.LightMapFormat = PixelFormat.Intensity;

			//if (CommandLine.HasParam("-lm_2"))
			//    _host.DrawingContext.LightMapFormat = PixelFormat.Rgba4;

			if ( CommandLine.HasParam( "-lm_4" ) )
				_host.DrawingContext.LightMapFormat = "GL_RGBA";

			switch ( _host.DrawingContext.LightMapFormat )
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
			var brushes = _host.Client.cl.model_precache.Where( m => m is BrushModelData ).ToArray( );

			//for ( var j = 1; j < QDef.MAX_MODELS; j++ )
			for ( var j = 0; j < brushes.Length; j++ )
			{
				var m = ( BrushModelData ) brushes[j];
				if ( m == null )
					break;

				if ( m.Name != null && m.Name.StartsWith( "*" ) )
					continue;

				_host.RenderContext.World.Entities.Surfaces.CurrentVertBase = m.Vertices;
				_host.RenderContext.World.Entities.Surfaces.CurrentModel = m;
				for ( var i = 0; i < m.NumSurfaces; i++ )
				{
					CreateSurfaceLightmap( ref tempBuffer, m.Surfaces[i] );
					if ( ( m.Surfaces[i].flags & ( Int32 ) Q1SurfaceFlags.Turbulence ) != 0 )
						continue;

					if ( ( m.Surfaces[i].flags & ( Int32 ) Q1SurfaceFlags.Sky ) != 0 )
						continue;

					_host.RenderContext.World.Entities.Surfaces.BuildSurfaceDisplayList( m.Surfaces[i] );
				}
			}

			if ( !_host.Cvars.glTexSort.Get<Boolean>( ) )
				_host.DrawingContext.SelectTexture( MTexTarget.TEXTURE1_SGIS );

			LightMapTexture = BaseTexture.FromBuffer( _host.Video.Device, "_Lightmaps", new ByteArraySegment( LightMaps ), 128, 128, false, false, isLightMap: true );

			LightMapTexture.Desc.LightMapBytes = _LightMapBytes;
			LightMapTexture.Desc.LightMapFormat = _host.DrawingContext.LightMapFormat;

			Array.Copy( tempBuffer, LightMapTexture.LightMapData, tempBuffer.Length );

			LightMapTexture.UploadLightmap( );

			if ( !_host.Cvars.glTexSort.Get<Boolean>( ) )
				_host.DrawingContext.SelectTexture( MTexTarget.TEXTURE0_SGIS );
		}


		/// <summary>
		/// GL_CreateSurfaceLightmap
		/// </summary>
		public void CreateSurfaceLightmap( ref Int32[,] tempBuffer, MemorySurface surf )
		{
			if ( ( surf.flags & ( ( Int32 ) Q1SurfaceFlags.Sky | ( Int32 ) Q1SurfaceFlags.Turbulence ) ) != 0 )
				return;

			var smax = ( surf.extents[0] >> 4 ) + 1;
			var tmax = ( surf.extents[1] >> 4 ) + 1;

			surf.lightmaptexturenum = _host.RenderContext.World.Entities.Surfaces.AllocBlock( ref tempBuffer, smax, tmax, ref surf.light_s, ref surf.light_t );
			var offset = surf.lightmaptexturenum * _LightMapBytes * RenderDef.BLOCK_WIDTH * RenderDef.BLOCK_HEIGHT;
			offset += ( surf.light_t * RenderDef.BLOCK_WIDTH + surf.light_s ) * _LightMapBytes;
			BuildLightMap( surf, new ByteArraySegment( LightMaps, offset ), RenderDef.BLOCK_WIDTH * _LightMapBytes );
		}


		/// <summary>
		/// R_BuildLightMap
		/// Combine and scale multiple lightmaps into the 8.8 format in blocklights
		/// </summary>
		private void BuildLightMap( MemorySurface surf, ByteArraySegment dest, Int32 stride )
		{
			surf.cached_dlight = ( surf.dlightframe == FrameCount );

			var smax = ( surf.extents[0] >> 4 ) + 1;
			var tmax = ( surf.extents[1] >> 4 ) + 1;
			var size = smax * tmax;

			var srcOffset = surf.sampleofs;
			var lightmap = surf.sample_base;// surf.samples;

			// set to full bright if no light data
			if ( _host.Cvars.FullBright.Get<Boolean>( ) || _host.Client.cl.worldmodel.LightData == null )
			{
				for ( var i = 0; i < size; i++ )
					BlockLights[i] = 255 * 256;
			}
			else
			{
				// clear to no light
				for ( var i = 0; i < size; i++ )
					BlockLights[i] = 0;

				// add all the lightmaps
				if ( lightmap != null )
					for ( var maps = 0; maps < BspDef.MAXLIGHTMAPS && surf.styles[maps] != 255; maps++ )
					{
						var scale = LightStyles[surf.styles[maps]];
						surf.cached_light[maps] = scale;    // 8.8 fraction
						for ( var i = 0; i < size; i++ )
							BlockLights[i] += ( UInt32 ) ( lightmap[srcOffset + i] * scale );
						srcOffset += size; // lightmap += size;	// skip to next lightmap
					}

				// add all the dynamic lights
				if ( surf.dlightframe == FrameCount )
					AddDynamicLights( surf );
			}
			// bound, invert, and shift
			//store:
			var blOffset = 0;
			var destOffset = dest.StartIndex;
			var data = dest.Data;
			switch ( _host.DrawingContext.LightMapFormat )
			{
				case "GL_RGBA":
					stride -= ( smax << 2 );
					for ( var i = 0; i < tmax; i++, destOffset += stride ) // dest += stride
					{
						for ( var j = 0; j < smax; j++ )
						{
							var t = BlockLights[blOffset++];// *bl++;
							t >>= 7;
							if ( t > 255 )
								t = 255;
							data[destOffset + 3] = ( Byte ) ( 255 - t ); //dest[3] = 255 - t;
							destOffset += 4;
						}
					}
					break;

				case "GL_ALPHA":
				case "GL_LUMINANCE":
					//case GL_INTENSITY:
					for ( var i = 0; i < tmax; i++, destOffset += stride )
					{
						for ( var j = 0; j < smax; j++ )
						{
							var t = BlockLights[blOffset++];// *bl++;
							t >>= 7;
							if ( t > 255 )
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
			var dlights = _host.Client.DLights;

			for ( var lnum = 0; lnum < ClientDef.MAX_DLIGHTS; lnum++ )
			{
				if ( ( surf.dlightbits & ( 1 << lnum ) ) == 0 )
					continue;       // not lit by this light

				var rad = dlights[lnum].radius;
				var dist = Vector3.Dot( dlights[lnum].origin, surf.plane.normal ) - surf.plane.dist;
				rad -= Math.Abs( dist );
				var minlight = dlights[lnum].minlight;
				if ( rad < minlight )
					continue;
				minlight = rad - minlight;

				var impact = dlights[lnum].origin - surf.plane.normal * dist;

				var local0 = Vector3.Dot( impact, tex.vecs[0].Xyz ) + tex.vecs[0].W;
				var local1 = Vector3.Dot( impact, tex.vecs[1].Xyz ) + tex.vecs[1].W;

				local0 -= surf.texturemins[0];
				local1 -= surf.texturemins[1];

				for ( var t = 0; t < tmax; t++ )
				{
					var td = ( Int32 ) ( local1 - t * 16 );
					if ( td < 0 )
						td = -td;
					for ( var s = 0; s < smax; s++ )
					{
						var sd = ( Int32 ) ( local0 - s * 16 );
						if ( sd < 0 )
							sd = -sd;
						if ( sd > td )
							dist = sd + ( td >> 1 );
						else
							dist = td + ( sd >> 1 );
						if ( dist < minlight )
							BlockLights[t * smax + s] += ( UInt32 ) ( ( rad - dist ) * 256 );
					}
				}
			}
		}

		/// <summary>
		/// R_BlendLightmaps
		/// </summary>
		public void BlendLightmaps( )
		{
			if ( _host.Cvars.FullBright.Get<Boolean>( ) )
				return;
			if ( !_host.Cvars.glTexSort.Get<Boolean>( ) )
				return;

			_host.Video.Device.Graphics.BeginBlendLightMap( ( !_host.Cvars.LightMap.Get<Boolean>( ) ), _host.DrawingContext.LightMapFormat );

			for ( var i = 0; i < RenderDef.MAX_LIGHTMAPS; i++ )
			{
				var p = _host.RenderContext.World.Entities.Surfaces.LightMapPolys[i];
				if ( p == null )
					continue;

				LightMapTexture.BindLightmap( ( ( GLTextureDesc ) LightMapTexture.Desc ).TextureNumber + i );

				if ( LightMapTexture.LightMapModified[i] )
					CommitLightmap( i );

				for ( ; p != null; p = p.chain )
				{
					if ( ( p.flags & ( Int32 ) Q1SurfaceFlags.Underwater ) != 0 )
						_host.Video.Device.Graphics.DrawWaterPolyLightmap( p, _host.RealTime );
					else
						_host.Video.Device.Graphics.DrawPoly( p, isLightmap: true );
				}
			}

			_host.Video.Device.Graphics.EndBlendLightMap( ( !_host.Cvars.LightMap.Get<Boolean>( ) ), _host.DrawingContext.LightMapFormat );
		}

		private void CommitLightmap( Int32 i )
		{
			LightMapTexture.CommitLightmap( LightMaps, i );
		}

		public void Bind( Int32 lightMapNumber )
        {
			LightMapTexture.BindLightmap( ( ( GLTextureDesc ) LightMapTexture.Desc ).TextureNumber + lightMapNumber );
		}

		public void SetDirty( MemorySurface fa )
        {
			LightMapTexture.LightMapModified[fa.lightmaptexturenum] = true;
			UpdateRect( fa, ref LightMapTexture.LightMapRectChange[fa.lightmaptexturenum] );
			var offset = fa.lightmaptexturenum * _LightMapBytes * RenderDef.BLOCK_WIDTH * RenderDef.BLOCK_HEIGHT;
			offset += fa.light_t * RenderDef.BLOCK_WIDTH * _LightMapBytes + fa.light_s * _LightMapBytes;
			BuildLightMap( fa, new ByteArraySegment( LightMaps, offset ), RenderDef.BLOCK_WIDTH * _LightMapBytes );
		}
		private void UpdateRect( MemorySurface fa, ref glRect_t theRect )
		{
			if ( fa.light_t < theRect.t )
			{
				if ( theRect.h != 0 )
					theRect.h += ( Byte ) ( theRect.t - fa.light_t );
				theRect.t = ( Byte ) fa.light_t;
			}
			if ( fa.light_s < theRect.l )
			{
				if ( theRect.w != 0 )
					theRect.w += ( Byte ) ( theRect.l - fa.light_s );
				theRect.l = ( Byte ) fa.light_s;
			}
			var smax = ( fa.extents[0] >> 4 ) + 1;
			var tmax = ( fa.extents[1] >> 4 ) + 1;
			if ( ( theRect.w + theRect.l ) < ( fa.light_s + smax ) )
				theRect.w = ( Byte ) ( ( fa.light_s - theRect.l ) + smax );
			if ( ( theRect.h + theRect.t ) < ( fa.light_t + tmax ) )
				theRect.h = ( Byte ) ( ( fa.light_t - theRect.t ) + tmax );
		}

		private void AddLightBlend( Single r, Single g, Single b, Single a2 )
		{
			_host.View.Blend.A += a2 * ( 1 - _host.View.Blend.A );

			var a = _host.View.Blend.A;

			a2 = a2 / a;

			_host.View.Blend.R = _host.View.Blend.R * ( 1 - a2 ) + r * a2; // error? - v_blend[0] = v_blend[1] * (1 - a2) + r * a2;
			_host.View.Blend.G = _host.View.Blend.G * ( 1 - a2 ) + g * a2;
			_host.View.Blend.B = _host.View.Blend.B * ( 1 - a2 ) + b * a2;
		}

		// Dynamic lights
		/// <summary>
		/// R_MarkLights
		/// </summary>
		public void MarkLights( dlight_t light, Int32 bit, MemoryNodeBase node )
		{
			if ( node.contents < 0 )
				return;

			var n = ( MemoryNode ) node;
			var splitplane = n.plane;
			var dist = Vector3.Dot( light.origin, splitplane.normal ) - splitplane.dist;

			if ( dist > light.radius )
			{
				MarkLights( light, bit, n.children[0] );
				return;
			}
			if ( dist < -light.radius )
			{
				MarkLights( light, bit, n.children[1] );
				return;
			}

			// mark the polygons
			for ( var i = 0; i < n.numsurfaces; i++ )
			{
				var surf = _host.Client.cl.worldmodel.Surfaces[n.firstsurface + i];
				if ( surf.dlightframe != _DlightFrameCount )
				{
					surf.dlightbits = 0;
					surf.dlightframe = _DlightFrameCount;
				}
				surf.dlightbits |= bit;
			}

			MarkLights( light, bit, n.children[0] );
			MarkLights( light, bit, n.children[1] );
		}

		/// <summary>
		/// R_PushDlights
		/// </summary>
		public void PushDlights( )
		{
			if ( _host.Cvars.glFlashBlend.Get<Boolean>( ) )
				return;

			_DlightFrameCount = FrameCount + 1;    // because the count hasn't advanced yet for this frame

			for ( var i = 0; i < ClientDef.MAX_DLIGHTS; i++ )
			{
				var l = _host.Client.DLights[i];
				if ( l.die < _host.Client.cl.time || l.radius == 0 )
					continue;
				MarkLights( l, 1 << i, _host.Client.cl.worldmodel.Nodes[0] );
			}
		}

		/// <summary>
		/// R_RenderDlight
		/// </summary>
		public void RenderDlight( dlight_t light )
		{
			var rad = light.radius * 0.35f;
			var v = light.origin - _host.RenderContext.Origin;
			if ( v.Length < rad )
			{   // view is inside the dlight
				AddLightBlend( 1, 0.5f, 0, light.radius * 0.0003f );
				return;
			}

			_host.Video.Device.Graphics.DrawDLight( light, _host.RenderContext.ViewPn, _host.RenderContext.ViewUp, _host.RenderContext.ViewRight );
		}

		/// <summary>
		/// R_RenderDlights
		/// </summary>
		public void RenderDlights( )
		{
			//int i;
			//dlight_t* l;

			if ( !_host.Cvars.glFlashBlend.Get<Boolean>( ) )
				return;

			_DlightFrameCount = FrameCount + 1;    // because the count hasn't advanced yet for this frame

			_host.Video.Device.Graphics.BeginDLights( );
			_host.Video.Device.SetZWrite( false );

			for ( var i = 0; i < ClientDef.MAX_DLIGHTS; i++ )
			{
				var l = _host.Client.DLights[i];
				if ( l.die < _host.Client.cl.time || l.radius == 0 )
					continue;

				RenderDlight( l );
			}

			_host.Video.Device.Graphics.EndDLights( );
		}

		/// <summary>
		/// R_LightPoint
		/// </summary>
		public Int32 LightPoint( ref Vector3 p )
		{
			if ( _host.Client.cl.worldmodel.LightData == null )
				return 255;

			var end = p;
			end.Z -= 2048;

			var r = RecursiveLightPoint( _host.Client.cl.worldmodel.Nodes[0], ref p, ref end );
			if ( r == -1 )
				r = 0;

			return r;
		}

		private Int32 RecursiveLightPoint( MemoryNodeBase node, ref Vector3 start, ref Vector3 end )
		{
			if ( node.contents < 0 )
				return -1;      // didn't hit anything

			var n = ( MemoryNode ) node;

			// calculate mid point

			// FIXME: optimize for axial
			var plane = n.plane;
			var front = Vector3.Dot( start, plane.normal ) - plane.dist;
			var back = Vector3.Dot( end, plane.normal ) - plane.dist;
			var side = front < 0 ? 1 : 0;

			if ( ( back < 0 ? 1 : 0 ) == side )
				return RecursiveLightPoint( n.children[side], ref start, ref end );

			var frac = front / ( front - back );
			var mid = start + ( end - start ) * frac;

			// go down front side
			var r = RecursiveLightPoint( n.children[side], ref start, ref mid );
			if ( r >= 0 )
				return r;       // hit something

			if ( ( back < 0 ? 1 : 0 ) == side )
				return -1;      // didn't hit anuthing

			// check for impact on this node
			LightSpot = mid;
			_LightPlane = plane;

			var surf = _host.Client.cl.worldmodel.Surfaces;
			Int32 offset = n.firstsurface;
			for ( var i = 0; i < n.numsurfaces; i++, offset++ )
			{
				if ( ( surf[offset].flags & ( Int32 ) Q1SurfaceFlags.Tiled ) != 0 )
					continue;   // no lightmaps

				var tex = surf[offset].texinfo;

				var s = ( Int32 ) ( Vector3.Dot( mid, tex.vecs[0].Xyz ) + tex.vecs[0].W );
				var t = ( Int32 ) ( Vector3.Dot( mid, tex.vecs[1].Xyz ) + tex.vecs[1].W );

				if ( s < surf[offset].texturemins[0] || t < surf[offset].texturemins[1] )
					continue;

				var ds = s - surf[offset].texturemins[0];
				var dt = t - surf[offset].texturemins[1];

				if ( ds > surf[offset].extents[0] || dt > surf[offset].extents[1] )
					continue;

				if ( surf[offset].sample_base == null )
					return 0;

				ds >>= 4;
				dt >>= 4;

				var lightmap = surf[offset].sample_base;
				var lmOffset = surf[offset].sampleofs;
				var extents = surf[offset].extents;
				r = 0;
				if ( lightmap != null )
				{
					lmOffset += dt * ( ( extents[0] >> 4 ) + 1 ) + ds;

					for ( var maps = 0; maps < BspDef.MAXLIGHTMAPS && surf[offset].styles[maps] != 255; maps++ )
					{
						var scale = LightStyles[surf[offset].styles[maps]];
						r += lightmap[lmOffset] * scale;
						lmOffset += ( ( extents[0] >> 4 ) + 1 ) * ( ( extents[1] >> 4 ) + 1 );
					}

					r >>= 8;
				}

				return r;
			}

			// go down back side
			return RecursiveLightPoint( n.children[side == 0 ? 1 : 0], ref mid, ref end );
		}
	}
}
