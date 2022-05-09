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

using SharpQuake.Renderer.Textures;
using System;

namespace SharpQuake.Rendering.UI.Elements.Warnings
{
    public class LagWarning : BaseUIElement
    {
        private BasePicture Picture
        {
            get;
            set;
        }

        /// <summary>
        /// Count from SCR_DrawTurtle()
        /// </summary>
        private Int32 TurtleCount
        {
            get;
            set;
        }

        public LagWarning( Host host ) : base( host )
        {
        }

        public override void Initialise()
        {
            Picture = BasePicture.FromWad( _host.Video.Device, _host.Wads.FromTexture( "turtle" ), "turtle", "GL_LINEAR" );

            HasInitialised = true;
        }

        /// <summary>
        /// SCR_DrawTurtle
        /// </summary>
        public override void Draw( )
        {
            base.Draw( );

            if ( !IsVisible || !HasInitialised )
                return;

            if ( !_host.Cvars.ShowTurtle.Get<Boolean>( ) )
                return;

            if ( _host.FrameTime < 0.1 )
            {
                TurtleCount = 0;
                return;
            }

            TurtleCount++;
            if ( TurtleCount < 3 )
                return;

            _host.Video.Device.Graphics.DrawPicture( Picture, _host.Screen.VRect.x, _host.Screen.VRect.y );
        }
    }
}
