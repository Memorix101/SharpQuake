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
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace SharpQuake.Framework.IO
{
    /// <summary>
    /// W_functions
    /// </summary>
    public class Wad
    {
        public Byte[] Data
        {
            get
            {
                return _Data;
            }
        }

        public IntPtr DataPointer
        {
            get
            {
                return _DataPtr;
            }
        }

        public const Int32 CMP_NONE = 0;
        public const Int32 CMP_LZSS = 1;

        public const Int32 TYP_NONE = 0;
        public const Int32 TYP_LABEL = 1;

        public const Int32 TYP_LUMPY = 64;				// 64 + grab command number
        public const Int32 TYP_PALETTE = 64;
        public const Int32 TYP_QTEX = 65;
        public const Int32 TYP_QPIC = 66;
        public const Int32 TYP_SOUND = 67;
        public const Int32 TYP_MIPTEX = 68;

        private Byte[] _Data; // void* wad_base
        public Dictionary<String, WadLumpInfo> _Lumps
        {
            get;
            private set;
        }
        private GCHandle _Handle;
        private IntPtr _DataPtr;

        // W_LoadWadFile (char *filename)
        public void LoadWadFile( String filename )
        {
            LoadWadFile( filename, FileSystem.LoadFile( filename ) );
        }

        public void LoadWadFile( String filename, Byte[] buffer )
        {
            _Data = buffer;
            if ( _Data == null )
                Utilities.Error( "Wad.LoadWadFile: couldn't load {0}", filename );

            if ( _Handle.IsAllocated )
            {
                _Handle.Free( );
            }
            _Handle = GCHandle.Alloc( _Data, GCHandleType.Pinned );
            _DataPtr = _Handle.AddrOfPinnedObject( );

            var header = Utilities.BytesToStructure<WadInfo>( _Data, 0 );

            if ( header.identification[0] != 'W' || header.identification[1] != 'A' ||
                header.identification[2] != 'D' || header.identification[3] != '2' )
                Utilities.Error( "Wad file {0} doesn't have WAD2 id\n", filename );

            var numlumps = EndianHelper.LittleLong( header.numlumps );
            var infotableofs = EndianHelper.LittleLong( header.infotableofs );
            var lumpInfoSize = Marshal.SizeOf( typeof( WadLumpInfo ) );

            _Lumps = new Dictionary<String, WadLumpInfo>( numlumps );

            for ( var i = 0; i < numlumps; i++ )
            {
                var ptr = new IntPtr( _DataPtr.ToInt64( ) + infotableofs + i * lumpInfoSize );
                var lump = ( WadLumpInfo ) Marshal.PtrToStructure( ptr, typeof( WadLumpInfo ) );
                lump.filepos = EndianHelper.LittleLong( lump.filepos );
                lump.size = EndianHelper.LittleLong( lump.size );
                if ( lump.type == TYP_QPIC )
                {
                    ptr = new IntPtr( _DataPtr.ToInt64( ) + lump.filepos );
                    var pic = ( WadPicHeader ) Marshal.PtrToStructure( ptr, typeof( WadPicHeader ) );
                    EndianHelper.SwapPic( pic );
                    Marshal.StructureToPtr( pic, ptr, true );
                }
                _Lumps.Add( Encoding.ASCII.GetString( lump.name ).TrimEnd( '\0' ).ToLower( ), lump );
            }
        }

        // lumpinfo_t *W_GetLumpinfo (char *name)
        public WadLumpInfo GetLumpInfo( String name )
        {
            WadLumpInfo lump;
            if( _Lumps.TryGetValue( name, out lump ) )
            {
                return lump;
            }
            else
            {
                Utilities.Error( "W_GetLumpinfo: {0} not found", name );
            }
            // We must never be there
            throw new InvalidOperationException( "W_GetLumpinfo: Unreachable code reached!" );
        }

        // void	*W_GetLumpName (char *name)
        // Uze: returns index in _Data byte array where the lumpinfo_t starts
        public Int32 GetLumpNameOffset( String name )
        {
            return GetLumpInfo( name ).filepos; // GetLumpInfo() never returns null
        }

        public Tuple<Byte[], Size> GetLumpBuffer( String name )
        {
            var lump = _Lumps
                .Where( l => Encoding.ASCII.GetString( l.Value.name ).Replace( "\0", "" ) == name )
                .FirstOrDefault( );

            if ( lump.Value == null )
                return null;

            var lumpInfo = lump.Value;

            if ( lumpInfo.type != 0x44 )
                return null;

            var offset = lumpInfo.filepos;

            var ptr = new IntPtr( DataPointer.ToInt64( ) + offset );

            var header = ( BspMipTex ) Marshal.PtrToStructure( ptr, typeof( BspMipTex ) );
            var headerSize = BspMipTex.SizeInBytes;
            
            var width = EndianHelper.LittleLong( ( Int32 ) header.width );
            var height = EndianHelper.LittleLong( ( Int32 ) header.height );
            
            if ( ( width & 15 ) != 0 || ( height & 15 ) != 0 )
                Utilities.Error( "Texture {0} is not 16 aligned", name );
                        
            var pixelCount = ( Int32 ) ( width * height / 64 * 85 );
            var pixels = new Byte[pixelCount];  

#warning BlockCopy tries to copy data over the bounds of _ModBase if certain mods are loaded. Needs proof fix!
            if ( offset + BspMipTex.SizeInBytes + pixelCount <= Data.Length )
                System.Buffer.BlockCopy( Data, offset + BspMipTex.SizeInBytes, pixels, 0, pixelCount );
            else
            {
                System.Buffer.BlockCopy( Data, offset, pixels, 0, pixelCount );
                ConsoleWrapper.Print( $"Texture info of {name} truncated to fit in bounds of _ModBase\n" );
            }

            return new Tuple<Byte[], Size>( pixels, new Size( width, height ) );
        }
    }
}