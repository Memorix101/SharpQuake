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
    public class ProgramFunction
    {
        public string_t first_statement;	// negative numbers are builtins
        public string_t parm_start;
        public string_t locals;				// total ints of parms + locals

        public string_t profile;		// runtime

        public string_t s_name;
        public string_t s_file;			// source file defined in

        public string_t numparms;

        [MarshalAs( UnmanagedType.ByValArray, SizeConst = ProgramDef.MAX_PARMS )]
        public Byte[] parm_size; // [MAX_PARMS];

        public static string_t SizeInBytes = Marshal.SizeOf( typeof( ProgramFunction ) );

        public String FileName
        {
            get
            {
                return ProgramsWrapper.GetString( this.s_file );
            }
        }

        public String Name
        {
            get
            {
                return ProgramsWrapper.GetString( this.s_name );
            }
        }

        public void SwapBytes( )
        {
            this.first_statement = EndianHelper.LittleLong( this.first_statement );
            this.parm_start = EndianHelper.LittleLong( this.parm_start );
            this.locals = EndianHelper.LittleLong( this.locals );
            this.s_name = EndianHelper.LittleLong( this.s_name );
            this.s_file = EndianHelper.LittleLong( this.s_file );
            this.numparms = EndianHelper.LittleLong( this.numparms );
        }

        public override String ToString( )
        {
            return String.Format( "{{{0}: {1}()}}", this.FileName, this.Name );
        }
    } // dfunction_t;
}
