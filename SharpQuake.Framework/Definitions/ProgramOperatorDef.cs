using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using string_t = System.Int32;

namespace SharpQuake.Framework
{
    public static class ProgramOperatorDef
    {
        public const string_t OFS_NULL = 0;
        public const string_t OFS_RETURN = 1;
        public const string_t OFS_PARM0 = 4;		// leave 3 ofs for each parm to hold vectors
        public const string_t OFS_PARM1 = 7;
        public const string_t OFS_PARM2 = 10;
        public const string_t OFS_PARM3 = 13;
        public const string_t OFS_PARM4 = 16;
        public const string_t OFS_PARM5 = 19;
        public const string_t OFS_PARM6 = 22;
        public const string_t OFS_PARM7 = 25;
        public const string_t RESERVED_OFS = 28;
    }
}
