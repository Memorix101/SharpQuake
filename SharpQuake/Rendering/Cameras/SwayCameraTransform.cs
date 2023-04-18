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

using SharpQuake.Framework.Mathematics;
using System;

namespace SharpQuake.Rendering.Cameras
{
    public class SwayCameraTransform : ICameraTransform
	{
		public Single? OverrideScale
        {
			get;
			set;
        }

		public dynamic CalculatedValue
		{
			get;
			set;
		}

		private readonly Host _host;

		public SwayCameraTransform( Host host )
		{
			_host = host;
		}

		private Vector3 Calculate( )
		{
			var time = _host.Client.cl.time;
			var v = new Vector3(
				( Single ) ( Math.Sin( time * _host.Cvars.IPitchCycle.Get<Single>( ) ) * _host.Cvars.IPitchLevel.Get<Single>( ) ),
				( Single ) ( Math.Sin( time * _host.Cvars.IYawCycle.Get<Single>( ) ) * _host.Cvars.IYawLevel.Get<Single>( ) ),
				( Single ) ( Math.Sin( time * _host.Cvars.IRollCycle.Get<Single>( ) ) * _host.Cvars.IRollLevel.Get<Single>( ) ) );
			return v;
		}

		/// <summary>
		/// V_AddIdle
		/// </summary>
		/// <remarks>
		/// Idle swaying
		/// </remarks>
		/// <returns></returns>
		public void Apply( )
		{
			var idleScale = OverrideScale.HasValue ? OverrideScale.Value : _host.Cvars.IdleScale.Get<Single>( );

			var v = Calculate( );

			_host.RenderContext.RefDef.viewangles += v * idleScale;
		}
    }
}