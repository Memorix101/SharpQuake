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
using System.Linq;
using SharpQuake.Framework;
using SharpQuake.Framework.IO.BSP;
using SharpQuake.Framework.Mathematics;
using SharpQuake.Game.Data.Models;
using SharpQuake.Game.Rendering.Memory;
using SharpQuake.Game.Rendering.Textures;
using SharpQuake.Game.World;
using SharpQuake.Renderer;
using SharpQuake.Renderer.OpenGL.Textures;
using SharpQuake.Renderer.Textures;
using SharpQuake.Rendering;

// gl_rsurf.c

namespace SharpQuake
{
	partial class render
	{
		private const Double COLINEAR_EPSILON = 0.001;

		//private Int32 _LightMapTextures; // lightmap_textures
		private MemoryVertex[] _CurrentVertBase; // r_pcurrentvertbase
		private ModelData _CurrentModel; // currentmodel
										 //private System.Boolean[] _LightMapModified = new System.Boolean[RenderDef.MAX_LIGHTMAPS]; // lightmap_modified
		private GLPoly[] _LightMapPolys = new GLPoly[RenderDef.MAX_LIGHTMAPS]; // lightmap_polys
																			   //private glRect_t[] _LightMapRectChange = new glRect_t[RenderDef.MAX_LIGHTMAPS]; // lightmap_rectchange
		private Int32 _ColinElim; // nColinElim
		
		
		private Entity _TempEnt = new Entity( ); // for DrawWorld

		protected TextureChains TextureChains
		{
			get;
			set;
		}

		public GLPoly[] LightMapPolys
        {
			get
            {
				return _LightMapPolys;
            }
			set
            {
				_LightMapPolys = value;
            }
		}
		public MemoryVertex[] CurrentVertBase
		{
			get
			{
				return _CurrentVertBase;
			}
			set
			{
				_CurrentVertBase = value;
			}
		}

		public ModelData CurrentModel
		{
			get
			{
				return _CurrentModel;
			}
			set
			{
				_CurrentModel = value;
			}
		}

		/// <summary>
		/// BuildSurfaceDisplayList
		/// </summary>
		public void BuildSurfaceDisplayList( MemorySurface fa )
		{
			var BrushModelData = ( BrushModelData ) _CurrentModel;
			// reconstruct the polygon
			var pedges = BrushModelData.Edges;
			var lnumverts = fa.numedges;

			//
			// draw texture
			//
			var poly = new GLPoly( );
			poly.AllocVerts( lnumverts );
			poly.next = fa.polys;
			poly.flags = fa.flags;
			fa.polys = poly;

			UInt16[] r_pedge_v;
			Vector3 vec;

			for ( var i = 0; i < lnumverts; i++ )
			{
				var lindex = BrushModelData.SurfEdges[fa.firstedge + i];
				if ( lindex > 0 )
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
			if ( !Host.Cvars.glKeepTJunctions.Get<Boolean>( ) && ( fa.flags & ( Int32 ) Q1SurfaceFlags.Underwater ) == 0 )
			{
				for ( var i = 0; i < lnumverts; ++i )
				{
					if ( Utilities.IsCollinear( poly.verts[( i + lnumverts - 1 ) % lnumverts],
						poly.verts[i],
						poly.verts[( i + 1 ) % lnumverts] ) )
					{
						Int32 j;
						for ( j = i + 1; j < lnumverts; ++j )
						{
							//int k;
							for ( var k = 0; k < ModelDef.VERTEXSIZE; ++k )
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

		// returns a texture number and the position inside it
		public Int32 AllocBlock( ref Int32[,] data, Int32 w, Int32 h, ref Int32 x, ref Int32 y )
		{
			for ( var texnum = 0; texnum < RenderDef.MAX_LIGHTMAPS; texnum++ )
			{
				var best = RenderDef.BLOCK_HEIGHT;

				for ( var i = 0; i < RenderDef.BLOCK_WIDTH - w; i++ )
				{
					Int32 j = 0, best2 = 0;

					for ( j = 0; j < w; j++ )
					{
						if ( data[texnum, i + j] >= best )
							break;
						if ( data[texnum, i + j] > best2 )
							best2 = data[texnum, i + j];
					}

					if ( j == w )
					{
						// this is a valid spot
						x = i;
						y = best = best2;
					}
				}

				if ( best + h > RenderDef.BLOCK_HEIGHT )
					continue;

				for ( var i = 0; i < w; i++ )
					data[texnum, x + i] = best + h;

				return texnum;
			}

			Utilities.Error( "AllocBlock: full" );
			return 0; // shut up compiler
		}

		/// <summary>
		/// R_DrawWaterSurfaces
		/// </summary>
		private void DrawWaterSurfaces( )
		{
			if ( Host.Cvars.WaterAlpha.Get<Single>( ) == 1.0f && Host.Cvars.glTexSort.Get<Boolean>( ) )
				return;

			//
			// go back to the world matrix
			//
			Host.Video.Device.ResetMatrix( );

			// WaterAlpha is broken - will fix when we introduce GLSL...
			//if ( _WaterAlpha.Value < 1.0 )
			//{
			//    GL.Enable( EnableCap.Blend );
			//    GL.Color4( 1, 1, 1, _WaterAlpha.Value );
			//    GL.TexEnv( TextureEnvTarget.TextureEnv, TextureEnvParameter.TextureEnvMode, ( Int32 ) TextureEnvMode.Modulate );
			//}

			if ( !Host.Cvars.glTexSort.Get<Boolean>( ) )
			{
				if ( TextureChains.WaterChain == null )
					return;

				for ( var s = TextureChains.WaterChain; s != null; s = s.texturechain )
				{
					s.texinfo.texture.texture.Bind( );
					WarpableTextures.EmitWaterPolys( Host.RealTime, s );
				}
				TextureChains.WaterChain = null;
			}
			else
			{
				for ( var i = 0; i < Host.Client.cl.worldmodel.NumTextures; i++ )
				{
					var t = Host.Client.cl.worldmodel.Textures[i];
					if ( t == null )
						continue;

					var s = t.texturechain;
					if ( s == null )
						continue;

					if ( ( s.flags & ( Int32 ) Q1SurfaceFlags.Turbulence ) == 0 )
						continue;

					// set modulate mode explicitly

					t.texture.Bind( );

					for ( ; s != null; s = s.texturechain )
						WarpableTextures.EmitWaterPolys( Host.RealTime, s );

					t.texturechain = null;
				}
			}

			// WaterAlpha is broken - will fix when we introduce GLSL...
			//if( _WaterAlpha.Value < 1.0 )
			//{
			//    GL.TexEnv( TextureEnvTarget.TextureEnv, TextureEnvParameter.TextureEnvMode, ( Int32 ) TextureEnvMode.Replace );
			//    GL.Color4( 1f, 1, 1, 1 );
			//    GL.Disable( EnableCap.Blend );
			//}
		}		

		/// <summary>
		/// R_DrawWorld
		/// </summary>
		private void DrawWorld( )
		{
			_TempEnt.Clear( );
			_TempEnt.model = Host.Client.cl.worldmodel;

			_ModelOrg = _RefDef.vieworg;
			_CurrentEntity = _TempEnt;
			Host.DrawingContext.CurrentTexture = -1;

			Array.Clear( _LightMapPolys, 0, _LightMapPolys.Length );

			RecursiveWorldNode( ( ( BrushModelData ) _TempEnt.model ).Nodes[0] );

			DrawTextureChains( );

			World.Lighting.BlendLightmaps( );
		}


		private void DrawTextureChains( )
		{
			if ( !Host.Cvars.glTexSort.Get<Boolean>( ) )
			{
				Host.Video.Device.DisableMultitexture( );

				if ( TextureChains.SkyChain != null )
				{
					WarpableTextures.DrawSkyChain( Host.RealTime, Host.RenderContext.Origin, TextureChains.SkyChain );
					TextureChains.SkyChain = null;
				}
				return;
			}
			var world = Host.Client.cl.worldmodel;
			for ( var i = 0; i < world.NumTextures; i++ )
			{
				var t = world.Textures[i];
				if ( t == null )
					continue;

				var s = t.texturechain;
				if ( s == null )
					continue;

				if ( i == World.Sky.TextureNumber )
					WarpableTextures.DrawSkyChain( Host.RealTime, Host.RenderContext.Origin, s );
				//else if( i == _MirrorTextureNum && _MirrorAlpha.Value != 1.0f )
				//{
				//    MirrorChain( s );
				//    continue;
				//}
				else
				{
					if ( ( s.flags & ( Int32 ) Q1SurfaceFlags.Turbulence ) != 0 && Host.Cvars.WaterAlpha.Get<Single>( ) != 1.0f )
						continue;   // draw translucent water later
					for ( ; s != null; s = s.texturechain )
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

			if ( ( fa.flags & ( Int32 ) Q1SurfaceFlags.Sky ) != 0 )
			{   // warp texture, no lightmaps
				WarpableTextures.EmitBothSkyLayers( Host.RealTime, Host.RenderContext.Origin, fa );
				return;
			}

			var t = TextureAnimation( fa.texinfo.texture );
			t.texture.Bind( );

			if ( ( fa.flags & ( Int32 ) Q1SurfaceFlags.Turbulence ) != 0 )
			{   // warp texture, no lightmaps
				WarpableTextures.EmitWaterPolys( Host.RealTime, fa );
				return;
			}

			if ( ( fa.flags & ( Int32 ) Q1SurfaceFlags.Underwater ) != 0 )
				Host.Video.Device.Graphics.DrawWaterPoly( fa.polys, Host.RealTime );
			else
				Host.Video.Device.Graphics.DrawPoly( fa.polys, t.scaleX, t.scaleY );

			// add the poly to the proper lightmap chain

			fa.polys.chain = _LightMapPolys[fa.lightmaptexturenum];
			_LightMapPolys[fa.lightmaptexturenum] = fa.polys;

			// check for lightmap modification
			var modified = false;
			for ( var maps = 0; maps < BspDef.MAXLIGHTMAPS && fa.styles[maps] != 255; maps++ )
				if ( World.Lighting.LightStyles[fa.styles[maps]] != fa.cached_light[maps] )
				{
					modified = true;
					break;
				}

			if ( modified ||
				fa.dlightframe == World.Lighting.FrameCount ||    // dynamic this frame
				fa.cached_dlight )          // dynamic previously
			{
				if ( Host.Cvars.Dynamic.Get<Boolean>( ) )
					World.Lighting.SetDirty( fa );
			}
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
			World.Occlusion.RecursiveWorldNode( node, _ModelOrg, World.Lighting.FrameCount, ref _Frustum, ( surf ) => 
			{
				DrawSequentialPoly( surf );
			}, ( efrags ) => 
			{
				StoreEfrags( efrags );
			} );
		}

		/// <summary>
		/// R_DrawSequentialPoly
		/// Systems that have fast state and texture changes can
		/// just do everything as it passes with no need to sort
		/// </summary>
		private void DrawSequentialPoly( MemorySurface s )
		{
			//
			// normal lightmaped poly
			//
			if ( ( s.flags & ( ( Int32 ) Q1SurfaceFlags.Sky | ( Int32 ) Q1SurfaceFlags.Turbulence | ( Int32 ) Q1SurfaceFlags.Underwater ) ) == 0 )
			{
				RenderDynamicLightmaps( s );
				var p = s.polys;
				var t = TextureAnimation( s.texinfo.texture );
				if ( Host.Video.Device.Desc.SupportsMultiTexture )
				{
					Host.Video.Device.Graphics.DrawSequentialPolyMultiTexture( t.texture, World.Lighting.LightMapTexture, World.Lighting.LightMaps, p, s.lightmaptexturenum );
					return;
				}
				else
				{
					Host.Video.Device.Graphics.DrawSequentialPoly( t.texture, World.Lighting.LightMapTexture, p, s.lightmaptexturenum );
				}

				return;
			}

			//
			// subdivided water surface warp
			//

			if ( ( s.flags & ( Int32 ) Q1SurfaceFlags.Turbulence ) != 0 )
			{
				Host.Video.Device.DisableMultitexture( );
				s.texinfo.texture.texture.Bind( );
				WarpableTextures.EmitWaterPolys( Host.RealTime, s );
				return;
			}

			//
			// subdivided sky warp
			//
			if ( ( s.flags & ( Int32 ) Q1SurfaceFlags.Sky ) != 0 )
			{
				WarpableTextures.EmitBothSkyLayers( Host.RealTime, Host.RenderContext.Origin, s );
				return;
			}

			//
			// underwater warped with lightmap
			//
			RenderDynamicLightmaps( s );
			if ( Host.Video.Device.Desc.SupportsMultiTexture )
			{
				var t = TextureAnimation( s.texinfo.texture );

				Host.DrawingContext.SelectTexture( MTexTarget.TEXTURE0_SGIS );

				Host.Video.Device.Graphics.DrawWaterPolyMultiTexture( World.Lighting.LightMaps, t.texture, World.Lighting.LightMapTexture, s.lightmaptexturenum, s.polys, Host.RealTime );
			}
			else
			{
				var p = s.polys;

				var t = TextureAnimation( s.texinfo.texture );
				t.texture.Bind( );
				Host.Video.Device.Graphics.DrawWaterPoly( p, Host.RealTime );

				World.Lighting.Bind( s.lightmaptexturenum );
				Host.Video.Device.Graphics.DrawWaterPolyLightmap( p, Host.RealTime, true );
			}
		}


		/// <summary>
		/// R_TextureAnimation
		/// Returns the proper texture for a given time and base texture
		/// </summary>
		private ModelTexture TextureAnimation( ModelTexture t )
		{
			if ( _CurrentEntity.frame != 0 )
			{
				if ( t.alternate_anims != null )
					t = t.alternate_anims;
			}

			if ( t.anim_total == 0 )
				return t;

			var reletive = ( Int32 ) ( Host.Client.cl.time * 10 ) % t.anim_total;
			var count = 0;
			while ( t.anim_min > reletive || t.anim_max <= reletive )
			{
				t = t.anim_next;
				if ( t == null )
					Utilities.Error( "R_TextureAnimation: broken cycle" );
				if ( ++count > 100 )
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

			if ( ( fa.flags & ( ( Int32 ) Q1SurfaceFlags.Sky | ( Int32 ) Q1SurfaceFlags.Turbulence ) ) != 0 )
				return;

			fa.polys.chain = _LightMapPolys[fa.lightmaptexturenum];
			_LightMapPolys[fa.lightmaptexturenum] = fa.polys;

			// check for lightmap modification
			var flag = false;
			for ( var maps = 0; maps < BspDef.MAXLIGHTMAPS && fa.styles[maps] != 255; maps++ )
				if ( World.Lighting.LightStyles[fa.styles[maps]] != fa.cached_light[maps] )
				{
					flag = true;
					break;
				}

			if ( flag ||
				fa.dlightframe == World.Lighting.FrameCount || // dynamic this frame
				fa.cached_dlight )  // dynamic previously
			{
				if ( Host.Cvars.Dynamic.Get<Boolean>( ) )
					World.Lighting.SetDirty( fa );
			}
		}

		/// <summary>
		/// R_DrawBrushModel
		/// </summary>
		private void DrawBrushModel( Entity e )
		{
			_CurrentEntity = e;
			Host.DrawingContext.CurrentTexture = -1;

			var clmodel = ( BrushModelData ) e.model;
			var rotated = false;
			Vector3 mins, maxs;
			if ( e.angles.X != 0 || e.angles.Y != 0 || e.angles.Z != 0 )
			{
				rotated = true;
				mins = e.origin;
				mins.X -= clmodel.Radius;
				mins.Y -= clmodel.Radius;
				mins.Z -= clmodel.Radius;
				maxs = e.origin;
				maxs.X += clmodel.Radius;
				maxs.Y += clmodel.Radius;
				maxs.Z += clmodel.Radius;
			}
			else
			{
				mins = e.origin + clmodel.BoundsMin;
				maxs = e.origin + clmodel.BoundsMax;
			}

			if ( Utilities.CullBox( ref mins, ref maxs, ref _Frustum ) )
				return;

			Array.Clear( _LightMapPolys, 0, _LightMapPolys.Length );
			_ModelOrg = _RefDef.vieworg - e.origin;
			if ( rotated )
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
			if ( clmodel.FirstModelSurface != 0 && !Host.Cvars.glFlashBlend.Get<Boolean>( ) )
			{
				for ( var k = 0; k < ClientDef.MAX_DLIGHTS; k++ )
				{
					if ( ( Host.Client.DLights[k].die < Host.Client.cl.time ) || ( Host.Client.DLights[k].radius == 0 ) )
						continue;

					World.Lighting.MarkLights( Host.Client.DLights[k], 1 << k, clmodel.Nodes[clmodel.Hulls[0].firstclipnode] );
				}
			}

			Host.Video.Device.PushMatrix( );
			e.angles.X = -e.angles.X;   // stupid quake bug
			Host.Video.Device.RotateForEntity( e.origin, e.angles );
			e.angles.X = -e.angles.X;   // stupid quake bug

			var surfOffset = clmodel.FirstModelSurface;
			var psurf = clmodel.Surfaces; //[clmodel.firstmodelsurface];

			//
			// draw texture
			//
			for ( var i = 0; i < clmodel.NumModelSurfaces; i++, surfOffset++ )
			{
				// find which side of the node we are on
				var pplane = psurf[surfOffset].plane;

				var dot = Vector3.Dot( _ModelOrg, pplane.normal ) - pplane.dist;

				// draw the polygon
				var planeBack = ( psurf[surfOffset].flags & ( Int32 ) Q1SurfaceFlags.PlaneBack ) != 0;
				if ( ( planeBack && ( dot < -QDef.BACKFACE_EPSILON ) ) || ( !planeBack && ( dot > QDef.BACKFACE_EPSILON ) ) )
				{
					if ( Host.Cvars.glTexSort.Get<Boolean>( ) )
						RenderBrushPoly( psurf[surfOffset] );
					else
						DrawSequentialPoly( psurf[surfOffset] );
				}
			}

			World.Lighting.BlendLightmaps( );

			Host.Video.Device.PopMatrix( );
		}
	}

	//glRect_t;
}
