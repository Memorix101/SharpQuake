﻿/// <copyright>
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
    public class QuitMenu : BaseMenu
    {
        private BaseMenu _PrevMenu; // m_quit_prevstate;

        public QuitMenu( MenuFactory menuFactory ) : base( "menu_quit", menuFactory )
        {
        }

        public override void Show( Host host )
        {
            if ( MenuFactory.CurrentMenu == this )
                return;

            host.Keyboard.Destination = KeyDestination.key_menu;
            _PrevMenu = MenuFactory.CurrentMenu;

            base.Show( host );
        }

        public override void KeyEvent( Int32 key )
        {
            switch ( key )
            {
                case KeysDef.K_ESCAPE:
                case 'n':
                case 'N':
                    if ( _PrevMenu != null )
                        _PrevMenu.Show( Host );
                    else
                        MenuFactory.CurrentMenu.Hide( );
                    break;

                case 'Y':
                case 'y':
                    Host.Keyboard.Destination = KeyDestination.key_console;
                    Host.Quit_f( null );
                    break;

                default:
                    break;
            }
        }

        public override void Draw( )
        {
            Host.Menus.DrawTextBox( 0, 0, 38, 23 );
            Host.Menus.PrintWhite( 16, 12, "  Quake version 1.09 by id Software\n\n" );
            Host.Menus.PrintWhite( 16, 28, "Programming        Art \n" );
            Host.Menus.Print( 16, 36, " John Carmack       Adrian Carmack\n" );
            Host.Menus.Print( 16, 44, " Michael Abrash     Kevin Cloud\n" );
            Host.Menus.Print( 16, 52, " John Cash          Paul Steed\n" );
            Host.Menus.Print( 16, 60, " Dave 'Zoid' Kirsch\n" );
            Host.Menus.PrintWhite( 16, 68, "Design             Biz\n" );
            Host.Menus.Print( 16, 76, " John Romero        Jay Wilbur\n" );
            Host.Menus.Print( 16, 84, " Sandy Petersen     Mike Wilson\n" );
            Host.Menus.Print( 16, 92, " American McGee     Donna Jackson\n" );
            Host.Menus.Print( 16, 100, " Tim Willits        Todd Hollenshead\n" );
            Host.Menus.PrintWhite( 16, 108, "Support            Projects\n" );
            Host.Menus.Print( 16, 116, " Barrett Alexander  Shawn Green\n" );
            Host.Menus.PrintWhite( 16, 124, "Sound Effects\n" );
            Host.Menus.Print( 16, 132, " Trent Reznor and Nine Inch Nails\n\n" );
            Host.Menus.PrintWhite( 16, 140, "Quake is a trademark of Id Software,\n" );
            Host.Menus.PrintWhite( 16, 148, "inc., (c)1996 Id Software, inc. All\n" );
            Host.Menus.PrintWhite( 16, 156, "rights reserved. NIN logo is a\n" );
            Host.Menus.PrintWhite( 16, 164, "registered trademark licensed to\n" );
            Host.Menus.PrintWhite( 16, 172, "Nothing Interactive, Inc. All rights\n" );
            Host.Menus.PrintWhite( 16, 180, "reserved. Press y to exit\n" );
        }
    }
}
