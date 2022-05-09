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
    public class ModalMessage : BaseUIElement, ITextRenderer
    {
        public override Boolean IsVisible
        {
            get;
            set;
        } = false;

        private String Message
        {
            get;
            set;
        }

        public ModalMessage( Host host ) : base( host )
        {
            HasInitialised = true;
        }

        public void Enqueue( String text )
        {
            Message = text;
        }

        // Not applicable to this component
        public void Reset( )
        {
            Message = null;
        }

        /// <summary>
        /// SCR_DrawNotifyString
        /// </summary>
        public override void Draw( )
        {
            base.Draw( );

            if ( !IsVisible || !HasInitialised )
                return;

            if ( string.IsNullOrEmpty( Message ) )
                return;

            var offset = 0;
            var y = ( Int32 ) ( _host.Screen.vid.height * 0.35 );

            do
            {
                var end = Message.IndexOf( '\n', offset );
                if ( end == -1 )
                    end = Message.Length;
                if ( end - offset > 40 )
                    end = offset + 40;

                var length = end - offset;
                if ( length > 0 )
                {
                    var x = ( _host.Screen.vid.width - length * 8 ) / 2;
                    for ( var j = 0; j < length; j++, x += 8 )
                        _host.DrawingContext.DrawCharacter( x, y, Message[offset + j] );

                    y += 8;
                }
                offset = end + 1;
            } while ( offset < Message.Length );
        }
    }
}
