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
    public enum Q1Lumps : Int32
    {
        Entities = 0,
        Planes = 1,
        Textures = 2,
        Vertices = 3,
        Visibility = 4,
        Nodes = 5,
        TextureInfo = 6,
        Faces = 7,
        Lighting = 8,
        ClipNodes = 9,
        Leaves = 10,
        MarkSurfaces = 11,
        Edges = 12,
        SurfaceEdges = 13,
        Models = 14,
        Count = 15
    }
}
