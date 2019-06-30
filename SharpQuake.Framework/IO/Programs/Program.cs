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
    public class Program
    {
        public string_t version;
        public string_t crc;			// check of header file

        public string_t ofs_statements;
        public string_t numstatements;	// statement 0 is an error

        public string_t ofs_globaldefs;
        public string_t numglobaldefs;

        public string_t ofs_fielddefs;
        public string_t numfielddefs;

        public string_t ofs_functions;
        public string_t numfunctions;	// function 0 is an empty

        public string_t ofs_strings;
        public string_t numstrings;		// first string is a null string

        public string_t ofs_globals;
        public string_t numglobals;

        public string_t entityfields;

        public static string_t SizeInBytes = Marshal.SizeOf( typeof( Program ) );

        public void SwapBytes( )
        {
            this.version = EndianHelper.LittleLong( this.version );
            this.crc = EndianHelper.LittleLong( this.crc );
            this.ofs_statements = EndianHelper.LittleLong( this.ofs_statements );
            this.numstatements = EndianHelper.LittleLong( this.numstatements );
            this.ofs_globaldefs = EndianHelper.LittleLong( this.ofs_globaldefs );
            this.numglobaldefs = EndianHelper.LittleLong( this.numglobaldefs );
            this.ofs_fielddefs = EndianHelper.LittleLong( this.ofs_fielddefs );
            this.numfielddefs = EndianHelper.LittleLong( this.numfielddefs );
            this.ofs_functions = EndianHelper.LittleLong( this.ofs_functions );
            this.numfunctions = EndianHelper.LittleLong( this.numfunctions );
            this.ofs_strings = EndianHelper.LittleLong( this.ofs_strings );
            this.numstrings = EndianHelper.LittleLong( this.numstrings );
            this.ofs_globals = EndianHelper.LittleLong( this.ofs_globals );
            this.numglobals = EndianHelper.LittleLong( this.numglobals );
            this.entityfields = EndianHelper.LittleLong( this.entityfields );
        }
    } // dprograms_t;
}
