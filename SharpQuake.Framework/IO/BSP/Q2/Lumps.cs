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
    public enum Q2Lumps : Int32
    {
        Entities = 0,
        Planes = 1,
        Vertices = 2,
        Visibility = 3,
        Nodes = 4,
        TextureInfo = 5,
        Faces = 6,
        Lighting = 7,
        Leaves = 8,
        LeafFaces = 9,
        LeafBrushes = 10,
        Edges = 11,
        SurfaceEdges = 12,
        Models = 13,
        Brushes = 14,
        BrushSides = 15,
        Pop = 16,
        Areas = 17,
        AreaPortals = 18,
        Count = 19
    }
}
