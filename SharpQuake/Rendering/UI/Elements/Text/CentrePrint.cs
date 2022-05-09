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

using SharpQuake.Framework.IO.Input;
using SharpQuake.Framework.Rendering.UI;
using System;
using System.Collections.Generic;
using System.Text;

namespace SharpQuake.Rendering.UI.Elements.Text
{
    public class CentrePrint : BaseUIElement, ITextRenderer
    {
        private Int32 _CenterLines; // scr_center_lines
        private Int32 _EraseLines; // scr_erase_lines

        public Single CenterTimeOff;

        //int _EraseCenter; // scr_erase_center
        private Single _CenterTimeStart; // scr_centertime_start	// for slow victory printing

        // scr_centertime_off
        private String _CenterString; // char	scr_centerstring[1024]

        public CentrePrint( Host host ) : base( host )
        {
            HasInitialised = true;
        }

        // SCR_CenterPrint
        //
        // Called for important messages that should stay in the center of the screen
        // for a few moments
        public void Enqueue( String str )
        {
            _CenterString = str;
            CenterTimeOff = _host.Cvars.CenterTime.Get<Int32>( );
            _CenterTimeStart = ( Single ) _host.Client.cl.time;

            // count the number of lines for centering
            _CenterLines = 1;
            foreach ( var c in _CenterString )
            {
                if ( c == '\n' )
                    _CenterLines++;
            }
        }

        // SCR_DrawCenterString
        private void DrawCenterString( )
        {
            Int32 remaining;

            // the finale prints the characters one at a time
            if ( _host.Client.cl.intermission > 0 )
                remaining = ( Int32 ) ( _host.Cvars.PrintSpeed.Get<Int32>( ) * ( _host.Client.cl.time - _CenterTimeStart ) );
            else
                remaining = 9999;

            var y = 48;
            if ( _CenterLines <= 4 )
                y = ( Int32 ) ( _host.Screen.vid.height * 0.35 );

            var lines = _CenterString.Split( '\n' );
            for ( var i = 0; i < lines.Length; i++ )
            {
                var line = lines[i].TrimEnd( '\r' );
                var x = ( _host.Screen.vid.width - line.Length * 8 ) / 2;

                for ( var j = 0; j < line.Length; j++, x += 8 )
                {
                    _host.DrawingContext.DrawCharacter( x, y, line[j] );
                    if ( remaining-- <= 0 )
                        return;
                }
                y += 8;
            }
        }

        // SCR_CheckDrawCenterString
        private void CheckDrawCenterString( )
        {
            _host.Screen.CopyTop = true;

            if ( _CenterLines > _EraseLines )
                _EraseLines = _CenterLines;

            CenterTimeOff -= ( Single ) _host.FrameTime;

            if ( CenterTimeOff <= 0 && _host.Client.cl.intermission == 0 )
                return;

            if ( _host.Keyboard.Destination != KeyDestination.key_game )
                return;

            DrawCenterString( );
        }

        public override void Draw( )
        {
            base.Draw( );

            if ( !IsVisible || !HasInitialised )
                return;

            CheckDrawCenterString( );
        }

        public void Reset()
        {
            CenterTimeOff = 0;
        }
    }
}
