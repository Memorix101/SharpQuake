using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;

namespace SharpQuake.Framework
{
    public class Plane
    {
        public Vector3 normal;
        public Single dist;
        public Byte type;			// for texture axis selection and fast side tests
        public Byte signbits;		// signx + signy<<1 + signz<<1
    } //mplane_t;
}
