/// <copyright>
///
/// Rewritten in C# by Yury Kiselev, 2010.
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
using SharpQuake.Framework;
using func_t = System.Int32;

// progs.h

using string_t = System.Int32;

namespace SharpQuake
{
    internal delegate void builtin_t();

    

    

    internal enum etype_t
    {
        ev_void, ev_string, ev_float, ev_vector, ev_entity, ev_field, ev_function, ev_pointer
    }

    internal enum OP
    {
        OP_DONE,
        OP_MUL_F,
        OP_MUL_V,
        OP_MUL_FV,
        OP_MUL_VF,
        OP_DIV_F,
        OP_ADD_F,
        OP_ADD_V,
        OP_SUB_F,
        OP_SUB_V,

        OP_EQ_F,
        OP_EQ_V,
        OP_EQ_S,
        OP_EQ_E,
        OP_EQ_FNC,

        OP_NE_F,
        OP_NE_V,
        OP_NE_S,
        OP_NE_E,
        OP_NE_FNC,

        OP_LE,
        OP_GE,
        OP_LT,
        OP_GT,

        OP_LOAD_F,
        OP_LOAD_V,
        OP_LOAD_S,
        OP_LOAD_ENT,
        OP_LOAD_FLD,
        OP_LOAD_FNC,

        OP_ADDRESS,

        OP_STORE_F,
        OP_STORE_V,
        OP_STORE_S,
        OP_STORE_ENT,
        OP_STORE_FLD,
        OP_STORE_FNC,

        OP_STOREP_F,
        OP_STOREP_V,
        OP_STOREP_S,
        OP_STOREP_ENT,
        OP_STOREP_FLD,
        OP_STOREP_FNC,

        OP_RETURN,
        OP_NOT_F,
        OP_NOT_V,
        OP_NOT_S,
        OP_NOT_ENT,
        OP_NOT_FNC,
        OP_IF,
        OP_IFNOT,
        OP_CALL0,
        OP_CALL1,
        OP_CALL2,
        OP_CALL3,
        OP_CALL4,
        OP_CALL5,
        OP_CALL6,
        OP_CALL7,
        OP_CALL8,
        OP_STATE,
        OP_GOTO,
        OP_AND,
        OP_OR,

        OP_BITAND,
        OP_BITOR
    }

    [StructLayout( LayoutKind.Sequential, Pack = 1 )]
    internal struct dstatement_t
    {
        public UInt16 op;
        public Int16 a, b, c;

        public static string_t SizeInBytes = Marshal.SizeOf(typeof(dstatement_t));

        public void SwapBytes()
        {
            this.op = ( UInt16 ) EndianHelper.LittleShort( ( Int16 ) this.op );
            this.a = EndianHelper.LittleShort( this.a );
            this.b = EndianHelper.LittleShort( this.b );
            this.c = EndianHelper.LittleShort( this.c );
        }
    }

    

    [StructLayout( LayoutKind.Explicit, Size = ( 4 * 28 ) )]
    internal struct pad_int28
    {
        //int pad[28];
    }

    // eval_t;

    internal static class OFS
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

    

    // edict_t;

    // Source: pr_comp.h
    // this file is shared by quake and qcc

    // etype_t;
    // dstatement_t;

    [StructLayout( LayoutKind.Sequential, Pack = 1 )]
    internal class ddef_t
    {
        public UInt16 type;		// if DEF_SAVEGLOBGAL bit is set

        // the variable needs to be saved in savegames
        public UInt16 ofs;

        public string_t s_name;

        public static string_t SizeInBytes = Marshal.SizeOf(typeof(ddef_t));

        public void SwapBytes()
        {
            this.type = ( UInt16 ) EndianHelper.LittleShort( ( Int16 ) this.type );
            this.ofs = ( UInt16 ) EndianHelper.LittleShort( ( Int16 ) this.ofs );
            this.s_name = EndianHelper.LittleLong( this.s_name );
        }
    } // ddef_t;

    [StructLayout( LayoutKind.Sequential, Pack = 1 )]
    internal class dfunction_t
    {
        public string_t first_statement;	// negative numbers are builtins
        public string_t parm_start;
        public string_t locals;				// total ints of parms + locals

        public string_t profile;		// runtime

        public string_t s_name;
        public string_t s_file;			// source file defined in

        public string_t numparms;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = ProgDefs.MAX_PARMS)]
        public Byte[] parm_size; // [MAX_PARMS];

        public static string_t SizeInBytes = Marshal.SizeOf(typeof(dfunction_t));

        public String FileName
        {
            get
            {
                return progs.GetString( this.s_file );
            }
        }

        public String Name
        {
            get
            {
                return progs.GetString( this.s_name );
            }
        }

        public void SwapBytes()
        {
            this.first_statement = EndianHelper.LittleLong( this.first_statement );
            this.parm_start = EndianHelper.LittleLong( this.parm_start );
            this.locals = EndianHelper.LittleLong( this.locals );
            this.s_name = EndianHelper.LittleLong( this.s_name );
            this.s_file = EndianHelper.LittleLong( this.s_file );
            this.numparms = EndianHelper.LittleLong( this.numparms );
        }

        public override String ToString()
        {
            return String.Format( "{{{0}: {1}()}}", this.FileName, this.Name );
        }
    } // dfunction_t;

    [StructLayout( LayoutKind.Sequential, Pack = 1 )]
    internal class dprograms_t
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

        public static string_t SizeInBytes = Marshal.SizeOf(typeof(dprograms_t));

        public void SwapBytes()
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

    //=================================================================
    // QuakeC compiler generated data from progdefs.q1
    //=================================================================

    [StructLayout( LayoutKind.Sequential, Pack = 1 )]
    internal class globalvars_t
    {
        private pad_int28 pad; //int pad[28];
        public string_t self;
        public string_t other;
        public string_t world;
        public Single time;
        public Single frametime;
        public Single force_retouch;
        public string_t mapname;
        public Single deathmatch;
        public Single coop;
        public Single teamplay;
        public Single serverflags;
        public Single total_secrets;
        public Single total_monsters;
        public Single found_secrets;
        public Single killed_monsters;
        public Single parm1;
        public Single parm2;
        public Single parm3;
        public Single parm4;
        public Single parm5;
        public Single parm6;
        public Single parm7;
        public Single parm8;
        public Single parm9;
        public Single parm10;
        public Single parm11;
        public Single parm12;
        public Single parm13;
        public Single parm14;
        public Single parm15;
        public Single parm16;
        public Vector3f v_forward;
        public Vector3f v_up;
        public Vector3f v_right;
        public Single trace_allsolid;
        public Single trace_startsolid;
        public Single trace_fraction;
        public Vector3f trace_endpos;
        public Vector3f trace_plane_normal;
        public Single trace_plane_dist;
        public string_t trace_ent;
        public Single trace_inopen;
        public Single trace_inwater;
        public string_t msg_entity;
        public func_t main;
        public func_t StartFrame;
        public func_t PlayerPreThink;
        public func_t PlayerPostThink;
        public func_t ClientKill;
        public func_t ClientConnect;
        public func_t PutClientInServer;
        public func_t ClientDisconnect;
        public func_t SetNewParms;
        public func_t SetChangeParms;

        public static string_t SizeInBytes = Marshal.SizeOf(typeof(globalvars_t));

        public void SetParams( Single[] src )
        {
            if( src.Length < server.NUM_SPAWN_PARMS )
                throw new ArgumentException( String.Format( "There must be {0} parameters!", server.NUM_SPAWN_PARMS ) );

            this.parm1 = src[0];
            this.parm2 = src[1];
            this.parm3 = src[2];
            this.parm4 = src[3];
            this.parm5 = src[4];
            this.parm6 = src[5];
            this.parm7 = src[6];
            this.parm8 = src[7];
            this.parm9 = src[8];
            this.parm10 = src[9];
            this.parm11 = src[10];
            this.parm12 = src[11];
            this.parm13 = src[12];
            this.parm14 = src[13];
            this.parm15 = src[14];
            this.parm16 = src[15];
        }
    } // globalvars_t;

    // entvars_t;
}
