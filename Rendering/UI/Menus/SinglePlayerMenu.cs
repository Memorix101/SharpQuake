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
                    Host.Sound.LocalSound( "misc/menu1.wav" );
                    if ( ++_Cursor >= SINGLEPLAYER_ITEMS )
                        _Cursor = 0;
                    break;

                case KeysDef.K_UPARROW:
                    Host.Sound.LocalSound( "misc/menu1.wav" );
                    if ( --_Cursor < 0 )
                        _Cursor = SINGLEPLAYER_ITEMS - 1;
                    break;

                case KeysDef.K_ENTER:
                    Host.Menu.EnterSound = true;

                    switch ( _Cursor )
                    {
                        case 0:
                            if ( Host.Server.sv.active )
                                if ( !Host.Screen.ModalMessage( "Are you sure you want to\nstart a new game?\n" ) )
                                    break;
                            Host.Keyboard.Destination = KeyDestination.key_game;
                            if ( Host.Server.sv.active )
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
            Host.Menu.DrawTransPic( 16, 4, Host.DrawingContext.CachePic( "gfx/qplaque.lmp" ) );
            var p = Host.DrawingContext.CachePic( "gfx/ttl_sgl.lmp" );
            Host.Menu.DrawPic( ( 320 - p.width ) / 2, 4, p );
            Host.Menu.DrawTransPic( 72, 32, Host.DrawingContext.CachePic( "gfx/sp_menu.lmp" ) );

            var f = ( Int32 ) ( Host.Time * 10 ) % 6;

            Host.Menu.DrawTransPic( 54, 32 + _Cursor * 20, Host.DrawingContext.CachePic( String.Format( "gfx/menudot{0}.lmp", f + 1 ) ) );
        }
    }
}
