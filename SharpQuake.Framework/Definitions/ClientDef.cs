using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpQuake.Framework
{
    public static class ClientDef
    {
        public const Int32 SIGNONS = 4;	// signon messages to receive before connected
        public const Int32 MAX_DLIGHTS = 32;
        public const Int32 MAX_BEAMS = 24;
        public const Int32 MAX_EFRAGS = 640;
        public const Int32 MAX_MAPSTRING = 2048;
        public const Int32 MAX_DEMOS = 8;
        public const Int32 MAX_DEMONAME = 16;
        public const Int32 MAX_VISEDICTS = 256;
        public const Int32 MAX_TEMP_ENTITIES = 64;	// lightning bolts, etc
        public const Int32 MAX_STATIC_ENTITIES = 128;          // torches, etc
    }
}
