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
    public class MultiplayerMenu : BaseMenu
    {
        private const Int32 MULTIPLAYER_ITEMS = 3;

        public MultiplayerMenu( MenuFactory menuFactory ) : base( "menu_multiplayer", menuFactory )
        {
        }

        public override void KeyEvent( Int32 key )
        {
            switch ( key )
            {
                case KeysDef.K_ESCAPE:
                    MenuFactory.Show( "menu_main" );
                    break;

                case KeysDef.K_DOWNARROW:
                    Host.Sound.LocalSound( "misc/menu1.wav" );
                    if ( ++Cursor >= MULTIPLAYER_ITEMS )
                        Cursor = 0;
                    break;

                case KeysDef.K_UPARROW:
                    Host.Sound.LocalSound( "misc/menu1.wav" );
                    if ( --Cursor < 0 )
                        Cursor = MULTIPLAYER_ITEMS - 1;
                    break;

                case KeysDef.K_ENTER:
                    Host.Menus.EnterSound = true;
                    switch ( Cursor )
                    {
                        case 0:
                            if ( Host.Network.TcpIpAvailable )
                                MenuFactory.Show( "menu_lan_config" );
                            break;

                        case 1:
                            if ( Host.Network.TcpIpAvailable )
                                MenuFactory.Show( "menu_lan_config" );
                            break;

                        case 2:
                            MenuFactory.Show( "menu_setup" );
                            break;
                    }
                    break;
            }
        }

        public override void Draw( )
        {
            Host.Menus.DrawTransPic( 16, 4, Host.DrawingContext.CachePic( "gfx/qplaque.lmp", "GL_NEAREST" ) );
            var p = Host.DrawingContext.CachePic( "gfx/p_multi.lmp", "GL_NEAREST" );
            Host.Menus.DrawPic( ( 320 - p.Width ) / 2, 4, p );
            Host.Menus.DrawTransPic( 72, 32, Host.DrawingContext.CachePic( "gfx/mp_menu.lmp", "GL_NEAREST" ) );

            Single f = ( Int32 ) ( Host.Time * 10 ) % 6;

            Host.Menus.DrawTransPic( 54, 32 + Cursor * 20, Host.DrawingContext.CachePic( String.Format( "gfx/menudot{0}.lmp", f + 1 ), "GL_NEAREST" ) );

            if ( Host.Network.TcpIpAvailable )
                return;
            Host.Menus.PrintWhite( ( 320 / 2 ) - ( ( 27 * 8 ) / 2 ), 148, "No Communications Available" );
        }
    }
}
