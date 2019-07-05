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
    public static class QStatsDef
    {
        //
        // stats are integers communicated to the client by the server
        //
        public static Int32 MAX_CL_STATS = 32;

        public static Int32 STAT_HEALTH = 0;
        public static Int32 STAT_FRAGS = 1;
        public static Int32 STAT_WEAPON = 2;
        public static Int32 STAT_AMMO = 3;
        public static Int32 STAT_ARMOR = 4;
        public static Int32 STAT_WEAPONFRAME = 5;
        public static Int32 STAT_SHELLS = 6;
        public static Int32 STAT_NAILS = 7;
        public static Int32 STAT_ROCKETS = 8;
        public static Int32 STAT_CELLS = 9;
        public static Int32 STAT_ACTIVEWEAPON = 10;
        public static Int32 STAT_TOTALSECRETS = 11;
        public static Int32 STAT_TOTALMONSTERS = 12;
        public static Int32 STAT_SECRETS = 13;		// bumped on client side by svc_foundsecret
        public static Int32 STAT_MONSTERS = 14;		// bumped by svc_killedmonster
    }
}
