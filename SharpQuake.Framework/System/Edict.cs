/// <copyright>
///
/// SharpQuakeEvolved changes by optimus-code, 2019
/// 
/// Based on SharpQuake Rewritten in C# by Yury Kiselev, 2010.
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
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

using string_t = System.Int32;

namespace SharpQuake.Framework
{
    /// <summary>
    /// On-disk edict
    /// </summary>
    [StructLayout( LayoutKind.Sequential, Pack = 1 )]
    public struct Edict
    {
        public Boolean free;
        public string_t dummy1, dummy2;	 // former link_t area

        public string_t num_leafs;

        [MarshalAs( UnmanagedType.ByValArray, SizeConst = ProgramDef.MAX_ENT_LEAFS )]
        public Int16[] leafnums; // [MAX_ENT_LEAFS];

        public EntityState baseline;

        public Single freetime;			// sv.time when the object was freed
        public EntVars v;					// C exported fields from progs
        // other fields from progs come immediately after

        public static string_t SizeInBytes = Marshal.SizeOf( typeof( Edict ) );
    } // dedict_t
}
