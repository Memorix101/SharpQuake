using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SharpQuake.Framework.IO.Alias
{
    [StructLayout( LayoutKind.Sequential, Pack = 1 )]
    public struct mdl_t
    {
        public Int32 ident;
        public Int32 version;
        public Vector3f scale;
        public Vector3f scale_origin;
        public Single boundingradius;
        public Vector3f eyeposition;
        public Int32 numskins;
        public Int32 skinwidth;
        public Int32 skinheight;
        public Int32 numverts;
        public Int32 numtris;
        public Int32 numframes;
        public SyncType synctype;
        public Int32 flags;
        public Single size;

        public static readonly Int32 SizeInBytes = Marshal.SizeOf( typeof( mdl_t ) );

        //static mdl_t()
        //{
        //    mdl_t.SizeInBytes = Marshal.SizeOf(typeof(mdl_t));
        //}
    } // mdl_t;
}
