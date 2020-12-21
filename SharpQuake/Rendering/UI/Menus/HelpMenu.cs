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

namespace SharpQuake.Rendering.UI
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
                    MainMenu.Show( Host );
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
            Host.Menu.DrawPic( 0, 0, Host.DrawingContext.CachePic( String.Format( "gfx/help{0}.lmp", _Page ), "GL_NEAREST" ) );
        }
    }
}
