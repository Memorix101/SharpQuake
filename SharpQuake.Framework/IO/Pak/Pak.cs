using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpQuake.Framework
{
    public class Pak
    {
        public String filename; // [MAX_OSPATH];
        public BinaryReader stream; //int handle;

        //int numfiles;
        public MemoryPakFile[] files;

        public Pak( String filename, BinaryReader reader, MemoryPakFile[] files )
        {
            this.filename = filename;
            this.stream = reader;
            this.files = files;
        }
    } // pack_t;
}
