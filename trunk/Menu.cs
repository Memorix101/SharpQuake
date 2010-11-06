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
using System.Collections.Generic;
using System.Text;
using System.IO;

// menu.h
// menu.c

namespace SharpQuake
{
    /// <summary>
    /// M_functions
    /// </summary>
    static class Menu
    {
        const int SLIDER_RANGE = 10;

        public static bool EnterSound; //qboolean	m_entersound	// play after drawing a frame, so caching
								// won't disrupt the sound
        static bool _RecursiveDraw; // qboolean m_recursiveDraw
        static byte[] _IdentityTable = new byte[256]; // identityTable
        static byte[] _TranslationTable = new byte[256]; //translationTable
        public static bool ReturnOnError;
        public static string ReturnReason;
        public static MenuBase ReturnMenu;


        // M_Init (void)
        public static void Init()
        {
            Cmd.Add("togglemenu", ToggleMenu_f);
            Cmd.Add("menu_main", Menu_Main_f);
            Cmd.Add("menu_singleplayer", Menu_SinglePlayer_f);
            Cmd.Add("menu_load", Menu_Load_f);
            Cmd.Add("menu_save", Menu_Save_f);
            Cmd.Add("menu_multiplayer", Menu_MultiPlayer_f);
            Cmd.Add("menu_setup", Menu_Setup_f);
            Cmd.Add("menu_options", Menu_Options_f);
            Cmd.Add("menu_keys", Menu_Keys_f);
            Cmd.Add("menu_video", Menu_Video_f);
            Cmd.Add("help", Menu_Help_f);
            Cmd.Add("menu_quit", Menu_Quit_f);
        }
        
        /// <summary>
        /// M_Keydown
        /// </summary>
        public static void KeyDown (int key)
        {
            if (MenuBase.CurrentMenu != null)
                MenuBase.CurrentMenu.KeyEvent(key);
        }


        /// <summary>
        /// M_Draw
        /// </summary>
        public static void Draw()
        {
            if (MenuBase.CurrentMenu == null || Key.Destination != keydest_t.key_menu)
                return;

            if (!_RecursiveDraw)
            {
                Scr.CopyEverithing = true;

                if (Scr.ConCurrent > 0)
                {
                    Drawer.DrawConsoleBackground(Scr.vid.height);
                    Sound.ExtraUpdate();
                }
                else
                    Drawer.FadeScreen();

                Scr.FullUpdate = 0;
            }
            else
            {
                _RecursiveDraw = false;
            }

            if (MenuBase.CurrentMenu != null)
                MenuBase.CurrentMenu.Draw();

            if (EnterSound)
            {
                Sound.LocalSound("misc/menu2.wav");
                EnterSound = false;
            }

            Sound.ExtraUpdate();
        }

        /// <summary>
        /// M_ToggleMenu_f
        /// </summary>
        public static void ToggleMenu_f()
        {
            EnterSound = true;

            if (Key.Destination == keydest_t.key_menu)
            {
                if (MenuBase.CurrentMenu != MenuBase.MainMenu)
                {
                    MenuBase.MainMenu.Show();
                    return;
                }
                MenuBase.Hide();
                return;
            }
            if (Key.Destination == keydest_t.key_console)
            {
                Con.ToggleConsole_f();
            }
            else
            {
                MenuBase.MainMenu.Show();
            }
        }

        /// <summary>
        /// M_Menu_Main_f
        /// </summary>
        static void Menu_Main_f()
        {
            MenuBase.MainMenu.Show();
        }

        /// <summary>
        /// M_Menu_SinglePlayer_f
        /// </summary>
        static void Menu_SinglePlayer_f()
        {
            MenuBase.SinglePlayerMenu.Show();
        }

        // M_Menu_Load_f
        static void Menu_Load_f()
        {
            MenuBase.LoadMenu.Show();
        }


        // M_Menu_Save_f
        static void Menu_Save_f()
        {
            MenuBase.SaveMenu.Show();
        }

        // M_Menu_MultiPlayer_f
        static void Menu_MultiPlayer_f()
        {
            MenuBase.MultiPlayerMenu.Show();
        }

        // M_Menu_Setup_f
        static void Menu_Setup_f()
        {
            MenuBase.SetupMenu.Show();
        }

        // M_Menu_Options_f
        static void Menu_Options_f()
        {
            MenuBase.OptionsMenu.Show();
        }

        /// <summary>
        /// M_Menu_Keys_f
        /// </summary>
        static void Menu_Keys_f()
        {
            MenuBase.KeysMenu.Show();
        }

        /// <summary>
        /// M_Menu_Video_f
        /// </summary>
        static void Menu_Video_f()
        {
            MenuBase.VideoMenu.Show();
        }

        /// <summary>
        /// M_Menu_Help_f
        /// </summary>
        static void Menu_Help_f()
        {
            MenuBase.HelpMenu.Show();
        }

        /// <summary>
        /// M_Menu_Quit_f
        /// </summary>
        static void Menu_Quit_f()
        {
            MenuBase.QuitMenu.Show();
        }

        public static void DrawPic(int x, int y, glpic_t pic)
        {
            Drawer.DrawPic(x + ((Scr.vid.width - 320) >> 1), y, pic);
        }

        public static void DrawTransPic(int x, int y, glpic_t pic)
        {
            Drawer.DrawTransPic(x + ((Scr.vid.width - 320) >> 1), y, pic);
        }

        /// <summary>
        /// M_DrawTransPicTranslate
        /// </summary>
        public static void DrawTransPicTranslate(int x, int y, glpic_t pic)
        {
            Drawer.TransPicTranslate(x + ((Scr.vid.width - 320) >> 1), y, pic, _TranslationTable);
        }

        /// <summary>
        /// M_Print
        /// </summary>
        public static void Print(int cx, int cy, string str)
        {
            for (int i = 0; i < str.Length; i++)
            {
                DrawCharacter(cx, cy, str[i] + 128);
                cx += 8;
            }
        }

        /// <summary>
        /// M_DrawCharacter
        /// </summary>
        public static void DrawCharacter(int cx, int line, int num)
        {
            Drawer.DrawCharacter(cx + ((Scr.vid.width - 320) >> 1), line, num);
        }

        /// <summary>
        /// M_PrintWhite
        /// </summary>
        public static void PrintWhite(int cx, int cy, string str)
        {
            for (int i = 0; i < str.Length; i++)
            {
                DrawCharacter(cx, cy, str[i]);
                cx += 8;
            }
        }

        /// <summary>
        /// M_DrawTextBox
        /// </summary>
        public static void DrawTextBox(int x, int y, int width, int lines)
        {
            // draw left side
            int cx = x;
            int cy = y;
            glpic_t p = Drawer.CachePic("gfx/box_tl.lmp");
            DrawTransPic(cx, cy, p);
            p = Drawer.CachePic("gfx/box_ml.lmp");
            for (int n = 0; n < lines; n++)
            {
                cy += 8;
                DrawTransPic(cx, cy, p);
            }
            p = Drawer.CachePic("gfx/box_bl.lmp");
            DrawTransPic(cx, cy + 8, p);

            // draw middle
            cx += 8;
            while (width > 0)
            {
                cy = y;
                p = Drawer.CachePic("gfx/box_tm.lmp");
                DrawTransPic(cx, cy, p);
                p = Drawer.CachePic("gfx/box_mm.lmp");
                for (int n = 0; n < lines; n++)
                {
                    cy += 8;
                    if (n == 1)
                        p = Drawer.CachePic("gfx/box_mm2.lmp");
                    DrawTransPic(cx, cy, p);
                }
                p = Drawer.CachePic("gfx/box_bm.lmp");
                DrawTransPic(cx, cy + 8, p);
                width -= 2;
                cx += 16;
            }

            // draw right side
            cy = y;
            p = Drawer.CachePic("gfx/box_tr.lmp");
            DrawTransPic(cx, cy, p);
            p = Drawer.CachePic("gfx/box_mr.lmp");
            for (int n = 0; n < lines; n++)
            {
                cy += 8;
                DrawTransPic(cx, cy, p);
            }
            p = Drawer.CachePic("gfx/box_br.lmp");
            DrawTransPic(cx, cy + 8, p);
        }

        /// <summary>
        /// M_DrawSlider
        /// </summary>
        public static void DrawSlider(int x, int y, float range)
        {
            if (range < 0)
                range = 0;
            if (range > 1)
                range = 1;
            DrawCharacter(x - 8, y, 128);
            int i;
            for (i = 0; i < SLIDER_RANGE; i++)
                DrawCharacter(x + i * 8, y, 129);
            DrawCharacter(x + i * 8, y, 130);
            DrawCharacter((int)(x + (SLIDER_RANGE - 1) * 8 * range), y, 131);
        }

        /// <summary>
        /// M_DrawCheckbox
        /// </summary>
        public static void DrawCheckbox(int x, int y, bool on)
        {
            if (on)
                Print(x, y, "on");
            else
                Print(x, y, "off");
        }

        /// <summary>
        /// M_BuildTranslationTable
        /// </summary>
        public static void BuildTranslationTable(int top, int bottom)
        {
            for (int j = 0; j < 256; j++)
                _IdentityTable[j] = (byte)j;

            _IdentityTable.CopyTo(_TranslationTable, 0);

            if (top < 128)	// the artists made some backwards ranges.  sigh.
                Array.Copy(_IdentityTable, top, _TranslationTable, Render.TOP_RANGE, 16); // memcpy (dest + Render.TOP_RANGE, source + top, 16);
            else
                for (int j = 0; j < 16; j++)
                    _TranslationTable[Render.TOP_RANGE + j] = _IdentityTable[top + 15 - j];

            if (bottom < 128)
                Array.Copy(_IdentityTable, bottom, _TranslationTable, Render.BOTTOM_RANGE, 16); // memcpy(dest + Render.BOTTOM_RANGE, source + bottom, 16);
            else
                for (int j = 0; j < 16; j++)
                    _TranslationTable[Render.BOTTOM_RANGE + j] = _IdentityTable[bottom + 15 - j];
        }

    }


    abstract class MenuBase
    {
        static MenuBase _CurrentMenu;
        
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

        public static MenuBase CurrentMenu
        {
            get { return _CurrentMenu; }
        }
        
        protected int _Cursor;

        public int Cursor
        {
            get { return _Cursor; }
        }

        public virtual void Show()
        {
            Menu.EnterSound = true;
            Key.Destination = keydest_t.key_menu;
            _CurrentMenu = this;
        }

        public abstract void KeyEvent(int key);
        public abstract void Draw();

        public static void Hide()
        {
            Key.Destination = keydest_t.key_game;
            _CurrentMenu = null;
        }
    }

    /// <summary>
    /// MainMenu
    /// </summary>
    class MainMenu : MenuBase
    {
        const int MAIN_ITEMS = 5;
        int _SaveDemoNum;

        public override void Show()
        {
            if (Key.Destination != keydest_t.key_menu)
            {
                _SaveDemoNum = Client.cls.demonum;
                Client.cls.demonum = -1;
            }

            base.Show();
        }

        /// <summary>
        /// M_Main_Key
        /// </summary>
        public override void KeyEvent(int key)
        {
            switch (key)
            {
                case Key.K_ESCAPE:
                    //Key.Destination = keydest_t.key_game;
                    MenuBase.Hide();
                    Client.cls.demonum = _SaveDemoNum;
                    if (Client.cls.demonum != -1 && !Client.cls.demoplayback && Client.cls.state != cactive_t.ca_connected)
                        Client.NextDemo();
                    break;

                case Key.K_DOWNARROW:
                    Sound.LocalSound("misc/menu1.wav");
                    if (++_Cursor >= MAIN_ITEMS)
                        _Cursor = 0;
                    break;

                case Key.K_UPARROW:
                    Sound.LocalSound("misc/menu1.wav");
                    if (--_Cursor < 0)
                        _Cursor = MAIN_ITEMS - 1;
                    break;

                case Key.K_ENTER:
                    Menu.EnterSound = true;

                    switch (_Cursor)
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
            Menu.DrawTransPic(16, 4, Drawer.CachePic("gfx/qplaque.lmp"));
            glpic_t p = Drawer.CachePic("gfx/ttl_main.lmp");
            Menu.DrawPic((320 - p.width) / 2, 4, p);
            Menu.DrawTransPic(72, 32, Drawer.CachePic("gfx/mainmenu.lmp"));

            int f = (int)(Host.Time * 10) % 6;

            Menu.DrawTransPic(54, 32 + _Cursor * 20, Drawer.CachePic(String.Format("gfx/menudot{0}.lmp", f + 1)));
        }
    }

    class SinglePlayerMenu : MenuBase
    {
        const int SINGLEPLAYER_ITEMS = 3;

        /// <summary>
        /// M_SinglePlayer_Key
        /// </summary>
        public override void KeyEvent(int key)
        {
            switch (key)
            {
                case Key.K_ESCAPE:
                    MenuBase.MainMenu.Show();
                    break;

                case Key.K_DOWNARROW:
                    Sound.LocalSound("misc/menu1.wav");
                    if (++_Cursor >= SINGLEPLAYER_ITEMS)
                        _Cursor = 0;
                    break;

                case Key.K_UPARROW:
                    Sound.LocalSound("misc/menu1.wav");
                    if (--_Cursor < 0)
                        _Cursor = SINGLEPLAYER_ITEMS - 1;
                    break;

                case Key.K_ENTER:
                    Menu.EnterSound = true;

                    switch (_Cursor)
                    {
                        case 0:
                            if (Server.sv.active)
                                if (!Scr.ModalMessage("Are you sure you want to\nstart a new game?\n"))
                                    break;
                            Key.Destination = keydest_t.key_game;
                            if (Server.sv.active)
                                Cbuf.AddText("disconnect\n");
                            Cbuf.AddText("maxplayers 1\n");
                            Cbuf.AddText("map start\n");
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
            Menu.DrawTransPic(16, 4, Drawer.CachePic("gfx/qplaque.lmp"));
            glpic_t p = Drawer.CachePic("gfx/ttl_sgl.lmp");
            Menu.DrawPic((320 - p.width) / 2, 4, p);
            Menu.DrawTransPic(72, 32, Drawer.CachePic("gfx/sp_menu.lmp"));

            int f = (int)(Host.Time * 10) % 6;

            Menu.DrawTransPic(54, 32 + _Cursor * 20, Drawer.CachePic(String.Format("gfx/menudot{0}.lmp", f + 1)));
        }
    }

    class LoadMenu : MenuBase
    {
        public const int MAX_SAVEGAMES = 12;
        protected string[] _FileNames; //[MAX_SAVEGAMES]; // filenames
        protected bool[] _Loadable; //[MAX_SAVEGAMES]; // loadable

        public LoadMenu()
        {
            _FileNames = new string[MAX_SAVEGAMES];
            _Loadable = new bool[MAX_SAVEGAMES];
        }

        public override void Show()
        {
            base.Show();
            ScanSaves ();
        }

        /// <summary>
        /// M_ScanSaves
        /// </summary>
        protected void ScanSaves()
        {
            for (int i=0 ; i<MAX_SAVEGAMES ; i++)
	        {
                _FileNames[i] = "--- UNUSED SLOT ---";
                _Loadable[i] = false;
                string name =  String.Format("{0}/s{1}.sav", Common.GameDir, i);
                FileStream fs = Sys.FileOpenRead(name);
                if (fs == null)
                    continue;
                
                using(StreamReader reader = new StreamReader(fs, Encoding.ASCII))
                {
                    string version = reader.ReadLine();
                    if (version == null)
                        continue;
                    string info = reader.ReadLine();
                    if (info == null)
                        continue;
                    info = info.TrimEnd('\0', '_').Replace('_', ' ');
                    if (!String.IsNullOrEmpty(info))
                    {
                        _FileNames[i] = info;
                        _Loadable[i] = true;
                    }
                }
	        }
        }
        
        public override void KeyEvent(int key)
        {
            switch (key)
            {
                case Key.K_ESCAPE:
                    MenuBase.SinglePlayerMenu.Show();
                    break;

                case Key.K_ENTER:
                    Sound.LocalSound("misc/menu2.wav");
                    if (!_Loadable[_Cursor])
                        return;
                    MenuBase.Hide();

                    // Host_Loadgame_f can't bring up the loading plaque because too much
                    // stack space has been used, so do it now
                    Scr.BeginLoadingPlaque();

                    // issue the load command
                    Cbuf.AddText(String.Format("load s{0}\n", _Cursor));
                    return;

                case Key.K_UPARROW:
                case Key.K_LEFTARROW:
                    Sound.LocalSound("misc/menu1.wav");
                    _Cursor--;
                    if (_Cursor < 0)
                        _Cursor = MAX_SAVEGAMES - 1;
                    break;

                case Key.K_DOWNARROW:
                case Key.K_RIGHTARROW:
                    Sound.LocalSound("misc/menu1.wav");
                    _Cursor++;
                    if (_Cursor >= MAX_SAVEGAMES)
                        _Cursor = 0;
                    break;
            }
        }

        public override void Draw()
        {
            glpic_t p = Drawer.CachePic("gfx/p_load.lmp");
            Menu.DrawPic((320 - p.width) / 2, 4, p);

            for (int i = 0; i < MAX_SAVEGAMES; i++)
                Menu.Print(16, 32 + 8 * i, _FileNames[i]);

            // line cursor
            Menu.DrawCharacter(8, 32 + _Cursor * 8, 12 + ((int)(Host.RealTime * 4) & 1));
        }
    }

    class SaveMenu : LoadMenu
    {
        public override void Show()
        {
            if (!Server.sv.active)
                return;
            if (Client.cl.intermission != 0)
                return;
            if (Server.svs.maxclients != 1)
                return;

            base.Show();
        }

        public override void KeyEvent(int key)
        {
            switch (key)
            {
                case Key.K_ESCAPE:
                    MenuBase.SinglePlayerMenu.Show();
                    break;

                case Key.K_ENTER:
                    MenuBase.Hide();
                    Cbuf.AddText(String.Format("save s{0}\n", _Cursor));
                    return;

                case Key.K_UPARROW:
                case Key.K_LEFTARROW:
                    Sound.LocalSound("misc/menu1.wav");
                    _Cursor--;
                    if (_Cursor < 0)
                        _Cursor = MAX_SAVEGAMES - 1;
                    break;

                case Key.K_DOWNARROW:
                case Key.K_RIGHTARROW:
                    Sound.LocalSound("misc/menu1.wav");
                    _Cursor++;
                    if (_Cursor >= MAX_SAVEGAMES)
                        _Cursor = 0;
                    break;
            }
        }

        public override void Draw()
        {
            glpic_t p = Drawer.CachePic("gfx/p_save.lmp");
            Menu.DrawPic((320 - p.width) / 2, 4, p);

            for (int i = 0; i < MAX_SAVEGAMES; i++)
                Menu.Print(16, 32 + 8 * i, _FileNames[i]);

            // line cursor
            Menu.DrawCharacter(8, 32 + _Cursor * 8, 12 + ((int)(Host.RealTime * 4) & 1));
        }
    }

    class QuitMenu : MenuBase
    {
        MenuBase _PrevMenu; // m_quit_prevstate;

        public override void Show()
        {
            if (CurrentMenu == this)
                return;

            Key.Destination = keydest_t.key_menu;
            _PrevMenu = CurrentMenu;

            base.Show();
        }

        public override void KeyEvent(int key)
        {
            switch (key)
            {
                case Key.K_ESCAPE:
                case 'n':
                case 'N':
                    if (_PrevMenu != null)
                        _PrevMenu.Show();
                    else
                        MenuBase.Hide();
                    break;

                case 'Y':
                case 'y':
                    Key.Destination = keydest_t.key_console;
                    Host.Quit_f();
                    break;

                default:
                    break;
            }
        }

        public override void Draw()
        {
            Menu.DrawTextBox(0, 0, 38, 23);
            Menu.PrintWhite(16, 12, "  Quake version 1.09 by id Software\n\n");
            Menu.PrintWhite(16, 28, "Programming        Art \n");
            Menu.Print(16, 36, " John Carmack       Adrian Carmack\n");
            Menu.Print(16, 44, " Michael Abrash     Kevin Cloud\n");
            Menu.Print(16, 52, " John Cash          Paul Steed\n");
            Menu.Print(16, 60, " Dave 'Zoid' Kirsch\n");
            Menu.PrintWhite(16, 68, "Design             Biz\n");
            Menu.Print(16, 76, " John Romero        Jay Wilbur\n");
            Menu.Print(16, 84, " Sandy Petersen     Mike Wilson\n");
            Menu.Print(16, 92, " American McGee     Donna Jackson\n");
            Menu.Print(16, 100, " Tim Willits        Todd Hollenshead\n");
            Menu.PrintWhite(16, 108, "Support            Projects\n");
            Menu.Print(16, 116, " Barrett Alexander  Shawn Green\n");
            Menu.PrintWhite(16, 124, "Sound Effects\n");
            Menu.Print(16, 132, " Trent Reznor and Nine Inch Nails\n\n");
            Menu.PrintWhite(16, 140, "Quake is a trademark of Id Software,\n");
            Menu.PrintWhite(16, 148, "inc., (c)1996 Id Software, inc. All\n");
            Menu.PrintWhite(16, 156, "rights reserved. NIN logo is a\n");
            Menu.PrintWhite(16, 164, "registered trademark licensed to\n");
            Menu.PrintWhite(16, 172, "Nothing Interactive, Inc. All rights\n");
            Menu.PrintWhite(16, 180, "reserved. Press y to exit\n");
        }
    }

    class HelpMenu : MenuBase
    {
        const int NUM_HELP_PAGES = 6;

        int _Page;

        public override void Show()
        {
            _Page = 0;
            base.Show();
        }

        public override void KeyEvent(int key)
        {
            switch (key)
            {
                case Key.K_ESCAPE:
                    MenuBase.MainMenu.Show();
                    break;

                case Key.K_UPARROW:
                case Key.K_RIGHTARROW:
                    Menu.EnterSound = true;
                    if (++_Page >= NUM_HELP_PAGES)
                        _Page = 0;
                    break;

                case Key.K_DOWNARROW:
                case Key.K_LEFTARROW:
                    Menu.EnterSound = true;
                    if (--_Page < 0)
                        _Page = NUM_HELP_PAGES - 1;
                    break;
            }
        }

        public override void Draw()
        {
            Menu.DrawPic(0, 0, Drawer.CachePic(String.Format("gfx/help{0}.lmp", _Page)));
        }
    }

    class OptionsMenu : MenuBase
    {
        const int OPTIONS_ITEMS = 13;

        float _BgmVolumeCoeff = 0.1f;

        public override void Show()
        {
            if (Sys.IsWindows)
            {
                _BgmVolumeCoeff = 1.0f;
            }

            if (_Cursor > OPTIONS_ITEMS - 1)
                _Cursor = 0;

            if (_Cursor == OPTIONS_ITEMS - 1 && MenuBase.VideoMenu == null)
                _Cursor = 0;

            base.Show();
        }

        public override void KeyEvent(int key)
        {
            switch (key)
            {
                case Key.K_ESCAPE:
                    MenuBase.MainMenu.Show();
                    break;

                case Key.K_ENTER:
                    Menu.EnterSound = true;
                    switch (_Cursor)
                    {
                        case 0:
                            MenuBase.KeysMenu.Show();
                            break;

                        case 1:
                            MenuBase.Hide();
                            Con.ToggleConsole_f();
                            break;

                        case 2:
                            Cbuf.AddText("exec default.cfg\n");
                            break;

                        case 12:
                            MenuBase.VideoMenu.Show();
                            break;

                        default:
                            AdjustSliders(1);
                            break;
                    }
                    return;

                case Key.K_UPARROW:
                    Sound.LocalSound("misc/menu1.wav");
                    _Cursor--;
                    if (_Cursor < 0)
                        _Cursor = OPTIONS_ITEMS - 1;
                    break;

                case Key.K_DOWNARROW:
                    Sound.LocalSound("misc/menu1.wav");
                    _Cursor++;
                    if (_Cursor >= OPTIONS_ITEMS)
                        _Cursor = 0;
                    break;

                case Key.K_LEFTARROW:
                    AdjustSliders(-1);
                    break;

                case Key.K_RIGHTARROW:
                    AdjustSliders(1);
                    break;
            }

            if (_Cursor == 12 && VideoMenu == null)
            {
                if (key == Key.K_UPARROW)
                    _Cursor = 11;
                else
                    _Cursor = 0;
            }

#if _WIN32
            if ((options_cursor == 13) && (modestate != MS_WINDOWED))
            {
                if (k == K_UPARROW)
                    options_cursor = 12;
                else
                    options_cursor = 0;
            }
#endif
        }

        public override void Draw()
        {
            Menu.DrawTransPic(16, 4, Drawer.CachePic("gfx/qplaque.lmp"));
            glpic_t p = Drawer.CachePic("gfx/p_option.lmp");
            Menu.DrawPic((320 - p.width) / 2, 4, p);

            Menu.Print(16, 32, "    Customize controls");
            Menu.Print(16, 40, "         Go to console");
            Menu.Print(16, 48, "     Reset to defaults");

            Menu.Print(16, 56, "           Screen size");
            float r = (Scr.ViewSize.Value - 30) / (120 - 30);
            Menu.DrawSlider(220, 56, r);

            Menu.Print(16, 64, "            Brightness");
            r = (1.0f - View.Gamma) / 0.5f;
            Menu.DrawSlider(220, 64, r);

            Menu.Print(16, 72, "           Mouse Speed");
            r = (Client.Sensitivity - 1) / 10;
            Menu.DrawSlider(220, 72, r);

            Menu.Print(16, 80, "       CD Music Volume");
            r = Sound.BgmVolume;
            Menu.DrawSlider(220, 80, r);

            Menu.Print(16, 88, "          Sound Volume");
            r = Sound.Volume;
            Menu.DrawSlider(220, 88, r);

            Menu.Print(16, 96, "            Always Run");
            Menu.DrawCheckbox(220, 96, Client.ForwardSpeed > 200);

            Menu.Print(16, 104, "          Invert Mouse");
            Menu.DrawCheckbox(220, 104, Client.MPitch < 0);

            Menu.Print(16, 112, "            Lookspring");
            Menu.DrawCheckbox(220, 112, Client.LookSpring);

            Menu.Print(16, 120, "            Lookstrafe");
            Menu.DrawCheckbox(220, 120, Client.LookStrafe);

            if (VideoMenu != null)
                Menu.Print(16, 128, "         Video Options");

#if _WIN32
	if (modestate == MS_WINDOWED)
	{
		Menu.Print (16, 136, "             Use Mouse");
		Menu.DrawCheckbox (220, 136, _windowed_mouse.value);
	}
#endif

            // cursor
            Menu.DrawCharacter(200, 32 + _Cursor * 8, 12 + ((int)(Host.RealTime * 4) & 1));
        }

        /// <summary>
        /// M_AdjustSliders
        /// </summary>
        void AdjustSliders(int dir)
        {
            Sound.LocalSound("misc/menu3.wav");
            float value;

            switch (_Cursor)
            {
                case 3:	// screen size
                    value = Scr.ViewSize.Value + dir * 10;
                    if (value < 30)
                        value = 30;
                    if (value > 120)
                        value = 120;
                    Cvar.Set("viewsize", value);
                    break;

                case 4:	// gamma
                    value = View.Gamma - dir * 0.05f;
                    if (value < 0.5)
                        value = 0.5f;
                    if (value > 1)
                        value = 1;
                    Cvar.Set("gamma", value);
                    break;

                case 5:	// mouse speed
                    value = Client.Sensitivity + dir * 0.5f;
                    if (value < 1)
                        value = 1;
                    if (value > 11)
                        value = 11;
                    Cvar.Set("sensitivity", value);
                    break;

                case 6:	// music volume
                    value = Sound.BgmVolume + dir * _BgmVolumeCoeff;
                    if (value < 0)
                        value = 0;
                    if (value > 1)
                        value = 1;
                    Cvar.Set("bgmvolume", value);
                    break;

                case 7:	// sfx volume
                    value = Sound.Volume + dir * 0.1f;
                    if (value < 0)
                        value = 0;
                    if (value > 1)
                        value = 1;
                    Cvar.Set("volume", value);
                    break;


                case 8:	// allways run
                    if (Client.ForwardSpeed > 200)
                    {
                        Cvar.Set("cl_forwardspeed", 200f);
                        Cvar.Set("cl_backspeed", 200f);
                    }
                    else
                    {
                        Cvar.Set("cl_forwardspeed", 400f);
                        Cvar.Set("cl_backspeed", 400f);
                    }
                    break;


                case 9:	// invert mouse
                    Cvar.Set("m_pitch", -Client.MPitch);
                    break;


                case 10:	// lookspring
                    Cvar.Set("lookspring", !Client.LookSpring ? 1f : 0f);
                    break;


                case 11:	// lookstrafe
                    Cvar.Set("lookstrafe", !Client.LookStrafe ? 1f : 0f);
                    break;

#if _WIN32
	        case 13:	// _windowed_mouse
		        Cvar_SetValue ("_windowed_mouse", !_windowed_mouse.value);
		        break;
#endif
            }
        }
    }

    class KeysMenu : MenuBase
    {
        static readonly string[][] _BindNames = new string[][]
        {
            new string[] {"+attack", 		"attack"},
            new string[] {"impulse 10", 	"change weapon"},
            new string[] {"+jump", 			"jump / swim up"},
            new string[] {"+forward", 		"walk forward"},
            new string[] {"+back", 			"backpedal"},
            new string[] {"+left", 			"turn left"},
            new string[] {"+right", 		"turn right"},
            new string[] {"+speed", 		"run"},
            new string[] {"+moveleft", 		"step left"},
            new string[] {"+moveright", 	"step right"},
            new string[] {"+strafe", 		"sidestep"},
            new string[] {"+lookup", 		"look up"},
            new string[] {"+lookdown", 		"look down"},
            new string[] {"centerview", 	"center view"},
            new string[] {"+mlook", 		"mouse look"},
            new string[] {"+klook", 		"keyboard look"},
            new string[] {"+moveup",		"swim up"},
            new string[] {"+movedown",		"swim down"}
        };

        //const inte	NUMCOMMANDS	(sizeof(bindnames)/sizeof(bindnames[0]))

        bool _BindGrab; // bind_grab

        public override void Show()
        {
            base.Show();
        }

        public override void KeyEvent(int key)
        {
            if (_BindGrab)
            {
                // defining a key
                Sound.LocalSound("misc/menu1.wav");
                if (key == Key.K_ESCAPE)
                {
                    _BindGrab = false;
                }
                else if (key != '`')
                {
                    string cmd = String.Format("bind \"{0}\" \"{1}\"\n", Key.KeynumToString(key), _BindNames[_Cursor][0]);
                    Cbuf.InsertText(cmd);
                }

                _BindGrab = false;
                return;
            }

            switch (key)
            {
                case Key.K_ESCAPE:
                    MenuBase.OptionsMenu.Show();
                    break;

                case Key.K_LEFTARROW:
                case Key.K_UPARROW:
                    Sound.LocalSound("misc/menu1.wav");
                    _Cursor--;
                    if (_Cursor < 0)
                        _Cursor = _BindNames.Length - 1;
                    break;

                case Key.K_DOWNARROW:
                case Key.K_RIGHTARROW:
                    Sound.LocalSound("misc/menu1.wav");
                    _Cursor++;
                    if (_Cursor >= _BindNames.Length)
                        _Cursor = 0;
                    break;

                case Key.K_ENTER:		// go into bind mode
                    int[] keys = new int[2];
                    FindKeysForCommand(_BindNames[_Cursor][0], keys);
                    Sound.LocalSound("misc/menu2.wav");
                    if (keys[1] != -1)
                        UnbindCommand(_BindNames[_Cursor][0]);
                    _BindGrab = true;
                    break;

                case Key.K_BACKSPACE:		// delete bindings
                case Key.K_DEL:				// delete bindings
                    Sound.LocalSound("misc/menu2.wav");
                    UnbindCommand(_BindNames[_Cursor][0]);
                    break;
            }
        }

        public override void Draw()
        {
            glpic_t p = Drawer.CachePic("gfx/ttl_cstm.lmp");
            Menu.DrawPic((320 - p.width) / 2, 4, p);

            if (_BindGrab)
                Menu.Print(12, 32, "Press a key or button for this action");
            else
                Menu.Print(18, 32, "Enter to change, backspace to clear");

            // search for known bindings
            int[] keys = new int[2];
            
            for (int i = 0; i < _BindNames.Length; i++)
            {
                int y = 48 + 8 * i;

                Menu.Print(16, y, _BindNames[i][1]);

                FindKeysForCommand(_BindNames[i][0], keys);

                if (keys[0] == -1)
                {
                    Menu.Print(140, y, "???");
                }
                else
                {
                    string name = Key.KeynumToString(keys[0]);
                    Menu.Print(140, y, name);
                    int x = name.Length * 8;
                    if (keys[1] != -1)
                    {
                        Menu.Print(140 + x + 8, y, "or");
                        Menu.Print(140 + x + 32, y, Key.KeynumToString(keys[1]));
                    }
                }
            }

            if (_BindGrab)
                Menu.DrawCharacter(130, 48 + _Cursor * 8, '=');
            else
                Menu.DrawCharacter(130, 48 + _Cursor * 8, 12 + ((int)(Host.RealTime * 4) & 1));

        }

        /// <summary>
        /// M_FindKeysForCommand
        /// </summary>
        void FindKeysForCommand(string command, int[] twokeys)
        {
            twokeys[0] = twokeys[1] = -1;
            int len = command.Length;
            int count = 0;

            for (int j = 0; j < 256; j++)
            {
                string b = Key.Bindings[j];
                if (String.IsNullOrEmpty(b))
                    continue;

                if (String.Compare(b, 0, command, 0, len) == 0)
                {
                    twokeys[count] = j;
                    count++;
                    if (count == 2)
                        break;
                }
            }
        }

        /// <summary>
        /// M_UnbindCommand
        /// </summary>
        void UnbindCommand(string command)
        {
            int len = command.Length;

            for (int j = 0; j < 256; j++)
            {
                string b = Key.Bindings[j];
                if (String.IsNullOrEmpty(b))
                    continue;

                if (String.Compare(b, 0, command, 0, len) == 0)
                    Key.SetBinding(j, String.Empty);
            }
        }
    }

    class MultiPleerMenu : MenuBase
    {
        const int MULTIPLAYER_ITEMS = 3;

        public override void KeyEvent(int key)
        {
            switch (key)
            {
                case Key.K_ESCAPE:
                    MenuBase.MainMenu.Show();
                    break;

                case Key.K_DOWNARROW:
                    Sound.LocalSound("misc/menu1.wav");
                    if (++_Cursor >= MULTIPLAYER_ITEMS)
                        _Cursor = 0;
                    break;

                case Key.K_UPARROW:
                    Sound.LocalSound("misc/menu1.wav");
                    if (--_Cursor < 0)
                        _Cursor = MULTIPLAYER_ITEMS - 1;
                    break;

                case Key.K_ENTER:
                    Menu.EnterSound = true;
                    switch (_Cursor)
                    {
                        case 0:
                            if (Net.TcpIpAvailable)
                                MenuBase.LanConfigMenu.Show();
                            break;

                        case 1:
                            if (Net.TcpIpAvailable)
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
            Menu.DrawTransPic(16, 4, Drawer.CachePic("gfx/qplaque.lmp"));
            glpic_t p = Drawer.CachePic("gfx/p_multi.lmp");
            Menu.DrawPic((320 - p.width) / 2, 4, p);
            Menu.DrawTransPic(72, 32, Drawer.CachePic("gfx/mp_menu.lmp"));

            float f = (int)(Host.Time * 10) % 6;

            Menu.DrawTransPic(54, 32 + _Cursor * 20, Drawer.CachePic(String.Format("gfx/menudot{0}.lmp", f + 1)));

            if (Net.TcpIpAvailable)
                return;
            Menu.PrintWhite((320 / 2) - ((27 * 8) / 2), 148, "No Communications Available");
        }
    }

    /// <summary>
    /// M_Menu_LanConfig_functions
    /// </summary>
    class LanConfigMenu : MenuBase
    {
        const int NUM_LANCONFIG_CMDS = 3;

        static readonly int[] _CursorTable = new int[] { 72, 92, 124 };
        
        int _Port;
        string _PortName;
        string _JoinName;

        public bool JoiningGame
        {
            get { return MenuBase.MultiPlayerMenu.Cursor == 0; }
        }
        public bool StartingGame
        {
            get { return MenuBase.MultiPlayerMenu.Cursor == 1; }
        }
        
        public LanConfigMenu()
        {
            _Cursor = -1;
            _JoinName = String.Empty;
        }

        public override void Show()
        {
            base.Show();
            
            if (_Cursor == -1)
            {
                if (JoiningGame)
                    _Cursor = 2;
                else
                    _Cursor = 1;
            }
            if (StartingGame && _Cursor == 2)
                _Cursor = 1;
            _Port = Net.DefaultHostPort;
            _PortName = _Port.ToString();

            Menu.ReturnOnError = false;
            Menu.ReturnReason = String.Empty;
        }

        public override void KeyEvent(int key)
        {
            switch (key)
            {
                case Key.K_ESCAPE:
                    MenuBase.MultiPlayerMenu.Show();
                    break;

                case Key.K_UPARROW:
                    Sound.LocalSound("misc/menu1.wav");
                    _Cursor--;
                    if (_Cursor < 0)
                        _Cursor = NUM_LANCONFIG_CMDS - 1;
                    break;

                case Key.K_DOWNARROW:
                    Sound.LocalSound("misc/menu1.wav");
                    _Cursor++;
                    if (_Cursor >= NUM_LANCONFIG_CMDS)
                        _Cursor = 0;
                    break;

                case Key.K_ENTER:
                    if (_Cursor == 0)
                        break;

                    Menu.EnterSound = true;
                    Net.HostPort = _Port;

                    if (_Cursor == 1)
                    {
                        if (StartingGame)
                        {
                            MenuBase.GameOptionsMenu.Show();
                        }
                        else
                        {
                            MenuBase.SearchMenu.Show();
                        }
                        break;
                    }

                    if (_Cursor == 2)
                    {
                        Menu.ReturnMenu = this;
                        Menu.ReturnOnError = true;
                        MenuBase.Hide();
                        Cbuf.AddText(String.Format("connect \"{0}\"\n", _JoinName));
                        break;
                    }
                    break;

                case Key.K_BACKSPACE:
                    if (_Cursor == 0)
                    {
                        if (!String.IsNullOrEmpty(_PortName))
                            _PortName = _PortName.Substring(0, _PortName.Length - 1);
                    }

                    if (_Cursor == 2)
                    {
                        if (!String.IsNullOrEmpty(_JoinName))
                            _JoinName = _JoinName.Substring(0, _JoinName.Length - 1);
                    }
                    break;

                default:
                    if (key < 32 || key > 127)
                        break;

                    if (_Cursor == 2)
                    {
                        if (_JoinName.Length < 21)
                            _JoinName += (char)key;
                    }

                    if (key < '0' || key > '9')
                        break;

                    if (_Cursor == 0)
                    {
                        if (_PortName.Length < 5)
                            _PortName += (char)key;
                    }
                    break;
            }

            if (StartingGame && _Cursor == 2)
                if (key == Key.K_UPARROW)
                    _Cursor = 1;
                else
                    _Cursor = 0;

            int k = Common.atoi(_PortName);
            if (k > 65535)
                k = _Port;
            else
                _Port = k;
            _PortName = _Port.ToString();
        }

        public override void Draw()
        {
            Menu.DrawTransPic(16, 4, Drawer.CachePic("gfx/qplaque.lmp"));
            glpic_t p = Drawer.CachePic("gfx/p_multi.lmp");
            int basex = (320 - p.width) / 2;
            Menu.DrawPic(basex, 4, p);

            string startJoin;
            if (StartingGame)
                startJoin = "New Game - TCP/IP";
            else
                startJoin = "Join Game - TCP/IP";

            Menu.Print(basex, 32, startJoin);
            basex += 8;

            Menu.Print(basex, 52, "Address:");
            Menu.Print(basex + 9 * 8, 52, Net.MyTcpIpAddress);

            Menu.Print(basex, _CursorTable[0], "Port");
            Menu.DrawTextBox(basex + 8 * 8, _CursorTable[0] - 8, 6, 1);
            Menu.Print(basex + 9 * 8, _CursorTable[0], _PortName);

            if (JoiningGame)
            {
                Menu.Print(basex, _CursorTable[1], "Search for local games...");
                Menu.Print(basex, 108, "Join game at:");
                Menu.DrawTextBox(basex + 8, _CursorTable[2] - 8, 22, 1);
                Menu.Print(basex + 16, _CursorTable[2], _JoinName);
            }
            else
            {
                Menu.DrawTextBox(basex, _CursorTable[1] - 8, 2, 1);
                Menu.Print(basex + 8, _CursorTable[1], "OK");
            }

            Menu.DrawCharacter(basex - 8, _CursorTable[_Cursor], 12 + ((int)(Host.RealTime * 4) & 1));

            if (_Cursor == 0)
                Menu.DrawCharacter(basex + 9 * 8 + 8 * _PortName.Length,
                    _CursorTable[0], 10 + ((int)(Host.RealTime * 4) & 1));

            if (_Cursor == 2)
                Menu.DrawCharacter(basex + 16 + 8 * _JoinName.Length, _CursorTable[2],
                    10 + ((int)(Host.RealTime * 4) & 1));

            if (!String.IsNullOrEmpty(Menu.ReturnReason))
                Menu.PrintWhite(basex, 148, Menu.ReturnReason);
        }
    }

    class SetupMenu : MenuBase
    {
        const int NUM_SETUP_CMDS = 5;

        readonly int[] _CursorTable = new int[]
        {
            40, 56, 80, 104, 140
        }; // setup_cursor_table
        
        string _HostName; // setup_hostname[16]
        string _MyName; // setup_myname[16]
        int _OldTop; // setup_oldtop
        int _OldBottom; // setup_oldbottom
        int _Top; // setup_top
        int _Bottom; // setup_bottom

        /// <summary>
        /// M_Menu_Setup_f
        /// </summary>
        public override void Show()
        {
            _MyName = Client.Name;
            _HostName = Net.HostName;
            _Top = _OldTop = ((int)Client.Color) >> 4;
            _Bottom = _OldBottom = ((int)Client.Color) & 15;

            base.Show();
        }

        public override void KeyEvent(int key)
        {
            switch (key)
            {
                case Key.K_ESCAPE:
                    MenuBase.MultiPlayerMenu.Show();
                    break;

                case Key.K_UPARROW:
                    Sound.LocalSound("misc/menu1.wav");
                    _Cursor--;
                    if (_Cursor < 0)
                        _Cursor = NUM_SETUP_CMDS - 1;
                    break;

                case Key.K_DOWNARROW:
                    Sound.LocalSound("misc/menu1.wav");
                    _Cursor++;
                    if (_Cursor >= NUM_SETUP_CMDS)
                        _Cursor = 0;
                    break;

                case Key.K_LEFTARROW:
                    if (_Cursor < 2)
                        return;
                    Sound.LocalSound("misc/menu3.wav");
                    if (_Cursor == 2)
                        _Top = _Top - 1;
                    if (_Cursor == 3)
                        _Bottom = _Bottom - 1;
                    break;
                
                case Key.K_RIGHTARROW:
                    if (_Cursor < 2)
                        return;
                forward:
                    Sound.LocalSound("misc/menu3.wav");
                    if (_Cursor == 2)
                        _Top = _Top + 1;
                    if (_Cursor == 3)
                        _Bottom = _Bottom + 1;
                    break;

                case Key.K_ENTER:
                    if (_Cursor == 0 || _Cursor == 1)
                        return;

                    if (_Cursor == 2 || _Cursor == 3)
                        goto forward;

                    // _Cursor == 4 (OK)
                    if (_MyName != Client.Name)
                        Cbuf.AddText(String.Format("name \"{0}\"\n", _MyName));
                    if (Net.HostName != _HostName)
                        Cvar.Set("hostname", _HostName);
                    if (_Top != _OldTop || _Bottom != _OldBottom)
                        Cbuf.AddText(String.Format("color {0} {1}\n", _Top, _Bottom));
                    Menu.EnterSound = true;
                    MenuBase.MultiPlayerMenu.Show();
                    break;

                case Key.K_BACKSPACE:
                    if (_Cursor == 0)
                    {
                        if (!String.IsNullOrEmpty(_HostName))
                            _HostName = _HostName.Substring(0, _HostName.Length - 1);// setup_hostname[strlen(setup_hostname) - 1] = 0;
                    }

                    if (_Cursor == 1)
                    {
                        if (!String.IsNullOrEmpty(_MyName))
                            _MyName = _MyName.Substring(0, _MyName.Length - 1);
                    }
                    break;

                default:
                    if (key < 32 || key > 127)
                        break;
                    if (_Cursor == 0)
                    {
                        int l = _HostName.Length;
                        if (l < 15)
                        {
                            _HostName = _HostName + (char)key;
                        }
                    }
                    if (_Cursor == 1)
                    {
                        int l = _MyName.Length;
                        if (l < 15)
                        {
                            _MyName = _MyName + (char)key;
                        }
                    }
                    break;
            }

            if (_Top > 13)
                _Top = 0;
            if (_Top < 0)
                _Top = 13;
            if (_Bottom > 13)
                _Bottom = 0;
            if (_Bottom < 0)
                _Bottom = 13;
        }

        public override void Draw()
        {
            Menu.DrawTransPic(16, 4, Drawer.CachePic("gfx/qplaque.lmp"));
            glpic_t p = Drawer.CachePic("gfx/p_multi.lmp");
            Menu.DrawPic((320 - p.width) / 2, 4, p);

            Menu.Print(64, 40, "Hostname");
            Menu.DrawTextBox(160, 32, 16, 1);
            Menu.Print(168, 40, _HostName);

            Menu.Print(64, 56, "Your name");
            Menu.DrawTextBox(160, 48, 16, 1);
            Menu.Print(168, 56, _MyName);

            Menu.Print(64, 80, "Shirt color");
            Menu.Print(64, 104, "Pants color");

            Menu.DrawTextBox(64, 140 - 8, 14, 1);
            Menu.Print(72, 140, "Accept Changes");

            p = Drawer.CachePic("gfx/bigbox.lmp");
            Menu.DrawTransPic(160, 64, p);
            p = Drawer.CachePic("gfx/menuplyr.lmp");
            Menu.BuildTranslationTable(_Top * 16, _Bottom * 16);
            Menu.DrawTransPicTranslate(172, 72, p);

            Menu.DrawCharacter(56, _CursorTable[_Cursor], 12 + ((int)(Host.RealTime * 4) & 1));

            if (_Cursor == 0)
                Menu.DrawCharacter(168 + 8 * _HostName.Length, _CursorTable[_Cursor], 10 + ((int)(Host.RealTime * 4) & 1));

            if (_Cursor == 1)
                Menu.DrawCharacter(168 + 8 * _MyName.Length, _CursorTable[_Cursor], 10 + ((int)(Host.RealTime * 4) & 1));
        }
    }

    /// <summary>
    /// M_Menu_GameOptions_functions
    /// </summary>
    class GameOptionsMenu : MenuBase
    {
        class level_t
        {
	        public string name;
	        public string description;

            public level_t(string name, string desc)
            {
                this.name = name;
                this.description = desc;
            }
        } //level_t;

        class episode_t
        {
	        public string description;
	        public int firstLevel;
	        public int levels;

            public episode_t(string desc, int firstLevel, int levels)
            {
                this.description = desc;
                this.firstLevel = firstLevel;
                this.levels = levels;
            }
        } //episode_t;

        static readonly level_t[] Levels = new level_t[]
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
        static readonly level_t[] HipnoticLevels = new level_t[]
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
        static readonly level_t[] RogueLevels = new level_t[]
        {
	        new level_t("start", "Split Decision"),
	        new level_t("r1m1",	"Deviant's Domain"),
	        new level_t("r1m2",	"Dread Portal"),
	        new level_t("r1m3",	"Judgement Call"),
	        new level_t("r1m4",	"Cave of Death"),
	        new level_t("r1m5",	"Towers of Wrath"),
	        new level_t("r1m6",	"Temple of Pain"),
	        new level_t("r1m7",	"Tomb of the Overlord"),
	        new level_t("r2m1",	"Tempus Fugit"),
	        new level_t("r2m2",	"Elemental Fury I"),
	        new level_t("r2m3",	"Elemental Fury II"),
	        new level_t("r2m4",	"Curse of Osiris"),
	        new level_t("r2m5",	"Wizard's Keep"),
	        new level_t("r2m6",	"Blood Sacrifice"),
	        new level_t("r2m7",	"Last Bastion"),
	        new level_t("r2m8",	"Source of Evil"),
	        new level_t("ctf1", "Division of Change")
        };

        static readonly episode_t[] Episodes = new episode_t[]
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
        static readonly episode_t[] HipnoticEpisodes = new episode_t[]
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
        static readonly episode_t[] RogueEpisodes = new episode_t[]
        {
	        new episode_t("Introduction", 0, 1),
	        new episode_t("Hell's Fortress", 1, 7),
	        new episode_t("Corridors of Time", 8, 8),
	        new episode_t("Deathmatch Arena", 16, 1)
        };

        static readonly int[] _CursorTable = new int[]
        {
            40, 56, 64, 72, 80, 88, 96, 112, 120
        };

        const int NUM_GAMEOPTIONS = 9;

        int _StartEpisode;
        int _StartLevel;
        int _MaxPlayers;
        bool _ServerInfoMessage;
        double _ServerInfoMessageTime;


        public override void Show()
        {
            base.Show();

            if (_MaxPlayers == 0)
                _MaxPlayers = Server.svs.maxclients;
            if (_MaxPlayers < 2)
                _MaxPlayers = Server.svs.maxclientslimit;

        }

        public override void KeyEvent(int key)
        {
            switch (key)
            {
                case Key.K_ESCAPE:
                    MenuBase.LanConfigMenu.Show();
                    break;

                case Key.K_UPARROW:
                    Sound.LocalSound("misc/menu1.wav");
                    _Cursor--;
                    if (_Cursor < 0)
                        _Cursor = NUM_GAMEOPTIONS - 1;
                    break;

                case Key.K_DOWNARROW:
                    Sound.LocalSound("misc/menu1.wav");
                    _Cursor++;
                    if (_Cursor >= NUM_GAMEOPTIONS)
                        _Cursor = 0;
                    break;

                case Key.K_LEFTARROW:
                    if (_Cursor == 0)
                        break;
                    Sound.LocalSound("misc/menu3.wav");
                    Change(-1);
                    break;

                case Key.K_RIGHTARROW:
                    if (_Cursor == 0)
                        break;
                    Sound.LocalSound("misc/menu3.wav");
                    Change(1);
                    break;

                case Key.K_ENTER:
                    Sound.LocalSound("misc/menu2.wav");
                    if (_Cursor == 0)
                    {
                        if (Server.IsActive)
                            Cbuf.AddText("disconnect\n");
                        Cbuf.AddText("listen 0\n");	// so host_netport will be re-examined
                        Cbuf.AddText(String.Format("maxplayers {0}\n", _MaxPlayers));
                        Scr.BeginLoadingPlaque();

                        if (Common.GameKind == GameKind.Hipnotic)
                            Cbuf.AddText(String.Format("map {0}\n",
                                HipnoticLevels[HipnoticEpisodes[_StartEpisode].firstLevel + _StartLevel].name));
                        else if (Common.GameKind == GameKind.Rogue)
                            Cbuf.AddText(String.Format("map {0}\n",
                                RogueLevels[RogueEpisodes[_StartEpisode].firstLevel + _StartLevel].name));
                        else
                            Cbuf.AddText(String.Format("map {0}\n", Levels[Episodes[_StartEpisode].firstLevel + _StartLevel].name));

                        return;
                    }

                    Change(1);
                    break;
            }
        }

        /// <summary>
        /// M_NetStart_Change
        /// </summary>
        void Change(int dir)
        {
            int count;

            switch (_Cursor)
            {
                case 1:
                    _MaxPlayers += dir;
                    if (_MaxPlayers > Server.svs.maxclientslimit)
                    {
                        _MaxPlayers = Server.svs.maxclientslimit;
                        _ServerInfoMessage = true;
                        _ServerInfoMessageTime = Host.RealTime;
                    }
                    if (_MaxPlayers < 2)
                        _MaxPlayers = 2;
                    break;

                case 2:
                    Cvar.Set("coop", Host.IsCoop ? 0 : 1);
                    break;

                case 3:
                    if (Common.GameKind == GameKind.Rogue)
                        count = 6;
                    else
                        count = 2;

                    float tp = Host.TeamPlay + dir;
                    if (tp > count)
                        tp = 0;
                    else if (tp < 0)
                        tp = count;

                    Cvar.Set("teamplay", tp);
                    break;

                case 4:
                    float skill = Host.Skill + dir;
                    if (skill > 3)
                        skill = 0;
                    if (skill < 0)
                        skill = 3;
                    Cvar.Set("skill", skill);
                    break;

                case 5:
                    float fraglimit = Host.FragLimit + dir * 10;
                    if (fraglimit > 100)
                        fraglimit = 0;
                    if (fraglimit < 0)
                        fraglimit = 100;
                    Cvar.Set("fraglimit", fraglimit);
                    break;

                case 6:
                    float timelimit = Host.TimeLimit + dir * 5;
                    if (timelimit > 60)
                        timelimit = 0;
                    if (timelimit < 0)
                        timelimit = 60;
                    Cvar.Set("timelimit", timelimit);
                    break;

                case 7:
                    _StartEpisode += dir;
                    //MED 01/06/97 added hipnotic count
                    if (Common.GameKind == GameKind.Hipnotic)
                        count = 6;
                    //PGM 01/07/97 added rogue count
                    //PGM 03/02/97 added 1 for dmatch episode
                    else if (Common.GameKind == GameKind.Rogue)
                        count = 4;
                    else if (Common.IsRegistered)
                        count = 7;
                    else
                        count = 2;

                    if (_StartEpisode < 0)
                        _StartEpisode = count - 1;

                    if (_StartEpisode >= count)
                        _StartEpisode = 0;

                    _StartLevel = 0;
                    break;

                case 8:
                    _StartLevel += dir;
                    //MED 01/06/97 added hipnotic episodes
                    if (Common.GameKind == GameKind.Hipnotic)
                        count = HipnoticEpisodes[_StartEpisode].levels;
                    //PGM 01/06/97 added hipnotic episodes
                    else if (Common.GameKind == GameKind.Rogue)
                        count = RogueEpisodes[_StartEpisode].levels;
                    else
                        count = Episodes[_StartEpisode].levels;

                    if (_StartLevel < 0)
                        _StartLevel = count - 1;

                    if (_StartLevel >= count)
                        _StartLevel = 0;
                    break;
            }
        }

        public override void Draw()
        {
            Menu.DrawTransPic(16, 4, Drawer.CachePic("gfx/qplaque.lmp"));
            glpic_t p = Drawer.CachePic("gfx/p_multi.lmp");
            Menu.DrawPic((320 - p.width) / 2, 4, p);

            Menu.DrawTextBox(152, 32, 10, 1);
            Menu.Print(160, 40, "begin game");

            Menu.Print(0, 56, "      Max players");
            Menu.Print(160, 56, _MaxPlayers.ToString());

            Menu.Print(0, 64, "        Game Type");
            if (Host.IsCoop)
                Menu.Print(160, 64, "Cooperative");
            else
                Menu.Print(160, 64, "Deathmatch");

            Menu.Print(0, 72, "        Teamplay");
            if (Common.GameKind == GameKind.Rogue)
            {
                string msg;
                switch ((int)Host.TeamPlay)
                {
                    case 1: msg = "No Friendly Fire"; break;
                    case 2: msg = "Friendly Fire"; break;
                    case 3: msg = "Tag"; break;
                    case 4: msg = "Capture the Flag"; break;
                    case 5: msg = "One Flag CTF"; break;
                    case 6: msg = "Three Team CTF"; break;
                    default:
                        msg = "Off";
                        break;
                }
                Menu.Print(160, 72, msg);
            }
            else
            {
                string msg;
                switch ((int)Host.TeamPlay)
                {
                    case 1: msg = "No Friendly Fire"; break;
                    case 2: msg = "Friendly Fire"; break;
                    default:
                        msg = "Off";
                        break;
                }
                Menu.Print(160, 72, msg);
            }

            Menu.Print(0, 80, "            Skill");
            if (Host.Skill == 0)
                Menu.Print(160, 80, "Easy difficulty");
            else if (Host.Skill == 1)
                Menu.Print(160, 80, "Normal difficulty");
            else if (Host.Skill == 2)
                Menu.Print(160, 80, "Hard difficulty");
            else
                Menu.Print(160, 80, "Nightmare difficulty");

            Menu.Print(0, 88, "       Frag Limit");
            if (Host.FragLimit == 0)
                Menu.Print(160, 88, "none");
            else
                Menu.Print(160, 88, String.Format("{0} frags", (int)Host.FragLimit));

            Menu.Print(0, 96, "       Time Limit");
            if (Host.TimeLimit == 0)
                Menu.Print(160, 96, "none");
            else
                Menu.Print(160, 96, String.Format("{0} minutes", (int)Host.TimeLimit));

            Menu.Print(0, 112, "         Episode");
            //MED 01/06/97 added hipnotic episodes
            if (Common.GameKind == GameKind.Hipnotic)
                Menu.Print(160, 112, HipnoticEpisodes[_StartEpisode].description);
            //PGM 01/07/97 added rogue episodes
            else if (Common.GameKind == GameKind.Rogue)
                Menu.Print(160, 112, RogueEpisodes[_StartEpisode].description);
            else
                Menu.Print(160, 112, Episodes[_StartEpisode].description);

            Menu.Print(0, 120, "           Level");
            //MED 01/06/97 added hipnotic episodes
            if (Common.GameKind == GameKind.Hipnotic)
            {
                Menu.Print(160, 120, HipnoticLevels[HipnoticEpisodes[_StartEpisode].firstLevel + _StartLevel].description);
                Menu.Print(160, 128, HipnoticLevels[HipnoticEpisodes[_StartEpisode].firstLevel + _StartLevel].name);
            }
            //PGM 01/07/97 added rogue episodes
            else if (Common.GameKind == GameKind.Rogue)
            {
                Menu.Print(160, 120, RogueLevels[RogueEpisodes[_StartEpisode].firstLevel + _StartLevel].description);
                Menu.Print(160, 128, RogueLevels[RogueEpisodes[_StartEpisode].firstLevel + _StartLevel].name);
            }
            else
            {
                Menu.Print(160, 120, Levels[Episodes[_StartEpisode].firstLevel + _StartLevel].description);
                Menu.Print(160, 128, Levels[Episodes[_StartEpisode].firstLevel + _StartLevel].name);
            }

            // line cursor
            Menu.DrawCharacter(144, _CursorTable[_Cursor], 12 + ((int)(Host.RealTime * 4) & 1));

            if (_ServerInfoMessage)
            {
                if ((Host.RealTime - _ServerInfoMessageTime) < 5.0)
                {
                    int x = (320 - 26 * 8) / 2;
                    Menu.DrawTextBox(x, 138, 24, 4);
                    x += 8;
                    Menu.Print(x, 146, "  More than 4 players   ");
                    Menu.Print(x, 154, " requires using command ");
                    Menu.Print(x, 162, "line parameters; please ");
                    Menu.Print(x, 170, "   see techinfo.txt.    ");
                }
                else
                {
                    _ServerInfoMessage = false;
                }
            }
        }
    }

    class SearchMenu : MenuBase
    {
        bool _SearchComplete;
        double _SearchCompleteTime;

        public override void Show()
        {
            base.Show();
            Net.SlistSilent = true;
            Net.SlistLocal = false;
            _SearchComplete = false;
            Net.Slist_f();
        }

        public override void KeyEvent(int key)
        {
            // nothing to do
        }

        public override void Draw()
        {
            glpic_t p = Drawer.CachePic("gfx/p_multi.lmp");
            Menu.DrawPic((320 - p.width) / 2, 4, p);
            int x = (320 / 2) - ((12 * 8) / 2) + 4;
            Menu.DrawTextBox(x - 8, 32, 12, 1);
            Menu.Print(x, 40, "Searching...");

            if (Net.SlistInProgress)
            {
                Net.Poll();
                return;
            }

            if (!_SearchComplete)
            {
                _SearchComplete = true;
                _SearchCompleteTime = Host.RealTime;
            }

            if (Net.HostCacheCount > 0)
            {
                MenuBase.ServerListMenu.Show();
                return;
            }

            Menu.PrintWhite((320 / 2) - ((22 * 8) / 2), 64, "No Quake servers found");
            if ((Host.RealTime - _SearchCompleteTime) < 3.0)
                return;

            MenuBase.LanConfigMenu.Show();
        }
    }

    class ServerListMenu : MenuBase
    {
        bool _Sorted;

        public override void Show()
        {
            base.Show();
            _Cursor = 0;
            Menu.ReturnOnError = false;
            Menu.ReturnReason = String.Empty;
            _Sorted = false;
        }

        public override void KeyEvent(int key)
        {
            switch (key)
            {
                case Key.K_ESCAPE:
                    MenuBase.LanConfigMenu.Show();
                    break;

                case Key.K_SPACE:
                    MenuBase.SearchMenu.Show();
                    break;

                case Key.K_UPARROW:
                case Key.K_LEFTARROW:
                    Sound.LocalSound("misc/menu1.wav");
                    _Cursor--;
                    if (_Cursor < 0)
                        _Cursor = Net.HostCacheCount - 1;
                    break;

                case Key.K_DOWNARROW:
                case Key.K_RIGHTARROW:
                    Sound.LocalSound("misc/menu1.wav");
                    _Cursor++;
                    if (_Cursor >= Net.HostCacheCount)
                        _Cursor = 0;
                    break;

                case Key.K_ENTER:
                    Sound.LocalSound("misc/menu2.wav");
                    Menu.ReturnMenu = this;
                    Menu.ReturnOnError = true;
                    _Sorted = false;
                    MenuBase.Hide();
                    Cbuf.AddText(String.Format("connect \"{0}\"\n", Net.HostCache[_Cursor].cname));
                    break;

                default:
                    break;
            }
        }

        public override void Draw()
        {
            if (!_Sorted)
            {
                if (Net.HostCacheCount > 1)
                {
                    Comparison<hostcache_t> cmp = delegate(hostcache_t a, hostcache_t b)
                    {
                        return String.Compare(a.cname, b.cname);
                    };

                    Array.Sort(Net.HostCache, cmp);
                }
                _Sorted = true;
            }

            glpic_t p = Drawer.CachePic("gfx/p_multi.lmp");
            Menu.DrawPic((320 - p.width) / 2, 4, p);
            for (int n = 0; n < Net.HostCacheCount; n++)
            {
                hostcache_t hc = Net.HostCache[n];
                string tmp;
                if (hc.maxusers > 0)
                    tmp = String.Format("{0,-15} {1,-15} {2:D2}/{3:D2}\n", hc.name, hc.map, hc.users, hc.maxusers);
                else
                    tmp = String.Format("{0,-15} {1,-15}\n", hc.name, hc.map);
                Menu.Print(16, 32 + 8 * n, tmp);
            }
            Menu.DrawCharacter(0, 32 + _Cursor * 8, 12 + ((int)(Host.RealTime * 4) & 1));

            if (!String.IsNullOrEmpty(Menu.ReturnReason))
                Menu.PrintWhite(16, 148, Menu.ReturnReason);
        }
    }

    class VideoMenu : MenuBase
    {
        struct modedesc_t
        {
            public int modenum;
            public string desc;
            public bool iscur;
        } //modedesc_t;

        const int MAX_COLUMN_SIZE = 9;
        const int MODE_AREA_HEIGHT = MAX_COLUMN_SIZE + 2;
        const int MAX_MODEDESCS = MAX_COLUMN_SIZE * 3;

        int _WModes; // vid_wmodes
        modedesc_t[] _ModeDescs = new modedesc_t[MAX_MODEDESCS]; // modedescs

        public override void KeyEvent(int key)
        {
            switch (key)
            {
                case Key.K_ESCAPE:
                    Sound.LocalSound("misc/menu1.wav");
                    MenuBase.OptionsMenu.Show();
                    break;

                default:
                    break;
            }
        }

        public override void Draw()
        {
            glpic_t p = Drawer.CachePic("gfx/vidmodes.lmp");
            Menu.DrawPic((320 - p.width) / 2, 4, p);

            _WModes = 0;
            int lnummodes = Vid.Modes.Length;

            for (int i = 1; (i < lnummodes) && (_WModes < MAX_MODEDESCS); i++)
            {
                mode_t m = Vid.Modes[i];

                int k = _WModes;

                _ModeDescs[k].modenum = i;
                _ModeDescs[k].desc = String.Format("{0}x{1}x{2}", m.width, m.height, m.bpp);
                _ModeDescs[k].iscur = false;

                if (i == Vid.ModeNum)
                    _ModeDescs[k].iscur = true;

                _WModes++;
            }

            if (_WModes > 0)
            {
                Menu.Print(2 * 8, 36 + 0 * 8, "Fullscreen Modes (WIDTHxHEIGHTxBPP)");

                int column = 8;
                int row = 36 + 2 * 8;

                for (int i = 0; i < _WModes; i++)
                {
                    if (_ModeDescs[i].iscur)
                        Menu.PrintWhite(column, row, _ModeDescs[i].desc);
                    else
                        Menu.Print(column, row, _ModeDescs[i].desc);

                    column += 13 * 8;

                    if ((i % Vid.VID_ROW_SIZE) == (Vid.VID_ROW_SIZE - 1))
                    {
                        column = 8;
                        row += 8;
                    }
                }
            }

            Menu.Print(3 * 8, 36 + MODE_AREA_HEIGHT * 8 + 8 * 2, "Video modes must be set from the");
            Menu.Print(3 * 8, 36 + MODE_AREA_HEIGHT * 8 + 8 * 3, "command line with -width <width>");
            Menu.Print(3 * 8, 36 + MODE_AREA_HEIGHT * 8 + 8 * 4, "and -bpp <bits-per-pixel>");
            Menu.Print(3 * 8, 36 + MODE_AREA_HEIGHT * 8 + 8 * 6, "Select windowed mode with -window");
        }
    }
}
