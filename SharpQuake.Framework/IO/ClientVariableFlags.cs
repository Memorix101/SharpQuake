using System;

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
