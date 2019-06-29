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

namespace SharpQuake
{
    partial class progs
    {
        private struct prstack_t
        {
            public int s;
            public dfunction_t f;
        } // prstack_t;

        public static int Argc
        {
            get
            {
                return _Argc;
            }
        }

        public static bool Trace;

        public static dfunction_t xFunction;

        private const int MAX_STACK_DEPTH = 32;

        private const int LOCALSTACK_SIZE = 2048;

        private static readonly string[] OpNames = new string[]
        {
            "DONE",

            "MUL_F",
            "MUL_V",
            "MUL_FV",
            "MUL_VF",

            "DIV",

            "ADD_F",
            "ADD_V",

            "SUB_F",
            "SUB_V",

            "EQ_F",
            "EQ_V",
            "EQ_S",
            "EQ_E",
            "EQ_FNC",

            "NE_F",
            "NE_V",
            "NE_S",
            "NE_E",
            "NE_FNC",

            "LE",
            "GE",
            "LT",
            "GT",

            "INDIRECT",
            "INDIRECT",
            "INDIRECT",
            "INDIRECT",
            "INDIRECT",
            "INDIRECT",

            "ADDRESS",

            "STORE_F",
            "STORE_V",
            "STORE_S",
            "STORE_ENT",
            "STORE_FLD",
            "STORE_FNC",

            "STOREP_F",
            "STOREP_V",
            "STOREP_S",
            "STOREP_ENT",
            "STOREP_FLD",
            "STOREP_FNC",

            "RETURN",

            "NOT_F",
            "NOT_V",
            "NOT_S",
            "NOT_ENT",
            "NOT_FNC",

            "IF",
            "IFNOT",

            "CALL0",
            "CALL1",
            "CALL2",
            "CALL3",
            "CALL4",
            "CALL5",
            "CALL6",
            "CALL7",
            "CALL8",

            "STATE",

            "GOTO",

            "AND",
            "OR",

            "BITAND",
            "BITOR"
        };

        // pr_trace
        private static prstack_t[] _Stack = new prstack_t[MAX_STACK_DEPTH]; // pr_stack

        private static int _Depth; // pr_depth

        private static int[] _LocalStack = new int[LOCALSTACK_SIZE]; // localstack
        private static int _LocalStackUsed; // localstack_used

        // pr_xfunction
        private static int _xStatement; // pr_xstatement

        private static int _Argc; // pr_argc

        /// <summary>
        /// PR_ExecuteProgram
        /// </summary>
        public unsafe static void Execute( int fnum )
        {
            if( fnum < 1 || fnum >= _Functions.Length )
            {
                if( progs.GlobalStruct.self != 0 )
                    Print( server.ProgToEdict( progs.GlobalStruct.self ) );
                host.Error( "PR_ExecuteProgram: NULL function" );
            }

            dfunction_t f = _Functions[fnum];

            int runaway = 100000;
            Trace = false;

            // make a stack frame
            int exitdepth = _Depth;

            int ofs;
            int s = EnterFunction( f );
            edict_t ed;

            while( true )
            {
                s++;	// next statement

                eval_t* a = (eval_t*)Get( _Statements[s].a );
                eval_t* b = (eval_t*)Get( _Statements[s].b );
                eval_t* c = (eval_t*)Get( _Statements[s].c );

                if( --runaway == 0 )
                    RunError( "runaway loop error" );

                xFunction.profile++;
                _xStatement = s;

                if( Trace )
                    PrintStatement( ref _Statements[s] );

                switch( (OP)_Statements[s].op )
                {
                    case OP.OP_ADD_F:
                        c->_float = a->_float + b->_float;
                        break;

                    case OP.OP_ADD_V:
                        c->vector[0] = a->vector[0] + b->vector[0];
                        c->vector[1] = a->vector[1] + b->vector[1];
                        c->vector[2] = a->vector[2] + b->vector[2];
                        break;

                    case OP.OP_SUB_F:
                        c->_float = a->_float - b->_float;
                        break;

                    case OP.OP_SUB_V:
                        c->vector[0] = a->vector[0] - b->vector[0];
                        c->vector[1] = a->vector[1] - b->vector[1];
                        c->vector[2] = a->vector[2] - b->vector[2];
                        break;

                    case OP.OP_MUL_F:
                        c->_float = a->_float * b->_float;
                        break;

                    case OP.OP_MUL_V:
                        c->_float = a->vector[0] * b->vector[0]
                                + a->vector[1] * b->vector[1]
                                + a->vector[2] * b->vector[2];
                        break;

                    case OP.OP_MUL_FV:
                        c->vector[0] = a->_float * b->vector[0];
                        c->vector[1] = a->_float * b->vector[1];
                        c->vector[2] = a->_float * b->vector[2];
                        break;

                    case OP.OP_MUL_VF:
                        c->vector[0] = b->_float * a->vector[0];
                        c->vector[1] = b->_float * a->vector[1];
                        c->vector[2] = b->_float * a->vector[2];
                        break;

                    case OP.OP_DIV_F:
                        c->_float = a->_float / b->_float;
                        break;

                    case OP.OP_BITAND:
                        c->_float = (int)a->_float & (int)b->_float;
                        break;

                    case OP.OP_BITOR:
                        c->_float = (int)a->_float | (int)b->_float;
                        break;

                    case OP.OP_GE:
                        c->_float = ( a->_float >= b->_float ) ? 1 : 0;
                        break;

                    case OP.OP_LE:
                        c->_float = ( a->_float <= b->_float ) ? 1 : 0;
                        break;

                    case OP.OP_GT:
                        c->_float = ( a->_float > b->_float ) ? 1 : 0;
                        break;

                    case OP.OP_LT:
                        c->_float = ( a->_float < b->_float ) ? 1 : 0;
                        break;

                    case OP.OP_AND:
                        c->_float = ( a->_float != 0 && b->_float != 0 ) ? 1 : 0;
                        break;

                    case OP.OP_OR:
                        c->_float = ( a->_float != 0 || b->_float != 0 ) ? 1 : 0;
                        break;

                    case OP.OP_NOT_F:
                        c->_float = ( a->_float != 0 ) ? 0 : 1;
                        break;

                    case OP.OP_NOT_V:
                        c->_float = ( a->vector[0] == 0 && a->vector[1] == 0 && a->vector[2] == 0 ) ? 1 : 0;
                        break;

                    case OP.OP_NOT_S:
                        c->_float = ( a->_string == 0 || String.IsNullOrEmpty( GetString( a->_string ) ) ) ? 1 : 0;
                        break;

                    case OP.OP_NOT_FNC:
                        c->_float = ( a->function == 0 ) ? 1 : 0;
                        break;

                    case OP.OP_NOT_ENT:
                        c->_float = ( server.ProgToEdict( a->edict ) == server.sv.edicts[0] ) ? 1 : 0;
                        break;

                    case OP.OP_EQ_F:
                        c->_float = ( a->_float == b->_float ) ? 1 : 0;
                        break;

                    case OP.OP_EQ_V:
                        c->_float = ( ( a->vector[0] == b->vector[0] ) &&
                            ( a->vector[1] == b->vector[1] ) &&
                            ( a->vector[2] == b->vector[2] ) ) ? 1 : 0;
                        break;

                    case OP.OP_EQ_S:
                        c->_float = ( GetString( a->_string ) == GetString( b->_string ) ) ? 1 : 0; //!strcmp(pr_strings + a->_string, pr_strings + b->_string);
                        break;

                    case OP.OP_EQ_E:
                        c->_float = ( a->_int == b->_int ) ? 1 : 0;
                        break;

                    case OP.OP_EQ_FNC:
                        c->_float = ( a->function == b->function ) ? 1 : 0;
                        break;

                    case OP.OP_NE_F:
                        c->_float = ( a->_float != b->_float ) ? 1 : 0;
                        break;

                    case OP.OP_NE_V:
                        c->_float = ( ( a->vector[0] != b->vector[0] ) ||
                            ( a->vector[1] != b->vector[1] ) || ( a->vector[2] != b->vector[2] ) ) ? 1 : 0;
                        break;

                    case OP.OP_NE_S:
                        c->_float = ( GetString( a->_string ) != GetString( b->_string ) ) ? 1 : 0; //strcmp(pr_strings + a->_string, pr_strings + b->_string);
                        break;

                    case OP.OP_NE_E:
                        c->_float = ( a->_int != b->_int ) ? 1 : 0;
                        break;

                    case OP.OP_NE_FNC:
                        c->_float = ( a->function != b->function ) ? 1 : 0;
                        break;

                    case OP.OP_STORE_F:
                    case OP.OP_STORE_ENT:
                    case OP.OP_STORE_FLD:		// integers
                    case OP.OP_STORE_S:
                    case OP.OP_STORE_FNC:		// pointers
                        b->_int = a->_int;
                        break;

                    case OP.OP_STORE_V:
                        b->vector[0] = a->vector[0];
                        b->vector[1] = a->vector[1];
                        b->vector[2] = a->vector[2];
                        break;

                    case OP.OP_STOREP_F:
                    case OP.OP_STOREP_ENT:
                    case OP.OP_STOREP_FLD:		// integers
                    case OP.OP_STOREP_S:
                    case OP.OP_STOREP_FNC:		// pointers
                        ed = EdictFromAddr( b->_int, out ofs );
                        ed.StoreInt( ofs, a );
                        break;

                    case OP.OP_STOREP_V:
                        ed = EdictFromAddr( b->_int, out ofs );
                        ed.StoreVector( ofs, a );
                        break;

                    case OP.OP_ADDRESS:
                        ed = server.ProgToEdict( a->edict );
                        if( ed == server.sv.edicts[0] && server.IsActive )
                            RunError( "assignment to world entity" );
                        c->_int = MakeAddr( a->edict, b->_int );
                        break;

                    case OP.OP_LOAD_F:
                    case OP.OP_LOAD_FLD:
                    case OP.OP_LOAD_ENT:
                    case OP.OP_LOAD_S:
                    case OP.OP_LOAD_FNC:
                        ed = server.ProgToEdict( a->edict );
                        ed.LoadInt( b->_int, c );
                        break;

                    case OP.OP_LOAD_V:
                        ed = server.ProgToEdict( a->edict );
                        ed.LoadVector( b->_int, c );
                        break;

                    case OP.OP_IFNOT:
                        if( a->_int == 0 )
                            s += _Statements[s].b - 1;	// offset the s++
                        break;

                    case OP.OP_IF:
                        if( a->_int != 0 )
                            s += _Statements[s].b - 1;	// offset the s++
                        break;

                    case OP.OP_GOTO:
                        s += _Statements[s].a - 1;	// offset the s++
                        break;

                    case OP.OP_CALL0:
                    case OP.OP_CALL1:
                    case OP.OP_CALL2:
                    case OP.OP_CALL3:
                    case OP.OP_CALL4:
                    case OP.OP_CALL5:
                    case OP.OP_CALL6:
                    case OP.OP_CALL7:
                    case OP.OP_CALL8:
                        _Argc = _Statements[s].op - (int)OP.OP_CALL0;
                        if( a->function == 0 )
                            RunError( "NULL function" );

                        dfunction_t newf = _Functions[a->function];

                        if( newf.first_statement < 0 )
                        {
                            // negative statements are built in functions
                            int i = -newf.first_statement;
                            if( i >= QBuiltins.Count )
                                RunError( "Bad builtin call number" );
                            QBuiltins.Execute( i );
                            break;
                        }

                        s = EnterFunction( newf );
                        break;

                    case OP.OP_DONE:
                    case OP.OP_RETURN:
                        float* ptr = (float*)_GlobalStructAddr;
                        int sta = _Statements[s].a;
                        ptr[OFS.OFS_RETURN + 0] = *(float*)Get( sta );
                        ptr[OFS.OFS_RETURN + 1] = *(float*)Get( sta + 1 );
                        ptr[OFS.OFS_RETURN + 2] = *(float*)Get( sta + 2 );

                        s = LeaveFunction();
                        if( _Depth == exitdepth )
                            return;		// all done
                        break;

                    case OP.OP_STATE:
                        ed = server.ProgToEdict( progs.GlobalStruct.self );
#if FPS_20
                        ed->v.nextthink = pr_global_struct->time + 0.05;
#else
                        ed.v.nextthink = progs.GlobalStruct.time + 0.1f;
#endif
                        if( a->_float != ed.v.frame )
                        {
                            ed.v.frame = a->_float;
                        }
                        ed.v.think = b->function;
                        break;

                    default:
                        RunError( "Bad opcode %i", _Statements[s].op );
                        break;
                }
            }
        }

        /// <summary>
        /// PR_RunError
        /// Aborts the currently executing function
        /// </summary>
        public static void RunError( string fmt, params object[] args )
        {
            PrintStatement( ref _Statements[_xStatement] );
            StackTrace();
            Con.Print( fmt, args );

            _Depth = 0;		// dump the stack so host_error can shutdown functions

            host.Error( "Program error" );
        }

        public static edict_t EdictFromAddr( int addr, out int ofs )
        {
            int prog = ( addr >> 16 ) & 0xFFFF;
            ofs = addr & 0xFFFF;
            return server.ProgToEdict( prog );
        }

        // PR_Profile_f
        private static void Profile_f()
        {
            if( _Functions == null )
                return;

            dfunction_t best;
            int num = 0;
            do
            {
                int max = 0;
                best = null;
                for( int i = 0; i < _Functions.Length; i++ )
                {
                    dfunction_t f = _Functions[i];
                    if( f.profile > max )
                    {
                        max = f.profile;
                        best = f;
                    }
                }
                if( best != null )
                {
                    if( num < 10 )
                        Con.Print( "{0,7} {1}\n", best.profile, GetString( best.s_name ) );
                    num++;
                    best.profile = 0;
                }
            } while( best != null );
        }

        /// <summary>
        /// PR_EnterFunction
        /// Returns the new program statement counter
        /// </summary>
        private static unsafe int EnterFunction( dfunction_t f )
        {
            _Stack[_Depth].s = _xStatement;
            _Stack[_Depth].f = xFunction;
            _Depth++;
            if( _Depth >= MAX_STACK_DEPTH )
                RunError( "stack overflow" );

            // save off any locals that the new function steps on
            int c = f.locals;
            if( _LocalStackUsed + c > LOCALSTACK_SIZE )
                RunError( "PR_ExecuteProgram: locals stack overflow\n" );

            for( int i = 0; i < c; i++ )
                _LocalStack[_LocalStackUsed + i] = *(int*)Get( f.parm_start + i );
            _LocalStackUsed += c;

            // copy parameters
            int o = f.parm_start;
            for( int i = 0; i < f.numparms; i++ )
            {
                for( int j = 0; j < f.parm_size[i]; j++ )
                {
                    Set( o, *(int*)Get( OFS.OFS_PARM0 + i * 3 + j ) );
                    o++;
                }
            }

            xFunction = f;
            return f.first_statement - 1;	// offset the s++
        }

        /// <summary>
        /// PR_StackTrace
        /// </summary>
        private static void StackTrace()
        {
            if( _Depth == 0 )
            {
                Con.Print( "<NO STACK>\n" );
                return;
            }

            _Stack[_Depth].f = progs.xFunction;
            for( int i = _Depth; i >= 0; i-- )
            {
                dfunction_t f = _Stack[i].f;

                if( f == null )
                {
                    Con.Print( "<NO FUNCTION>\n" );
                }
                else
                    Con.Print( "{0,12} : {1}\n", GetString( f.s_file ), GetString( f.s_name ) );
            }
        }

        /// <summary>
        /// PR_PrintStatement
        /// </summary>
        private static void PrintStatement( ref dstatement_t s )
        {
            if( s.op < OpNames.Length )
            {
                Con.Print( "{0,10} ", OpNames[s.op] );
            }

            OP op = (OP)s.op;
            if( op == OP.OP_IF || op == OP.OP_IFNOT )
                Con.Print( "{0}branch {1}", GlobalString( s.a ), s.b );
            else if( op == OP.OP_GOTO )
            {
                Con.Print( "branch {0}", s.a );
            }
            else if( (uint)( s.op - OP.OP_STORE_F ) < 6 )
            {
                Con.Print( GlobalString( s.a ) );
                Con.Print( GlobalStringNoContents( s.b ) );
            }
            else
            {
                if( s.a != 0 )
                    Con.Print( GlobalString( s.a ) );
                if( s.b != 0 )
                    Con.Print( GlobalString( s.b ) );
                if( s.c != 0 )
                    Con.Print( GlobalStringNoContents( s.c ) );
            }
            Con.Print( "\n" );
        }

        /// <summary>
        /// PR_LeaveFunction
        /// </summary>
        private static int LeaveFunction()
        {
            if( _Depth <= 0 )
                sys.Error( "prog stack underflow" );

            // restore locals from the stack
            int c = xFunction.locals;
            _LocalStackUsed -= c;
            if( _LocalStackUsed < 0 )
                RunError( "PR_ExecuteProgram: locals stack underflow\n" );

            for( int i = 0; i < c; i++ )
            {
                Set( xFunction.parm_start + i, _LocalStack[_LocalStackUsed + i] );
                //((int*)pr_globals)[pr_xfunction->parm_start + i] = localstack[localstack_used + i];
            }

            // up stack
            _Depth--;
            xFunction = _Stack[_Depth].f;

            return _Stack[_Depth].s;
        }

        private static int MakeAddr( int prog, int offset )
        {
            return ( ( prog & 0xFFFF ) << 16 ) + ( offset & 0xFFFF );
        }
    }
}
