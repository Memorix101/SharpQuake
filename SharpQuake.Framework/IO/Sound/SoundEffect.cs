using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpQuake.Framework.IO.Sound
{
	public class SoundEffect_t
	{
		public String name; // char[MAX_QPATH];
		public CacheUser cache; // cache_user_t

		public void Clear( )
		{
			name = null;
			cache = null;
		}
	} // sfx_t;
}
