using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpQuake.Framework
{
    public static class NetworkDef
    {
        public const Int32 NET_PROTOCOL_VERSION = 3;

        public const Int32 HOSTCACHESIZE = 8;

        public const Int32 NET_NAMELEN = 64;

        public const Int32 NET_MAXMESSAGE = 8192;

        public const Int32 NET_HEADERSIZE = 2 * sizeof( UInt32 );

        public const Int32 NET_DATAGRAMSIZE = QDef.MAX_DATAGRAM + NET_HEADERSIZE;

    }
}
