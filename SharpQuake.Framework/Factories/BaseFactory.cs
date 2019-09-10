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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpQuake.Framework.Factories
{
    public class BaseFactory<TKey, TItem> : IBaseFactory, IDisposable where TItem : class
    {
        protected Type KeyType
        {
            get;
            set;
        }

        protected Type ItemType
        {
            get;
            set;
        }

        protected Boolean UniqueKeys
        {
            get;
            private set;
        }

        private Object Items
        {
            get;
            set;
        }

        protected Dictionary<TKey, TItem> DictionaryItems
        {
            get
            {
                return ( Dictionary<TKey, TItem> ) Items;
            }
            private set
            {
                Items = value;
            }
        }

        protected List<KeyValuePair<TKey, TItem>> ListItems
        {
            get
            {
                return ( List<KeyValuePair<TKey, TItem>> ) Items;
            }
            private set
            {
                Items = value;
            }
        }

        public BaseFactory( Boolean uniqueKeys = true )
        {
            KeyType = typeof( TKey );
            ItemType = typeof( TItem );
            UniqueKeys = uniqueKeys;

            if ( UniqueKeys )
                Items = new Dictionary<TKey, TItem>( );
            else
                Items = new List<KeyValuePair<TKey, TItem>>( );
        }

        public Boolean Contains( TKey key )
        {
            if ( UniqueKeys )
                return DictionaryItems.ContainsKey( key );
            else
                return ListItems.Count( i => i.Key.Equals( key ) ) > 0;
        }

        public TItem Get( TKey key )
        {
            var exists = Contains( key );

            if ( !exists )
                return null;

            if ( UniqueKeys )
                return DictionaryItems[key];
            else
                return ListItems.Where( i => i.Key.Equals( key ) ).FirstOrDefault().Value;
        }

        public Int32 IndexOf( TKey key )
        {
            var exists = Contains( key );

            if ( !exists )
                return -1;

            if ( UniqueKeys )
                return DictionaryItems.Keys.ToList( ).IndexOf( key );
            else
                return ListItems.IndexOf( ListItems.Where( i => i.Key.Equals( key ) ).First( ) );
        }

        public TItem GetByIndex( Int32 index )
        {
            if ( index >= ( UniqueKeys ? DictionaryItems.Count : ListItems.Count ) )
                return null;

            if ( UniqueKeys )
                return DictionaryItems.Values.Select( v => v ).ToArray()[index];
            else
                return ListItems.Select( v => v.Value ).ToArray( )[index];
        }

        public void Add( TKey key, TItem item )
        {
            var exists = Contains( key );

            if ( exists )
                return;

            if ( UniqueKeys )
                DictionaryItems.Add( key, item );
            else
                ListItems.Add( new KeyValuePair<TKey, TItem>( key, item ) );
        }

        public void Remove( TKey key )
        {
            var exists = Contains( key );

            if ( !exists )
                return;

            if ( UniqueKeys )
                DictionaryItems.Remove( key );
            else
                ListItems.RemoveAll( i => i.Key.Equals( key ) );
        }

        public void Clear( )
        {
            if ( UniqueKeys )
                DictionaryItems.Clear( );
            else
                ListItems.Clear( );
        }

        public virtual void Dispose( )
        {
            //throw new NotImplementedException( );
        }
    }
}
