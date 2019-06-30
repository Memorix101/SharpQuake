using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpQuake.Framework
{
    public static class SurfaceDef
    {
        public const Int32 SURF_PLANEBACK = 2;
        public const Int32 SURF_DRAWSKY = 4;
        public const Int32 SURF_DRAWSPRITE = 8;
        public const Int32 SURF_DRAWTURB = 0x10;
        public const Int32 SURF_DRAWTILED = 0x20;
        public const Int32 SURF_DRAWBACKGROUND = 0x40;
        public const Int32 SURF_UNDERWATER = 0x80;
    }
}
