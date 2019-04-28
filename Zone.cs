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

// zone.h
// zone.c

// Used only to emulate Chache_xxx functions

namespace SharpQuake
{
    /// <summary>
    /// Cache_functions
    /// </summary>
    internal static class Cache
    {
        private static CacheEntry _Head;

        private static int _Capacity;

        private static int _BytesAllocated;

        // Cache_Init
        public static void Init( int capacity )
        {
            _Capacity = capacity;
            _BytesAllocated = 0;
            _Head = new CacheEntry( true );

            cmd.Add( "flush", Flush );
        }

        // Cache_Check
        /// <summary>
        /// Cache_Check
        /// Returns value of c.data if still cached or null
        /// </summary>
        public static object Check( cache_user_t c )
        {
            CacheEntry cs = (CacheEntry)c;

            if( cs == null || cs.data == null )
                return null;

            // move to head of LRU
            cs.RemoveFromLRU();
            cs.LRUInstertAfter( _Head );

            return cs.data;
        }

        // Cache_Alloc
        public static cache_user_t Alloc( int size, string name )
        {
            if( size <= 0 )
                sys.Error( "Cache_Alloc: size {0}", size );

            size = ( size + 15 ) & ~15;

            CacheEntry entry = null;

            // find memory for it
            while( true )
            {
                entry = TryAlloc( size );
                if( entry != null )
                    break;

                // free the least recently used cahedat
                if( _Head.LruPrev == _Head )// cache_head.lru_prev == &cache_head)
                    sys.Error( "Cache_Alloc: out of memory" );
                // not enough memory at all
                Free( _Head.LruPrev );
            }

            Check( entry );
            return entry;
        }

        /// <summary>
        /// Cache_Report
        /// </summary>
        public static void Report()
        {
            Con.DPrint( "{0,4:F1} megabyte data cache, used {1,4:F1} megabyte\n",
                _Capacity / (float)( 1024 * 1024 ), _BytesAllocated / (float)( 1024 * 1024 ) );
        }

        //Cache_Flush
        //
        //Throw everything out, so new data will be demand cached
        private static void Flush()
        {
            while( _Head.Next != _Head )
                Free( _Head.Next ); // reclaim the space
        }

        // Cache_Free
        //
        // Frees the memory and removes it from the LRU list
        private static void Free( cache_user_t c )
        {
            if( c.data == null )
                sys.Error( "Cache_Free: not allocated" );

            CacheEntry entry = (CacheEntry)c;
            entry.Remove();
        }

        // Cache_TryAlloc
        private static CacheEntry TryAlloc( int size )
        {
            if( _BytesAllocated + size > _Capacity )
                return null;

            CacheEntry result = new CacheEntry( size );
            _Head.InsertBefore( result );
            result.LRUInstertAfter( _Head );
            return result;
        }

        private class CacheEntry : cache_user_t
        {
            public CacheEntry Next
            {
                get
                {
                    return _Next;
                }
            }

            public CacheEntry Prev
            {
                get
                {
                    return _Prev;
                }
            }

            public CacheEntry LruPrev
            {
                get
                {
                    return _LruPrev;
                }
            }

            public CacheEntry LruNext
            {
                get
                {
                    return _LruNext;
                }
            }

            private CacheEntry _Prev;
            private CacheEntry _Next;
            private CacheEntry _LruPrev;
            private CacheEntry _LruNext;
            private int _Size;

            // Cache_UnlinkLRU
            public void RemoveFromLRU()
            {
                if( _LruNext == null || _LruPrev == null )
                    sys.Error( "Cache_UnlinkLRU: NULL link" );

                _LruNext._LruPrev = _LruPrev;
                _LruPrev._LruNext = _LruNext;
                _LruPrev = _LruNext = null;
            }

            // inserts <this> instance after <prev> in LRU list
            public void LRUInstertAfter( CacheEntry prev )
            {
                if( _LruNext != null || _LruPrev != null )
                    sys.Error( "Cache_MakeLRU: active link" );

                prev._LruNext._LruPrev = this;
                _LruNext = prev._LruNext;
                _LruPrev = prev;
                prev._LruNext = this;
            }

            // inserts <this> instance before <next>
            public void InsertBefore( CacheEntry next )
            {
                _Next = next;
                if( next._Prev != null )
                    _Prev = next._Prev;
                else
                    _Prev = next;

                if( next._Prev != null )
                    next._Prev._Next = this;
                else
                    next._Prev = this;
                next._Prev = this;

                if( next._Next == null )
                    next._Next = this;
            }

            public void Remove()
            {
                _Prev._Next = _Next;
                _Next._Prev = _Prev;
                _Next = _Prev = null;

                data = null;
                _BytesAllocated -= _Size;
                _Size = 0;

                RemoveFromLRU();
            }

            public CacheEntry( bool isHead = false )
            {
                if( isHead )
                {
                    _Next = this;
                    _Prev = this;
                    _LruNext = this;
                    _LruPrev = this;
                }
            }

            public CacheEntry( int size )
            {
                _Size = size;
                Cache._BytesAllocated += _Size;
            }

            ~CacheEntry()
            {
                Cache._BytesAllocated -= _Size;
            }
        }
    }

    internal class cache_user_t
    {
        public object data;
    }
}
