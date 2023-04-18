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
using System.Linq;
using System.Text;
using SharpQuake.Framework.IO;
using SharpQuake.Framework.IO.WAD;

namespace SharpQuake.Framework.Factories.IO.WAD
{
    public class WadFactory : BaseFactory<String, Wad>
    {
        private Dictionary<String, String> Textures
		{
            get;
            set;
		}

        public WadFactory( ) : base()
        {
            Textures = new Dictionary<String, String>();
        }

        public void Initialise( )
        {
            Search();
        }

        private Boolean Load( String wadFile, out Wad wad )
        {
            var data = FileSystem.LoadFile( wadFile );

            if ( data == null )
            {
                wad = null;
                return false;
            }

            wad = new Wad();
            wad.LoadWadFile( wadFile, data );
            Add( wadFile, wad );
            return true;
        }

        private void AddTextures( String wadFile, Wad wad )
        {
            var textures = wad._Lumps.Values
                .Select( s => Encoding.ASCII.GetString( s.name ).Replace( "\0", "" ).ToLower() )
                .ToArray();

            foreach ( var texture in textures )
            {
                if ( !Textures.ContainsKey( texture ) )
                    Textures.Add( texture, wadFile );
            }
        }

        public WadLumpBuffer LoadTexture( String texture )
		{
            var wad = FromTexture( texture );

            if ( wad != null )
                return wad.GetLumpBuffer( texture );

            return null;
        }

        public Wad FromTexture( String texture )
        {
            var lowerName = texture.ToLower();

            if ( Textures.ContainsKey( lowerName ) )
            {
                var wadFile = Textures[lowerName];
                return Get( wadFile );
            }

            return null;
        }

        private void Search( )
        {
            foreach ( var wadFile in FileSystem.Search( "*.wad" ) )
            {
                if ( !Load( wadFile.FileName, out var wad ) )
                    continue;

                AddTextures( wadFile.FileName, wad );
            }
        }
    }
}
