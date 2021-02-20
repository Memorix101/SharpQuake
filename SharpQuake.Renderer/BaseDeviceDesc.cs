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
using System.Drawing;

namespace SharpQuake.Renderer
{
    public class BaseDeviceDesc
    {
        public virtual Boolean IsFullScreen
        {
            get;
            set;
        }

        public virtual Boolean SupportsMultiTexture
        {
            get;
            set;
        }

        public virtual Boolean MultiTexturing
        {
            get;
            set;
        }

        public virtual Int32 Width
        {
            get;
            set;
        }

        public virtual Int32 Height
        {
            get;
            set;
        }

        public virtual Int32 ActualWidth
        {
            get;
            set;
        }

        public virtual Int32 ActualHeight
        {
            get;
            set;
        }

        public virtual Double AspectRatio
        {
            get;
            set;
        }

        public virtual Single Gamma
        {
            get;
            set;
        }

        public virtual String Renderer
        {
            get;
            set;
        }

        public virtual String Vendor
        {
            get;
            set;
        }

        public virtual String Version
        {
            get;
            set;
        }

        public virtual String Extensions
        {
            get;
            set;
        }

        public virtual Rectangle ViewRect
        {
            get;
            set;
        }

        public virtual Single DepthMinimum
        {
            get;
            set;
        }

        public virtual Single DepthMaximum
        {
            get;
            set;
        }

        public virtual Int32 TrickFrame
        {
            get;
            set;
        }
    }
}
