using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SharpQuake.Framework
{
    [StructLayout( LayoutKind.Sequential, Pack = 1 )]
    public class Link
    {
        private Link _Prev, _Next;
        private Object _Owner;

        public Link Prev
        {
            get
            {
                return _Prev;
            }
        }

        public Link Next
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

        public Link( Object owner )
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

        public void InsertBefore( Link before )
        {
            _Next = before;
            _Prev = before._Prev;
            _Prev._Next = this;
            _Next._Prev = this;
        }

        public void InsertAfter( Link after )
        {
            _Next = after.Next;
            _Prev = after;
            _Prev._Next = this;
            _Next._Prev = this;
        }
    } // link_t;
}
