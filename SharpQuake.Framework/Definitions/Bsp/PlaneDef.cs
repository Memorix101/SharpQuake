using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpQuake.Framework
{
    public static class PlaneDef
    {
        // 0-2 are axial planes
        public const System.Int32 PLANE_X = 0;

        public const System.Int32 PLANE_Y = 1;
        public const System.Int32 PLANE_Z = 2;

        // 3-5 are non-axial planes snapped to the nearest
        public const System.Int32 PLANE_ANYX = 3;

        public const System.Int32 PLANE_ANYY = 4;
        public const System.Int32 PLANE_ANYZ = 5;
    }
}
