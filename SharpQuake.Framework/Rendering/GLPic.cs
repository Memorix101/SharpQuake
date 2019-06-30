using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpQuake.Framework
{
    public class GLPic
    {
        public Int32 width, height;
        public Int32 texnum;
        public Single sl, tl, sh, th;

        public GLPic( )
        {
            sl = 0;
            sh = 1;
            tl = 0;
            th = 1;
        }
    } //glpic_t;
}
