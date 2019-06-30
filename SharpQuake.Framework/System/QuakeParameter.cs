using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpQuake.Framework
{
    public static class QuakeParameter
    {
        public static String globalbasedir;
        public static String globalgameid;
    } // qparam
    // entity_state_t;

    // the host system specifies the base of the directory tree, the
    // command line parms passed to the program, and the amount of memory
    // available for the program to use
    public class QuakeParameters
    {
        public String basedir;
        public String cachedir;		// for development over ISDN lines
        public String[] argv;

        public QuakeParameters( )
        {
            this.basedir = String.Empty;
            this.cachedir = String.Empty;
        }
    }// quakeparms_t;
}
