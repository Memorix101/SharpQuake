using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpQuake.Framework
{
    public static class Tokeniser
    {
        private static String _Token; // com_token

        public static String Token
        {
            get
            {
                return _Token;
            }
        }
        
        /// <summary>
        /// COM_Parse
        /// Parse a token out of a string
        /// </summary>
        public static String Parse( String data )
        {
            _Token = String.Empty;

            if ( String.IsNullOrEmpty( data ) )
                return null;

            // skip whitespace
            var i = 0;

            while ( i < data.Length )
            {
                while ( i < data.Length )
                {
                    if ( data[i] > ' ' )
                        break;

                    i++;
                }

                if ( i >= data.Length )
                    return null;

                // skip // comments
                if ( ( data[i] == '/' ) && ( i + 1 < data.Length ) && ( data[i + 1] == '/' ) )
                {
                    while ( i < data.Length && data[i] != '\n' )
                        i++;
                }
                else
                    break;
            }

            if ( i >= data.Length )
                return null;

            var i0 = i;

            // handle quoted strings specially
            if ( data[i] == '\"' )
            {
                i++;
                i0 = i;

                while ( i < data.Length && data[i] != '\"' )
                    i++;

                if ( i == data.Length )
                {
                    _Token = data.Substring( i0, i - i0 );
                    return null;
                }
                else
                {
                    _Token = data.Substring( i0, i - i0 );
                    return ( i + 1 < data.Length ? data.Substring( i + 1 ) : null );
                }
            }

            // parse single characters
            var c = data[i];

            if ( c == '{' || c == '}' || c == ')' || c == '(' || c == '\'' || c == ':' )
            {
                _Token = data.Substring( i, 1 );
                return ( i + 1 < data.Length ? data.Substring( i + 1 ) : null );
            }

            // parse a regular word
            while ( i < data.Length )
            {
                c = data[i];

                if ( c <= 32 || c == '{' || c == '}' || c == ')' || c == '(' || c == '\'' || c == ':' )
                {
                    i--;
                    break;
                }

                i++;
            }

            if ( i == data.Length )
            {
                _Token = data.Substring( i0, i - i0 );
                return null;
            }

            _Token = data.Substring( i0, i - i0 + 1 );
            return ( i + 1 < data.Length ? data.Substring( i + 1 ) : null );
        }

    }
}
