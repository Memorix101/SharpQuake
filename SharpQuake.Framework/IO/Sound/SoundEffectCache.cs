using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpQuake.Framework.IO.Sound
{
	// !!! if this is changed, it much be changed in asm_i386.h too !!!
	public class SoundEffectCache_t
	{
		public Int32 length;
		public Int32 loopstart;
		public Int32 speed;
		public Int32 width;
		public Int32 stereo;
		public Byte[] data; // [1];		// variable sized
	} // sfxcache_t;
}
