/// <copyright>
///
/// Rewritten in C# by Yury Kiselev, 2010.
///
/// Copyright (C) 1996-1997 Id Software, Inc.
///
/// This program is free software; you can redistribute it and/or
/// modify it under the terms of the GNU General Public License
/// as published by the Free Software Foundation; either version 2
/// of the License, or (at your option) any later version.
///
/// This program is distributed in the hope that it will be useful,
/// but WITHOUT ANY WARRANTY; without even the implied warranty of
/// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
///
/// See the GNU General Public License for more details.
///
/// You should have received a copy of the GNU General Public License
/// along with this program; if not, write to the Free Software
/// Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
/// </copyright>

using System;
using System.Collections.Generic;
using System.Text;
using SharpQuake.Framework;

// cmd.h -- Command buffer and command execution

namespace SharpQuake
{
    internal delegate void xcommand_t(); // typedef void (*xcommand_t) (void);

    // Command execution takes a string, breaks it into tokens,
    // then searches for a command or variable that matches the first token.
    //
    // Commands can come from three sources, but the handler functions may choose
    // to dissallow the action or forward it to a remote server if the source is
    // not apropriate.

    internal enum cmd_source_t
    {
        src_client,     // came in over a net connection as a clc_stringcmd

        // host_client will be valid during this state.
        src_command		// from the command buffer
    }

    internal static class Command
    {
        public static cmd_source_t Source
        {
            get
            {
                return _Source;
            }
        }

        public static Int32 Argc
        {
            get
            {
                return _Argc;
            }
        }

        // char	*Cmd_Args (void);
        public static String Args
        {
            get
            {
                return _Args;
            }
        }

        internal static Boolean Wait
        {
            get
            {
                return _Wait;
            }
            set
            {
                _Wait = value;
            }
        }

        private const Int32 MAX_ALIAS_NAME = 32;
        private const Int32 MAX_ARGS = 80;

        private static cmd_source_t _Source; // extern	cmd_source_t	cmd_source;
        private static Dictionary<String, String> _Aliases;
        private static Dictionary<String, xcommand_t> _Functions;
        private static Int32 _Argc;
        private static String[] _Argv;// char	*cmd_argv[MAX_ARGS];
        private static String _Args;// char* cmd_args = NULL;
        private static Boolean _Wait; // qboolean cmd_wait;

        public static void Init()
        {
            //
            // register our commands
            //
            Add( "stuffcmds", StuffCmds_f );
            Add( "exec", Exec_f );
            Add( "echo", Echo_f );
            Add( "alias", Alias_f );
            Add( "cmd", ForwardToServer );
            Add( "wait", Cbuf.Cmd_Wait_f ); // todo: move to Cbuf class?
        }

        // Cmd_AddCommand()
        // called by the init functions of other parts of the program to
        // register commands and functions to call for them.
        public static void Add( String name, xcommand_t function )
        {
            // ??? because hunk allocation would get stomped
            if( host.IsInitialized )
                Utilities.Error( "Cmd.Add after host initialized!" );

            // fail if the command is a variable name
            if( CVar.Exists( name ) )
            {
                Con.Print( "Cmd.Add: {0} already defined as a var!\n", name );
                return;
            }

            // fail if the command already exists
            if( Exists( name ) )
            {
                Con.Print( "Cmd.Add: {0} already defined!\n", name );
                return;
            }

            _Functions.Add( name, function );
        }

        // Cmd_CompleteCommand()
        // attempts to match a partial command for automatic command line completion
        // returns NULL if nothing fits
        public static String[] Complete( String partial )
        {
            if( String.IsNullOrEmpty( partial ) )
                return null;

            List<String> result = new List<String>();
            foreach( var cmd in _Functions.Keys )
            {
                if( cmd.StartsWith( partial ) )
                    result.Add( cmd );
            }
            return ( result.Count > 0 ? result.ToArray() : null );
        }

        // Cmd_Argv ()
        // will return an empty string, not a NULL
        // if arg > argc, so string operations are allways safe.
        public static String Argv( Int32 arg )
        {
            if( arg < 0 || arg >= _Argc )
                return String.Empty;

            return _Argv[arg];
        }

        // Cmd_Exists
        public static Boolean Exists( String name )
        {
            return ( Find( name ) != null );
        }

        // void Cmd_TokenizeString (char *text);
        // Takes a null terminated string.  Does not need to be /n terminated.
        // breaks the string up into arg tokens.
        // Parses the given string into command line tokens.
        public static void TokenizeString( String text )
        {
            // clear the args from the last string
            _Argc = 0;
            _Args = null;
            _Argv = null;

            List<String> argv = new List<String>( MAX_ARGS );
            while( !String.IsNullOrEmpty( text ) )
            {
                if( _Argc == 1 )
                    _Args = text;

                text = Tokeniser.Parse( text );

                if( String.IsNullOrEmpty( Tokeniser.Token ) )
                    break;

                if( _Argc < MAX_ARGS )
                {
                    argv.Add( Tokeniser.Token );
                    _Argc++;
                }
            }
            _Argv = argv.ToArray();
        }

        // void	Cmd_ExecuteString (char *text, cmd_source_t src);
        // Parses a single line of text into arguments and tries to execute it.
        // The text can come from the command buffer, a remote client, or stdin.
        //
        // A complete command line has been parsed, so try to execute it
        // FIXME: lookupnoadd the token to speed search?
        public static void ExecuteString( String text, cmd_source_t src )
        {
            _Source = src;

            TokenizeString( text );

            // execute the command line
            if( _Argc <= 0 )
                return;		// no tokens

            // check functions
            xcommand_t handler = Find( _Argv[0] ); // must search with comparison like Q_strcasecmp()
            if( handler != null )
            {
                handler();
            }
            else
            {
                // check alias
                var alias = FindAlias( _Argv[0] ); // must search with compare func like Q_strcasecmp
                if( !String.IsNullOrEmpty( alias ) )
                {
                    Cbuf.InsertText( alias );
                }
                else
                {
                    // check cvars
                    if( !CVar.Command() )
                        Con.Print( "Unknown command \"{0}\"\n", _Argv[0] );
                }
            }
        }

        // void	Cmd_ForwardToServer (void);
        // adds the current command line as a clc_stringcmd to the client message.
        // things like godmode, noclip, etc, are commands directed to the server,
        // so when they are typed in at the console, they will need to be forwarded.
        //
        // Sends the entire command line over to the server
        public static void ForwardToServer()
        {
            if( client.cls.state != cactive_t.ca_connected )
            {
                Con.Print( "Can't \"{0}\", not connected\n", Command.Argv( 0 ) );
                return;
            }

            if( client.cls.demoplayback )
                return;		// not really connected

            MessageWriter writer = client.cls.message;
            writer.WriteByte( protocol.clc_stringcmd );
            if( !Command.Argv( 0 ).Equals( "cmd" ) )
            {
                writer.Print( Command.Argv( 0 ) + " " );
            }
            if( Command.Argc > 1 )
            {
                writer.Print( Command.Args );
            }
            else
            {
                writer.Print( "\n" );
            }
        }

        public static String JoinArgv()
        {
            return String.Join( " ", _Argv );
        }

        private static xcommand_t Find( String name )
        {
            xcommand_t result;
            _Functions.TryGetValue( name, out result );
            return result;
        }

        private static String FindAlias( String name )
        {
            String result;
            _Aliases.TryGetValue( name, out result );
            return result;
        }

        /// <summary>
        /// Cmd_StuffCmds_f
        /// Adds command line parameters as script statements
        /// Commands lead with a +, and continue until a - or another +
        /// quake +prog jctest.qp +cmd amlev1
        /// quake -nosound +cmd amlev1
        /// </summary>
        private static void StuffCmds_f()
        {
            if( _Argc != 1 )
            {
                Con.Print( "stuffcmds : execute command line parameters\n" );
                return;
            }

            // build the combined string to parse from
            StringBuilder sb = new StringBuilder( 1024 );
            for( var i = 1; i < _Argc; i++ )
            {
                if( !String.IsNullOrEmpty( _Argv[i] ) )
                {
                    sb.Append( _Argv[i] );
                    if( i + 1 < _Argc )
                        sb.Append( " " );
                }
            }

            // pull out the commands
            var text = sb.ToString();
            sb.Length = 0;

            for( var i = 0; i < text.Length; i++ )
            {
                if( text[i] == '+' )
                {
                    i++;

                    var j = i;
                    while( ( j < text.Length ) && ( text[j] != '+' ) && ( text[j] != '-' ) )
                    {
                        j++;
                    }

                    sb.Append( text.Substring( i, j - i + 1 ) );
                    sb.AppendLine();
                    i = j - 1;
                }
            }

            if( sb.Length > 0 )
            {
                Cbuf.InsertText( sb.ToString() );
            }
        }

        // Cmd_Exec_f
        private static void Exec_f()
        {
            if( _Argc != 2 )
            {
                Con.Print( "exec <filename> : execute a script file\n" );
                return;
            }

            Byte[] bytes = FileSystem.LoadFile( _Argv[1] );
            if( bytes == null )
            {
                Con.Print( "couldn't exec {0}\n", _Argv[1] );
                return;
            }
            var script = Encoding.ASCII.GetString( bytes );
            Con.Print( "execing {0}\n", _Argv[1] );
            Cbuf.InsertText( script );
        }

        // Cmd_Echo_f
        // Just prints the rest of the line to the console
        private static void Echo_f()
        {
            for( var i = 1; i < _Argc; i++ )
            {
                Con.Print( "{0} ", _Argv[i] );
            }
            Con.Print( "\n" );
        }

        // Cmd_Alias_f
        // Creates a new command that executes a command string (possibly ; seperated)
        private static void Alias_f()
        {
            if( _Argc == 1 )
            {
                Con.Print( "Current alias commands:\n" );
                foreach( KeyValuePair<String, String> alias in _Aliases )
                {
                    Con.Print( "{0} : {1}\n", alias.Key, alias.Value );
                }
                return;
            }

            var name = _Argv[1];
            if( name.Length >= MAX_ALIAS_NAME )
            {
                Con.Print( "Alias name is too long\n" );
                return;
            }

            // copy the rest of the command line
            StringBuilder sb = new StringBuilder( 1024 );
            for( var i = 2; i < _Argc; i++ )
            {
                sb.Append( _Argv[i] );
                if( i + 1 < _Argc )
                    sb.Append( " " );
            }
            sb.AppendLine();
            _Aliases[name] = sb.ToString();
        }

        static Command()
        {
            _Aliases = new Dictionary<String, String>();
            _Functions = new Dictionary<String, xcommand_t>();
        }
    }

    // cmd_source_t;

    //Any number of commands can be added in a frame, from several different sources.
    //Most commands come from either keybindings or console line input, but remote
    //servers can also send across commands and entire text files can be execed.

    //The + command line options are also added to the command buffer.

    //The game starts with a Cbuf_AddText ("exec quake.rc\n"); Cbuf_Execute ();

    internal static class Cbuf
    {
        private static StringBuilder _Buf;
        private static Boolean _Wait;

        // Cbuf_Init()
        // allocates an initial text buffer that will grow as needed
        public static void Init()
        {
            // nothing to do
        }

        // Cbuf_AddText()
        // as new commands are generated from the console or keybindings,
        // the text is added to the end of the command buffer.
        public static void AddText( String text )
        {
            if( String.IsNullOrEmpty( text ) )
                return;

            var len = text.Length;
            if( _Buf.Length + len > _Buf.Capacity )
            {
                Con.Print( "Cbuf.AddText: overflow!\n" );
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
        public static void InsertText( String text )
        {
            _Buf.Insert( 0, text );
        }

        // Cbuf_Execute()
        // Pulls off \n terminated lines of text from the command buffer and sends
        // them through Cmd_ExecuteString.  Stops when the buffer is empty.
        // Normally called once per frame, but may be explicitly invoked.
        // Do not call inside a command function!
        public static void Execute()
        {
            while( _Buf.Length > 0 )
            {
                var text = _Buf.ToString();

                // find a \n or ; line break
                Int32 quotes = 0, i;
                for( i = 0; i < text.Length; i++ )
                {
                    if( text[i] == '"' )
                        quotes++;

                    if( ( ( quotes & 1 ) == 0 ) && ( text[i] == ';' ) )
                        break;  // don't break if inside a quoted string

                    if( text[i] == '\n' )
                        break;
                }

                var line = text.Substring( 0, i ).TrimEnd( '\n', ';' );

                // delete the text from the command buffer and move remaining commands down
                // this is necessary because commands (exec, alias) can insert data at the
                // beginning of the text buffer

                if( i == _Buf.Length )
                {
                    _Buf.Length = 0;
                }
                else
                {
                    _Buf.Remove( 0, i + 1 );
                }

                // execute the command line
                if( !String.IsNullOrEmpty( line ) )
                {
                    Command.ExecuteString( line, cmd_source_t.src_command );

                    if( _Wait )
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
        public static void Cmd_Wait_f()
        {
            _Wait = true;
        }

        static Cbuf()
        {
            _Buf = new StringBuilder( 8192 ); // space for commands and script files
        }
    }
}
