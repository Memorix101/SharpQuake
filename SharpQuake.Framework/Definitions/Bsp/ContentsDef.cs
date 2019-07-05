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
    public static class ContentsDef
    {
        public const System.Int32 CONTENTS_EMPTY = -1;
        public const System.Int32 CONTENTS_SOLID = -2;
        public const System.Int32 CONTENTS_WATER = -3;
        public const System.Int32 CONTENTS_SLIME = -4;
        public const System.Int32 CONTENTS_LAVA = -5;
        public const System.Int32 CONTENTS_SKY = -6;
        public const System.Int32 CONTENTS_ORIGIN = -7;		// removed at csg time
        public const System.Int32 CONTENTS_CLIP = -8;		// changed to contents_solid

        public const System.Int32 CONTENTS_CURRENT_0 = -9;
        public const System.Int32 CONTENTS_CURRENT_90 = -10;
        public const System.Int32 CONTENTS_CURRENT_180 = -11;
        public const System.Int32 CONTENTS_CURRENT_270 = -12;
        public const System.Int32 CONTENTS_CURRENT_UP = -13;
        public const System.Int32 CONTENTS_CURRENT_DOWN = -14;
    }
}
