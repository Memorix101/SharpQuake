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
    public class Menu
    {
        public Boolean EnterSound;
        public Boolean ReturnOnError;
        public String ReturnReason;
        public MenuBase ReturnMenu;
        private const Int32 SLIDER_RANGE = 10;

        //qboolean	m_entersound	// play after drawing a frame, so caching

        // won't disrupt the sound
        private Boolean _RecursiveDraw; // qboolean m_recursiveDraw

        private Byte[] _IdentityTable = new Byte[256]; // identityTable
        private Byte[] _TranslationTable = new Byte[256]; //translationTable

        // Instances
        private Host Host
        {
            get;
            set;
        }

        // M_Init (void)
        public void Initialise( Host host )
        {
            Host = host;

            Host.Command.Add( "togglemenu", ToggleMenu_f );
            Host.Command.Add( "menu_main", Menu_Main_f );
            Host.Command.Add( "menu_singleplayer", Menu_SinglePlayer_f );
            Host.Command.Add( "menu_load", Menu_Load_f );
            Host.Command.Add( "menu_save", Menu_Save_f );
            Host.Command.Add( "menu_multiplayer", Menu_MultiPlayer_f );
            Host.Command.Add( "menu_setup", Menu_Setup_f );
            Host.Command.Add( "menu_options", Menu_Options_f );
            Host.Command.Add( "menu_keys", Menu_Keys_f );
            Host.Command.Add( "menu_video", Menu_Video_f );
            Host.Command.Add( "help", Menu_Help_f );
            Host.Command.Add( "menu_quit", Menu_Quit_f );
        }

        /// <summary>
        /// M_Keydown
        /// </summary>
        public void KeyDown( Int32 key )
        {
            if( MenuBase.CurrentMenu != null )
                MenuBase.CurrentMenu.KeyEvent( key );
        }

        /// <summary>
        /// M_Draw
        /// </summary>
        public void Draw()
        {
            if( MenuBase.CurrentMenu == null || Host.Keyboard.Destination != KeyDestination.key_menu )
                return;

            if( !_RecursiveDraw )
            {
                Scr.CopyEverithing = true;

                if( Scr.ConCurrent > 0 )
                {
                    Host.DrawingContext.DrawConsoleBackground( Scr.vid.height );
                    snd.ExtraUpdate();
                }
                else
                    Host.DrawingContext.FadeScreen();

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
        public void ToggleMenu_f()
        {
            EnterSound = true;

            if( Host.Keyboard.Destination == KeyDestination.key_menu )
            {
                if( MenuBase.CurrentMenu != MenuBase.MainMenu )
                {
                    MenuBase.MainMenu.Show( Host );
                    return;
                }
                MenuBase.CurrentMenu.Hide();
                return;
            }
            if( Host.Keyboard.Destination == KeyDestination.key_console )
            {
                Host.Console.ToggleConsole_f();
            }
            else
            {
                MenuBase.MainMenu.Show( Host );
            }
        }

        public void DrawPic( Int32 x, Int32 y, GLPic pic )
        {
            Host.DrawingContext.DrawPic( x + ( ( Scr.vid.width - 320 ) >> 1 ), y, pic );
        }

        public void DrawTransPic( Int32 x, Int32 y, GLPic pic )
        {
            Host.DrawingContext.DrawTransPic( x + ( ( Scr.vid.width - 320 ) >> 1 ), y, pic );
        }

        /// <summary>
        /// M_DrawTransPicTranslate
        /// </summary>
        public void DrawTransPicTranslate( Int32 x, Int32 y, GLPic pic )
        {
            Host.DrawingContext.TransPicTranslate( x + ( ( Scr.vid.width - 320 ) >> 1 ), y, pic, _TranslationTable );
        }

        /// <summary>
        /// M_Print
        /// </summary>
        public void Print( Int32 cx, Int32 cy, String str )
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
        public void DrawCharacter( Int32 cx, Int32 line, Int32 num )
        {
            Host.DrawingContext.DrawCharacter( cx + ( ( Scr.vid.width - 320 ) >> 1 ), line, num );
        }

        /// <summary>
        /// M_PrintWhite
        /// </summary>
        public void PrintWhite( Int32 cx, Int32 cy, String str )
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
        public void DrawTextBox( Int32 x, Int32 y, Int32 width, Int32 lines )
        {
            // draw left side
            var cx = x;
            var cy = y;
            var p = Host.DrawingContext.CachePic( "gfx/box_tl.lmp" );
            DrawTransPic( cx, cy, p );
            p = Host.DrawingContext.CachePic( "gfx/box_ml.lmp" );
            for( var n = 0; n < lines; n++ )
            {
                cy += 8;
                DrawTransPic( cx, cy, p );
            }
            p = Host.DrawingContext.CachePic( "gfx/box_bl.lmp" );
            DrawTransPic( cx, cy + 8, p );

            // draw middle
            cx += 8;
            while( width > 0 )
            {
                cy = y;
                p = Host.DrawingContext.CachePic( "gfx/box_tm.lmp" );
                DrawTransPic( cx, cy, p );
                p = Host.DrawingContext.CachePic( "gfx/box_mm.lmp" );
                for( var n = 0; n < lines; n++ )
                {
                    cy += 8;
                    if( n == 1 )
                        p = Host.DrawingContext.CachePic( "gfx/box_mm2.lmp" );
                    DrawTransPic( cx, cy, p );
                }
                p = Host.DrawingContext.CachePic( "gfx/box_bm.lmp" );
                DrawTransPic( cx, cy + 8, p );
                width -= 2;
                cx += 16;
            }

            // draw right side
            cy = y;
            p = Host.DrawingContext.CachePic( "gfx/box_tr.lmp" );
            DrawTransPic( cx, cy, p );
            p = Host.DrawingContext.CachePic( "gfx/box_mr.lmp" );
            for( var n = 0; n < lines; n++ )
            {
                cy += 8;
                DrawTransPic( cx, cy, p );
            }
            p = Host.DrawingContext.CachePic( "gfx/box_br.lmp" );
            DrawTransPic( cx, cy + 8, p );
        }

        /// <summary>
        /// M_DrawSlider
        /// </summary>
        public void DrawSlider( Int32 x, Int32 y, Single range )
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
        public void DrawCheckbox( Int32 x, Int32 y, Boolean on )
        {
            if( on )
                Print( x, y, "on" );
            else
                Print( x, y, "off" );
        }

        /// <summary>
        /// M_BuildTranslationTable
        /// </summary>
        public void BuildTranslationTable( Int32 top, Int32 bottom )
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
        private void Menu_Main_f()
        {
            MenuBase.MainMenu.Show( Host );
        }

        /// <summary>
        /// M_Menu_SinglePlayer_f
        /// </summary>
        private void Menu_SinglePlayer_f()
        {
            MenuBase.SinglePlayerMenu.Show( Host );
        }

        // M_Menu_Load_f
        private void Menu_Load_f()
        {
            MenuBase.LoadMenu.Show( Host );
        }

        // M_Menu_Save_f
        private void Menu_Save_f()
        {
            MenuBase.SaveMenu.Show( Host );
        }

        // M_Menu_MultiPlayer_f
        private void Menu_MultiPlayer_f()
        {
            MenuBase.MultiPlayerMenu.Show( Host );
        }

        // M_Menu_Setup_f
        private void Menu_Setup_f()
        {
            MenuBase.SetupMenu.Show( Host );
        }

        // M_Menu_Options_f
        private void Menu_Options_f()
        {
            MenuBase.OptionsMenu.Show( Host );
        }

        /// <summary>
        /// M_Menu_Keys_f
        /// </summary>
        private void Menu_Keys_f()
        {
            MenuBase.KeysMenu.Show( Host );
        }

        /// <summary>
        /// M_Menu_Video_f
        /// </summary>
        private void Menu_Video_f()
        {
            MenuBase.VideoMenu.Show( Host );
        }

        /// <summary>
        /// M_Menu_Help_f
        /// </summary>
        private void Menu_Help_f()
        {
            MenuBase.HelpMenu.Show( Host );
        }

        /// <summary>
        /// M_Menu_Quit_f
        /// </summary>
        private void Menu_Quit_f()
        {
            MenuBase.QuitMenu.Show( Host );
        }
    }
}
