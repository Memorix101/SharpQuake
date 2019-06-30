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
    public enum GameKind
    {
        StandardQuake, Rogue, Hipnotic
    }

    public static class Utilities
    {
        public static Boolean IsWindows
        {
            get
            {
                PlatformID platform = Environment.OSVersion.Platform;
                return ( platform == PlatformID.Win32Windows || platform == PlatformID.Win32NT || platform == PlatformID.WinCE || platform == PlatformID.Xbox );
            }
        }

        public static Boolean IsBigEndian
        {
            get
            {
                return !BitConverter.IsLittleEndian;
            }
        }

        public static GameKind GameKind
        {
            get
            {
                return _GameKind;
            }
        }

        public static Int32 Argc
        {
            get
            {
                return _Argv.Length;
            }
        }

        public static String[] Args
        {
            get
            {
                return _Argv;
            }
            set
            {
                _Argv = new String[value.Length];
                value.CopyTo( _Argv, 0 );
                _Args = String.Join( " ", value );
            }
        }

        public static String Token
        {
            get
            {
                return _Token;
            }
        }

        public static Vector3 ZeroVector = Vector3.Zero;

        // for passing as reference
        public static Vector3f ZeroVector3f = default( Vector3f );

        private static readonly Byte[] ZeroBytes = new Byte[4096];

        // this graphic needs to be in the pak file to use registered features
        private static UInt16[] _Pop = new UInt16[]
        {
            0x0000,0x0000,0x0000,0x0000,0x0000,0x0000,0x0000,0x0000
        ,0x0000,0x0000,0x6600,0x0000,0x0000,0x0000,0x6600,0x0000
        ,0x0000,0x0066,0x0000,0x0000,0x0000,0x0000,0x0067,0x0000
        ,0x0000,0x6665,0x0000,0x0000,0x0000,0x0000,0x0065,0x6600
        ,0x0063,0x6561,0x0000,0x0000,0x0000,0x0000,0x0061,0x6563
        ,0x0064,0x6561,0x0000,0x0000,0x0000,0x0000,0x0061,0x6564
        ,0x0064,0x6564,0x0000,0x6469,0x6969,0x6400,0x0064,0x6564
        ,0x0063,0x6568,0x6200,0x0064,0x6864,0x0000,0x6268,0x6563
        ,0x0000,0x6567,0x6963,0x0064,0x6764,0x0063,0x6967,0x6500
        ,0x0000,0x6266,0x6769,0x6a68,0x6768,0x6a69,0x6766,0x6200
        ,0x0000,0x0062,0x6566,0x6666,0x6666,0x6666,0x6562,0x0000
        ,0x0000,0x0000,0x0062,0x6364,0x6664,0x6362,0x0000,0x0000
        ,0x0000,0x0000,0x0000,0x0062,0x6662,0x0000,0x0000,0x0000
        ,0x0000,0x0000,0x0000,0x0061,0x6661,0x0000,0x0000,0x0000
        ,0x0000,0x0000,0x0000,0x0000,0x6500,0x0000,0x0000,0x0000
        ,0x0000,0x0000,0x0000,0x0000,0x6400,0x0000,0x0000,0x0000
        };

        // for passing as reference
        private static String[] safeargvs = new String[]
        {
        "-stdvid", "-nolan", "-nosound", "-nocdaudio", "-nojoy", "-nomouse", "-dibonly"
        };

        private static IByteOrderConverter _Converter;
        public static String[] _Argv;
        public static String _Args; // com_cmdline
        private static GameKind _GameKind; // qboolean		standard_quake = true, rogue, hipnotic;
        private static Char[] _Slashes = new Char[] { '/', '\\' };
        private static String _Token; // com_token

        public static String Argv( Int32 index )
        {
            return _Argv[index];
        }

        // int COM_CheckParm (char *parm)
        // Returns the position (1 to argc-1) in the program's argument list
        // where the given parameter apears, or 0 if not present
        public static Int32 CheckParm( String parm )
        {
            for ( var i = 1; i < _Argv.Length; i++ )
            {
                if ( _Argv[i].Equals( parm ) )
                    return i;
            }
            return 0;
        }

        public static Boolean HasParam( String parm )
        {
            return ( CheckParm( parm ) > 0 );
        }

        /// <summary>
        /// COM_Parse
        /// Parse a token out of a string
        /// </summary>
        public static String Parse( String data )
        {
            _Token = String.Empty;

            if ( String.IsNullOrEmpty( data ) )
                return null;

            // skip whitespace
            var i = 0;
            while ( i < data.Length )
            {
                while ( i < data.Length )
                {
                    if ( data[i] > ' ' )
                        break;

                    i++;
                }

                if ( i >= data.Length )
                    return null;

                // skip // comments
                if ( ( data[i] == '/' ) && ( i + 1 < data.Length ) && ( data[i + 1] == '/' ) )
                {
                    while ( i < data.Length && data[i] != '\n' )
                        i++;
                }
                else
                    break;
            }

            if ( i >= data.Length )
                return null;

            var i0 = i;

            // handle quoted strings specially
            if ( data[i] == '\"' )
            {
                i++;
                i0 = i;
                while ( i < data.Length && data[i] != '\"' )
                    i++;

                if ( i == data.Length )
                {
                    _Token = data.Substring( i0, i - i0 );
                    return null;
                }
                else
                {
                    _Token = data.Substring( i0, i - i0 );
                    return ( i + 1 < data.Length ? data.Substring( i + 1 ) : null );
                }
            }

            // parse single characters
            var c = data[i];
            if ( c == '{' || c == '}' || c == ')' || c == '(' || c == '\'' || c == ':' )
            {
                _Token = data.Substring( i, 1 );
                return ( i + 1 < data.Length ? data.Substring( i + 1 ) : null );
            }

            // parse a regular word
            while ( i < data.Length )
            {
                c = data[i];
                if ( c <= 32 || c == '{' || c == '}' || c == ')' || c == '(' || c == '\'' || c == ':' )
                {
                    i--;
                    break;
                }
                i++;
            }

            if ( i == data.Length )
            {
                _Token = data.Substring( i0, i - i0 );
                return null;
            }

            _Token = data.Substring( i0, i - i0 + 1 );
            return ( i + 1 < data.Length ? data.Substring( i + 1 ) : null );
        }

        public static Int32 atoi( String s )
        {
            if ( String.IsNullOrEmpty( s ) )
                return 0;

            var sign = 1;
            var result = 0;
            var offset = 0;
            if ( s.StartsWith( "-" ) )
            {
                sign = -1;
                offset++;
            }

            var i = -1;

            if ( s.Length > 2 )
            {
                i = s.IndexOf( "0x", offset, 2 );
                if ( i == -1 )
                {
                    i = s.IndexOf( "0X", offset, 2 );
                }
            }

            if ( i == offset )
            {
                Int32.TryParse( s.Substring( offset + 2 ), System.Globalization.NumberStyles.HexNumber, null, out result );
            }
            else
            {
                i = s.IndexOf( '\'', offset, 1 );
                if ( i != -1 )
                {
                    result = ( Byte ) s[i + 1];
                }
                else
                    Int32.TryParse( s.Substring( offset ), out result );
            }
            return sign * result;
        }

        public static Single atof( String s )
        {
            Single v;
            Single.TryParse( s, NumberStyles.Float, CultureInfo.InvariantCulture.NumberFormat, out v );
            return v;
        }

        public static Boolean SameText( String a, String b )
        {
            return ( String.Compare( a, b, true ) == 0 );
        }

        public static Boolean SameText( String a, String b, Int32 count )
        {
            return ( String.Compare( a, 0, b, 0, count, true ) == 0 );
        }

        public static Int16 BigShort( Int16 l )
        {
            return _Converter.BigShort( l );
        }

        public static Int16 LittleShort( Int16 l )
        {
            return _Converter.LittleShort( l );
        }

        public static Int32 BigLong( Int32 l )
        {
            return _Converter.BigLong( l );
        }

        public static Int32 LittleLong( Int32 l )
        {
            return _Converter.LittleLong( l );
        }

        public static Single BigFloat( Single l )
        {
            return _Converter.BigFloat( l );
        }

        public static Single LittleFloat( Single l )
        {
            return _Converter.LittleFloat( l );
        }

        public static Vector3 LittleVector( Vector3 src )
        {
            return new Vector3( _Converter.LittleFloat( src.X ),
                _Converter.LittleFloat( src.Y ), _Converter.LittleFloat( src.Z ) );
        }

        public static Vector3 LittleVector3( Single[] src )
        {
            return new Vector3( _Converter.LittleFloat( src[0] ),
                _Converter.LittleFloat( src[1] ), _Converter.LittleFloat( src[2] ) );
        }

        public static Vector4 LittleVector4( Single[] src, Int32 offset )
        {
            return new Vector4( _Converter.LittleFloat( src[offset + 0] ),
                _Converter.LittleFloat( src[offset + 1] ),
                _Converter.LittleFloat( src[offset + 2] ),
                _Converter.LittleFloat( src[offset + 3] ) );
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
            Union4b u = Union4b.Empty;
            u.i0 = value;
            dest[offset + 0] = u.b0;
            dest[offset + 1] = u.b1;
            dest[offset + 2] = u.b2;
            dest[offset + 3] = u.b3;
        }

        static Utilities( )
        {
            // set the byte swapping variables in a portable manner
            if ( BitConverter.IsLittleEndian )
            {
                _Converter = new LittleEndianConverter( );
            }
            else
            {
                _Converter = new BigEndianConverter( );
            }
        }

        // void COM_Init (char *path)
        public static void Init( String path, String[] argv )
        {
            _Argv = argv;
        }

        // void COM_InitArgv (int argc, char **argv)
        public static void InitArgv( String[] argv )
        {
            // reconstitute the command line for the cmdline externally visible cvar
            _Args = String.Join( " ", argv );
            _Argv = new String[argv.Length];
            argv.CopyTo( _Argv, 0 );

            var safe = false;
            foreach ( var arg in _Argv )
            {
                if ( arg == "-safe" )
                {
                    safe = true;
                    break;
                }
            }

            if ( safe )
            {
                // force all the safe-mode switches. Note that we reserved extra space in
                // case we need to add these, so we don't need an overflow check
                String[] largv = new String[_Argv.Length + safeargvs.Length];
                _Argv.CopyTo( largv, 0 );
                safeargvs.CopyTo( largv, _Argv.Length );
                _Argv = largv;
            }

            _GameKind = GameKind.StandardQuake;

            if ( HasParam( "-rogue" ) )
                _GameKind = GameKind.Rogue;

            if ( HasParam( "-hipnotic" ) )
                _GameKind = GameKind.Hipnotic;
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
            Byte[] buf = new Byte[count];
            if ( stream.Read( buf, 0, count ) < count )
            {
                throw new IOException( "Stream reading error!" );
            }
            return BytesToStructure<T>( buf, 0 );
        }


        public static void WriteString( BinaryWriter dest, String value )
        {
            Byte[] buf = Encoding.ASCII.GetBytes( value );
            dest.Write( buf.Length );
            dest.Write( buf );
        }

        public static String ReadString( BinaryReader src )
        {
            var length = src.ReadInt32( );
            if ( length <= 0 )
            {
                throw new Exception( "Invalid string length: " + length.ToString( ) );
            }
            Byte[] buf = new Byte[length];
            src.Read( buf, 0, length );
            return Encoding.ASCII.GetString( buf );
        }


        public static T BytesToStructure<T>( Byte[] src, Int32 startIndex )
        {
            GCHandle handle = GCHandle.Alloc( src, GCHandleType.Pinned );
            try
            {
                IntPtr ptr = handle.AddrOfPinnedObject( );
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
            Byte[] buf = new Byte[Marshal.SizeOf( typeof( T ) )];
            GCHandle handle = GCHandle.Alloc( buf, GCHandleType.Pinned );
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
            GCHandle handle = GCHandle.Alloc( dest, GCHandleType.Pinned );
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