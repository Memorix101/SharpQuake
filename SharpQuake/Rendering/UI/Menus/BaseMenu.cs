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
using SharpQuake.Factories.Rendering.UI;
using SharpQuake.Framework;
using SharpQuake.Framework.IO.Input;

namespace SharpQuake.Rendering.UI
{
    public abstract class BaseMenu
    {
        public String Name
        {
            get;
            private set;
        }

        public Int32 Cursor
        {
            get;
            protected set;
        }

        // CHANGE 
        protected Host Host
        {
            get;
            set;
        }

        protected MenuFactory MenuFactory
		{
            get;
            set;
		}

        public BaseMenu( String name, MenuFactory menuFactory )
		{
            Name = name;
            MenuFactory = menuFactory;
        }

        public void Hide( )
        {
            Host.Keyboard.Destination = KeyDestination.key_game;
            MenuFactory.SetActive( null );
        }

        public virtual void Show( Host host )
        {
            Host = host;

            Host.Menus.EnterSound = true;
            Host.Keyboard.Destination = KeyDestination.key_menu;
            MenuFactory.SetActive( this );
        }

        public abstract void KeyEvent( Int32 key );

        public abstract void Draw( );
    }
}
