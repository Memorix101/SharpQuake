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
using System.IO.Compression;

namespace SharpQuake.Framework.IO
{
    public class SearchPath
    {
        public String filename; // char[MAX_OSPATH];
        public Pak pack; // only one of filename / pack will be used
        public ZipArchive pk3;
        public String pk3filename;

        public SearchPath( String path )
        {
            if ( path.EndsWith( ".PAK" ) )
            {
                pack = FileSystem.LoadPackFile( path );
                if ( pack == null )
                    Utilities.Error( "Couldn't load packfile: {0}", path );
            }
            else if ( path.EndsWith( ".PK3" ) )
            {
                pk3 = ZipFile.OpenRead( path );
                pk3filename = path;
                if ( pk3 == null )
                    Utilities.Error( "Couldn't load pk3file: {0}", path );
            }
            else
                filename = path;
        }

        public SearchPath( Pak pak )
        {
            pack = pak;
        }

        public SearchPath( ZipArchive archive )
        {
            pk3 = archive;
        }
    } // searchpath_t;    
}
