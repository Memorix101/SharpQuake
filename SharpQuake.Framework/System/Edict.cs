using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

using string_t = System.Int32;

namespace SharpQuake.Framework
{
    /// <summary>
    /// On-disk edict
    /// </summary>
    [StructLayout( LayoutKind.Sequential, Pack = 1 )]
    public struct Edict
    {
        public Boolean free;
        public string_t dummy1, dummy2;	 // former link_t area

        public string_t num_leafs;

        [MarshalAs( UnmanagedType.ByValArray, SizeConst = ProgramDef.MAX_ENT_LEAFS )]
        public Int16[] leafnums; // [MAX_ENT_LEAFS];

        public EntityState baseline;

        public Single freetime;			// sv.time when the object was freed
        public EntVars v;					// C exported fields from progs
        // other fields from progs come immediately after

        public static string_t SizeInBytes = Marshal.SizeOf( typeof( Edict ) );
    } // dedict_t
}
