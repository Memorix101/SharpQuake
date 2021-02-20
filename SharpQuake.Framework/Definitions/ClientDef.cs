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

namespace SharpQuake.Framework
{
    public static class ClientDef
    {
        public const Int32 SIGNONS = 4;	// signon messages to receive before connected
        public const Int32 MAX_DLIGHTS = 32;
        public const Int32 MAX_BEAMS = 24;
        public const Int32 MAX_EFRAGS = 640;
        public const Int32 MAX_MAPSTRING = 2048;
        public const Int32 MAX_DEMOS = 8;
        public const Int32 MAX_DEMONAME = 16;
        public const Int32 MAX_VISEDICTS = 256;
        public const Int32 MAX_TEMP_ENTITIES = 64;	// lightning bolts, etc
        public const Int32 MAX_STATIC_ENTITIES = 128;          // torches, etc
    }
}
