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

using SharpQuake.Framework.Rendering.UI;
using System;

namespace SharpQuake.Rendering.UI.Elements
{
    /// <summary>
    /// Represents the most basic functions for an element
    /// </summary>
    /// <remarks>
    /// (Excluding rendering which is left to classes that inherit from this)
    /// </remarks>
    public class BaseUIElement : IElementRenderer
    {
        /// <summary>
        /// Is the element visible
        /// </summary>
        /// <remarks>
        /// (Defaults to true)
        /// </remarks>
        public virtual Boolean IsVisible
        {
            get;
            set;
        } = true;

        private Boolean _IsDirty = false;

        /// <summary>
        /// Does the element need re-drawing
        /// </summary>
        public Boolean IsDirty
        {
            get
            {
                return _IsDirty;
            }
            set
            {
                _IsDirty = true;
                OnDirty( );
            }
        }

        public Boolean HasInitialised
        {
            get;
            protected set;
        }

        public virtual Boolean ManualInitialisation
        {
            get
            {
                return false;
            }
        }

        protected readonly Host _host;

        public BaseUIElement( Host host )
        {
            _host = host;
        }

        /// <summary>
        /// Initialise any textures, resources etc.
        /// </summary>
        public virtual void Initialise( )
        {
        }

        /// <summary>
        /// Draw the UI element
        /// </summary>
        public virtual void Draw( )
        {
            _IsDirty = false;
        }

        /// <summary>
        /// The element needs re-drawing
        /// </summary>
        protected virtual void OnDirty( )
        {
        }
    }
}
