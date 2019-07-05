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

namespace SharpQuake.Framework
{
    public static class SurfaceDef
    {
        public const Int32 SURF_PLANEBACK = 2;
        public const Int32 SURF_DRAWSKY = 4;
        public const Int32 SURF_DRAWSPRITE = 8;
        public const Int32 SURF_DRAWTURB = 0x10;
        public const Int32 SURF_DRAWTILED = 0x20;
        public const Int32 SURF_DRAWBACKGROUND = 0x40;
        public const Int32 SURF_UNDERWATER = 0x80;
    }
}
