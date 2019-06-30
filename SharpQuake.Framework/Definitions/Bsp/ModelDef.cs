using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpQuake.Framework
{
    public static class ModelDef
    { 
        // modelgen.h
        public const Int32 ALIAS_VERSION = 6;

        public const Int32 IDPOLYHEADER = ( ( 'O' << 24 ) + ( 'P' << 16 ) + ( 'D' << 8 ) + 'I' ); // little-endian "IDPO"

        // spritegn.h
        public const Int32 SPRITE_VERSION = 1;

        public const Int32 IDSPRITEHEADER = ( ( 'P' << 24 ) + ( 'S' << 16 ) + ( 'D' << 8 ) + 'I' ); // little-endian "IDSP"

        public const Int32 VERTEXSIZE = 7;
        public const Int32 MAX_SKINS = 32;
        public const Int32 MAXALIASVERTS = 1024; //1024
        public const Int32 MAXALIASFRAMES = 256;
        public const Int32 MAXALIASTRIS = 2048;
        public const Int32 MAX_MOD_KNOWN = 512;

        public const Int32 MAX_LBM_HEIGHT = 480;

        public const Int32 ANIM_CYCLE = 2;

        public static Single ALIAS_BASE_SIZE_RATIO = ( 1.0f / 11.0f );
    }
}
