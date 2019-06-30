using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpQuake.Framework
{
    public static class ContentsDef
    {
        public const System.Int32 CONTENTS_EMPTY = -1;
        public const System.Int32 CONTENTS_SOLID = -2;
        public const System.Int32 CONTENTS_WATER = -3;
        public const System.Int32 CONTENTS_SLIME = -4;
        public const System.Int32 CONTENTS_LAVA = -5;
        public const System.Int32 CONTENTS_SKY = -6;
        public const System.Int32 CONTENTS_ORIGIN = -7;		// removed at csg time
        public const System.Int32 CONTENTS_CLIP = -8;		// changed to contents_solid

        public const System.Int32 CONTENTS_CURRENT_0 = -9;
        public const System.Int32 CONTENTS_CURRENT_90 = -10;
        public const System.Int32 CONTENTS_CURRENT_180 = -11;
        public const System.Int32 CONTENTS_CURRENT_270 = -12;
        public const System.Int32 CONTENTS_CURRENT_UP = -13;
        public const System.Int32 CONTENTS_CURRENT_DOWN = -14;
    }
}
