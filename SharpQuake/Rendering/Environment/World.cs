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
    public class World
    {
        public Entity WorldEntity
        {
            get;
            private set; 
        }

        public Occlusion Occlusion
        {
            get;
            private set;
        }

        public ParticleSystem Particles
        {
            get;
            private set;
        }

        public Lighting Lighting
        {
            get;
            private set;
        }

        public Sky Sky
        {
            get;
            private set;
        }

        private readonly Host _host;

        public World( Host host ) 
        { 
            _host = host;

            Sky = new Sky( host );
            Lighting = new Lighting( host );
            WorldEntity = new Entity( );
            Particles = new ParticleSystem( _host.Video.Device );
        }

        public void Initialise( TextureChains textureChains )
        {
            Occlusion = new Occlusion( _host, textureChains );
        }

        /// <summary>
        /// R_NewMap
        /// </summary>
        public void NewMap()
        {
            Lighting.Reset( );

            WorldEntity.Clear( );
            WorldEntity.model = _host.Client.cl.worldmodel;

            // clear out efrags in case the level hasn't been reloaded
            // FIXME: is this one short?
            for ( var i = 0; i < _host.Client.cl.worldmodel.NumLeafs; i++ )
                _host.Client.cl.worldmodel.Leaves[i].efrags = null;

            Occlusion.ViewLeaf = null;
            Particles.Clear( );

            Lighting.BuildLightMaps( );

            Sky.Identify( );
        }
    }
}
