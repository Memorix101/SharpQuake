using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using string_t = System.Int32;

namespace SharpQuake.Framework
{
    public delegate void builtin_t( );

    /// <summary>
    /// PR_functions
    /// </summary>
    public static partial class ProgramDef
    {
        public const string_t DEF_SAVEGLOBAL = ( 1 << 15 );
        public const string_t MAX_PARMS = 8;
        public const string_t MAX_ENT_LEAFS = 16;

        public const string_t PROG_VERSION = 6;
        public const string_t PROGHEADER_CRC = 5927;

        // Used to link the framework and game dynamic value
        public static Int32 EdictSize;
    }
}
