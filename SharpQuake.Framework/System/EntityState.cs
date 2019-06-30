using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpQuake.Framework
{
    public struct EntityState
    {
        public static readonly EntityState Empty = new EntityState( );
        public Vector3f origin;
        public Vector3f angles;
        public Int32 modelindex;
        public Int32 frame;
        public Int32 colormap;
        public Int32 skin;
        public Int32 effects;
    }
}
