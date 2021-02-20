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

namespace SharpQuake.Framework
{
    public class CacheEntry : CacheUser
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

        private Cache Cache
        {
            get;
            set;
        }

        private CacheEntry _Prev;
        private CacheEntry _Next;
        private CacheEntry _LruPrev;
        private CacheEntry _LruNext;
        private System.Int32 _Size;

        // Cache_UnlinkLRU
        public void RemoveFromLRU( )
        {
            if ( _LruNext == null || _LruPrev == null )
                Utilities.Error( "Cache_UnlinkLRU: NULL link" );

            _LruNext._LruPrev = _LruPrev;
            _LruPrev._LruNext = _LruNext;
            _LruPrev = _LruNext = null;
        }

        // inserts <this> instance after <prev> in LRU list
        public void LRUInstertAfter( CacheEntry prev )
        {
            if ( _LruNext != null || _LruPrev != null )
                Utilities.Error( "Cache_MakeLRU: active link" );

            prev._LruNext._LruPrev = this;
            _LruNext = prev._LruNext;
            _LruPrev = prev;
            prev._LruNext = this;
        }

        // inserts <this> instance before <next>
        public void InsertBefore( CacheEntry next )
        {
            _Next = next;
            if ( next._Prev != null )
                _Prev = next._Prev;
            else
                _Prev = next;

            if ( next._Prev != null )
                next._Prev._Next = this;
            else
                next._Prev = this;
            next._Prev = this;

            if ( next._Next == null )
                next._Next = this;
        }

        public void Remove( )
        {
            _Prev._Next = _Next;
            _Next._Prev = _Prev;
            _Next = _Prev = null;

            data = null;
            Cache.BytesAllocated -= _Size;
            _Size = 0;

            RemoveFromLRU( );
        }

        public CacheEntry( Cache cache, System.Boolean isHead = false )
        {
            if ( isHead )
            {
                _Next = this;
                _Prev = this;
                _LruNext = this;
                _LruPrev = this;
            }
        }

        public CacheEntry( Cache cache, Int32 size )
        {
            Cache = cache;

            _Size = size;
            Cache.BytesAllocated += _Size;
        }

        ~CacheEntry( )
        {
            if ( Cache != null )
                Cache.BytesAllocated -= _Size;
        }
    }
}
