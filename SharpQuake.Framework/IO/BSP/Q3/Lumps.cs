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
/// 
using System;

namespace SharpQuake.Framework.IO.BSP
{
    public enum Q3Lumps : Int32
    {
        Entities = 0, // entities to spawn (used by server and client)
        Textures = 1, // textures used (used by faces)
        Planes = 2, // planes used (used by bsp nodes)
        Nodes = 3, // bsp nodes (used by bsp nodes, bsp leafs, rendering, collisions)
        Leaves = 4, // bsp leafs (used by bsp nodes)
        LeafFaces = 5, // array of ints indexing faces (used by leafs)
        LeafBrushes = 6, // array of ints indexing brushes (used by leafs)
        Models = 7, // models (used by rendering, collisions)
        Brushes = 8, // brushes (used by effects, collisions)
        BrushSides = 9, // brush faces (used by brushes)
        Vertices = 10, // mesh vertices (used by faces)
        Triangles = 11,// mesh triangles (used by faces)
        Effects = 12, // fog (used by faces)
        Faces = 13, // surfaces (used by leafs)
        LightMaps = 14, // lightmap textures (used by faces)
        LightGrid = 15, // lighting as a voxel grid (used by rendering)
        PVS = 16, // potentially visible set; bit[clusters][clusters] (used by rendering)
        Count = 17
    }
}
