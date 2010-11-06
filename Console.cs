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
using System.Runtime.InteropServices;
using System.IO;

namespace SharpQuake
{
    /// <summary>
    /// Con_functions
    /// </summary>
    static class Con
    {
        const string LOG_FILE_NAME = "qconsole.log";

        const int CON_TEXTSIZE = 16384;
        const int NUM_CON_TIMES = 4;

        static char[] _Text = new char[CON_TEXTSIZE]; // char		*con_text=0;
        static int _VisLines; // con_vislines
        static int _TotalLines;	// con_totallines   // total lines in console scrollback
        public static int BackScroll; // con_backscroll		// lines up from bottom to display
        static int _Current; // con_current		// where next message will be printed
        static int _X; // con_x		// offset in current line for next print
        static int _CR; // from Print()
        static double[] _Times = new double[NUM_CON_TIMES]; // con_times	// realtime time the line was generated
								// for transparent notify lines
        static int _LineWidth; // con_linewidth
        static bool _DebugLog; // qboolean	con_debuglog;
        static bool _IsInitialized; // qboolean con_initialized;
        static bool _ForcedUp; // qboolean con_forcedup		// because no entities to refresh
        static int _NotifyLines; // con_notifylines	// scan lines to clear for notify lines
        static Cvar  _NotifyTime; // con_notifytime = { "con_notifytime", "3" };		//seconds
        static float _CursorSpeed = 4; // con_cursorspeed
        static FileStream _Log;

        public static bool IsInitialized
        {
            get { return _IsInitialized; }
        }
        public static bool ForcedUp
        {
            get { return _ForcedUp; }
            set { _ForcedUp = value; }
        }
        public static int NotifyLines
        {
            get { return _NotifyLines; }
            set { _NotifyLines = value; }
        }
        public static int TotalLines
        {
            get { return _TotalLines; }
        }

        // Con_CheckResize (void)
        public static void CheckResize()
        {
            int width = (Scr.vid.width >> 3) - 2;
	        if (width == _LineWidth)
		        return;

	        if (width < 1)	// video hasn't been initialized yet
	        {
		        width = 38;
                _LineWidth = width; // con_linewidth = width;
		        _TotalLines = CON_TEXTSIZE / _LineWidth;
                Common.FillArray(_Text, ' '); // Q_memset (con_text, ' ', CON_TEXTSIZE);
	        }
	        else
	        {
		        int oldwidth = _LineWidth;
		        _LineWidth = width;
		        int oldtotallines = _TotalLines;
		        _TotalLines = CON_TEXTSIZE / _LineWidth;
		        int numlines = oldtotallines;

		        if (_TotalLines < numlines)
			        numlines = _TotalLines;

		        int numchars = oldwidth;
	
		        if (_LineWidth < numchars)
			        numchars = _LineWidth;

                char[] tmp = _Text;
                _Text = new char[CON_TEXTSIZE];
                Common.FillArray(_Text, ' ');
		        
		        for (int i = 0; i < numlines; i++)
		        {
			        for (int j = 0; j < numchars; j++)
			        {
                        _Text[(_TotalLines - 1 - i) * _LineWidth + j] = tmp[((_Current - i + oldtotallines) %
                                      oldtotallines) * oldwidth + j];
			        }
		        }

                ClearNotify();
	        }

	        BackScroll = 0;
	        _Current = _TotalLines - 1;
        }

        // Con_Init (void)
        public static void Init()
        {
	        _DebugLog = (Common.CheckParm("-condebug") > 0);
	        if (_DebugLog)
	        {
                string path = Path.Combine(Common.GameDir, LOG_FILE_NAME);
                if (File.Exists(path))
                    File.Delete(path);
                
                _Log = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read);
	        }

	        _LineWidth = -1;
	        CheckResize();	
	        
            Con.Print("Console initialized.\n");

            //
            // register our commands
            //
            if (_NotifyTime == null)
            {
                _NotifyTime = new Cvar("con_notifytime", "3");
            }

	        Cmd.Add("toggleconsole", ToggleConsole_f);
	        Cmd.Add("messagemode", MessageMode_f);
	        Cmd.Add("messagemode2", MessageMode2_f);
	        Cmd.Add("clear", Clear_f);
	        
            _IsInitialized = true;
        }

        // Con_DrawConsole
        //
        // Draws the console with the solid background
        // The typing input line at the bottom should only be drawn if typing is allowed
        public static void Draw(int lines, bool drawinput)
        {
            if (lines <= 0)
                return;

            // draw the background
            Drawer.DrawConsoleBackground(lines);

            // draw the text
            _VisLines = lines;

            int rows = (lines - 16) >> 3;		// rows of text to draw
            int y = lines - 16 - (rows << 3);	// may start slightly negative

            for (int i = _Current - rows + 1; i <= _Current; i++, y += 8)
            {
                int j = i - BackScroll;
                if (j < 0)
                    j = 0;

                int offset = (j % _TotalLines) * _LineWidth;

                for (int x = 0; x < _LineWidth; x++)
                    Drawer.DrawCharacter((x + 1) << 3, y, _Text[offset + x]);
            }

            // draw the input prompt, user text, and cursor if desired
            if (drawinput)
                DrawInput();
        }

        /// <summary>
        /// Con_Printf
        /// </summary>
        public static void Print(string fmt, params object[] args)
        {
            string msg = (args.Length > 0 ? String.Format(fmt, args) : fmt);
            
            // log all messages to file
            if (_DebugLog)
                DebugLog(msg);

	        if (!_IsInitialized)
		        return;
		
	        if (Client.cls.state == cactive_t.ca_dedicated)
		        return;		// no graphics mode

            // write it to the scrollable buffer
            Print(msg);
	
            // update the screen if the console is displayed
            if (Client.cls.signon != Client.SIGNONS && !Scr.IsDisabledForLoading)
                Scr.UpdateScreen();
        }

        /// <summary>
        /// Con_DebugLog
        /// </summary>
        static void DebugLog(string msg)
        {
            if (_Log != null)
            {
                byte[] tmp = Encoding.UTF8.GetBytes(msg);
                _Log.Write(tmp, 0, tmp.Length);
            }
        }

        public static void Shutdown()
        {
            if (_Log != null)
            {
                _Log.Flush();
                _Log.Dispose();
                _Log = null;
            }
        }

        // Con_Print (char *txt)
        //
        // Handles cursor positioning, line wrapping, etc
        // All console printing must go through this in order to be logged to disk
        // If no console is visible, the notify window will pop up.
        static void Print(string txt)
        {
	        int mask, offset = 0;
	
	        BackScroll = 0;

	        if (txt.StartsWith(((char)1).ToString()))// [0] == 1)
	        {
		        mask = 128;	// go to colored text
                Sound.LocalSound("misc/talk.wav"); // play talk wav
		        offset++;
	        }
	        else if (txt.StartsWith(((char)2).ToString())) //txt[0] == 2)
	        {
		        mask = 128;	// go to colored text
		        offset++;
	        }
	        else
		        mask = 0;

            while (offset < txt.Length)
	        {
                char c = txt[offset];
                
                int l;
	            // count word length
                for (l = 0; l < _LineWidth && offset + l < txt.Length; l++)
                {
                    if (txt[offset + l] <= ' ')
                        break;
                }

	            // word wrap
		        if (l != _LineWidth && (_X + l > _LineWidth))
			        _X = 0;

		        offset++;

                if (_CR != 0)
                {
                    _Current--;
                    _CR = 0;
                }
		
		        if (_X == 0)
		        {
			        LineFeed();
		            // mark time for transparent overlay
			        if (_Current >= 0)
				        _Times[_Current % NUM_CON_TIMES] = Host.RealTime; // realtime
		        }

		        switch (c)
		        {
		            case '\n':
			            _X = 0;
			            break;

		            case '\r':
			            _X = 0;
			            _CR = 1;
			            break;

		            default:	// display character and advance
			            int y = _Current % _TotalLines;
			            _Text[y * _LineWidth + _X] = (char)(c | mask);
			            _X++;
			            if (_X >= _LineWidth)
				            _X = 0;
			            break;
		        }
	        }
        }

        //
        // Con_DPrintf
        //
        // A Con_Printf that only shows up if the "developer" cvar is set
        public static void DPrint(string fmt, params object[] args)
        {
            // don't confuse non-developers with techie stuff...
	        if (Host.IsDeveloper)
                Print(fmt, args);
        }

        // Con_SafePrintf (char *fmt, ...)
        //
        // Okay to call even when the screen can't be updated
        public static void SafePrint(string fmt, params object[] args)
        {
	        bool temp = Scr.IsDisabledForLoading;
	        Scr.IsDisabledForLoading = true;
	        Print(fmt, args);
	        Scr.IsDisabledForLoading = temp;
        }

        /// <summary>
        /// Con_Clear_f
        /// </summary>
        static void Clear_f()
        {
            Common.FillArray(_Text, ' ');
        }
        
        /// <summary>
        /// Con_DrawNotify
        /// </summary>
        public static void DrawNotify()
        {
            int v = 0;
            for (int i = _Current - NUM_CON_TIMES + 1; i <= _Current; i++)
            {
                if (i < 0)
                    continue;
                double time = _Times[i % NUM_CON_TIMES];
                if (time == 0)
                    continue;
                time = Host.RealTime - time;
                if (time > _NotifyTime.Value)
                    continue;

                int textOffset = (i % _TotalLines) * _LineWidth;

                Scr.ClearNotify = 0;
                Scr.CopyTop = true;

                for (int x = 0; x < _LineWidth; x++)
                    Drawer.DrawCharacter((x + 1) << 3, v, _Text[textOffset + x]);

                v += 8;
            }

            if (Key.Destination == keydest_t.key_message)
            {
                Scr.ClearNotify = 0;
                Scr.CopyTop = true;

                int x = 0;

                Drawer.DrawString(8, v, "say:");
                string chat = Key.ChatBuffer;
                for (; x < chat.Length; x++)
                {
                    Drawer.DrawCharacter((x + 5) << 3, v, chat[x]);
                }
                Drawer.DrawCharacter((x + 5) << 3, v, 10 + ((int)(Host.RealTime * _CursorSpeed) & 1));
                v += 8;
            }

            if (v > _NotifyLines)
                _NotifyLines = v;

        }

        // Con_ClearNotify (void)
        public static void ClearNotify()
        {
            for (int i = 0; i < NUM_CON_TIMES; i++)
                _Times[i] = 0;
        }

        /// <summary>
        /// Con_ToggleConsole_f
        /// </summary>
        public static void ToggleConsole_f()
        {
            if (Key.Destination == keydest_t.key_console)
            {
                if (Client.cls.state == cactive_t. ca_connected)
                {
                    Key.Destination = keydest_t.key_game;
                    Key.Lines[Key.EditLine][1] = '\0';	// clear any typing
                    Key.LinePos = 1;
                }
                else
                {
                    MenuBase.MainMenu.Show();
                }
            }
            else
                Key.Destination = keydest_t.key_console;

            Scr.EndLoadingPlaque();
            Array.Clear(_Times, 0, _Times.Length);
        }

        // Con_MessageMode_f
        static void MessageMode_f()
        {
	        Key.Destination = keydest_t.key_message;
	        Key.TeamMessage = false;
        }

						
        //Con_MessageMode2_f
        static void MessageMode2_f()
        {
	        Key.Destination = keydest_t.key_message;
	        Key.TeamMessage = true;
        }

        
        // Con_Linefeed
        static void LineFeed()
        {
	        _X = 0;
	        _Current++;

            for (int i = 0; i < _LineWidth; i++)
            {
                _Text[(_Current % _TotalLines) * _LineWidth + i] = ' ';
            }
        }

        // Con_DrawInput
        //
        // The input line scrolls horizontally if typing goes beyond the right edge
        static void DrawInput()
        {
            if (Key.Destination != keydest_t.key_console && !_ForcedUp)
                return;		// don't draw anything

            // add the cursor frame
            Key.Lines[Key.EditLine][Key.LinePos] = (char)(10 + ((int)(Host.RealTime * _CursorSpeed) & 1));

            // fill out remainder with spaces
            for (int i = Key.LinePos + 1; i < _LineWidth; i++)
                Key.Lines[Key.EditLine][i] = ' ';

            //	prestep if horizontally scrolling
            int offset = 0;
            if (Key.LinePos >= _LineWidth)
                offset = 1 + Key.LinePos - _LineWidth;
            //text += 1 + key_linepos - con_linewidth;

            // draw it
            int y = _VisLines - 16;

            for (int i = 0; i < _LineWidth; i++)
                Drawer.DrawCharacter((i + 1) << 3, _VisLines - 16, Key.Lines[Key.EditLine][offset + i]);

            // remove cursor
            Key.Lines[Key.EditLine][Key.LinePos] = '\0';
        }
    }
}
