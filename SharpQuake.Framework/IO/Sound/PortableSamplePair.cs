using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SharpQuake.Framework.IO.Sound
{
	// !!! if this is changed, it much be changed in asm_i386.h too !!!
	[StructLayout( LayoutKind.Sequential, Pack = 1 )]
	public struct PortableSamplePair_t
	{
		public Int32 left;
		public Int32 right;

		public override String ToString( )
		{
			return String.Format( "{{{0}, {1}}}", left, right );
		}
	}

}
