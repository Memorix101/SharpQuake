using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpQuake.Framework
{
    public static class NetworkWrapper
    {
        public static Func<Int32, INetLanDriver> OnGetLanDriver;

        public static INetLanDriver GetLanDriver( Int32 driverIndex )
        {
            return OnGetLanDriver?.Invoke( driverIndex );
        }
    }
}
