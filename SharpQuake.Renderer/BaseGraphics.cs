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
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpQuake.Framework;
using SharpQuake.Framework.Mathematics;
using SharpQuake.Renderer.Textures;

namespace SharpQuake.Renderer
{
    public class BaseGraphics : IDisposable
    {
        public BaseDevice Device
        {
            get;
            private set;
        }

        protected BaseTexture CurrentParticleTexture
        {
            get;
            private set;
        }

        public BaseGraphics( BaseDevice device )
        {
            Device = device;
        }

        public virtual void Initialise()
        {
            //throw new NotImplementedException( );
        }

        public virtual void Dispose( )
        {
            //throw new NotImplementedException( );
        }

        public virtual void DrawTexture2D( BaseTexture texture, Int32 x, Int32 y, Color? colour = null, Boolean hasAlpha = false )
        {
            DrawTexture2D( texture, x, y, texture.Desc.Width, texture.Desc.Height, colour, hasAlpha );
        }

        public virtual void DrawTexture2D( BaseTexture texture, Int32 x, Int32 y, Int32 width, Int32 height, Color? colour = null, Boolean hasAlpha = false )
        {
            DrawTexture2D( texture, new Rectangle( x, y, width, height ), colour, hasAlpha );
        }

        public virtual void DrawTexture2D( BaseTexture texture, Rectangle destRect, Color? colour = null, Boolean hasAlpha = false )
        {
            var srcRectF = new RectangleF( );
            srcRectF.X = 0;
            srcRectF.Y = 0;
            srcRectF.Width = 1;
            srcRectF.Height = 1;

            DrawTexture2D( texture, srcRectF, destRect, colour, hasAlpha );
        }

        public virtual void DrawTexture2D( BaseTexture texture, RectangleF sourceRect, Int32 x, Int32 y, Color? colour = null, Boolean hasAlpha = false )
        {
            DrawTexture2D( texture, sourceRect, new Rectangle( x, y, texture.Desc.Width, texture.Desc.Height ), colour, hasAlpha );
        }

        public virtual void DrawTexture2D( BaseTexture texture, RectangleF sourceRect, Rectangle destRect, Color? colour = null, Boolean hasAlpha = false )
        {
            throw new NotImplementedException( );
        }

        public virtual void DrawPicture( BasePicture picture, Int32 x, Int32 y, Color? colour = null, Boolean hasAlpha = false )
        {
            if ( Device.TextureAtlas.IsDirty )
                Device.TextureAtlas.Upload( );

            DrawTexture2D( picture.Texture, picture.Source, new Rectangle( x, y, picture.Width, picture.Height ), colour, hasAlpha );
        }

        public virtual void DrawPicture( BasePicture picture, Int32 x, Int32 y, Int32 width, Int32 height, Color? colour = null, Boolean hasAlpha = false )
        {
            if ( Device.TextureAtlas.IsDirty )
                Device.TextureAtlas.Upload( );

            DrawTexture2D( picture.Texture, picture.Source, new Rectangle( x, y, width, height ), colour, hasAlpha );
        }
        
        public virtual void BeginParticles( BaseTexture texture )
        {
            CurrentParticleTexture = texture;
        }

        public virtual void DrawParticle( Single colour, Vector3 up, Vector3 right, Vector3 origin, Single scale )
        {
            throw new NotImplementedException( );
        }

        public virtual void EndParticles( )
        {
            CurrentParticleTexture = null;
        }

        /// <summary>
        /// EmitSkyPolys
        /// </summary>
        public virtual void EmitSkyPolys( GLPoly polys, Vector3 origin, Single speed, Boolean blend = false )
        {
            throw new NotImplementedException( );
        }

        public virtual void DrawPoly( GLPoly p, Single scaleX = 1f, Single scaleY = 1f, Boolean isLightmap = false )
        {
            throw new NotImplementedException( );
        }

        /// <summary>
        /// EmitWaterPolys
        /// Does a water warp on the pre-fragmented glpoly_t chain
        /// </summary>
        public virtual void EmitWaterPolys( ref Single[] turbSin, Double time, Double turbScale, GLPoly polys )
        {
            throw new NotImplementedException( );
        }

        public virtual void DrawWaterPoly( GLPoly p, Double time )
        {
            throw new NotImplementedException( );
        }

        public virtual void DrawWaterPolyLightmap( GLPoly p, Double time, Boolean blend = false )
        {
            throw new NotImplementedException( );
        }

        public virtual void Fill( Int32 x, Int32 y, Int32 width, Int32 height, Color color )
        {
            throw new NotImplementedException( );
        }

        public virtual void DrawTransTranslate( BaseTexture texture, Int32 x, Int32 y, Int32 width, Int32 height, Byte[] translation )
        {
            throw new NotImplementedException( );
        }

        public virtual void PolyBlend( Color4 colour )
        {
            throw new NotImplementedException( );
        }

        // Draw_Fill
        //
        // Fills a box of pixels with a single color
        public virtual void FillUsingPalette( Int32 x, Int32 y, Int32 width, Int32 height, Int32 colour )
        {
            Fill( x, y, width, height, Device.Palette.ToColour( colour ) );
        }

        public virtual void FadeScreen()
        {
            throw new NotImplementedException( );
        }
    }
}
