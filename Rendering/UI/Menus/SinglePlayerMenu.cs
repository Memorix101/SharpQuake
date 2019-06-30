using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpQuake.Framework;

namespace SharpQuake
{
    public class SinglePlayerMenu : MenuBase
    {
        private const Int32 SINGLEPLAYER_ITEMS = 3;

        /// <summary>
        /// M_SinglePlayer_Key
        /// </summary>
        public override void KeyEvent( Int32 key )
        {
            switch ( key )
            {
                case KeysDef.K_ESCAPE:
                    MenuBase.MainMenu.Show( Host );
                    break;

                case KeysDef.K_DOWNARROW:
                    snd.LocalSound( "misc/menu1.wav" );
                    if ( ++_Cursor >= SINGLEPLAYER_ITEMS )
                        _Cursor = 0;
                    break;

                case KeysDef.K_UPARROW:
                    snd.LocalSound( "misc/menu1.wav" );
                    if ( --_Cursor < 0 )
                        _Cursor = SINGLEPLAYER_ITEMS - 1;
                    break;

                case KeysDef.K_ENTER:
                    Menu.EnterSound = true;

                    switch ( _Cursor )
                    {
                        case 0:
                            if ( server.sv.active )
                                if ( !Scr.ModalMessage( "Are you sure you want to\nstart a new game?\n" ) )
                                    break;
                            Key.Destination = keydest_t.key_game;
                            if ( server.sv.active )
                                Host.CommandBuffer.AddText( "disconnect\n" );
                            Host.CommandBuffer.AddText( "maxplayers 1\n" );
                            Host.CommandBuffer.AddText( "map start\n" );
                            break;

                        case 1:
                            MenuBase.LoadMenu.Show( Host );
                            break;

                        case 2:
                            MenuBase.SaveMenu.Show( Host );
                            break;
                    }
                    break;
            }
        }

        /// <summary>
        /// M_SinglePlayer_Draw
        /// </summary>
        public override void Draw( )
        {
            Menu.DrawTransPic( 16, 4, Drawer.CachePic( "gfx/qplaque.lmp" ) );
            GLPic p = Drawer.CachePic( "gfx/ttl_sgl.lmp" );
            Menu.DrawPic( ( 320 - p.width ) / 2, 4, p );
            Menu.DrawTransPic( 72, 32, Drawer.CachePic( "gfx/sp_menu.lmp" ) );

            var f = ( Int32 ) ( Host.Time * 10 ) % 6;

            Menu.DrawTransPic( 54, 32 + _Cursor * 20, Drawer.CachePic( String.Format( "gfx/menudot{0}.lmp", f + 1 ) ) );
        }
    }
}
