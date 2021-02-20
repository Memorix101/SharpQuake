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
    public enum Q3Contents : Int64
    {
        Solid = 0x00000001, // solid (opaque and transparent)
        Lava = 	0x00000008, // lava
        Slime = 0x00000010, // slime
        Water = 0x00000020,// water
        Fog = 	0x00000040, // unused?
        AreaPortal = 0x00008000, // areaportal (separates areas)
        PlayerClip = 0x00010000, // block players
        MonsterClip = 0x00020000,// block monsters
        Teleporter = 0x00040000, // hint for Q3's bots
        JumpPad = 0x00080000, // hint for Q3's bots
        ClusterPortal = 0x00100000, // hint for Q3's bots
        DoNotEnter = 0x00200000, // hint for Q3's bots
        BotClip = 0x00400000, // hint for Q3's bots
        Origin = 0x01000000, // used by origin brushes to indicate origin of bmodel (removed by map compiler)
        Body = 	0x02000000, // used by bbox entities (should never be on a brush)
        Corpse = 0x04000000, // used by dead bodies (SOLID_CORPSE in darkplaces)
        Detail = 0x08000000, // brushes that do not split the bsp tree (decorations)
        Structural = 0x10000000, // brushes that split the bsp tree
        Translucent = 0x20000000, // leaves surfaces that are inside for rendering
        Trigger = 0x40000000, // used by trigger entities
        NoDrop = 0x80000000 // remove items that fall into this brush
    }
}
