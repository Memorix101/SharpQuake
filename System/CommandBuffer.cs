using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpQuake.Framework;

namespace SharpQuake
{
    //Any number of commands can be added in a frame, from several different sources.
    //Most commands come from either keybindings or console line input, but remote
    //servers can also send across commands and entire text files can be execed.

    //The + command line options are also added to the command buffer.

    //The game starts with a Cbuf_AddText ("exec quake.rc\n"); Cbuf_Execute ();

    public class CommandBuffer // Cbuf
    {
        private StringBuilder _Buf;
        private Boolean _Wait;

        public Host Host
        {
            get;
            private set;
        }

        // Cbuf_Init()
        // allocates an initial text buffer that will grow as needed
        public void Initialise( Host host )
        {
            Host = host;
            // nothing to do
        }

        // Cbuf_AddText()
        // as new commands are generated from the console or keybindings,
        // the text is added to the end of the command buffer.
        public void AddText( String text )
        {
            if ( String.IsNullOrEmpty( text ) )
                return;

            var len = text.Length;
            if ( _Buf.Length + len > _Buf.Capacity )
            {
                Host.Console.Print( "Cbuf.AddText: overflow!\n" );
            }
            else
            {
                _Buf.Append( text );
            }
        }

        // Cbuf_InsertText()
        // when a command wants to issue other commands immediately, the text is
        // inserted at the beginning of the buffer, before any remaining unexecuted
        // commands.
        // Adds command text immediately after the current command
        // ???Adds a \n to the text
        // FIXME: actually change the command buffer to do less copying
        public void InsertText( String text )
        {
            _Buf.Insert( 0, text );
        }

        // Cbuf_Execute()
        // Pulls off \n terminated lines of text from the command buffer and sends
        // them through Cmd_ExecuteString.  Stops when the buffer is empty.
        // Normally called once per frame, but may be explicitly invoked.
        // Do not call inside a command function!
        public void Execute( )
        {
            while ( _Buf.Length > 0 )
            {
                var text = _Buf.ToString( );

                // find a \n or ; line break
                Int32 quotes = 0, i;
                for ( i = 0; i < text.Length; i++ )
                {
                    if ( text[i] == '"' )
                        quotes++;

                    if ( ( ( quotes & 1 ) == 0 ) && ( text[i] == ';' ) )
                        break;  // don't break if inside a quoted string

                    if ( text[i] == '\n' )
                        break;
                }

                var line = text.Substring( 0, i ).TrimEnd( '\n', ';' );

                // delete the text from the command buffer and move remaining commands down
                // this is necessary because commands (exec, alias) can insert data at the
                // beginning of the text buffer

                if ( i == _Buf.Length )
                {
                    _Buf.Length = 0;
                }
                else
                {
                    _Buf.Remove( 0, i + 1 );
                }

                // execute the command line
                if ( !String.IsNullOrEmpty( line ) )
                {
                    Host.Command.ExecuteString( line, CommandSource.src_command );

                    if ( _Wait )
                    {
                        // skip out while text still remains in buffer, leaving it
                        // for next frame
                        _Wait = false;
                        break;
                    }
                }
            }
        }

        // Cmd_Wait_f
        // Causes execution of the remainder of the command buffer to be delayed until
        // next frame.  This allows commands like:
        // bind g "impulse 5 ; +attack ; wait ; -attack ; impulse 2"
        public void Cmd_Wait_f( )
        {
            _Wait = true;
        }

        public CommandBuffer( )
        {
            _Buf = new StringBuilder( 8192 ); // space for commands and script files
        }
    }
}
