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

using System;
using System.Runtime.InteropServices;

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
                return ProgramsWrapper.GetString( s_file );
            }
        }

        public String Name
        {
            get
            {
                return ProgramsWrapper.GetString( s_name );
            }
        }

        public void SwapBytes( )
        {
            first_statement = EndianHelper.LittleLong( first_statement );
            parm_start = EndianHelper.LittleLong( parm_start );
            locals = EndianHelper.LittleLong( locals );
            s_name = EndianHelper.LittleLong( s_name );
            s_file = EndianHelper.LittleLong( s_file );
            numparms = EndianHelper.LittleLong( numparms );
        }

        public override String ToString( )
        {
            return String.Format( "{{{0}: {1}()}}", FileName, Name );
        }
    } // dfunction_t;
}
