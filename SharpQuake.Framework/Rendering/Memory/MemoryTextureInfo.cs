using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;

namespace SharpQuake.Framework
{
    public class MemoryTextureInfo
    {
        public Vector4[] vecs; //public float[][] vecs; //[2][4];
        public Single mipadjust;
        public Texture texture;
        public Int32 flags;

        public MemoryTextureInfo( )
        {
            vecs = new Vector4[2];// float[2][] { new float[4], new float[4] };
        }
    } //mtexinfo_t;
}
