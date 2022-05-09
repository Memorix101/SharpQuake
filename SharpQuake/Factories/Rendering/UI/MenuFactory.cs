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
/// 

using SharpQuake.Framework.Factories;
using SharpQuake.Framework.IO;
using SharpQuake.Framework.IO.Input;
using SharpQuake.Renderer.Textures;
using SharpQuake.Rendering.UI;
using SharpQuake.Rendering.UI.Elements;
using System;

// menu.h
// menu.c

namespace SharpQuake.Factories.Rendering.UI
{
	/// <summary>
	/// M_functions
	/// </summary>
	public class MenuFactory : BaseFactory<String, BaseMenu>
	{
		public BaseMenu CurrentMenu
		{
			get;
			private set;
		}

		public Boolean EnterSound;
		public Boolean ReturnOnError;
		public String ReturnReason;
		public BaseMenu ReturnMenu;
		private const Int32 SLIDER_RANGE = 10;

		//qboolean	m_entersound	// play after drawing a frame, so caching

		// won't disrupt the sound
		private Boolean _RecursiveDraw; // qboolean m_recursiveDraw

		private Byte[] _IdentityTable = new Byte[256]; // identityTable
		private Byte[] _TranslationTable = new Byte[256]; //translationTable

		// Instances
		public Host Host
		{
			get;
			private set;
		}

		private void InitialiseMenus( )
		{
			// Top Level
			Add( new MainMenu( this ) );
			Add( new SinglePlayerMenu( this ) );
			Add( new LoadMenu( "menu_load", this ) );
			Add( new SaveMenu( this ) );
			Add( new MultiplayerMenu( this ) );
			Add( new OptionsMenu( this ) );
			Add( new VideoMenu( this ) );
			Add( new HelpMenu( this ) );
			Add( new QuitMenu( this ) );

			// Submenus
			Add( new KeysMenu( this ) );
			Add( new LanConfigMenu( this ) );
			Add( new SetupMenu( this ) );
			Add( new GameOptionsMenu( this ) );
			Add( new SearchMenu( this ) );
			Add( new ServerListMenu( this ) );
		}

		public void Add( BaseMenu menu )
		{
			Add( menu.Name, menu );
		}

		/// <summary>
		/// M_Init
		/// </summary>
		public void Initialise( Host host )
		{
			Host = host;
			Host.Commands.Add( "togglemenu", ToggleMenu_f );
			InitialiseMenus();
		}

		public override void Add( String key, BaseMenu item )
		{
			base.Add( key, item );

			// Automatically setup command
			Host.Commands.Add( key, Generic_Menu_f );
		}

		public void Show( String name )
		{
			var menu = Get( name );

			if ( menu != null )
				menu.Show( Host );
		}

		public void SetActive( BaseMenu menu )
		{
			CurrentMenu = menu;
		}

		private void Generic_Menu_f( CommandMessage msg )
		{
			var menuName = msg.Name;
			Show( menuName );
		}

		/// <summary>
		/// M_Keydown
		/// </summary>
		public void KeyDown( Int32 key )
		{
			CurrentMenu?.KeyEvent( key );
		}

		/// <summary>
		/// M_Draw
		/// </summary>
		public void Draw( )
		{
			if ( CurrentMenu == null || Host.Keyboard.Destination != KeyDestination.key_menu )
				return;

			if ( !_RecursiveDraw )
			{
				Host.Screen.CopyEverithing = true;

				if ( Host.Screen.Elements.Get<VisualConsole>( ElementFactory.CONSOLE )?.ConCurrent > 0 )
				{
					Host.Console.DrawConsoleBackground( Host.Screen.vid.height );
					Host.Sound.ExtraUpdate();
				}
				else
					Host.DrawingContext.FadeScreen();

				Host.Screen.FullUpdate = 0;
			}
			else
			{
				_RecursiveDraw = false;
			}

			CurrentMenu?.Draw();

			if ( EnterSound )
			{
				Host.Sound.LocalSound( "misc/menu2.wav" );
				EnterSound = false;
			}

			Host.Sound.ExtraUpdate();
		}

		/// <summary>
		/// M_ToggleMenu_f
		/// </summary>
		public void ToggleMenu_f( CommandMessage msg )
		{
			EnterSound = true;

			if ( Host.Keyboard.Destination == KeyDestination.key_menu )
			{
				if ( CurrentMenu.Name != "menu_menu" )
				{
					Show( "menu_main" );
					return;
				}
				CurrentMenu.Hide();
				return;
			}
			if ( Host.Keyboard.Destination == KeyDestination.key_console )
			{
				Host.Console.ToggleConsole_f( null );
			}
			else
			{
				Show( "menu_main" );
			}
		}

		public void DrawPic( Int32 x, Int32 y, BasePicture pic )
		{
			Host.Video.Device.Graphics.DrawPicture( pic, x + ( ( Host.Screen.vid.width - 320 ) >> 1 ), y );
		}

		public void DrawTransPic( Int32 x, Int32 y, BasePicture pic )
		{
			Host.Video.Device.Graphics.DrawPicture( pic, x + ( ( Host.Screen.vid.width - 320 ) >> 1 ), y, hasAlpha: true );
		}

		/// <summary>
		/// M_DrawTransPicTranslate
		/// </summary>
		public void DrawTransPicTranslate( Int32 x, Int32 y, BasePicture pic )
		{
			Host.DrawingContext.TransPicTranslate( x + ( ( Host.Screen.vid.width - 320 ) >> 1 ), y, pic, _TranslationTable );
		}

		/// <summary>
		/// M_Print
		/// </summary>
		public void Print( Int32 cx, Int32 cy, String str )
		{
			for ( var i = 0; i < str.Length; i++ )
			{
				DrawCharacter( cx, cy, str[i] + 128 );
				cx += 8;
			}
		}

		/// <summary>
		/// M_DrawCharacter
		/// </summary>
		public void DrawCharacter( Int32 cx, Int32 line, Int32 num )
		{
			Host.DrawingContext.DrawCharacter( cx + ( ( Host.Screen.vid.width - 320 ) >> 1 ), line, num );
		}

		/// <summary>
		/// M_PrintWhite
		/// </summary>
		public void PrintWhite( Int32 cx, Int32 cy, String str )
		{
			for ( var i = 0; i < str.Length; i++ )
			{
				DrawCharacter( cx, cy, str[i] );
				cx += 8;
			}
		}

		/// <summary>
		/// M_DrawTextBox
		/// </summary>
		public void DrawTextBox( Int32 x, Int32 y, Int32 width, Int32 lines )
		{
			// draw left side
			var cx = x;
			var cy = y;
			var p = Host.Pictures.Cache( "gfx/box_tl.lmp", "GL_NEAREST" );
			DrawTransPic( cx, cy, p );
			p = Host.Pictures.Cache( "gfx/box_ml.lmp", "GL_NEAREST" );
			for ( var n = 0; n < lines; n++ )
			{
				cy += 8;
				DrawTransPic( cx, cy, p );
			}
			p = Host.Pictures.Cache( "gfx/box_bl.lmp", "GL_NEAREST" );
			DrawTransPic( cx, cy + 8, p );

			// draw middle
			cx += 8;
			while ( width > 0 )
			{
				cy = y;
				p = Host.Pictures.Cache( "gfx/box_tm.lmp", "GL_NEAREST" );
				DrawTransPic( cx, cy, p );
				p = Host.Pictures.Cache( "gfx/box_mm.lmp", "GL_NEAREST" );
				for ( var n = 0; n < lines; n++ )
				{
					cy += 8;
					if ( n == 1 )
						p = Host.Pictures.Cache( "gfx/box_mm2.lmp", "GL_NEAREST" );
					DrawTransPic( cx, cy, p );
				}
				p = Host.Pictures.Cache( "gfx/box_bm.lmp", "GL_NEAREST" );
				DrawTransPic( cx, cy + 8, p );
				width -= 2;
				cx += 16;
			}

			// draw right side
			cy = y;
			p = Host.Pictures.Cache( "gfx/box_tr.lmp", "GL_NEAREST" );
			DrawTransPic( cx, cy, p );
			p = Host.Pictures.Cache( "gfx/box_mr.lmp", "GL_NEAREST" );
			for ( var n = 0; n < lines; n++ )
			{
				cy += 8;
				DrawTransPic( cx, cy, p );
			}
			p = Host.Pictures.Cache( "gfx/box_br.lmp", "GL_NEAREST" );
			DrawTransPic( cx, cy + 8, p );
		}

		/// <summary>
		/// M_DrawSlider
		/// </summary>
		public void DrawSlider( Int32 x, Int32 y, Single range )
		{
			if ( range < 0 )
				range = 0;
			if ( range > 1 )
				range = 1;
			DrawCharacter( x - 8, y, 128 );
			Int32 i;
			for ( i = 0; i < SLIDER_RANGE; i++ )
				DrawCharacter( x + i * 8, y, 129 );
			DrawCharacter( x + i * 8, y, 130 );
			DrawCharacter( ( Int32 ) ( x + ( SLIDER_RANGE - 1 ) * 8 * range ), y, 131 );
		}

		/// <summary>
		/// M_DrawCheckbox
		/// </summary>
		public void DrawCheckbox( Int32 x, Int32 y, Boolean on )
		{
			if ( on )
				Print( x, y, "on" );
			else
				Print( x, y, "off" );
		}

		/// <summary>
		/// M_BuildTranslationTable
		/// </summary>
		public void BuildTranslationTable( Int32 top, Int32 bottom )
		{
			for ( var j = 0; j < 256; j++ )
				_IdentityTable[j] = ( Byte ) j;

			_IdentityTable.CopyTo( _TranslationTable, 0 );

			if ( top < 128 )    // the artists made some backwards ranges.  sigh.
				Array.Copy( _IdentityTable, top, _TranslationTable, render.TOP_RANGE, 16 ); // memcpy (dest + Render.TOP_RANGE, source + top, 16);
			else
				for ( var j = 0; j < 16; j++ )
					_TranslationTable[render.TOP_RANGE + j] = _IdentityTable[top + 15 - j];

			if ( bottom < 128 )
				Array.Copy( _IdentityTable, bottom, _TranslationTable, render.BOTTOM_RANGE, 16 ); // memcpy(dest + Render.BOTTOM_RANGE, source + bottom, 16);
			else
				for ( var j = 0; j < 16; j++ )
					_TranslationTable[render.BOTTOM_RANGE + j] = _IdentityTable[bottom + 15 - j];
		}
	}
}
