/// <copyright>
///
/// Rewritten in C# by Yury Kiselev, 2010.
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
using System.Runtime.InteropServices;
using System.Text;
using SharpQuake.Framework;

namespace SharpQuake
{
    [StructLayout( LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi )]
    internal struct wadinfo_t
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst=4)]
        public Byte[] identification; // [4];		// should be WAD2 or 2DAW

        public Int32 numlumps;
        public Int32 infotableofs;
    }

    /// <summary>
    /// W_functions
    /// </summary>
    internal static class Wad
    {
        public static Byte[] Data
        {
            get
            {
                return _Data;
            }
        }

        public static IntPtr DataPointer
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

        private static Byte[] _Data; // void* wad_base
        private static Dictionary<String, lumpinfo_t> _Lumps;
        private static GCHandle _Handle;
        private static IntPtr _DataPtr;

        // W_LoadWadFile (char *filename)
        public static void LoadWadFile( String filename )
        {
            _Data = FileSystem.LoadFile( filename );
            if( _Data == null )
                Utilities.Error( "Wad.LoadWadFile: couldn't load {0}", filename );

            if( _Handle.IsAllocated )
            {
                _Handle.Free();
            }
            _Handle = GCHandle.Alloc( _Data, GCHandleType.Pinned );
            _DataPtr = _Handle.AddrOfPinnedObject();

            wadinfo_t header = Utilities.BytesToStructure<wadinfo_t>( _Data, 0 );

            if( header.identification[0] != 'W' || header.identification[1] != 'A' ||
                header.identification[2] != 'D' || header.identification[3] != '2' )
                Utilities.Error( "Wad file {0} doesn't have WAD2 id\n", filename );

            var numlumps = Common.LittleLong( header.numlumps );
            var infotableofs = Common.LittleLong( header.infotableofs );
            var lumpInfoSize = Marshal.SizeOf( typeof( lumpinfo_t ) );

            _Lumps = new Dictionary<String, lumpinfo_t>( numlumps );

            for( var i = 0; i < numlumps; i++ )
            {
                IntPtr ptr = new IntPtr( _DataPtr.ToInt64() + infotableofs + i * lumpInfoSize );
                lumpinfo_t lump = (lumpinfo_t)Marshal.PtrToStructure( ptr, typeof( lumpinfo_t ) );
                lump.filepos = Common.LittleLong( lump.filepos );
                lump.size = Common.LittleLong( lump.size );
                if( lump.type == TYP_QPIC )
                {
                    ptr = new IntPtr( _DataPtr.ToInt64() + lump.filepos );
                    dqpicheader_t pic = (dqpicheader_t)Marshal.PtrToStructure( ptr, typeof( dqpicheader_t ) );
                    SwapPic( pic );
                    Marshal.StructureToPtr( pic, ptr, true );
                }
                _Lumps.Add( Encoding.ASCII.GetString( lump.name ).TrimEnd( '\0' ).ToLower(), lump );
            }
        }

        // lumpinfo_t *W_GetLumpinfo (char *name)
        public static lumpinfo_t GetLumpInfo( String name )
        {
            lumpinfo_t lump;
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
        public static Int32 GetLumpNameOffset( String name )
        {
            return GetLumpInfo( name ).filepos; // GetLumpInfo() never returns null
        }

        // SwapPic (qpic_t *pic)
        public static void SwapPic( dqpicheader_t pic )
        {
            pic.width = Common.LittleLong( pic.width );
            pic.height = Common.LittleLong( pic.height );
        }
    }

    [StructLayout( LayoutKind.Sequential, Pack = 1 )]
    internal class dqpicheader_t
    {
        public Int32 width, height;
    }

    //wadinfo_t;

    [StructLayout( LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi )]
    internal class lumpinfo_t
    {
        public Int32 filepos;
        public Int32 disksize;
        public Int32 size;                   // uncompressed
        public Byte type;
        public Byte compression;
        private Byte pad1, pad2;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public Byte[] name; //[16];				// must be null terminated
    } // lumpinfo_t;
}