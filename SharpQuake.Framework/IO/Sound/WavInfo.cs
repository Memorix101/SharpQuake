using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpQuake.Framework.IO.Sound
{
	//[StructLayout(LayoutKind.Sequential)]
	public class WavInfo_t
	{
		public Int32 rate;
		public Int32 width;
		public Int32 channels;
		public Int32 loopstart;
		public Int32 samples;
		public Int32 dataofs;       // chunk starts this many bytes from file start
	} // wavinfo_t;
}
