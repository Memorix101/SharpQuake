using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpQuake.Framework.IO
{
    [Flags]
    public enum ClientVariableFlags
    {
        None = 0,
        Archive = 1,
        Server = 2
    }
}
