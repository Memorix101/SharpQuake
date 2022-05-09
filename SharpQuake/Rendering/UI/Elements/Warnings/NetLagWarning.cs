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
    public class NetLagWarning : BaseUIElement
    {
        private BasePicture Picture
        {
            get;
            set;
        }

        public NetLagWarning( Host host ) : base( host )
        {
        }

        public override void Initialise()
        {
            Picture = BasePicture.FromWad( _host.Video.Device, _host.Wads.FromTexture( "net" ), "net", "GL_LINEAR" );
            HasInitialised = true;
        }

        /// <summary>
        /// SCR_DrawNet
        /// </summary>
        public override void Draw( )
        {
            base.Draw( );

            if ( !IsVisible || !HasInitialised )
                return;

            if ( _host.RealTime - _host.Client.cl.last_received_message < 0.3 )
                return;

            if ( _host.Client.cls.demoplayback )
                return;

            _host.Video.Device.Graphics.DrawPicture( Picture, _host.Screen.VRect.x + 64, _host.Screen.VRect.y );
        }
    }
}
