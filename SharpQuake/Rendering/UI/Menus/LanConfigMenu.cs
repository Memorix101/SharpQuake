/// <copyright>
///
/// SharpQuakeEvolved changes by optimus-code, 2019
/// 
/// Based on SharpQuake (Quake Rewritten in C# by Yury Kiselev, 2010.)
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

using SharpQuake.Factories.Rendering.UI;
using SharpQuake.Framework;
using System;

namespace SharpQuake.Rendering.UI
{
	/// <summary>
	/// M_Menu_LanConfig_functions
	/// </summary>
	public class LanConfigMenu : BaseMenu
	{
		public Boolean JoiningGame
		{
			get
			{
				return MenuFactory.Get( "menu_multiplayer" ).Cursor == 0;
			}
		}

		public Boolean StartingGame
		{
			get
			{
				return MenuFactory.Get( "menu_multiplayer" ).Cursor == 1;
			}
		}

		private const Int32 NUM_LANCONFIG_CMDS = 3;

		private static readonly Int32[] CursorTable = new Int32[] { 72, 92, 124 };

		private Int32 _Port;
		private String _PortName;
		private String _JoinName;

		public LanConfigMenu( MenuFactory menuFactory ) : base( "menu_lan_config", menuFactory )
		{
			Cursor = -1;
			_JoinName = String.Empty;
		}

		public override void Show( Host host )
		{
			base.Show( host );

			if ( Cursor == -1 )
			{
				if ( JoiningGame )
					Cursor = 2;
				else
					Cursor = 1;
			}
			if ( StartingGame && Cursor == 2 )
				Cursor = 1;
			_Port = Host.Network.DefaultHostPort;
			_PortName = _Port.ToString();

			Host.Menus.ReturnOnError = false;
			Host.Menus.ReturnReason = String.Empty;
		}

		public override void KeyEvent( Int32 key )
		{
			switch ( key )
			{
				case KeysDef.K_ESCAPE:
					MenuFactory.Show( "menu_multiplayer" );
					break;

				case KeysDef.K_UPARROW:
					Host.Sound.LocalSound( "misc/menu1.wav" );
					Cursor--;
					if ( Cursor < 0 )
						Cursor = NUM_LANCONFIG_CMDS - 1;
					break;

				case KeysDef.K_DOWNARROW:
					Host.Sound.LocalSound( "misc/menu1.wav" );
					Cursor++;
					if ( Cursor >= NUM_LANCONFIG_CMDS )
						Cursor = 0;
					break;

				case KeysDef.K_ENTER:
					if ( Cursor == 0 )
						break;

					Host.Menus.EnterSound = true;
					Host.Network.HostPort = _Port;

					if ( Cursor == 1 )
					{
						if ( StartingGame )
						{
							MenuFactory.Show( "menu_options" );
						}
						else
						{
							MenuFactory.Show( "menu_search" );
						}
						break;
					}

					if ( Cursor == 2 )
					{
						Host.Menus.ReturnMenu = this;
						Host.Menus.ReturnOnError = true;
						MenuFactory.CurrentMenu.Hide();
						Host.Commands.Buffer.Append( String.Format( "connect \"{0}\"\n", _JoinName ) );
						break;
					}
					break;

				case KeysDef.K_BACKSPACE:
					if ( Cursor == 0 )
					{
						if ( !String.IsNullOrEmpty( _PortName ) )
							_PortName = _PortName.Substring( 0, _PortName.Length - 1 );
					}

					if ( Cursor == 2 )
					{
						if ( !String.IsNullOrEmpty( _JoinName ) )
							_JoinName = _JoinName.Substring( 0, _JoinName.Length - 1 );
					}
					break;

				default:
					if ( key < 32 || key > 127 )
						break;

					if ( Cursor == 2 )
					{
						if ( _JoinName.Length < 21 )
							_JoinName += ( Char ) key;
					}

					if ( key < '0' || key > '9' )
						break;

					if ( Cursor == 0 )
					{
						if ( _PortName.Length < 5 )
							_PortName += ( Char ) key;
					}
					break;
			}

			if ( StartingGame && Cursor == 2 )
				if ( key == KeysDef.K_UPARROW )
					Cursor = 1;
				else
					Cursor = 0;

			var k = MathLib.atoi( _PortName );
			if ( k > 65535 )
				k = _Port;
			else
				_Port = k;
			_PortName = _Port.ToString();
		}

		public override void Draw( )
		{
			Host.Menus.DrawTransPic( 16, 4, Host.DrawingContext.CachePic( "gfx/qplaque.lmp", "GL_NEAREST" ) );
			var p = Host.DrawingContext.CachePic( "gfx/p_multi.lmp", "GL_NEAREST" );
			var basex = ( 320 - p.Width ) / 2;
			Host.Menus.DrawPic( basex, 4, p );

			String startJoin;
			if ( StartingGame )
				startJoin = "New Game - TCP/IP";
			else
				startJoin = "Join Game - TCP/IP";

			Host.Menus.Print( basex, 32, startJoin );
			basex += 8;

			Host.Menus.Print( basex, 52, "Address:" );
			Host.Menus.Print( basex + 9 * 8, 52, Host.Network.MyTcpIpAddress );

			Host.Menus.Print( basex, CursorTable[0], "Port" );
			Host.Menus.DrawTextBox( basex + 8 * 8, CursorTable[0] - 8, 6, 1 );
			Host.Menus.Print( basex + 9 * 8, CursorTable[0], _PortName );

			if ( JoiningGame )
			{
				Host.Menus.Print( basex, CursorTable[1], "Search for local games..." );
				Host.Menus.Print( basex, 108, "Join game at:" );
				Host.Menus.DrawTextBox( basex + 8, CursorTable[2] - 8, 22, 1 );
				Host.Menus.Print( basex + 16, CursorTable[2], _JoinName );
			}
			else
			{
				Host.Menus.DrawTextBox( basex, CursorTable[1] - 8, 2, 1 );
				Host.Menus.Print( basex + 8, CursorTable[1], "OK" );
			}

			Host.Menus.DrawCharacter( basex - 8, CursorTable[Cursor], 12 + ( ( Int32 ) ( Host.RealTime * 4 ) & 1 ) );

			if ( Cursor == 0 )
				Host.Menus.DrawCharacter( basex + 9 * 8 + 8 * _PortName.Length,
					CursorTable[0], 10 + ( ( Int32 ) ( Host.RealTime * 4 ) & 1 ) );

			if ( Cursor == 2 )
				Host.Menus.DrawCharacter( basex + 16 + 8 * _JoinName.Length, CursorTable[2],
					10 + ( ( Int32 ) ( Host.RealTime * 4 ) & 1 ) );

			if ( !String.IsNullOrEmpty( Host.Menus.ReturnReason ) )
				Host.Menus.PrintWhite( basex, 148, Host.Menus.ReturnReason );
		}
	}
}
