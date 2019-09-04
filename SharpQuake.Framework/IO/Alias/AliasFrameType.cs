using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

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
