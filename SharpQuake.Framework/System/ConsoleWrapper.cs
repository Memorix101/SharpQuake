using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpQuake.Framework
{
    public static class ConsoleWrapper
    {
        public static Action<String> OnPrint;
        public static Action<String, Object[]> OnPrint2;
        public static Action<String, Object[]> OnDPrint;

        private static void Print( String txt )
        {
            OnPrint?.Invoke( txt );
        }

        public static void Print( String fmt, params Object[] args )
        {
            OnPrint2?.Invoke( fmt, args );
        }

        public static void DPrint( String fmt, params Object[] args )
        {
            OnDPrint?.Invoke( fmt, args );
        }
    }
}
