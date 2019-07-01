using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpQuake.Framework;

namespace SharpQuake
{
    /// <summary>
    /// M_Menu_LanConfig_functions
    /// </summary>
    public class LanConfigMenu : MenuBase
    {
        public Boolean JoiningGame
        {
            get
            {
                return MenuBase.MultiPlayerMenu.Cursor == 0;
            }
        }

        public Boolean StartingGame
        {
            get
            {
                return MenuBase.MultiPlayerMenu.Cursor == 1;
            }
        }

        private const Int32 NUM_LANCONFIG_CMDS = 3;

        private static readonly Int32[] _CursorTable = new Int32[] { 72, 92, 124 };

        private Int32 _Port;
        private String _PortName;
        private String _JoinName;

        public override void Show( Host host )
        {
            base.Show( host );

            if ( _Cursor == -1 )
            {
                if ( JoiningGame )
                    _Cursor = 2;
                else
                    _Cursor = 1;
            }
            if ( StartingGame && _Cursor == 2 )
                _Cursor = 1;
            _Port = net.DefaultHostPort;
            _PortName = _Port.ToString( );

            Menu.ReturnOnError = false;
            Menu.ReturnReason = String.Empty;
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
                        _Cursor = NUM_LANCONFIG_CMDS - 1;
                    break;

                case KeysDef.K_DOWNARROW:
                    snd.LocalSound( "misc/menu1.wav" );
                    _Cursor++;
                    if ( _Cursor >= NUM_LANCONFIG_CMDS )
                        _Cursor = 0;
                    break;

                case KeysDef.K_ENTER:
                    if ( _Cursor == 0 )
                        break;

                    Menu.EnterSound = true;
                    net.HostPort = _Port;

                    if ( _Cursor == 1 )
                    {
                        if ( StartingGame )
                        {
                            MenuBase.GameOptionsMenu.Show( Host );
                        }
                        else
                        {
                            MenuBase.SearchMenu.Show( Host );
                        }
                        break;
                    }

                    if ( _Cursor == 2 )
                    {
                        Menu.ReturnMenu = this;
                        Menu.ReturnOnError = true;
                        MenuBase.CurrentMenu.Hide( );
                        Host.CommandBuffer.AddText( String.Format( "connect \"{0}\"\n", _JoinName ) );
                        break;
                    }
                    break;

                case KeysDef.K_BACKSPACE:
                    if ( _Cursor == 0 )
                    {
                        if ( !String.IsNullOrEmpty( _PortName ) )
                            _PortName = _PortName.Substring( 0, _PortName.Length - 1 );
                    }

                    if ( _Cursor == 2 )
                    {
                        if ( !String.IsNullOrEmpty( _JoinName ) )
                            _JoinName = _JoinName.Substring( 0, _JoinName.Length - 1 );
                    }
                    break;

                default:
                    if ( key < 32 || key > 127 )
                        break;

                    if ( _Cursor == 2 )
                    {
                        if ( _JoinName.Length < 21 )
                            _JoinName += ( Char ) key;
                    }

                    if ( key < '0' || key > '9' )
                        break;

                    if ( _Cursor == 0 )
                    {
                        if ( _PortName.Length < 5 )
                            _PortName += ( Char ) key;
                    }
                    break;
            }

            if ( StartingGame && _Cursor == 2 )
                if ( key == KeysDef.K_UPARROW )
                    _Cursor = 1;
                else
                    _Cursor = 0;

            var k = MathLib.atoi( _PortName );
            if ( k > 65535 )
                k = _Port;
            else
                _Port = k;
            _PortName = _Port.ToString( );
        }

        public override void Draw( )
        {
            Menu.DrawTransPic( 16, 4, Drawer.CachePic( "gfx/qplaque.lmp" ) );
            GLPic p = Drawer.CachePic( "gfx/p_multi.lmp" );
            var basex = ( 320 - p.width ) / 2;
            Menu.DrawPic( basex, 4, p );

            String startJoin;
            if ( StartingGame )
                startJoin = "New Game - TCP/IP";
            else
                startJoin = "Join Game - TCP/IP";

            Menu.Print( basex, 32, startJoin );
            basex += 8;

            Menu.Print( basex, 52, "Address:" );
            Menu.Print( basex + 9 * 8, 52, net.MyTcpIpAddress );

            Menu.Print( basex, _CursorTable[0], "Port" );
            Menu.DrawTextBox( basex + 8 * 8, _CursorTable[0] - 8, 6, 1 );
            Menu.Print( basex + 9 * 8, _CursorTable[0], _PortName );

            if ( JoiningGame )
            {
                Menu.Print( basex, _CursorTable[1], "Search for local games..." );
                Menu.Print( basex, 108, "Join game at:" );
                Menu.DrawTextBox( basex + 8, _CursorTable[2] - 8, 22, 1 );
                Menu.Print( basex + 16, _CursorTable[2], _JoinName );
            }
            else
            {
                Menu.DrawTextBox( basex, _CursorTable[1] - 8, 2, 1 );
                Menu.Print( basex + 8, _CursorTable[1], "OK" );
            }

            Menu.DrawCharacter( basex - 8, _CursorTable[_Cursor], 12 + ( ( Int32 ) ( Host.RealTime * 4 ) & 1 ) );

            if ( _Cursor == 0 )
                Menu.DrawCharacter( basex + 9 * 8 + 8 * _PortName.Length,
                    _CursorTable[0], 10 + ( ( Int32 ) ( Host.RealTime * 4 ) & 1 ) );

            if ( _Cursor == 2 )
                Menu.DrawCharacter( basex + 16 + 8 * _JoinName.Length, _CursorTable[2],
                    10 + ( ( Int32 ) ( Host.RealTime * 4 ) & 1 ) );

            if ( !String.IsNullOrEmpty( Menu.ReturnReason ) )
                Menu.PrintWhite( basex, 148, Menu.ReturnReason );
        }

        public LanConfigMenu( )
        {
            _Cursor = -1;
            _JoinName = String.Empty;
        }
    }
}
