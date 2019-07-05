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
    // Command execution takes a string, breaks it into tokens,
    // then searches for a command or variable that matches the first token.
    //
    // Commands can come from three sources, but the handler functions may choose
    // to dissallow the action or forward it to a remote server if the source is
    // not apropriate.

    public class Command
    {
        public CommandSource Source
        {
            get
            {
                return _Source;
            }
        }

        public Int32 Argc
        {
            get
            {
                return _Argc;
            }
        }

        // char	*Cmd_Args (void);
        public String Args
        {
            get
            {
                return _Args;
            }
        }

        internal Boolean Wait
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

        private CommandSource _Source; // extern	cmd_source_t	cmd_source;
        private Dictionary<String, String> _Aliases;
        private Dictionary<String, XCommand> _Functions;
        private Int32 _Argc;
        private String[] _Argv;// char	*cmd_argv[MAX_ARGS];
        private String _Args;// char* cmd_args = NULL;
        private Boolean _Wait; // qboolean cmd_wait;

        // Instances
        public Host Host
        {
            get;
            private set;
        }

        public void Initialise( )
        {
            //
            // register our commands
            //
            Add( "stuffcmds", StuffCmds_f );
            Add( "exec", Exec_f );
            Add( "echo", Echo_f );
            Add( "alias", Alias_f );
            Add( "cmd", ForwardToServer );
            Add( "wait", Host.CommandBuffer.Cmd_Wait_f ); // todo: move to Cbuf class?
        }

        // Cmd_AddCommand()
        // called by the init functions of other parts of the program to
        // register commands and functions to call for them.
        public void Add( String name, XCommand function )
        {
            // ??? because hunk allocation would get stomped
            if( Host != null && Host.IsInitialised )
                Utilities.Error( "Cmd.Add after host initialized!" );

            // fail if the command is a variable name
            if( CVar.Exists( name ) )
            {
                Host.Console.Print( "Cmd.Add: {0} already defined as a var!\n", name );
                return;
            }

            // fail if the command already exists
            if( Exists( name ) )
            {
                Host.Console.Print( "Cmd.Add: {0} already defined!\n", name );
                return;
            }

            _Functions.Add( name, function );
        }

        // Cmd_CompleteCommand()
        // attempts to match a partial command for automatic command line completion
        // returns NULL if nothing fits
        public String[] Complete( String partial )
        {
            if( String.IsNullOrEmpty( partial ) )
                return null;

            var result = new List<String>();
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
        public String Argv( Int32 arg )
        {
            if( arg < 0 || arg >= _Argc )
                return String.Empty;

            return _Argv[arg];
        }

        // Cmd_Exists
        public Boolean Exists( String name )
        {
            return ( Find( name ) != null );
        }

        // void Cmd_TokenizeString (char *text);
        // Takes a null terminated string.  Does not need to be /n terminated.
        // breaks the string up into arg tokens.
        // Parses the given string into command line tokens.
        public void TokenizeString( String text )
        {
            // clear the args from the last string
            _Argc = 0;
            _Args = null;
            _Argv = null;

            var argv = new List<String>( MAX_ARGS );
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
        public void ExecuteString( String text, CommandSource src )
        {
            _Source = src;

            TokenizeString( text );

            // execute the command line
            if( _Argc <= 0 )
                return;		// no tokens

            // check functions
            var handler = Find( _Argv[0] ); // must search with comparison like Q_strcasecmp()
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
                    Host.CommandBuffer.InsertText( alias );
                }
                else
                {
                    // check cvars
                    if( !CVar.Command() )
                        Host.Console.Print( "Unknown command \"{0}\"\n", _Argv[0] );
                }
            }
        }

        // void	Cmd_ForwardToServer (void);
        // adds the current command line as a clc_stringcmd to the client message.
        // things like godmode, noclip, etc, are commands directed to the server,
        // so when they are typed in at the console, they will need to be forwarded.
        //
        // Sends the entire command line over to the server
        public void ForwardToServer()
        {
            if( Host.Client.cls.state != cactive_t.ca_connected )
            {
                Host.Console.Print( "Can't \"{0}\", not connected\n", Host.Command.Argv( 0 ) );
                return;
            }

            if( Host.Client.cls.demoplayback )
                return;		// not really connected

            var writer = Host.Client.cls.message;
            writer.WriteByte( ProtocolDef.clc_stringcmd );
            if( !Host.Command.Argv( 0 ).Equals( "cmd" ) )
            {
                writer.Print( Host.Command.Argv( 0 ) + " " );
            }
            if( Host.Command.Argc > 1 )
            {
                writer.Print( Host.Command.Args );
            }
            else
            {
                writer.Print( "\n" );
            }
        }

        public String JoinArgv()
        {
            return String.Join( " ", _Argv );
        }

        private XCommand Find( String name )
        {
            XCommand result;
            _Functions.TryGetValue( name, out result );
            return result;
        }

        private String FindAlias( String name )
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
        private void StuffCmds_f()
        {
            if( _Argc != 1 )
            {
                Host.Console.Print( "stuffcmds : execute command line parameters\n" );
                return;
            }

            // build the combined string to parse from
            var sb = new StringBuilder( 1024 );
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
                Host.CommandBuffer.InsertText( sb.ToString() );
            }
        }

        // Cmd_Exec_f
        private void Exec_f()
        {
            if( _Argc != 2 )
            {
                Host.Console.Print( "exec <filename> : execute a script file\n" );
                return;
            }

            var bytes = FileSystem.LoadFile( _Argv[1] );
            if( bytes == null )
            {
                Host.Console.Print( "couldn't exec {0}\n", _Argv[1] );
                return;
            }
            var script = Encoding.ASCII.GetString( bytes );
            Host.Console.Print( "execing {0}\n", _Argv[1] );
            Host.CommandBuffer.InsertText( script );
        }

        // Cmd_Echo_f
        // Just prints the rest of the line to the console
        private void Echo_f()
        {
            for( var i = 1; i < _Argc; i++ )
            {
                Host.Console.Print( "{0} ", _Argv[i] );
            }
            Host.Console.Print( "\n" );
        }

        // Cmd_Alias_f
        // Creates a new command that executes a command string (possibly ; seperated)
        private void Alias_f()
        {
            if( _Argc == 1 )
            {
                Host.Console.Print( "Current alias commands:\n" );
                foreach( var alias in _Aliases )
                {
                    Host.Console.Print( "{0} : {1}\n", alias.Key, alias.Value );
                }
                return;
            }

            var name = _Argv[1];
            if( name.Length >= MAX_ALIAS_NAME )
            {
                Host.Console.Print( "Alias name is too long\n" );
                return;
            }

            // copy the rest of the command line
            var sb = new StringBuilder( 1024 );
            for( var i = 2; i < _Argc; i++ )
            {
                sb.Append( _Argv[i] );
                if( i + 1 < _Argc )
                    sb.Append( " " );
            }
            sb.AppendLine();
            _Aliases[name] = sb.ToString();
        }

        public Command( Host host )
        {
            Host = host;

            _Aliases = new Dictionary<String, String>();
            _Functions = new Dictionary<String, XCommand>();
        }

        // Temporary workaround until code is refactored furher
        public void SetupWrapper()
        {
            CommandWrapper.OnAdd += ( name, cmd ) =>
            {
                Add( name, cmd );
            };
        }
    }

    // cmd_source_t;
}
