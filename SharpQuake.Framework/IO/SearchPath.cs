using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpQuake.Framework
{
    internal class searchpath_t
    {
        public String filename; // char[MAX_OSPATH];
        public Pak pack; // only one of filename / pack will be used
        public ZipArchive pk3;
        public String pk3filename;

        public searchpath_t( String path )
        {
            if ( path.EndsWith( ".PAK" ) )
            {
                this.pack = FileSystem.LoadPackFile( path );
                if ( this.pack == null )
                    Utilities.Error( "Couldn't load packfile: {0}", path );
            }
            else if ( path.EndsWith( ".PK3" ) )
            {
                this.pk3 = ZipFile.OpenRead( path );
                this.pk3filename = path;
                if ( this.pk3 == null )
                    Utilities.Error( "Couldn't load pk3file: {0}", path );
            }
            else
                this.filename = path;
        }

        public searchpath_t( Pak pak )
        {
            this.pack = pak;
        }

        public searchpath_t( ZipArchive archive )
        {
            this.pk3 = archive;
        }
    } // searchpath_t;    
}
