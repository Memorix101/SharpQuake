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

using SharpQuake.Framework;
using System;
using System.Text;

namespace SharpQuake.Rendering.UI.Elements.HUD
{
    public class SPScoreboard : BaseUIElement
    {
        public override Boolean ManualInitialisation
        {
            get
            {
                return true;
            }
        }

        private HudResources _resources;

        public SPScoreboard( Host host ) : base( host )
        {
        }

        public override void Initialise( )
        {
            base.Initialise( );

            _resources = _host.Screen.HudResources;

            HasInitialised = true;
        }

        public override void Draw( )
        {
            base.Draw( );

            if ( !IsVisible || !HasInitialised )
                return;

            _host.Screen.CopyEverithing = true;
            _host.Screen.FullUpdate = 0;

            _resources.DrawPic( 0, 0, _resources.ScoreBar );
            SoloScoreboard( );
        }

        /// <summary>
        /// Sbar_SoloScoreboard
        /// </summary>
        private void SoloScoreboard( )
        {
            var sb = new StringBuilder( 80 );
            var cl = _host.Client.cl;

            sb.AppendFormat( "Monsters:{0,3:d} /{1,3:d}", cl.stats[QStatsDef.STAT_MONSTERS], _host.Client.cl.stats[QStatsDef.STAT_TOTALMONSTERS] );
            _resources.DrawString( 8, 4, sb.ToString( ) );

            sb.Length = 0;
            sb.AppendFormat( "Secrets :{0,3:d} /{1,3:d}", cl.stats[QStatsDef.STAT_SECRETS], cl.stats[QStatsDef.STAT_TOTALSECRETS] );
            _resources.DrawString( 8, 12, sb.ToString( ) );

            // time
            var minutes = ( Int32 ) ( cl.time / 60.0 );
            var seconds = ( Int32 ) ( cl.time - 60 * minutes );
            var tens = seconds / 10;
            var units = seconds - 10 * tens;
            sb.Length = 0;
            sb.AppendFormat( "Time :{0,3}:{1}{2}", minutes, tens, units );
            _resources.DrawString( 184, 4, sb.ToString( ) );

            // draw level name
            var l = cl.levelname.Length;
            _resources.DrawString( 232 - l * 4, 12, cl.levelname );
        }
    }
}
