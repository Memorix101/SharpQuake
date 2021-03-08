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
using SharpQuake.Factories.Rendering.UI;
using SharpQuake.Framework;

namespace SharpQuake.Rendering.UI
{
    public class SaveMenu : LoadMenu
    {
        public SaveMenu( MenuFactory menuFactory ) : base( "menu_save", menuFactory )
		{
		}

        public override void Show( Host host )
        {
            if ( !Host.Server.sv.active )
                return;
            if ( Host.Client.cl.intermission != 0 )
                return;
            if ( Host.Server.svs.maxclients != 1 )
                return;

            base.Show( host );
        }

        public override void KeyEvent( Int32 key )
        {
            switch ( key )
            {
                case KeysDef.K_ESCAPE:
                    MenuFactory.Show( "menu_singleplayer" );
                    break;

                case KeysDef.K_ENTER:
                    MenuFactory.CurrentMenu.Hide( );
                    Host.Commands.Buffer.Append( String.Format( "save s{0}\n", Cursor ) );
                    return;

                case KeysDef.K_UPARROW:
                case KeysDef.K_LEFTARROW:
                    Host.Sound.LocalSound( "misc/menu1.wav" );
                    Cursor--;
                    if ( Cursor < 0 )
                        Cursor = MAX_SAVEGAMES - 1;
                    break;

                case KeysDef.K_DOWNARROW:
                case KeysDef.K_RIGHTARROW:
                    Host.Sound.LocalSound( "misc/menu1.wav" );
                    Cursor++;
                    if ( Cursor >= MAX_SAVEGAMES )
                        Cursor = 0;
                    break;
            }
        }

        public override void Draw( )
        {
            var p = Host.DrawingContext.CachePic( "gfx/p_save.lmp", "GL_NEAREST" );
            Host.Menus.DrawPic( ( 320 - p.Width ) / 2, 4, p );

            for ( var i = 0; i < MAX_SAVEGAMES; i++ )
                Host.Menus.Print( 16, 32 + 8 * i, _FileNames[i] );

            // line cursor
            Host.Menus.DrawCharacter( 8, 32 + Cursor * 8, 12 + ( ( Int32 ) ( Host.RealTime * 4 ) & 1 ) );
        }
    }
}
