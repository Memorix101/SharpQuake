using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;

namespace SharpQuake.Framework
{
    public struct usercmd_t
    {
        public Vector3 viewangles;

        // intended velocities
        public Single forwardmove;

        public Single sidemove;
        public Single upmove;

        public void Clear( )
        {
            this.viewangles = Vector3.Zero;
            this.forwardmove = 0;
            this.sidemove = 0;
            this.upmove = 0;
        }
    }
}
