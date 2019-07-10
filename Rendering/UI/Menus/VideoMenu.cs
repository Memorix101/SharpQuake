/// <copyright>
///
/// SharpQuakeEvolved changes by optimus-code, 2019
/// 
/// Based on SharpQuake (Quake Rewritten in C# by Yury Kiselev, 2010.)
///
/// Copyright (C) 1996-1997 Id Software, Inc.
///
/// This program is free software; you can redistribute it and/or
/// modify it under the terms of the GNU General Public License
/// as published by the Free Software Foundation; either version 2
/// of the License, or (at your option) any later version.
///
/// This program is distributed in the hope that it will be useful,
/// but WITHOUT ANY WARRANTY; without even the implied warranty of
/// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
///
/// See the GNU General Public License for more details.
///
/// You should have received a copy of the GNU General Public License
/// along with this program; if not, write to the Free Software
/// Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
/// </copyright>

using System;
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
            var p = Host.DrawingContext.CachePic( "gfx/vidmodes.lmp", "GL_NEAREST" );
            Host.Menu.DrawPic( ( 320 - p.Width ) / 2, 4, p );

            _WModes = 0;
            var lnummodes = Host.Video.Device.AvailableModes.Length;

            for ( var i = 1; ( i < lnummodes ) && ( _WModes < MAX_MODEDESCS ); i++ )
            {
                var m = Host.Video.Device.AvailableModes[i];

                var k = _WModes;

                _ModeDescs[k].modenum = i;
                _ModeDescs[k].desc = String.Format( "{0}x{1}x{2}", m.Width, m.Height, m.BitsPerPixel );
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

                    if ( ( i % Vid.VID_ROW_SIZE ) == ( Vid.VID_ROW_SIZE - 1 ) )
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
