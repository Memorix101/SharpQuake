using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpQuake.Framework;

namespace SharpQuake
{
    public class SetupMenu : MenuBase
    {
        private const Int32 NUM_SETUP_CMDS = 5;

        private readonly Int32[] _CursorTable = new Int32[]
        {
            40, 56, 80, 104, 140
        }; // setup_cursor_table

        private String _HostName; // setup_hostname[16]
        private String _MyName; // setup_myname[16]
        private Int32 _OldTop; // setup_oldtop
        private Int32 _OldBottom; // setup_oldbottom
        private Int32 _Top; // setup_top
        private Int32 _Bottom; // setup_bottom

        /// <summary>
        /// M_Menu_Setup_f
        /// </summary>
        public override void Show( Host host )
        {
            _MyName = client.Name;
            _HostName = Host.Network.HostName;
            _Top = _OldTop = ( ( Int32 ) client.Color ) >> 4;
            _Bottom = _OldBottom = ( ( Int32 ) client.Color ) & 15;

            base.Show( host );
        }

        public override void KeyEvent( Int32 key )
        {
            switch ( key )
            {
                case KeysDef.K_ESCAPE:
                    MenuBase.MultiPlayerMenu.Show( Host );
                    break;

                case KeysDef.K_UPARROW:
                    snd.LocalSound( "misc/menu1.wav" );
                    _Cursor--;
                    if ( _Cursor < 0 )
                        _Cursor = NUM_SETUP_CMDS - 1;
                    break;

                case KeysDef.K_DOWNARROW:
                    snd.LocalSound( "misc/menu1.wav" );
                    _Cursor++;
                    if ( _Cursor >= NUM_SETUP_CMDS )
                        _Cursor = 0;
                    break;

                case KeysDef.K_LEFTARROW:
                    if ( _Cursor < 2 )
                        return;
                    snd.LocalSound( "misc/menu3.wav" );
                    if ( _Cursor == 2 )
                        _Top = _Top - 1;
                    if ( _Cursor == 3 )
                        _Bottom = _Bottom - 1;
                    break;

                case KeysDef.K_RIGHTARROW:
                    if ( _Cursor < 2 )
                        return;
                    forward:
                    snd.LocalSound( "misc/menu3.wav" );
                    if ( _Cursor == 2 )
                        _Top = _Top + 1;
                    if ( _Cursor == 3 )
                        _Bottom = _Bottom + 1;
                    break;

                case KeysDef.K_ENTER:
                    if ( _Cursor == 0 || _Cursor == 1 )
                        return;

                    if ( _Cursor == 2 || _Cursor == 3 )
                        goto forward;

                    // _Cursor == 4 (OK)
                    if ( _MyName != client.Name )
                        Host.CommandBuffer.AddText( String.Format( "name \"{0}\"\n", _MyName ) );
                    if ( Host.Network.HostName != _HostName )
                        CVar.Set( "hostname", _HostName );
                    if ( _Top != _OldTop || _Bottom != _OldBottom )
                        Host.CommandBuffer.AddText( String.Format( "color {0} {1}\n", _Top, _Bottom ) );
                    Host.Menu.EnterSound = true;
                    MenuBase.MultiPlayerMenu.Show( Host );
                    break;

                case KeysDef.K_BACKSPACE:
                    if ( _Cursor == 0 )
                    {
                        if ( !String.IsNullOrEmpty( _HostName ) )
                            _HostName = _HostName.Substring( 0, _HostName.Length - 1 );// setup_hostname[strlen(setup_hostname) - 1] = 0;
                    }

                    if ( _Cursor == 1 )
                    {
                        if ( !String.IsNullOrEmpty( _MyName ) )
                            _MyName = _MyName.Substring( 0, _MyName.Length - 1 );
                    }
                    break;

                default:
                    if ( key < 32 || key > 127 )
                        break;
                    if ( _Cursor == 0 )
                    {
                        var l = _HostName.Length;
                        if ( l < 15 )
                        {
                            _HostName = _HostName + ( Char ) key;
                        }
                    }
                    if ( _Cursor == 1 )
                    {
                        var l = _MyName.Length;
                        if ( l < 15 )
                        {
                            _MyName = _MyName + ( Char ) key;
                        }
                    }
                    break;
            }

            if ( _Top > 13 )
                _Top = 0;
            if ( _Top < 0 )
                _Top = 13;
            if ( _Bottom > 13 )
                _Bottom = 0;
            if ( _Bottom < 0 )
                _Bottom = 13;
        }

        public override void Draw( )
        {
            Host.Menu.DrawTransPic( 16, 4, Drawer.CachePic( "gfx/qplaque.lmp" ) );
            var p = Drawer.CachePic( "gfx/p_multi.lmp" );
            Host.Menu.DrawPic( ( 320 - p.width ) / 2, 4, p );

            Host.Menu.Print( 64, 40, "Hostname" );
            Host.Menu.DrawTextBox( 160, 32, 16, 1 );
            Host.Menu.Print( 168, 40, _HostName );

            Host.Menu.Print( 64, 56, "Your name" );
            Host.Menu.DrawTextBox( 160, 48, 16, 1 );
            Host.Menu.Print( 168, 56, _MyName );

            Host.Menu.Print( 64, 80, "Shirt color" );
            Host.Menu.Print( 64, 104, "Pants color" );

            Host.Menu.DrawTextBox( 64, 140 - 8, 14, 1 );
            Host.Menu.Print( 72, 140, "Accept Changes" );

            p = Drawer.CachePic( "gfx/bigbox.lmp" );
            Host.Menu.DrawTransPic( 160, 64, p );
            p = Drawer.CachePic( "gfx/menuplyr.lmp" );
            Host.Menu.BuildTranslationTable( _Top * 16, _Bottom * 16 );
            Host.Menu.DrawTransPicTranslate( 172, 72, p );

            Host.Menu.DrawCharacter( 56, _CursorTable[_Cursor], 12 + ( ( Int32 ) ( Host.RealTime * 4 ) & 1 ) );

            if ( _Cursor == 0 )
                Host.Menu.DrawCharacter( 168 + 8 * _HostName.Length, _CursorTable[_Cursor], 10 + ( ( Int32 ) ( Host.RealTime * 4 ) & 1 ) );

            if ( _Cursor == 1 )
                Host.Menu.DrawCharacter( 168 + 8 * _MyName.Length, _CursorTable[_Cursor], 10 + ( ( Int32 ) ( Host.RealTime * 4 ) & 1 ) );
        }
    }
}
