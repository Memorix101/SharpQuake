using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

using string_t = System.Int32;

namespace SharpQuake.Framework
{
    [StructLayout( LayoutKind.Sequential, Pack = 1 )]
    public class ProgramDefinition
    {
        public UInt16 type;		// if DEF_SAVEGLOBGAL bit is set

        // the variable needs to be saved in savegames
        public UInt16 ofs;

        public string_t s_name;

        public static string_t SizeInBytes = Marshal.SizeOf( typeof( ProgramDefinition ) );

        public void SwapBytes( )
        {
            this.type = ( UInt16 ) EndianHelper.LittleShort( ( Int16 ) this.type );
            this.ofs = ( UInt16 ) EndianHelper.LittleShort( ( Int16 ) this.ofs );
            this.s_name = EndianHelper.LittleLong( this.s_name );
        }
    } // ddef_t;
}
