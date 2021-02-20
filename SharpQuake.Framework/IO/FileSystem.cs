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
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace SharpQuake.Framework.IO
{
    public static class FileSystem
    {
        public const Int32 MAX_FILES_IN_PACK = 2048;

        private static String _CacheDir; // com_cachedir[MAX_OSPATH];
        private static String _GameDir; // com_gamedir[MAX_OSPATH];
        private static List<SearchPath> _SearchPaths; // searchpath_t    *com_searchpaths;
        public static Boolean _StaticRegistered; // static_registered
        private static Char[] _Slashes = new Char[] { '/', '\\' };
        public static Boolean _IsModified; // com_modified

        public static String GameDir
        {
            get
            {
                return _GameDir;
            }
        }

        static FileSystem( )
        {
            _SearchPaths = new List<SearchPath>( );
        }

        // COM_InitFilesystem
        public static void InitFileSystem( QuakeParameters hostParams )
        {
            //
            // -basedir <path>
            // Overrides the system supplied base directory (under GAMENAME)
            //
            var basedir = String.Empty;
            var i = CommandLine.CheckParm( "-basedir" );
            if ( ( i > 0 ) && ( i < CommandLine._Argv.Length - 1 ) )
            {
                basedir = CommandLine._Argv[i + 1];
            }
            else
            {
                basedir = hostParams.basedir;
                QuakeParameter.globalbasedir = basedir;
            }

            if ( !String.IsNullOrEmpty( basedir ) )
                basedir = basedir.TrimEnd( '\\', '/' );

            //
            // -cachedir <path>
            // Overrides the system supplied cache directory (NULL or /qcache)
            // -cachedir - will disable caching.
            //
            i = CommandLine.CheckParm( "-cachedir" );
            if ( ( i > 0 ) && ( i < CommandLine._Argv.Length - 1 ) )
            {
                if ( CommandLine._Argv[i + 1][0] == '-' )
                    _CacheDir = String.Empty;
                else
                    _CacheDir = CommandLine._Argv[i + 1];
            }
            else if ( !String.IsNullOrEmpty( hostParams.cachedir ) )
            {
                _CacheDir = hostParams.cachedir;
            }
            else
            {
                _CacheDir = String.Empty;
            }

            //
            // start up with GAMENAME by default (id1)
            //
            AddGameDirectory( basedir + "/" + QDef.GAMENAME );
            QuakeParameter.globalgameid = QDef.GAMENAME;

            if ( CommandLine.HasParam( "-rogue" ) )
            {
                AddGameDirectory( basedir + "/rogue" );
                QuakeParameter.globalgameid = "rogue";
            }

            if ( CommandLine.HasParam( "-hipnotic" ) )
            {
                AddGameDirectory( basedir + "/hipnotic" );
                QuakeParameter.globalgameid = "hipnotic";
            }
            //
            // -game <gamedir>
            // Adds basedir/gamedir as an override game
            //
            i = CommandLine.CheckParm( "-game" );
            if ( ( i > 0 ) && ( i < CommandLine._Argv.Length - 1 ) )
            {
                _IsModified = true;
                AddGameDirectory( basedir + "/" + CommandLine._Argv[i + 1] );
            }

            //
            // -path <dir or packfile> [<dir or packfile>] ...
            // Fully specifies the exact serach path, overriding the generated one
            //
            i = CommandLine.CheckParm( "-path" );
            if ( i > 0 )
            {
                _IsModified = true;
                _SearchPaths.Clear( );
                while ( ++i < CommandLine._Argv.Length )
                {
                    if ( String.IsNullOrEmpty( CommandLine._Argv[i] ) || CommandLine._Argv[i][0] == '+' || CommandLine._Argv[i][0] == '-' )
                        break;

                    _SearchPaths.Insert( 0, new SearchPath( CommandLine._Argv[i] ) );
                }
            }
        }

        // COM_AddGameDirectory
        //
        // Sets com_gamedir, adds the directory to the head of the path,
        // then loads and adds pak1.pak pak2.pak ...
        private static void AddGameDirectory( String dir )
        {
            _GameDir = dir;

            //
            // add the directory to the search path
            //
            _SearchPaths.Insert( 0, new SearchPath( dir ) );

            //
            // add any pak files in the format pak0.pak pak1.pak, ...
            //
            for ( var i = 0; ; i++ )
            {
                var pakfile = String.Format( "{0}/PAK{1}.PAK", dir, i );
                var pak = LoadPackFile( pakfile );
                if ( pak == null )
                    break;

                _SearchPaths.Insert( 0, new SearchPath( pak ) );
            }

            //
            // add any pk3 files in the format pak0.pk3 pak1.pk3, ...
            //
            foreach ( var pk3file in Directory.GetFiles( _GameDir, "*.pk3", SearchOption.AllDirectories ).OrderByDescending( f => f ) )
            {
                var file = OpenRead( pk3file );

                if ( file != null )
                {
                    file.Dispose( );

                    var pk3 = ZipFile.OpenRead( pk3file );

                    if ( pk3 == null )
                        break;

                    _SearchPaths.Insert( 0, new SearchPath( pk3 ) );
                }
            }
        }

        public static String[] Search( String pattern )
        {
            return Directory.GetFiles( _GameDir, pattern, SearchOption.AllDirectories )
                .OrderBy( f => f )
                .Select( f => f.Replace( $"{_GameDir}\\", String.Empty ).Replace( "\\", "//" ) )
                .ToArray( );
        }

        // COM_Path_f
        public static void Path_f( CommandMessage msg )
        {
            ConsoleWrapper.Print( "Current search path:\n" );
            foreach ( var sp in _SearchPaths )
            {
                if ( sp.pack != null )
                {
                    ConsoleWrapper.Print( "{0} ({1} files)\n", sp.pack.filename, sp.pack.files.Length );
                }
                if ( sp.pk3 != null )
                {
                    ConsoleWrapper.Print( "{0} ({1} files)\n", sp.pk3filename, sp.pk3.Entries.Count );
                }
                else
                {
                    ConsoleWrapper.Print( "{0}\n", sp.filename );
                }
            }
        }

        // COM_CopyFile
        //
        // Copies a file over from the net to the local cache, creating any directories
        // needed.  This is for the ConsoleWrappervenience of developers using ISDN from home.
        private static void CopyFile( String netpath, String cachepath )
        {
            using ( Stream src = OpenRead( netpath ), dest = OpenWrite( cachepath ) )
            {
                if ( src == null )
                {
                    Utilities.Error( "CopyFile: cannot open file {0}\n", netpath );
                }
                var remaining = src.Length;
                var dirName = Path.GetDirectoryName( cachepath );
                if ( !Directory.Exists( dirName ) )
                    Directory.CreateDirectory( dirName );

                var buf = new Byte[4096];
                while ( remaining > 0 )
                {
                    var count = buf.Length;
                    if ( remaining < count )
                        count = ( Int32 ) remaining;

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
        private static Int32 FindFile( String filename, out DisposableWrapper<BinaryReader> file, Boolean duplicateStream )
        {
            file = null;

            var cachepath = String.Empty;

            //
            // search through the path, one element at a time
            //
            foreach ( var sp in _SearchPaths )
            {
                // is the element a pak file?
                if ( sp.pack != null )
                {
                    // look through all the pak file elements
                    var pak = sp.pack;
                    foreach ( var pfile in pak.files )
                    {
                        if ( pfile.name.Equals( filename ) )
                        {
                            // found it!
                            ConsoleWrapper.DPrint( "PackFile: {0} : {1}\n", sp.pack.filename, filename );
                            if ( duplicateStream )
                            {
                                var pfs = ( FileStream ) pak.stream.BaseStream;
                                var fs = new FileStream( pfs.Name, FileMode.Open, FileAccess.Read, FileShare.Read );
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
                else if ( sp.pk3 != null ) // is the element a pk3 file?
                {
                    // look through all the pak file elements
                    var pk3 = sp.pk3;

                    foreach ( var pfile in pk3.Entries )
                    {
                        if ( pfile.FullName.Equals( filename ) )
                        {
                            // found it!
                            ConsoleWrapper.DPrint( "PK3File: {0} : {1}\n", sp.pk3filename, filename );

                            file = new DisposableWrapper<BinaryReader>( new BinaryReader( pfile.Open( ), Encoding.ASCII ), false );

                            return ( Int32 ) pfile.Length;
                        }
                    }
                }
                else
                {
                    // check a file in the directory tree
                    if ( !_StaticRegistered )
                    {
                        // if not a registered version, don't ever go beyond base
                        if ( filename.IndexOfAny( _Slashes ) != -1 ) // strchr (filename, '/') || strchr (filename,'\\'))
                            continue;
                    }

                    var netpath = sp.filename + "/" + filename;  //sprintf (netpath, "%s/%s",search->filename, filename);
                    var findtime = GetFileTime( netpath );
                    if ( findtime == DateTime.MinValue )
                        continue;

                    // see if the file needs to be updated in the cache
                    if ( String.IsNullOrEmpty( _CacheDir ) )// !com_cachedir[0])
                    {
                        cachepath = netpath; //  strcpy(cachepath, netpath);
                    }
                    else
                    {
                        if ( Utilities.IsWindows )
                        {
                            if ( netpath.Length < 2 || netpath[1] != ':' )
                                cachepath = _CacheDir + netpath;
                            else
                                cachepath = _CacheDir + netpath.Substring( 2 );
                        }
                        else
                        {
                            cachepath = _CacheDir + netpath;
                        }

                        var cachetime = GetFileTime( cachepath );
                        if ( cachetime < findtime )
                            CopyFile( netpath, cachepath );
                        netpath = cachepath;
                    }

                    ConsoleWrapper.DPrint( "FindFile: {0}\n", netpath );
                    var fs = OpenRead( netpath );
                    if ( fs == null )
                    {
                        file = null;
                        return -1;
                    }
                    file = new DisposableWrapper<BinaryReader>( new BinaryReader( fs, Encoding.ASCII ), true );
                    return ( Int32 ) fs.Length;
                }
            }

            ConsoleWrapper.DPrint( "FindFile: can't find {0}\n", filename );
            return -1;
        }

        // COM_OpenFile(char* filename, int* hndl)
        // filename never has a leading slash, but may ConsoleWrappertain directory walks
        // returns a handle and a length
        // it may actually be inside a pak file
        private static Int32 OpenFile( String filename, out DisposableWrapper<BinaryReader> file )
        {
            return FindFile( filename, out file, false );
        }

        /// <summary>
        /// COM_LoadFile
        /// </summary>
        public static Byte[] LoadFile( String path )
        {
            // look for it in the filesystem or pack files
            DisposableWrapper<BinaryReader> file;
            var length = OpenFile( path, out file );
            if ( file == null )
                return null;

            var result = new Byte[length];
            using ( file )
            {
                //Drawer.BeginDisc( );
                var left = length;
                while ( left > 0 )
                {
                    var count = file.Object.Read( result, length - left, left );
                    if ( count == 0 )
                        Utilities.Error( "COM_LoadFile: reading failed!" );
                    left -= count;
                }
               // Drawer.EndDisc( );
            }
            return result;
        }

        /// <summary>
        /// COM_LoadPackFile
        /// Takes an explicit (not game tree related) path to a pak file.
        /// Loads the header and directory, adding the files at the beginning
        /// of the list so they override previous pack files.
        /// </summary>
        public static Pak LoadPackFile( String packfile )
        {
            var file = OpenRead( packfile );
            if ( file == null )
                return null;

            var header = Utilities.ReadStructure<PakHeader>( file );

            var id = Encoding.ASCII.GetString( header.id );
            if ( id != "PACK" )
                Utilities.Error( "{0} is not a packfile", packfile );

            header.dirofs = EndianHelper.LittleLong( header.dirofs );
            header.dirlen = EndianHelper.LittleLong( header.dirlen );

            var numpackfiles = header.dirlen / Marshal.SizeOf( typeof( PakFile ) );

            if ( numpackfiles > MAX_FILES_IN_PACK )
                Utilities.Error( "{0} has {1} files", packfile, numpackfiles );

            //if (numpackfiles != PAK0_COUNT)
            //    _IsModified = true;    // not the original file

            file.Seek( header.dirofs, SeekOrigin.Begin );
            var buf = new Byte[header.dirlen];
            if ( file.Read( buf, 0, buf.Length ) != buf.Length )
            {
                Utilities.Error( "{0} buffering failed!", packfile );
            }
            var info = new List<PakFile>( MAX_FILES_IN_PACK );
            var handle = GCHandle.Alloc( buf, GCHandleType.Pinned );
            try
            {
                var ptr = handle.AddrOfPinnedObject( );
                Int32 count = 0, structSize = Marshal.SizeOf( typeof( PakFile ) );
                while ( count < header.dirlen )
                {
                    var tmp = ( PakFile ) Marshal.PtrToStructure( ptr, typeof( PakFile ) );
                    info.Add( tmp );
                    ptr = new IntPtr( ptr.ToInt64( ) + structSize );
                    count += structSize;
                }
                if ( numpackfiles != info.Count )
                {
                    Utilities.Error( "{0} directory reading failed!", packfile );
                }
            }
            finally
            {
                handle.Free( );
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
            var newfiles = new MemoryPakFile[numpackfiles];
            for ( var i = 0; i < numpackfiles; i++ )
            {
                var pf = new MemoryPakFile( );
                pf.name = Utilities.GetString( info[i].name );
                pf.filepos = EndianHelper.LittleLong( info[i].filepos );
                pf.filelen = EndianHelper.LittleLong( info[i].filelen );
                newfiles[i] = pf;
            }

            var pack = new Pak( packfile, new BinaryReader( file, Encoding.ASCII ), newfiles );
            ConsoleWrapper.Print( "Added packfile {0} ({1} files)\n", packfile, numpackfiles );
            return pack;
        }

        // COM_FOpenFile(char* filename, FILE** file)
        // If the requested file is inside a packfile, a new FILE * will be opened
        // into the file.
        public static Int32 FOpenFile( String filename, out DisposableWrapper<BinaryReader> file )
        {
            return FindFile( filename, out file, true );
        }


        // Sys_FileOpenRead
        public static FileStream OpenRead( String path )
        {
            try
            {
                return new FileStream( path, FileMode.Open, FileAccess.Read, FileShare.Read );
            }
            catch ( Exception )
            {
                return null;
            }
        }

        /// <summary>
        /// Sys_FileOpenWrite
        /// </summary>
        public static FileStream OpenWrite( String path, Boolean allowFail = false )
        {
            try
            {
                return new FileStream( path, FileMode.Create, FileAccess.Write, FileShare.Read );
            }
            catch ( Exception ex )
            {
                if ( !allowFail )
                {
                    Utilities.Error( "Error opening {0}: {1}", path, ex.Message );
                    throw;
                }
            }
            return null;
        }


        // Sys_FileTime()
        public static DateTime GetFileTime( String path )
        {
            if ( String.IsNullOrEmpty( path ) || path.LastIndexOf( '*' ) != -1 )
                return DateTime.MinValue;
            try
            {
                var result = File.GetLastWriteTimeUtc( path );
                if ( result.Year == 1601 )
                    return DateTime.MinValue; // file does not exists

                return result.ToLocalTime( );
            }
            catch ( IOException )
            {
                return DateTime.MinValue;
            }
        }
    }
}
