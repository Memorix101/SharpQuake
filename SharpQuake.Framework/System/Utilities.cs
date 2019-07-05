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
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using global::SharpQuake.Framework;
using OpenTK;

//
// Source: common.h + common.c
//

// All of Quake's data access is through a hierarchical file system, but the contents of the file system can be transparently merged from several sources.
//
// The "base directory" is the path to the directory holding the quake.exe and all game directories.  The sys_* files pass this to host_init in quakeparms_t->basedir.  This can be overridden with the "-basedir" command line parm to allow code debugging in a different directory.  The base directory is
// only used during filesystem initialization.
//
// The "game directory" is the first tree on the search path and directory that all generated files (savegames, screenshots, demos, config files) will be saved to.  This can be overridden with the "-game" command line parameter.  The game directory can never be changed while quake is executing.  This is a precacution against having a malicious server instruct clients to write files over areas they shouldn't.
//
// The "cache directory" is only used during development to save network bandwidth, especially over ISDN / T1 lines.  If there is a cache directory
// specified, when a file is found by the normal search path, it will be mirrored
// into the cache directory, then opened there.
//
//
//
// FIXME:
// The file "parms.txt" will be read out of the game directory and appended to the current command line arguments to allow different games to initialize startup parms differently.  This could be used to add a "-sspeed 22050" for the high quality sound edition.  Because they are added at the end, they will not override an explicit setting on the original command line.

namespace SharpQuake.Framework
{
    public static class Utilities
    {
        public static Boolean IsWindows
        {
            get
            {
                var platform = Environment.OSVersion.Platform;
                return ( platform == PlatformID.Win32Windows || platform == PlatformID.Win32NT || platform == PlatformID.WinCE || platform == PlatformID.Xbox );
            }
        }

        public static Vector3 ZeroVector = Vector3.Zero;

        // for passing as reference
        public static Vector3f ZeroVector3f = default( Vector3f );

        private static readonly Byte[] ZeroBytes = new Byte[4096];

        public static Boolean SameText( String a, String b )
        {
            return ( String.Compare( a, b, true ) == 0 );
        }

        public static Boolean SameText( String a, String b, Int32 count )
        {
            return ( String.Compare( a, 0, b, 0, count, true ) == 0 );
        }

        public static void FillArray<T>( T[] dest, T value )
        {
            var elementSizeInBytes = Marshal.SizeOf( typeof( T ) );
            var blockSize = Math.Min( dest.Length, 4096 / elementSizeInBytes );

            for ( var i = 0; i < blockSize; i++ )
                dest[i] = value;

            var blockSizeInBytes = blockSize * elementSizeInBytes;
            var offset = blockSizeInBytes;
            var lengthInBytes = Buffer.ByteLength( dest );

            while ( true )// offset + blockSize <= lengthInBytes)
            {
                var left = lengthInBytes - offset;
                if ( left < blockSizeInBytes )
                    blockSizeInBytes = left;

                if ( blockSizeInBytes <= 0 )
                    break;

                Buffer.BlockCopy( dest, 0, dest, offset, blockSizeInBytes );
                offset += blockSizeInBytes;
            }
        }

        public static void ZeroArray<T>( T[] dest, Int32 startIndex, Int32 length )
        {
            var elementBytes = Marshal.SizeOf( typeof( T ) );
            var offset = startIndex * elementBytes;
            var sizeInBytes = dest.Length * elementBytes - offset;

            while ( true )
            {
                var blockSize = sizeInBytes - offset;
                if ( blockSize > ZeroBytes.Length )
                    blockSize = ZeroBytes.Length;

                if ( blockSize <= 0 )
                    break;

                Buffer.BlockCopy( ZeroBytes, 0, dest, offset, blockSize );
                offset += blockSize;
            }
        }

        public static String Copy( String src, Int32 maxLength )
        {
            if ( src == null )
                return null;

            return ( src.Length > maxLength ? src.Substring( 1, maxLength ) : src );
        }

        public static void Copy( Single[] src, out Vector3 dest )
        {
            dest.X = src[0];
            dest.Y = src[1];
            dest.Z = src[2];
        }

        public static void Copy( ref Vector3 src, Single[] dest )
        {
            dest[0] = src.X;
            dest[1] = src.Y;
            dest[2] = src.Z;
        }

        public static String GetString( Byte[] src )
        {
            var count = 0;

            while ( count < src.Length && src[count] != 0 )
                count++;

            return ( count > 0 ? Encoding.ASCII.GetString( src, 0, count ) : String.Empty );
        }

        public static Vector3 ToVector( ref Vector3f v )
        {
            return new Vector3( v.x, v.y, v.z );
        }

        public static void WriteInt( Byte[] dest, Int32 offset, Int32 value )
        {
            var u = Union4b.Empty;
            u.i0 = value;
            dest[offset + 0] = u.b0;
            dest[offset + 1] = u.b1;
            dest[offset + 2] = u.b2;
            dest[offset + 3] = u.b3;
        }

        /// <summary>
        /// Sys_Error
        /// an error will cause the entire program to exit
        /// </summary>
        public static void Error( String fmt, params Object[] args )
        {
            throw new QuakeSystemError( args.Length > 0 ? String.Format( fmt, args ) : fmt );
        }

        public static T ReadStructure<T>( Stream stream )
        {
            var count = Marshal.SizeOf( typeof( T ) );
            var buf = new Byte[count];

            if ( stream.Read( buf, 0, count ) < count )
                throw new IOException( "Stream reading error!" );

            return BytesToStructure<T>( buf, 0 );
        }
        
        public static void WriteString( BinaryWriter dest, String value )
        {
            var buf = Encoding.ASCII.GetBytes( value );
            dest.Write( buf.Length );
            dest.Write( buf );
        }

        public static String ReadString( BinaryReader src )
        {
            var length = src.ReadInt32( );

            if ( length <= 0 )
                throw new Exception( "Invalid string length: " + length.ToString( ) );

            var buf = new Byte[length];
            src.Read( buf, 0, length );

            return Encoding.ASCII.GetString( buf );
        }

        public static T BytesToStructure<T>( Byte[] src, Int32 startIndex )
        {
            var handle = GCHandle.Alloc( src, GCHandleType.Pinned );

            try
            {
                var ptr = handle.AddrOfPinnedObject( );
                if ( startIndex != 0 )
                {
                    var ptr2 = ptr.ToInt64( ) + startIndex;
                    ptr = new IntPtr( ptr2 );
                }
                return ( T ) Marshal.PtrToStructure( ptr, typeof( T ) );
            }
            finally
            {
                handle.Free( );
            }
        }

        public static Byte[] StructureToBytes<T>( ref T src )
        {
            var buf = new Byte[Marshal.SizeOf( typeof( T ) )];
            var handle = GCHandle.Alloc( buf, GCHandleType.Pinned );

            try
            {
                Marshal.StructureToPtr( src, handle.AddrOfPinnedObject( ), true );
            }
            finally
            {
                handle.Free( );
            }

            return buf;
        }

        public static void StructureToBytes<T>( ref T src, Byte[] dest, Int32 offset )
        {
            var handle = GCHandle.Alloc( dest, GCHandleType.Pinned );

            try
            {
                var addr = handle.AddrOfPinnedObject( ).ToInt64( ) + offset;
                Marshal.StructureToPtr( src, new IntPtr( addr ), true );
            }
            finally
            {
                handle.Free( );
            }
        }
    }
}