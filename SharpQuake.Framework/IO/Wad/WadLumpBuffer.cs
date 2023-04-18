using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace SharpQuake.Framework.IO.WAD
{
	public class WadLumpBuffer
	{
		public Byte[] Pixels
		{
			get;
			set;
		}

		public Size Size
		{
			get;
			set;
		}

		public Byte[] Palette
		{
			get;
			set;
		}

		public WadLumpBuffer( Int32 width, Int32 height, Byte[] pixels, Byte[] palette )
		{
			Size = new Size( width, height );
			Pixels = pixels;
			Palette = palette;
		}
	}
}
