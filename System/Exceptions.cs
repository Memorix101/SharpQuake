using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpQuake
{
    public class QuakeException : Exception
    {
        public QuakeException( )
        {
        }

        public QuakeException( String message )
            : base( message )
        {
        }
    }

    public class EndGameException : QuakeException
    {
    }

    public class QuakeSystemError : QuakeException
    {
        public QuakeSystemError( String message )
            : base( message )
        {
        }
    }
}
