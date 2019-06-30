using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpQuake.Framework
{
    public class VidDef
    {
        public Byte[] colormap;		// 256 * VID_GRADES size
        public Int32 fullbright;		// index of first fullbright color
        public Int32 rowbytes; // unsigned	// may be > width if displayed in a window
        public Int32 width; // unsigned
        public Int32 height; // unsigned
        public Single aspect;		// width / height -- < 0 is taller than wide
        public Int32 numpages;
        public System.Boolean recalc_refdef;	// if true, recalc vid-based stuff
        public Int32 conwidth; // unsigned
        public Int32 conheight; // unsigned
        public Int32 maxwarpwidth;
        public Int32 maxwarpheight;
    } // viddef_t;
}
