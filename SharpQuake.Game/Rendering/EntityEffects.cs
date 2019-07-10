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

namespace SharpQuake.Game.Rendering
{
    // entity effects
    public static class EntityEffects
    {
        public static Int32 EF_BRIGHTFIELD = 1;
        public static Int32 EF_MUZZLEFLASH = 2;
        public static Int32 EF_BRIGHTLIGHT = 4;
        public static Int32 EF_DIMLIGHT = 8;
#if QUAKE2
        public static int EF_DARKLIGHT = 16;
        public static int EF_DARKFIELD = 32;
        public static int EF_LIGHT = 64;
        public static int EF_NODRAW = 128;
#endif
    }
}
