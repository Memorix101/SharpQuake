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
    internal enum keydest_t
    {
        key_game, key_console, key_message, key_menu
    }

    /// <summary>
    /// Key_functions
    /// </summary>
    internal static class Key
    {
        private struct keyname_t
        {
            public String name;
            public Int32 keynum;

            public keyname_t( String name, Int32 keynum )
            {
                this.name = name;
                this.keynum = keynum;
            }
        } //keyname_t;

        public static keydest_t Destination
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

        public static Boolean TeamMessage
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

        public static Char[][] Lines
        {
            get
            {
                return _Lines;
            }
        }

        public static Int32 EditLine
        {
            get
            {
                return _EditLine;
            }
        }

        public static String ChatBuffer
        {
            get
            {
                return _ChatBuffer.ToString();
            }
        }

        public static Int32 LastPress
        {
            get
            {
                return _LastPress;
            }
        }

        public static String[] Bindings
        {
            get
            {
                return _Bindings;
            }
        }

        //
        // these are the key numbers that should be passed to Key_Event
        //
        public const Int32 K_TAB = 9;

        public const Int32 K_ENTER = 13;

        public const Int32 K_ESCAPE = 27;

        public const Int32 K_SPACE = 32;

        public const Int32 K_BACKSPACE = 127;

        // normal keys should be passed as lowercased ascii
        public const Int32 K_UPARROW = 128;

        public const Int32 K_DOWNARROW = 129;

        public const Int32 K_LEFTARROW = 130;

        public const Int32 K_RIGHTARROW = 131;

        public const Int32 K_ALT = 132;

        public const Int32 K_CTRL = 133;

        public const Int32 K_SHIFT = 134;

        public const Int32 K_F1 = 135;

        public const Int32 K_F2 = 136;

        public const Int32 K_F3 = 137;

        public const Int32 K_F4 = 138;

        public const Int32 K_F5 = 139;

        public const Int32 K_F6 = 140;

        public const Int32 K_F7 = 141;

        public const Int32 K_F8 = 142;

        public const Int32 K_F9 = 143;

        public const Int32 K_F10 = 144;

        public const Int32 K_F11 = 145;

        public const Int32 K_F12 = 146;

        public const Int32 K_INS = 147;

        public const Int32 K_DEL = 148;

        public const Int32 K_PGDN = 149;

        public const Int32 K_PGUP = 150;

        public const Int32 K_HOME = 151;

        public const Int32 K_END = 152;

        public const Int32 K_PAUSE = 255;

        //
        // mouse buttons generate virtual keys
        //
        public const Int32 K_MOUSE1 = 200;

        public const Int32 K_MOUSE2 = 201;

        public const Int32 K_MOUSE3 = 202;

        //
        // joystick buttons
        //
        public const Int32 K_JOY1 = 203;

        public const Int32 K_JOY2 = 204;

        public const Int32 K_JOY3 = 205;

        public const Int32 K_JOY4 = 206;

        //
        // aux keys are for multi-buttoned joysticks to generate so they can use
        // the normal binding process
        //
        public const Int32 K_AUX1 = 207;

        public const Int32 K_AUX2 = 208;

        public const Int32 K_AUX3 = 209;

        public const Int32 K_AUX4 = 210;

        public const Int32 K_AUX5 = 211;

        public const Int32 K_AUX6 = 212;

        public const Int32 K_AUX7 = 213;

        public const Int32 K_AUX8 = 214;

        public const Int32 K_AUX9 = 215;

        public const Int32 K_AUX10 = 216;

        public const Int32 K_AUX11 = 217;

        public const Int32 K_AUX12 = 218;

        public const Int32 K_AUX13 = 219;

        public const Int32 K_AUX14 = 220;

        public const Int32 K_AUX15 = 221;

        public const Int32 K_AUX16 = 222;

        public const Int32 K_AUX17 = 223;

        public const Int32 K_AUX18 = 224;

        public const Int32 K_AUX19 = 225;

        public const Int32 K_AUX20 = 226;

        public const Int32 K_AUX21 = 227;

        public const Int32 K_AUX22 = 228;

        public const Int32 K_AUX23 = 229;

        public const Int32 K_AUX24 = 230;

        public const Int32 K_AUX25 = 231;

        public const Int32 K_AUX26 = 232;

        public const Int32 K_AUX27 = 233;

        public const Int32 K_AUX28 = 234;

        public const Int32 K_AUX29 = 235;

        public const Int32 K_AUX30 = 236;

        public const Int32 K_AUX31 = 237;

        public const Int32 K_AUX32 = 238;

        public const Int32 K_MWHEELUP = 239;

        // JACK: Intellimouse(c) Mouse Wheel Support
        public const Int32 K_MWHEELDOWN = 240;

        public static Int32 LinePos;

        public static Int32 KeyCount;

        private const Int32 MAXCMDLINE = 256;

        private static keyname_t[] _KeyNames = new keyname_t[]
        {
            new keyname_t("TAB", K_TAB),
            new keyname_t("ENTER", K_ENTER),
            new keyname_t("ESCAPE", K_ESCAPE),
            new keyname_t("SPACE", K_SPACE),
            new keyname_t("BACKSPACE", K_BACKSPACE),
            new keyname_t("UPARROW", K_UPARROW),
            new keyname_t("DOWNARROW", K_DOWNARROW),
            new keyname_t("LEFTARROW", K_LEFTARROW),
            new keyname_t("RIGHTARROW", K_RIGHTARROW),

            new keyname_t("ALT", K_ALT),
            new keyname_t("CTRL", K_CTRL),
            new keyname_t("SHIFT", K_SHIFT),

            new keyname_t("F1", K_F1),
            new keyname_t("F2", K_F2),
            new keyname_t("F3", K_F3),
            new keyname_t("F4", K_F4),
            new keyname_t("F5", K_F5),
            new keyname_t("F6", K_F6),
            new keyname_t("F7", K_F7),
            new keyname_t("F8", K_F8),
            new keyname_t("F9", K_F9),
            new keyname_t("F10", K_F10),
            new keyname_t("F11", K_F11),
            new keyname_t("F12", K_F12),

            new keyname_t("INS", K_INS),
            new keyname_t("DEL", K_DEL),
            new keyname_t("PGDN", K_PGDN),
            new keyname_t("PGUP", K_PGUP),
            new keyname_t("HOME", K_HOME),
            new keyname_t("END", K_END),

            new keyname_t("MOUSE1", K_MOUSE1),
            new keyname_t("MOUSE2", K_MOUSE2),
            new keyname_t("MOUSE3", K_MOUSE3),

            new keyname_t("JOY1", K_JOY1),
            new keyname_t("JOY2", K_JOY2),
            new keyname_t("JOY3", K_JOY3),
            new keyname_t("JOY4", K_JOY4),

            new keyname_t("AUX1", K_AUX1),
            new keyname_t("AUX2", K_AUX2),
            new keyname_t("AUX3", K_AUX3),
            new keyname_t("AUX4", K_AUX4),
            new keyname_t("AUX5", K_AUX5),
            new keyname_t("AUX6", K_AUX6),
            new keyname_t("AUX7", K_AUX7),
            new keyname_t("AUX8", K_AUX8),
            new keyname_t("AUX9", K_AUX9),
            new keyname_t("AUX10", K_AUX10),
            new keyname_t("AUX11", K_AUX11),
            new keyname_t("AUX12", K_AUX12),
            new keyname_t("AUX13", K_AUX13),
            new keyname_t("AUX14", K_AUX14),
            new keyname_t("AUX15", K_AUX15),
            new keyname_t("AUX16", K_AUX16),
            new keyname_t("AUX17", K_AUX17),
            new keyname_t("AUX18", K_AUX18),
            new keyname_t("AUX19", K_AUX19),
            new keyname_t("AUX20", K_AUX20),
            new keyname_t("AUX21", K_AUX21),
            new keyname_t("AUX22", K_AUX22),
            new keyname_t("AUX23", K_AUX23),
            new keyname_t("AUX24", K_AUX24),
            new keyname_t("AUX25", K_AUX25),
            new keyname_t("AUX26", K_AUX26),
            new keyname_t("AUX27", K_AUX27),
            new keyname_t("AUX28", K_AUX28),
            new keyname_t("AUX29", K_AUX29),
            new keyname_t("AUX30", K_AUX30),
            new keyname_t("AUX31", K_AUX31),
            new keyname_t("AUX32", K_AUX32),

            new keyname_t("PAUSE", K_PAUSE),

            new keyname_t("MWHEELUP", K_MWHEELUP),
            new keyname_t("MWHEELDOWN", K_MWHEELDOWN),

            new keyname_t("SEMICOLON", ';'),	// because a raw semicolon seperates commands
        };

        private static Char[][] _Lines = new Char[32][];//, MAXCMDLINE]; // char	key_lines[32][MAXCMDLINE];

        // key_linepos
        private static Boolean _ShiftDown; // = false;

        private static Int32 _LastPress; // key_lastpress

        private static Int32 _EditLine; // edit_line=0;
        private static Int32 _HistoryLine; // history_line=0;

        private static keydest_t _KeyDest; // key_dest

        // key_count			// incremented every key event

        private static String[] _Bindings = new String[256]; // char	*keybindings[256];
        private static Boolean[] _ConsoleKeys = new Boolean[256]; // consolekeys[256]	// if true, can't be rebound while in console
        private static Boolean[] _MenuBound = new Boolean[256]; // menubound[256]	// if true, can't be rebound while in menu
        private static Int32[] _KeyShift = new Int32[256]; // keyshift[256]		// key to map to if shift held down in console
        private static Int32[] _Repeats = new Int32[256]; // key_repeats[256]	// if > 1, it is autorepeating
        private static Boolean[] _KeyDown = new Boolean[256];

        private static StringBuilder _ChatBuffer = new StringBuilder( 32 ); // chat_buffer
        private static Boolean _TeamMessage; // qboolean team_message = false;

        // Key_Event (int key, qboolean down)
        //
        // Called by the system between frames for both key up and key down events
        // Should NOT be called during an interrupt!
        public static void Event( Int32 key, Boolean down )
        {
            _KeyDown[key] = down;

            if( !down )
                _Repeats[key] = 0;

            _LastPress = key;
            KeyCount++;
            if( KeyCount <= 0 )
                return;     // just catching keys for Con_NotifyBox

            // update auto-repeat status
            if( down )
            {
                _Repeats[key]++;
                if( key != K_BACKSPACE && key != K_PAUSE && key != K_PGUP && key != K_PGDN && _Repeats[key] > 1 )
                {
                    return; // ignore most autorepeats
                }

                if( key >= 200 && String.IsNullOrEmpty( _Bindings[key] ) )
                    Con.Print( "{0} is unbound, hit F4 to set.\n", KeynumToString( key ) );
            }

            if( key == K_SHIFT )
                _ShiftDown = down;

            //
            // handle escape specialy, so the user can never unbind it
            //
            if( key == K_ESCAPE )
            {
                if( !down )
                    return;

                switch( _KeyDest )
                {
                    case keydest_t.key_message:
                        KeyMessage( key );
                        break;

                    case keydest_t.key_menu:
                        menu.KeyDown( key );
                        break;

                    case keydest_t.key_game:
                    case keydest_t.key_console:
                        menu.ToggleMenu_f();
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
            if( !down )
            {
                var kb = _Bindings[key];
                if( !String.IsNullOrEmpty( kb ) && kb.StartsWith( "+" ) )
                {
                    Cbuf.AddText( String.Format( "-{0} {1}\n", kb.Substring( 1 ), key ) );
                }
                if( _KeyShift[key] != key )
                {
                    kb = _Bindings[_KeyShift[key]];
                    if( !String.IsNullOrEmpty( kb ) && kb.StartsWith( "+" ) )
                        Cbuf.AddText( String.Format( "-{0} {1}\n", kb.Substring( 1 ), key ) );
                }
                return;
            }

            //
            // during demo playback, most keys bring up the main menu
            //
            if( client.cls.demoplayback && down && _ConsoleKeys[key] && _KeyDest == keydest_t.key_game )
            {
                menu.ToggleMenu_f();
                return;
            }

            //
            // if not a consolekey, send to the interpreter no matter what mode is
            //
            if( ( _KeyDest == keydest_t.key_menu && _MenuBound[key] ) ||
                ( _KeyDest == keydest_t.key_console && !_ConsoleKeys[key] ) ||
                ( _KeyDest == keydest_t.key_game && ( !Con.ForcedUp || !_ConsoleKeys[key] ) ) )
            {
                var kb = _Bindings[key];
                if( !String.IsNullOrEmpty( kb ) )
                {
                    if( kb.StartsWith( "+" ) )
                    {
                        // button commands add keynum as a parm
                        Cbuf.AddText( String.Format( "{0} {1}\n", kb, key ) );
                    }
                    else
                    {
                        Cbuf.AddText( kb );
                        Cbuf.AddText( "\n" );
                    }
                }
                return;
            }

            if( !down )
                return;     // other systems only care about key down events

            if( _ShiftDown )
            {
                key = _KeyShift[key];
            }

            switch( _KeyDest )
            {
                case keydest_t.key_message:
                    KeyMessage( key );
                    break;

                case keydest_t.key_menu:
                    menu.KeyDown( key );
                    break;

                case keydest_t.key_game:
                case keydest_t.key_console:
                    KeyConsole( key );
                    break;

                default:
                    Utilities.Error( "Bad key_dest" );
                    break;
            }
        }

        // Key_Init (void);
        public static void Init()
        {
            for( var i = 0; i < 32; i++ )
            {
                _Lines[i] = new Char[MAXCMDLINE];
                _Lines[i][0] = ']'; // key_lines[i][0] = ']'; key_lines[i][1] = 0;
            }
            LinePos = 1;

            //
            // init ascii characters in console mode
            //
            for( var i = 32; i < 128; i++ )
                _ConsoleKeys[i] = true;
            _ConsoleKeys[K_ENTER] = true;
            _ConsoleKeys[K_TAB] = true;
            _ConsoleKeys[K_LEFTARROW] = true;
            _ConsoleKeys[K_RIGHTARROW] = true;
            _ConsoleKeys[K_UPARROW] = true;
            _ConsoleKeys[K_DOWNARROW] = true;
            _ConsoleKeys[K_BACKSPACE] = true;
            _ConsoleKeys[K_PGUP] = true;
            _ConsoleKeys[K_PGDN] = true;
            _ConsoleKeys[K_SHIFT] = true;
            _ConsoleKeys[K_MWHEELUP] = true;
            _ConsoleKeys[K_MWHEELDOWN] = true;
            _ConsoleKeys['`'] = false;
            _ConsoleKeys['~'] = false;

            for( var i = 0; i < 256; i++ )
                _KeyShift[i] = i;
            for( Int32 i = 'a'; i <= 'z'; i++ )
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

            _MenuBound[K_ESCAPE] = true;
            for( var i = 0; i < 12; i++ )
                _MenuBound[K_F1 + i] = true;

            //
            // register our functions
            //
            Command.Add( "bind", Bind_f );
            Command.Add( "unbind", Unbind_f );
            Command.Add( "unbindall", UnbindAll_f );
        }

        /// <summary>
        /// Key_WriteBindings
        /// </summary>
        public static void WriteBindings( Stream dest )
        {
            StringBuilder sb = new StringBuilder( 4096 );
            for( var i = 0; i < 256; i++ )
            {
                if( !String.IsNullOrEmpty( _Bindings[i] ) )
                {
                    sb.Append( "bind \"" );
                    sb.Append( KeynumToString( i ) );
                    sb.Append( "\" \"" );
                    sb.Append( _Bindings[i] );
                    sb.AppendLine( "\"" );
                }
            }
            Byte[] buf = Encoding.ASCII.GetBytes( sb.ToString() );
            dest.Write( buf, 0, buf.Length );
        }

        /// <summary>
        /// Key_SetBinding
        /// </summary>
        public static void SetBinding( Int32 keynum, String binding )
        {
            if( keynum != -1 )
            {
                _Bindings[keynum] = binding;
            }
        }

        // Key_ClearStates (void)
        public static void ClearStates()
        {
            for( var i = 0; i < 256; i++ )
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
        public static String KeynumToString( Int32 keynum )
        {
            if( keynum == -1 )
                return "<KEY NOT FOUND>";

            if( keynum > 32 && keynum < 127 )
            {
                // printable ascii
                return ( ( Char ) keynum ).ToString();
            }

            foreach( keyname_t kn in _KeyNames )
            {
                if( kn.keynum == keynum )
                    return kn.name;
            }
            return "<UNKNOWN KEYNUM>";
        }

        // Key_StringToKeynum
        //
        // Returns a key number to be used to index keybindings[] by looking at
        // the given string.  Single ascii characters return themselves, while
        // the K_* names are matched up.
        private static Int32 StringToKeynum( String str )
        {
            if( String.IsNullOrEmpty( str ) )
                return -1;
            if( str.Length == 1 )
                return str[0];

            foreach( keyname_t keyname in _KeyNames )
            {
                if( Utilities.SameText( keyname.name, str ) )
                    return keyname.keynum;
            }
            return -1;
        }

        //Key_Unbind_f
        private static void Unbind_f()
        {
            if( Command.Argc != 2 )
            {
                Con.Print( "unbind <key> : remove commands from a key\n" );
                return;
            }

            var b = StringToKeynum( Command.Argv( 1 ) );
            if( b == -1 )
            {
                Con.Print( "\"{0}\" isn't a valid key\n", Command.Argv( 1 ) );
                return;
            }

            SetBinding( b, null );
        }

        // Key_Unbindall_f
        private static void UnbindAll_f()
        {
            for( var i = 0; i < 256; i++ )
                if( !String.IsNullOrEmpty( _Bindings[i] ) )
                    SetBinding( i, null );
        }

        //Key_Bind_f
        private static void Bind_f()
        {
            var c = Command.Argc;
            if( c != 2 && c != 3 )
            {
                Con.Print( "bind <key> [command] : attach a command to a key\n" );
                return;
            }

            var b = StringToKeynum( Command.Argv( 1 ) );
            if( b == -1 )
            {
                Con.Print( "\"{0}\" isn't a valid key\n", Command.Argv( 1 ) );
                return;
            }

            if( c == 2 )
            {
                if( !String.IsNullOrEmpty( _Bindings[b] ) )// keybindings[b])
                    Con.Print( "\"{0}\" = \"{1}\"\n", Command.Argv( 1 ), _Bindings[b] );
                else
                    Con.Print( "\"{0}\" is not bound\n", Command.Argv( 1 ) );
                return;
            }

            // copy the rest of the command line
            // start out with a null string
            StringBuilder sb = new StringBuilder( 1024 );
            for( var i = 2; i < c; i++ )
            {
                if( i > 2 )
                    sb.Append( " " );
                sb.Append( Command.Argv( i ) );
            }

            SetBinding( b, sb.ToString() );
        }

        // Key_Message (int key)
        private static void KeyMessage( Int32 key )
        {
            if( key == K_ENTER )
            {
                if( _TeamMessage )
                    Cbuf.AddText( "say_team \"" );
                else
                    Cbuf.AddText( "say \"" );
                Cbuf.AddText( _ChatBuffer.ToString() );
                Cbuf.AddText( "\"\n" );

                Key.Destination = keydest_t.key_game;
                _ChatBuffer.Length = 0;
                return;
            }

            if( key == K_ESCAPE )
            {
                Key.Destination = keydest_t.key_game;
                _ChatBuffer.Length = 0;
                return;
            }

            if( key < 32 || key > 127 )
                return;	// non printable

            if( key == K_BACKSPACE )
            {
                if( _ChatBuffer.Length > 0 )
                {
                    _ChatBuffer.Length--;
                }
                return;
            }

            if( _ChatBuffer.Length == 31 )
                return; // all full

            _ChatBuffer.Append( ( Char ) key );
        }

        /// <summary>
        /// Key_Console
        /// Interactive line editing and console scrollback
        /// </summary>
        private static void KeyConsole( Int32 key )
        {
            if( key == K_ENTER )
            {
                var line = new String( _Lines[_EditLine] ).TrimEnd( '\0', ' ' );
                var cmd = line.Substring( 1 );
                Cbuf.AddText( cmd );	// skip the >
                Cbuf.AddText( "\n" );
                Con.Print( "{0}\n", line );
                _EditLine = ( _EditLine + 1 ) & 31;
                _HistoryLine = _EditLine;
                _Lines[_EditLine][0] = ']';
                Key.LinePos = 1;
                if( client.cls.state == cactive_t.ca_disconnected )
                    Scr.UpdateScreen();	// force an update, because the command
                // may take some time
                return;
            }

            if( key == K_TAB )
            {
                // command completion
                var txt = new String( _Lines[_EditLine], 1, MAXCMDLINE - 1 ).TrimEnd( '\0', ' ' );
                String[] cmds = Command.Complete( txt );
                String[] vars = CVar.CompleteName( txt );
                String match = null;
                if( cmds != null )
                {
                    if( cmds.Length > 1 || vars != null )
                    {
                        Con.Print( "\nCommands:\n" );
                        foreach( var s in cmds )
                            Con.Print( "  {0}\n", s );
                    }
                    else
                        match = cmds[0];
                }
                if( vars != null )
                {
                    if( vars.Length > 1 || cmds != null )
                    {
                        Con.Print( "\nVariables:\n" );
                        foreach( var s in vars )
                            Con.Print( "  {0}\n", s );
                    }
                    else if( match == null )
                        match = vars[0];
                }
                if( !String.IsNullOrEmpty( match ) )
                {
                    var len = Math.Min( match.Length, MAXCMDLINE - 3 );
                    for( var i = 0; i < len; i++ )
                    {
                        _Lines[_EditLine][i + 1] = match[i];
                    }
                    Key.LinePos = len + 1;
                    _Lines[_EditLine][Key.LinePos] = ' ';
                    Key.LinePos++;
                    _Lines[_EditLine][Key.LinePos] = '\0';
                    return;
                }
            }

            if( key == K_BACKSPACE || key == K_LEFTARROW )
            {
                if( Key.LinePos > 1 )
                    Key.LinePos--;
                return;
            }

            if( key == K_UPARROW )
            {
                do
                {
                    _HistoryLine = ( _HistoryLine - 1 ) & 31;
                } while( _HistoryLine != _EditLine && ( _Lines[_HistoryLine][1] == 0 ) );
                if( _HistoryLine == _EditLine )
                    _HistoryLine = ( _EditLine + 1 ) & 31;
                Array.Copy( _Lines[_HistoryLine], _Lines[_EditLine], MAXCMDLINE );
                Key.LinePos = 0;
                while( _Lines[_EditLine][Key.LinePos] != '\0' && Key.LinePos < MAXCMDLINE )
                    Key.LinePos++;
                return;
            }

            if( key == K_DOWNARROW )
            {
                if( _HistoryLine == _EditLine )
                    return;
                do
                {
                    _HistoryLine = ( _HistoryLine + 1 ) & 31;
                }
                while( _HistoryLine != _EditLine && ( _Lines[_HistoryLine][1] == '\0' ) );
                if( _HistoryLine == _EditLine )
                {
                    _Lines[_EditLine][0] = ']';
                    Key.LinePos = 1;
                }
                else
                {
                    Array.Copy( _Lines[_HistoryLine], _Lines[_EditLine], MAXCMDLINE );
                    Key.LinePos = 0;
                    while( _Lines[_EditLine][Key.LinePos] != '\0' && Key.LinePos < MAXCMDLINE )
                        Key.LinePos++;
                }
                return;
            }

            if( key == K_PGUP || key == K_MWHEELUP )
            {
                Con.BackScroll += 2;
                if( Con.BackScroll > Con.TotalLines - ( Scr.vid.height >> 3 ) - 1 )
                    Con.BackScroll = Con.TotalLines - ( Scr.vid.height >> 3 ) - 1;
                return;
            }

            if( key == K_PGDN || key == K_MWHEELDOWN )
            {
                Con.BackScroll -= 2;
                if( Con.BackScroll < 0 )
                    Con.BackScroll = 0;
                return;
            }

            if( key == K_HOME )
            {
                Con.BackScroll = Con.TotalLines - ( Scr.vid.height >> 3 ) - 1;
                return;
            }

            if( key == K_END )
            {
                Con.BackScroll = 0;
                return;
            }

            if( key < 32 || key > 127 )
                return;	// non printable

            if( Key.LinePos < MAXCMDLINE - 1 )
            {
                _Lines[_EditLine][Key.LinePos] = ( Char ) key;
                Key.LinePos++;
                _Lines[_EditLine][Key.LinePos] = '\0';
            }
        }
    }

    // keydest_t;
}
