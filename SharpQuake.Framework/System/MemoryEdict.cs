using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            this.v = default( EntVars );
            if ( this.fields != null )
                Array.Clear( this.fields, 0, this.fields.Length );
            this.free = false;
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
                fixed ( void* pv = &this.v )
                {
                    EVal* a = ( EVal* ) ( ( Int32* ) pv + offset1 );
                    result->_int = a->_int;
                }
            }
            else
            {
                fixed ( void* pv = this.fields )
                {
                    EVal* a = ( EVal* ) ( ( Int32* ) pv + offset1 );
                    result->_int = a->_int;
                }
            }
        }

        public unsafe void StoreInt( string_t offset, EVal* value )
        {
            Int32 offset1;
            if ( IsV( offset, out offset1 ) )
            {
                fixed ( void* pv = &this.v )
                {
                    EVal* a = ( EVal* ) ( ( Int32* ) pv + offset1 );
                    a->_int = value->_int;
                }
            }
            else
            {
                fixed ( void* pv = this.fields )
                {
                    EVal* a = ( EVal* ) ( ( Int32* ) pv + offset1 );
                    a->_int = value->_int;
                }
            }
        }

        public unsafe void LoadVector( string_t offset, EVal* result )
        {
            Int32 offset1;
            if ( IsV( offset, out offset1 ) )
            {
                fixed ( void* pv = &this.v )
                {
                    EVal* a = ( EVal* ) ( ( Int32* ) pv + offset1 );
                    result->vector[0] = a->vector[0];
                    result->vector[1] = a->vector[1];
                    result->vector[2] = a->vector[2];
                }
            }
            else
            {
                fixed ( void* pf = this.fields )
                {
                    EVal* a = ( EVal* ) ( ( Int32* ) pf + offset1 );
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
                fixed ( void* pv = &this.v )
                {
                    EVal* a = ( EVal* ) ( ( Int32* ) pv + offset1 );
                    a->vector[0] = value->vector[0];
                    a->vector[1] = value->vector[1];
                    a->vector[2] = value->vector[2];
                }
            }
            else
            {
                fixed ( void* pf = this.fields )
                {
                    EVal* a = ( EVal* ) ( ( Int32* ) pf + offset1 );
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
                fixed ( void* pv = &this.v )
                {
                    EVal* a = ( EVal* ) ( ( Int32* ) pv + offset1 );
                    result = a->_int;
                }
            }
            else
            {
                fixed ( void* pv = this.fields )
                {
                    EVal* a = ( EVal* ) ( ( Int32* ) pv + offset1 );
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
                fixed ( void* pv = &this.v )
                {
                    EVal* a = ( EVal* ) ( ( Single* ) pv + offset1 );
                    result = a->_float;
                }
            }
            else
            {
                fixed ( void* pv = this.fields )
                {
                    EVal* a = ( EVal* ) ( ( Single* ) pv + offset1 );
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
                fixed ( void* pv = &this.v )
                {
                    EVal* a = ( EVal* ) ( ( Single* ) pv + offset1 );
                    a->_float = value;
                }
            }
            else
            {
                fixed ( void* pv = this.fields )
                {
                    EVal* a = ( EVal* ) ( ( Single* ) pv + offset1 );
                    a->_float = value;
                }
            }
        }

        public MemoryEdict( )
        {
            this.area = new Link( this );
            this.leafnums = new Int16[ProgramDef.MAX_ENT_LEAFS];
            this.fields = new Single[( ProgramDef.EdictSize - EntVars.SizeInBytes ) >> 2];
        }
    } // edict_t;
}
