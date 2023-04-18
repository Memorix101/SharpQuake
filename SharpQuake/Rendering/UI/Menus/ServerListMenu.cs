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
    public class ServerListMenu : BaseMenu
    {
        private Boolean _Sorted;

        public ServerListMenu( MenuFactory menuFactory ) : base( "menu_server_list", menuFactory )
        {
        }

        public override void Show( Host host )
        {
            base.Show( host );
            Cursor = 0;
            Host.Menus.ReturnOnError = false;
            Host.Menus.ReturnReason = String.Empty;
            _Sorted = false;
        }

        public override void KeyEvent( Int32 key )
        {
            switch ( key )
            {
                case KeysDef.K_ESCAPE:
                    MenuFactory.Show( "menu_lan_config" );
                    break;

                case KeysDef.K_SPACE:
                    MenuFactory.Show( "menu_search" );
                    break;

                case KeysDef.K_UPARROW:
                case KeysDef.K_LEFTARROW:
                    Host.Sound.LocalSound( "misc/menu1.wav" );
                    Cursor--;
                    if ( Cursor < 0 )
                        Cursor = Host.Network.HostCacheCount - 1;
                    break;

                case KeysDef.K_DOWNARROW:
                case KeysDef.K_RIGHTARROW:
                    Host.Sound.LocalSound( "misc/menu1.wav" );
                    Cursor++;
                    if ( Cursor >= Host.Network.HostCacheCount )
                        Cursor = 0;
                    break;

                case KeysDef.K_ENTER:
                    Host.Sound.LocalSound( "misc/menu2.wav" );
                    Host.Menus.ReturnMenu = this;
                    Host.Menus.ReturnOnError = true;
                    _Sorted = false;
                    MenuFactory.CurrentMenu.Hide( );
                    Host.Commands.Buffer.Append( String.Format( "connect \"{0}\"\n", Host.Network.HostCache[Cursor].cname ) );
                    break;

                default:
                    break;
            }
        }

        public override void Draw( )
        {
            if ( !_Sorted )
            {
                if ( Host.Network.HostCacheCount > 1 )
                {
                    Comparison<hostcache_t> cmp = delegate ( hostcache_t a, hostcache_t b )
                    {
                        return String.Compare( a.cname, b.cname );
                    };

                    Array.Sort( Host.Network.HostCache, cmp );
                }
                _Sorted = true;
            }

            var p = Host.Pictures.Cache( "gfx/p_multi.lmp", "GL_NEAREST" );
            Host.Menus.DrawPic( ( 320 - p.Width ) / 2, 4, p );
            for ( var n = 0; n < Host.Network.HostCacheCount; n++ )
            {
                var hc = Host.Network.HostCache[n];
                String tmp;
                if ( hc.maxusers > 0 )
                    tmp = String.Format( "{0,-15} {1,-15} {2:D2}/{3:D2}\n", hc.name, hc.map, hc.users, hc.maxusers );
                else
                    tmp = String.Format( "{0,-15} {1,-15}\n", hc.name, hc.map );
                Host.Menus.Print( 16, 32 + 8 * n, tmp );
            }
            Host.Menus.DrawCharacter( 0, 32 + Cursor * 8, 12 + ( ( Int32 ) ( Host.RealTime * 4 ) & 1 ) );

            if ( !String.IsNullOrEmpty( Host.Menus.ReturnReason ) )
                Host.Menus.PrintWhite( 16, 148, Host.Menus.ReturnReason );
        }
    }
}
