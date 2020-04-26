using System.Runtime.InteropServices;

namespace SharpQuake.Framework.IO.Sprite
{
	[StructLayout( LayoutKind.Sequential, Pack = 1 )]
    public struct dspriteframetype_t
    {
        public spriteframetype_t type;
    } // dspriteframetype_t;
}
