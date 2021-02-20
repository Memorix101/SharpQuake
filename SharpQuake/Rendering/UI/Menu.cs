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
using System;
using SharpQuake.Framework;
using SharpQuake.Framework.IO;
using SharpQuake.Framework.IO.Input;
using SharpQuake.Renderer.Textures;

// menu.h
// menu.c

namespace SharpQuake.Rendering.UI
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
        public Host Host
        {
            get;
            private set;
        }

        public Menu( Host host )
        {
            Host = host;
        }

        /// <summary>
        /// M_Init
        /// </summary>
        public void Initialise( )
        {
            Host.Commands.Add( "togglemenu", ToggleMenu_f );
            Host.Commands.Add( "menu_main", Menu_Main_f );
            Host.Commands.Add( "menu_singleplayer", Menu_SinglePlayer_f );
            Host.Commands.Add( "menu_load", Menu_Load_f );
            Host.Commands.Add( "menu_save", Menu_Save_f );
            Host.Commands.Add( "menu_multiplayer", Menu_MultiPlayer_f );
            Host.Commands.Add( "menu_setup", Menu_Setup_f );
            Host.Commands.Add( "menu_options", Menu_Options_f );
            Host.Commands.Add( "menu_keys", Menu_Keys_f );
            Host.Commands.Add( "menu_video", Menu_Video_f );
            Host.Commands.Add( "help", Menu_Help_f );
            Host.Commands.Add( "menu_quit", Menu_Quit_f );
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
                Host.Screen.CopyEverithing = true;

                if( Host.Screen.ConCurrent > 0 )
                {
                    Host.DrawingContext.DrawConsoleBackground( Host.Screen.vid.height );
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

            if( MenuBase.CurrentMenu != null )
                MenuBase.CurrentMenu.Draw();

            if( EnterSound )
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
                Host.Console.ToggleConsole_f( null );
            }
            else
            {
                MenuBase.MainMenu.Show( Host );
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
            Host.DrawingContext.DrawCharacter( cx + ( ( Host.Screen.vid.width - 320 ) >> 1 ), line, num );
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
            var p = Host.DrawingContext.CachePic( "gfx/box_tl.lmp", "GL_NEAREST" );
            DrawTransPic( cx, cy, p );
            p = Host.DrawingContext.CachePic( "gfx/box_ml.lmp", "GL_NEAREST" );
            for( var n = 0; n < lines; n++ )
            {
                cy += 8;
                DrawTransPic( cx, cy, p );
            }
            p = Host.DrawingContext.CachePic( "gfx/box_bl.lmp", "GL_NEAREST" );
            DrawTransPic( cx, cy + 8, p );

            // draw middle
            cx += 8;
            while( width > 0 )
            {
                cy = y;
                p = Host.DrawingContext.CachePic( "gfx/box_tm.lmp", "GL_NEAREST" );
                DrawTransPic( cx, cy, p );
                p = Host.DrawingContext.CachePic( "gfx/box_mm.lmp", "GL_NEAREST" );
                for( var n = 0; n < lines; n++ )
                {
                    cy += 8;
                    if( n == 1 )
                        p = Host.DrawingContext.CachePic( "gfx/box_mm2.lmp", "GL_NEAREST" );
                    DrawTransPic( cx, cy, p );
                }
                p = Host.DrawingContext.CachePic( "gfx/box_bm.lmp", "GL_NEAREST" );
                DrawTransPic( cx, cy + 8, p );
                width -= 2;
                cx += 16;
            }

            // draw right side
            cy = y;
            p = Host.DrawingContext.CachePic( "gfx/box_tr.lmp", "GL_NEAREST" );
            DrawTransPic( cx, cy, p );
            p = Host.DrawingContext.CachePic( "gfx/box_mr.lmp", "GL_NEAREST" );
            for( var n = 0; n < lines; n++ )
            {
                cy += 8;
                DrawTransPic( cx, cy, p );
            }
            p = Host.DrawingContext.CachePic( "gfx/box_br.lmp", "GL_NEAREST" );
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
        private void Menu_Main_f( CommandMessage msg )
        {
            MenuBase.MainMenu.Show( Host );
        }

        /// <summary>
        /// M_Menu_SinglePlayer_f
        /// </summary>
        private void Menu_SinglePlayer_f( CommandMessage msg )
        {
            MenuBase.SinglePlayerMenu.Show( Host );
        }

        /// <summary>
        /// M_Menu_Load_f
        /// </summary>
        /// <param name="msg"></param>
        private void Menu_Load_f( CommandMessage msg )
        {
            MenuBase.LoadMenu.Show( Host );
        }

        /// <summary>
        /// M_Menu_Save_f
        /// </summary>
        /// <param name="msg"></param>
        private void Menu_Save_f( CommandMessage msg )
        {
            MenuBase.SaveMenu.Show( Host );
        }

        /// <summary>
        /// M_Menu_MultiPlayer_f
        /// </summary>
        /// <param name="msg"></param>
        private void Menu_MultiPlayer_f( CommandMessage msg )
        {
            MenuBase.MultiPlayerMenu.Show( Host );
        }

        /// <summary>
        /// M_Menu_Setup_f
        /// </summary>
        /// <param name="msg"></param>
        private void Menu_Setup_f( CommandMessage msg )
        {
            MenuBase.SetupMenu.Show( Host );
        }

        /// <summary>
        /// M_Menu_Options_f
        /// </summary>
        /// <param name="msg"></param>
        private void Menu_Options_f( CommandMessage msg )
        {
            MenuBase.OptionsMenu.Show( Host );
        }

        /// <summary>
        /// M_Menu_Keys_f
        /// </summary>
        private void Menu_Keys_f( CommandMessage msg )
        {
            MenuBase.KeysMenu.Show( Host );
        }

        /// <summary>
        /// M_Menu_Video_f
        /// </summary>
        private void Menu_Video_f( CommandMessage msg )
        {
            MenuBase.VideoMenu.Show( Host );
        }

        /// <summary>
        /// M_Menu_Help_f
        /// </summary>
        private void Menu_Help_f( CommandMessage msg )
        {
            MenuBase.HelpMenu.Show( Host );
        }

        /// <summary>
        /// M_Menu_Quit_f
        /// </summary>
        private void Menu_Quit_f( CommandMessage msg )
        {
            MenuBase.QuitMenu.Show( Host );
        }
    }
}
