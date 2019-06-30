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
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using SharpQuake.Framework;

namespace SharpQuake
{
    partial class progs
    {
        private struct gefv_cache
        {
            public ddef_t pcache;
            public String field;// char	field[MAX_FIELD_LEN];
        }

        public static Int32 EdictSize
        {
            get
            {
                return _EdictSize;
            }
        }

        //static StringBuilder _AddedStrings = new StringBuilder(4096);
        public static Int64 GlobalStructAddr
        {
            get
            {
                return _GlobalStructAddr;
            }
        }

        public static Int32 Crc
        {
            get
            {
                return _Crc;
            }
        }

        public static globalvars_t GlobalStruct;
        private const Int32 MAX_FIELD_LEN = 64;
        private const Int32 GEFV_CACHESIZE = 2;

        //gefv_cache;

        private static gefv_cache[] _gefvCache = new gefv_cache[GEFV_CACHESIZE]; // gefvCache
        private static Int32 _gefvPos;

        private static Int32[] _TypeSize = new Int32[8] // type_size
        {
            1, sizeof(Int32)/4, 1, 3, 1, 1, sizeof(Int32)/4, IntPtr.Size/4
        };

        private static CVar _NoMonsters;// = { "nomonsters", "0" };
        private static CVar _GameCfg;// = { "gamecfg", "0" };
        private static CVar _Scratch1;// = { "scratch1", "0" };
        private static CVar _Scratch2;// = { "scratch2", "0" };
        private static CVar _Scratch3;// = { "scratch3", "0" };
        private static CVar _Scratch4;// = { "scratch4", "0" };
        private static CVar _SavedGameCfg;// = { "savedgamecfg", "0", true };
        private static CVar _Saved1;// = { "saved1", "0", true };
        private static CVar _Saved2;// = { "saved2", "0", true };
        private static CVar _Saved3;// = { "saved3", "0", true };
        private static CVar _Saved4;// = { "saved4", "0", true };

        private static dprograms_t _Progs; // progs
        private static dfunction_t[] _Functions; // pr_functions
        private static String _Strings; // pr_strings
        private static ddef_t[] _FieldDefs; // pr_fielddefs
        private static ddef_t[] _GlobalDefs; // pr_globaldefs
        private static dstatement_t[] _Statements; // pr_statements

        // pr_global_struct
        private static Single[] _Globals; // Added by Uze: all data after globalvars_t (numglobals * 4 - globalvars_t.SizeInBytes)

        private static Int32 _EdictSize; // pr_edict_size	// in bytes
        private static UInt16 _Crc; // pr_crc
        private static GCHandle _HGlobalStruct;
        private static GCHandle _HGlobals;
        private static Int64 _GlobalStructAddr;
        private static Int64 _GlobalsAddr;
        private static List<String> _DynamicStrings = new List<String>( 512 );

        // PR_Init
        public static void Init()
        {
            Command.Add( "edict", PrintEdict_f );
            Command.Add( "edicts", PrintEdicts );
            Command.Add( "edictcount", EdictCount );
            Command.Add( "profile", Profile_f );
            Command.Add( "test5", Test5_f );

            if( _NoMonsters == null )
            {
                _NoMonsters = new CVar( "nomonsters", "0" );
                _GameCfg = new CVar( "gamecfg", "0" );
                _Scratch1 = new CVar( "scratch1", "0" );
                _Scratch2 = new CVar( "scratch2", "0" );
                _Scratch3 = new CVar( "scratch3", "0" );
                _Scratch4 = new CVar( "scratch4", "0" );
                _SavedGameCfg = new CVar( "savedgamecfg", "0", true );
                _Saved1 = new CVar( "saved1", "0", true );
                _Saved2 = new CVar( "saved2", "0", true );
                _Saved3 = new CVar( "saved3", "0", true );
                _Saved4 = new CVar( "saved4", "0", true );
            }
        }

        /// <summary>
        /// PR_LoadProgs
        /// </summary>
        public static void LoadProgs()
        {
            FreeHandles();

            QBuiltins.ClearState();
            _DynamicStrings.Clear();

            // flush the non-C variable lookup cache
            for( var i = 0; i < GEFV_CACHESIZE; i++ )
                _gefvCache[i].field = null;

            SharpQuake.Crc.Init( out _Crc );

            Byte[] buf = FileSystem.LoadFile( "progs.dat" );

            _Progs = Utilities.BytesToStructure<dprograms_t>( buf, 0 );
            if( _Progs == null )
                Utilities.Error( "PR_LoadProgs: couldn't load progs.dat" );
            Con.DPrint( "Programs occupy {0}K.\n", buf.Length / 1024 );

            for( var i = 0; i < buf.Length; i++ )
                SharpQuake.Crc.ProcessByte( ref _Crc, buf[i] );

            // byte swap the header
            _Progs.SwapBytes();

            if( _Progs.version != ProgDefs.PROG_VERSION )
                Utilities.Error( "progs.dat has wrong version number ({0} should be {1})", _Progs.version, ProgDefs.PROG_VERSION );
            if( _Progs.crc != ProgDefs.PROGHEADER_CRC )
                Utilities.Error( "progs.dat system vars have been modified, progdefs.h is out of date" );

            // Functions
            _Functions = new dfunction_t[_Progs.numfunctions];
            var offset = _Progs.ofs_functions;
            for( var i = 0; i < _Functions.Length; i++, offset += dfunction_t.SizeInBytes )
            {
                _Functions[i] = Utilities.BytesToStructure<dfunction_t>( buf, offset );
                _Functions[i].SwapBytes();
            }

            // strings
            offset = _Progs.ofs_strings;
            var str0 = offset;
            for( var i = 0; i < _Progs.numstrings; i++, offset++ )
            {
                // count string length
                while( buf[offset] != 0 )
                    offset++;
            }
            var length = offset - str0;
            _Strings = Encoding.ASCII.GetString( buf, str0, length );

            // Globaldefs
            _GlobalDefs = new ddef_t[_Progs.numglobaldefs];
            offset = _Progs.ofs_globaldefs;
            for( var i = 0; i < _GlobalDefs.Length; i++, offset += ddef_t.SizeInBytes )
            {
                _GlobalDefs[i] = Utilities.BytesToStructure<ddef_t>( buf, offset );
                _GlobalDefs[i].SwapBytes();
            }

            // Fielddefs
            _FieldDefs = new ddef_t[_Progs.numfielddefs];
            offset = _Progs.ofs_fielddefs;
            for( var i = 0; i < _FieldDefs.Length; i++, offset += ddef_t.SizeInBytes )
            {
                _FieldDefs[i] = Utilities.BytesToStructure<ddef_t>( buf, offset );
                _FieldDefs[i].SwapBytes();
                if( ( _FieldDefs[i].type & ProgDefs.DEF_SAVEGLOBAL ) != 0 )
                    Utilities.Error( "PR_LoadProgs: pr_fielddefs[i].type & DEF_SAVEGLOBAL" );
            }

            // Statements
            _Statements = new dstatement_t[_Progs.numstatements];
            offset = _Progs.ofs_statements;
            for( var i = 0; i < _Statements.Length; i++, offset += dstatement_t.SizeInBytes )
            {
                _Statements[i] = Utilities.BytesToStructure<dstatement_t>( buf, offset );
                _Statements[i].SwapBytes();
            }

            // Swap bytes inplace if needed
            if( !BitConverter.IsLittleEndian )
            {
                offset = _Progs.ofs_globals;
                for( var i = 0; i < _Progs.numglobals; i++, offset += 4 )
                {
                    SwapHelper.Swap4b( buf, offset );
                }
            }
            GlobalStruct = Utilities.BytesToStructure<globalvars_t>( buf, _Progs.ofs_globals );
            _Globals = new Single[_Progs.numglobals - globalvars_t.SizeInBytes / 4];
            Buffer.BlockCopy( buf, _Progs.ofs_globals + globalvars_t.SizeInBytes, _Globals, 0, _Globals.Length * 4 );

            _EdictSize = _Progs.entityfields * 4 + Edict.SizeInBytes - EntVars.SizeInBytes;
            ProgDefs.EdictSize = _EdictSize;
            _HGlobals = GCHandle.Alloc( _Globals, GCHandleType.Pinned );
            _GlobalsAddr = _HGlobals.AddrOfPinnedObject().ToInt64();

            _HGlobalStruct = GCHandle.Alloc( progs.GlobalStruct, GCHandleType.Pinned );
            _GlobalStructAddr = _HGlobalStruct.AddrOfPinnedObject().ToInt64();
        }

        // ED_PrintEdicts
        //
        // For debugging, prints all the entities in the current server
        public static void PrintEdicts()
        {
            Con.Print( "{0} entities\n", server.sv.num_edicts );
            for( var i = 0; i < server.sv.num_edicts; i++ )
                PrintNum( i );
        }

        public static Int32 StringOffset( String value )
        {
            var tmp = '\0' + value + '\0';
            var offset = _Strings.IndexOf( tmp, StringComparison.Ordinal );
            if( offset != -1 )
            {
                return MakeStingId( offset + 1, true );
            }

            for( var i = 0; i < _DynamicStrings.Count; i++ )
            {
                if( _DynamicStrings[i] == value )
                {
                    return MakeStingId( i, false );
                }
            }
            return -1;
        }

        /// <summary>
        /// ED_LoadFromFile
        /// The entities are directly placed in the array, rather than allocated with
        /// ED_Alloc, because otherwise an error loading the map would have entity
        /// number references out of order.
        ///
        /// Creates a server's entity / program execution context by
        /// parsing textual entity definitions out of an ent file.
        ///
        /// Used for both fresh maps and savegame loads.  A fresh map would also need
        /// to call ED_CallSpawnFunctions () to let the objects initialize themselves.
        /// </summary>
        public static void LoadFromFile( String data )
        {
            MemoryEdict ent = null;
            var inhibit = 0;
            progs.GlobalStruct.time = ( Single ) server.sv.time;

            // parse ents
            while( true )
            {
                // parse the opening brace
                data = Tokeniser.Parse( data );
                if( data == null )
                    break;

                if( Tokeniser.Token != "{" )
                    Utilities.Error( "ED_LoadFromFile: found {0} when expecting {", Tokeniser.Token );

                if( ent == null )
                    ent = server.EdictNum( 0 );
                else
                    ent = server.AllocEdict();
                data = ParseEdict( data, ent );

                // remove things from different skill levels or deathmatch
                if( host.Deathmatch != 0 )
                {
                    if( ( ( Int32 ) ent.v.spawnflags & SpawnFlags.SPAWNFLAG_NOT_DEATHMATCH ) != 0 )
                    {
                        server.FreeEdict( ent );
                        inhibit++;
                        continue;
                    }
                }
                else if( ( host.CurrentSkill == 0 && ( ( Int32 ) ent.v.spawnflags & SpawnFlags.SPAWNFLAG_NOT_EASY ) != 0 ) ||
                    ( host.CurrentSkill == 1 && ( ( Int32 ) ent.v.spawnflags & SpawnFlags.SPAWNFLAG_NOT_MEDIUM ) != 0 ) ||
                    ( host.CurrentSkill >= 2 && ( ( Int32 ) ent.v.spawnflags & SpawnFlags.SPAWNFLAG_NOT_HARD ) != 0 ) )
                {
                    server.FreeEdict( ent );
                    inhibit++;
                    continue;
                }

                //
                // immediately call spawn function
                //
                if( ent.v.classname == 0 )
                {
                    Con.Print( "No classname for:\n" );
                    Print( ent );
                    server.FreeEdict( ent );
                    continue;
                }

                // look for the spawn function
                var func = IndexOfFunction( GetString( ent.v.classname ) );
                if( func == -1 )
                {
                    Con.Print( "No spawn function for:\n" );
                    Print( ent );
                    server.FreeEdict( ent );
                    continue;
                }

                progs.GlobalStruct.self = server.EdictToProg( ent );
                Execute( func );
            }

            Con.DPrint( "{0} entities inhibited\n", inhibit );
        }

        /// <summary>
        /// ED_ParseEdict
        /// Parses an edict out of the given string, returning the new position
        /// ed should be a properly initialized empty edict.
        /// Used for initial level load and for savegames.
        /// </summary>
        public static String ParseEdict( String data, MemoryEdict ent )
        {
            var init = false;

            // clear it
            if( ent != server.sv.edicts[0] )	// hack
                ent.Clear();

            // go through all the dictionary pairs
            Boolean anglehack;
            while( true )
            {
                // parse key
                data = Tokeniser.Parse( data );
                if( Tokeniser.Token.StartsWith( "}" ) )
                    break;

                if( data == null )
                    Utilities.Error( "ED_ParseEntity: EOF without closing brace" );

                var token = Tokeniser.Token;

                // anglehack is to allow QuakeEd to write single scalar angles
                // and allow them to be turned into vectors. (FIXME...)
                if( token == "angle" )
                {
                    token = "angles";
                    anglehack = true;
                }
                else
                    anglehack = false;

                // FIXME: change light to _light to get rid of this hack
                if( token == "light" )
                    token = "light_lev";	// hack for single light def

                var keyname = token.TrimEnd();

                // parse value
                data = Tokeniser.Parse( data );
                if( data == null )
                    Utilities.Error( "ED_ParseEntity: EOF without closing brace" );

                if( Tokeniser.Token.StartsWith( "}" ) )
                    Utilities.Error( "ED_ParseEntity: closing brace without data" );

                init = true;

                // keynames with a leading underscore are used for utility comments,
                // and are immediately discarded by quake
                if( keyname[0] == '_' )
                    continue;

                ddef_t key = FindField( keyname );
                if( key == null )
                {
                    Con.Print( "'{0}' is not a field\n", keyname );
                    continue;
                }

                token = Tokeniser.Token;
                if( anglehack )
                {
                    token = "0 " + token + " 0";
                }

                if( !ParsePair( ent, key, token ) )
                    host.Error( "ED_ParseEdict: parse error" );
            }

            if( !init )
                ent.free = true;

            return data;
        }

        /// <summary>
        /// ED_Print
        /// For debugging
        /// </summary>
        public unsafe static void Print( MemoryEdict ed )
        {
            if( ed.free )
            {
                Con.Print( "FREE\n" );
                return;
            }

            Con.Print( "\nEDICT {0}:\n", server.NumForEdict( ed ) );
            for( var i = 1; i < _Progs.numfielddefs; i++ )
            {
                ddef_t d = _FieldDefs[i];
                var name = GetString( d.s_name );

                if( name.Length > 2 && name[name.Length - 2] == '_' )
                    continue; // skip _x, _y, _z vars

                var type = d.type & ~ProgDefs.DEF_SAVEGLOBAL;
                Int32 offset;
                if( ed.IsV( d.ofs, out offset ) )
                {
                    fixed ( void* ptr = &ed.v )
                    {
                        Int32* v = ( Int32* )ptr + offset;
                        if( IsEmptyField( type, v ) )
                            continue;

                        Con.Print( "{0,15} ", name );
                        Con.Print( "{0}\n", ValueString( (etype_t)d.type, (void*)v ) );
                    }
                }
                else
                {
                    fixed ( void* ptr = ed.fields )
                    {
                        Int32* v = ( Int32* )ptr + offset;
                        if( IsEmptyField( type, v ) )
                            continue;

                        Con.Print( "{0,15} ", name );
                        Con.Print( "{0}\n", ValueString( (etype_t)d.type, (void*)v ) );
                    }
                }
            }
        }

        public static String GetString( Int32 strId )
        {
            Int32 offset;
            if( IsStaticString( strId, out offset ) )
            {
                var i0 = offset;
                while( offset < _Strings.Length && _Strings[offset] != 0 )
                    offset++;

                var length = offset - i0;
                if( length > 0 )
                    return _Strings.Substring( i0, length );
            }
            else
            {
                if( offset < 0 || offset >= _DynamicStrings.Count )
                {
                    throw new ArgumentException( "Invalid string id!" );
                }
                return _DynamicStrings[offset];
            }

            return String.Empty;
        }

        public static Boolean SameName( Int32 name1, String name2 )
        {
            var offset = name1;
            if( offset + name2.Length > _Strings.Length )
                return false;

            for( var i = 0; i < name2.Length; i++, offset++ )
                if( _Strings[offset] != name2[i] )
                    return false;

            if( offset < _Strings.Length && _Strings[offset] != 0 )
                return false;

            return true;
        }

        /// <summary>
        /// Like ED_NewString but returns string id (string_t)
        /// </summary>
        public static Int32 NewString( String s )
        {
            var id = AllocString();
            StringBuilder sb = new StringBuilder( s.Length );
            var len = s.Length;
            for( var i = 0; i < len; i++ )
            {
                if( s[i] == '\\' && i < len - 1 )
                {
                    i++;
                    if( s[i] == 'n' )
                        sb.Append( '\n' );
                    else
                        sb.Append( '\\' );
                }
                else
                    sb.Append( s[i] );
            }
            SetString( id, sb.ToString() );
            return id;
        }

        public static Single GetEdictFieldFloat( MemoryEdict ed, String field, Single defValue = 0 )
        {
            ddef_t def = CachedSearch( ed, field );
            if( def == null )
                return defValue;

            return ed.GetFloat( def.ofs );
        }

        public static Boolean SetEdictFieldFloat( MemoryEdict ed, String field, Single value )
        {
            ddef_t def = CachedSearch( ed, field );
            if( def != null )
            {
                ed.SetFloat( def.ofs, value );
                return true;
            }
            return false;
        }

        public static Int32 AllocString()
        {
            var id = _DynamicStrings.Count;
            _DynamicStrings.Add( String.Empty );
            return MakeStingId( id, false );
        }

        public static void SetString( Int32 id, String value )
        {
            Int32 offset;
            if( IsStaticString( id, out offset ) )
            {
                throw new ArgumentException( "Static strings are read-only!" );
            }
            if( offset < 0 || offset >= _DynamicStrings.Count )
            {
                throw new ArgumentException( "Invalid string id!" );
            }
            _DynamicStrings[offset] = value;
        }

        /// <summary>
        /// ED_WriteGlobals
        /// </summary>
        public unsafe static void WriteGlobals( StreamWriter writer )
        {
            writer.WriteLine( "{" );
            for( var i = 0; i < _Progs.numglobaldefs; i++ )
            {
                ddef_t def = _GlobalDefs[i];
                etype_t type = (etype_t)def.type;
                if( ( def.type & ProgDefs.DEF_SAVEGLOBAL ) == 0 )
                    continue;

                type &= (etype_t)~ProgDefs.DEF_SAVEGLOBAL;

                if( type != etype_t.ev_string && type != etype_t.ev_float && type != etype_t.ev_entity )
                    continue;

                writer.Write( "\"" );
                writer.Write( GetString( def.s_name ) );
                writer.Write( "\" \"" );
                writer.Write( UglyValueString( type, (EVal*)Get( def.ofs ) ) );
                writer.WriteLine( "\"" );
            }
            writer.WriteLine( "}" );
        }

        /// <summary>
        /// ED_Write
        /// </summary>
        public unsafe static void WriteEdict( StreamWriter writer, MemoryEdict ed )
        {
            writer.WriteLine( "{" );

            if( ed.free )
            {
                writer.WriteLine( "}" );
                return;
            }

            for( var i = 1; i < _Progs.numfielddefs; i++ )
            {
                ddef_t d = _FieldDefs[i];
                var name = GetString( d.s_name );
                if( name != null && name.Length > 2 && name[name.Length - 2] == '_' )// [strlen(name) - 2] == '_')
                    continue;	// skip _x, _y, _z vars

                var type = d.type & ~ProgDefs.DEF_SAVEGLOBAL;
                Int32 offset1;
                if( ed.IsV( d.ofs, out offset1 ) )
                {
                    fixed ( void* ptr = &ed.v )
                    {
                        Int32* v = ( Int32* )ptr + offset1;
                        if( IsEmptyField( type, v ) )
                            continue;

                        writer.WriteLine( "\"{0}\" \"{1}\"", name, UglyValueString( (etype_t)d.type, (EVal*)v ) );
                    }
                }
                else
                {
                    fixed ( void* ptr = ed.fields )
                    {
                        Int32* v = ( Int32* )ptr + offset1;
                        if( IsEmptyField( type, v ) )
                            continue;

                        writer.WriteLine( "\"{0}\" \"{1}\"", name, UglyValueString( (etype_t)d.type, (EVal*)v ) );
                    }
                }
            }

            writer.WriteLine( "}" );
        }

        /// <summary>
        /// ED_ParseGlobals
        /// </summary>
        public static void ParseGlobals( String data )
        {
            while( true )
            {
                // parse key
                data = Tokeniser.Parse( data );
                if( Tokeniser.Token.StartsWith( "}" ) )
                    break;

                if( String.IsNullOrEmpty( data ) )
                    Utilities.Error( "ED_ParseEntity: EOF without closing brace" );

                var keyname = Tokeniser.Token;

                // parse value
                data = Tokeniser.Parse( data );
                if( String.IsNullOrEmpty( data ) )
                    Utilities.Error( "ED_ParseEntity: EOF without closing brace" );

                if( Tokeniser.Token.StartsWith( "}" ) )
                    Utilities.Error( "ED_ParseEntity: closing brace without data" );

                ddef_t key = FindGlobal( keyname );
                if( key == null )
                {
                    Con.Print( "'{0}' is not a global\n", keyname );
                    continue;
                }

                if( !ParseGlobalPair( key, Tokeniser.Token ) )
                    host.Error( "ED_ParseGlobals: parse error" );
            }
        }

        /// <summary>
        /// ED_PrintNum
        /// </summary>
        public static void PrintNum( Int32 ent )
        {
            Print( server.EdictNum( ent ) );
        }

        private static void Test5_f()
        {
            entity_t p = client.ViewEntity;
            if( p == null )
                return;

            OpenTK.Vector3 org = p.origin;

            for( var i = 0; i < server.sv.edicts.Length; i++ )
            {
                MemoryEdict ed = server.sv.edicts[i];

                if( ed.free )
                    continue;

                OpenTK.Vector3 vmin, vmax;
                MathLib.Copy( ref ed.v.absmax, out vmax );
                MathLib.Copy( ref ed.v.absmin, out vmin );

                if( org.X >= vmin.X && org.Y >= vmin.Y && org.Z >= vmin.Z &&
                    org.X <= vmax.X && org.Y <= vmax.Y && org.Z <= vmax.Z )
                {
                    Con.Print( "{0}\n", i );
                }
            }
        }

        private static void FreeHandles()
        {
            if( _HGlobals.IsAllocated )
            {
                _HGlobals.Free();
                _GlobalsAddr = 0;
            }
            if( _HGlobalStruct.IsAllocated )
            {
                _HGlobalStruct.Free();
                _GlobalStructAddr = 0;
            }
        }

        /// <summary>
        /// ED_PrintEdict_f
        /// For debugging, prints a single edict
        /// </summary>
        private static void PrintEdict_f()
        {
            var i = MathLib.atoi( Command.Argv( 1 ) );
            if( i >= server.sv.num_edicts )
            {
                Con.Print( "Bad edict number\n" );
                return;
            }
            progs.PrintNum( i );
        }

        // ED_Count
        //
        // For debugging
        private static void EdictCount()
        {
            Int32 active = 0, models = 0, solid = 0, step = 0;

            for( var i = 0; i < server.sv.num_edicts; i++ )
            {
                MemoryEdict ent = server.EdictNum( i );
                if( ent.free )
                    continue;
                active++;
                if( ent.v.solid != 0 )
                    solid++;
                if( ent.v.model != 0 )
                    models++;
                if( ent.v.movetype == Movetypes.MOVETYPE_STEP )
                    step++;
            }

            Con.Print( "num_edicts:{0}\n", server.sv.num_edicts );
            Con.Print( "active    :{0}\n", active );
            Con.Print( "view      :{0}\n", models );
            Con.Print( "touch     :{0}\n", solid );
            Con.Print( "step      :{0}\n", step );
        }

        /// <summary>
        /// ED_FindFunction
        /// </summary>
        private static dfunction_t FindFunction( String name )
        {
            var i = IndexOfFunction( name );
            if( i != -1 )
                return _Functions[i];

            return null;
        }

        private static Int32 IndexOfFunction( String name )
        {
            for( var i = 0; i < _Functions.Length; i++ )
            {
                if( SameName( _Functions[i].s_name, name ) )
                    return i;
            }
            return -1;
        }

        /// <summary>
        /// Since memory block containing original edict_t plus additional data
        /// is split into two fiels - edict_t.v and edict_t.fields we must check key.ofs
        /// to choose between thistwo parts.
        /// Warning: Key offset is in integers not bytes!
        /// </summary>
        private static unsafe Boolean ParsePair( MemoryEdict ent, ddef_t key, String s )
        {
            Int32 offset1;
            if( ent.IsV( key.ofs, out offset1 ) )
            {
                fixed ( EntVars* ptr = &ent.v )
                {
                    return ParsePair( ( Int32* )ptr + offset1, key, s );
                }
            }
            else
                fixed ( Single* ptr = ent.fields )
                {
                    return ParsePair( ptr + offset1, key, s );
                }
        }

        /// <summary>
        /// ED_ParseEpair
        /// Can parse either fields or globals returns false if error
        /// Uze: Warning! value pointer is already with correct offset (value = base + key.ofs)!
        /// </summary>
        private static unsafe Boolean ParsePair( void* value, ddef_t key, String s )
        {
            void* d = value;// (void *)((int *)base + key->ofs);

            switch( (etype_t)( key.type & ~ProgDefs.DEF_SAVEGLOBAL ) )
            {
                case etype_t.ev_string:
                    *( Int32* )d = NewString( s );// - pr_strings;
                    break;

                case etype_t.ev_float:
                    *( Single* )d = MathLib.atof( s );
                    break;

                case etype_t.ev_vector:
                    String[] vs = s.Split( ' ' );
                    ( ( Single* )d )[0] = MathLib.atof( vs[0] );
                    ( ( Single* )d )[1] = ( vs.Length > 1 ? MathLib.atof( vs[1] ) : 0 );
                    ( ( Single* )d )[2] = ( vs.Length > 2 ? MathLib.atof( vs[2] ) : 0 );
                    break;

                case etype_t.ev_entity:
                    *( Int32* )d = server.EdictToProg( server.EdictNum( MathLib.atoi( s ) ) );
                    break;

                case etype_t.ev_field:
                    var f = IndexOfField( s );
                    if( f == -1 )
                    {
                        Con.Print( "Can't find field {0}\n", s );
                        return false;
                    }
                    *( Int32* )d = GetInt32( _FieldDefs[f].ofs );
                    break;

                case etype_t.ev_function:
                    var func = IndexOfFunction( s );
                    if( func == -1 )
                    {
                        Con.Print( "Can't find function {0}\n", s );
                        return false;
                    }
                    *( Int32* )d = func;// - pr_functions;
                    break;

                default:
                    break;
            }
            return true;
        }

        private static Int32 IndexOfField( String name )
        {
            for( var i = 0; i < _FieldDefs.Length; i++ )
            {
                if( SameName( _FieldDefs[i].s_name, name ) )
                    return i;
            }
            return -1;
        }

        /// <summary>
        /// Returns true if ofs is inside GlobalStruct or false if ofs is in _Globals
        /// Out parameter offset is set to correct offset inside either GlobalStruct or _Globals
        /// </summary>
        private static Boolean IsGlobalStruct( Int32 ofs, out Int32 offset )
        {
            if( ofs < globalvars_t.SizeInBytes >> 2 )
            {
                offset = ofs;
                return true;
            }
            offset = ofs - ( globalvars_t.SizeInBytes >> 2 );
            return false;
        }

        /// <summary>
        /// Mimics G_xxx macros
        /// But globals are split too, so we must check offset and choose
        /// GlobalStruct or _Globals
        /// </summary>
        private static unsafe void* Get( Int32 offset )
        {
            Int32 offset1;
            if( IsGlobalStruct( offset, out offset1 ) )
            {
                return ( Int32* )_GlobalStructAddr + offset1;
            }
            return ( Int32* )_GlobalsAddr + offset1;
        }

        private static unsafe void Set( Int32 offset, Int32 value )
        {
            if( offset < globalvars_t.SizeInBytes >> 2 )
            {
                *( ( Int32* )_GlobalStructAddr + offset ) = value;
            }
            else
            {
                *( ( Int32* )_GlobalsAddr + offset - ( globalvars_t.SizeInBytes >> 2 ) ) = value;
            }
        }

        private static unsafe Int32 GetInt32( Int32 offset )
        {
            return *( ( Int32* )Get( offset ) );
        }

        /// <summary>
        /// ED_FindField
        /// </summary>
        private static ddef_t FindField( String name )
        {
            var i = IndexOfField( name );
            if( i != -1 )
                return _FieldDefs[i];

            return null;
        }

        /// <summary>
        /// PR_ValueString
        /// </summary>
        private static unsafe String ValueString( etype_t type, void* val )
        {
            String result;
            type &= (etype_t)~ProgDefs.DEF_SAVEGLOBAL;

            switch( type )
            {
                case etype_t.ev_string:
                    result = GetString( *( Int32* )val );
                    break;

                case etype_t.ev_entity:
                    result = "entity " + server.NumForEdict( server.ProgToEdict( *( Int32* )val ) );
                    break;

                case etype_t.ev_function:
                    dfunction_t f = _Functions[*( Int32* )val];
                    result = GetString( f.s_name ) + "()";
                    break;

                case etype_t.ev_field:
                    ddef_t def = FindField( *( Int32* )val );
                    result = "." + GetString( def.s_name );
                    break;

                case etype_t.ev_void:
                    result = "void";
                    break;

                case etype_t.ev_float:
                    result = ( *( Single* )val ).ToString( "F1", CultureInfo.InvariantCulture.NumberFormat );
                    break;

                case etype_t.ev_vector:
                    result = String.Format( CultureInfo.InvariantCulture.NumberFormat,
                        "{0,5:F1} {1,5:F1} {2,5:F1}", ( ( Single* )val )[0], ( ( Single* )val )[1], ( ( Single* )val )[2] );
                    break;

                case etype_t.ev_pointer:
                    result = "pointer";
                    break;

                default:
                    result = "bad type " + type.ToString();
                    break;
            }

            return result;
        }

        private static Int32 IndexOfField( Int32 ofs )
        {
            for( var i = 0; i < _FieldDefs.Length; i++ )
            {
                if( _FieldDefs[i].ofs == ofs )
                    return i;
            }
            return -1;
        }

        /// <summary>
        /// ED_FieldAtOfs
        /// </summary>
        private static ddef_t FindField( Int32 ofs )
        {
            var i = IndexOfField( ofs );
            if( i != -1 )
                return _FieldDefs[i];

            return null;
        }

        private static ddef_t CachedSearch( MemoryEdict ed, String field )
        {
            ddef_t def = null;
            for( var i = 0; i < GEFV_CACHESIZE; i++ )
            {
                if( field == _gefvCache[i].field )
                {
                    def = _gefvCache[i].pcache;
                    return def;
                }
            }

            def = FindField( field );

            _gefvCache[_gefvPos].pcache = def;
            _gefvCache[_gefvPos].field = field;
            _gefvPos ^= 1;

            return def;
        }

        private static Int32 MakeStingId( Int32 index, Boolean isStatic )
        {
            return ( ( isStatic ? 0 : 1 ) << 24 ) + ( index & 0xFFFFFF );
        }

        private static Boolean IsStaticString( Int32 stringId, out Int32 offset )
        {
            offset = stringId & 0xFFFFFF;
            return ( ( stringId >> 24 ) & 1 ) == 0;
        }

        /// <summary>
        /// PR_UglyValueString
        /// Returns a string describing *data in a type specific manner
        /// Easier to parse than PR_ValueString
        /// </summary>
        private static unsafe String UglyValueString( etype_t type, EVal* val )
        {
            type &= (etype_t)~ProgDefs.DEF_SAVEGLOBAL;
            String result;

            switch( type )
            {
                case etype_t.ev_string:
                    result = GetString( val->_string );
                    break;

                case etype_t.ev_entity:
                    result = server.NumForEdict( server.ProgToEdict( val->edict ) ).ToString();
                    break;

                case etype_t.ev_function:
                    dfunction_t f = _Functions[val->function];
                    result = GetString( f.s_name );
                    break;

                case etype_t.ev_field:
                    ddef_t def = FindField( val->_int );
                    result = GetString( def.s_name );
                    break;

                case etype_t.ev_void:
                    result = "void";
                    break;

                case etype_t.ev_float:
                    result = val->_float.ToString( "F6", CultureInfo.InvariantCulture.NumberFormat );
                    break;

                case etype_t.ev_vector:
                    result = String.Format( CultureInfo.InvariantCulture.NumberFormat,
                        "{0:F6} {1:F6} {2:F6}", val->vector[0], val->vector[1], val->vector[2] );
                    break;

                default:
                    result = "bad type " + type.ToString();
                    break;
            }

            return result;
        }

        private static unsafe Boolean IsEmptyField( Int32 type, Int32* v )
        {
            for( var j = 0; j < _TypeSize[type]; j++ )
                if( v[j] != 0 )
                    return false;

            return true;
        }

        /// <summary>
        /// ED_FindGlobal
        /// </summary>
        private static ddef_t FindGlobal( String name )
        {
            for( var i = 0; i < _GlobalDefs.Length; i++ )
            {
                ddef_t def = _GlobalDefs[i];
                if( name == GetString( def.s_name ) )
                    return def;
            }
            return null;
        }

        private static unsafe Boolean ParseGlobalPair( ddef_t key, String value )
        {
            Int32 offset;
            if( IsGlobalStruct( key.ofs, out offset ) )
            {
                return ParsePair( ( Single* )_GlobalStructAddr + offset, key, value );
            }
            return ParsePair( ( Single* )_GlobalsAddr + offset, key, value );
        }

        /// <summary>
        /// PR_GlobalString
        /// Returns a string with a description and the contents of a global,
        /// padded to 20 field width
        /// </summary>
        private static unsafe String GlobalString( Int32 ofs )
        {
            var line = String.Empty;
            void* val = Get( ofs );// (void*)&pr_globals[ofs];
            ddef_t def = GlobalAtOfs( ofs );
            if( def == null )
                line = String.Format( "{0}(???)", ofs );
            else
            {
                var s = ValueString( (etype_t)def.type, val );
                line = String.Format( "{0}({1}){2} ", ofs, GetString( def.s_name ), s );
            }

            line = line.PadRight( 20 );

            return line;
        }

        /// <summary>
        /// PR_GlobalStringNoContents
        /// </summary>
        private static String GlobalStringNoContents( Int32 ofs )
        {
            var line = String.Empty;
            ddef_t def = GlobalAtOfs( ofs );
            if( def == null )
                line = String.Format( "{0}(???)", ofs );
            else
                line = String.Format( "{0}({1}) ", ofs, GetString( def.s_name ) );

            line = line.PadRight( 20 );

            return line;
        }

        /// <summary>
        /// ED_GlobalAtOfs
        /// </summary>
        private static ddef_t GlobalAtOfs( Int32 ofs )
        {
            for( var i = 0; i < _GlobalDefs.Length; i++ )
            {
                ddef_t def = _GlobalDefs[i];
                if( def.ofs == ofs )
                    return def;
            }
            return null;
        }
    }
}
