using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpQuake.Framework
{
    public static class Timer
    {
        private static Stopwatch _StopWatch;

        /// <summary>
        /// Sys_FloatTime
        /// </summary>
        public static Double GetFloatTime( )
        {
            if ( _StopWatch == null )
            {
                _StopWatch = new Stopwatch( );
                _StopWatch.Start( );
            }
            return _StopWatch.Elapsed.TotalSeconds;
        }
    }
}
