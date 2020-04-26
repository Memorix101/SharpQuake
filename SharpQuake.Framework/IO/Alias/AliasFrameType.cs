using System.Runtime.InteropServices;

namespace SharpQuake.Framework.IO.Alias
{
	public enum aliasframetype_t
    {
        ALIAS_SINGLE = 0,
        ALIAS_GROUP
    } // aliasframetype_t;

    [StructLayout( LayoutKind.Sequential, Pack = 1 )]
    public struct daliasframetype_t
    {
        public aliasframetype_t type;
    } // daliasframetype_t;
}
