using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpQuake.Framework
{
    // entity effects
    public static class EntityEffects
    {
        public static Int32 EF_BRIGHTFIELD = 1;
        public static Int32 EF_MUZZLEFLASH = 2;
        public static Int32 EF_BRIGHTLIGHT = 4;
        public static Int32 EF_DIMLIGHT = 8;
#if QUAKE2
        public static int EF_DARKLIGHT = 16;
        public static int EF_DARKFIELD = 32;
        public static int EF_LIGHT = 64;
        public static int EF_NODRAW = 128;
#endif
    }
}
