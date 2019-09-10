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
    public enum Q3SurfaceFlags : Int32
    {
        NoDamage = 1,
        Slick = 2,
        Sky = 4,
        Ladder = 8, // has no surfaceparm
        NoImpact = 16,
        NoMarks = 32,
        Flesh = 64 ,// has no surfaceparm
        NoDraw = 128,
        Hint = 256,
        Skip = 512, // has no surfaceparm
        NoLightMap = 1024,
        PointLight = 2048,
        MetalSteps = 4096,
        NoSteps = 8192, // has no surfaceparm
        NonSolid = 16384,
        LightFilter = 32768,
        AreaShadow = 65536,
        NoDynamicLight = 131072,
        Dust = 262144
    }
}
