using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpQuake.Framework.IO.Sound
{
	public class DMA_t
	{
		public Boolean gamealive;
		public Boolean soundalive;
		public Boolean splitbuffer;
		public Int32 channels;
		public Int32 samples;             // mono samples in buffer
		public Int32 submission_chunk;        // don't mix less than this #
		public Int32 samplepos;               // in mono samples
		public Int32 samplebits;
		public Int32 speed;
		public Byte[] buffer;
	} // dma_t;
}
