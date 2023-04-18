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

using SharpQuake.Game.World;
using SharpQuake.Renderer;
using System;
using System.Collections.Generic;
using System.Text;

namespace SharpQuake.Rendering.Environment
{
    public class Sky
    {
        public Int32 TextureNumber
        {
            get;
            private set;
        }

        private readonly Host _host;

        public Sky( Host host )
        {
            _host = host;
        }

        public void Identify( )
        {
            // identify sky texture
            TextureNumber = -1;
            //_MirrorTextureNum = -1;
            var world = _host.Client.cl.worldmodel;
            for ( var i = 0; i < world.NumTextures; i++ )
            {
                if ( world.Textures[i] == null )
                    continue;

                if ( world.Textures[i].name != null )
                {
                    if ( world.Textures[i].name.StartsWith( "sky" ) )
                        TextureNumber = i;
                    //if( world.textures[i].name.StartsWith( "window02_1" ) )
                    //    _MirrorTextureNum = i;
                }
                world.Textures[i].texturechain = null;
            }
        }
    }
}
