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
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using SharpQuake.Framework.Mathematics;

namespace SharpQuake.Framework.IO.Sound
{
	// !!! if this is changed, it much be changed in asm_i386.h too !!!
	[StructLayout( LayoutKind.Sequential )]
	public class Channel_t
	{
		public SoundEffect_t sfx;           // sfx number
		public Int32 leftvol;       // 0-255 volume
		public Int32 rightvol;      // 0-255 volume
		public Int32 end;           // end time in global paintsamples
		public Int32 pos;           // sample position in sfx
		public Int32 looping;       // where to loop, -1 = no looping
		public Int32 entnum;            // to allow overriding a specific sound
		public Int32 entchannel;        //
		public Vector3 origin;          // origin of sound effect
		public Single dist_mult;        // distance multiplier (attenuation/clipK)
		public Int32 master_vol;        // 0-255 master volume

		public void Clear( )
		{
			sfx = null;
			leftvol = 0;
			rightvol = 0;
			end = 0;
			pos = 0;
			looping = 0;
			entnum = 0;
			entchannel = 0;
			origin = Vector3.Zero;
			dist_mult = 0;
			master_vol = 0;
		}
	} // channel_t;
}
