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
    public class GLModel : BaseModel
    {
        public GLModel( BaseDevice device, BaseModelDesc desc ) : base( device, desc )
        {
        }
        
        public override void DrawAliasModel( Single shadeLight, Vector3 shadeVector, Single[] shadeDots, Single lightSpotZ, aliashdr_t paliashdr, Double time, System.Boolean shadows = true, System.Boolean smoothModels = true, System.Boolean affineModels = false, System.Boolean noColours = false, System.Boolean isEyes = false )
        {
            Device.DisableMultitexture( );

            GL.Enable( EnableCap.Texture2D );

            GL.PushMatrix( );

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

            SetupAliasFrame( shadeLight, Desc.AliasFrame, time, paliashdr, shadeDots );

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
                DrawAliasShadow( paliashdr, Desc.LastPoseNumber, lightSpotZ, shadeVector );
                GL.Enable( EnableCap.Texture2D );
                GL.Disable( EnableCap.Blend );
                GL.Color4( 1f, 1, 1, 1 );
                GL.PopMatrix( );
            }

            GL.Disable( EnableCap.Texture2D );
        }

        protected override void DrawAliasShadow( aliashdr_t paliashdr, Int32 posenum, Single lightSpotZ, Vector3 shadeVector )
        {
            var lheight = Desc.Origin.Z - lightSpotZ;
            Single height = 0;
            var verts = paliashdr.posedata;
            var voffset = posenum * paliashdr.poseverts;
            var order = paliashdr.commands;

            height = -lheight + 1.0f;
            var orderOffset = 0;

            while ( true )
            {
                // get the vertex count and primitive type
                var count = order[orderOffset++];
                if ( count == 0 )
                    break;		// done

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
                        verts[voffset].v[0] * paliashdr.scale.X + paliashdr.scale_origin.X,
                        verts[voffset].v[1] * paliashdr.scale.Y + paliashdr.scale_origin.Y,
                        verts[voffset].v[2] * paliashdr.scale.Z + paliashdr.scale_origin.Z
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
        /// <summary>
        /// GL_DrawAliasFrame
        /// </summary>
        protected override void DrawAliasFrame( Single shadeLight, Single[] shadeDots, aliashdr_t paliashdr, Int32 posenum )
        {
            Desc.LastPoseNumber = posenum;

            var verts = paliashdr.posedata;
            var vertsOffset = posenum * paliashdr.poseverts;
            var order = paliashdr.commands;
            var orderOffset = 0;

            while ( true )
            {
                // get the vertex count and primitive type
                var count = order[orderOffset++];
                if ( count == 0 )
                    break;		// done

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
