using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpQuake.Framework
{
    public static class CommandWrapper
    {
        public static Action<String, XCommand> OnAdd;

        public static void Add( String name, XCommand cmd )
        {
            OnAdd?.Invoke( name, cmd );
        }
    }
}
