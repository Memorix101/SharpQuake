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
using System.Text;
using System.Threading.Tasks;

namespace SharpQuake.Framework
{
    public static class ModelDef
    { 
        // modelgen.h
        public const Int32 ALIAS_VERSION = 6;

        public const Int32 IDPOLYHEADER = ( ( 'O' << 24 ) + ( 'P' << 16 ) + ( 'D' << 8 ) + 'I' ); // little-endian "IDPO"

        // spritegn.h
        public const Int32 SPRITE_VERSION = 1;

        public const Int32 IDSPRITEHEADER = ( ( 'P' << 24 ) + ( 'S' << 16 ) + ( 'D' << 8 ) + 'I' ); // little-endian "IDSP"

        public const Int32 VERTEXSIZE = 7;
        public const Int32 MAX_SKINS = 32;
        public const Int32 MAXALIASVERTS = 1024; //1024
        public const Int32 MAXALIASFRAMES = 256;
        public const Int32 MAXALIASTRIS = 2048;
        public const Int32 MAX_MOD_KNOWN = 512;

        public const Int32 MAX_LBM_HEIGHT = 480;

        public const Int32 ANIM_CYCLE = 2;

        public static Single ALIAS_BASE_SIZE_RATIO = ( 1.0f / 11.0f );
    }
}
