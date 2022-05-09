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

using SharpQuake.Factories.Rendering.UI;
using SharpQuake.Framework;
using SharpQuake.Framework.IO.Input;
using SharpQuake.Framework.Rendering.UI;
using System;

namespace SharpQuake.Rendering.UI.Elements
{
    public class VisualConsole : BaseUIElement, IResetableRenderer
    {
        //scr_con_current
        public Single ConCurrent
        {
            get;
            private set;
        }

        private Single _ConLines;		// lines of console to display
        private Int32 _ClearConsole; // clearconsole
                                     // clearnotify

        public VisualConsole( Host host ) : base( host )
        {
        }

        public override void Initialise()
        {
            
            HasInitialised = true;
        }

        /// <summary>
        /// SCR_SetUpToDrawConsole
        /// </summary>
        public void Configure()
        {
            _host.Console.CheckResize( );

            if ( _host.Screen.Elements.IsVisible( ElementFactory.LOADING ) )
                return;     // never a console with loading plaque

            // decide on the height of the console
            _host.Console.ForcedUp = ( _host.Client.cl.worldmodel == null ) || ( _host.Client.cls.signon != ClientDef.SIGNONS );

            if ( _host.Console.ForcedUp )
            {
                _ConLines = _host.Screen.vid.height; // full screen
                ConCurrent = _ConLines;
            }
            else if ( _host.Keyboard.Destination == KeyDestination.key_console )
                _ConLines = _host.Screen.vid.height / 2; // half screen
            else
                _ConLines = 0; // none visible

            if ( _ConLines < ConCurrent )
            {
                ConCurrent -= ( Int32 ) ( _host.Cvars.ConSpeed.Get<Int32>( ) * _host.FrameTime );
                if ( _ConLines > ConCurrent )
                    ConCurrent = _ConLines;
            }
            else if ( _ConLines > ConCurrent )
            {
                ConCurrent += ( Int32 ) ( _host.Cvars.ConSpeed.Get<Int32>( ) * _host.FrameTime );
                if ( _ConLines < ConCurrent )
                    ConCurrent = _ConLines;
            }

            if ( _ClearConsole++ < _host.Screen.vid.numpages )
            {
                _host.Screen.Elements.SetDirty( ElementFactory.HUD );
            }
            else if ( _host.Screen.ClearNotify++ < _host.Screen.vid.numpages )
            {
                //????????????
            }
            else
                _host.Console.NotifyLines = 0;
        }

        /// <summary>
        /// SCR_DrawConsole
        /// </summary>
        public override void Draw( )
        {
            base.Draw( );

            if ( !HasInitialised )
                return;

            if ( ConCurrent > 0 )
            {
                _host.Screen.CopyEverithing = true;
                _host.Console.Draw( ( Int32 ) ConCurrent, true );
                _ClearConsole = 0;
            }
            else if ( _host.Keyboard.Destination == KeyDestination.key_game ||
                _host.Keyboard.Destination == KeyDestination.key_message )
            {
                _host.Console.DrawNotify( );	// only draw notify in game
            }
        }

        public void Reset( )
        {
            ConCurrent = 0;
        }
    }
}
