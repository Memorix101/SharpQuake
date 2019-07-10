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
    /// <summary>
    /// Cache_functions
    /// </summary>
    public class Cache
    {
        public CacheEntry Head
        {
            get;
            private set;
        }

        public Int32 Capacity
        {
            get;
            private set;
        }

        public Int32 BytesAllocated
        {
            get;
            set;
        }


        // Cache_Init
        public void Initialise( Int32 capacity )
        {
            Capacity = capacity;
            BytesAllocated = 0;
            Head = new CacheEntry( this, true );

            CommandWrapper.Add( "flush", Flush );
        }

        // Cache_Check
        /// <summary>
        /// Cache_Check
        /// Returns value of c.data if still cached or null
        /// </summary>
        public Object Check( CacheUser c )
        {
            var cs = ( CacheEntry ) c;

            if ( cs == null || cs.data == null )
                return null;

            // move to head of LRU
            cs.RemoveFromLRU( );
            cs.LRUInstertAfter( Head );

            return cs.data;
        }

        // Cache_Alloc
        public CacheUser Alloc( Int32 size, String name )
        {
            if ( size <= 0 )
                Utilities.Error( "Cache_Alloc: size {0}", size );

            size = ( size + 15 ) & ~15;

            CacheEntry entry = null;

            // find memory for it
            while ( true )
            {
                entry = TryAlloc( size );
                if ( entry != null )
                    break;

                // free the least recently used cahedat
                if ( Head.LruPrev == Head )// cache_head.lru_prev == &cache_head)
                    Utilities.Error( "Cache_Alloc: out of memory" );
                // not enough memory at all
                Free( Head.LruPrev );
            }

            Check( entry );
            return entry;
        }

        /// <summary>
        /// Cache_Report
        /// </summary>
        public void Report( )
        {
            ConsoleWrapper.DPrint( "{0,4:F1} megabyte data cache, used {1,4:F1} megabyte\n",
                Capacity / ( System.Single ) ( 1024 * 1024 ), BytesAllocated / ( System.Single ) ( 1024 * 1024 ) );
        }

        //Cache_Flush
        //
        //Throw everything out, so new data will be demand cached
        private void Flush( )
        {
            while ( Head.Next != Head )
                Free( Head.Next ); // reclaim the space
        }

        // Cache_Free
        //
        // Frees the memory and removes it from the LRU list
        private void Free( CacheUser c )
        {
            if ( c.data == null )
                Utilities.Error( "Cache_Free: not allocated" );

            var entry = ( CacheEntry ) c;
            entry.Remove( );
        }

        // Cache_TryAlloc
        private CacheEntry TryAlloc( System.Int32 size )
        {
            if ( BytesAllocated + size > Capacity )
                return null;

            var result = new CacheEntry( this, size );
            Head.InsertBefore( result );
            result.LRUInstertAfter( Head );
            return result;
        }
    }
}
