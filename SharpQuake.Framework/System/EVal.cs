using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using string_t = System.Int32;
using func_t = System.Int32;
using System.Runtime.InteropServices;

namespace SharpQuake.Framework
{
    [StructLayout( LayoutKind.Explicit, Size = 12, Pack = 1 )]
    public unsafe struct EVal
    {
        [FieldOffset( 0 )]
        public string_t _string;

        [FieldOffset( 0 )]
        public Single _float;

        [FieldOffset( 0 )]
        public fixed Single vector[3];

        [FieldOffset( 0 )]
        public string_t function;

        [FieldOffset( 0 )]
        public string_t _int;

        [FieldOffset( 0 )]
        public string_t edict;
    } // eval_t
}
