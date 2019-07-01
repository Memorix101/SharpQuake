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
                case KeysDef.K_ESCAPE:
                    MenuBase.MainMenu.Show( Host );
                    break;

                case KeysDef.K_DOWNARROW:
                    snd.LocalSound( "misc/menu1.wav" );
                    if ( ++_Cursor >= MULTIPLAYER_ITEMS )
                        _Cursor = 0;
                    break;

                case KeysDef.K_UPARROW:
                    snd.LocalSound( "misc/menu1.wav" );
                    if ( --_Cursor < 0 )
                        _Cursor = MULTIPLAYER_ITEMS - 1;
                    break;

                case KeysDef.K_ENTER:
                    Host.Menu.EnterSound = true;
                    switch ( _Cursor )
                    {
                        case 0:
                            if ( Host.Network.TcpIpAvailable )
                                MenuBase.LanConfigMenu.Show( Host );
                            break;

                        case 1:
                            if ( Host.Network.TcpIpAvailable )
                                MenuBase.LanConfigMenu.Show( Host );
                            break;

                        case 2:
                            MenuBase.SetupMenu.Show( Host );
                            break;
                    }
                    break;
            }
        }

        public override void Draw( )
        {
            Host.Menu.DrawTransPic( 16, 4, Drawer.CachePic( "gfx/qplaque.lmp" ) );
            var p = Drawer.CachePic( "gfx/p_multi.lmp" );
            Host.Menu.DrawPic( ( 320 - p.width ) / 2, 4, p );
            Host.Menu.DrawTransPic( 72, 32, Drawer.CachePic( "gfx/mp_menu.lmp" ) );

            Single f = ( Int32 ) ( Host.Time * 10 ) % 6;

            Host.Menu.DrawTransPic( 54, 32 + _Cursor * 20, Drawer.CachePic( String.Format( "gfx/menudot{0}.lmp", f + 1 ) ) );

            if ( Host.Network.TcpIpAvailable )
                return;
            Host.Menu.PrintWhite( ( 320 / 2 ) - ( ( 27 * 8 ) / 2 ), 148, "No Communications Available" );
        }
    }
}
