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
using SharpQuake.Framework;

namespace SharpQuake
{
    public abstract class MenuBase
    {
        public static MenuBase CurrentMenu
        {
            get
            {
                return _CurrentMenu;
            }
        }

        public Int32 Cursor
        {
            get
            {
                return _Cursor;
            }
        }

        // Top level menu items
        public static readonly MenuBase MainMenu = new MainMenu( );

        public static readonly MenuBase SinglePlayerMenu = new SinglePlayerMenu( );
        public static readonly MenuBase MultiPlayerMenu = new MultiplayerMenu( );
        public static readonly MenuBase OptionsMenu = new OptionsMenu( );
        public static readonly MenuBase HelpMenu = new HelpMenu( );
        public static readonly MenuBase QuitMenu = new QuitMenu( );
        public static readonly MenuBase LoadMenu = new LoadMenu( );
        public static readonly MenuBase SaveMenu = new SaveMenu( );

        // Submenus
        public static readonly MenuBase KeysMenu = new KeysMenu( );

        public static readonly MenuBase LanConfigMenu = new LanConfigMenu( );
        public static readonly MenuBase SetupMenu = new SetupMenu( );
        public static readonly MenuBase GameOptionsMenu = new GameOptionsMenu( );
        public static readonly MenuBase SearchMenu = new SearchMenu( );
        public static readonly MenuBase ServerListMenu = new ServerListMenu( );
        public static readonly MenuBase VideoMenu = new VideoMenu( );
        protected Int32 _Cursor;
        private static MenuBase _CurrentMenu;

        // CHANGE 
        protected Host Host
        {
            get;
            set;
        }

        public void Hide( )
        {
            Host.Keyboard.Destination = KeyDestination.key_game;
            _CurrentMenu = null;
        }

        public virtual void Show( Host host )
        {
            Host = host;

            Host.Menu.EnterSound = true;
            Host.Keyboard.Destination = KeyDestination.key_menu;
            _CurrentMenu = this;
        }

        public abstract void KeyEvent( Int32 key );

        public abstract void Draw( );
    }
}
