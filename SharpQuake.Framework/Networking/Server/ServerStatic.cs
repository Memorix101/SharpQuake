using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpQuake.Framework
{
    public class server_static_t
    {
        public Int32 maxclients;
        public Int32 maxclientslimit;
        public client_t[] clients; // [maxclients]
        public Int32 serverflags;     // episode completion information
        public Boolean changelevel_issued;	// cleared when at SV_SpawnServer
    }// server_static_t;
}
