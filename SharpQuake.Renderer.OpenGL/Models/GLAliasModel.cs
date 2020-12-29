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
using SharpQuake.Framework.Mathematics;
using SharpQuake.Renderer.Models;
using OpenTK.Graphics.OpenGL;
using SharpQuake.Framework;

namespace SharpQuake.Renderer.OpenGL.Models
{
	public class GLAliasModel : BaseAliasModel
	{
		public GLAliasModel( BaseDevice device, BaseAliasModelDesc desc ) : base( device, desc )
		{
		}

		public override void DrawAliasModel( Single shadeLight, Vector3 shadeVector, Single[] shadeDots, Single lightSpotZ, Double realTime, Double time, ref Int32 poseNum, ref Int32 poseNum2, ref Single frameStartTime, ref Single frameInterval, ref Vector3 origin1, ref Vector3 origin2, ref Single translateStartTime, ref Vector3 angles1, ref Vector3 angles2, ref Single rotateStartTime, System.Boolean shadows = true, System.Boolean smoothModels = true, System.Boolean affineModels = false, System.Boolean noColours = false, System.Boolean isEyes = false, System.Boolean useInterpolation = true )
		{
			Device.DisableMultitexture( );

			GL.Enable( EnableCap.Texture2D );

			GL.PushMatrix( );

			if ( useInterpolation )
				Device.BlendedRotateForEntity( Desc.Origin, Desc.EulerAngles, realTime, ref origin1, ref origin2, ref translateStartTime, ref angles1, ref angles2, ref rotateStartTime );
			else
				Device.RotateForEntity( Desc.Origin, Desc.EulerAngles );

			if ( isEyes )
			{
				var v = Desc.ScaleOrigin;
				v.Z -= ( 22 + 8 );
				GL.Translate( v.X, v.Y, v.Z );
				// double size of eyes, since they are really hard to see in gl
				var s = Desc.Scale * 2.0f;
				GL.Scale( s.X, s.Y, s.Z );
			}
			else
			{
				GL.Translate( Desc.ScaleOrigin.X, Desc.ScaleOrigin.Y, Desc.ScaleOrigin.Z );
				GL.Scale( Desc.Scale.X, Desc.Scale.Y, Desc.Scale.Z );
			}

			var anim = ( Int32 ) ( time * 10 ) & 3;
			//var texture = Host.Model.SkinTextures[paliashdr.gl_texturenum[_CurrentEntity.skinnum, anim]];
			Desc.Texture.Bind( );

			// we can't dynamically colormap textures, so they are cached
			// seperately for the players.  Heads are just uncolored.
			//if ( _CurrentEntity.colormap != Host.Screen.vid.colormap && !noColours && playernum >= 1 )
			//{
			//    PlayerTextures[playernum > 0 ? playernum - 1 : playernum].Bind( );
			//    //Host.DrawingContext.Bind( _PlayerTextures - 1 + playernum );
			//}

			if ( smoothModels )
				GL.ShadeModel( ShadingModel.Smooth );

			GL.TexEnv( TextureEnvTarget.TextureEnv, TextureEnvParameter.TextureEnvMode, ( Int32 ) TextureEnvMode.Modulate );

			if ( affineModels )
				GL.Hint( HintTarget.PerspectiveCorrectionHint, HintMode.Fastest );

			//SetupAliasFrame( shadeLight, Desc.AliasFrame, time, paliashdr, shadeDots );
			SetupAliasBlendedFrame( shadeLight, AliasDesc.AliasFrame, realTime, time, shadeDots, ref poseNum, ref poseNum2, ref frameStartTime, ref frameInterval );

			GL.TexEnv( TextureEnvTarget.TextureEnv, TextureEnvParameter.TextureEnvMode, ( Int32 ) TextureEnvMode.Replace );

			GL.ShadeModel( ShadingModel.Flat );
			if ( affineModels )
				GL.Hint( HintTarget.PerspectiveCorrectionHint, HintMode.Nicest );

			GL.PopMatrix( );

			if ( shadows )
			{
				GL.PushMatrix( );
				Device.RotateForEntity( Desc.Origin, Desc.EulerAngles );
				GL.Disable( EnableCap.Texture2D );
				GL.Enable( EnableCap.Blend );
				GL.Color4( 0, 0, 0, 0.5f );
				DrawAliasShadow( AliasDesc.LastPoseNumber, lightSpotZ, shadeVector );
				GL.Enable( EnableCap.Texture2D );
				GL.Disable( EnableCap.Blend );
				GL.Color4( 1f, 1, 1, 1 );
				GL.PopMatrix( );
			}

			GL.Disable( EnableCap.Texture2D );
		}

		protected override void DrawAliasShadow( Int32 posenum, Single lightSpotZ, Vector3 shadeVector )
		{
			var lheight = Desc.Origin.Z - lightSpotZ;
			Single height = 0;
			var verts = AliasDesc.AliasHeader.posedata;
			var voffset = posenum * AliasDesc.AliasHeader.poseverts;
			var order = AliasDesc.AliasHeader.commands;

			height = -lheight + 1.0f;
			var orderOffset = 0;

			while ( true )
			{
				// get the vertex count and primitive type
				var count = order[orderOffset++];
				if ( count == 0 )
					break;      // done

				if ( count < 0 )
				{
					count = -count;
					GL.Begin( PrimitiveType.TriangleFan );
				}
				else
					GL.Begin( PrimitiveType.TriangleStrip );

				do
				{
					// texture coordinates come from the draw list
					// (skipped for shadows) glTexCoord2fv ((float *)order);
					orderOffset += 2;

					// normals and vertexes come from the frame list
					var point = new Vector3(
						verts[voffset].v[0] * AliasDesc.AliasHeader.scale.X + AliasDesc.AliasHeader.scale_origin.X,
						verts[voffset].v[1] * AliasDesc.AliasHeader.scale.Y + AliasDesc.AliasHeader.scale_origin.Y,
						verts[voffset].v[2] * AliasDesc.AliasHeader.scale.Z + AliasDesc.AliasHeader.scale_origin.Z
					);

					point.X -= shadeVector.X * ( point.Z + lheight );
					point.Y -= shadeVector.Y * ( point.Z + lheight );
					point.Z = height;

					GL.Vertex3( point.X, point.Y, point.Z );

					voffset++;
				} while ( --count > 0 );

				GL.End( );
			}
		}

		/*
         =============
         GL_DrawAliasBlendedFrame

         fenix@io.com: model animation interpolation
         =============
         */

		protected override void DrawAliasBlendedFrame( Single shadeLight, Single[] shadeDots, Int32 posenum, Int32 posenum2, Single blend )
		{
			AliasDesc.LastPoseNumber0 = posenum;
			AliasDesc.LastPoseNumber = posenum2;

			var verts = AliasDesc.AliasHeader.posedata;
			var vert2 = verts;
			var vertsOffset = posenum * AliasDesc.AliasHeader.poseverts;

			var verts2Offset = posenum2 * AliasDesc.AliasHeader.poseverts;

			var order = AliasDesc.AliasHeader.commands;
			var orderOffset = 0;

			while ( true )
			{
				if ( orderOffset >= order.Length )
					break;

				// get the vertex count and primitive type
				var count = order[orderOffset];
				orderOffset++;
				if ( count == 0 )
					break;      // done

				if ( count < 0 )
				{
					count = -count;
					GL.Begin( PrimitiveType.TriangleFan );
				}
				else
					GL.Begin( PrimitiveType.TriangleStrip );

				Union4b u1 = Union4b.Empty, u2 = Union4b.Empty;
				do
				{
					if ( orderOffset + 1 >= order.Length )
						break;
					// texture coordinates come from the draw list
					u1.i0 = order[orderOffset + 0];
					u2.i0 = order[orderOffset + 1];
					orderOffset += 2;
					GL.TexCoord2( u1.f0, u2.f0 );

					// normals and vertexes come from the frame list
					// blend the light intensity from the two frames together
					Vector3 d = Vector3.Zero;

					if ( vertsOffset >= verts.Length )
					{
						count = 0;
						break;
					}

					d.X = shadeDots[vert2[verts2Offset].lightnormalindex] -
						   shadeDots[verts[vertsOffset].lightnormalindex];

					//var l = shadeDots[verts[vertsOffset].lightnormalindex] * shadeLight;
					var l = shadeLight * ( shadeDots[verts[vertsOffset].lightnormalindex] + ( blend * d.X ) );
					GL.Color3( l, l, l );

					var v = new Vector3( verts[vertsOffset].v[0], verts[vertsOffset].v[1], verts[vertsOffset].v[2] );
					var v2 = new Vector3( vert2[verts2Offset].v[0], vert2[verts2Offset].v[1], verts[verts2Offset].v[2] );
					d = v2 - v;

					GL.Vertex3( ( Single ) verts[vertsOffset].v[0] + ( blend * d[0] ), verts[vertsOffset].v[1] + ( blend * d[1] ), verts[vertsOffset].v[2] + ( blend * d[2] ) );
					vertsOffset++;
					verts2Offset++;
				} while ( --count > 0 );
				GL.End( );
			}
		}

		/// <summary>
		/// GL_DrawAliasFrame
		/// </summary>
		protected override void DrawAliasFrame( Single shadeLight, Single[] shadeDots, Int32 posenum )
		{
			AliasDesc.LastPoseNumber = posenum;

			var verts = AliasDesc.AliasHeader.posedata;
			var vertsOffset = posenum * AliasDesc.AliasHeader.poseverts;
			var order = AliasDesc.AliasHeader.commands;
			var orderOffset = 0;

			while ( true )
			{
				// get the vertex count and primitive type
				var count = order[orderOffset++];
				if ( count == 0 )
					break;      // done

				if ( count < 0 )
				{
					count = -count;
					GL.Begin( PrimitiveType.TriangleFan );
				}
				else
					GL.Begin( PrimitiveType.TriangleStrip );

				Union4b u1 = Union4b.Empty, u2 = Union4b.Empty;
				do
				{
					// texture coordinates come from the draw list
					u1.i0 = order[orderOffset + 0];
					u2.i0 = order[orderOffset + 1];
					orderOffset += 2;
					GL.TexCoord2( u1.f0, u2.f0 );

					// normals and vertexes come from the frame list
					var l = shadeDots[verts[vertsOffset].lightnormalindex] * shadeLight;
					GL.Color3( l, l, l );
					GL.Vertex3( ( Single ) verts[vertsOffset].v[0], verts[vertsOffset].v[1], verts[vertsOffset].v[2] );
					vertsOffset++;
				} while ( --count > 0 );
				GL.End( );
			}
		}
	}
}
