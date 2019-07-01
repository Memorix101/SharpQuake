using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpQuake.Framework;

namespace SharpQuake
{
    public class QuitMenu : MenuBase
    {
        private MenuBase _PrevMenu; // m_quit_prevstate;

        public override void Show( Host host )
        {
            if ( CurrentMenu == this )
                return;

            host.Keyboard.Destination = KeyDestination.key_menu;
            _PrevMenu = CurrentMenu;

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
                        MenuBase.CurrentMenu.Hide( );
                    break;

                case 'Y':
                case 'y':
                    Host.Keyboard.Destination = KeyDestination.key_console;
                    Host.Quit_f( );
                    break;

                default:
                    break;
            }
        }

        public override void Draw( )
        {
            Menu.DrawTextBox( 0, 0, 38, 23 );
            Menu.PrintWhite( 16, 12, "  Quake version 1.09 by id Software\n\n" );
            Menu.PrintWhite( 16, 28, "Programming        Art \n" );
            Menu.Print( 16, 36, " John Carmack       Adrian Carmack\n" );
            Menu.Print( 16, 44, " Michael Abrash     Kevin Cloud\n" );
            Menu.Print( 16, 52, " John Cash          Paul Steed\n" );
            Menu.Print( 16, 60, " Dave 'Zoid' Kirsch\n" );
            Menu.PrintWhite( 16, 68, "Design             Biz\n" );
            Menu.Print( 16, 76, " John Romero        Jay Wilbur\n" );
            Menu.Print( 16, 84, " Sandy Petersen     Mike Wilson\n" );
            Menu.Print( 16, 92, " American McGee     Donna Jackson\n" );
            Menu.Print( 16, 100, " Tim Willits        Todd Hollenshead\n" );
            Menu.PrintWhite( 16, 108, "Support            Projects\n" );
            Menu.Print( 16, 116, " Barrett Alexander  Shawn Green\n" );
            Menu.PrintWhite( 16, 124, "Sound Effects\n" );
            Menu.Print( 16, 132, " Trent Reznor and Nine Inch Nails\n\n" );
            Menu.PrintWhite( 16, 140, "Quake is a trademark of Id Software,\n" );
            Menu.PrintWhite( 16, 148, "inc., (c)1996 Id Software, inc. All\n" );
            Menu.PrintWhite( 16, 156, "rights reserved. NIN logo is a\n" );
            Menu.PrintWhite( 16, 164, "registered trademark licensed to\n" );
            Menu.PrintWhite( 16, 172, "Nothing Interactive, Inc. All rights\n" );
            Menu.PrintWhite( 16, 180, "reserved. Press y to exit\n" );
        }
    }
}
