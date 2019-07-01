using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpQuake.Framework;

namespace SharpQuake
{
    public class SearchMenu : MenuBase
    {
        private Boolean _SearchComplete;
        private Double _SearchCompleteTime;

        public override void Show( Host host )
        {
            base.Show( host );
            net.SlistSilent = true;
            net.SlistLocal = false;
            _SearchComplete = false;
            net.Slist_f( );
        }

        public override void KeyEvent( Int32 key )
        {
            // nothing to do
        }

        public override void Draw( )
        {
            GLPic p = Drawer.CachePic( "gfx/p_multi.lmp" );
            Host.Menu.DrawPic( ( 320 - p.width ) / 2, 4, p );
            var x = ( 320 / 2 ) - ( ( 12 * 8 ) / 2 ) + 4;
            Host.Menu.DrawTextBox( x - 8, 32, 12, 1 );
            Host.Menu.Print( x, 40, "Searching..." );

            if ( net.SlistInProgress )
            {
                net.Poll( );
                return;
            }

            if ( !_SearchComplete )
            {
                _SearchComplete = true;
                _SearchCompleteTime = Host.RealTime;
            }

            if ( net.HostCacheCount > 0 )
            {
                MenuBase.ServerListMenu.Show( Host );
                return;
            }

            Host.Menu.PrintWhite( ( 320 / 2 ) - ( ( 22 * 8 ) / 2 ), 64, "No Quake servers found" );
            if ( ( Host.RealTime - _SearchCompleteTime ) < 3.0 )
                return;

            MenuBase.LanConfigMenu.Show( Host );
        }
    }
}
