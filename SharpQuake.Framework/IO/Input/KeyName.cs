using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpQuake.Framework
{
    public struct KeyName
    {
        public String name;
        public Int32 keynum;

        public KeyName( String name, Int32 keynum )
        {
            this.name = name;
            this.keynum = keynum;
        }
    } //keyname_t;
}
