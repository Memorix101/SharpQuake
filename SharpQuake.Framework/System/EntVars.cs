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

using string_t = System.Int32;
using func_t = System.Int32;

namespace SharpQuake.Framework
{
    [StructLayout( LayoutKind.Sequential, Pack = 1 )]
    public struct EntVars
    {
        public Single modelindex;
        public Vector3f absmin;
        public Vector3f absmax;
        public Single ltime;
        public Single movetype;
        public Single solid;
        public Vector3f origin;
        public Vector3f oldorigin;
        public Vector3f velocity;
        public Vector3f angles;
        public Vector3f avelocity;
        public Vector3f punchangle;
        public string_t classname;
        public string_t model;
        public Single frame;
        public Single skin;
        public Single effects;
        public Vector3f mins;
        public Vector3f maxs;
        public Vector3f size;
        public func_t touch;
        public func_t use;
        public func_t think;
        public func_t blocked;
        public Single nextthink;
        public string_t groundentity;
        public Single health;
        public Single frags;
        public Single weapon;
        public string_t weaponmodel;
        public Single weaponframe;
        public Single currentammo;
        public Single ammo_shells;
        public Single ammo_nails;
        public Single ammo_rockets;
        public Single ammo_cells;
        public Single items;
        public Single takedamage;
        public string_t chain;
        public Single deadflag;
        public Vector3f view_ofs;
        public Single button0;
        public Single button1;
        public Single button2;
        public Single impulse;
        public Single fixangle;
        public Vector3f v_angle;
        public Single idealpitch;
        public string_t netname;
        public string_t enemy;
        public Single flags;
        public Single colormap;
        public Single team;
        public Single max_health;
        public Single teleport_time;
        public Single armortype;
        public Single armorvalue;
        public Single waterlevel;
        public Single watertype;
        public Single ideal_yaw;
        public Single yaw_speed;
        public string_t aiment;
        public string_t goalentity;
        public Single spawnflags;
        public string_t target;
        public string_t targetname;
        public Single dmg_take;
        public Single dmg_save;
        public string_t dmg_inflictor;
        public string_t owner;
        public Vector3f movedir;
        public string_t message;
        public Single sounds;
        public string_t noise;
        public string_t noise1;
        public string_t noise2;
        public string_t noise3;

        public static string_t SizeInBytes = Marshal.SizeOf( typeof( EntVars ) );
    } // entvars_t
}
