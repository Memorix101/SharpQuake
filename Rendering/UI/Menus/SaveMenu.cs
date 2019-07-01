using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpQuake.Framework;

namespace SharpQuake
{
    public class SaveMenu : LoadMenu
    {
        public override void Show( Host host )
        {
            if ( !server.sv.active )
                return;
            if ( client.cl.intermission != 0 )
                return;
            if ( server.svs.maxclients != 1 )
                return;

            base.Show( host );
        }

        public override void KeyEvent( Int32 key )
        {
            switch ( key )
            {
                case KeysDef.K_ESCAPE:
                    MenuBase.SinglePlayerMenu.Show( Host );
                    break;

                case KeysDef.K_ENTER:
                    MenuBase.CurrentMenu.Hide( );
                    Host.CommandBuffer.AddText( String.Format( "save s{0}\n", _Cursor ) );
                    return;

                case KeysDef.K_UPARROW:
                case KeysDef.K_LEFTARROW:
                    snd.LocalSound( "misc/menu1.wav" );
                    _Cursor--;
                    if ( _Cursor < 0 )
                        _Cursor = MAX_SAVEGAMES - 1;
                    break;

                case KeysDef.K_DOWNARROW:
                case KeysDef.K_RIGHTARROW:
                    snd.LocalSound( "misc/menu1.wav" );
                    _Cursor++;
                    if ( _Cursor >= MAX_SAVEGAMES )
                        _Cursor = 0;
                    break;
            }
        }

        public override void Draw( )
        {
            GLPic p = Drawer.CachePic( "gfx/p_save.lmp" );
            Menu.DrawPic( ( 320 - p.width ) / 2, 4, p );

            for ( var i = 0; i < MAX_SAVEGAMES; i++ )
                Menu.Print( 16, 32 + 8 * i, _FileNames[i] );

            // line cursor
            Menu.DrawCharacter( 8, 32 + _Cursor * 8, 12 + ( ( Int32 ) ( Host.RealTime * 4 ) & 1 ) );
        }
    }
}
