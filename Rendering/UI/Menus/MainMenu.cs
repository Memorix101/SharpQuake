using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpQuake.Framework;

namespace SharpQuake
{
    /// <summary>
    /// MainMenu
    /// </summary>
    public class MainMenu : MenuBase
    {
        private const Int32 MAIN_ITEMS = 5;
        private Int32 _SaveDemoNum;

        public override void Show( Host host )
        {
            if ( host.Keyboard.Destination != KeyDestination.key_menu )
            {
                _SaveDemoNum = client.cls.demonum;
                client.cls.demonum = -1;
            }

            base.Show( host );
        }

        /// <summary>
        /// M_Main_Key
        /// </summary>
        public override void KeyEvent( Int32 key )
        {
            switch ( key )
            {
                case KeysDef.K_ESCAPE:
                    //Host.Keyboard.Destination = keydest_t.key_game;
                    MenuBase.CurrentMenu.Hide( );
                    client.cls.demonum = _SaveDemoNum;
                    if ( client.cls.demonum != -1 && !client.cls.demoplayback && client.cls.state != cactive_t.ca_connected )
                        client.NextDemo( );
                    break;

                case KeysDef.K_DOWNARROW:
                    snd.LocalSound( "misc/menu1.wav" );
                    if ( ++_Cursor >= MAIN_ITEMS )
                        _Cursor = 0;
                    break;

                case KeysDef.K_UPARROW:
                    snd.LocalSound( "misc/menu1.wav" );
                    if ( --_Cursor < 0 )
                        _Cursor = MAIN_ITEMS - 1;
                    break;

                case KeysDef.K_ENTER:
                    Host.Menu.EnterSound = true;

                    switch ( _Cursor )
                    {
                        case 0:
                            MenuBase.SinglePlayerMenu.Show( Host );
                            break;

                        case 1:
                            MenuBase.MultiPlayerMenu.Show( Host );
                            break;

                        case 2:
                            MenuBase.OptionsMenu.Show( Host );
                            break;

                        case 3:
                            MenuBase.HelpMenu.Show( Host );
                            break;

                        case 4:
                            MenuBase.QuitMenu.Show( Host );
                            break;
                    }
                    break;
            }
        }

        public override void Draw( )
        {
            Host.Menu.DrawTransPic( 16, 4, Drawer.CachePic( "gfx/qplaque.lmp" ) );
            var p = Drawer.CachePic( "gfx/ttl_main.lmp" );
            Host.Menu.DrawPic( ( 320 - p.width ) / 2, 4, p );
            Host.Menu.DrawTransPic( 72, 32, Drawer.CachePic( "gfx/mainmenu.lmp" ) );

            var f = ( Int32 ) ( Host.Time * 10 ) % 6;

            Host.Menu.DrawTransPic( 54, 32 + _Cursor * 20, Drawer.CachePic( String.Format( "gfx/menudot{0}.lmp", f + 1 ) ) );
        }
    }
}
