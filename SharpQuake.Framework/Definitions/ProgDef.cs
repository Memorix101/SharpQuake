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

using string_t = System.Int32;

namespace SharpQuake.Framework
{
    public delegate void builtin_t( );

    /// <summary>
    /// PR_functions
    /// </summary>
    public static partial class ProgramDef
    {
        public const string_t DEF_SAVEGLOBAL = ( 1 << 15 );
        public const string_t MAX_PARMS = 8;
        public const string_t MAX_ENT_LEAFS = 16;

        public const string_t PROG_VERSION = 6;
        public const string_t PROGHEADER_CRC = 5927;

        // Used to link the framework and game dynamic value
        public static Int32 EdictSize;
    }
}
