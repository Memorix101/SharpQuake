using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpQuake.Framework
{
    public class MemoryLeaf : MemoryNodeBase
    {
        // leaf specific
        /// <summary>
        /// loadmodel->visdata
        /// Use in pair with visofs!
        /// </summary>
        public Byte[] compressed_vis; // byte*
        public Int32 visofs; // added by Uze
        public EFrag efrags;

        /// <summary>
        /// loadmodel->marksurfaces
        /// </summary>
        public MemorySurface[] marksurfaces;
        public Int32 firstmarksurface; // msurface_t	**firstmarksurface;
        public Int32 nummarksurfaces;
        //public int key;			// BSP sequence number for leaf's contents
        public Byte[] ambient_sound_level; // [NUM_AMBIENTS];

        public MemoryLeaf( )
        {
            this.ambient_sound_level = new Byte[AmbientDef.NUM_AMBIENTS];
        }
    } //mleaf_t;

}
