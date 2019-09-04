/// <copyright>
///
/// SharpQuakeEvolved changes by optimus-code, 2019
/// 
/// Based on SharpQuake (Quake Rewritten in C# by Yury Kiselev, 2010.)
///
/// Copyright (C) 1996-1997 Id Software, Inc.
///
/// This program is free software; you can redistribute it and/or
/// modify it under the terms of the GNU General Public License
/// as published by the Free Software Foundation; either version 2
/// of the License, or (at your option) any later version.
///
/// This program is distributed in the hope that it will be useful,
/// but WITHOUT ANY WARRANTY; without even the implied warranty of
/// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
///
/// See the GNU General Public License for more details.
///
/// You should have received a copy of the GNU General Public License
/// along with this program; if not, write to the Free Software
/// Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
/// </copyright>

using System.Runtime.InteropServices;

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
            version = EndianHelper.LittleLong( version );
            crc = EndianHelper.LittleLong( crc );
            ofs_statements = EndianHelper.LittleLong( ofs_statements );
            numstatements = EndianHelper.LittleLong( numstatements );
            ofs_globaldefs = EndianHelper.LittleLong( ofs_globaldefs );
            numglobaldefs = EndianHelper.LittleLong( numglobaldefs );
            ofs_fielddefs = EndianHelper.LittleLong( ofs_fielddefs );
            numfielddefs = EndianHelper.LittleLong( numfielddefs );
            ofs_functions = EndianHelper.LittleLong( ofs_functions );
            numfunctions = EndianHelper.LittleLong( numfunctions );
            ofs_strings = EndianHelper.LittleLong( ofs_strings );
            numstrings = EndianHelper.LittleLong( numstrings );
            ofs_globals = EndianHelper.LittleLong( ofs_globals );
            numglobals = EndianHelper.LittleLong( numglobals );
            entityfields = EndianHelper.LittleLong( entityfields );
        }
    } // dprograms_t;
}
