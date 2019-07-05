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
using func_t = System.Int32;
using System.Runtime.InteropServices;

namespace SharpQuake.Framework
{
    [StructLayout( LayoutKind.Explicit, Size = 12, Pack = 1 )]
    public unsafe struct EVal
    {
        [FieldOffset( 0 )]
        public string_t _string;

        [FieldOffset( 0 )]
        public Single _float;

        [FieldOffset( 0 )]
        public fixed Single vector[3];

        [FieldOffset( 0 )]
        public string_t function;

        [FieldOffset( 0 )]
        public string_t _int;

        [FieldOffset( 0 )]
        public string_t edict;
    } // eval_t
}
