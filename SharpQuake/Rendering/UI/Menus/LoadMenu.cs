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
using SharpQuake.Framework.IO;
using System;
using System.IO;
using System.Text;

namespace SharpQuake.Rendering.UI
{
	public class LoadMenu : BaseMenu
	{
		public const Int32 MAX_SAVEGAMES = 12;
		protected String[] _FileNames; //[MAX_SAVEGAMES]; // filenames
		protected Boolean[] _Loadable; //[MAX_SAVEGAMES]; // loadable

		public LoadMenu( String name, MenuFactory menuFactory ) : base( name, menuFactory )
		{
		}

		public override void Show( Host host )
		{
			base.Show( host );
			ScanSaves();
		}

		public override void KeyEvent( Int32 key )
		{
			switch ( key )
			{
				case KeysDef.K_ESCAPE:
					MenuFactory.Show( "menu_singleplayer" );
					break;

				case KeysDef.K_ENTER:
					Host.Sound.LocalSound( "misc/menu2.wav" );
					if ( !_Loadable[Cursor] )
						return;
					MenuFactory.CurrentMenu.Hide();

					// Host_Loadgame_f can't bring up the loading plaque because too much
					// stack space has been used, so do it now
					Host.Screen.BeginLoadingPlaque();

					// issue the load command
					Host.Commands.Buffer.Append( String.Format( "load s{0}\n", Cursor ) );
					return;

				case KeysDef.K_UPARROW:
				case KeysDef.K_LEFTARROW:
					Host.Sound.LocalSound( "misc/menu1.wav" );
					Cursor--;
					if ( Cursor < 0 )
						Cursor = MAX_SAVEGAMES - 1;
					break;

				case KeysDef.K_DOWNARROW:
				case KeysDef.K_RIGHTARROW:
					Host.Sound.LocalSound( "misc/menu1.wav" );
					Cursor++;
					if ( Cursor >= MAX_SAVEGAMES )
						Cursor = 0;
					break;
			}
		}

		public override void Draw( )
		{
			var p = Host.DrawingContext.CachePic( "gfx/p_load.lmp", "GL_NEAREST" );
			Host.Menus.DrawPic( ( 320 - p.Width ) / 2, 4, p );

			for ( var i = 0; i < MAX_SAVEGAMES; i++ )
				Host.Menus.Print( 16, 32 + 8 * i, _FileNames[i] );

			// line cursor
			Host.Menus.DrawCharacter( 8, 32 + Cursor * 8, 12 + ( ( Int32 ) ( Host.RealTime * 4 ) & 1 ) );
		}

		/// <summary>
		/// M_ScanSaves
		/// </summary>
		protected void ScanSaves( )
		{
			for ( var i = 0; i < MAX_SAVEGAMES; i++ )
			{
				_FileNames[i] = "--- UNUSED SLOT ---";
				_Loadable[i] = false;
				var name = String.Format( "{0}/s{1}.sav", FileSystem.GameDir, i );
				var fs = FileSystem.OpenRead( name );
				if ( fs == null )
					continue;

				using ( var reader = new StreamReader( fs, Encoding.ASCII ) )
				{
					var version = reader.ReadLine();
					if ( version == null )
						continue;
					var info = reader.ReadLine();
					if ( info == null )
						continue;
					info = info.TrimEnd( '\0', '_' ).Replace( '_', ' ' );
					if ( !String.IsNullOrEmpty( info ) )
					{
						_FileNames[i] = info;
						_Loadable[i] = true;
					}
				}
			}
		}

		public LoadMenu( MenuFactory menuFactory ) : base( "menu_load", menuFactory )
		{
			_FileNames = new String[MAX_SAVEGAMES];
			_Loadable = new Boolean[MAX_SAVEGAMES];
		}
	}
}
