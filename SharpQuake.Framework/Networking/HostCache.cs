using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SharpQuake.Framework
{
    public class hostcache_t
    {
        public String name; //[16];
        public String map; //[16];
        public String cname; //[32];
        public Int32 users;
        public Int32 maxusers;
        public Int32 driver;
        public Int32 ldriver;
        public EndPoint addr; // qsockaddr ?????
    }
}
