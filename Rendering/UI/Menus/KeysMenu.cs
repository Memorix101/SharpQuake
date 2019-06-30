using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpQuake.Framework;

namespace SharpQuake
{
    public class KeysMenu : MenuBase
    {
        private static readonly String[][] _BindNames = new String[][]
        {
            new String[] {"+attack",        "attack"},
            new String[] {"impulse 10",     "change weapon"},
            new String[] {"+jump",          "jump / swim up"},
            new String[] {"+forward",       "walk forward"},
            new String[] {"+back",          "backpedal"},
            new String[] {"+left",          "turn left"},
            new String[] {"+right",         "turn right"},
            new String[] {"+speed",         "run"},
            new String[] {"+moveleft",      "step left"},
            new String[] {"+moveright",     "step right"},
            new String[] {"+strafe",        "sidestep"},
            new String[] {"+lookup",        "look up"},
            new String[] {"+lookdown",      "look down"},
            new String[] {"centerview",     "center view"},
            new String[] {"+mlook",         "mouse look"},
            new String[] {"+klook",         "keyboard look"},
            new String[] {"+moveup",        "swim up"},
            new String[] {"+movedown",      "swim down"}
        };

        //const inte	NUMCOMMANDS	(sizeof(bindnames)/sizeof(bindnames[0]))

        private Boolean _BindGrab; // bind_grab

        public override void Show( Host host )
        {
            base.Show( host );
        }

        public override void KeyEvent( Int32 key )
        {
            if ( _BindGrab )
            {
                // defining a key
                snd.LocalSound( "misc/menu1.wav" );
                if ( key == KeysDef.K_ESCAPE )
                {
                    _BindGrab = false;
                }
                else if ( key != '`' )
                {
                    var cmd = String.Format( "bind \"{0}\" \"{1}\"\n", Key.KeynumToString( key ), _BindNames[_Cursor][0] );
                    Host.CommandBuffer.InsertText( cmd );
                }

                _BindGrab = false;
                return;
            }

            switch ( key )
            {
                case KeysDef.K_ESCAPE:
                    MenuBase.OptionsMenu.Show( Host );
                    break;

                case KeysDef.K_LEFTARROW:
                case KeysDef.K_UPARROW:
                    snd.LocalSound( "misc/menu1.wav" );
                    _Cursor--;
                    if ( _Cursor < 0 )
                        _Cursor = _BindNames.Length - 1;
                    break;

                case KeysDef.K_DOWNARROW:
                case KeysDef.K_RIGHTARROW:
                    snd.LocalSound( "misc/menu1.wav" );
                    _Cursor++;
                    if ( _Cursor >= _BindNames.Length )
                        _Cursor = 0;
                    break;

                case KeysDef.K_ENTER:		// go into bind mode
                    Int32[] keys = new Int32[2];
                    FindKeysForCommand( _BindNames[_Cursor][0], keys );
                    snd.LocalSound( "misc/menu2.wav" );
                    if ( keys[1] != -1 )
                        UnbindCommand( _BindNames[_Cursor][0] );
                    _BindGrab = true;
                    break;

                case KeysDef.K_BACKSPACE:		// delete bindings
                case KeysDef.K_DEL:				// delete bindings
                    snd.LocalSound( "misc/menu2.wav" );
                    UnbindCommand( _BindNames[_Cursor][0] );
                    break;
            }
        }

        public override void Draw( )
        {
            GLPic p = Drawer.CachePic( "gfx/ttl_cstm.lmp" );
            Menu.DrawPic( ( 320 - p.width ) / 2, 4, p );

            if ( _BindGrab )
                Menu.Print( 12, 32, "Press a key or button for this action" );
            else
                Menu.Print( 18, 32, "Enter to change, backspace to clear" );

            // search for known bindings
            Int32[] keys = new Int32[2];

            for ( var i = 0; i < _BindNames.Length; i++ )
            {
                var y = 48 + 8 * i;

                Menu.Print( 16, y, _BindNames[i][1] );

                FindKeysForCommand( _BindNames[i][0], keys );

                if ( keys[0] == -1 )
                {
                    Menu.Print( 140, y, "???" );
                }
                else
                {
                    var name = Key.KeynumToString( keys[0] );
                    Menu.Print( 140, y, name );
                    var x = name.Length * 8;
                    if ( keys[1] != -1 )
                    {
                        Menu.Print( 140 + x + 8, y, "or" );
                        Menu.Print( 140 + x + 32, y, Key.KeynumToString( keys[1] ) );
                    }
                }
            }

            if ( _BindGrab )
                Menu.DrawCharacter( 130, 48 + _Cursor * 8, '=' );
            else
                Menu.DrawCharacter( 130, 48 + _Cursor * 8, 12 + ( ( Int32 ) ( Host.RealTime * 4 ) & 1 ) );
        }

        /// <summary>
        /// M_FindKeysForCommand
        /// </summary>
        private void FindKeysForCommand( String command, Int32[] twokeys )
        {
            twokeys[0] = twokeys[1] = -1;
            var len = command.Length;
            var count = 0;

            for ( var j = 0; j < 256; j++ )
            {
                var b = Key.Bindings[j];
                if ( String.IsNullOrEmpty( b ) )
                    continue;

                if ( String.Compare( b, 0, command, 0, len ) == 0 )
                {
                    twokeys[count] = j;
                    count++;
                    if ( count == 2 )
                        break;
                }
            }
        }

        /// <summary>
        /// M_UnbindCommand
        /// </summary>
        private void UnbindCommand( String command )
        {
            var len = command.Length;

            for ( var j = 0; j < 256; j++ )
            {
                var b = Key.Bindings[j];
                if ( String.IsNullOrEmpty( b ) )
                    continue;

                if ( String.Compare( b, 0, command, 0, len ) == 0 )
                    Key.SetBinding( j, String.Empty );
            }
        }
    }

}
