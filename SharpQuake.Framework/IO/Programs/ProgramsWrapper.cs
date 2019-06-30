using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpQuake.Framework
{
    public static class ProgramsWrapper
    {
        public static Func<Int32, String> OnGetString;

        public static String GetString( Int32 strId )
        {
            return OnGetString?.Invoke( strId );
        }
    }
}
