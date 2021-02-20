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
using System.Drawing;
using SharpQuake.Framework;
using SharpQuake.Renderer.Textures;

namespace SharpQuake.Renderer
{
    public class Font : IDisposable
    {
        public BaseDevice Device
        {
            get;
            private set;
        }

        public String Name
        {
            get;
            private set;
        }

        public BaseTexture Texture
        {
            get;
            private set;
        }

        public Font( BaseDevice device, String name )
        {
            Device = device;
            Name = name;
        }

        public virtual void Initialise( ByteArraySegment buffer )
        {
            Texture = BaseTexture.FromBuffer( Device, Name, buffer, 128, 128, false, true, filter: "GL_NEAREST" );
        }

        public virtual Int32 Measure( String str )
        {
            return str.Length * 8;
        }

        // Draw_String
        public virtual void Draw( Int32 x, Int32 y, String str, Color? color = null )
        {
            for ( var i = 0; i < str.Length; i++, x += 8 )
                DrawCharacter( x, y, str[i], color );
        }

        // Draw_Character
        //
        // Draws one 8*8 graphics character with 0 being transparent.
        // It can be clipped to the top of the screen to allow the console to be
        // smoothly scrolled off.
        // Vertex color modification has no effect currently
        public virtual void DrawCharacter( Int32 x, Int32 y, Int32 num, Color? colour = null )
        {
            if ( num == 32 )
                return;		// space

            num &= 255;

            if ( y <= -8 )
                return;			// totally off screen

            var row = num >> 4;
            var col = num & 15;

            var frow = row * 0.0625f;
            var fcol = col * 0.0625f;
            var size = 0.0625f;

            Device.Graphics.DrawTexture2D( Texture,
                   new RectangleF( fcol, frow, size, size ), new Rectangle( x, y, 8, 8 ), colour );
        }

        public virtual void Dispose( )
        {
        }
    }
}
