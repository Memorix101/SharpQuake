using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpQuake.Framework;

namespace SharpQuake
{
    public class HelpMenu : MenuBase
    {
        private const Int32 NUM_HELP_PAGES = 6;

        private Int32 _Page;

        public override void Show( Host host )
        {
            _Page = 0;
            base.Show( host );
        }

        public override void KeyEvent( Int32 key )
        {
            switch ( key )
            {
                case KeysDef.K_ESCAPE:
                    MenuBase.MainMenu.Show( Host );
                    break;

                case KeysDef.K_UPARROW:
                case KeysDef.K_RIGHTARROW:
                    Host.Menu.EnterSound = true;
                    if ( ++_Page >= NUM_HELP_PAGES )
                        _Page = 0;
                    break;

                case KeysDef.K_DOWNARROW:
                case KeysDef.K_LEFTARROW:
                    Host.Menu.EnterSound = true;
                    if ( --_Page < 0 )
                        _Page = NUM_HELP_PAGES - 1;
                    break;
            }
        }

        public override void Draw( )
        {
            Host.Menu.DrawPic( 0, 0, Host.DrawingContext.CachePic( String.Format( "gfx/help{0}.lmp", _Page ) ) );
        }
    }
}
