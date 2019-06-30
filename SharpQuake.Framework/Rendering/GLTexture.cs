using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpQuake.Framework
{
    public class GLTexture
    {
        public Int32 texnum;
        public String owner;
        public String identifier; //char	identifier[64];
        public Int32 width, height;
        public Boolean mipmap;
    } //gltexture_t;
}
