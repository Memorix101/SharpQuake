using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpQuake.Framework;

namespace SharpQuake
{
    public class VideoMenu : MenuBase
    {
        private struct modedesc_t
        {
            public Int32 modenum;
            public String desc;
            public Boolean iscur;
        } //modedesc_t;

        private const Int32 MAX_COLUMN_SIZE = 9;
        private const Int32 MODE_AREA_HEIGHT = MAX_COLUMN_SIZE + 2;
        private const Int32 MAX_MODEDESCS = MAX_COLUMN_SIZE * 3;

        private Int32 _WModes; // vid_wmodes
        private modedesc_t[] _ModeDescs = new modedesc_t[MAX_MODEDESCS]; // modedescs

        public override void KeyEvent( Int32 key )
        {
            switch ( key )
            {
                case KeysDef.K_ESCAPE:
                    Host.Sound.LocalSound( "misc/menu1.wav" );
                    MenuBase.OptionsMenu.Show( Host );
                    break;

                default:
                    break;
            }
        }

        public override void Draw( )
        {
            var p = Host.DrawingContext.CachePic( "gfx/vidmodes.lmp" );
            Host.Menu.DrawPic( ( 320 - p.width ) / 2, 4, p );

            _WModes = 0;
            var lnummodes = Host.Video.Modes.Length;

            for ( var i = 1; ( i < lnummodes ) && ( _WModes < MAX_MODEDESCS ); i++ )
            {
                var m = Host.Video.Modes[i];

                var k = _WModes;

                _ModeDescs[k].modenum = i;
                _ModeDescs[k].desc = String.Format( "{0}x{1}x{2}", m.width, m.height, m.bpp );
                _ModeDescs[k].iscur = false;

                if ( i == Host.Video.ModeNum )
                    _ModeDescs[k].iscur = true;

                _WModes++;
            }

            if ( _WModes > 0 )
            {
                Host.Menu.Print( 2 * 8, 36 + 0 * 8, "Fullscreen Modes (WIDTHxHEIGHTxBPP)" );

                var column = 8;
                var row = 36 + 2 * 8;

                for ( var i = 0; i < _WModes; i++ )
                {
                    if ( _ModeDescs[i].iscur )
                        Host.Menu.PrintWhite( column, row, _ModeDescs[i].desc );
                    else
                        Host.Menu.Print( column, row, _ModeDescs[i].desc );

                    column += 13 * 8;

                    if ( ( i % vid.VID_ROW_SIZE ) == ( vid.VID_ROW_SIZE - 1 ) )
                    {
                        column = 8;
                        row += 8;
                    }
                }
            }

            Host.Menu.Print( 3 * 8, 36 + MODE_AREA_HEIGHT * 8 + 8 * 2, "Video modes must be set from the" );
            Host.Menu.Print( 3 * 8, 36 + MODE_AREA_HEIGHT * 8 + 8 * 3, "command line with -width <width>" );
            Host.Menu.Print( 3 * 8, 36 + MODE_AREA_HEIGHT * 8 + 8 * 4, "and -bpp <bits-per-pixel>" );
            Host.Menu.Print( 3 * 8, 36 + MODE_AREA_HEIGHT * 8 + 8 * 6, "Select windowed mode with -window" );
        }
    }
}
