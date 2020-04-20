using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using SharpQuake.Framework.Mathematics;

namespace SharpQuake.Framework.IO.Sound
{
	// !!! if this is changed, it much be changed in asm_i386.h too !!!
	[StructLayout( LayoutKind.Sequential )]
	public class Channel_t
	{
		public SoundEffect_t sfx;           // sfx number
		public Int32 leftvol;       // 0-255 volume
		public Int32 rightvol;      // 0-255 volume
		public Int32 end;           // end time in global paintsamples
		public Int32 pos;           // sample position in sfx
		public Int32 looping;       // where to loop, -1 = no looping
		public Int32 entnum;            // to allow overriding a specific sound
		public Int32 entchannel;        //
		public Vector3 origin;          // origin of sound effect
		public Single dist_mult;        // distance multiplier (attenuation/clipK)
		public Int32 master_vol;        // 0-255 master volume

		public void Clear( )
		{
			sfx = null;
			leftvol = 0;
			rightvol = 0;
			end = 0;
			pos = 0;
			looping = 0;
			entnum = 0;
			entchannel = 0;
			origin = Vector3.Zero;
			dist_mult = 0;
			master_vol = 0;
		}
	} // channel_t;
}
