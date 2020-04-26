using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpQuake.Framework.Definitions
{
	public static class ParticleDef
	{
		public const Int32 MAX_PARTICLES = 2048;

		// default max # of particles at one time
		public const Int32 ABSOLUTE_MIN_PARTICLES = 512;

		// no fewer than this no matter what's on the command line
		public const Int32 NUMVERTEXNORMALS = 162;

		public static Int32[] _Ramp1 = new Int32[] { 0x6f, 0x6d, 0x6b, 0x69, 0x67, 0x65, 0x63, 0x61 };

		public static Int32[] _Ramp2 = new Int32[] { 0x6f, 0x6e, 0x6d, 0x6c, 0x6b, 0x6a, 0x68, 0x66 };

		public static Int32[] _Ramp3 = new Int32[] { 0x6d, 0x6b, 6, 5, 4, 3 };

		public static Byte[,] _DotTexture = new Byte[8, 8]
		{
			{0,1,1,0,0,0,0,0},
			{1,1,1,1,0,0,0,0},
			{1,1,1,1,0,0,0,0},
			{0,1,1,0,0,0,0,0},
			{0,0,0,0,0,0,0,0},
			{0,0,0,0,0,0,0,0},
			{0,0,0,0,0,0,0,0},
			{0,0,0,0,0,0,0,0},
		};
	}
}
