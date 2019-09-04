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

using string_t = System.Int32;

namespace SharpQuake.Framework
{
    /// <summary>
    /// In-memory edict
    /// </summary>
    public class MemoryEdict
    {
        public Boolean free;
        public Link area; // linked to a division node or leaf

        public string_t num_leafs;
        public Int16[] leafnums; // [MAX_ENT_LEAFS];

        public EntityState baseline;

        public Single freetime;			// sv.time when the object was freed
        public EntVars v;					// C exported fields from progs
        public Single[] fields; // other fields from progs

        public void Clear( )
        {
            v = default( EntVars );
            if ( fields != null )
                Array.Clear( fields, 0, fields.Length );
            free = false;
        }

        public Boolean IsV( string_t offset, out string_t correctedOffset )
        {
            if ( offset < ( EntVars.SizeInBytes >> 2 ) )
            {
                correctedOffset = offset;
                return true;
            }
            correctedOffset = offset - ( EntVars.SizeInBytes >> 2 );
            return false;
        }

        public unsafe void LoadInt( string_t offset, EVal* result )
        {
            Int32 offset1;
            if ( IsV( offset, out offset1 ) )
            {
                fixed ( void* pv = &v )
                {
                    var a = ( EVal* ) ( ( Int32* ) pv + offset1 );
                    result->_int = a->_int;
                }
            }
            else
            {
                fixed ( void* pv = fields )
                {
                    var a = ( EVal* ) ( ( Int32* ) pv + offset1 );
                    result->_int = a->_int;
                }
            }
        }

        public unsafe void StoreInt( string_t offset, EVal* value )
        {
            Int32 offset1;
            if ( IsV( offset, out offset1 ) )
            {
                fixed ( void* pv = &v )
                {
                    var a = ( EVal* ) ( ( Int32* ) pv + offset1 );
                    a->_int = value->_int;
                }
            }
            else
            {
                fixed ( void* pv = fields )
                {
                    var a = ( EVal* ) ( ( Int32* ) pv + offset1 );
                    a->_int = value->_int;
                }
            }
        }

        public unsafe void LoadVector( string_t offset, EVal* result )
        {
            Int32 offset1;
            if ( IsV( offset, out offset1 ) )
            {
                fixed ( void* pv = &v )
                {
                    var a = ( EVal* ) ( ( Int32* ) pv + offset1 );
                    result->vector[0] = a->vector[0];
                    result->vector[1] = a->vector[1];
                    result->vector[2] = a->vector[2];
                }
            }
            else
            {
                fixed ( void* pf = fields )
                {
                    var a = ( EVal* ) ( ( Int32* ) pf + offset1 );
                    result->vector[0] = a->vector[0];
                    result->vector[1] = a->vector[1];
                    result->vector[2] = a->vector[2];
                }
            }
        }

        public unsafe void StoreVector( string_t offset, EVal* value )
        {
            Int32 offset1;
            if ( IsV( offset, out offset1 ) )
            {
                fixed ( void* pv = &v )
                {
                    var a = ( EVal* ) ( ( Int32* ) pv + offset1 );
                    a->vector[0] = value->vector[0];
                    a->vector[1] = value->vector[1];
                    a->vector[2] = value->vector[2];
                }
            }
            else
            {
                fixed ( void* pf = fields )
                {
                    var a = ( EVal* ) ( ( Int32* ) pf + offset1 );
                    a->vector[0] = value->vector[0];
                    a->vector[1] = value->vector[1];
                    a->vector[2] = value->vector[2];
                }
            }
        }

        public unsafe string_t GetInt( string_t offset )
        {
            Int32 offset1, result;
            if ( IsV( offset, out offset1 ) )
            {
                fixed ( void* pv = &v )
                {
                    var a = ( EVal* ) ( ( Int32* ) pv + offset1 );
                    result = a->_int;
                }
            }
            else
            {
                fixed ( void* pv = fields )
                {
                    var a = ( EVal* ) ( ( Int32* ) pv + offset1 );
                    result = a->_int;
                }
            }
            return result;
        }

        public unsafe Single GetFloat( string_t offset )
        {
            Int32 offset1;
            Single result;
            if ( IsV( offset, out offset1 ) )
            {
                fixed ( void* pv = &v )
                {
                    var a = ( EVal* ) ( ( Single* ) pv + offset1 );
                    result = a->_float;
                }
            }
            else
            {
                fixed ( void* pv = fields )
                {
                    var a = ( EVal* ) ( ( Single* ) pv + offset1 );
                    result = a->_float;
                }
            }
            return result;
        }

        public unsafe void SetFloat( string_t offset, Single value )
        {
            Int32 offset1;
            if ( IsV( offset, out offset1 ) )
            {
                fixed ( void* pv = &v )
                {
                    var a = ( EVal* ) ( ( Single* ) pv + offset1 );
                    a->_float = value;
                }
            }
            else
            {
                fixed ( void* pv = fields )
                {
                    var a = ( EVal* ) ( ( Single* ) pv + offset1 );
                    a->_float = value;
                }
            }
        }

        public MemoryEdict( )
        {
            area = new Link( this );
            leafnums = new Int16[ProgramDef.MAX_ENT_LEAFS];
            fields = new Single[( ProgramDef.EdictSize - EntVars.SizeInBytes ) >> 2];
        }
    } // edict_t;
}
