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
using System.Text;

namespace SharpQuake.Rendering.Cameras
{
    public class BobCameraTransform : ICameraTransform
	{
		public dynamic CalculatedValue
		{
			get;
			set;
		}

		private readonly Host _host;

		public BobCameraTransform( Host host )
		{
			_host = host;
		}

		/// <summary>
		/// V_CalcBob
		/// </summary>
		/// <returns></returns>
		private Single Calculate( )
		{
			var cl = _host.Client.cl;
			var bobCycle = _host.Cvars.ClBobCycle.Get<Single>( );
			var bobUp = _host.Cvars.ClBobUp.Get<Single>( );
			var cycle = ( Single ) ( cl.time - ( Int32 ) ( cl.time / bobCycle ) * bobCycle );
			cycle /= bobCycle;
			if ( cycle < bobUp )
				cycle = ( Single ) Math.PI * cycle / bobUp;
			else
				cycle = ( Single ) ( Math.PI + Math.PI * ( cycle - bobUp ) / ( 1.0 - bobUp ) );

			// bob is proportional to velocity in the xy plane
			// (don't count Z, or jumping messes it up)
			var tmp = cl.velocity.Xy;
			Double bob = tmp.Length * _host.Cvars.ClBob.Get<Single>( );
			bob = bob * 0.3 + bob * 0.7 * Math.Sin( cycle );
			if ( bob > 4 )
				bob = 4;
			else if ( bob < -7 )
				bob = -7;
			return ( Single ) bob;
		}

		public void Apply( )
		{
			var ent = _host.Client.ViewEntity;
			var rdef = _host.RenderContext.RefDef;
			var cl = _host.Client.cl;

			var bob = Calculate( );

			// refresh position
			rdef.vieworg = ent.origin;
			rdef.vieworg.Z += cl.viewheight + bob;
			CalculatedValue = bob;
		}
    }
}
