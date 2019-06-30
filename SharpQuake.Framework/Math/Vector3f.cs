using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SharpQuake.Framework
{
    [StructLayout( LayoutKind.Sequential, Pack = 1 )]
    public struct Vector3f
    {
        public Single x, y, z;

        public Boolean IsEmpty
        {
            get
            {
                return ( this.x == 0 ) && ( this.y == 0 ) && ( this.z == 0 );
            }
        }
    }
}
