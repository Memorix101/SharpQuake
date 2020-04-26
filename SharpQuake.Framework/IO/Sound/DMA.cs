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

namespace SharpQuake.Framework.IO.Sound
{
	public class DMA_t
	{
		public Boolean gamealive;
		public Boolean soundalive;
		public Boolean splitbuffer;
		public Int32 channels;
		public Int32 samples;             // mono samples in buffer
		public Int32 submission_chunk;        // don't mix less than this #
		public Int32 samplepos;               // in mono samples
		public Int32 samplebits;
		public Int32 speed;
		public Byte[] buffer;
	} // dma_t;
}
