using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpQuake.Framework
{
    public class msurface_t
    {
        public Int32 visframe;		// should be drawn when node is crossed

        public Plane plane;
        public Int32 flags;

        public Int32 firstedge;	// look up in model->surfedges[], negative numbers
        public Int32 numedges;	// are backwards edges

        public Int16[] texturemins; //[2];
        public Int16[] extents; //[2];

        public Int32 light_s, light_t;	// gl lightmap coordinates

        public glpoly_t polys;			// multiple if warped
        public msurface_t texturechain;

        public mtexinfo_t texinfo;

        // lighting info
        public Int32 dlightframe;
        public Int32 dlightbits;

        public Int32 lightmaptexturenum;
        public Byte[] styles; //[MAXLIGHTMAPS];
        public Int32[] cached_light; //[MAXLIGHTMAPS];	// values currently used in lightmap
        public Boolean cached_dlight;				// true if dynamic light in cache
        /// <summary>
        /// Former "samples" field. Use in pair with sampleofs field!!!
        /// </summary>
        public Byte[] sample_base;		// [numstyles*surfsize]
        public Int32 sampleofs; // added by Uze. In original Quake samples = loadmodel->lightdata + offset;
        // now samples = loadmodel->lightdata;

        public msurface_t( )
        {
            texturemins = new Int16[2];
            extents = new Int16[2];
            styles = new Byte[BspDef.MAXLIGHTMAPS];
            cached_light = new Int32[BspDef.MAXLIGHTMAPS];
            // samples is allocated when needed
        }
    } //msurface_t;
}
