using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SharpQuake.Framework
{
    //
    // on disk
    //
    [StructLayout( LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi )]
    public struct dpackfile_t
    {
        [MarshalAs( UnmanagedType.ByValArray, SizeConst = 56 )]
        public Byte[] name; // [56];

        public Int32 filepos, filelen;
    }

    //
    // in memory
    //

    public class packfile_t
    {
        public String name; // [MAX_QPATH];
        public Int32 filepos, filelen;

        public override String ToString( )
        {
            return String.Format( "{0}, at {1}, {2} bytes}", this.name, this.filepos, this.filelen );
        }
    } // packfile_t;

}
