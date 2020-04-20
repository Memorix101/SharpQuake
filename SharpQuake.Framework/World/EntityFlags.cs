using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpQuake.Framework.World
{
	public enum EntityFlags : Int32
	{
		Rocket = 1,           // leave a trail
		Grenade = 2,          // leave a trail
		Gib = 4,				 // leave a trail
		Rotate = 8,           // rotate (bonus items)
		Tracer = 16,          // green split trail
		ZomGib = 32,          // small blood trail
		Tracer2 = 64,         // orange split trail + rotate
		Tracer3 = 128		 // purple trail
	}
}
