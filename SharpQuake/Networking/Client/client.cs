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

using SharpQuake.Framework;
using SharpQuake.Game.Client;
using SharpQuake.Game.World;
using System;

// client.h

namespace SharpQuake
{
	public partial class client
	{
		public client_static_t cls
		{
			get
			{
				return _Static;
			}
		}

		public client_state_t cl
		{
			get
			{
				return _State;
			}
		}

		public Entity[] Entities
		{
			get
			{
				return _Entities;
			}
		}

		/// <summary>
		/// cl_entities[cl.viewentity]
		/// Player model (visible when out of body)
		/// </summary>
		public Entity ViewEntity
		{
			get
			{
				return _Entities[_State.viewentity];
			}
		}

		/// <summary>
		/// cl.viewent
		/// Weapon model (only visible from inside body)
		/// </summary>
		public Entity ViewEnt
		{
			get
			{
				return _State.viewent;
			}
		}

		public Single ForwardSpeed
		{
			get
			{
				return Host.Cvars.ForwardSpeed.Get<Single>();
			}
		}

		public Boolean LookSpring
		{
			get
			{
				return Host.Cvars.LookSpring.Get<Boolean>();
			}
		}

		public Boolean LookStrafe
		{
			get
			{
				return Host.Cvars.LookStrafe.Get<Boolean>();
			}
		}

		public dlight_t[] DLights
		{
			get
			{
				return _DLights;
			}
		}

		public lightstyle_t[] LightStyle
		{
			get
			{
				return _LightStyle;
			}
		}

		public Entity[] VisEdicts
		{
			get
			{
				return _VisEdicts;
			}
		}

		public Single Sensitivity
		{
			get
			{
				return Host.Cvars.Sensitivity.Get<Single>();
			}
		}

		public Single MSide
		{
			get
			{
				return Host.Cvars.MSide.Get<Single>();
			}
		}

		public Single MYaw
		{
			get
			{
				return Host.Cvars.MYaw.Get<Single>();
			}
		}

		public Single MPitch
		{
			get
			{
				return Host.Cvars.MPitch.Get<Single>();
			}
		}

		public Single MForward
		{
			get
			{
				return Host.Cvars.MForward.Get<Single>();
			}
		}

		public String Name
		{
			get
			{
				return Host.Cvars.Name.Get<String>();
			}
		}

		public Single Color
		{
			get
			{
				return Host.Cvars.Color.Get<Single>();
			}
		}

		public Int32 NumVisEdicts;

		private client_static_t _Static;
		private client_state_t _State;

		public client( Host host )
		{
			Host = host;
			_Static = new client_static_t();
			_State = new client_state_t();
		}

		private EFrag[] _EFrags = new EFrag[ClientDef.MAX_EFRAGS]; // cl_efrags
		private Entity[] _Entities = new Entity[QDef.MAX_EDICTS]; // cl_entities
		private Entity[] _StaticEntities = new Entity[ClientDef.MAX_STATIC_ENTITIES]; // cl_static_entities
		private lightstyle_t[] _LightStyle = new lightstyle_t[QDef.MAX_LIGHTSTYLES]; // cl_lightstyle
		private dlight_t[] _DLights = new dlight_t[ClientDef.MAX_DLIGHTS]; // cl_dlights

		// cl_numvisedicts
		private Entity[] _VisEdicts = new Entity[ClientDef.MAX_VISEDICTS]; // cl_visedicts[MAX_VISEDICTS]
	}
}