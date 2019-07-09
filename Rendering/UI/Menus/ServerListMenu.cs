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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpQuake.Framework;

namespace SharpQuake
{
    public class ServerListMenu : MenuBase
    {
        private Boolean _Sorted;

        public override void Show( Host host )
        {
            base.Show( host );
            _Cursor = 0;
            Host.Menu.ReturnOnError = false;
            Host.Menu.ReturnReason = String.Empty;
            _Sorted = false;
        }

        public override void KeyEvent( Int32 key )
        {
            switch ( key )
            {
                case KeysDef.K_ESCAPE:
                    MenuBase.LanConfigMenu.Show( Host );
                    break;

                case KeysDef.K_SPACE:
                    MenuBase.SearchMenu.Show( Host );
                    break;

                case KeysDef.K_UPARROW:
                case KeysDef.K_LEFTARROW:
                    Host.Sound.LocalSound( "misc/menu1.wav" );
                    _Cursor--;
                    if ( _Cursor < 0 )
                        _Cursor = Host.Network.HostCacheCount - 1;
                    break;

                case KeysDef.K_DOWNARROW:
                case KeysDef.K_RIGHTARROW:
                    Host.Sound.LocalSound( "misc/menu1.wav" );
                    _Cursor++;
                    if ( _Cursor >= Host.Network.HostCacheCount )
                        _Cursor = 0;
                    break;

                case KeysDef.K_ENTER:
                    Host.Sound.LocalSound( "misc/menu2.wav" );
                    Host.Menu.ReturnMenu = this;
                    Host.Menu.ReturnOnError = true;
                    _Sorted = false;
                    MenuBase.CurrentMenu.Hide( );
                    Host.CommandBuffer.AddText( String.Format( "connect \"{0}\"\n", Host.Network.HostCache[_Cursor].cname ) );
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

            var p = Host.DrawingContext.CachePic( "gfx/p_multi.lmp", "GL_NEAREST" );
            Host.Menu.DrawPic( ( 320 - p.Width ) / 2, 4, p );
            for ( var n = 0; n < Host.Network.HostCacheCount; n++ )
            {
                var hc = Host.Network.HostCache[n];
                String tmp;
                if ( hc.maxusers > 0 )
                    tmp = String.Format( "{0,-15} {1,-15} {2:D2}/{3:D2}\n", hc.name, hc.map, hc.users, hc.maxusers );
                else
                    tmp = String.Format( "{0,-15} {1,-15}\n", hc.name, hc.map );
                Host.Menu.Print( 16, 32 + 8 * n, tmp );
            }
            Host.Menu.DrawCharacter( 0, 32 + _Cursor * 8, 12 + ( ( Int32 ) ( Host.RealTime * 4 ) & 1 ) );

            if ( !String.IsNullOrEmpty( Host.Menu.ReturnReason ) )
                Host.Menu.PrintWhite( 16, 148, Host.Menu.ReturnReason );
        }
    }
}
