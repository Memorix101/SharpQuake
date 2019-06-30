using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpQuake.Framework
{
    /// <summary>
    /// Cache_functions
    /// </summary>
    public static class Cache
    {
        public static CacheEntry _Head;

        public static System.Int32 _Capacity;

        public static System.Int32 _BytesAllocated;

        // Cache_Init
        public static void Init( System.Int32 capacity )
        {
            _Capacity = capacity;
            _BytesAllocated = 0;
            _Head = new CacheEntry( true );

            CommandWrapper.Add( "flush", Flush );
        }

        // Cache_Check
        /// <summary>
        /// Cache_Check
        /// Returns value of c.data if still cached or null
        /// </summary>
        public static System.Object Check( CacheUser c )
        {
            CacheEntry cs = ( CacheEntry ) c;

            if ( cs == null || cs.data == null )
                return null;

            // move to head of LRU
            cs.RemoveFromLRU( );
            cs.LRUInstertAfter( _Head );

            return cs.data;
        }

        // Cache_Alloc
        public static CacheUser Alloc( System.Int32 size, System.String name )
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
                if ( _Head.LruPrev == _Head )// cache_head.lru_prev == &cache_head)
                    Utilities.Error( "Cache_Alloc: out of memory" );
                // not enough memory at all
                Free( _Head.LruPrev );
            }

            Check( entry );
            return entry;
        }

        /// <summary>
        /// Cache_Report
        /// </summary>
        public static void Report( )
        {
            ConsoleWrapper.DPrint( "{0,4:F1} megabyte data cache, used {1,4:F1} megabyte\n",
                _Capacity / ( System.Single ) ( 1024 * 1024 ), _BytesAllocated / ( System.Single ) ( 1024 * 1024 ) );
        }

        //Cache_Flush
        //
        //Throw everything out, so new data will be demand cached
        private static void Flush( )
        {
            while ( _Head.Next != _Head )
                Free( _Head.Next ); // reclaim the space
        }

        // Cache_Free
        //
        // Frees the memory and removes it from the LRU list
        private static void Free( CacheUser c )
        {
            if ( c.data == null )
                Utilities.Error( "Cache_Free: not allocated" );

            CacheEntry entry = ( CacheEntry ) c;
            entry.Remove( );
        }

        // Cache_TryAlloc
        private static CacheEntry TryAlloc( System.Int32 size )
        {
            if ( _BytesAllocated + size > _Capacity )
                return null;

            CacheEntry result = new CacheEntry( size );
            _Head.InsertBefore( result );
            result.LRUInstertAfter( _Head );
            return result;
        }
    }
}
