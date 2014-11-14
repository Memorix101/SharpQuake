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
using System.Text;
using System.Runtime.InteropServices;
using System.Globalization;
using System.IO;

namespace SharpQuake
{
    partial class Progs
    {
        const int MAX_FIELD_LEN = 64;
        const int GEFV_CACHESIZE = 2;

        struct gefv_cache
        {
	        public ddef_t pcache;
	        public string field;// char	field[MAX_FIELD_LEN];
        } //gefv_cache;

        static gefv_cache[] _gefvCache = new gefv_cache[GEFV_CACHESIZE]; // gefvCache
        static int _gefvPos;
        
        static int[] _TypeSize = new int[8] // type_size
        {
            1, sizeof(int)/4, 1, 3, 1, 1, sizeof(int)/4, IntPtr.Size/4
        };

        static Cvar _NoMonsters;// = { "nomonsters", "0" };
        static Cvar _GameCfg;// = { "gamecfg", "0" };
        static Cvar _Scratch1;// = { "scratch1", "0" };
        static Cvar _Scratch2;// = { "scratch2", "0" };
        static Cvar _Scratch3;// = { "scratch3", "0" };
        static Cvar _Scratch4;// = { "scratch4", "0" };
        static Cvar _SavedGameCfg;// = { "savedgamecfg", "0", true };
        static Cvar _Saved1;// = { "saved1", "0", true };
        static Cvar _Saved2;// = { "saved2", "0", true };
        static Cvar _Saved3;// = { "saved3", "0", true };
        static Cvar _Saved4;// = { "saved4", "0", true };

        static dprograms_t _Progs; // progs
        static dfunction_t[] _Functions; // pr_functions
        static string _Strings; // pr_strings
        static ddef_t[] _FieldDefs; // pr_fielddefs
        static ddef_t[] _GlobalDefs; // pr_globaldefs
        static dstatement_t[] _Statements; // pr_statements
        public static globalvars_t GlobalStruct; // pr_global_struct
        static float[] _Globals; // Added by Uze: all data after globalvars_t (numglobals * 4 - globalvars_t.SizeInBytes)
        static int _EdictSize; // pr_edict_size	// in bytes
        static ushort _Crc; // pr_crc
        static GCHandle _HGlobalStruct;
        static GCHandle _HGlobals;
        static long _GlobalStructAddr;
        static long _GlobalsAddr;
        static List<string> _DynamicStrings = new List<string>(512);
        //static StringBuilder _AddedStrings = new StringBuilder(4096);

        public static int EdictSize
        {
            get { return _EdictSize; }
        }
        public static long GlobalStructAddr
        {
            get { return _GlobalStructAddr; }
        }
        public static int Crc
        {
            get { return _Crc; }
        }

        // PR_Init
        public static void Init()
        {
            Cmd.Add("edict", PrintEdict_f);
            Cmd.Add("edicts", PrintEdicts);
            Cmd.Add("edictcount", EdictCount);
            Cmd.Add("profile", Profile_f);
            Cmd.Add("test5", Test5_f);

            if (_NoMonsters == null)
            {
                _NoMonsters = new Cvar("nomonsters", "0");
                _GameCfg = new Cvar("gamecfg", "0");
                _Scratch1 = new Cvar("scratch1", "0");
                _Scratch2 = new Cvar("scratch2", "0");
                _Scratch3 = new Cvar("scratch3", "0");
                _Scratch4 = new Cvar("scratch4", "0");
                _SavedGameCfg = new Cvar("savedgamecfg", "0", true);
                _Saved1 = new Cvar("saved1", "0", true);
                _Saved2 = new Cvar("saved2", "0", true);
                _Saved3 = new Cvar("saved3", "0", true);
                _Saved4 = new Cvar("saved4", "0", true);
            }
        }

        static void Test5_f()
        {
            entity_t p = Client.ViewEntity;
            if (p == null)
                return;
            
            OpenTK.Vector3 org = p.origin;

            for (int i = 0; i < Server.sv.edicts.Length; i++)
            {
                edict_t ed = Server.sv.edicts[i];
                
                if (ed.free)
                    continue;
                
                OpenTK.Vector3 vmin, vmax;
                Mathlib.Copy(ref ed.v.absmax, out vmax);
                Mathlib.Copy(ref ed.v.absmin, out vmin);

                if (org.X >= vmin.X && org.Y >= vmin.Y && org.Z >= vmin.Z &&
                    org.X <= vmax.X && org.Y <= vmax.Y && org.Z <= vmax.Z)
                {
                    Con.Print("{0}\n", i);
                }
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
            for (int i = 0; i < GEFV_CACHESIZE; i++)
                _gefvCache[i].field = null;

            CRC.Init(out _Crc);

            byte[] buf = Common.LoadFile("progs.dat");

            _Progs = Sys.BytesToStructure<dprograms_t>(buf, 0);
            if (_Progs == null)
                Sys.Error("PR_LoadProgs: couldn't load progs.dat");
            Con.DPrint("Programs occupy {0}K.\n", buf.Length / 1024);

            for (int i = 0; i < buf.Length; i++)
                CRC.ProcessByte(ref _Crc, buf[i]);

            // byte swap the header
            _Progs.SwapBytes();

            if (_Progs.version != PROG_VERSION)
                Sys.Error("progs.dat has wrong version number ({0} should be {1})", _Progs.version, PROG_VERSION);
            if (_Progs.crc != PROGHEADER_CRC)
                Sys.Error("progs.dat system vars have been modified, progdefs.h is out of date");

            // Functions
            _Functions = new dfunction_t[_Progs.numfunctions];
            int offset = _Progs.ofs_functions;
            for (int i = 0; i < _Functions.Length; i++, offset += dfunction_t.SizeInBytes)
            {
                _Functions[i] = Sys.BytesToStructure<dfunction_t>(buf, offset);
                _Functions[i].SwapBytes();
            }

            // strings
            offset = _Progs.ofs_strings;
            int str0 = offset;
            for(int i = 0; i < _Progs.numstrings; i++, offset++)
            {
                // count string length
                while (buf[offset] != 0)
                    offset++;
            }
            int length = offset - str0;
            _Strings = Encoding.ASCII.GetString(buf, str0, length);

            // Globaldefs
            _GlobalDefs = new ddef_t[_Progs.numglobaldefs];
            offset = _Progs.ofs_globaldefs;
            for (int i = 0; i < _GlobalDefs.Length; i++, offset += ddef_t.SizeInBytes)
            {
                _GlobalDefs[i] = Sys.BytesToStructure<ddef_t>(buf, offset);
                _GlobalDefs[i].SwapBytes();
            }

            // Fielddefs
            _FieldDefs = new ddef_t[_Progs.numfielddefs];
            offset = _Progs.ofs_fielddefs;
            for (int i = 0; i < _FieldDefs.Length; i++, offset += ddef_t.SizeInBytes)
            {
                _FieldDefs[i] = Sys.BytesToStructure<ddef_t>(buf, offset);
                _FieldDefs[i].SwapBytes();
                if ((_FieldDefs[i].type & DEF_SAVEGLOBAL) != 0)
                    Sys.Error("PR_LoadProgs: pr_fielddefs[i].type & DEF_SAVEGLOBAL");
            }

            // Statements
            _Statements = new dstatement_t[_Progs.numstatements];
            offset = _Progs.ofs_statements;
            for (int i = 0; i < _Statements.Length; i++, offset += dstatement_t.SizeInBytes)
            {
                _Statements[i] = Sys.BytesToStructure<dstatement_t>(buf, offset);
                _Statements[i].SwapBytes();
            }

            // Swap bytes inplace if needed
            if (!BitConverter.IsLittleEndian)
            {
                offset = _Progs.ofs_globals;
                for (int i = 0; i < _Progs.numglobals; i++, offset += 4)
                {
                    SwapHelper.Swap4b(buf, offset);
                }
            }
            GlobalStruct = Sys.BytesToStructure<globalvars_t>(buf, _Progs.ofs_globals);
            _Globals = new float[_Progs.numglobals - globalvars_t.SizeInBytes / 4];
            Buffer.BlockCopy(buf, _Progs.ofs_globals + globalvars_t.SizeInBytes, _Globals, 0, _Globals.Length * 4);

            _EdictSize = _Progs.entityfields * 4 + dedict_t.SizeInBytes - entvars_t.SizeInBytes;

            _HGlobals = GCHandle.Alloc(_Globals, GCHandleType.Pinned);
            _GlobalsAddr = _HGlobals.AddrOfPinnedObject().ToInt64();

            _HGlobalStruct = GCHandle.Alloc(Progs.GlobalStruct, GCHandleType.Pinned);
            _GlobalStructAddr = _HGlobalStruct.AddrOfPinnedObject().ToInt64();
        }

        private static void FreeHandles()
        {
            if (_HGlobals.IsAllocated)
            {
                _HGlobals.Free();
                _GlobalsAddr = 0;
            }
            if (_HGlobalStruct.IsAllocated)
            {
                _HGlobalStruct.Free();
                _GlobalStructAddr = 0;
            }
        }


        /// <summary>
        /// ED_PrintEdict_f
        /// For debugging, prints a single edict
        /// </summary>
        static void PrintEdict_f()
        {
            int i = Common.atoi(Cmd.Argv(1));
            if (i >= Server.sv.num_edicts)
            {
                Con.Print("Bad edict number\n");
                return;
            }
            Progs.PrintNum(i);
        }

        // ED_Count
        //
        // For debugging
        static void EdictCount()
        {
            int active = 0, models = 0, solid = 0, step = 0;

            for (int i = 0; i < Server.sv.num_edicts; i++)
            {
                edict_t ent = Server.EdictNum(i);
                if (ent.free)
                    continue;
                active++;
                if (ent.v.solid != 0)
                    solid++;
                if (ent.v.model != 0)
                    models++;
                if (ent.v.movetype == Movetypes.MOVETYPE_STEP)
                    step++;
            }

            Con.Print("num_edicts:{0}\n", Server.sv.num_edicts);
            Con.Print("active    :{0}\n", active);
            Con.Print("view      :{0}\n", models);
            Con.Print("touch     :{0}\n", solid);
            Con.Print("step      :{0}\n", step);
        }

        
        // ED_PrintEdicts
        //
        // For debugging, prints all the entities in the current server
        public static void PrintEdicts()
        {
            Con.Print("{0} entities\n", Server.sv.num_edicts);
            for (int i = 0; i < Server.sv.num_edicts; i++)
                PrintNum(i);
        }

        public static int StringOffset(string value)
        {
            string tmp = '\0' + value + '\0';
            int offset = _Strings.IndexOf(tmp, StringComparison.Ordinal);
            if (offset != -1)
            {
                return MakeStingId(offset + 1, true);
            }

            for (int i = 0; i < _DynamicStrings.Count; i++)
            {
                if (_DynamicStrings[i] == value)
                {
                    return MakeStingId(i, false);
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
        public static void LoadFromFile(string data)
        {
            edict_t ent = null;
            int inhibit = 0;
            Progs.GlobalStruct.time = (float)Server.sv.time;

            // parse ents
            while (true)
            {
                // parse the opening brace	
                data = Common.Parse(data);
                if (data == null)
                    break;
                
                if (Common.Token != "{")
                    Sys.Error("ED_LoadFromFile: found {0} when expecting {", Common.Token);

                if (ent == null)
                    ent = Server.EdictNum(0);
                else
                    ent = Server.AllocEdict();
                data = ParseEdict(data, ent);

                // remove things from different skill levels or deathmatch
                if (Host.Deathmatch != 0)
                {
                    if (((int)ent.v.spawnflags & SpawnFlags.SPAWNFLAG_NOT_DEATHMATCH) != 0)
                    {
                        Server.FreeEdict(ent);
                        inhibit++;
                        continue;
                    }
                }
                else if ((Host.CurrentSkill == 0 && ((int)ent.v.spawnflags & SpawnFlags.SPAWNFLAG_NOT_EASY) != 0) ||
                    (Host.CurrentSkill == 1 && ((int)ent.v.spawnflags & SpawnFlags.SPAWNFLAG_NOT_MEDIUM) != 0) ||
                    (Host.CurrentSkill >= 2 && ((int)ent.v.spawnflags & SpawnFlags.SPAWNFLAG_NOT_HARD) != 0))
                {
                    Server.FreeEdict(ent);
                    inhibit++;
                    continue;
                }

                //
                // immediately call spawn function
                //
                if (ent.v.classname == 0)
                {
                    Con.Print("No classname for:\n");
                    Print(ent);
                    Server.FreeEdict(ent);
                    continue;
                }

                // look for the spawn function
                int func = IndexOfFunction(GetString(ent.v.classname));
                if (func == -1)
                {
                    Con.Print("No spawn function for:\n");
                    Print(ent);
                    Server.FreeEdict(ent);
                    continue;
                }

                Progs.GlobalStruct.self = Server.EdictToProg(ent);
                Execute(func);
            }

            Con.DPrint("{0} entities inhibited\n", inhibit);
        }

        /// <summary>
        /// ED_FindFunction
        /// </summary>
        static dfunction_t FindFunction(string name)
        {
            int i = IndexOfFunction(name);
            if (i != -1)
                return _Functions[i];

            return null;
        }

        static int IndexOfFunction(string name)
        {
            for (int i = 0; i < _Functions.Length; i++)
            {
                if (SameName(_Functions[i].s_name, name))
                    return i;
            }
            return -1;
        }

        /// <summary>
        /// ED_ParseEdict
        /// Parses an edict out of the given string, returning the new position
        /// ed should be a properly initialized empty edict.
        /// Used for initial level load and for savegames.
        /// </summary>
        public static string ParseEdict(string data, edict_t ent)
        {
            bool init = false;

            // clear it
            if (ent != Server.sv.edicts[0])	// hack
                ent.Clear();

            // go through all the dictionary pairs
            bool anglehack;
            while (true)
            {
                // parse key
                data = Common.Parse(data);
                if (Common.Token.StartsWith("}"))
                    break;

                if (data == null)
                    Sys.Error("ED_ParseEntity: EOF without closing brace");

                string token = Common.Token;

                // anglehack is to allow QuakeEd to write single scalar angles
                // and allow them to be turned into vectors. (FIXME...)
                if (token == "angle")
                {
                    token = "angles";
                    anglehack = true;
                }
                else
                    anglehack = false;

                // FIXME: change light to _light to get rid of this hack
                if (token == "light")
                    token = "light_lev";	// hack for single light def

                string keyname = token.TrimEnd();

                // parse value	
                data = Common.Parse(data);
                if (data == null)
                    Sys.Error("ED_ParseEntity: EOF without closing brace");

                if (Common.Token.StartsWith("}"))
                    Sys.Error("ED_ParseEntity: closing brace without data");

                init = true;

                // keynames with a leading underscore are used for utility comments,
                // and are immediately discarded by quake
                if (keyname[0] == '_')
                    continue;

                ddef_t key = FindField(keyname);
                if (key == null)
                {
                    Con.Print("'{0}' is not a field\n", keyname);
                    continue;
                }
                
                token = Common.Token;
                if (anglehack)
                {
                    token = "0 " + token +" 0";
                }

                if (!ParsePair(ent, key, token))
                    Host.Error("ED_ParseEdict: parse error");
            }

            if (!init)
                ent.free = true;

            return data;
        }

        /// <summary>
        /// Since memory block containing original edict_t plus additional data
        /// is split into two fiels - edict_t.v and edict_t.fields we must check key.ofs
        /// to choose between thistwo parts.
        /// Warning: Key offset is in integers not bytes!
        /// </summary>
        static unsafe bool ParsePair(edict_t ent, ddef_t key, string s)
        {
            int offset1;
            if (ent.IsV(key.ofs, out offset1))
            {
                fixed (entvars_t* ptr = &ent.v)
                {
                    return ParsePair((int*)ptr + offset1, key, s);
                }
            }
            else
                fixed (float* ptr = ent.fields)
                {
                    return ParsePair(ptr + offset1, key, s);
                }
        }

        /// <summary>
        /// ED_ParseEpair
        /// Can parse either fields or globals returns false if error
        /// Uze: Warning! value pointer is already with correct offset (value = base + key.ofs)!
        /// </summary>
        static unsafe bool ParsePair(void* value, ddef_t key, string s)
        {
            void* d = value;// (void *)((int *)base + key->ofs);

            switch ((etype_t)(key.type & ~DEF_SAVEGLOBAL))
            {
                case etype_t.ev_string:
                    *(int*)d = NewString(s);// - pr_strings;
                    break;

                case etype_t.ev_float:
                    *(float*)d = Common.atof(s);
                    break;

                case etype_t.ev_vector:
                    string[] vs = s.Split(' ');
                    ((float*)d)[0] = Common.atof(vs[0]);
                    ((float*)d)[1] = (vs.Length > 1 ? Common.atof(vs[1]) : 0);
                    ((float*)d)[2] = (vs.Length > 2 ? Common.atof(vs[2]) : 0);
                    break;

                case etype_t.ev_entity:
                    *(int*)d = Server.EdictToProg(Server.EdictNum(Common.atoi(s)));
                    break;

                case etype_t.ev_field:
                    int f = IndexOfField(s);
                    if (f == -1)
                    {
                        Con.Print("Can't find field {0}\n", s);
                        return false;
                    }
                    *(int*)d = GetInt32(_FieldDefs[f].ofs);
                    break;

                case etype_t.ev_function:
                    int func = IndexOfFunction(s);
                    if (func == -1)
                    {
                        Con.Print("Can't find function {0}\n", s);
                        return false;
                    }
                    *(int*)d = func;// - pr_functions;
                    break;

                default:
                    break;
            }
            return true;
        }

        static int IndexOfField(string name)
        {
            for (int i = 0; i < _FieldDefs.Length; i++)
            {
                if (SameName(_FieldDefs[i].s_name, name))
                    return i;
            }
            return -1;
        }

        /// <summary>
        /// Returns true if ofs is inside GlobalStruct or false if ofs is in _Globals
        /// Out parameter offset is set to correct offset inside either GlobalStruct or _Globals
        /// </summary>
        static bool IsGlobalStruct(int ofs, out int offset)
        {
            if (ofs < globalvars_t.SizeInBytes >> 2)
            {
                offset = ofs;
                return true;
            }
            offset = ofs - (globalvars_t.SizeInBytes >> 2);
            return false;
        }

        /// <summary>
        /// Mimics G_xxx macros
        /// But globals are split too, so we must check offset and choose
        /// GlobalStruct or _Globals
        /// </summary>
        static unsafe void* Get(int offset)
        {
            int offset1;
            if (IsGlobalStruct(offset, out offset1))
            {
                return (int*)_GlobalStructAddr + offset1;
            }
            return (int*)_GlobalsAddr + offset1;
        }

        static unsafe void Set(int offset, int value)
        {
            if (offset < globalvars_t.SizeInBytes >> 2)
            {
                *((int*)_GlobalStructAddr + offset) = value;
            }
            else
            {
                *((int*)_GlobalsAddr + offset - (globalvars_t.SizeInBytes >> 2)) = value;
            }
        }

        static unsafe int GetInt32(int offset)
        {
            return *((int*)Get(offset));
        }

        /// <summary>
        /// ED_FindField
        /// </summary>
        static ddef_t FindField(string name)
        {
            int i = IndexOfField(name);
            if (i != -1)
                return _FieldDefs[i];
            
            return null;
        }

        /// <summary>
        /// ED_Print
        /// For debugging
        /// </summary>
        public unsafe static void Print(edict_t ed)
        {
            if (ed.free)
            {
                Con.Print("FREE\n");
                return;
            }

            Con.Print("\nEDICT {0}:\n", Server.NumForEdict(ed));
            for (int i = 1; i < _Progs.numfielddefs; i++)
            {
                ddef_t d = _FieldDefs[i];
                string name = GetString(d.s_name);

                if (name.Length > 2 && name[name.Length - 2] == '_')
                    continue; // skip _x, _y, _z vars

                int type = d.type & ~DEF_SAVEGLOBAL;
                int offset;
                if (ed.IsV(d.ofs, out offset))
                {
                    fixed (void* ptr = &ed.v)
                    {
                        int* v = (int*)ptr + offset;
                        if (IsEmptyField(type, v))
                            continue;

                        Con.Print("{0,15} ", name);
                        Con.Print("{0}\n", ValueString((etype_t)d.type, (void*)v));
                    }
                }
                else
                {
                    fixed (void* ptr = ed.fields)
                    {
                        int* v = (int*)ptr + offset;
                        if (IsEmptyField(type, v))
                            continue;

                        Con.Print("{0,15} ", name);
                        Con.Print("{0}\n", ValueString((etype_t)d.type, (void*)v));
                    }
                }
            }
        }

        /// <summary>
        /// PR_ValueString
        /// </summary>
        static unsafe string ValueString(etype_t type, void* val)
        {
            string result;
            type &= (etype_t)~DEF_SAVEGLOBAL;

            switch (type)
            {
                case etype_t.ev_string:
                    result = GetString(*(int*)val);
                    break;

                case etype_t.ev_entity:
                    result = "entity " + Server.NumForEdict(Server.ProgToEdict(*(int*)val));
                    break;

                case etype_t.ev_function:
                    dfunction_t f = _Functions[*(int*)val];
                    result = GetString(f.s_name) + "()";
                    break;

                case etype_t.ev_field:
                    ddef_t def = FindField(*(int*)val);
                    result = "." + GetString(def.s_name);
                    break;

                case etype_t.ev_void:
                    result = "void";
                    break;

                case etype_t.ev_float:
                    result = (*(float*)val).ToString("F1", CultureInfo.InvariantCulture.NumberFormat);
                    break;

                case etype_t.ev_vector:
                    result = String.Format(CultureInfo.InvariantCulture.NumberFormat,
                        "{0,5:F1} {1,5:F1} {2,5:F1}", ((float*)val)[0], ((float*)val)[1], ((float*)val)[2]);
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

        static int IndexOfField(int ofs)
        {
            for (int i = 0; i < _FieldDefs.Length; i++)
            {
                if (_FieldDefs[i].ofs == ofs)
                    return i;
            }
            return -1;
        }

        /// <summary>
        /// ED_FieldAtOfs
        /// </summary>
        static ddef_t FindField(int ofs)
        {
            int i = IndexOfField(ofs);
            if (i != -1)
                return _FieldDefs[i];
            
            return null;
        }

        public static string GetString(int strId)
        {
            int offset;
            if (IsStaticString(strId, out offset))
            {
                int i0 = offset;
                while (offset < _Strings.Length && _Strings[offset] != 0)
                    offset++;

                int length = offset - i0;
                if (length > 0)
                    return _Strings.Substring(i0, length);
            }
            else
            {
                if (offset < 0 || offset >= _DynamicStrings.Count)
                {
                    throw new ArgumentException("Invalid string id!");
                }
                return _DynamicStrings[offset];
            }

            return String.Empty;
        }

        public static bool SameName(int name1, string name2)
        {
            int offset = name1;
            if (offset + name2.Length > _Strings.Length)
                return false;

            for (int i = 0; i < name2.Length; i++, offset++)
                if (_Strings[offset] != name2[i])
                    return false;

            if (offset < _Strings.Length && _Strings[offset] != 0)
                return false;
            
            return true;
        }

        /// <summary>
        /// Like ED_NewString but returns string id (string_t)
        /// </summary>
        public static int NewString(string s)
        {
            int id = AllocString();
            StringBuilder sb = new StringBuilder(s.Length);
            int len = s.Length;
            for (int i = 0; i < len;  i++)
            {
                if (s[i] == '\\' && i < len - 1)
                {
                    i++;
                    if (s[i] == 'n')
                        sb.Append('\n');
                    else
                        sb.Append('\\');
                }
                else
                    sb.Append(s[i]);
            }
            SetString(id, sb.ToString());
            return id;
        }

        static ddef_t CachedSearch(edict_t ed, string field)
        {
            ddef_t def = null;
            for (int i = 0; i < GEFV_CACHESIZE; i++)
            {
                if (field == _gefvCache[i].field)
                {
                    def = _gefvCache[i].pcache;
                    return def;
                }
            }

            def = FindField(field);

            _gefvCache[_gefvPos].pcache = def;
            _gefvCache[_gefvPos].field = field;
            _gefvPos ^= 1;

            return def;
        }

        public static float GetEdictFieldFloat(edict_t ed, string field, float defValue = 0)
        {
            ddef_t def = CachedSearch(ed, field);
            if (def == null)
                return defValue;
            
            return ed.GetFloat(def.ofs);
        }

        public static bool SetEdictFieldFloat(edict_t ed, string field, float value)
        {
            ddef_t def = CachedSearch(ed, field);
            if (def != null)
            {
                ed.SetFloat(def.ofs, value);
                return true;
            }
            return false;
        }

        static int MakeStingId(int index, bool isStatic)
        {
            return ((isStatic ? 0 : 1) << 24) + (index & 0xFFFFFF);
        }

        static bool IsStaticString(int stringId, out int offset)
        {
            offset = stringId & 0xFFFFFF;
            return ((stringId >> 24) & 1) == 0;
        }

        public static int AllocString()
        {
            int id = _DynamicStrings.Count;
            _DynamicStrings.Add(String.Empty);
            return MakeStingId(id, false);
        }

        public static void SetString(int id, string value)
        {
            int offset;
            if (IsStaticString(id, out offset))
            {
                throw new ArgumentException("Static strings are read-only!");
            }
            if (offset < 0 || offset >= _DynamicStrings.Count)
            {
                throw new ArgumentException("Invalid string id!");
            }
            _DynamicStrings[offset] = value;
        }

        /// <summary>
        /// ED_WriteGlobals
        /// </summary>
        public unsafe static void WriteGlobals(StreamWriter writer)
        {
            writer.WriteLine("{");
            for (int i = 0; i < _Progs.numglobaldefs; i++)
            {
                ddef_t def = _GlobalDefs[i];
                etype_t type = (etype_t)def.type;
                if ((def.type & DEF_SAVEGLOBAL) == 0)
                    continue;

                type &= (etype_t)~DEF_SAVEGLOBAL;

                if (type != etype_t.ev_string && type != etype_t.ev_float && type != etype_t.ev_entity)
                    continue;

                writer.Write("\"");
                writer.Write(GetString(def.s_name));
                writer.Write("\" \"");
                writer.Write(UglyValueString(type, (eval_t*)Get(def.ofs)));
                writer.WriteLine("\"");
            }
            writer.WriteLine("}");
        }

        /// <summary>
        /// PR_UglyValueString
        /// Returns a string describing *data in a type specific manner
        /// Easier to parse than PR_ValueString
        /// </summary>
        static unsafe string UglyValueString(etype_t type, eval_t* val)
        {
            type &= (etype_t)~DEF_SAVEGLOBAL;
            string result;

            switch (type)
            {
                case etype_t.ev_string:
                    result = GetString(val->_string);
                    break;

                case etype_t.ev_entity:
                    result = Server.NumForEdict(Server.ProgToEdict(val->edict)).ToString();
                    break;

                case etype_t.ev_function:
                    dfunction_t f = _Functions[val->function];
                    result = GetString(f.s_name);
                    break;

                case etype_t.ev_field:
                    ddef_t def = FindField(val->_int);
                    result = GetString(def.s_name);
                    break;

                case etype_t.ev_void:
                    result = "void";
                    break;

                case etype_t.ev_float:
                    result = val->_float.ToString("F6", CultureInfo.InvariantCulture.NumberFormat);
                    break;

                case etype_t.ev_vector:
                    result = String.Format(CultureInfo.InvariantCulture.NumberFormat,
                        "{0:F6} {1:F6} {2:F6}", val->vector[0], val->vector[1], val->vector[2]);
                    break;

                default:
                    result = "bad type " + type.ToString();
                    break;
            }

            return result;
        }

        /// <summary>
        /// ED_Write
        /// </summary>
        public unsafe static void WriteEdict(StreamWriter writer, edict_t ed)
        {
            writer.WriteLine("{");

            if (ed.free)
            {
                writer.WriteLine("}");
                return;
            }

            for (int i = 1; i < _Progs.numfielddefs; i++)
            {
                ddef_t d = _FieldDefs[i];
                string name = GetString(d.s_name);
                if (name != null && name.Length > 2 && name[name.Length - 2] == '_')// [strlen(name) - 2] == '_')
                    continue;	// skip _x, _y, _z vars

                int type = d.type & ~DEF_SAVEGLOBAL;
                int offset1;
                if (ed.IsV(d.ofs, out offset1))
                {
                    fixed (void* ptr = &ed.v)
                    {
                        int* v = (int*)ptr + offset1;
                        if (IsEmptyField(type, v))
                            continue;

                        writer.WriteLine("\"{0}\" \"{1}\"", name, UglyValueString((etype_t)d.type, (eval_t*)v));
                    }
                }
                else
                {
                    fixed (void* ptr = ed.fields)
                    {
                        int* v = (int*)ptr + offset1;
                        if (IsEmptyField(type, v))
                            continue;

                        writer.WriteLine("\"{0}\" \"{1}\"", name, UglyValueString((etype_t)d.type, (eval_t*)v));
                    }
                }
            }

            writer.WriteLine("}");
        }

        static unsafe bool IsEmptyField(int type, int* v)
        {
            for (int j = 0; j < _TypeSize[type]; j++)
                if (v[j] != 0)
                    return false;

            return true;
        }

        /// <summary>
        /// ED_ParseGlobals
        /// </summary>
        public static void ParseGlobals(string data)
        {
            while (true)
            {
                // parse key
                data = Common.Parse(data);
                if (Common.Token.StartsWith("}"))
                    break;
                
                if (String.IsNullOrEmpty(data))
                    Sys.Error("ED_ParseEntity: EOF without closing brace");

                string keyname = Common.Token;

                // parse value	
                data = Common.Parse(data);
                if (String.IsNullOrEmpty(data))
                    Sys.Error("ED_ParseEntity: EOF without closing brace");

                if (Common.Token.StartsWith("}"))
                    Sys.Error("ED_ParseEntity: closing brace without data");

                ddef_t key = FindGlobal(keyname);
                if (key == null)
                {
                    Con.Print("'{0}' is not a global\n", keyname);
                    continue;
                }

                if (!ParseGlobalPair(key, Common.Token))
                    Host.Error("ED_ParseGlobals: parse error");
            }
        }

        /// <summary>
        /// ED_FindGlobal
        /// </summary>
        static ddef_t FindGlobal(string name)
        {
            for (int i = 0; i < _GlobalDefs.Length; i++)
            {
                ddef_t def = _GlobalDefs[i];
                if (name == GetString(def.s_name))
                    return def;
            }
            return null;
        }

        static unsafe bool ParseGlobalPair(ddef_t key, string value)
        {
            int offset;
            if (IsGlobalStruct(key.ofs, out offset))
            {
                return ParsePair((float*)_GlobalStructAddr + offset, key, value);
            }
            return ParsePair((float*)_GlobalsAddr + offset, key, value);
        }

        /// <summary>
        /// ED_PrintNum
        /// </summary>
        public static void PrintNum(int ent)
        {
            Print(Server.EdictNum(ent));
        }

        /// <summary>
        /// PR_GlobalString
        /// Returns a string with a description and the contents of a global,
        /// padded to 20 field width
        /// </summary>
        static unsafe string GlobalString(int ofs)
        {
            string line = String.Empty;
            void* val = Get(ofs);// (void*)&pr_globals[ofs];
            ddef_t def = GlobalAtOfs(ofs);
            if (def == null)
                line = String.Format("{0}(???)", ofs);
            else
            {
                string s = ValueString((etype_t)def.type, val);
                line = String.Format("{0}({1}){2} ", ofs, GetString(def.s_name), s);
            }

            line = line.PadRight(20);

            return line;
        }

        /// <summary>
        /// PR_GlobalStringNoContents
        /// </summary>
        static string GlobalStringNoContents(int ofs)
        {
            string line = String.Empty;
            ddef_t def = GlobalAtOfs(ofs);
            if (def == null)
                line = String.Format("{0}(???)", ofs);
            else
                line = String.Format("{0}({1}) ", ofs, GetString(def.s_name));

            line = line.PadRight(20);

            return line;
        }

        /// <summary>
        /// ED_GlobalAtOfs
        /// </summary>
        static ddef_t GlobalAtOfs(int ofs)
        {
            for (int i = 0; i < _GlobalDefs.Length; i++)
            {
                ddef_t def = _GlobalDefs[i];
                if (def.ofs == ofs)
                    return def;
            }
            return null;
        }

    }
}
