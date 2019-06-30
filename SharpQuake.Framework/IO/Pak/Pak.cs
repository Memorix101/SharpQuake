using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpQuake.Framework
{
    public class pack_t
    {
        public String filename; // [MAX_OSPATH];
        public BinaryReader stream; //int handle;

        //int numfiles;
        public packfile_t[] files;

        public pack_t( String filename, BinaryReader reader, packfile_t[] files )
        {
            this.filename = filename;
            this.stream = reader;
            this.files = files;
        }
    } // pack_t;
}
