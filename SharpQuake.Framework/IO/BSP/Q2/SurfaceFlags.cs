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

namespace SharpQuake.Framework.IO.BSP
{
    [Flags]
    public enum Q2SurfaceFlags : Int32
    {
        Light = 0x1, // value will hold the light strength
        Slick = 0x2, // effects game physics
        Sky = 0x4, // don't draw, but add to skybox
        Warp = 0x8, // turbulent water warp
        Trans33 = 0x10,
        Trans66 = 0x20,
        Flowing = 0x40, // scroll towards angle
        NoDraw = 0x80, // don't bother referencing the texture
        Hint = 0x100, // make a primary bsp splitter
        Skip = 0x200, // completely ignore, allowing non-closed brushes
        AlphaTest = 0x02000000, // alpha test masking of color 255 in wal textures (supported by modded engines)
    }
}