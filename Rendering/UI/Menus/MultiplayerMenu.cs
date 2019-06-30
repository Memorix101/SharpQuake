using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpQuake.Framework;

namespace SharpQuake
{
    public class MultiplayerMenu : MenuBase
    {
        private const Int32 MULTIPLAYER_ITEMS = 3;

        public override void KeyEvent( Int32 key )
        {
            switch ( key )
            {
                case Key.K_ESCAPE:
                    MenuBase.MainMenu.Show( );
                    break;

                case Key.K_DOWNARROW:
                    snd.LocalSound( "misc/menu1.wav" );
                    if ( ++_Cursor >= MULTIPLAYER_ITEMS )
                        _Cursor = 0;
                    break;

                case Key.K_UPARROW:
                    snd.LocalSound( "misc/menu1.wav" );
                    if ( --_Cursor < 0 )
                        _Cursor = MULTIPLAYER_ITEMS - 1;
                    break;

                case Key.K_ENTER:
                    Menu.EnterSound = true;
                    switch ( _Cursor )
                    {
                        case 0:
                            if ( net.TcpIpAvailable )
                                MenuBase.LanConfigMenu.Show( );
                            break;

                        case 1:
                            if ( net.TcpIpAvailable )
                                MenuBase.LanConfigMenu.Show( );
                            break;

                        case 2:
                            MenuBase.SetupMenu.Show( );
                            break;
                    }
                    break;
            }
        }

        public override void Draw( )
        {
            Menu.DrawTransPic( 16, 4, Drawer.CachePic( "gfx/qplaque.lmp" ) );
            GLPic p = Drawer.CachePic( "gfx/p_multi.lmp" );
            Menu.DrawPic( ( 320 - p.width ) / 2, 4, p );
            Menu.DrawTransPic( 72, 32, Drawer.CachePic( "gfx/mp_menu.lmp" ) );

            Single f = ( Int32 ) ( host.Time * 10 ) % 6;

            Menu.DrawTransPic( 54, 32 + _Cursor * 20, Drawer.CachePic( String.Format( "gfx/menudot{0}.lmp", f + 1 ) ) );

            if ( net.TcpIpAvailable )
                return;
            Menu.PrintWhite( ( 320 / 2 ) - ( ( 27 * 8 ) / 2 ), 148, "No Communications Available" );
        }
    }
}
