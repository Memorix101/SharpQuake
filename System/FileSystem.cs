using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SharpQuake
{
    public static class FileSystem
    {
        public const int MAX_FILES_IN_PACK = 2048;

        private static string _CacheDir; // com_cachedir[MAX_OSPATH];
        private static string _GameDir; // com_gamedir[MAX_OSPATH];
        private static List<searchpath_t> _SearchPaths; // searchpath_t    *com_searchpaths;
        public static bool _StaticRegistered; // static_registered
        private static char[] _Slashes = new char[] { '/', '\\' };
        public static bool _IsModified; // com_modified

        public static string GameDir
        {
            get
            {
                return _GameDir;
            }
        }

        static FileSystem( )
        {
            _SearchPaths = new List<searchpath_t>( );
        }

        // COM_InitFilesystem
        public static void InitFileSystem( )
        {
            //
            // -basedir <path>
            // Overrides the system supplied base directory (under GAMENAME)
            //
            string basedir = String.Empty;
            int i = Common.CheckParm( "-basedir" );
            if ( ( i > 0 ) && ( i < Common._Argv.Length - 1 ) )
            {
                basedir = Common._Argv[i + 1];
            }
            else
            {
                basedir = host.Params.basedir;
                qparam.globalbasedir = basedir;
            }

            if ( !String.IsNullOrEmpty( basedir ) )
            {
                basedir.TrimEnd( '\\', '/' );
            }

            //
            // -cachedir <path>
            // Overrides the system supplied cache directory (NULL or /qcache)
            // -cachedir - will disable caching.
            //
            i = Common.CheckParm( "-cachedir" );
            if ( ( i > 0 ) && ( i < Common._Argv.Length - 1 ) )
            {
                if ( Common._Argv[i + 1][0] == '-' )
                    _CacheDir = String.Empty;
                else
                    _CacheDir = Common._Argv[i + 1];
            }
            else if ( !String.IsNullOrEmpty( host.Params.cachedir ) )
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
            qparam.globalgameid = QDef.GAMENAME;

            if ( Common.HasParam( "-rogue" ) )
            {
                AddGameDirectory( basedir + "/rogue" );
                qparam.globalgameid = "rogue";
            }

            if ( Common.HasParam( "-hipnotic" ) )
            {
                AddGameDirectory( basedir + "/hipnotic" );
                qparam.globalgameid = "hipnotic";
            }
            //
            // -game <gamedir>
            // Adds basedir/gamedir as an override game
            //
            i = Common.CheckParm( "-game" );
            if ( ( i > 0 ) && ( i < Common._Argv.Length - 1 ) )
            {
                _IsModified = true;
                AddGameDirectory( basedir + "/" + Common._Argv[i + 1] );
            }

            //
            // -path <dir or packfile> [<dir or packfile>] ...
            // Fully specifies the exact serach path, overriding the generated one
            //
            i = Common.CheckParm( "-path" );
            if ( i > 0 )
            {
                _IsModified = true;
                _SearchPaths.Clear( );
                while ( ++i < Common._Argv.Length )
                {
                    if ( String.IsNullOrEmpty( Common._Argv[i] ) || Common._Argv[i][0] == '+' || Common._Argv[i][0] == '-' )
                        break;

                    _SearchPaths.Insert( 0, new searchpath_t( Common._Argv[i] ) );
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
            for ( int i = 0; ; i++ )
            {
                string pakfile = String.Format( "{0}/PAK{1}.PAK", dir, i );
                pack_t pak = LoadPackFile( pakfile );
                if ( pak == null )
                    break;

                _SearchPaths.Insert( 0, new searchpath_t( pak ) );
            }

            //
            // add any pk3 files in the format pak0.pk3 pak1.pk3, ...
            //
            foreach ( var pk3file in Directory.GetFiles( dir, "*.pk3" ).OrderByDescending( f => f ) )
            {
                FileStream file = sys.FileOpenRead( pk3file );

                if ( file != null )
                {
                    file.Dispose( );

                    ZipArchive pk3 = ZipFile.OpenRead( pk3file );

                    if ( pk3 == null )
                        break;

                    _SearchPaths.Insert( 0, new searchpath_t( pk3 ) );
                }
            }
        }

        // COM_Path_f
        public static void Path_f( )
        {
            Con.Print( "Current search path:\n" );
            foreach ( searchpath_t sp in _SearchPaths )
            {
                if ( sp.pack != null )
                {
                    Con.Print( "{0} ({1} files)\n", sp.pack.filename, sp.pack.files.Length );
                }
                if ( sp.pk3 != null )
                {
                    Con.Print( "{0} ({1} files)\n", sp.pk3filename, sp.pk3.Entries.Count );
                }
                else
                {
                    Con.Print( "{0}\n", sp.filename );
                }
            }
        }

        // COM_CopyFile
        //
        // Copies a file over from the net to the local cache, creating any directories
        // needed.  This is for the convenience of developers using ISDN from home.
        private static void CopyFile( string netpath, string cachepath )
        {
            using ( Stream src = sys.FileOpenRead( netpath ), dest = sys.FileOpenWrite( cachepath ) )
            {
                if ( src == null )
                {
                    sys.Error( "CopyFile: cannot open file {0}\n", netpath );
                }
                long remaining = src.Length;
                string dirName = Path.GetDirectoryName( cachepath );
                if ( !Directory.Exists( dirName ) )
                    Directory.CreateDirectory( dirName );

                byte[] buf = new byte[4096];
                while ( remaining > 0 )
                {
                    int count = buf.Length;
                    if ( remaining < count )
                        count = ( int ) remaining;

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
            foreach ( searchpath_t sp in _SearchPaths )
            {
                // is the element a pak file?
                if ( sp.pack != null )
                {
                    // look through all the pak file elements
                    pack_t pak = sp.pack;
                    foreach ( packfile_t pfile in pak.files )
                    {
                        if ( pfile.name.Equals( filename ) )
                        {
                            // found it!
                            Con.DPrint( "PackFile: {0} : {1}\n", sp.pack.filename, filename );
                            if ( duplicateStream )
                            {
                                FileStream pfs = ( FileStream ) pak.stream.BaseStream;
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
                else if ( sp.pk3 != null ) // is the element a pk3 file?
                {
                    // look through all the pak file elements
                    ZipArchive pk3 = sp.pk3;

                    foreach ( var pfile in pk3.Entries )
                    {
                        if ( pfile.FullName.Equals( filename ) )
                        {
                            // found it!
                            Con.DPrint( "PK3File: {0} : {1}\n", sp.pk3filename, filename );

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

                    string netpath = sp.filename + "/" + filename;  //sprintf (netpath, "%s/%s",search->filename, filename);
                    DateTime findtime = sys.GetFileTime( netpath );
                    if ( findtime == DateTime.MinValue )
                        continue;

                    // see if the file needs to be updated in the cache
                    if ( String.IsNullOrEmpty( _CacheDir ) )// !com_cachedir[0])
                    {
                        cachepath = netpath; //  strcpy(cachepath, netpath);
                    }
                    else
                    {
                        if ( sys.IsWindows )
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

                        DateTime cachetime = sys.GetFileTime( cachepath );
                        if ( cachetime < findtime )
                            CopyFile( netpath, cachepath );
                        netpath = cachepath;
                    }

                    Con.DPrint( "FindFile: {0}\n", netpath );
                    FileStream fs = sys.FileOpenRead( netpath );
                    if ( fs == null )
                    {
                        file = null;
                        return -1;
                    }
                    file = new DisposableWrapper<BinaryReader>( new BinaryReader( fs, Encoding.ASCII ), true );
                    return ( int ) fs.Length;
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



        /// <summary>
        /// COM_LoadFile
        /// </summary>
        public static byte[] LoadFile( string path )
        {
            // look for it in the filesystem or pack files
            DisposableWrapper<BinaryReader> file;
            int length = OpenFile( path, out file );
            if ( file == null )
                return null;

            byte[] result = new byte[length];
            using ( file )
            {
                Drawer.BeginDisc( );
                int left = length;
                while ( left > 0 )
                {
                    int count = file.Object.Read( result, length - left, left );
                    if ( count == 0 )
                        sys.Error( "COM_LoadFile: reading failed!" );
                    left -= count;
                }
                Drawer.EndDisc( );
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
            if ( file == null )
                return null;

            dpackheader_t header = sys.ReadStructure<dpackheader_t>( file );

            string id = Encoding.ASCII.GetString( header.id );
            if ( id != "PACK" )
                sys.Error( "{0} is not a packfile", packfile );

            header.dirofs = Common.LittleLong( header.dirofs );
            header.dirlen = Common.LittleLong( header.dirlen );

            int numpackfiles = header.dirlen / Marshal.SizeOf( typeof( dpackfile_t ) );

            if ( numpackfiles > MAX_FILES_IN_PACK )
                sys.Error( "{0} has {1} files", packfile, numpackfiles );

            //if (numpackfiles != PAK0_COUNT)
            //    _IsModified = true;    // not the original file

            file.Seek( header.dirofs, SeekOrigin.Begin );
            byte[] buf = new byte[header.dirlen];
            if ( file.Read( buf, 0, buf.Length ) != buf.Length )
            {
                sys.Error( "{0} buffering failed!", packfile );
            }
            List<dpackfile_t> info = new List<dpackfile_t>( MAX_FILES_IN_PACK );
            GCHandle handle = GCHandle.Alloc( buf, GCHandleType.Pinned );
            try
            {
                IntPtr ptr = handle.AddrOfPinnedObject( );
                int count = 0, structSize = Marshal.SizeOf( typeof( dpackfile_t ) );
                while ( count < header.dirlen )
                {
                    dpackfile_t tmp = ( dpackfile_t ) Marshal.PtrToStructure( ptr, typeof( dpackfile_t ) );
                    info.Add( tmp );
                    ptr = new IntPtr( ptr.ToInt64( ) + structSize );
                    count += structSize;
                }
                if ( numpackfiles != info.Count )
                {
                    sys.Error( "{0} directory reading failed!", packfile );
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
            packfile_t[] newfiles = new packfile_t[numpackfiles];
            for ( int i = 0; i < numpackfiles; i++ )
            {
                packfile_t pf = new packfile_t( );
                pf.name = Common.GetString( info[i].name );
                pf.filepos = Common.LittleLong( info[i].filepos );
                pf.filelen = Common.LittleLong( info[i].filelen );
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
    }

    //
    // in memory
    //

    public class packfile_t
    {
        public string name; // [MAX_QPATH];
        public int filepos, filelen;

        public override string ToString( )
        {
            return String.Format( "{0}, at {1}, {2} bytes}", this.name, this.filepos, this.filelen );
        }
    } // packfile_t;

    public class pack_t
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
        public ZipArchive pk3;
        public string pk3filename;

        public searchpath_t( string path )
        {
            if ( path.EndsWith( ".PAK" ) )
            {
                this.pack = FileSystem.LoadPackFile( path );
                if ( this.pack == null )
                    sys.Error( "Couldn't load packfile: {0}", path );
            }
            else if ( path.EndsWith( ".PK3" ) )
            {
                this.pk3 = ZipFile.OpenRead( path );
                this.pk3filename = path;
                if ( this.pk3 == null )
                    sys.Error( "Couldn't load pk3file: {0}", path );
            }
            else
                this.filename = path;
        }

        public searchpath_t( pack_t pak )
        {
            this.pack = pak;
        }

        public searchpath_t( ZipArchive archive )
        {
            this.pk3 = archive;
        }
    } // searchpath_t;


    //
    // on disk
    //
    [StructLayout( LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi )]
    internal struct dpackfile_t
    {
        [MarshalAs( UnmanagedType.ByValArray, SizeConst = 56 )]
        public byte[] name; // [56];

        public int filepos, filelen;
    }

    [StructLayout( LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi )]
    internal struct dpackheader_t
    {
        [MarshalAs( UnmanagedType.ByValArray, SizeConst = 4 )]
        public byte[] id; // [4];

        [MarshalAs( UnmanagedType.I4, SizeConst = 4 )]
        public int dirofs;

        [MarshalAs( UnmanagedType.I4, SizeConst = 4 )]
        public int dirlen;
    }
}
