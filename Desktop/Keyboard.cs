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
using System.IO;
using System.Text;
using SharpQuake.Framework;

// keys.h
// keys.c

// key up events are sent even if in console mode

namespace SharpQuake
{
    /// <summary>
    /// Key_functions
    /// </summary>
    public class Keyboard
    {
        public KeyDestination Destination
        {
            get
            {
                return _KeyDest;
            }
            set
            {
                _KeyDest = value;
            }
        }

        public Boolean TeamMessage
        {
            get
            {
                return _TeamMessage;
            }
            set
            {
                _TeamMessage = value;
            }
        }

        public Char[][] Lines
        {
            get
            {
                return _Lines;
            }
        }

        public Int32 EditLine
        {
            get
            {
                return _EditLine;
            }
        }

        public String ChatBuffer
        {
            get
            {
                return _ChatBuffer.ToString( );
            }
        }

        public Int32 LastPress
        {
            get
            {
                return _LastPress;
            }
        }

        public String[] Bindings
        {
            get
            {
                return _Bindings;
            }
        }

        // Instances
        public Host Host
        {
            get;
            private set;
        }

        public Int32 LinePos;

        public Int32 KeyCount;

        private Char[][] _Lines = new Char[32][];//, MAXCMDLINE]; // char	key_lines[32][MAXCMDLINE];

        // key_linepos
        private Boolean _ShiftDown; // = false;

        private Int32 _LastPress; // key_lastpress

        private Int32 _EditLine; // edit_line=0;
        private Int32 _HistoryLine; // history_line=0;

        private KeyDestination _KeyDest; // key_dest

        // key_count			// incremented every key event

        private String[] _Bindings = new String[256]; // char	*keybindings[256];
        private Boolean[] _ConsoleKeys = new Boolean[256]; // consolekeys[256]	// if true, can't be rebound while in console
        private Boolean[] _MenuBound = new Boolean[256]; // menubound[256]	// if true, can't be rebound while in menu
        private Int32[] _KeyShift = new Int32[256]; // keyshift[256]		// key to map to if shift held down in console
        private Int32[] _Repeats = new Int32[256]; // key_repeats[256]	// if > 1, it is autorepeating
        private Boolean[] _KeyDown = new Boolean[256];

        private StringBuilder _ChatBuffer = new StringBuilder( 32 ); // chat_buffer
        private Boolean _TeamMessage; // qboolean team_message = false;

        public Keyboard( Host host )
        {
            Host = host;
        }

        // Key_Event (int key, qboolean down)
        //
        // Called by the system between frames for both key up and key down events
        // Should NOT be called during an interrupt!
        public void Event( Int32 key, Boolean down )
        {
            _KeyDown[key] = down;

            if ( !down )
                _Repeats[key] = 0;

            _LastPress = key;
            KeyCount++;
            if ( KeyCount <= 0 )
                return;     // just catching keys for Con_NotifyBox

            // update auto-repeat status
            if ( down )
            {
                _Repeats[key]++;
                if ( key != KeysDef.K_BACKSPACE && key != KeysDef.K_PAUSE && key != KeysDef.K_PGUP && key != KeysDef.K_PGDN && _Repeats[key] > 1 )
                {
                    return; // ignore most autorepeats
                }

                if ( key >= 200 && String.IsNullOrEmpty( _Bindings[key] ) )
                    Host.Console.Print( "{0} is unbound, hit F4 to set.\n", KeynumToString( key ) );
            }

            if ( key == KeysDef.K_SHIFT )
                _ShiftDown = down;

            //
            // handle escape specialy, so the user can never unbind it
            //
            if ( key == KeysDef.K_ESCAPE )
            {
                if ( !down )
                    return;

                switch ( _KeyDest )
                {
                    case KeyDestination.key_message:
                        KeyMessage( key );
                        break;

                    case KeyDestination.key_menu:
                        Host.Menu.KeyDown( key );
                        break;

                    case KeyDestination.key_game:
                    case KeyDestination.key_console:
                        Host.Menu.ToggleMenu_f( );
                        break;

                    default:
                        Utilities.Error( "Bad key_dest" );
                        break;
                }
                return;
            }

            //
            // key up events only generate commands if the game key binding is
            // a button command (leading + sign).  These will occur even in console mode,
            // to keep the character from continuing an action started before a console
            // switch.  Button commands include the keynum as a parameter, so multiple
            // downs can be matched with ups
            //
            if ( !down )
            {
                var kb = _Bindings[key];

                if ( !String.IsNullOrEmpty( kb ) && kb.StartsWith( "+" ) )
                {
                    Host.CommandBuffer.AddText( String.Format( "-{0} {1}\n", kb.Substring( 1 ), key ) );
                }

                if ( _KeyShift[key] != key )
                {
                    kb = _Bindings[_KeyShift[key]];
                    if ( !String.IsNullOrEmpty( kb ) && kb.StartsWith( "+" ) )
                        Host.CommandBuffer.AddText( String.Format( "-{0} {1}\n", kb.Substring( 1 ), key ) );
                }
                return;
            }

            //
            // during demo playback, most keys bring up the main menu
            //
            if ( Host.Client.cls.demoplayback && down && _ConsoleKeys[key] && _KeyDest == KeyDestination.key_game )
            {
                Host.Menu.ToggleMenu_f( );
                return;
            }

            //
            // if not a consolekey, send to the interpreter no matter what mode is
            //
            if ( ( _KeyDest == KeyDestination.key_menu && _MenuBound[key] ) ||
                ( _KeyDest == KeyDestination.key_console && !_ConsoleKeys[key] ) ||
                ( _KeyDest == KeyDestination.key_game && ( !Host.Console.ForcedUp || !_ConsoleKeys[key] ) ) )
            {
                var kb = _Bindings[key];
                if ( !String.IsNullOrEmpty( kb ) )
                {
                    if ( kb.StartsWith( "+" ) )
                    {
                        // button commands add keynum as a parm
                        Host.CommandBuffer.AddText( String.Format( "{0} {1}\n", kb, key ) );
                    }
                    else
                    {
                        Host.CommandBuffer.AddText( kb );
                        Host.CommandBuffer.AddText( "\n" );
                    }
                }
                return;
            }

            if ( !down )
                return;     // other systems only care about key down events

            if ( _ShiftDown )
            {
                key = _KeyShift[key];
            }

            switch ( _KeyDest )
            {
                case KeyDestination.key_message:
                    KeyMessage( key );
                    break;

                case KeyDestination.key_menu:
                    Host.Menu.KeyDown( key );
                    break;

                case KeyDestination.key_game:
                case KeyDestination.key_console:
                    KeyConsole( key );
                    break;

                default:
                    Utilities.Error( "Bad key_dest" );
                    break;
            }
        }

        // Key_Init (void);
        public void Initialise( )
        {
            for ( var i = 0; i < 32; i++ )
            {
                _Lines[i] = new Char[KeysDef.MAXCMDLINE];
                _Lines[i][0] = ']'; // key_lines[i][0] = ']'; key_lines[i][1] = 0;
            }

            LinePos = 1;

            //
            // init ascii characters in console mode
            //
            for ( var i = 32; i < 128; i++ )
                _ConsoleKeys[i] = true;

            _ConsoleKeys[KeysDef.K_ENTER] = true;
            _ConsoleKeys[KeysDef.K_TAB] = true;
            _ConsoleKeys[KeysDef.K_LEFTARROW] = true;
            _ConsoleKeys[KeysDef.K_RIGHTARROW] = true;
            _ConsoleKeys[KeysDef.K_UPARROW] = true;
            _ConsoleKeys[KeysDef.K_DOWNARROW] = true;
            _ConsoleKeys[KeysDef.K_BACKSPACE] = true;
            _ConsoleKeys[KeysDef.K_PGUP] = true;
            _ConsoleKeys[KeysDef.K_PGDN] = true;
            _ConsoleKeys[KeysDef.K_SHIFT] = true;
            _ConsoleKeys[KeysDef.K_MWHEELUP] = true;
            _ConsoleKeys[KeysDef.K_MWHEELDOWN] = true;
            _ConsoleKeys['`'] = false;
            _ConsoleKeys['~'] = false;

            for ( var i = 0; i < 256; i++ )
                _KeyShift[i] = i;
            for ( Int32 i = 'a'; i <= 'z'; i++ )
                _KeyShift[i] = i - 'a' + 'A';
            _KeyShift['1'] = '!';
            _KeyShift['2'] = '@';
            _KeyShift['3'] = '#';
            _KeyShift['4'] = '$';
            _KeyShift['5'] = '%';
            _KeyShift['6'] = '^';
            _KeyShift['7'] = '&';
            _KeyShift['8'] = '*';
            _KeyShift['9'] = '(';
            _KeyShift['0'] = ')';
            _KeyShift['-'] = '_';
            _KeyShift['='] = '+';
            _KeyShift[','] = '<';
            _KeyShift['.'] = '>';
            _KeyShift['/'] = '?';
            _KeyShift[';'] = ':';
            _KeyShift['\''] = '"';
            _KeyShift['['] = '{';
            _KeyShift[']'] = '}';
            _KeyShift['`'] = '~';
            _KeyShift['\\'] = '|';

            _MenuBound[KeysDef.K_ESCAPE] = true;
            for ( var i = 0; i < 12; i++ )
                _MenuBound[KeysDef.K_F1 + i] = true;

            //
            // register our functions
            //
            Host.Command.Add( "bind", Bind_f );
            Host.Command.Add( "unbind", Unbind_f );
            Host.Command.Add( "unbindall", UnbindAll_f );
        }

        /// <summary>
        /// Key_WriteBindings
        /// </summary>
        public void WriteBindings( Stream dest )
        {
            var sb = new StringBuilder( 4096 );
            for ( var i = 0; i < 256; i++ )
            {
                if ( !String.IsNullOrEmpty( _Bindings[i] ) )
                {
                    sb.Append( "bind \"" );
                    sb.Append( KeynumToString( i ) );
                    sb.Append( "\" \"" );
                    sb.Append( _Bindings[i] );
                    sb.AppendLine( "\"" );
                }
            }
            var buf = Encoding.ASCII.GetBytes( sb.ToString( ) );
            dest.Write( buf, 0, buf.Length );
        }

        /// <summary>
        /// Key_SetBinding
        /// </summary>
        public void SetBinding( Int32 keynum, String binding )
        {
            if ( keynum != -1 )
            {
                _Bindings[keynum] = binding;
            }
        }

        // Key_ClearStates (void)
        public void ClearStates( )
        {
            for ( var i = 0; i < 256; i++ )
            {
                _KeyDown[i] = false;
                _Repeats[i] = 0;
            }
        }

        // Key_KeynumToString
        //
        // Returns a string (either a single ascii char, or a K_* name) for the
        // given keynum.
        // FIXME: handle quote special (general escape sequence?)
        public String KeynumToString( Int32 keynum )
        {
            if ( keynum == -1 )
                return "<KEY NOT FOUND>";

            if ( keynum > 32 && keynum < 127 )
            {
                // printable ascii
                return ( ( Char ) keynum ).ToString( );
            }

            foreach ( var kn in KeysDef.KeyNames )
            {
                if ( kn.keynum == keynum )
                    return kn.name;
            }
            return "<UNKNOWN KEYNUM>";
        }

        // Key_StringToKeynum
        //
        // Returns a key number to be used to index keybindings[] by looking at
        // the given string.  Single ascii characters return themselves, while
        // the K_* names are matched up.
        private Int32 StringToKeynum( String str )
        {
            if ( String.IsNullOrEmpty( str ) )
                return -1;
            if ( str.Length == 1 )
                return str[0];

            foreach ( var keyname in KeysDef.KeyNames )
            {
                if ( Utilities.SameText( keyname.name, str ) )
                    return keyname.keynum;
            }
            return -1;
        }

        //Key_Unbind_f
        private void Unbind_f( )
        {
            if ( Host.Command.Argc != 2 )
            {
                Host.Console.Print( "unbind <key> : remove commands from a key\n" );
                return;
            }

            var b = StringToKeynum( Host.Command.Argv( 1 ) );
            if ( b == -1 )
            {
                Host.Console.Print( "\"{0}\" isn't a valid key\n", Host.Command.Argv( 1 ) );
                return;
            }

            SetBinding( b, null );
        }

        // Key_Unbindall_f
        private void UnbindAll_f( )
        {
            for ( var i = 0; i < 256; i++ )
                if ( !String.IsNullOrEmpty( _Bindings[i] ) )
                    SetBinding( i, null );
        }

        //Key_Bind_f
        private void Bind_f( )
        {
            var c = Host.Command.Argc;
            if ( c != 2 && c != 3 )
            {
                Host.Console.Print( "bind <key> [command] : attach a command to a key\n" );
                return;
            }

            var b = StringToKeynum( Host.Command.Argv( 1 ) );
            if ( b == -1 )
            {
                Host.Console.Print( "\"{0}\" isn't a valid key\n", Host.Command.Argv( 1 ) );
                return;
            }

            if ( c == 2 )
            {
                if ( !String.IsNullOrEmpty( _Bindings[b] ) )// keybindings[b])
                    Host.Console.Print( "\"{0}\" = \"{1}\"\n", Host.Command.Argv( 1 ), _Bindings[b] );
                else
                    Host.Console.Print( "\"{0}\" is not bound\n", Host.Command.Argv( 1 ) );
                return;
            }

            // copy the rest of the command line
            // start out with a null string
            var sb = new StringBuilder( 1024 );
            for ( var i = 2; i < c; i++ )
            {
                if ( i > 2 )
                    sb.Append( " " );
                sb.Append( Host.Command.Argv( i ) );
            }

            SetBinding( b, sb.ToString( ) );
        }

        // Key_Message (int key)
        private void KeyMessage( Int32 key )
        {
            if ( key == KeysDef.K_ENTER )
            {
                if ( _TeamMessage )
                    Host.CommandBuffer.AddText( "say_team \"" );
                else
                    Host.CommandBuffer.AddText( "say \"" );

                Host.CommandBuffer.AddText( _ChatBuffer.ToString( ) );
                Host.CommandBuffer.AddText( "\"\n" );

                Destination = KeyDestination.key_game;
                _ChatBuffer.Length = 0;
                return;
            }

            if ( key == KeysDef.K_ESCAPE )
            {
                Destination = KeyDestination.key_game;
                _ChatBuffer.Length = 0;
                return;
            }

            if ( key < 32 || key > 127 )
                return;	// non printable

            if ( key == KeysDef.K_BACKSPACE )
            {
                if ( _ChatBuffer.Length > 0 )
                {
                    _ChatBuffer.Length--;
                }
                return;
            }

            if ( _ChatBuffer.Length == 31 )
                return; // all full

            _ChatBuffer.Append( ( Char ) key );
        }

        /// <summary>
        /// Key_Console
        /// Interactive line editing and console scrollback
        /// </summary>
        private void KeyConsole( Int32 key )
        {
            if ( key == KeysDef.K_ENTER )
            {
                var line = new String( _Lines[_EditLine] ).TrimEnd( '\0', ' ' );
                var cmd = line.Substring( 1 );
                Host.CommandBuffer.AddText( cmd );	// skip the >
                Host.CommandBuffer.AddText( "\n" );
                Host.Console.Print( "{0}\n", line );
                _EditLine = ( _EditLine + 1 ) & 31;
                _HistoryLine = _EditLine;
                _Lines[_EditLine][0] = ']';
                LinePos = 1;
                if ( Host.Client.cls.state == cactive_t.ca_disconnected )
                    Host.Screen.UpdateScreen( );	// force an update, because the command
                // may take some time
                return;
            }

            if ( key == KeysDef.K_TAB )
            {
                // command completion
                var txt = new String( _Lines[_EditLine], 1, KeysDef.MAXCMDLINE - 1 ).TrimEnd( '\0', ' ' );
                var cmds = Host.Command.Complete( txt );
                var vars = CVar.CompleteName( txt );
                String match = null;
                if ( cmds != null )
                {
                    if ( cmds.Length > 1 || vars != null )
                    {
                        Host.Console.Print( "\nCommands:\n" );
                        foreach ( var s in cmds )
                            Host.Console.Print( "  {0}\n", s );
                    }
                    else
                        match = cmds[0];
                }
                if ( vars != null )
                {
                    if ( vars.Length > 1 || cmds != null )
                    {
                        Host.Console.Print( "\nVariables:\n" );
                        foreach ( var s in vars )
                            Host.Console.Print( "  {0}\n", s );
                    }
                    else if ( match == null )
                        match = vars[0];
                }
                if ( !String.IsNullOrEmpty( match ) )
                {
                    var len = Math.Min( match.Length, KeysDef.MAXCMDLINE - 3 );
                    for ( var i = 0; i < len; i++ )
                    {
                        _Lines[_EditLine][i + 1] = match[i];
                    }
                    LinePos = len + 1;
                    _Lines[_EditLine][LinePos] = ' ';
                    LinePos++;
                    _Lines[_EditLine][LinePos] = '\0';
                    return;
                }
            }

            if ( key == KeysDef.K_BACKSPACE || key == KeysDef.K_LEFTARROW )
            {
                if ( LinePos > 1 )
                    LinePos--;
                return;
            }

            if ( key == KeysDef.K_UPARROW )
            {
                do
                {
                    _HistoryLine = ( _HistoryLine - 1 ) & 31;
                } while ( _HistoryLine != _EditLine && ( _Lines[_HistoryLine][1] == 0 ) );
                if ( _HistoryLine == _EditLine )
                    _HistoryLine = ( _EditLine + 1 ) & 31;
                Array.Copy( _Lines[_HistoryLine], _Lines[_EditLine], KeysDef.MAXCMDLINE );
                LinePos = 0;
                while ( _Lines[_EditLine][LinePos] != '\0' && LinePos < KeysDef.MAXCMDLINE )
                    LinePos++;
                return;
            }

            if ( key == KeysDef.K_DOWNARROW )
            {
                if ( _HistoryLine == _EditLine )
                    return;
                do
                {
                    _HistoryLine = ( _HistoryLine + 1 ) & 31;
                }
                while ( _HistoryLine != _EditLine && ( _Lines[_HistoryLine][1] == '\0' ) );
                if ( _HistoryLine == _EditLine )
                {
                    _Lines[_EditLine][0] = ']';
                    LinePos = 1;
                }
                else
                {
                    Array.Copy( _Lines[_HistoryLine], _Lines[_EditLine], KeysDef.MAXCMDLINE );
                    LinePos = 0;
                    while ( _Lines[_EditLine][LinePos] != '\0' && LinePos < KeysDef.MAXCMDLINE )
                        LinePos++;
                }
                return;
            }

            if ( key == KeysDef.K_PGUP || key == KeysDef.K_MWHEELUP )
            {
                Host.Console.BackScroll += 2;
                if ( Host.Console.BackScroll > Host.Console.TotalLines - ( Host.Screen.vid.height >> 3 ) - 1 )
                    Host.Console.BackScroll = Host.Console.TotalLines - ( Host.Screen.vid.height >> 3 ) - 1;
                return;
            }

            if ( key == KeysDef.K_PGDN || key == KeysDef.K_MWHEELDOWN )
            {
                Host.Console.BackScroll -= 2;
                if ( Host.Console.BackScroll < 0 )
                    Host.Console.BackScroll = 0;
                return;
            }

            if ( key == KeysDef.K_HOME )
            {
                Host.Console.BackScroll = Host.Console.TotalLines - ( Host.Screen.vid.height >> 3 ) - 1;
                return;
            }

            if ( key == KeysDef.K_END )
            {
                Host.Console.BackScroll = 0;
                return;
            }

            if ( key < 32 || key > 127 )
                return;	// non printable

            if ( LinePos < KeysDef.MAXCMDLINE - 1 )
            {
                _Lines[_EditLine][LinePos] = ( Char ) key;
                LinePos++;
                _Lines[_EditLine][LinePos] = '\0';
            }
        }
    }

    // keydest_t;
}
