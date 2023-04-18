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
/// 

using SharpQuake.Framework.Factories;
using SharpQuake.Renderer.Textures;
using System;
using SharpQuake.Framework;

namespace SharpQuake.Factories.Rendering.UI
{
    /// <summary>
    /// Factory to manage loading and caching images
    /// </summary>
    public class PictureFactory : BaseFactory<String, BasePicture>
	{
		private Host _host;

		public void Initialise( Host host )
        {
			_host = host;
		}

        /// <summary>
        /// TODO - Add proper texture cleanup
        /// </summary>
        public override void Dispose( )
        {
        }

        /// <summary>
        /// Get a picture from cache
        /// </summary>
        /// <remarks>
        /// (Loads from disk if not present)
        /// </remarks>
        /// <param name="textRenderer"></param>
        public BasePicture Cache( String path, String filter = "GL_LINEAR_MIPMAP_NEAREST", System.Boolean ignoreAtlas = false )
		{
			if ( Contains( path ) )
				return Get( path );

			if ( DictionaryItems.Count == DrawDef.MAX_CACHED_PICS )
				Utilities.Error( "menu_numcachepics == MAX_CACHED_PICS" );

			var picture = BasePicture.FromFile( _host.Video.Device, path, filter, ignoreAtlas );

			if ( picture != null )
				Add( path, picture );

			return picture;
		}
	}
}
