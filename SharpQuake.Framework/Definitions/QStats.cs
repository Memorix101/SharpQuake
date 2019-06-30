using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpQuake.Framework
{
    public static class QStats
    {
        //
        // stats are integers communicated to the client by the server
        //
        public static Int32 MAX_CL_STATS = 32;

        public static Int32 STAT_HEALTH = 0;
        public static Int32 STAT_FRAGS = 1;
        public static Int32 STAT_WEAPON = 2;
        public static Int32 STAT_AMMO = 3;
        public static Int32 STAT_ARMOR = 4;
        public static Int32 STAT_WEAPONFRAME = 5;
        public static Int32 STAT_SHELLS = 6;
        public static Int32 STAT_NAILS = 7;
        public static Int32 STAT_ROCKETS = 8;
        public static Int32 STAT_CELLS = 9;
        public static Int32 STAT_ACTIVEWEAPON = 10;
        public static Int32 STAT_TOTALSECRETS = 11;
        public static Int32 STAT_TOTALMONSTERS = 12;
        public static Int32 STAT_SECRETS = 13;		// bumped on client side by svc_foundsecret
        public static Int32 STAT_MONSTERS = 14;		// bumped by svc_killedmonster
    }
}
