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
    public class FPSCounter : BaseUIElement, ITextRenderer
    {
        private UInt32 FrameCount
        {
            get;
            set;
        }

        public UInt32 FPS
        {
            get;
            private set;
        }

        private DateTime LastUpdate
        {
            get;
            set;
        }

        public FPSCounter( Host host ) : base( host )
        {
            HasInitialised = true;
        }

        // Not applicable to this component
        public void Enqueue( String text )
        {          
        }

        public override void Draw( )
        {
            base.Draw( );

            if ( !IsVisible || !HasInitialised )
                return;

            if ( DateTime.Now.Subtract( LastUpdate ).TotalSeconds >= 1 )
            {
                FPS = FrameCount;
                FrameCount = 0;
                LastUpdate = DateTime.Now;
            }

            FrameCount++;

            _host.DrawingContext.DrawString( _host.Screen.vid.width - 16 - 10, 10, $"{FPS}", System.Drawing.Color.Yellow );
        }
    }
}
