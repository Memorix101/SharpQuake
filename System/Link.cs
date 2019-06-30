using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SharpQuake
{
    [StructLayout( LayoutKind.Sequential, Pack = 1 )]
    internal class link_t
    {
        private link_t _Prev, _Next;
        private Object _Owner;

        public link_t Prev
        {
            get
            {
                return _Prev;
            }
        }

        public link_t Next
        {
            get
            {
                return _Next;
            }
        }

        public Object Owner
        {
            get
            {
                return _Owner;
            }
        }

        public link_t( Object owner )
        {
            _Owner = owner;
        }

        public void Clear( )
        {
            _Prev = _Next = this;
        }

        public void ClearToNulls( )
        {
            _Prev = _Next = null;
        }

        public void Remove( )
        {
            _Next._Prev = _Prev;
            _Prev._Next = _Next;
            _Next = null;
            _Prev = null;
        }

        public void InsertBefore( link_t before )
        {
            _Next = before;
            _Prev = before._Prev;
            _Prev._Next = this;
            _Next._Prev = this;
        }

        public void InsertAfter( link_t after )
        {
            _Next = after.Next;
            _Prev = after;
            _Prev._Next = this;
            _Next._Prev = this;
        }
    } // link_t;
}
