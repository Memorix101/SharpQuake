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

using string_t = System.Int32;

namespace SharpQuake.Framework
{
    public static class ProgramOperatorDef
    {
        public const string_t OFS_NULL = 0;
        public const string_t OFS_RETURN = 1;
        public const string_t OFS_PARM0 = 4;		// leave 3 ofs for each parm to hold vectors
        public const string_t OFS_PARM1 = 7;
        public const string_t OFS_PARM2 = 10;
        public const string_t OFS_PARM3 = 13;
        public const string_t OFS_PARM4 = 16;
        public const string_t OFS_PARM5 = 19;
        public const string_t OFS_PARM6 = 22;
        public const string_t OFS_PARM7 = 25;
        public const string_t RESERVED_OFS = 28;
    }
}
