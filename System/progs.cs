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
using func_t = System.Int32;

// progs.h

using string_t = System.Int32;

namespace SharpQuake
{
    internal delegate void builtin_t();

    [StructLayout( LayoutKind.Explicit, Size = 12, Pack = 1 )]
    internal unsafe struct eval_t
    {
        [FieldOffset(0)]
        public string_t _string;

        [FieldOffset(0)]
        public Single _float;

        [FieldOffset(0)]
        public fixed Single vector[3];

        [FieldOffset(0)]
        public string_t function;

        [FieldOffset(0)]
        public string_t _int;

        [FieldOffset(0)]
        public string_t edict;
    }

    /// <summary>
    /// On-disk edict
    /// </summary>
    [StructLayout( LayoutKind.Sequential, Pack = 1 )]
    internal struct dedict_t
    {
        public Boolean free;
        public string_t dummy1, dummy2;	 // former link_t area

        public string_t num_leafs;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = progs.MAX_ENT_LEAFS)]
        public Int16[] leafnums; // [MAX_ENT_LEAFS];

        public entity_state_t baseline;

        public Single freetime;			// sv.time when the object was freed
        public entvars_t v;					// C exported fields from progs
        // other fields from progs come immediately after

        public static string_t SizeInBytes = Marshal.SizeOf(typeof(dedict_t));
    }

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
            this.op = ( UInt16 ) Common.LittleShort( ( Int16 ) this.op );
            this.a = Common.LittleShort( this.a );
            this.b = Common.LittleShort( this.b );
            this.c = Common.LittleShort( this.c );
        }
    }

    [StructLayout( LayoutKind.Sequential, Pack = 1 )]
    internal struct v3f
    {
        public Single x, y, z;

        public Boolean IsEmpty
        {
            get
            {
                return ( this.x == 0 ) && ( this.y == 0 ) && ( this.z == 0 );
            }
        }
    }

    [StructLayout( LayoutKind.Explicit, Size = ( 4 * 28 ) )]
    internal struct pad_int28
    {
        //int pad[28];
    }

    [StructLayout( LayoutKind.Sequential, Pack = 1 )]
    internal struct entvars_t
    {
        public Single modelindex;
        public v3f absmin;
        public v3f absmax;
        public Single ltime;
        public Single movetype;
        public Single solid;
        public v3f origin;
        public v3f oldorigin;
        public v3f velocity;
        public v3f angles;
        public v3f avelocity;
        public v3f punchangle;
        public string_t classname;
        public string_t model;
        public Single frame;
        public Single skin;
        public Single effects;
        public v3f mins;
        public v3f maxs;
        public v3f size;
        public func_t touch;
        public func_t use;
        public func_t think;
        public func_t blocked;
        public Single nextthink;
        public string_t groundentity;
        public Single health;
        public Single frags;
        public Single weapon;
        public string_t weaponmodel;
        public Single weaponframe;
        public Single currentammo;
        public Single ammo_shells;
        public Single ammo_nails;
        public Single ammo_rockets;
        public Single ammo_cells;
        public Single items;
        public Single takedamage;
        public string_t chain;
        public Single deadflag;
        public v3f view_ofs;
        public Single button0;
        public Single button1;
        public Single button2;
        public Single impulse;
        public Single fixangle;
        public v3f v_angle;
        public Single idealpitch;
        public string_t netname;
        public string_t enemy;
        public Single flags;
        public Single colormap;
        public Single team;
        public Single max_health;
        public Single teleport_time;
        public Single armortype;
        public Single armorvalue;
        public Single waterlevel;
        public Single watertype;
        public Single ideal_yaw;
        public Single yaw_speed;
        public string_t aiment;
        public string_t goalentity;
        public Single spawnflags;
        public string_t target;
        public string_t targetname;
        public Single dmg_take;
        public Single dmg_save;
        public string_t dmg_inflictor;
        public string_t owner;
        public v3f movedir;
        public string_t message;
        public Single sounds;
        public string_t noise;
        public string_t noise1;
        public string_t noise2;
        public string_t noise3;

        public static string_t SizeInBytes = Marshal.SizeOf(typeof(entvars_t));
    }

    /// <summary>
    /// PR_functions
    /// </summary>
    static partial class progs
    {
        public const string_t DEF_SAVEGLOBAL = (1<<15);
        public const string_t MAX_PARMS = 8;
        public const string_t MAX_ENT_LEAFS = 16;

        private const string_t PROG_VERSION = 6;
        private const string_t PROGHEADER_CRC = 5927;
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

    /// <summary>
    /// In-memory edict
    /// </summary>
    internal class edict_t
    {
        public Boolean free;
        public link_t area; // linked to a division node or leaf

        public string_t num_leafs;
        public Int16[] leafnums; // [MAX_ENT_LEAFS];

        public entity_state_t baseline;

        public Single freetime;			// sv.time when the object was freed
        public entvars_t v;					// C exported fields from progs
        public Single[] fields; // other fields from progs

        public void Clear()
        {
            this.v = default( entvars_t );
            if( this.fields != null )
                Array.Clear( this.fields, 0, this.fields.Length );
            this.free = false;
        }

        public Boolean IsV( string_t offset, out string_t correctedOffset )
        {
            if( offset < ( entvars_t.SizeInBytes >> 2 ) )
            {
                correctedOffset = offset;
                return true;
            }
            correctedOffset = offset - ( entvars_t.SizeInBytes >> 2 );
            return false;
        }

        public unsafe void LoadInt( string_t offset, eval_t* result )
        {
            Int32 offset1;
            if( IsV( offset, out offset1 ) )
            {
                fixed ( void* pv = &this.v )
                {
                    eval_t* a = (eval_t*)( ( Int32* )pv + offset1 );
                    result->_int = a->_int;
                }
            }
            else
            {
                fixed ( void* pv = this.fields )
                {
                    eval_t* a = (eval_t*)( ( Int32* )pv + offset1 );
                    result->_int = a->_int;
                }
            }
        }

        public unsafe void StoreInt( string_t offset, eval_t* value )
        {
            Int32 offset1;
            if( IsV( offset, out offset1 ) )
            {
                fixed ( void* pv = &this.v )
                {
                    eval_t* a = (eval_t*)( ( Int32* )pv + offset1 );
                    a->_int = value->_int;
                }
            }
            else
            {
                fixed ( void* pv = this.fields )
                {
                    eval_t* a = (eval_t*)( ( Int32* )pv + offset1 );
                    a->_int = value->_int;
                }
            }
        }

        public unsafe void LoadVector( string_t offset, eval_t* result )
        {
            Int32 offset1;
            if( IsV( offset, out offset1 ) )
            {
                fixed ( void* pv = &this.v )
                {
                    eval_t* a = (eval_t*)( ( Int32* )pv + offset1 );
                    result->vector[0] = a->vector[0];
                    result->vector[1] = a->vector[1];
                    result->vector[2] = a->vector[2];
                }
            }
            else
            {
                fixed ( void* pf = this.fields )
                {
                    eval_t* a = (eval_t*)( ( Int32* )pf + offset1 );
                    result->vector[0] = a->vector[0];
                    result->vector[1] = a->vector[1];
                    result->vector[2] = a->vector[2];
                }
            }
        }

        public unsafe void StoreVector( string_t offset, eval_t* value )
        {
            Int32 offset1;
            if( IsV( offset, out offset1 ) )
            {
                fixed ( void* pv = &this.v )
                {
                    eval_t* a = (eval_t*)( ( Int32* )pv + offset1 );
                    a->vector[0] = value->vector[0];
                    a->vector[1] = value->vector[1];
                    a->vector[2] = value->vector[2];
                }
            }
            else
            {
                fixed ( void* pf = this.fields )
                {
                    eval_t* a = (eval_t*)( ( Int32* )pf + offset1 );
                    a->vector[0] = value->vector[0];
                    a->vector[1] = value->vector[1];
                    a->vector[2] = value->vector[2];
                }
            }
        }

        public unsafe string_t GetInt( string_t offset )
        {
            Int32 offset1, result;
            if( IsV( offset, out offset1 ) )
            {
                fixed ( void* pv = &this.v )
                {
                    eval_t* a = (eval_t*)( ( Int32* )pv + offset1 );
                    result = a->_int;
                }
            }
            else
            {
                fixed ( void* pv = this.fields )
                {
                    eval_t* a = (eval_t*)( ( Int32* )pv + offset1 );
                    result = a->_int;
                }
            }
            return result;
        }

        public unsafe Single GetFloat( string_t offset )
        {
            Int32 offset1;
            Single result;
            if( IsV( offset, out offset1 ) )
            {
                fixed ( void* pv = &this.v )
                {
                    eval_t* a = (eval_t*)( ( Single* )pv + offset1 );
                    result = a->_float;
                }
            }
            else
            {
                fixed ( void* pv = this.fields )
                {
                    eval_t* a = (eval_t*)( ( Single* )pv + offset1 );
                    result = a->_float;
                }
            }
            return result;
        }

        public unsafe void SetFloat( string_t offset, Single value )
        {
            Int32 offset1;
            if( IsV( offset, out offset1 ) )
            {
                fixed ( void* pv = &this.v )
                {
                    eval_t* a = (eval_t*)( ( Single* )pv + offset1 );
                    a->_float = value;
                }
            }
            else
            {
                fixed ( void* pv = this.fields )
                {
                    eval_t* a = (eval_t*)( ( Single* )pv + offset1 );
                    a->_float = value;
                }
            }
        }

        public edict_t()
        {
            this.area = new link_t( this );
            this.leafnums = new Int16[progs.MAX_ENT_LEAFS];
            this.fields = new Single[( progs.EdictSize - entvars_t.SizeInBytes ) >> 2];
        }
    } // edict_t;

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
            this.type = ( UInt16 ) Common.LittleShort( ( Int16 ) this.type );
            this.ofs = ( UInt16 ) Common.LittleShort( ( Int16 ) this.ofs );
            this.s_name = Common.LittleLong( this.s_name );
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

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = progs.MAX_PARMS)]
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
            this.first_statement = Common.LittleLong( this.first_statement );
            this.parm_start = Common.LittleLong( this.parm_start );
            this.locals = Common.LittleLong( this.locals );
            this.s_name = Common.LittleLong( this.s_name );
            this.s_file = Common.LittleLong( this.s_file );
            this.numparms = Common.LittleLong( this.numparms );
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
            this.version = Common.LittleLong( this.version );
            this.crc = Common.LittleLong( this.crc );
            this.ofs_statements = Common.LittleLong( this.ofs_statements );
            this.numstatements = Common.LittleLong( this.numstatements );
            this.ofs_globaldefs = Common.LittleLong( this.ofs_globaldefs );
            this.numglobaldefs = Common.LittleLong( this.numglobaldefs );
            this.ofs_fielddefs = Common.LittleLong( this.ofs_fielddefs );
            this.numfielddefs = Common.LittleLong( this.numfielddefs );
            this.ofs_functions = Common.LittleLong( this.ofs_functions );
            this.numfunctions = Common.LittleLong( this.numfunctions );
            this.ofs_strings = Common.LittleLong( this.ofs_strings );
            this.numstrings = Common.LittleLong( this.numstrings );
            this.ofs_globals = Common.LittleLong( this.ofs_globals );
            this.numglobals = Common.LittleLong( this.numglobals );
            this.entityfields = Common.LittleLong( this.entityfields );
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
        public v3f v_forward;
        public v3f v_up;
        public v3f v_right;
        public Single trace_allsolid;
        public Single trace_startsolid;
        public Single trace_fraction;
        public v3f trace_endpos;
        public v3f trace_plane_normal;
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
