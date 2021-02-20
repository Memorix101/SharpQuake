/// <copyright>
///
/// SharpQuakeEvolved changes by optimus-code, 2019
/// 
/// Based on SharpQuake (Quake Rewritten in C# by Yury Kiselev, 2010.)
///
/// Copyright (C) 1996-1997 Id Software, Inc.
///
/// This program is free software, you can redistribute it and/or
/// modify it under the terms of the GNU General Public License
/// as published by the Free Software Foundation, either version 2
/// of the License, or (at your option) any later version.
///
/// This program is distributed in the hope that it will be useful,
/// but WITHOUT ANY WARRANTY, without even the implied warranty of
/// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
///
/// See the GNU General Public License for more details.
///
/// You should have received a copy of the GNU General Public License
/// along with this program, if not, write to the Free Software
/// Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
/// </copyright>
/// <copyright>
///
/// SharpQuakeEvolved changes by optimus-code, 2019
/// 
/// Based on SharpQuake (Quake Rewritten in C# by Yury Kiselev, 2010.)
///
/// Copyright (C) 1996-1997 Id Software, Inc.
///
/// This program is free software, you can redistribute it and/or
/// modify it under the terms of the GNU General Public License
/// as published by the Free Software Foundation, either version 2
/// of the License, or (at your option) any later version.
///
/// This program is distributed in the hope that it will be useful,
/// but WITHOUT ANY WARRANTY, without even the implied warranty of
/// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
///
/// See the GNU General Public License for more details.
///
/// You should have received a copy of the GNU General Public License
/// along with this program, if not, write to the Free Software
/// Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
/// </copyright>

using System;

namespace SharpQuake.Framework.IO.BSP
{
    public enum Q2Contents : Int32
    {
        Solid = 0x00000001,
        Window = 0x00000002,
        Aux = 0x00000004,
        Lava = 0x00000008,
        Slime = 0x00000010,
        Water = 0x00000020,
        Mist = 0x00000040,
        AreaPortal = 0x00008000,
        PlayerClip = 0x00010000,
        MonsterClip = 0x00020000,
        Current0 = 0x00040000,
        Current90 = 0x00080000,
        Current180 = 0x00100000,
        Current270 = 0x00200000,
        CurrentUp = 0x00400000,
        CurrentDown = 0x00800000,
        Origin = 0x01000000,
        Monster = 0x02000000,
        DeadMonster = 0x04000000,
        Detail = 0x08000000,
        Translucent = 0x10000000,
        Ladder = 0x20000000
    }
}
