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

namespace SharpQuake.Rendering.UI.Elements.HUD
{
    public class IntermissionOverlay : BaseUIElement
    {
        public override Boolean ManualInitialisation
        {
            get
            {
                return true;
            }
        }

        private HudResources _resources;

        public IntermissionOverlay( Host host ) : base( host )
        {
        }

        public override void Initialise( )
        {
            base.Initialise( );

            _resources = _host.Screen.HudResources;

            HasInitialised = true;
        }

        /// <summary>
        /// Sbar_IntermissionOverlay
        /// called each frame after the level has been completed
        /// </summary>
        public override void Draw( )
        {
            base.Draw( );

            if ( !IsVisible || !HasInitialised )
                return;

            var pic = _host.DrawingContext.CachePic( "gfx/complete.lmp", "GL_LINEAR" );
            _host.Video.Device.Graphics.DrawPicture( pic, 64, 24 );

            pic = _host.DrawingContext.CachePic( "gfx/inter.lmp", "GL_LINEAR" );
            _host.Video.Device.Graphics.DrawPicture( pic, 0, 56, hasAlpha: true );

            // time
            var dig = _host.Client.cl.completed_time / 60;
            IntermissionNumber( 160, 64, dig, 3, 0 );
            var num = _host.Client.cl.completed_time - dig * 60;

            _host.Video.Device.Graphics.DrawPicture( _resources.Colon, 234, 64, hasAlpha: true );

            _host.Video.Device.Graphics.DrawPicture( _resources.Numbers[0, num / 10], 246, 64, hasAlpha: true );
            _host.Video.Device.Graphics.DrawPicture( _resources.Numbers[0, num % 10], 266, 64, hasAlpha: true );

            IntermissionNumber( 160, 104, _host.Client.cl.stats[QStatsDef.STAT_SECRETS], 3, 0 );
            _host.Video.Device.Graphics.DrawPicture( _resources.Slash, 232, 104, hasAlpha: true );
            IntermissionNumber( 240, 104, _host.Client.cl.stats[QStatsDef.STAT_TOTALSECRETS], 3, 0 );

            IntermissionNumber( 160, 144, _host.Client.cl.stats[QStatsDef.STAT_MONSTERS], 3, 0 );
            _host.Video.Device.Graphics.DrawPicture( _resources.Slash, 232, 144, hasAlpha: true );
            IntermissionNumber( 240, 144, _host.Client.cl.stats[QStatsDef.STAT_TOTALMONSTERS], 3, 0 );
        }

        /// <summary>
        /// Sbar_IntermissionNumber
        /// </summary>
        private void IntermissionNumber( Int32 x, Int32 y, Int32 num, Int32 digits, Int32 color )
        {
            var str = num.ToString( );
            if ( str.Length > digits )
            {
                str = str.Remove( 0, str.Length - digits );
            }

            if ( str.Length < digits )
                x += ( digits - str.Length ) * 24;

            for ( var i = 0; i < str.Length; i++ )
            {
                var frame = ( str[i] == '-' ? HudResources.STAT_MINUS : str[i] - '0' );

                _host.Video.Device.Graphics.DrawPicture( _resources.Numbers[color, frame], x, y, hasAlpha: true );

                //_host.DrawingContext.DrawTransPic( x, y, _Nums[color, frame] );
                x += 24;
            }
        }
    }
}
