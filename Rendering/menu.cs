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

// menu.h
// menu.c

namespace SharpQuake
{
    /// <summary>
    /// M_functions
    /// </summary>
    internal static class menu
    {
        public static Boolean EnterSound;
        public static Boolean ReturnOnError;
        public static String ReturnReason;
        public static MenuBase ReturnMenu;
        private const Int32 SLIDER_RANGE = 10;

        //qboolean	m_entersound	// play after drawing a frame, so caching

        // won't disrupt the sound
        private static Boolean _RecursiveDraw; // qboolean m_recursiveDraw

        private static Byte[] _IdentityTable = new Byte[256]; // identityTable
        private static Byte[] _TranslationTable = new Byte[256]; //translationTable

        // M_Init (void)
        public static void Init()
        {
            Command.Add( "togglemenu", ToggleMenu_f );
            Command.Add( "menu_main", Menu_Main_f );
            Command.Add( "menu_singleplayer", Menu_SinglePlayer_f );
            Command.Add( "menu_load", Menu_Load_f );
            Command.Add( "menu_save", Menu_Save_f );
            Command.Add( "menu_multiplayer", Menu_MultiPlayer_f );
            Command.Add( "menu_setup", Menu_Setup_f );
            Command.Add( "menu_options", Menu_Options_f );
            Command.Add( "menu_keys", Menu_Keys_f );
            Command.Add( "menu_video", Menu_Video_f );
            Command.Add( "help", Menu_Help_f );
            Command.Add( "menu_quit", Menu_Quit_f );
        }

        /// <summary>
        /// M_Keydown
        /// </summary>
        public static void KeyDown( Int32 key )
        {
            if( MenuBase.CurrentMenu != null )
                MenuBase.CurrentMenu.KeyEvent( key );
        }

        /// <summary>
        /// M_Draw
        /// </summary>
        public static void Draw()
        {
            if( MenuBase.CurrentMenu == null || Key.Destination != keydest_t.key_menu )
                return;

            if( !_RecursiveDraw )
            {
                Scr.CopyEverithing = true;

                if( Scr.ConCurrent > 0 )
                {
                    Drawer.DrawConsoleBackground( Scr.vid.height );
                    snd.ExtraUpdate();
                }
                else
                    Drawer.FadeScreen();

                Scr.FullUpdate = 0;
            }
            else
            {
                _RecursiveDraw = false;
            }

            if( MenuBase.CurrentMenu != null )
                MenuBase.CurrentMenu.Draw();

            if( EnterSound )
            {
                snd.LocalSound( "misc/menu2.wav" );
                EnterSound = false;
            }

            snd.ExtraUpdate();
        }

        /// <summary>
        /// M_ToggleMenu_f
        /// </summary>
        public static void ToggleMenu_f()
        {
            EnterSound = true;

            if( Key.Destination == keydest_t.key_menu )
            {
                if( MenuBase.CurrentMenu != MenuBase.MainMenu )
                {
                    MenuBase.MainMenu.Show();
                    return;
                }
                MenuBase.Hide();
                return;
            }
            if( Key.Destination == keydest_t.key_console )
            {
                Con.ToggleConsole_f();
            }
            else
            {
                MenuBase.MainMenu.Show();
            }
        }

        public static void DrawPic( Int32 x, Int32 y, glpic_t pic )
        {
            Drawer.DrawPic( x + ( ( Scr.vid.width - 320 ) >> 1 ), y, pic );
        }

        public static void DrawTransPic( Int32 x, Int32 y, glpic_t pic )
        {
            Drawer.DrawTransPic( x + ( ( Scr.vid.width - 320 ) >> 1 ), y, pic );
        }

        /// <summary>
        /// M_DrawTransPicTranslate
        /// </summary>
        public static void DrawTransPicTranslate( Int32 x, Int32 y, glpic_t pic )
        {
            Drawer.TransPicTranslate( x + ( ( Scr.vid.width - 320 ) >> 1 ), y, pic, _TranslationTable );
        }

        /// <summary>
        /// M_Print
        /// </summary>
        public static void Print( Int32 cx, Int32 cy, String str )
        {
            for( var i = 0; i < str.Length; i++ )
            {
                DrawCharacter( cx, cy, str[i] + 128 );
                cx += 8;
            }
        }

        /// <summary>
        /// M_DrawCharacter
        /// </summary>
        public static void DrawCharacter( Int32 cx, Int32 line, Int32 num )
        {
            Drawer.DrawCharacter( cx + ( ( Scr.vid.width - 320 ) >> 1 ), line, num );
        }

        /// <summary>
        /// M_PrintWhite
        /// </summary>
        public static void PrintWhite( Int32 cx, Int32 cy, String str )
        {
            for( var i = 0; i < str.Length; i++ )
            {
                DrawCharacter( cx, cy, str[i] );
                cx += 8;
            }
        }

        /// <summary>
        /// M_DrawTextBox
        /// </summary>
        public static void DrawTextBox( Int32 x, Int32 y, Int32 width, Int32 lines )
        {
            // draw left side
            var cx = x;
            var cy = y;
            glpic_t p = Drawer.CachePic( "gfx/box_tl.lmp" );
            DrawTransPic( cx, cy, p );
            p = Drawer.CachePic( "gfx/box_ml.lmp" );
            for( var n = 0; n < lines; n++ )
            {
                cy += 8;
                DrawTransPic( cx, cy, p );
            }
            p = Drawer.CachePic( "gfx/box_bl.lmp" );
            DrawTransPic( cx, cy + 8, p );

            // draw middle
            cx += 8;
            while( width > 0 )
            {
                cy = y;
                p = Drawer.CachePic( "gfx/box_tm.lmp" );
                DrawTransPic( cx, cy, p );
                p = Drawer.CachePic( "gfx/box_mm.lmp" );
                for( var n = 0; n < lines; n++ )
                {
                    cy += 8;
                    if( n == 1 )
                        p = Drawer.CachePic( "gfx/box_mm2.lmp" );
                    DrawTransPic( cx, cy, p );
                }
                p = Drawer.CachePic( "gfx/box_bm.lmp" );
                DrawTransPic( cx, cy + 8, p );
                width -= 2;
                cx += 16;
            }

            // draw right side
            cy = y;
            p = Drawer.CachePic( "gfx/box_tr.lmp" );
            DrawTransPic( cx, cy, p );
            p = Drawer.CachePic( "gfx/box_mr.lmp" );
            for( var n = 0; n < lines; n++ )
            {
                cy += 8;
                DrawTransPic( cx, cy, p );
            }
            p = Drawer.CachePic( "gfx/box_br.lmp" );
            DrawTransPic( cx, cy + 8, p );
        }

        /// <summary>
        /// M_DrawSlider
        /// </summary>
        public static void DrawSlider( Int32 x, Int32 y, Single range )
        {
            if( range < 0 )
                range = 0;
            if( range > 1 )
                range = 1;
            DrawCharacter( x - 8, y, 128 );
            Int32 i;
            for( i = 0; i < SLIDER_RANGE; i++ )
                DrawCharacter( x + i * 8, y, 129 );
            DrawCharacter( x + i * 8, y, 130 );
            DrawCharacter( ( Int32 ) ( x + ( SLIDER_RANGE - 1 ) * 8 * range ), y, 131 );
        }

        /// <summary>
        /// M_DrawCheckbox
        /// </summary>
        public static void DrawCheckbox( Int32 x, Int32 y, Boolean on )
        {
            if( on )
                Print( x, y, "on" );
            else
                Print( x, y, "off" );
        }

        /// <summary>
        /// M_BuildTranslationTable
        /// </summary>
        public static void BuildTranslationTable( Int32 top, Int32 bottom )
        {
            for( var j = 0; j < 256; j++ )
                _IdentityTable[j] = ( Byte ) j;

            _IdentityTable.CopyTo( _TranslationTable, 0 );

            if( top < 128 )	// the artists made some backwards ranges.  sigh.
                Array.Copy( _IdentityTable, top, _TranslationTable, render.TOP_RANGE, 16 ); // memcpy (dest + Render.TOP_RANGE, source + top, 16);
            else
                for( var j = 0; j < 16; j++ )
                    _TranslationTable[render.TOP_RANGE + j] = _IdentityTable[top + 15 - j];

            if( bottom < 128 )
                Array.Copy( _IdentityTable, bottom, _TranslationTable, render.BOTTOM_RANGE, 16 ); // memcpy(dest + Render.BOTTOM_RANGE, source + bottom, 16);
            else
                for( var j = 0; j < 16; j++ )
                    _TranslationTable[render.BOTTOM_RANGE + j] = _IdentityTable[bottom + 15 - j];
        }

        /// <summary>
        /// M_Menu_Main_f
        /// </summary>
        private static void Menu_Main_f()
        {
            MenuBase.MainMenu.Show();
        }

        /// <summary>
        /// M_Menu_SinglePlayer_f
        /// </summary>
        private static void Menu_SinglePlayer_f()
        {
            MenuBase.SinglePlayerMenu.Show();
        }

        // M_Menu_Load_f
        private static void Menu_Load_f()
        {
            MenuBase.LoadMenu.Show();
        }

        // M_Menu_Save_f
        private static void Menu_Save_f()
        {
            MenuBase.SaveMenu.Show();
        }

        // M_Menu_MultiPlayer_f
        private static void Menu_MultiPlayer_f()
        {
            MenuBase.MultiPlayerMenu.Show();
        }

        // M_Menu_Setup_f
        private static void Menu_Setup_f()
        {
            MenuBase.SetupMenu.Show();
        }

        // M_Menu_Options_f
        private static void Menu_Options_f()
        {
            MenuBase.OptionsMenu.Show();
        }

        /// <summary>
        /// M_Menu_Keys_f
        /// </summary>
        private static void Menu_Keys_f()
        {
            MenuBase.KeysMenu.Show();
        }

        /// <summary>
        /// M_Menu_Video_f
        /// </summary>
        private static void Menu_Video_f()
        {
            MenuBase.VideoMenu.Show();
        }

        /// <summary>
        /// M_Menu_Help_f
        /// </summary>
        private static void Menu_Help_f()
        {
            MenuBase.HelpMenu.Show();
        }

        /// <summary>
        /// M_Menu_Quit_f
        /// </summary>
        private static void Menu_Quit_f()
        {
            MenuBase.QuitMenu.Show();
        }
    }

    internal abstract class MenuBase
    {
        public static MenuBase CurrentMenu
        {
            get
            {
                return _CurrentMenu;
            }
        }

        public Int32 Cursor
        {
            get
            {
                return _Cursor;
            }
        }

        // Top level menu items
        public static readonly MenuBase MainMenu = new MainMenu();

        public static readonly MenuBase SinglePlayerMenu = new SinglePlayerMenu();
        public static readonly MenuBase MultiPlayerMenu = new MultiPleerMenu();
        public static readonly MenuBase OptionsMenu = new OptionsMenu();
        public static readonly MenuBase HelpMenu = new HelpMenu();
        public static readonly MenuBase QuitMenu = new QuitMenu();
        public static readonly MenuBase LoadMenu = new LoadMenu();
        public static readonly MenuBase SaveMenu = new SaveMenu();

        // Submenus
        public static readonly MenuBase KeysMenu = new KeysMenu();

        public static readonly MenuBase LanConfigMenu = new LanConfigMenu();
        public static readonly MenuBase SetupMenu = new SetupMenu();
        public static readonly MenuBase GameOptionsMenu = new GameOptionsMenu();
        public static readonly MenuBase SearchMenu = new SearchMenu();
        public static readonly MenuBase ServerListMenu = new ServerListMenu();
        public static readonly MenuBase VideoMenu = new VideoMenu();
        protected Int32 _Cursor;
        private static MenuBase _CurrentMenu;

        public static void Hide()
        {
            Key.Destination = keydest_t.key_game;
            _CurrentMenu = null;
        }

        public virtual void Show()
        {
            menu.EnterSound = true;
            Key.Destination = keydest_t.key_menu;
            _CurrentMenu = this;
        }

        public abstract void KeyEvent( Int32 key );

        public abstract void Draw();
    }

    /// <summary>
    /// MainMenu
    /// </summary>
    internal class MainMenu : MenuBase
    {
        private const Int32 MAIN_ITEMS = 5;
        private Int32 _SaveDemoNum;

        public override void Show()
        {
            if( Key.Destination != keydest_t.key_menu )
            {
                _SaveDemoNum = client.cls.demonum;
                client.cls.demonum = -1;
            }

            base.Show();
        }

        /// <summary>
        /// M_Main_Key
        /// </summary>
        public override void KeyEvent( Int32 key )
        {
            switch( key )
            {
                case Key.K_ESCAPE:
                    //Key.Destination = keydest_t.key_game;
                    MenuBase.Hide();
                    client.cls.demonum = _SaveDemoNum;
                    if( client.cls.demonum != -1 && !client.cls.demoplayback && client.cls.state != cactive_t.ca_connected )
                        client.NextDemo();
                    break;

                case Key.K_DOWNARROW:
                    snd.LocalSound( "misc/menu1.wav" );
                    if( ++_Cursor >= MAIN_ITEMS )
                        _Cursor = 0;
                    break;

                case Key.K_UPARROW:
                    snd.LocalSound( "misc/menu1.wav" );
                    if( --_Cursor < 0 )
                        _Cursor = MAIN_ITEMS - 1;
                    break;

                case Key.K_ENTER:
                    menu.EnterSound = true;

                    switch( _Cursor )
                    {
                        case 0:
                            MenuBase.SinglePlayerMenu.Show();
                            break;

                        case 1:
                            MenuBase.MultiPlayerMenu.Show();
                            break;

                        case 2:
                            MenuBase.OptionsMenu.Show();
                            break;

                        case 3:
                            MenuBase.HelpMenu.Show();
                            break;

                        case 4:
                            MenuBase.QuitMenu.Show();
                            break;
                    }
                    break;
            }
        }

        public override void Draw()
        {
            menu.DrawTransPic( 16, 4, Drawer.CachePic( "gfx/qplaque.lmp" ) );
            glpic_t p = Drawer.CachePic( "gfx/ttl_main.lmp" );
            menu.DrawPic( ( 320 - p.width ) / 2, 4, p );
            menu.DrawTransPic( 72, 32, Drawer.CachePic( "gfx/mainmenu.lmp" ) );

            var f = ( Int32 ) ( host.Time * 10 ) % 6;

            menu.DrawTransPic( 54, 32 + _Cursor * 20, Drawer.CachePic( String.Format( "gfx/menudot{0}.lmp", f + 1 ) ) );
        }
    }

    internal class SinglePlayerMenu : MenuBase
    {
        private const Int32 SINGLEPLAYER_ITEMS = 3;

        /// <summary>
        /// M_SinglePlayer_Key
        /// </summary>
        public override void KeyEvent( Int32 key )
        {
            switch( key )
            {
                case Key.K_ESCAPE:
                    MenuBase.MainMenu.Show();
                    break;

                case Key.K_DOWNARROW:
                    snd.LocalSound( "misc/menu1.wav" );
                    if( ++_Cursor >= SINGLEPLAYER_ITEMS )
                        _Cursor = 0;
                    break;

                case Key.K_UPARROW:
                    snd.LocalSound( "misc/menu1.wav" );
                    if( --_Cursor < 0 )
                        _Cursor = SINGLEPLAYER_ITEMS - 1;
                    break;

                case Key.K_ENTER:
                    menu.EnterSound = true;

                    switch( _Cursor )
                    {
                        case 0:
                            if( server.sv.active )
                                if( !Scr.ModalMessage( "Are you sure you want to\nstart a new game?\n" ) )
                                    break;
                            Key.Destination = keydest_t.key_game;
                            if( server.sv.active )
                                Cbuf.AddText( "disconnect\n" );
                            Cbuf.AddText( "maxplayers 1\n" );
                            Cbuf.AddText( "map start\n" );
                            break;

                        case 1:
                            MenuBase.LoadMenu.Show();
                            break;

                        case 2:
                            MenuBase.SaveMenu.Show();
                            break;
                    }
                    break;
            }
        }

        /// <summary>
        /// M_SinglePlayer_Draw
        /// </summary>
        public override void Draw()
        {
            menu.DrawTransPic( 16, 4, Drawer.CachePic( "gfx/qplaque.lmp" ) );
            glpic_t p = Drawer.CachePic( "gfx/ttl_sgl.lmp" );
            menu.DrawPic( ( 320 - p.width ) / 2, 4, p );
            menu.DrawTransPic( 72, 32, Drawer.CachePic( "gfx/sp_menu.lmp" ) );

            var f = ( Int32 ) ( host.Time * 10 ) % 6;

            menu.DrawTransPic( 54, 32 + _Cursor * 20, Drawer.CachePic( String.Format( "gfx/menudot{0}.lmp", f + 1 ) ) );
        }
    }

    internal class LoadMenu : MenuBase
    {
        public const Int32 MAX_SAVEGAMES = 12;
        protected String[] _FileNames; //[MAX_SAVEGAMES]; // filenames
        protected Boolean[] _Loadable; //[MAX_SAVEGAMES]; // loadable

        public override void Show()
        {
            base.Show();
            ScanSaves();
        }

        public override void KeyEvent( Int32 key )
        {
            switch( key )
            {
                case Key.K_ESCAPE:
                    MenuBase.SinglePlayerMenu.Show();
                    break;

                case Key.K_ENTER:
                    snd.LocalSound( "misc/menu2.wav" );
                    if( !_Loadable[_Cursor] )
                        return;
                    MenuBase.Hide();

                    // Host_Loadgame_f can't bring up the loading plaque because too much
                    // stack space has been used, so do it now
                    Scr.BeginLoadingPlaque();

                    // issue the load command
                    Cbuf.AddText( String.Format( "load s{0}\n", _Cursor ) );
                    return;

                case Key.K_UPARROW:
                case Key.K_LEFTARROW:
                    snd.LocalSound( "misc/menu1.wav" );
                    _Cursor--;
                    if( _Cursor < 0 )
                        _Cursor = MAX_SAVEGAMES - 1;
                    break;

                case Key.K_DOWNARROW:
                case Key.K_RIGHTARROW:
                    snd.LocalSound( "misc/menu1.wav" );
                    _Cursor++;
                    if( _Cursor >= MAX_SAVEGAMES )
                        _Cursor = 0;
                    break;
            }
        }

        public override void Draw()
        {
            glpic_t p = Drawer.CachePic( "gfx/p_load.lmp" );
            menu.DrawPic( ( 320 - p.width ) / 2, 4, p );

            for( var i = 0; i < MAX_SAVEGAMES; i++ )
                menu.Print( 16, 32 + 8 * i, _FileNames[i] );

            // line cursor
            menu.DrawCharacter( 8, 32 + _Cursor * 8, 12 + ( ( Int32 ) ( host.RealTime * 4 ) & 1 ) );
        }

        /// <summary>
        /// M_ScanSaves
        /// </summary>
        protected void ScanSaves()
        {
            for( var i = 0; i < MAX_SAVEGAMES; i++ )
            {
                _FileNames[i] = "--- UNUSED SLOT ---";
                _Loadable[i] = false;
                var name = String.Format( "{0}/s{1}.sav", Common.GameDir, i );
                FileStream fs = FileSystem.OpenRead( name );
                if( fs == null )
                    continue;

                using( StreamReader reader = new StreamReader( fs, Encoding.ASCII ) )
                {
                    var version = reader.ReadLine();
                    if( version == null )
                        continue;
                    var info = reader.ReadLine();
                    if( info == null )
                        continue;
                    info = info.TrimEnd( '\0', '_' ).Replace( '_', ' ' );
                    if( !String.IsNullOrEmpty( info ) )
                    {
                        _FileNames[i] = info;
                        _Loadable[i] = true;
                    }
                }
            }
        }

        public LoadMenu()
        {
            _FileNames = new String[MAX_SAVEGAMES];
            _Loadable = new Boolean[MAX_SAVEGAMES];
        }
    }

    internal class SaveMenu : LoadMenu
    {
        public override void Show()
        {
            if( !server.sv.active )
                return;
            if( client.cl.intermission != 0 )
                return;
            if( server.svs.maxclients != 1 )
                return;

            base.Show();
        }

        public override void KeyEvent( Int32 key )
        {
            switch( key )
            {
                case Key.K_ESCAPE:
                    MenuBase.SinglePlayerMenu.Show();
                    break;

                case Key.K_ENTER:
                    MenuBase.Hide();
                    Cbuf.AddText( String.Format( "save s{0}\n", _Cursor ) );
                    return;

                case Key.K_UPARROW:
                case Key.K_LEFTARROW:
                    snd.LocalSound( "misc/menu1.wav" );
                    _Cursor--;
                    if( _Cursor < 0 )
                        _Cursor = MAX_SAVEGAMES - 1;
                    break;

                case Key.K_DOWNARROW:
                case Key.K_RIGHTARROW:
                    snd.LocalSound( "misc/menu1.wav" );
                    _Cursor++;
                    if( _Cursor >= MAX_SAVEGAMES )
                        _Cursor = 0;
                    break;
            }
        }

        public override void Draw()
        {
            glpic_t p = Drawer.CachePic( "gfx/p_save.lmp" );
            menu.DrawPic( ( 320 - p.width ) / 2, 4, p );

            for( var i = 0; i < MAX_SAVEGAMES; i++ )
                menu.Print( 16, 32 + 8 * i, _FileNames[i] );

            // line cursor
            menu.DrawCharacter( 8, 32 + _Cursor * 8, 12 + ( ( Int32 ) ( host.RealTime * 4 ) & 1 ) );
        }
    }

    internal class QuitMenu : MenuBase
    {
        private MenuBase _PrevMenu; // m_quit_prevstate;

        public override void Show()
        {
            if( CurrentMenu == this )
                return;

            Key.Destination = keydest_t.key_menu;
            _PrevMenu = CurrentMenu;

            base.Show();
        }

        public override void KeyEvent( Int32 key )
        {
            switch( key )
            {
                case Key.K_ESCAPE:
                case 'n':
                case 'N':
                    if( _PrevMenu != null )
                        _PrevMenu.Show();
                    else
                        MenuBase.Hide();
                    break;

                case 'Y':
                case 'y':
                    Key.Destination = keydest_t.key_console;
                    host.Quit_f();
                    break;

                default:
                    break;
            }
        }

        public override void Draw()
        {
            menu.DrawTextBox( 0, 0, 38, 23 );
            menu.PrintWhite( 16, 12, "  Quake version 1.09 by id Software\n\n" );
            menu.PrintWhite( 16, 28, "Programming        Art \n" );
            menu.Print( 16, 36, " John Carmack       Adrian Carmack\n" );
            menu.Print( 16, 44, " Michael Abrash     Kevin Cloud\n" );
            menu.Print( 16, 52, " John Cash          Paul Steed\n" );
            menu.Print( 16, 60, " Dave 'Zoid' Kirsch\n" );
            menu.PrintWhite( 16, 68, "Design             Biz\n" );
            menu.Print( 16, 76, " John Romero        Jay Wilbur\n" );
            menu.Print( 16, 84, " Sandy Petersen     Mike Wilson\n" );
            menu.Print( 16, 92, " American McGee     Donna Jackson\n" );
            menu.Print( 16, 100, " Tim Willits        Todd Hollenshead\n" );
            menu.PrintWhite( 16, 108, "Support            Projects\n" );
            menu.Print( 16, 116, " Barrett Alexander  Shawn Green\n" );
            menu.PrintWhite( 16, 124, "Sound Effects\n" );
            menu.Print( 16, 132, " Trent Reznor and Nine Inch Nails\n\n" );
            menu.PrintWhite( 16, 140, "Quake is a trademark of Id Software,\n" );
            menu.PrintWhite( 16, 148, "inc., (c)1996 Id Software, inc. All\n" );
            menu.PrintWhite( 16, 156, "rights reserved. NIN logo is a\n" );
            menu.PrintWhite( 16, 164, "registered trademark licensed to\n" );
            menu.PrintWhite( 16, 172, "Nothing Interactive, Inc. All rights\n" );
            menu.PrintWhite( 16, 180, "reserved. Press y to exit\n" );
        }
    }

    internal class HelpMenu : MenuBase
    {
        private const Int32 NUM_HELP_PAGES = 6;

        private Int32 _Page;

        public override void Show()
        {
            _Page = 0;
            base.Show();
        }

        public override void KeyEvent( Int32 key )
        {
            switch( key )
            {
                case Key.K_ESCAPE:
                    MenuBase.MainMenu.Show();
                    break;

                case Key.K_UPARROW:
                case Key.K_RIGHTARROW:
                    menu.EnterSound = true;
                    if( ++_Page >= NUM_HELP_PAGES )
                        _Page = 0;
                    break;

                case Key.K_DOWNARROW:
                case Key.K_LEFTARROW:
                    menu.EnterSound = true;
                    if( --_Page < 0 )
                        _Page = NUM_HELP_PAGES - 1;
                    break;
            }
        }

        public override void Draw()
        {
            menu.DrawPic( 0, 0, Drawer.CachePic( String.Format( "gfx/help{0}.lmp", _Page ) ) );
        }
    }

    internal class OptionsMenu : MenuBase
    {
        private const Int32 OPTIONS_ITEMS = 13;

        //private float _BgmVolumeCoeff = 0.1f;

        public override void Show()
        {
           /*if( sys.IsWindows )  fix cd audio first
            {
                _BgmVolumeCoeff = 1.0f;
            }*/

            if( _Cursor > OPTIONS_ITEMS - 1 )
                _Cursor = 0;

            if( _Cursor == OPTIONS_ITEMS - 1 && MenuBase.VideoMenu == null )
                _Cursor = 0;

            base.Show();
        }

        public override void KeyEvent( Int32 key )
        {
            switch( key )
            {
                case Key.K_ESCAPE:
                    MenuBase.MainMenu.Show();
                    break;

                case Key.K_ENTER:
                    menu.EnterSound = true;
                    switch( _Cursor )
                    {
                        case 0:
                            MenuBase.KeysMenu.Show();
                            break;

                        case 1:
                            MenuBase.Hide();
                            Con.ToggleConsole_f();
                            break;

                        case 2:
                            Cbuf.AddText( "exec default.cfg\n" );
                            break;

                        case 12:
                            MenuBase.VideoMenu.Show();
                            break;

                        default:
                            AdjustSliders( 1 );
                            break;
                    }
                    return;

                case Key.K_UPARROW:
                    snd.LocalSound( "misc/menu1.wav" );
                    _Cursor--;
                    if( _Cursor < 0 )
                        _Cursor = OPTIONS_ITEMS - 1;
                    break;

                case Key.K_DOWNARROW:
                    snd.LocalSound( "misc/menu1.wav" );
                    _Cursor++;
                    if( _Cursor >= OPTIONS_ITEMS )
                        _Cursor = 0;
                    break;

                case Key.K_LEFTARROW:
                    AdjustSliders( -1 );
                    break;

                case Key.K_RIGHTARROW:
                    AdjustSliders( 1 );
                    break;
            }

            /*if( _Cursor == 12 && VideoMenu == null )
            {
                if( key == Key.K_UPARROW )
                    _Cursor = 11;
                else
                    _Cursor = 0;
            }*/

            if (_Cursor == 12)
            {
                if (key == Key.K_UPARROW)
                    _Cursor = 11;
                else
                    _Cursor = 0;
            }

/*#if _WIN32
            if ((options_cursor == 13) && (modestate != MS_WINDOWED))
            {
                if (k == K_UPARROW)
                    options_cursor = 12;
                else
                    options_cursor = 0;
            }
#endif*/
            }

        public override void Draw()
        {
            menu.DrawTransPic( 16, 4, Drawer.CachePic( "gfx/qplaque.lmp" ) );
            glpic_t p = Drawer.CachePic( "gfx/p_option.lmp" );
            menu.DrawPic( ( 320 - p.width ) / 2, 4, p );

            menu.Print( 16, 32, "    Customize controls" );
            menu.Print( 16, 40, "         Go to console" );
            menu.Print( 16, 48, "     Reset to defaults" );

            menu.Print( 16, 56, "           Screen size" );
            var r = ( Scr.ViewSize.Value - 30 ) / ( 120 - 30 );
            menu.DrawSlider( 220, 56, r );

            menu.Print( 16, 64, "            Brightness" );
            r = ( 1.0f - view.Gamma ) / 0.5f;
            menu.DrawSlider( 220, 64, r );

            menu.Print( 16, 72, "           Mouse Speed" );
            r = ( client.Sensitivity - 1 ) / 10;
            menu.DrawSlider( 220, 72, r );

            menu.Print( 16, 80, "       CD Music Volume" );
            r = snd.BgmVolume;
            menu.DrawSlider( 220, 80, r );

            menu.Print( 16, 88, "          Sound Volume" );
            r = snd.Volume;
            menu.DrawSlider( 220, 88, r );

            menu.Print( 16, 96, "            Always Run" );
            menu.DrawCheckbox( 220, 96, client.ForwardSpeed > 200 );

            menu.Print( 16, 104, "          Invert Mouse" );
            menu.DrawCheckbox( 220, 104, client.MPitch < 0 );

            menu.Print( 16, 112, "            Lookspring" );
            menu.DrawCheckbox( 220, 112, client.LookSpring );

            menu.Print( 16, 120, "            Lookstrafe" );
            menu.DrawCheckbox( 220, 120, client.LookStrafe );

            /*if( VideoMenu != null )
                Menu.Print( 16, 128, "         Video Options" );*/

#if _WIN32
	if (modestate == MS_WINDOWED)
	{
		Menu.Print (16, 136, "             Use Mouse");
		Menu.DrawCheckbox (220, 136, _windowed_mouse.value);
	}
#endif

            // cursor
            menu.DrawCharacter( 200, 32 + _Cursor * 8, 12 + ( ( Int32 ) ( host.RealTime * 4 ) & 1 ) );
        }

        /// <summary>
        /// M_AdjustSliders
        /// </summary>
        private void AdjustSliders( Int32 dir )
        {
            snd.LocalSound( "misc/menu3.wav" );
            Single value;

            switch( _Cursor )
            {
                case 3:	// screen size
                    value = Scr.ViewSize.Value + dir * 10;
                    if( value < 30 )
                        value = 30;
                    if( value > 120 )
                        value = 120;
                    CVar.Set( "viewsize", value );
                    break;

                case 4:	// gamma
                    value = view.Gamma - dir * 0.05f;
                    if( value < 0.5 )
                        value = 0.5f;
                    if( value > 1 )
                        value = 1;
                    CVar.Set( "gamma", value );
                    break;

                case 5:	// mouse speed
                    value = client.Sensitivity + dir * 0.5f;
                    if( value < 1 )
                        value = 1;
                    if( value > 11 )
                        value = 11;
                    CVar.Set( "sensitivity", value );
                    break;

                case 6:	// music volume
                    value = snd.BgmVolume + dir * 0.1f; ///_BgmVolumeCoeff;
                    if( value < 0 )
                        value = 0;
                    if( value > 1 )
                        value = 1;
                    CVar.Set( "bgmvolume", value );
                    break;

                case 7:	// sfx volume
                    value = snd.Volume + dir * 0.1f;
                    if( value < 0 )
                        value = 0;
                    if( value > 1 )
                        value = 1;
                    CVar.Set( "volume", value );
                    break;

                case 8:	// allways run
                    if( client.ForwardSpeed > 200 )
                    {
                        CVar.Set( "cl_forwardspeed", 200f );
                        CVar.Set( "cl_backspeed", 200f );
                    }
                    else
                    {
                        CVar.Set( "cl_forwardspeed", 400f );
                        CVar.Set( "cl_backspeed", 400f );
                    }
                    break;

                case 9:	// invert mouse
                    CVar.Set( "m_pitch", -client.MPitch );
                    break;

                case 10:	// lookspring
                    CVar.Set( "lookspring", !client.LookSpring ? 1f : 0f );
                    break;

                case 11:	// lookstrafe
                    CVar.Set( "lookstrafe", !client.LookStrafe ? 1f : 0f );
                    break;

#if _WIN32
	        case 13:	// _windowed_mouse
		        Cvar_SetValue ("_windowed_mouse", !_windowed_mouse.value);
		        break;
#endif
            }
        }
    }

    internal class KeysMenu : MenuBase
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

        public override void Show()
        {
            base.Show();
        }

        public override void KeyEvent( Int32 key )
        {
            if( _BindGrab )
            {
                // defining a key
                snd.LocalSound( "misc/menu1.wav" );
                if( key == Key.K_ESCAPE )
                {
                    _BindGrab = false;
                }
                else if( key != '`' )
                {
                    var cmd = String.Format( "bind \"{0}\" \"{1}\"\n", Key.KeynumToString( key ), _BindNames[_Cursor][0] );
                    Cbuf.InsertText( cmd );
                }

                _BindGrab = false;
                return;
            }

            switch( key )
            {
                case Key.K_ESCAPE:
                    MenuBase.OptionsMenu.Show();
                    break;

                case Key.K_LEFTARROW:
                case Key.K_UPARROW:
                    snd.LocalSound( "misc/menu1.wav" );
                    _Cursor--;
                    if( _Cursor < 0 )
                        _Cursor = _BindNames.Length - 1;
                    break;

                case Key.K_DOWNARROW:
                case Key.K_RIGHTARROW:
                    snd.LocalSound( "misc/menu1.wav" );
                    _Cursor++;
                    if( _Cursor >= _BindNames.Length )
                        _Cursor = 0;
                    break;

                case Key.K_ENTER:		// go into bind mode
                    Int32[] keys = new Int32[2];
                    FindKeysForCommand( _BindNames[_Cursor][0], keys );
                    snd.LocalSound( "misc/menu2.wav" );
                    if( keys[1] != -1 )
                        UnbindCommand( _BindNames[_Cursor][0] );
                    _BindGrab = true;
                    break;

                case Key.K_BACKSPACE:		// delete bindings
                case Key.K_DEL:				// delete bindings
                    snd.LocalSound( "misc/menu2.wav" );
                    UnbindCommand( _BindNames[_Cursor][0] );
                    break;
            }
        }

        public override void Draw()
        {
            glpic_t p = Drawer.CachePic( "gfx/ttl_cstm.lmp" );
            menu.DrawPic( ( 320 - p.width ) / 2, 4, p );

            if( _BindGrab )
                menu.Print( 12, 32, "Press a key or button for this action" );
            else
                menu.Print( 18, 32, "Enter to change, backspace to clear" );

            // search for known bindings
            Int32[] keys = new Int32[2];

            for( var i = 0; i < _BindNames.Length; i++ )
            {
                var y = 48 + 8 * i;

                menu.Print( 16, y, _BindNames[i][1] );

                FindKeysForCommand( _BindNames[i][0], keys );

                if( keys[0] == -1 )
                {
                    menu.Print( 140, y, "???" );
                }
                else
                {
                    var name = Key.KeynumToString( keys[0] );
                    menu.Print( 140, y, name );
                    var x = name.Length * 8;
                    if( keys[1] != -1 )
                    {
                        menu.Print( 140 + x + 8, y, "or" );
                        menu.Print( 140 + x + 32, y, Key.KeynumToString( keys[1] ) );
                    }
                }
            }

            if( _BindGrab )
                menu.DrawCharacter( 130, 48 + _Cursor * 8, '=' );
            else
                menu.DrawCharacter( 130, 48 + _Cursor * 8, 12 + ( ( Int32 ) ( host.RealTime * 4 ) & 1 ) );
        }

        /// <summary>
        /// M_FindKeysForCommand
        /// </summary>
        private void FindKeysForCommand( String command, Int32[] twokeys )
        {
            twokeys[0] = twokeys[1] = -1;
            var len = command.Length;
            var count = 0;

            for( var j = 0; j < 256; j++ )
            {
                var b = Key.Bindings[j];
                if( String.IsNullOrEmpty( b ) )
                    continue;

                if( String.Compare( b, 0, command, 0, len ) == 0 )
                {
                    twokeys[count] = j;
                    count++;
                    if( count == 2 )
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

            for( var j = 0; j < 256; j++ )
            {
                var b = Key.Bindings[j];
                if( String.IsNullOrEmpty( b ) )
                    continue;

                if( String.Compare( b, 0, command, 0, len ) == 0 )
                    Key.SetBinding( j, String.Empty );
            }
        }
    }

    internal class MultiPleerMenu : MenuBase
    {
        private const Int32 MULTIPLAYER_ITEMS = 3;

        public override void KeyEvent( Int32 key )
        {
            switch( key )
            {
                case Key.K_ESCAPE:
                    MenuBase.MainMenu.Show();
                    break;

                case Key.K_DOWNARROW:
                    snd.LocalSound( "misc/menu1.wav" );
                    if( ++_Cursor >= MULTIPLAYER_ITEMS )
                        _Cursor = 0;
                    break;

                case Key.K_UPARROW:
                    snd.LocalSound( "misc/menu1.wav" );
                    if( --_Cursor < 0 )
                        _Cursor = MULTIPLAYER_ITEMS - 1;
                    break;

                case Key.K_ENTER:
                    menu.EnterSound = true;
                    switch( _Cursor )
                    {
                        case 0:
                            if( net.TcpIpAvailable )
                                MenuBase.LanConfigMenu.Show();
                            break;

                        case 1:
                            if( net.TcpIpAvailable )
                                MenuBase.LanConfigMenu.Show();
                            break;

                        case 2:
                            MenuBase.SetupMenu.Show();
                            break;
                    }
                    break;
            }
        }

        public override void Draw()
        {
            menu.DrawTransPic( 16, 4, Drawer.CachePic( "gfx/qplaque.lmp" ) );
            glpic_t p = Drawer.CachePic( "gfx/p_multi.lmp" );
            menu.DrawPic( ( 320 - p.width ) / 2, 4, p );
            menu.DrawTransPic( 72, 32, Drawer.CachePic( "gfx/mp_menu.lmp" ) );

            Single f = ( Int32 ) ( host.Time * 10 ) % 6;

            menu.DrawTransPic( 54, 32 + _Cursor * 20, Drawer.CachePic( String.Format( "gfx/menudot{0}.lmp", f + 1 ) ) );

            if( net.TcpIpAvailable )
                return;
            menu.PrintWhite( ( 320 / 2 ) - ( ( 27 * 8 ) / 2 ), 148, "No Communications Available" );
        }
    }

    /// <summary>
    /// M_Menu_LanConfig_functions
    /// </summary>
    internal class LanConfigMenu : MenuBase
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

        public override void Show()
        {
            base.Show();

            if( _Cursor == -1 )
            {
                if( JoiningGame )
                    _Cursor = 2;
                else
                    _Cursor = 1;
            }
            if( StartingGame && _Cursor == 2 )
                _Cursor = 1;
            _Port = net.DefaultHostPort;
            _PortName = _Port.ToString();

            menu.ReturnOnError = false;
            menu.ReturnReason = String.Empty;
        }

        public override void KeyEvent( Int32 key )
        {
            switch( key )
            {
                case Key.K_ESCAPE:
                    MenuBase.MultiPlayerMenu.Show();
                    break;

                case Key.K_UPARROW:
                    snd.LocalSound( "misc/menu1.wav" );
                    _Cursor--;
                    if( _Cursor < 0 )
                        _Cursor = NUM_LANCONFIG_CMDS - 1;
                    break;

                case Key.K_DOWNARROW:
                    snd.LocalSound( "misc/menu1.wav" );
                    _Cursor++;
                    if( _Cursor >= NUM_LANCONFIG_CMDS )
                        _Cursor = 0;
                    break;

                case Key.K_ENTER:
                    if( _Cursor == 0 )
                        break;

                    menu.EnterSound = true;
                    net.HostPort = _Port;

                    if( _Cursor == 1 )
                    {
                        if( StartingGame )
                        {
                            MenuBase.GameOptionsMenu.Show();
                        }
                        else
                        {
                            MenuBase.SearchMenu.Show();
                        }
                        break;
                    }

                    if( _Cursor == 2 )
                    {
                        menu.ReturnMenu = this;
                        menu.ReturnOnError = true;
                        MenuBase.Hide();
                        Cbuf.AddText( String.Format( "connect \"{0}\"\n", _JoinName ) );
                        break;
                    }
                    break;

                case Key.K_BACKSPACE:
                    if( _Cursor == 0 )
                    {
                        if( !String.IsNullOrEmpty( _PortName ) )
                            _PortName = _PortName.Substring( 0, _PortName.Length - 1 );
                    }

                    if( _Cursor == 2 )
                    {
                        if( !String.IsNullOrEmpty( _JoinName ) )
                            _JoinName = _JoinName.Substring( 0, _JoinName.Length - 1 );
                    }
                    break;

                default:
                    if( key < 32 || key > 127 )
                        break;

                    if( _Cursor == 2 )
                    {
                        if( _JoinName.Length < 21 )
                            _JoinName += ( Char ) key;
                    }

                    if( key < '0' || key > '9' )
                        break;

                    if( _Cursor == 0 )
                    {
                        if( _PortName.Length < 5 )
                            _PortName += ( Char ) key;
                    }
                    break;
            }

            if( StartingGame && _Cursor == 2 )
                if( key == Key.K_UPARROW )
                    _Cursor = 1;
                else
                    _Cursor = 0;

            var k = Common.atoi( _PortName );
            if( k > 65535 )
                k = _Port;
            else
                _Port = k;
            _PortName = _Port.ToString();
        }

        public override void Draw()
        {
            menu.DrawTransPic( 16, 4, Drawer.CachePic( "gfx/qplaque.lmp" ) );
            glpic_t p = Drawer.CachePic( "gfx/p_multi.lmp" );
            var basex = ( 320 - p.width ) / 2;
            menu.DrawPic( basex, 4, p );

            String startJoin;
            if( StartingGame )
                startJoin = "New Game - TCP/IP";
            else
                startJoin = "Join Game - TCP/IP";

            menu.Print( basex, 32, startJoin );
            basex += 8;

            menu.Print( basex, 52, "Address:" );
            menu.Print( basex + 9 * 8, 52, net.MyTcpIpAddress );

            menu.Print( basex, _CursorTable[0], "Port" );
            menu.DrawTextBox( basex + 8 * 8, _CursorTable[0] - 8, 6, 1 );
            menu.Print( basex + 9 * 8, _CursorTable[0], _PortName );

            if( JoiningGame )
            {
                menu.Print( basex, _CursorTable[1], "Search for local games..." );
                menu.Print( basex, 108, "Join game at:" );
                menu.DrawTextBox( basex + 8, _CursorTable[2] - 8, 22, 1 );
                menu.Print( basex + 16, _CursorTable[2], _JoinName );
            }
            else
            {
                menu.DrawTextBox( basex, _CursorTable[1] - 8, 2, 1 );
                menu.Print( basex + 8, _CursorTable[1], "OK" );
            }

            menu.DrawCharacter( basex - 8, _CursorTable[_Cursor], 12 + ( ( Int32 ) ( host.RealTime * 4 ) & 1 ) );

            if( _Cursor == 0 )
                menu.DrawCharacter( basex + 9 * 8 + 8 * _PortName.Length,
                    _CursorTable[0], 10 + ( ( Int32 ) ( host.RealTime * 4 ) & 1 ) );

            if( _Cursor == 2 )
                menu.DrawCharacter( basex + 16 + 8 * _JoinName.Length, _CursorTable[2],
                    10 + ( ( Int32 ) ( host.RealTime * 4 ) & 1 ) );

            if( !String.IsNullOrEmpty( menu.ReturnReason ) )
                menu.PrintWhite( basex, 148, menu.ReturnReason );
        }

        public LanConfigMenu()
        {
            _Cursor = -1;
            _JoinName = String.Empty;
        }
    }

    internal class SetupMenu : MenuBase
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
        public override void Show()
        {
            _MyName = client.Name;
            _HostName = net.HostName;
            _Top = _OldTop = ( ( Int32 ) client.Color ) >> 4;
            _Bottom = _OldBottom = ( ( Int32 ) client.Color ) & 15;

            base.Show();
        }

        public override void KeyEvent( Int32 key )
        {
            switch( key )
            {
                case Key.K_ESCAPE:
                    MenuBase.MultiPlayerMenu.Show();
                    break;

                case Key.K_UPARROW:
                    snd.LocalSound( "misc/menu1.wav" );
                    _Cursor--;
                    if( _Cursor < 0 )
                        _Cursor = NUM_SETUP_CMDS - 1;
                    break;

                case Key.K_DOWNARROW:
                    snd.LocalSound( "misc/menu1.wav" );
                    _Cursor++;
                    if( _Cursor >= NUM_SETUP_CMDS )
                        _Cursor = 0;
                    break;

                case Key.K_LEFTARROW:
                    if( _Cursor < 2 )
                        return;
                    snd.LocalSound( "misc/menu3.wav" );
                    if( _Cursor == 2 )
                        _Top = _Top - 1;
                    if( _Cursor == 3 )
                        _Bottom = _Bottom - 1;
                    break;

                case Key.K_RIGHTARROW:
                    if( _Cursor < 2 )
                        return;
forward:
                    snd.LocalSound( "misc/menu3.wav" );
                    if( _Cursor == 2 )
                        _Top = _Top + 1;
                    if( _Cursor == 3 )
                        _Bottom = _Bottom + 1;
                    break;

                case Key.K_ENTER:
                    if( _Cursor == 0 || _Cursor == 1 )
                        return;

                    if( _Cursor == 2 || _Cursor == 3 )
                        goto forward;

                    // _Cursor == 4 (OK)
                    if( _MyName != client.Name )
                        Cbuf.AddText( String.Format( "name \"{0}\"\n", _MyName ) );
                    if( net.HostName != _HostName )
                        CVar.Set( "hostname", _HostName );
                    if( _Top != _OldTop || _Bottom != _OldBottom )
                        Cbuf.AddText( String.Format( "color {0} {1}\n", _Top, _Bottom ) );
                    menu.EnterSound = true;
                    MenuBase.MultiPlayerMenu.Show();
                    break;

                case Key.K_BACKSPACE:
                    if( _Cursor == 0 )
                    {
                        if( !String.IsNullOrEmpty( _HostName ) )
                            _HostName = _HostName.Substring( 0, _HostName.Length - 1 );// setup_hostname[strlen(setup_hostname) - 1] = 0;
                    }

                    if( _Cursor == 1 )
                    {
                        if( !String.IsNullOrEmpty( _MyName ) )
                            _MyName = _MyName.Substring( 0, _MyName.Length - 1 );
                    }
                    break;

                default:
                    if( key < 32 || key > 127 )
                        break;
                    if( _Cursor == 0 )
                    {
                        var l = _HostName.Length;
                        if( l < 15 )
                        {
                            _HostName = _HostName + ( Char ) key;
                        }
                    }
                    if( _Cursor == 1 )
                    {
                        var l = _MyName.Length;
                        if( l < 15 )
                        {
                            _MyName = _MyName + ( Char ) key;
                        }
                    }
                    break;
            }

            if( _Top > 13 )
                _Top = 0;
            if( _Top < 0 )
                _Top = 13;
            if( _Bottom > 13 )
                _Bottom = 0;
            if( _Bottom < 0 )
                _Bottom = 13;
        }

        public override void Draw()
        {
            menu.DrawTransPic( 16, 4, Drawer.CachePic( "gfx/qplaque.lmp" ) );
            glpic_t p = Drawer.CachePic( "gfx/p_multi.lmp" );
            menu.DrawPic( ( 320 - p.width ) / 2, 4, p );

            menu.Print( 64, 40, "Hostname" );
            menu.DrawTextBox( 160, 32, 16, 1 );
            menu.Print( 168, 40, _HostName );

            menu.Print( 64, 56, "Your name" );
            menu.DrawTextBox( 160, 48, 16, 1 );
            menu.Print( 168, 56, _MyName );

            menu.Print( 64, 80, "Shirt color" );
            menu.Print( 64, 104, "Pants color" );

            menu.DrawTextBox( 64, 140 - 8, 14, 1 );
            menu.Print( 72, 140, "Accept Changes" );

            p = Drawer.CachePic( "gfx/bigbox.lmp" );
            menu.DrawTransPic( 160, 64, p );
            p = Drawer.CachePic( "gfx/menuplyr.lmp" );
            menu.BuildTranslationTable( _Top * 16, _Bottom * 16 );
            menu.DrawTransPicTranslate( 172, 72, p );

            menu.DrawCharacter( 56, _CursorTable[_Cursor], 12 + ( ( Int32 ) ( host.RealTime * 4 ) & 1 ) );

            if( _Cursor == 0 )
                menu.DrawCharacter( 168 + 8 * _HostName.Length, _CursorTable[_Cursor], 10 + ( ( Int32 ) ( host.RealTime * 4 ) & 1 ) );

            if( _Cursor == 1 )
                menu.DrawCharacter( 168 + 8 * _MyName.Length, _CursorTable[_Cursor], 10 + ( ( Int32 ) ( host.RealTime * 4 ) & 1 ) );
        }
    }

    /// <summary>
    /// M_Menu_GameOptions_functions
    /// </summary>
    internal class GameOptionsMenu : MenuBase
    {
        private const Int32 NUM_GAMEOPTIONS = 9;

        private static readonly level_t[] Levels = new level_t[]
        {
            new level_t("start", "Entrance"),	// 0

	        new level_t("e1m1", "Slipgate Complex"),				// 1
	        new level_t("e1m2", "Castle of the Damned"),
            new level_t("e1m3", "The Necropolis"),
            new level_t("e1m4", "The Grisly Grotto"),
            new level_t("e1m5", "Gloom Keep"),
            new level_t("e1m6", "The Door To Chthon"),
            new level_t("e1m7", "The House of Chthon"),
            new level_t("e1m8", "Ziggurat Vertigo"),

            new level_t("e2m1", "The Installation"),				// 9
	        new level_t("e2m2", "Ogre Citadel"),
            new level_t("e2m3", "Crypt of Decay"),
            new level_t("e2m4", "The Ebon Fortress"),
            new level_t("e2m5", "The Wizard's Manse"),
            new level_t("e2m6", "The Dismal Oubliette"),
            new level_t("e2m7", "Underearth"),

            new level_t("e3m1", "Termination Central"),			// 16
	        new level_t("e3m2", "The Vaults of Zin"),
            new level_t("e3m3", "The Tomb of Terror"),
            new level_t("e3m4", "Satan's Dark Delight"),
            new level_t("e3m5", "Wind Tunnels"),
            new level_t("e3m6", "Chambers of Torment"),
            new level_t("e3m7", "The Haunted Halls"),

            new level_t("e4m1", "The Sewage System"),				// 23
	        new level_t("e4m2", "The Tower of Despair"),
            new level_t("e4m3", "The Elder God Shrine"),
            new level_t("e4m4", "The Palace of Hate"),
            new level_t("e4m5", "Hell's Atrium"),
            new level_t("e4m6", "The Pain Maze"),
            new level_t("e4m7", "Azure Agony"),
            new level_t("e4m8", "The Nameless City"),

            new level_t("end", "Shub-Niggurath's Pit"),			// 31

	        new level_t("dm1", "Place of Two Deaths"),				// 32
	        new level_t("dm2", "Claustrophobopolis"),
            new level_t("dm3", "The Abandoned Base"),
            new level_t("dm4", "The Bad Place"),
            new level_t("dm5", "The Cistern"),
            new level_t("dm6", "The Dark Zone")
        };

        //MED 01/06/97 added hipnotic levels
        private static readonly level_t[] HipnoticLevels = new level_t[]
        {
           new level_t("start", "Command HQ"),  // 0

           new level_t("hip1m1", "The Pumping Station"),          // 1
           new level_t("hip1m2", "Storage Facility"),
           new level_t("hip1m3", "The Lost Mine"),
           new level_t("hip1m4", "Research Facility"),
           new level_t("hip1m5", "Military Complex"),

           new level_t("hip2m1", "Ancient Realms"),          // 6
           new level_t("hip2m2", "The Black Cathedral"),
           new level_t("hip2m3", "The Catacombs"),
           new level_t("hip2m4", "The Crypt"),
           new level_t("hip2m5", "Mortum's Keep"),
           new level_t("hip2m6", "The Gremlin's Domain"),

           new level_t("hip3m1", "Tur Torment"),       // 12
           new level_t("hip3m2", "Pandemonium"),
           new level_t("hip3m3", "Limbo"),
           new level_t("hip3m4", "The Gauntlet"),

           new level_t("hipend", "Armagon's Lair"),       // 16

           new level_t("hipdm1", "The Edge of Oblivion")           // 17
        };

        //PGM 01/07/97 added rogue levels
        //PGM 03/02/97 added dmatch level
        private static readonly level_t[] RogueLevels = new level_t[]
        {
            new level_t("start", "Split Decision"),
            new level_t("r1m1", "Deviant's Domain"),
            new level_t("r1m2", "Dread Portal"),
            new level_t("r1m3", "Judgement Call"),
            new level_t("r1m4", "Cave of Death"),
            new level_t("r1m5", "Towers of Wrath"),
            new level_t("r1m6", "Temple of Pain"),
            new level_t("r1m7", "Tomb of the Overlord"),
            new level_t("r2m1", "Tempus Fugit"),
            new level_t("r2m2", "Elemental Fury I"),
            new level_t("r2m3", "Elemental Fury II"),
            new level_t("r2m4", "Curse of Osiris"),
            new level_t("r2m5", "Wizard's Keep"),
            new level_t("r2m6", "Blood Sacrifice"),
            new level_t("r2m7", "Last Bastion"),
            new level_t("r2m8", "Source of Evil"),
            new level_t("ctf1", "Division of Change")
        };

        private static readonly episode_t[] Episodes = new episode_t[]
        {
            new episode_t("Welcome to Quake", 0, 1),
            new episode_t("Doomed Dimension", 1, 8),
            new episode_t("Realm of Black Magic", 9, 7),
            new episode_t("Netherworld", 16, 7),
            new episode_t("The Elder World", 23, 8),
            new episode_t("Final Level", 31, 1),
            new episode_t("Deathmatch Arena", 32, 6)
        };

        //MED 01/06/97  added hipnotic episodes
        private static readonly episode_t[] HipnoticEpisodes = new episode_t[]
        {
           new episode_t("Scourge of Armagon", 0, 1),
           new episode_t("Fortress of the Dead", 1, 5),
           new episode_t("Dominion of Darkness", 6, 6),
           new episode_t("The Rift", 12, 4),
           new episode_t("Final Level", 16, 1),
           new episode_t("Deathmatch Arena", 17, 1)
        };

        //PGM 01/07/97 added rogue episodes
        //PGM 03/02/97 added dmatch episode
        private static readonly episode_t[] RogueEpisodes = new episode_t[]
        {
            new episode_t("Introduction", 0, 1),
            new episode_t("Hell's Fortress", 1, 7),
            new episode_t("Corridors of Time", 8, 8),
            new episode_t("Deathmatch Arena", 16, 1)
        };

        private static readonly Int32[] _CursorTable = new Int32[]
        {
            40, 56, 64, 72, 80, 88, 96, 112, 120
        };

        private Int32 _StartEpisode;

        private Int32 _StartLevel;

        private Int32 _MaxPlayers;

        private Boolean _ServerInfoMessage;

        private Double _ServerInfoMessageTime;

        public override void Show()
        {
            base.Show();

            if( _MaxPlayers == 0 )
                _MaxPlayers = server.svs.maxclients;
            if( _MaxPlayers < 2 )
                _MaxPlayers = server.svs.maxclientslimit;
        }

        public override void KeyEvent( Int32 key )
        {
            switch( key )
            {
                case Key.K_ESCAPE:
                    MenuBase.LanConfigMenu.Show();
                    break;

                case Key.K_UPARROW:
                    snd.LocalSound( "misc/menu1.wav" );
                    _Cursor--;
                    if( _Cursor < 0 )
                        _Cursor = NUM_GAMEOPTIONS - 1;
                    break;

                case Key.K_DOWNARROW:
                    snd.LocalSound( "misc/menu1.wav" );
                    _Cursor++;
                    if( _Cursor >= NUM_GAMEOPTIONS )
                        _Cursor = 0;
                    break;

                case Key.K_LEFTARROW:
                    if( _Cursor == 0 )
                        break;
                    snd.LocalSound( "misc/menu3.wav" );
                    Change( -1 );
                    break;

                case Key.K_RIGHTARROW:
                    if( _Cursor == 0 )
                        break;
                    snd.LocalSound( "misc/menu3.wav" );
                    Change( 1 );
                    break;

                case Key.K_ENTER:
                    snd.LocalSound( "misc/menu2.wav" );
                    if( _Cursor == 0 )
                    {
                        if( server.IsActive )
                            Cbuf.AddText( "disconnect\n" );
                        Cbuf.AddText( "listen 0\n" );	// so host_netport will be re-examined
                        Cbuf.AddText( String.Format( "maxplayers {0}\n", _MaxPlayers ) );
                        Scr.BeginLoadingPlaque();

                        if( Common.GameKind == GameKind.Hipnotic )
                            Cbuf.AddText( String.Format( "map {0}\n",
                                HipnoticLevels[HipnoticEpisodes[_StartEpisode].firstLevel + _StartLevel].name ) );
                        else if( Common.GameKind == GameKind.Rogue )
                            Cbuf.AddText( String.Format( "map {0}\n",
                                RogueLevels[RogueEpisodes[_StartEpisode].firstLevel + _StartLevel].name ) );
                        else
                            Cbuf.AddText( String.Format( "map {0}\n", Levels[Episodes[_StartEpisode].firstLevel + _StartLevel].name ) );

                        return;
                    }

                    Change( 1 );
                    break;
            }
        }

        public override void Draw()
        {
            menu.DrawTransPic( 16, 4, Drawer.CachePic( "gfx/qplaque.lmp" ) );
            glpic_t p = Drawer.CachePic( "gfx/p_multi.lmp" );
            menu.DrawPic( ( 320 - p.width ) / 2, 4, p );

            menu.DrawTextBox( 152, 32, 10, 1 );
            menu.Print( 160, 40, "begin game" );

            menu.Print( 0, 56, "      Max players" );
            menu.Print( 160, 56, _MaxPlayers.ToString() );

            menu.Print( 0, 64, "        Game Type" );
            if( host.IsCoop )
                menu.Print( 160, 64, "Cooperative" );
            else
                menu.Print( 160, 64, "Deathmatch" );

            menu.Print( 0, 72, "        Teamplay" );
            if( Common.GameKind == GameKind.Rogue )
            {
                String msg;
                switch( ( Int32 ) host.TeamPlay )
                {
                    case 1:
                        msg = "No Friendly Fire";
                        break;

                    case 2:
                        msg = "Friendly Fire";
                        break;

                    case 3:
                        msg = "Tag";
                        break;

                    case 4:
                        msg = "Capture the Flag";
                        break;

                    case 5:
                        msg = "One Flag CTF";
                        break;

                    case 6:
                        msg = "Three Team CTF";
                        break;

                    default:
                        msg = "Off";
                        break;
                }
                menu.Print( 160, 72, msg );
            }
            else
            {
                String msg;
                switch( ( Int32 ) host.TeamPlay )
                {
                    case 1:
                        msg = "No Friendly Fire";
                        break;

                    case 2:
                        msg = "Friendly Fire";
                        break;

                    default:
                        msg = "Off";
                        break;
                }
                menu.Print( 160, 72, msg );
            }

            menu.Print( 0, 80, "            Skill" );
            if( host.Skill == 0 )
                menu.Print( 160, 80, "Easy difficulty" );
            else if( host.Skill == 1 )
                menu.Print( 160, 80, "Normal difficulty" );
            else if( host.Skill == 2 )
                menu.Print( 160, 80, "Hard difficulty" );
            else
                menu.Print( 160, 80, "Nightmare difficulty" );

            menu.Print( 0, 88, "       Frag Limit" );
            if( host.FragLimit == 0 )
                menu.Print( 160, 88, "none" );
            else
                menu.Print( 160, 88, String.Format( "{0} frags", ( Int32 ) host.FragLimit ) );

            menu.Print( 0, 96, "       Time Limit" );
            if( host.TimeLimit == 0 )
                menu.Print( 160, 96, "none" );
            else
                menu.Print( 160, 96, String.Format( "{0} minutes", ( Int32 ) host.TimeLimit ) );

            menu.Print( 0, 112, "         Episode" );
            //MED 01/06/97 added hipnotic episodes
            if( Common.GameKind == GameKind.Hipnotic )
                menu.Print( 160, 112, HipnoticEpisodes[_StartEpisode].description );
            //PGM 01/07/97 added rogue episodes
            else if( Common.GameKind == GameKind.Rogue )
                menu.Print( 160, 112, RogueEpisodes[_StartEpisode].description );
            else
                menu.Print( 160, 112, Episodes[_StartEpisode].description );

            menu.Print( 0, 120, "           Level" );
            //MED 01/06/97 added hipnotic episodes
            if( Common.GameKind == GameKind.Hipnotic )
            {
                menu.Print( 160, 120, HipnoticLevels[HipnoticEpisodes[_StartEpisode].firstLevel + _StartLevel].description );
                menu.Print( 160, 128, HipnoticLevels[HipnoticEpisodes[_StartEpisode].firstLevel + _StartLevel].name );
            }
            //PGM 01/07/97 added rogue episodes
            else if( Common.GameKind == GameKind.Rogue )
            {
                menu.Print( 160, 120, RogueLevels[RogueEpisodes[_StartEpisode].firstLevel + _StartLevel].description );
                menu.Print( 160, 128, RogueLevels[RogueEpisodes[_StartEpisode].firstLevel + _StartLevel].name );
            }
            else
            {
                menu.Print( 160, 120, Levels[Episodes[_StartEpisode].firstLevel + _StartLevel].description );
                menu.Print( 160, 128, Levels[Episodes[_StartEpisode].firstLevel + _StartLevel].name );
            }

            // line cursor
            menu.DrawCharacter( 144, _CursorTable[_Cursor], 12 + ( ( Int32 ) ( host.RealTime * 4 ) & 1 ) );

            if( _ServerInfoMessage )
            {
                if( ( host.RealTime - _ServerInfoMessageTime ) < 5.0 )
                {
                    var x = ( 320 - 26 * 8 ) / 2;
                    menu.DrawTextBox( x, 138, 24, 4 );
                    x += 8;
                    menu.Print( x, 146, "  More than 4 players   " );
                    menu.Print( x, 154, " requires using command " );
                    menu.Print( x, 162, "line parameters; please " );
                    menu.Print( x, 170, "   see techinfo.txt.    " );
                }
                else
                {
                    _ServerInfoMessage = false;
                }
            }
        }

        private class level_t
        {
            public String name;
            public String description;

            public level_t( String name, String desc )
            {
                this.name = name;
                this.description = desc;
            }
        } //level_t;

        private class episode_t
        {
            public String description;
            public Int32 firstLevel;
            public Int32 levels;

            public episode_t( String desc, Int32 firstLevel, Int32 levels )
            {
                this.description = desc;
                this.firstLevel = firstLevel;
                this.levels = levels;
            }
        } //episode_t;

        /// <summary>
        /// M_NetStart_Change
        /// </summary>
        private void Change( Int32 dir )
        {
            Int32 count;

            switch( _Cursor )
            {
                case 1:
                    _MaxPlayers += dir;
                    if( _MaxPlayers > server.svs.maxclientslimit )
                    {
                        _MaxPlayers = server.svs.maxclientslimit;
                        _ServerInfoMessage = true;
                        _ServerInfoMessageTime = host.RealTime;
                    }
                    if( _MaxPlayers < 2 )
                        _MaxPlayers = 2;
                    break;

                case 2:
                    CVar.Set( "coop", host.IsCoop ? 0 : 1 );
                    break;

                case 3:
                    if( Common.GameKind == GameKind.Rogue )
                        count = 6;
                    else
                        count = 2;

                    var tp = host.TeamPlay + dir;
                    if( tp > count )
                        tp = 0;
                    else if( tp < 0 )
                        tp = count;

                    CVar.Set( "teamplay", tp );
                    break;

                case 4:
                    var skill = host.Skill + dir;
                    if( skill > 3 )
                        skill = 0;
                    if( skill < 0 )
                        skill = 3;
                    CVar.Set( "skill", skill );
                    break;

                case 5:
                    var fraglimit = host.FragLimit + dir * 10;
                    if( fraglimit > 100 )
                        fraglimit = 0;
                    if( fraglimit < 0 )
                        fraglimit = 100;
                    CVar.Set( "fraglimit", fraglimit );
                    break;

                case 6:
                    var timelimit = host.TimeLimit + dir * 5;
                    if( timelimit > 60 )
                        timelimit = 0;
                    if( timelimit < 0 )
                        timelimit = 60;
                    CVar.Set( "timelimit", timelimit );
                    break;

                case 7:
                    _StartEpisode += dir;
                    //MED 01/06/97 added hipnotic count
                    if( Common.GameKind == GameKind.Hipnotic )
                        count = 6;
                    //PGM 01/07/97 added rogue count
                    //PGM 03/02/97 added 1 for dmatch episode
                    else if( Common.GameKind == GameKind.Rogue )
                        count = 4;
                    else if( Common.IsRegistered )
                        count = 7;
                    else
                        count = 2;

                    if( _StartEpisode < 0 )
                        _StartEpisode = count - 1;

                    if( _StartEpisode >= count )
                        _StartEpisode = 0;

                    _StartLevel = 0;
                    break;

                case 8:
                    _StartLevel += dir;
                    //MED 01/06/97 added hipnotic episodes
                    if( Common.GameKind == GameKind.Hipnotic )
                        count = HipnoticEpisodes[_StartEpisode].levels;
                    //PGM 01/06/97 added hipnotic episodes
                    else if( Common.GameKind == GameKind.Rogue )
                        count = RogueEpisodes[_StartEpisode].levels;
                    else
                        count = Episodes[_StartEpisode].levels;

                    if( _StartLevel < 0 )
                        _StartLevel = count - 1;

                    if( _StartLevel >= count )
                        _StartLevel = 0;
                    break;
            }
        }
    }

    internal class SearchMenu : MenuBase
    {
        private Boolean _SearchComplete;
        private Double _SearchCompleteTime;

        public override void Show()
        {
            base.Show();
            net.SlistSilent = true;
            net.SlistLocal = false;
            _SearchComplete = false;
            net.Slist_f();
        }

        public override void KeyEvent( Int32 key )
        {
            // nothing to do
        }

        public override void Draw()
        {
            glpic_t p = Drawer.CachePic( "gfx/p_multi.lmp" );
            menu.DrawPic( ( 320 - p.width ) / 2, 4, p );
            var x = ( 320 / 2 ) - ( ( 12 * 8 ) / 2 ) + 4;
            menu.DrawTextBox( x - 8, 32, 12, 1 );
            menu.Print( x, 40, "Searching..." );

            if( net.SlistInProgress )
            {
                net.Poll();
                return;
            }

            if( !_SearchComplete )
            {
                _SearchComplete = true;
                _SearchCompleteTime = host.RealTime;
            }

            if( net.HostCacheCount > 0 )
            {
                MenuBase.ServerListMenu.Show();
                return;
            }

            menu.PrintWhite( ( 320 / 2 ) - ( ( 22 * 8 ) / 2 ), 64, "No Quake servers found" );
            if( ( host.RealTime - _SearchCompleteTime ) < 3.0 )
                return;

            MenuBase.LanConfigMenu.Show();
        }
    }

    internal class ServerListMenu : MenuBase
    {
        private Boolean _Sorted;

        public override void Show()
        {
            base.Show();
            _Cursor = 0;
            menu.ReturnOnError = false;
            menu.ReturnReason = String.Empty;
            _Sorted = false;
        }

        public override void KeyEvent( Int32 key )
        {
            switch( key )
            {
                case Key.K_ESCAPE:
                    MenuBase.LanConfigMenu.Show();
                    break;

                case Key.K_SPACE:
                    MenuBase.SearchMenu.Show();
                    break;

                case Key.K_UPARROW:
                case Key.K_LEFTARROW:
                    snd.LocalSound( "misc/menu1.wav" );
                    _Cursor--;
                    if( _Cursor < 0 )
                        _Cursor = net.HostCacheCount - 1;
                    break;

                case Key.K_DOWNARROW:
                case Key.K_RIGHTARROW:
                    snd.LocalSound( "misc/menu1.wav" );
                    _Cursor++;
                    if( _Cursor >= net.HostCacheCount )
                        _Cursor = 0;
                    break;

                case Key.K_ENTER:
                    snd.LocalSound( "misc/menu2.wav" );
                    menu.ReturnMenu = this;
                    menu.ReturnOnError = true;
                    _Sorted = false;
                    MenuBase.Hide();
                    Cbuf.AddText( String.Format( "connect \"{0}\"\n", net.HostCache[_Cursor].cname ) );
                    break;

                default:
                    break;
            }
        }

        public override void Draw()
        {
            if( !_Sorted )
            {
                if( net.HostCacheCount > 1 )
                {
                    Comparison<hostcache_t> cmp = delegate ( hostcache_t a, hostcache_t b )
                    {
                        return String.Compare( a.cname, b.cname );
                    };

                    Array.Sort( net.HostCache, cmp );
                }
                _Sorted = true;
            }

            glpic_t p = Drawer.CachePic( "gfx/p_multi.lmp" );
            menu.DrawPic( ( 320 - p.width ) / 2, 4, p );
            for( var n = 0; n < net.HostCacheCount; n++ )
            {
                hostcache_t hc = net.HostCache[n];
                String tmp;
                if( hc.maxusers > 0 )
                    tmp = String.Format( "{0,-15} {1,-15} {2:D2}/{3:D2}\n", hc.name, hc.map, hc.users, hc.maxusers );
                else
                    tmp = String.Format( "{0,-15} {1,-15}\n", hc.name, hc.map );
                menu.Print( 16, 32 + 8 * n, tmp );
            }
            menu.DrawCharacter( 0, 32 + _Cursor * 8, 12 + ( ( Int32 ) ( host.RealTime * 4 ) & 1 ) );

            if( !String.IsNullOrEmpty( menu.ReturnReason ) )
                menu.PrintWhite( 16, 148, menu.ReturnReason );
        }
    }

    internal class VideoMenu : MenuBase
    {
        private struct modedesc_t
        {
            public Int32 modenum;
            public String desc;
            public Boolean iscur;
        } //modedesc_t;

        private const Int32 MAX_COLUMN_SIZE = 9;
        private const Int32 MODE_AREA_HEIGHT = MAX_COLUMN_SIZE + 2;
        private const Int32 MAX_MODEDESCS = MAX_COLUMN_SIZE * 3;

        private Int32 _WModes; // vid_wmodes
        private modedesc_t[] _ModeDescs = new modedesc_t[MAX_MODEDESCS]; // modedescs

        public override void KeyEvent( Int32 key )
        {
            switch( key )
            {
                case Key.K_ESCAPE:
                    snd.LocalSound( "misc/menu1.wav" );
                    MenuBase.OptionsMenu.Show();
                    break;

                default:
                    break;
            }
        }

        public override void Draw()
        {
            glpic_t p = Drawer.CachePic( "gfx/vidmodes.lmp" );
            menu.DrawPic( ( 320 - p.width ) / 2, 4, p );

            _WModes = 0;
            var lnummodes = vid.Modes.Length;

            for( var i = 1; ( i < lnummodes ) && ( _WModes < MAX_MODEDESCS ); i++ )
            {
                mode_t m = vid.Modes[i];

                var k = _WModes;

                _ModeDescs[k].modenum = i;
                _ModeDescs[k].desc = String.Format( "{0}x{1}x{2}", m.width, m.height, m.bpp );
                _ModeDescs[k].iscur = false;

                if( i == vid.ModeNum )
                    _ModeDescs[k].iscur = true;

                _WModes++;
            }

            if( _WModes > 0 )
            {
                menu.Print( 2 * 8, 36 + 0 * 8, "Fullscreen Modes (WIDTHxHEIGHTxBPP)" );

                var column = 8;
                var row = 36 + 2 * 8;

                for( var i = 0; i < _WModes; i++ )
                {
                    if( _ModeDescs[i].iscur )
                        menu.PrintWhite( column, row, _ModeDescs[i].desc );
                    else
                        menu.Print( column, row, _ModeDescs[i].desc );

                    column += 13 * 8;

                    if( ( i % vid.VID_ROW_SIZE ) == ( vid.VID_ROW_SIZE - 1 ) )
                    {
                        column = 8;
                        row += 8;
                    }
                }
            }

            menu.Print( 3 * 8, 36 + MODE_AREA_HEIGHT * 8 + 8 * 2, "Video modes must be set from the" );
            menu.Print( 3 * 8, 36 + MODE_AREA_HEIGHT * 8 + 8 * 3, "command line with -width <width>" );
            menu.Print( 3 * 8, 36 + MODE_AREA_HEIGHT * 8 + 8 * 4, "and -bpp <bits-per-pixel>" );
            menu.Print( 3 * 8, 36 + MODE_AREA_HEIGHT * 8 + 8 * 6, "Select windowed mode with -window" );
        }
    }
}
