using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpQuake.Framework
{
    //
    // in memory
    //

    public class MemoryPakFile
    {
        public String name; // [MAX_QPATH];
        public Int32 filepos, filelen;

        public override String ToString( )
        {
            return String.Format( "{0}, at {1}, {2} bytes}", this.name, this.filepos, this.filelen );
        }
    } // packfile_t;
}
