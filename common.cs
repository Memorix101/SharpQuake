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
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using OpenTK;

//
// Source: common.h + common.c
//

// All of Quake's data access is through a hierchal file system, but the contents of the file system can be transparently merged from several sources.
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

namespace SharpQuake
{
    internal enum GameKind
    {
        StandardQuake, Rogue, Hipnotic
    }

    //
    // on disk
    //
    [StructLayout( LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi )]
    internal struct dpackfile_t
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst=56)]
        public byte[] name; // [56];

        public int filepos, filelen;
    }

    [StructLayout( LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi )]
    internal struct dpackheader_t
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] id; // [4];

        [MarshalAs(UnmanagedType.I4, SizeConst = 4)]
        public int dirofs;

        [MarshalAs(UnmanagedType.I4, SizeConst = 4)]
        public int dirlen;
    }

    [StructLayout( LayoutKind.Explicit )]
    internal struct Union4b
    {
        [FieldOffset(0)]
        public uint ui0;

        [FieldOffset(0)]
        public int i0;

        [FieldOffset(0)]
        public float f0;

        [FieldOffset(0)]
        public short s0;

        [FieldOffset(2)]
        public short s1;

        [FieldOffset(0)]
        public ushort us0;

        [FieldOffset(2)]
        public ushort us1;

        [FieldOffset(0)]
        public byte b0;

        [FieldOffset(1)]
        public byte b1;

        [FieldOffset(2)]
        public byte b2;

        [FieldOffset(3)]
        public byte b3;

        public static readonly Union4b Empty = new Union4b(0, 0, 0, 0);

        public Union4b( byte b0, byte b1, byte b2, byte b3 )
        {
            // Shut up compiler
            this.ui0 = 0;
            this.i0 = 0;
            this.f0 = 0;
            this.s0 = 0;
            this.s1 = 0;
            this.us0 = 0;
            this.us1 = 0;
            this.b0 = b0;
            this.b1 = b1;
            this.b2 = b2;
            this.b3 = b3;
        }
    }

    internal static class common
    {
        public static bool IsBigEndian
        {
            get
            {
                return !BitConverter.IsLittleEndian;
            }
        }

        public static string GameDir
        {
            get
            {
                return _GameDir;
            }
        }

        public static GameKind GameKind
        {
            get
            {
                return _GameKind;
            }
        }

        public static int Argc
        {
            get
            {
                return _Argv.Length;
            }
        }

        public static string[] Args
        {
            get
            {
                return _Argv;
            }
            set
            {
                _Argv = new string[value.Length];
                value.CopyTo( _Argv, 0 );
                _Args = String.Join( " ", value );
            }
        }

        public static string Token
        {
            get
            {
                return _Token;
            }
        }

        public static bool IsRegistered
        {
            get
            {
                return _Registered.Value != 0;
            }
        }

        public const int MAX_FILES_IN_PACK = 2048;

        // if a packfile directory differs from this, it is assumed to be hacked
        public const int PAK0_COUNT = 339;

        public const int PAK0_CRC = 32981;

        public static Vector3 ZeroVector = Vector3.Zero;

        // for passing as reference
        public static v3f ZeroVector3f = default(v3f);

        private static readonly byte[] ZeroBytes = new byte[4096];

        // this graphic needs to be in the pak file to use registered features
        private static ushort[] _Pop = new ushort[]
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
        private static string[] safeargvs = new string[]
        {
            "-stdvid", "-nolan", "-nosound", "-nocdaudio", "-nojoy", "-nomouse", "-dibonly"
        };

        private static IByteOrderConverter _Converter;
        private static cvar _Registered;
        private static cvar _CmdLine;
        private static string _CacheDir; // com_cachedir[MAX_OSPATH];
        private static string _GameDir; // com_gamedir[MAX_OSPATH];
        private static List<searchpath_t> _SearchPaths; // searchpath_t    *com_searchpaths;
        private static string[] _Argv;
        private static string _Args; // com_cmdline
        private static GameKind _GameKind; // qboolean		standard_quake = true, rogue, hipnotic;
        private static bool _IsModified; // com_modified
        private static bool _StaticRegistered; // static_registered
        private static char[] _Slashes = new char[] { '/', '\\' };
        private static string _Token; // com_token

        public static string Argv( int index )
        {
            return _Argv[index];
        }

        // int COM_CheckParm (char *parm)
        // Returns the position (1 to argc-1) in the program's argument list
        // where the given parameter apears, or 0 if not present
        public static int CheckParm( string parm )
        {
            for( int i = 1; i < _Argv.Length; i++ )
            {
                if( _Argv[i].Equals( parm ) )
                    return i;
            }
            return 0;
        }

        public static bool HasParam( string parm )
        {
            return ( CheckParm( parm ) > 0 );
        }

        // void COM_Init (char *path)
        public static void Init( string path, string[] argv )
        {
            _Argv = argv;
            _Registered = new cvar( "registered", "0" );
            _CmdLine = new cvar( "cmdline", "0", false, true );

            cmd.Add( "path", Path_f );

            InitFileSystem();
            CheckRegistered();
        }

        // void COM_InitArgv (int argc, char **argv)
        public static void InitArgv( string[] argv )
        {
            // reconstitute the command line for the cmdline externally visible cvar
            _Args = String.Join( " ", argv );
            _Argv = new string[argv.Length];
            argv.CopyTo( _Argv, 0 );

            bool safe = false;
            foreach( string arg in _Argv )
            {
                if( arg == "-safe" )
                {
                    safe = true;
                    break;
                }
            }

            if( safe )
            {
                // force all the safe-mode switches. Note that we reserved extra space in
                // case we need to add these, so we don't need an overflow check
                string[] largv = new string[_Argv.Length + safeargvs.Length];
                _Argv.CopyTo( largv, 0 );
                safeargvs.CopyTo( largv, _Argv.Length );
                _Argv = largv;
            }

            _GameKind = GameKind.StandardQuake;

            if( HasParam( "-rogue" ) )
                _GameKind = GameKind.Rogue;

            if( HasParam( "-hipnotic" ) )
                _GameKind = GameKind.Hipnotic;
        }

        /// <summary>
        /// COM_Parse
        /// Parse a token out of a string
        /// </summary>
        public static string Parse( string data )
        {
            _Token = String.Empty;

            if( String.IsNullOrEmpty( data ) )
                return null;

            // skip whitespace
            int i = 0;
            while( i < data.Length )
            {
                while( i < data.Length )
                {
                    if( data[i] > ' ' )
                        break;

                    i++;
                }

                if( i >= data.Length )
                    return null;

                // skip // comments
                if( ( data[i] == '/' ) && ( i + 1 < data.Length ) && ( data[i + 1] == '/' ) )
                {
                    while( i < data.Length && data[i] != '\n' )
                        i++;
                }
                else
                    break;
            }

            if( i >= data.Length )
                return null;

            int i0 = i;

            // handle quoted strings specially
            if( data[i] == '\"' )
            {
                i++;
                i0 = i;
                while( i < data.Length && data[i] != '\"' )
                    i++;

                if( i == data.Length )
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
            char c = data[i];
            if( c == '{' || c == '}' || c == ')' || c == '(' || c == '\'' || c == ':' )
            {
                _Token = data.Substring( i, 1 );
                return ( i + 1 < data.Length ? data.Substring( i + 1 ) : null );
            }

            // parse a regular word
            while( i < data.Length )
            {
                c = data[i];
                if( c <= 32 || c == '{' || c == '}' || c == ')' || c == '(' || c == '\'' || c == ':' )
                {
                    i--;
                    break;
                }
                i++;
            }

            if( i == data.Length )
            {
                _Token = data.Substring( i0, i - i0 );
                return null;
            }

            _Token = data.Substring( i0, i - i0 + 1 );
            return ( i + 1 < data.Length ? data.Substring( i + 1 ) : null );
        }

        /// <summary>
        /// COM_LoadFile
        /// </summary>
        public static byte[] LoadFile( string path )
        {
            // look for it in the filesystem or pack files
            DisposableWrapper<BinaryReader> file;
            int length = OpenFile( path, out file );
            if( file == null )
                return null;

            byte[] result = new byte[length];
            using( file )
            {
                Drawer.BeginDisc();
                int left = length;
                while( left > 0 )
                {
                    int count = file.Object.Read( result, length - left, left );
                    if( count == 0 )
                        sys.Error( "COM_LoadFile: reading failed!" );
                    left -= count;
                }
                Drawer.EndDisc();
            }
            return result;
        }

        /// <summary>
        /// COM_LoadPackFile
        /// Takes an explicit (not game tree related) path to a pak file.
        /// Loads the header and directory, adding the files at the beginning
        /// of the list so they override previous pack files.
        /// </summary>
        public static pack_t LoadPackFile( string packfile )
        {
            FileStream file = sys.FileOpenRead( packfile );
            if( file == null )
                return null;

            dpackheader_t header = sys.ReadStructure<dpackheader_t>( file );

            string id = Encoding.ASCII.GetString( header.id );
            if( id != "PACK" )
                sys.Error( "{0} is not a packfile", packfile );

            header.dirofs = LittleLong( header.dirofs );
            header.dirlen = LittleLong( header.dirlen );

            int numpackfiles = header.dirlen / Marshal.SizeOf( typeof( dpackfile_t ) );

            if( numpackfiles > MAX_FILES_IN_PACK )
                sys.Error( "{0} has {1} files", packfile, numpackfiles );

            //if (numpackfiles != PAK0_COUNT)
            //    _IsModified = true;    // not the original file

            file.Seek( header.dirofs, SeekOrigin.Begin );
            byte[] buf = new byte[header.dirlen];
            if( file.Read( buf, 0, buf.Length ) != buf.Length )
            {
                sys.Error( "{0} buffering failed!", packfile );
            }
            List<dpackfile_t> info = new List<dpackfile_t>( MAX_FILES_IN_PACK );
            GCHandle handle = GCHandle.Alloc( buf, GCHandleType.Pinned );
            try
            {
                IntPtr ptr = handle.AddrOfPinnedObject();
                int count = 0, structSize = Marshal.SizeOf( typeof( dpackfile_t ) );
                while( count < header.dirlen )
                {
                    dpackfile_t tmp = (dpackfile_t)Marshal.PtrToStructure( ptr, typeof( dpackfile_t ) );
                    info.Add( tmp );
                    ptr = new IntPtr( ptr.ToInt64() + structSize );
                    count += structSize;
                }
                if( numpackfiles != info.Count )
                {
                    sys.Error( "{0} directory reading failed!", packfile );
                }
            }
            finally
            {
                handle.Free();
            }

            // crc the directory to check for modifications
            //ushort crc;
            //CRC.Init(out crc);
            //for (int i = 0; i < buf.Length; i++)
            //    CRC.ProcessByte(ref crc, buf[i]);
            //if (crc != PAK0_CRC)
            //    _IsModified = true;

            buf = null;

            // parse the directory
            packfile_t[] newfiles = new packfile_t[numpackfiles];
            for( int i = 0; i < numpackfiles; i++ )
            {
                packfile_t pf = new packfile_t();
                pf.name = common.GetString( info[i].name );
                pf.filepos = LittleLong( info[i].filepos );
                pf.filelen = LittleLong( info[i].filelen );
                newfiles[i] = pf;
            }

            pack_t pack = new pack_t( packfile, new BinaryReader( file, Encoding.ASCII ), newfiles );
            Con.Print( "Added packfile {0} ({1} files)\n", packfile, numpackfiles );
            return pack;
        }

        // COM_FOpenFile(char* filename, FILE** file)
        // If the requested file is inside a packfile, a new FILE * will be opened
        // into the file.
        public static int FOpenFile( string filename, out DisposableWrapper<BinaryReader> file )
        {
            return FindFile( filename, out file, true );
        }

        public static int atoi( string s )
        {
            if( String.IsNullOrEmpty( s ) )
                return 0;

            int sign = 1;
            int result = 0;
            int offset = 0;
            if( s.StartsWith( "-" ) )
            {
                sign = -1;
                offset++;
            }

            int i = -1;

            if( s.Length > 2 )
            {
                i = s.IndexOf( "0x", offset, 2 );
                if( i == -1 )
                {
                    i = s.IndexOf( "0X", offset, 2 );
                }
            }

            if( i == offset )
            {
                int.TryParse( s.Substring( offset + 2 ), System.Globalization.NumberStyles.HexNumber, null, out result );
            }
            else
            {
                i = s.IndexOf( '\'', offset, 1 );
                if( i != -1 )
                {
                    result = (byte)s[i + 1];
                }
                else
                    int.TryParse( s.Substring( offset ), out result );
            }
            return sign * result;
        }

        public static float atof( string s )
        {
            float v;
            float.TryParse( s, NumberStyles.Float, CultureInfo.InvariantCulture.NumberFormat, out v );
            return v;
        }

        public static bool SameText( string a, string b )
        {
            return ( String.Compare( a, b, true ) == 0 );
        }

        public static bool SameText( string a, string b, int count )
        {
            return ( String.Compare( a, 0, b, 0, count, true ) == 0 );
        }

        public static short BigShort( short l )
        {
            return _Converter.BigShort( l );
        }

        public static short LittleShort( short l )
        {
            return _Converter.LittleShort( l );
        }

        public static int BigLong( int l )
        {
            return _Converter.BigLong( l );
        }

        public static int LittleLong( int l )
        {
            return _Converter.LittleLong( l );
        }

        public static float BigFloat( float l )
        {
            return _Converter.BigFloat( l );
        }

        public static float LittleFloat( float l )
        {
            return _Converter.LittleFloat( l );
        }

        public static Vector3 LittleVector( Vector3 src )
        {
            return new Vector3( _Converter.LittleFloat( src.X ),
                _Converter.LittleFloat( src.Y ), _Converter.LittleFloat( src.Z ) );
        }

        public static Vector3 LittleVector3( float[] src )
        {
            return new Vector3( _Converter.LittleFloat( src[0] ),
                _Converter.LittleFloat( src[1] ), _Converter.LittleFloat( src[2] ) );
        }

        public static Vector4 LittleVector4( float[] src, int offset )
        {
            return new Vector4( _Converter.LittleFloat( src[offset + 0] ),
                _Converter.LittleFloat( src[offset + 1] ),
                _Converter.LittleFloat( src[offset + 2] ),
                _Converter.LittleFloat( src[offset + 3] ) );
        }

        public static void FillArray<T>( T[] dest, T value )
        {
            int elementSizeInBytes = Marshal.SizeOf( typeof( T ) );
            int blockSize = Math.Min( dest.Length, 4096 / elementSizeInBytes );
            for( int i = 0; i < blockSize; i++ )
                dest[i] = value;

            int blockSizeInBytes = blockSize * elementSizeInBytes;
            int offset = blockSizeInBytes;
            int lengthInBytes = Buffer.ByteLength( dest );
            while( true )// offset + blockSize <= lengthInBytes)
            {
                int left = lengthInBytes - offset;
                if( left < blockSizeInBytes )
                    blockSizeInBytes = left;

                if( blockSizeInBytes <= 0 )
                    break;

                Buffer.BlockCopy( dest, 0, dest, offset, blockSizeInBytes );
                offset += blockSizeInBytes;
            }
        }

        public static void ZeroArray<T>( T[] dest, int startIndex, int length )
        {
            int elementBytes = Marshal.SizeOf( typeof( T ) );
            int offset = startIndex * elementBytes;
            int sizeInBytes = dest.Length * elementBytes - offset;
            while( true )
            {
                int blockSize = sizeInBytes - offset;
                if( blockSize > ZeroBytes.Length )
                    blockSize = ZeroBytes.Length;

                if( blockSize <= 0 )
                    break;

                Buffer.BlockCopy( ZeroBytes, 0, dest, offset, blockSize );
                offset += blockSize;
            }
        }

        public static string Copy( string src, int maxLength )
        {
            if( src == null )
                return null;

            return ( src.Length > maxLength ? src.Substring( 1, maxLength ) : src );
        }

        public static void Copy( float[] src, out Vector3 dest )
        {
            dest.X = src[0];
            dest.Y = src[1];
            dest.Z = src[2];
        }

        public static void Copy( ref Vector3 src, float[] dest )
        {
            dest[0] = src.X;
            dest[1] = src.Y;
            dest[2] = src.Z;
        }

        public static string GetString( byte[] src )
        {
            int count = 0;
            while( count < src.Length && src[count] != 0 )
                count++;

            return ( count > 0 ? Encoding.ASCII.GetString( src, 0, count ) : String.Empty );
        }

        public static Vector3 ToVector( ref v3f v )
        {
            return new Vector3( v.x, v.y, v.z );
        }

        public static void WriteInt( byte[] dest, int offset, int value )
        {
            Union4b u = Union4b.Empty;
            u.i0 = value;
            dest[offset + 0] = u.b0;
            dest[offset + 1] = u.b1;
            dest[offset + 2] = u.b2;
            dest[offset + 3] = u.b3;
        }

        // COM_CopyFile
        //
        // Copies a file over from the net to the local cache, creating any directories
        // needed.  This is for the convenience of developers using ISDN from home.
        private static void CopyFile( string netpath, string cachepath )
        {
            using( Stream src = sys.FileOpenRead( netpath ), dest = sys.FileOpenWrite( cachepath ) )
            {
                if( src == null )
                {
                    sys.Error( "CopyFile: cannot open file {0}\n", netpath );
                }
                long remaining = src.Length;
                string dirName = Path.GetDirectoryName( cachepath );
                if( !Directory.Exists( dirName ) )
                    Directory.CreateDirectory( dirName );

                byte[] buf = new byte[4096];
                while( remaining > 0 )
                {
                    int count = buf.Length;
                    if( remaining < count )
                        count = (int)remaining;

                    src.Read( buf, 0, count );
                    dest.Write( buf, 0, count );
                    remaining -= count;
                }
            }
        }

        /// <summary>
        /// COM_FindFile
        /// Finds the file in the search path.
        /// </summary>
        private static int FindFile( string filename, out DisposableWrapper<BinaryReader> file, bool duplicateStream )
        {
            file = null;

            string cachepath = String.Empty;

            //
            // search through the path, one element at a time
            //
            foreach( searchpath_t sp in _SearchPaths )
            {
                // is the element a pak file?
                if( sp.pack != null )
                {
                    // look through all the pak file elements
                    pack_t pak = sp.pack;
                    foreach( packfile_t pfile in pak.files )
                    {
                        if( pfile.name.Equals( filename ) )
                        {
                            // found it!
                            Con.DPrint( "PackFile: {0} : {1}\n", sp.pack.filename, filename );
                            if( duplicateStream )
                            {
                                FileStream pfs = (FileStream)pak.stream.BaseStream;
                                FileStream fs = new FileStream( pfs.Name, FileMode.Open, FileAccess.Read, FileShare.Read );
                                file = new DisposableWrapper<BinaryReader>( new BinaryReader( fs, Encoding.ASCII ), true );
                            }
                            else
                            {
                                file = new DisposableWrapper<BinaryReader>( pak.stream, false );
                            }

                            file.Object.BaseStream.Seek( pfile.filepos, SeekOrigin.Begin );
                            return pfile.filelen;
                        }
                    }
                }
                else
                {
                    // check a file in the directory tree
                    if( !_StaticRegistered )
                    {
                        // if not a registered version, don't ever go beyond base
                        if( filename.IndexOfAny( _Slashes ) != -1 ) // strchr (filename, '/') || strchr (filename,'\\'))
                            continue;
                    }

                    string netpath = sp.filename + "/" + filename;  //sprintf (netpath, "%s/%s",search->filename, filename);
                    DateTime findtime = sys.GetFileTime( netpath );
                    if( findtime == DateTime.MinValue )
                        continue;

                    // see if the file needs to be updated in the cache
                    if( String.IsNullOrEmpty( _CacheDir ) )// !com_cachedir[0])
                    {
                        cachepath = netpath; //  strcpy(cachepath, netpath);
                    }
                    else
                    {
                        if( sys.IsWindows )
                        {
                            if( netpath.Length < 2 || netpath[1] != ':' )
                                cachepath = _CacheDir + netpath;
                            else
                                cachepath = _CacheDir + netpath.Substring( 2 );
                        }
                        else
                        {
                            cachepath = _CacheDir + netpath;
                        }

                        DateTime cachetime = sys.GetFileTime( cachepath );
                        if( cachetime < findtime )
                            CopyFile( netpath, cachepath );
                        netpath = cachepath;
                    }

                    Con.DPrint( "FindFile: {0}\n", netpath );
                    FileStream fs = sys.FileOpenRead( netpath );
                    if( fs == null )
                    {
                        file = null;
                        return -1;
                    }
                    file = new DisposableWrapper<BinaryReader>( new BinaryReader( fs, Encoding.ASCII ), true );
                    return (int)fs.Length;
                }
            }

            Con.DPrint( "FindFile: can't find {0}\n", filename );
            return -1;
        }

        // COM_OpenFile(char* filename, int* hndl)
        // filename never has a leading slash, but may contain directory walks
        // returns a handle and a length
        // it may actually be inside a pak file
        private static int OpenFile( string filename, out DisposableWrapper<BinaryReader> file )
        {
            return FindFile( filename, out file, false );
        }

        // COM_Path_f
        private static void Path_f()
        {
            Con.Print( "Current search path:\n" );
            foreach( searchpath_t sp in _SearchPaths )
            {
                if( sp.pack != null )
                {
                    Con.Print( "{0} ({1} files)\n", sp.pack.filename, sp.pack.files.Length );
                }
                else
                {
                    Con.Print( "{0}\n", sp.filename );
                }
            }
        }

        // COM_CheckRegistered
        //
        // Looks for the pop.txt file and verifies it.
        // Sets the "registered" cvar.
        // Immediately exits out if an alternate game was attempted to be started without
        // being registered.
        private static void CheckRegistered()
        {
            _StaticRegistered = false;

            byte[] buf = LoadFile( "gfx/pop.lmp" );
            if( buf == null || buf.Length < 256 )
            {
                Con.Print( "Playing shareware version.\n" );
                if( _IsModified )
                    sys.Error( "You must have the registered version to use modified games" );
                return;
            }

            ushort[] check = new ushort[buf.Length / 2];
            Buffer.BlockCopy( buf, 0, check, 0, buf.Length );
            for( int i = 0; i < 128; i++ )
            {
                if( _Pop[i] != (ushort)_Converter.BigShort( (short)check[i] ) )
                    sys.Error( "Corrupted data file." );
            }

            cvar.Set( "cmdline", _Args );
            cvar.Set( "registered", "1" );
            _StaticRegistered = true;
            Con.Print( "Playing registered version.\n" );
        }

        // COM_InitFilesystem
        private static void InitFileSystem()
        {
            //
            // -basedir <path>
            // Overrides the system supplied base directory (under GAMENAME)
            //
            string basedir = String.Empty;
            int i = CheckParm( "-basedir" );
            if( ( i > 0 ) && ( i < _Argv.Length - 1 ) )
            {
                basedir = _Argv[i + 1];
            }
            else
            {
                basedir = host.Params.basedir;
            }

            if( !String.IsNullOrEmpty( basedir ) )
            {
                basedir.TrimEnd( '\\', '/' );
            }

            //
            // -cachedir <path>
            // Overrides the system supplied cache directory (NULL or /qcache)
            // -cachedir - will disable caching.
            //
            i = CheckParm( "-cachedir" );
            if( ( i > 0 ) && ( i < _Argv.Length - 1 ) )
            {
                if( _Argv[i + 1][0] == '-' )
                    _CacheDir = String.Empty;
                else
                    _CacheDir = _Argv[i + 1];
            }
            else if( !String.IsNullOrEmpty( host.Params.cachedir ) )
            {
                _CacheDir = host.Params.cachedir;
            }
            else
            {
                _CacheDir = String.Empty;
            }

            //
            // start up with GAMENAME by default (id1)
            //
            AddGameDirectory( basedir + "/" + QDef.GAMENAME );

            if( HasParam( "-rogue" ) )
                AddGameDirectory( basedir + "/rogue" );
            if( HasParam( "-hipnotic" ) )
                AddGameDirectory( basedir + "/hipnotic" );

            //
            // -game <gamedir>
            // Adds basedir/gamedir as an override game
            //
            i = CheckParm( "-game" );
            if( ( i > 0 ) && ( i < _Argv.Length - 1 ) )
            {
                _IsModified = true;
                AddGameDirectory( basedir + "/" + _Argv[i + 1] );
            }

            //
            // -path <dir or packfile> [<dir or packfile>] ...
            // Fully specifies the exact serach path, overriding the generated one
            //
            i = CheckParm( "-path" );
            if( i > 0 )
            {
                _IsModified = true;
                _SearchPaths.Clear();
                while( ++i < _Argv.Length )
                {
                    if( String.IsNullOrEmpty( _Argv[i] ) || _Argv[i][0] == '+' || _Argv[i][0] == '-' )
                        break;

                    _SearchPaths.Insert( 0, new searchpath_t( _Argv[i] ) );
                }
            }
        }

        // COM_AddGameDirectory
        //
        // Sets com_gamedir, adds the directory to the head of the path,
        // then loads and adds pak1.pak pak2.pak ...
        private static void AddGameDirectory( string dir )
        {
            _GameDir = dir;

            //
            // add the directory to the search path
            //
            _SearchPaths.Insert( 0, new searchpath_t( dir ) );

            //
            // add any pak files in the format pak0.pak pak1.pak, ...
            //
            for( int i = 0; ; i++ )
            {
                string pakfile = String.Format( "{0}/PAK{1}.PAK", dir, i );
                pack_t pak = LoadPackFile( pakfile );
                if( pak == null )
                    break;

                _SearchPaths.Insert( 0, new searchpath_t( pak ) );
            }
        }

        static common()
        {
            // set the byte swapping variables in a portable manner
            if( BitConverter.IsLittleEndian )
            {
                _Converter = new LittleEndianConverter();
            }
            else
            {
                _Converter = new BigEndianConverter();
            }

            _SearchPaths = new List<searchpath_t>();
        }
    }

    [StructLayout( LayoutKind.Sequential, Pack = 1 )]
    internal class link_t
    {
        private link_t _Prev, _Next;
        private object _Owner;

        public link_t Prev
        {
            get
            {
                return _Prev;
            }
        }

        public link_t Next
        {
            get
            {
                return _Next;
            }
        }

        public object Owner
        {
            get
            {
                return _Owner;
            }
        }

        public link_t( object owner )
        {
            _Owner = owner;
        }

        public void Clear()
        {
            _Prev = _Next = this;
        }

        public void ClearToNulls()
        {
            _Prev = _Next = null;
        }

        public void Remove()
        {
            _Next._Prev = _Prev;
            _Prev._Next = _Next;
            _Next = null;
            _Prev = null;
        }

        public void InsertBefore( link_t before )
        {
            _Next = before;
            _Prev = before._Prev;
            _Prev._Next = this;
            _Next._Prev = this;
        }

        public void InsertAfter( link_t after )
        {
            _Next = after.Next;
            _Prev = after;
            _Prev._Next = this;
            _Next._Prev = this;
        }
    } // link_t;

    // MSG_WriteXxx() functions
    internal class MsgWriter
    {
        public byte[] Data
        {
            get
            {
                return _Buffer;
            }
        }

        public bool IsEmpty
        {
            get
            {
                return ( _Count == 0 );
            }
        }

        public int Length
        {
            get
            {
                return _Count;
            }
        }

        public bool AllowOverflow
        {
            get; set;
        }

        public bool IsOveflowed
        {
            get; set;
        }

        public int Capacity
        {
            get
            {
                return _Buffer.Length;
            }
            set
            {
                SetBufferSize( value );
            }
        }

        private byte[] _Buffer;

        private int _Count;

        private Union4b _Val = Union4b.Empty;

        public object GetState()
        {
            object st = null;
            SaveState( ref st );
            return st;
        }

        public void SaveState( ref object state )
        {
            if( state == null )
            {
                state = new State();
            }
            State st = GetState( state );
            if( st.Buffer == null || st.Buffer.Length != _Buffer.Length )
            {
                st.Buffer = new byte[_Buffer.Length];
            }
            Buffer.BlockCopy( _Buffer, 0, st.Buffer, 0, _Buffer.Length );
            st.Count = _Count;
        }

        public void RestoreState( object state )
        {
            State st = GetState( state );
            SetBufferSize( st.Buffer.Length );
            Buffer.BlockCopy( st.Buffer, 0, _Buffer, 0, _Buffer.Length );
            _Count = st.Count;
        }

        // void MSG_WriteChar(sizebuf_t* sb, int c);
        public void WriteChar( int c )
        {
#if PARANOID
            if (c < -128 || c > 127)
                Sys.Error("MSG_WriteChar: range error");
#endif
            NeedRoom( 1 );
            _Buffer[_Count++] = (byte)c;
        }

        // MSG_WriteByte(sizebuf_t* sb, int c);
        public void WriteByte( int c )
        {
#if PARANOID
            if (c < 0 || c > 255)
                Sys.Error("MSG_WriteByte: range error");
#endif
            NeedRoom( 1 );
            _Buffer[_Count++] = (byte)c;
        }

        // MSG_WriteShort(sizebuf_t* sb, int c)
        public void WriteShort( int c )
        {
#if PARANOID
            if (c < short.MinValue || c > short.MaxValue)
                Sys.Error("MSG_WriteShort: range error");
#endif
            NeedRoom( 2 );
            _Buffer[_Count++] = (byte)( c & 0xff );
            _Buffer[_Count++] = (byte)( c >> 8 );
        }

        // MSG_WriteLong(sizebuf_t* sb, int c);
        public void WriteLong( int c )
        {
            NeedRoom( 4 );
            _Buffer[_Count++] = (byte)( c & 0xff );
            _Buffer[_Count++] = (byte)( ( c >> 8 ) & 0xff );
            _Buffer[_Count++] = (byte)( ( c >> 16 ) & 0xff );
            _Buffer[_Count++] = (byte)( c >> 24 );
        }

        // MSG_WriteFloat(sizebuf_t* sb, float f)
        public void WriteFloat( float f )
        {
            NeedRoom( 4 );
            _Val.f0 = f;
            _Val.i0 = common.LittleLong( _Val.i0 );

            _Buffer[_Count++] = _Val.b0;
            _Buffer[_Count++] = _Val.b1;
            _Buffer[_Count++] = _Val.b2;
            _Buffer[_Count++] = _Val.b3;
        }

        // MSG_WriteString(sizebuf_t* sb, char* s)
        public void WriteString( string s )
        {
            int count = 1;
            if( !String.IsNullOrEmpty( s ) )
                count += s.Length;

            NeedRoom( count );
            for( int i = 0; i < count - 1; i++ )
                _Buffer[_Count++] = (byte)s[i];
            _Buffer[_Count++] = 0;
        }

        // SZ_Print()
        public void Print( string s )
        {
            if( _Count > 0 && _Buffer[_Count - 1] == 0 )
                _Count--; // remove previous trailing 0
            WriteString( s );
        }

        // MSG_WriteCoord(sizebuf_t* sb, float f)
        public void WriteCoord( float f )
        {
            WriteShort( (int)( f * 8 ) );
        }

        // MSG_WriteAngle(sizebuf_t* sb, float f)
        public void WriteAngle( float f )
        {
            WriteByte( ( (int)f * 256 / 360 ) & 255 );
        }

        public void Write( byte[] src, int offset, int count )
        {
            if( count > 0 )
            {
                NeedRoom( count );
                Buffer.BlockCopy( src, offset, _Buffer, _Count, count );
                _Count += count;
            }
        }

        public void Clear()
        {
            _Count = 0;
        }

        public void FillFrom( Stream src, int count )
        {
            Clear();
            NeedRoom( count );
            while( _Count < count )
            {
                int r = src.Read( _Buffer, _Count, count - _Count );
                if( r == 0 )
                    break;
                _Count += r;
            }
        }

        public void FillFrom( byte[] src, int startIndex, int count )
        {
            Clear();
            NeedRoom( count );
            Buffer.BlockCopy( src, startIndex, _Buffer, 0, count );
            _Count = count;
        }

        public int FillFrom( Socket socket, ref EndPoint ep )
        {
            Clear();
            int result = net.LanDriver.Read( socket, _Buffer, _Buffer.Length, ref ep );
            if( result >= 0 )
                _Count = result;
            return result;
        }

        public void AppendFrom( byte[] src, int startIndex, int count )
        {
            NeedRoom( count );
            Buffer.BlockCopy( src, startIndex, _Buffer, _Count, count );
            _Count += count;
        }

        protected void NeedRoom( int bytes )
        {
            if( _Count + bytes > _Buffer.Length )
            {
                if( !this.AllowOverflow )
                    sys.Error( "MsgWriter: overflow without allowoverflow set!" );

                this.IsOveflowed = true;
                _Count = 0;
                if( bytes > _Buffer.Length )
                    sys.Error( "MsgWriter: Requested more than whole buffer has!" );
            }
        }

        private class State
        {
            public byte[] Buffer;
            public int Count;
        }

        private void SetBufferSize( int value )
        {
            if( _Buffer != null )
            {
                if( _Buffer.Length == value )
                    return;

                Array.Resize( ref _Buffer, value );

                if( _Count > _Buffer.Length )
                    _Count = _Buffer.Length;
            }
            else
                _Buffer = new byte[value];
        }

        private State GetState( object state )
        {
            if( state == null )
            {
                throw new ArgumentNullException();
            }
            State st = state as State;
            if( st == null )
            {
                throw new ArgumentException( "Passed object is not a state!" );
            }
            return st;
        }

        public MsgWriter()
                    : this( 0 )
        {
        }

        public MsgWriter( int capacity )
        {
            SetBufferSize( capacity );
            this.AllowOverflow = false;
        }
    }

    // MSG_ReadXxx() functions
    internal class MsgReader
    {
        /// <summary>
        /// msg_badread
        /// </summary>
        public bool IsBadRead
        {
            get
            {
                return _IsBadRead;
            }
        }

        /// <summary>
        /// msg_readcount
        /// </summary>
        public int Position
        {
            get
            {
                return _Count;
            }
        }

        private MsgWriter _Source;
        private bool _IsBadRead;
        private int _Count;
        private Union4b _Val;
        private char[] _Tmp;

        /// <summary>
        /// MSG_BeginReading
        /// </summary>
        public void Reset()
        {
            _IsBadRead = false;
            _Count = 0;
        }

        /// <summary>
        /// MSG_ReadChar
        /// reads sbyte
        /// </summary>
        public int ReadChar()
        {
            if( !HasRoom( 1 ) )
                return -1;

            return (sbyte)_Source.Data[_Count++];
        }

        // MSG_ReadByte (void)
        public int ReadByte()
        {
            if( !HasRoom( 1 ) )
                return -1;

            return (byte)_Source.Data[_Count++];
        }

        // MSG_ReadShort (void)
        public int ReadShort()
        {
            if( !HasRoom( 2 ) )
                return -1;

            int c = (short)( _Source.Data[_Count + 0] + ( _Source.Data[_Count + 1] << 8 ) );
            _Count += 2;
            return c;
        }

        // MSG_ReadLong (void)
        public int ReadLong()
        {
            if( !HasRoom( 4 ) )
                return -1;

            int c = _Source.Data[_Count + 0] +
                ( _Source.Data[_Count + 1] << 8 ) +
                ( _Source.Data[_Count + 2] << 16 ) +
                ( _Source.Data[_Count + 3] << 24 );

            _Count += 4;
            return c;
        }

        // MSG_ReadFloat (void)
        public float ReadFloat()
        {
            if( !HasRoom( 4 ) )
                return 0;

            _Val.b0 = _Source.Data[_Count + 0];
            _Val.b1 = _Source.Data[_Count + 1];
            _Val.b2 = _Source.Data[_Count + 2];
            _Val.b3 = _Source.Data[_Count + 3];

            _Count += 4;

            _Val.i0 = common.LittleLong( _Val.i0 );
            return _Val.f0;
        }

        // char *MSG_ReadString (void)
        public string ReadString()
        {
            int l = 0;
            do
            {
                int c = ReadChar();
                if( c == -1 || c == 0 )
                    break;
                _Tmp[l] = (char)c;
                l++;
            } while( l < _Tmp.Length - 1 );

            return new String( _Tmp, 0, l );
        }

        // float MSG_ReadCoord (void)
        public float ReadCoord()
        {
            return ReadShort() * ( 1.0f / 8 );
        }

        // float MSG_ReadAngle (void)
        public float ReadAngle()
        {
            return ReadChar() * ( 360.0f / 256 );
        }

        public Vector3 ReadCoords()
        {
            Vector3 result;
            result.X = ReadCoord();
            result.Y = ReadCoord();
            result.Z = ReadCoord();
            return result;
        }

        public Vector3 ReadAngles()
        {
            Vector3 result;
            result.X = ReadAngle();
            result.Y = ReadAngle();
            result.Z = ReadAngle();
            return result;
        }

        private bool HasRoom( int bytes )
        {
            if( _Count + bytes > _Source.Length )
            {
                _IsBadRead = true;
                return false;
            }
            return true;
        }

        public MsgReader( MsgWriter source )
        {
            _Source = source;
            _Val = Union4b.Empty;
            _Tmp = new char[2048];
        }
    }

    #region Byte order converters

    internal static class SwapHelper
    {
        public static short ShortSwap( short l )
        {
            byte b1, b2;

            b1 = (byte)( l & 255 );
            b2 = (byte)( ( l >> 8 ) & 255 );

            return (short)( ( b1 << 8 ) + b2 );
        }

        public static int LongSwap( int l )
        {
            byte b1, b2, b3, b4;

            b1 = (byte)( l & 255 );
            b2 = (byte)( ( l >> 8 ) & 255 );
            b3 = (byte)( ( l >> 16 ) & 255 );
            b4 = (byte)( ( l >> 24 ) & 255 );

            return ( (int)b1 << 24 ) + ( (int)b2 << 16 ) + ( (int)b3 << 8 ) + b4;
        }

        public static float FloatSwap( float f )
        {
            byte[] bytes = BitConverter.GetBytes( f );
            byte[] bytes2 = new byte[4];

            bytes2[0] = bytes[3];
            bytes2[1] = bytes[2];
            bytes2[2] = bytes[1];
            bytes2[3] = bytes[0];

            return BitConverter.ToSingle( bytes2, 0 );
        }

        public static void Swap4b( byte[] buff, int offset )
        {
            byte b1, b2, b3, b4;

            b1 = buff[offset + 0];
            b2 = buff[offset + 1];
            b3 = buff[offset + 2];
            b4 = buff[offset + 3];

            buff[offset + 0] = b4;
            buff[offset + 1] = b3;
            buff[offset + 2] = b2;
            buff[offset + 3] = b1;
        }
    }

    internal class LittleEndianConverter : IByteOrderConverter
    {
        #region IByteOrderConverter Members

        short IByteOrderConverter.BigShort( short l )
        {
            return SwapHelper.ShortSwap( l );
        }

        short IByteOrderConverter.LittleShort( short l )
        {
            return l;
        }

        int IByteOrderConverter.BigLong( int l )
        {
            return SwapHelper.LongSwap( l );
        }

        int IByteOrderConverter.LittleLong( int l )
        {
            return l;
        }

        float IByteOrderConverter.BigFloat( float l )
        {
            return SwapHelper.FloatSwap( l );
        }

        float IByteOrderConverter.LittleFloat( float l )
        {
            return l;
        }

        #endregion IByteOrderConverter Members
    }

    internal class BigEndianConverter : IByteOrderConverter
    {
        #region IByteOrderConverter Members

        short IByteOrderConverter.BigShort( short l )
        {
            return l;
        }

        short IByteOrderConverter.LittleShort( short l )
        {
            return SwapHelper.ShortSwap( l );
        }

        int IByteOrderConverter.BigLong( int l )
        {
            return l;
        }

        int IByteOrderConverter.LittleLong( int l )
        {
            return SwapHelper.LongSwap( l );
        }

        float IByteOrderConverter.BigFloat( float l )
        {
            return l;
        }

        float IByteOrderConverter.LittleFloat( float l )
        {
            return SwapHelper.FloatSwap( l );
        }

        #endregion IByteOrderConverter Members
    }

    internal interface IByteOrderConverter
    {
        short BigShort( short l );

        short LittleShort( short l );

        int BigLong( int l );

        int LittleLong( int l );

        float BigFloat( float l );

        float LittleFloat( float l );
    }

    #endregion Byte order converters

    //
    // in memory
    //

    internal class packfile_t
    {
        public string name; // [MAX_QPATH];
        public int filepos, filelen;

        public override string ToString()
        {
            return String.Format( "{0}, at {1}, {2} bytes}", this.name, this.filepos, this.filelen );
        }
    } // packfile_t;

    internal class pack_t
    {
        public string filename; // [MAX_OSPATH];
        public BinaryReader stream; //int handle;

        //int numfiles;
        public packfile_t[] files;

        public pack_t( string filename, BinaryReader reader, packfile_t[] files )
        {
            this.filename = filename;
            this.stream = reader;
            this.files = files;
        }
    } // pack_t;

    // dpackfile_t;

    // dpackheader_t;

    internal class searchpath_t
    {
        public string filename; // char[MAX_OSPATH];
        public pack_t pack; // only one of filename / pack will be used

        public searchpath_t( string path )
        {
            if( path.EndsWith( ".PAK" ) )
            {
                this.pack = common.LoadPackFile( path );
                if( this.pack == null )
                    sys.Error( "Couldn't load packfile: {0}", path );
            }
            else
                this.filename = path;
        }

        public searchpath_t( pack_t pak )
        {
            this.pack = pak;
        }
    } // searchpath_t;

    internal class DisposableWrapper<T> : IDisposable where T : class, IDisposable
    {
        public T Object
        {
            get
            {
                return _Object;
            }
        }

        private T _Object;
        private bool _Owned;

        private void Dispose( bool disposing )
        {
            if( _Object != null && _Owned )
            {
                _Object.Dispose();
                _Object = null;
            }
        }

        public DisposableWrapper( T obj, bool dispose )
        {
            _Object = obj;
            _Owned = dispose;
        }

        ~DisposableWrapper()
        {
            Dispose( false );
        }

        #region IDisposable Members

        public void Dispose()
        {
            Dispose( true );
            GC.SuppressFinalize( this );
        }

        #endregion IDisposable Members
    }

    internal class ByteArraySegment
    {
        public byte[] Data
        {
            get
            {
                return _Segment.Array;
            }
        }

        public int StartIndex
        {
            get
            {
                return _Segment.Offset;
            }
        }

        public int Length
        {
            get
            {
                return _Segment.Count;
            }
        }

        private ArraySegment<byte> _Segment;

        public ByteArraySegment( byte[] array )
            : this( array, 0, -1 )
        {
        }

        public ByteArraySegment( byte[] array, int startIndex )
            : this( array, startIndex, -1 )
        {
        }

        public ByteArraySegment( byte[] array, int startIndex, int length )
        {
            if( array == null )
            {
                throw new ArgumentNullException( "array" );
            }
            if( length == -1 )
            {
                length = array.Length - startIndex;
            }
            if( length <= 0 )
            {
                throw new ArgumentException( "Invalid length!" );
            }
            _Segment = new ArraySegment<byte>( array, startIndex, length );
        }
    }

    internal class QuakeException : Exception
    {
        public QuakeException()
        {
        }

        public QuakeException( string message )
            : base( message )
        {
        }
    }

    internal class EndGameException : QuakeException
    {
    }

    internal class QuakeSystemError : QuakeException
    {
        public QuakeSystemError( string message )
            : base( message )
        {
        }
    }
}
