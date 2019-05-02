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
using System.Drawing;
using System.IO;
using OpenTK;
using OpenTK.Graphics.OpenGL;

// screen.h
// gl_screen.c

namespace SharpQuake
{
    /// <summary>
    /// SCR_functions
    /// </summary>
    static partial class Scr
    {
        public static viddef_t vid
        {
            get
            {
                return _VidDef;
            }
        }

        public static cvar ViewSize
        {
            get
            {
                return _ViewSize;
            }
        }

        public static float ConCurrent
        {
            get
            {
                return _ConCurrent;
            }
        }

        public static bool CopyEverithing
        {
            get
            {
                return _CopyEverything;
            }
            set
            {
                _CopyEverything = value;
            }
        }

        public static bool IsDisabledForLoading;
        public static bool BlockDrawing = false;
        public static bool SkipUpdate;

        // scr_skipupdate
        public static bool FullSbarDraw;

        // fullsbardraw = false
        public static bool IsPermedia;

        // only the refresh window will be updated unless these variables are flagged
        public static bool CopyTop;

        public static int ClearNotify;
        public static int glX;
        public static int glY;
        public static int glWidth;
        public static int glHeight;
        public static float CenterTimeOff;
        public static int FullUpdate;
        private static viddef_t _VidDef = new viddef_t();	// viddef_t vid (global video state)
        private static vrect_t _VRect; // scr_vrect

        // scr_disabled_for_loading
        private static bool _DrawLoading; // scr_drawloading

        private static double _DisabledTime; // float scr_disabled_time

        // qboolean block_drawing
        private static bool _DrawDialog; // scr_drawdialog

        // isPermedia
        private static bool _IsInitialized;

        private static bool _InUpdate;
        private static glpic_t _Ram;
        private static glpic_t _Net;
        private static glpic_t _Turtle;
        private static int _TurtleCount; // static count from SCR_DrawTurtle()
        private static bool _CopyEverything;

        private static float _ConCurrent; // scr_con_current
        private static float _ConLines;		// lines of console to display
        private static int _ClearConsole; // clearconsole
                                          // clearnotify

        private static float _OldScreenSize; // float oldscreensize
        private static float _OldFov; // float oldfov
        private static int _CenterLines; // scr_center_lines
        private static int _EraseLines; // scr_erase_lines

        //static int _EraseCenter; // scr_erase_center
        private static float _CenterTimeStart; // scr_centertime_start	// for slow victory printing

        // scr_centertime_off
        private static string _CenterString; // char	scr_centerstring[1024]

        private static cvar _ViewSize; // = { "viewsize", "100", true };
        private static cvar _Fov;// = { "fov", "90" };	// 10 - 170
        private static cvar _ConSpeed;// = { "scr_conspeed", "300" };
        private static cvar _CenterTime;// = { "scr_centertime", "2" };
        private static cvar _ShowRam;// = { "showram", "1" };
        private static cvar _ShowTurtle;// = { "showturtle", "0" };
        private static cvar _ShowPause;// = { "showpause", "1" };
        private static cvar _PrintSpeed;// = { "scr_printspeed", "8" };
        private static cvar _glTripleBuffer;// = { "gl_triplebuffer", "1", true };

        private static string _NotifyString; // scr_notifystring
        private static bool _IsMouseWindowed; // windowed_mouse (don't confuse with _windowed_mouse cvar)
                                              // scr_fullupdate    set to 0 to force full redraw

        // SCR_Init
        public static void Init()
        {
            if( _ViewSize == null )
            {
                _ViewSize = new cvar( "viewsize", "100", true );
                _Fov = new cvar( "fov", "90" );	// 10 - 170
                _ConSpeed = new cvar( "scr_conspeed", "3000" );
                _CenterTime = new cvar( "scr_centertime", "2" );
                _ShowRam = new cvar( "showram", "1" );
                _ShowTurtle = new cvar( "showturtle", "0" );
                _ShowPause = new cvar( "showpause", "1" );
                _PrintSpeed = new cvar( "scr_printspeed", "8" );
                _glTripleBuffer = new cvar( "gl_triplebuffer", "1", true );
            }

            //
            // register our commands
            //
            cmd.Add( "screenshot", ScreenShot_f );
            cmd.Add( "sizeup", SizeUp_f );
            cmd.Add( "sizedown", SizeDown_f );

            _Ram = Drawer.PicFromWad( "ram" );
            _Net = Drawer.PicFromWad( "net" );
            _Turtle = Drawer.PicFromWad( "turtle" );

            if( common.HasParam( "-fullsbar" ) )
                FullSbarDraw = true;

            _IsInitialized = true;
        }

        // void SCR_UpdateScreen (void);
        // This is called every frame, and can also be called explicitly to flush
        // text to the screen.
        //
        // WARNING: be very careful calling this from elsewhere, because the refresh
        // needs almost the entire 256k of stack space!
        public static void UpdateScreen()
        {
            if( BlockDrawing || !_IsInitialized || _InUpdate )
                return;

            _InUpdate = true;
            try
            {
                if( mainwindow.Instance != null )
                {
                    if( (mainwindow.Instance.VSync == VSyncMode.On ) != SharpQuake.vid.Wait )
                        mainwindow.Instance.VSync = (SharpQuake.vid.Wait ? VSyncMode.On : VSyncMode.Off );
                }

                _VidDef.numpages = 2 + (int)_glTripleBuffer.Value;

                CopyTop = false;
                _CopyEverything = false;

                if( IsDisabledForLoading )
                {
                    if( ( host.RealTime - _DisabledTime ) > 60 )
                    {
                        IsDisabledForLoading = false;
                        Con.Print( "Load failed.\n" );
                    }
                    else
                        return;
                }

                if( !Con.IsInitialized )
                    return;	// not initialized yet

                BeginRendering();

                //
                // determine size of refresh window
                //
                if( _OldFov != _Fov.Value )
                {
                    _OldFov = _Fov.Value;
                    _VidDef.recalc_refdef = true;
                }

                if( _OldScreenSize != _ViewSize.Value )
                {
                    _OldScreenSize = _ViewSize.Value;
                    _VidDef.recalc_refdef = true;
                }

                if( _VidDef.recalc_refdef )
                    CalcRefdef();

                //
                // do 3D refresh drawing, and then update the screen
                //
                SetUpToDrawConsole();

                view.RenderView();

                Set2D();

                //
                // draw any areas not covered by the refresh
                //
                Scr.TileClear();

                if( _DrawDialog )
                {
                    sbar.Draw();
                    Drawer.FadeScreen();
                    DrawNotifyString();
                    _CopyEverything = true;
                }
                else if( _DrawLoading )
                {
                    DrawLoading();
                    sbar.Draw();
                }
                else if( client.cl.intermission == 1 && Key.Destination == keydest_t.key_game )
                {
                    sbar.IntermissionOverlay();
                }
                else if( client.cl.intermission == 2 && Key.Destination == keydest_t.key_game )
                {
                    sbar.FinaleOverlay();
                    CheckDrawCenterString();
                }
                else
                {
                    if( view.Crosshair > 0 )
                        Drawer.DrawCharacter( _VRect.x + _VRect.width / 2, _VRect.y + _VRect.height / 2, '+' );

                    DrawRam();
                    DrawNet();
                    DrawTurtle();
                    DrawPause();
                    CheckDrawCenterString();
                    sbar.Draw();
                    DrawConsole();
                    menu.Draw();
                }

                view.UpdatePalette();
                EndRendering();
            }
            finally
            {
                _InUpdate = false;
            }
        }

        /// <summary>
        /// GL_EndRendering
        /// </summary>
        public static void EndRendering()
        {
            mainwindow form = mainwindow.Instance;
            if( form == null )
                return;

            if( !SkipUpdate || BlockDrawing )
                form.SwapBuffers();

            // handle the mouse state
            if( !SharpQuake.vid.WindowedMouse )
            {
                if(_IsMouseWindowed)
                {
                    input.DeactivateMouse();
                    input.ShowMouse();
                    _IsMouseWindowed = false;
                }
            }
            else
            {
                _IsMouseWindowed = true;
                if(Key.Destination == keydest_t.key_game && !input.IsMouseActive &&
                    client.cls.state != cactive_t.ca_disconnected )// && ActiveApp)
                {
                    input.ActivateMouse();
                    input.HideMouse();
                }
                else if(input.IsMouseActive && Key.Destination != keydest_t.key_game )
                {
                    input.DeactivateMouse();
                    input.ShowMouse();
                }
            }

            if( FullSbarDraw )
                sbar.Changed();
        }

        // SCR_CenterPrint
        //
        // Called for important messages that should stay in the center of the screen
        // for a few moments
        public static void CenterPrint( string str )
        {
            _CenterString = str;
            CenterTimeOff = _CenterTime.Value;
            _CenterTimeStart = (float)client.cl.time;

            // count the number of lines for centering
            _CenterLines = 1;
            foreach( char c in _CenterString )
            {
                if( c == '\n' )
                    _CenterLines++;
            }
        }

        /// <summary>
        /// SCR_EndLoadingPlaque
        /// </summary>
        public static void EndLoadingPlaque()
        {
            Scr.IsDisabledForLoading = false;
            Scr.FullUpdate = 0;
            Con.ClearNotify();
        }

        /// <summary>
        /// SCR_BeginLoadingPlaque
        /// </summary>
        public static void BeginLoadingPlaque()
        {
            snd.StopAllSounds( true );

            if( client.cls.state != cactive_t.ca_connected )
                return;
            if( client.cls.signon != client.SIGNONS )
                return;

            // redraw with no console and the loading plaque
            Con.ClearNotify();
            CenterTimeOff = 0;
            _ConCurrent = 0;

            _DrawLoading = true;
            Scr.FullUpdate = 0;
            sbar.Changed();
            UpdateScreen();
            _DrawLoading = false;

            Scr.IsDisabledForLoading = true;
            _DisabledTime = host.RealTime;
            Scr.FullUpdate = 0;
        }

        /// <summary>
        /// SCR_ModalMessage
        /// Displays a text string in the center of the screen and waits for a Y or N keypress.
        /// </summary>
        public static bool ModalMessage( string text )
        {
            if( client.cls.state == cactive_t.ca_dedicated )
                return true;

            _NotifyString = text;

            // draw a fresh screen
            Scr.FullUpdate = 0;
            _DrawDialog = true;
            UpdateScreen();
            _DrawDialog = false;

            snd.ClearBuffer();		// so dma doesn't loop current sound

            do
            {
                Key.KeyCount = -1;		// wait for a key down and up
                sys.SendKeyEvents();
            } while( Key.LastPress != 'y' && Key.LastPress != 'n' && Key.LastPress != Key.K_ESCAPE );

            Scr.FullUpdate = 0;
            UpdateScreen();

            return ( Key.LastPress == 'y' );
        }

        // SCR_SizeUp_f
        //
        // Keybinding command
        private static void SizeUp_f()
        {
            cvar.Set( "viewsize", _ViewSize.Value + 10 );
            _VidDef.recalc_refdef = true;
        }

        // SCR_SizeDown_f
        //
        // Keybinding command
        private static void SizeDown_f()
        {
            cvar.Set( "viewsize", _ViewSize.Value - 10 );
            _VidDef.recalc_refdef = true;
        }

        // SCR_ScreenShot_f
        private static void ScreenShot_f()
        {
            //
            // find a file name to save it to
            //
            string path = null;
            int i;
            for( i = 0; i <= 999; i++ )
            {
                path = Path.Combine( common.GameDir, String.Format( "quake{0:D3}.tga", i ) );
                if( sys.GetFileTime( path ) == DateTime.MinValue )
                    break;	// file doesn't exist
            }
            if( i == 100 )
            {
                Con.Print( "SCR_ScreenShot_f: Couldn't create a file\n" );
                return;
            }

            FileStream fs = sys.FileOpenWrite( path, true );
            if( fs == null )
            {
                Con.Print( "SCR_ScreenShot_f: Couldn't create a file\n" );
                return;
            }
            using( BinaryWriter writer = new BinaryWriter( fs ) )
            {
                // Write tga header (18 bytes)
                writer.Write( (ushort)0 );
                writer.Write( (byte)2 ); //buffer[2] = 2; uncompressed type
                writer.Write( (byte)0 );
                writer.Write( (uint)0 );
                writer.Write( (uint)0 );
                writer.Write( (byte)( glWidth & 0xff ) );
                writer.Write( (byte)( glWidth >> 8 ) );
                writer.Write( (byte)( glHeight & 0xff ) );
                writer.Write( (byte)( glHeight >> 8 ) );
                writer.Write( (byte)24 ); // pixel size
                writer.Write( (ushort)0 );

                byte[] buffer = new byte[glWidth * glHeight * 3];
                GL.ReadPixels( glX, glY, glWidth, glHeight, PixelFormat.Rgb, PixelType.UnsignedByte, buffer );

                // swap 012 to 102
                int c = glWidth * glHeight * 3;
                for( i = 0; i < c; i += 3 )
                {
                    byte temp = buffer[i + 0];
                    buffer[i + 0] = buffer[i + 1];
                    buffer[i + 1] = temp;
                }
                writer.Write( buffer, 0, buffer.Length );
            }
            Con.Print( "Wrote {0}\n", Path.GetFileName( path ) );
        }

        /// <summary>
        /// GL_BeginRendering
        /// </summary>
        private static void BeginRendering()
        {
            glX = 0;
            glY = 0;
            glWidth = 0;
            glHeight = 0;

            INativeWindow window = mainwindow.Instance;
            if( window != null )
            {
                Size size = window.ClientSize;
                glWidth = size.Width;
                glHeight = size.Height;
            }
        }

        // SCR_CalcRefdef
        //
        // Must be called whenever vid changes
        // Internal use only
        private static void CalcRefdef()
        {
            Scr.FullUpdate = 0; // force a background redraw
            _VidDef.recalc_refdef = false;

            // force the status bar to redraw
            sbar.Changed();

            // bound viewsize
            if( _ViewSize.Value < 30 )
                cvar.Set( "viewsize", "30" );
            if( _ViewSize.Value > 120 )
                cvar.Set( "viewsize", "120" );

            // bound field of view
            if( _Fov.Value < 10 )
                cvar.Set( "fov", "10" );
            if( _Fov.Value > 170 )
                cvar.Set( "fov", "170" );

            // intermission is always full screen
            float size;
            if( client.cl.intermission > 0 )
                size = 120;
            else
                size = _ViewSize.Value;

            if( size >= 120 )
                sbar.Lines = 0; // no status bar at all
            else if( size >= 110 )
                sbar.Lines = 24; // no inventory
            else
                sbar.Lines = 24 + 16 + 8;

            bool full = false;
            if( _ViewSize.Value >= 100.0 )
            {
                full = true;
                size = 100.0f;
            }
            else
                size = _ViewSize.Value;

            if( client.cl.intermission > 0 )
            {
                full = true;
                size = 100;
                sbar.Lines = 0;
            }
            size /= 100.0f;

            int h = _VidDef.height - sbar.Lines;

            refdef_t rdef = render.RefDef;
            rdef.vrect.width = (int)( _VidDef.width * size );
            if( rdef.vrect.width < 96 )
            {
                size = 96.0f / rdef.vrect.width;
                rdef.vrect.width = 96;  // min for icons
            }

            rdef.vrect.height = (int)( _VidDef.height * size );
            if( rdef.vrect.height > _VidDef.height - sbar.Lines )
                rdef.vrect.height = _VidDef.height - sbar.Lines;
            if( rdef.vrect.height > _VidDef.height )
                rdef.vrect.height = _VidDef.height;
            rdef.vrect.x = ( _VidDef.width - rdef.vrect.width ) / 2;
            if( full )
                rdef.vrect.y = 0;
            else
                rdef.vrect.y = ( h - rdef.vrect.height ) / 2;

            rdef.fov_x = _Fov.Value;
            rdef.fov_y = CalcFov( rdef.fov_x, rdef.vrect.width, rdef.vrect.height );

            _VRect = rdef.vrect;
        }

        // CalcFov
        private static float CalcFov( float fov_x, float width, float height )
        {
            if( fov_x < 1 || fov_x > 179 )
                sys.Error( "Bad fov: {0}", fov_x );

            double x = width / Math.Tan( fov_x / 360.0 * Math.PI );
            double a = Math.Atan( height / x );
            a = a * 360.0 / Math.PI;
            return (float)a;
        }

        /// <summary>
        /// SCR_SetUpToDrawConsole
        /// </summary>
        private static void SetUpToDrawConsole()
        {
            Con.CheckResize();

            if( _DrawLoading )
                return;     // never a console with loading plaque

            // decide on the height of the console
            Con.ForcedUp = ( client.cl.worldmodel == null ) || ( client.cls.signon != client.SIGNONS );

            if( Con.ForcedUp )
            {
                _ConLines = _VidDef.height; // full screen
                _ConCurrent = _ConLines;
            }
            else if( Key.Destination == keydest_t.key_console )
                _ConLines = _VidDef.height / 2; // half screen
            else
                _ConLines = 0; // none visible

            if( _ConLines < _ConCurrent )
            {
                _ConCurrent -= (int)( _ConSpeed.Value * host.FrameTime );
                if( _ConLines > _ConCurrent )
                    _ConCurrent = _ConLines;
            }
            else if( _ConLines > _ConCurrent )
            {
                _ConCurrent += (int)( _ConSpeed.Value * host.FrameTime );
                if( _ConLines < _ConCurrent )
                    _ConCurrent = _ConLines;
            }

            if( _ClearConsole++ < _VidDef.numpages )
            {
                sbar.Changed();
            }
            else if( ClearNotify++ < _VidDef.numpages )
            {
                //????????????
            }
            else
                Con.NotifyLines = 0;
        }

        // SCR_TileClear
        private static void TileClear()
        {
            refdef_t rdef = render.RefDef;
            if( rdef.vrect.x > 0 )
            {
                // left
                Drawer.TileClear( 0, 0, rdef.vrect.x, _VidDef.height - sbar.Lines );
                // right
                Drawer.TileClear( rdef.vrect.x + rdef.vrect.width, 0,
                    _VidDef.width - rdef.vrect.x + rdef.vrect.width,
                    _VidDef.height - sbar.Lines );
            }
            if( rdef.vrect.y > 0 )
            {
                // top
                Drawer.TileClear( rdef.vrect.x, 0, rdef.vrect.x + rdef.vrect.width, rdef.vrect.y );
                // bottom
                Drawer.TileClear( rdef.vrect.x, rdef.vrect.y + rdef.vrect.height,
                    rdef.vrect.width, _VidDef.height - sbar.Lines - ( rdef.vrect.height + rdef.vrect.y ) );
            }
        }

        /// <summary>
        /// SCR_DrawNotifyString
        /// </summary>
        private static void DrawNotifyString()
        {
            int offset = 0;
            int y = (int)( Scr.vid.height * 0.35 );

            do
            {
                int end = _NotifyString.IndexOf( '\n', offset );
                if( end == -1 )
                    end = _NotifyString.Length;
                if( end - offset > 40 )
                    end = offset + 40;

                int length = end - offset;
                if( length > 0 )
                {
                    int x = ( vid.width - length * 8 ) / 2;
                    for( int j = 0; j < length; j++, x += 8 )
                        Drawer.DrawCharacter( x, y, _NotifyString[offset + j] );

                    y += 8;
                }
                offset = end + 1;
            } while( offset < _NotifyString.Length );
        }

        /// <summary>
        /// SCR_DrawLoading
        /// </summary>
        private static void DrawLoading()
        {
            if( !_DrawLoading )
                return;

            glpic_t pic = Drawer.CachePic( "gfx/loading.lmp" );
            Drawer.DrawPic( ( vid.width - pic.width ) / 2, ( vid.height - 48 - pic.height ) / 2, pic );
        }

        // SCR_CheckDrawCenterString
        private static void CheckDrawCenterString()
        {
            CopyTop = true;
            if( _CenterLines > _EraseLines )
                _EraseLines = _CenterLines;

            CenterTimeOff -= (float)host.FrameTime;

            if( CenterTimeOff <= 0 && client.cl.intermission == 0 )
                return;
            if( Key.Destination != keydest_t.key_game )
                return;

            DrawCenterString();
        }

        // SCR_DrawRam
        private static void DrawRam()
        {
            if( _ShowRam.Value == 0 )
                return;

            if( !render.CacheTrash )
                return;

            Drawer.DrawPic( _VRect.x + 32, _VRect.y, _Ram );
        }

        // SCR_DrawTurtle
        private static void DrawTurtle()
        {
            //static int	count;

            if( _ShowTurtle.Value == 0 )
                return;

            if( host.FrameTime < 0.1 )
            {
                _TurtleCount = 0;
                return;
            }

            _TurtleCount++;
            if( _TurtleCount < 3 )
                return;

            Drawer.DrawPic( _VRect.x, _VRect.y, _Turtle );
        }

        // SCR_DrawNet
        private static void DrawNet()
        {
            if( host.RealTime - client.cl.last_received_message < 0.3 )
                return;
            if( client.cls.demoplayback )
                return;

            Drawer.DrawPic( _VRect.x + 64, _VRect.y, _Net );
        }

        // DrawPause
        private static void DrawPause()
        {
            if( _ShowPause.Value == 0 )	// turn off for screenshots
                return;

            if( !client.cl.paused )
                return;

            glpic_t pic = Drawer.CachePic( "gfx/pause.lmp" );
            Drawer.DrawPic( ( vid.width - pic.width ) / 2, ( vid.height - 48 - pic.height ) / 2, pic );
        }

        // SCR_DrawConsole
        private static void DrawConsole()
        {
            if( _ConCurrent > 0 )
            {
                _CopyEverything = true;
                Con.Draw( (int)_ConCurrent, true );
                _ClearConsole = 0;
            }
            else if( Key.Destination == keydest_t.key_game ||
                Key.Destination == keydest_t.key_message )
            {
                Con.DrawNotify();	// only draw notify in game
            }
        }

        // SCR_DrawCenterString
        private static void DrawCenterString()
        {
            int remaining;

            // the finale prints the characters one at a time
            if( client.cl.intermission > 0 )
                remaining = (int)( _PrintSpeed.Value * ( client.cl.time - _CenterTimeStart ) );
            else
                remaining = 9999;

            int y = 48;
            if( _CenterLines <= 4 )
                y = (int)( _VidDef.height * 0.35 );

            string[] lines = _CenterString.Split( '\n' );
            for( int i = 0; i < lines.Length; i++ )
            {
                string line = lines[i].TrimEnd( '\r' );
                int x = ( vid.width - line.Length * 8 ) / 2;

                for( int j = 0; j < line.Length; j++, x += 8 )
                {
                    Drawer.DrawCharacter( x, y, line[j] );
                    if( remaining-- <= 0 )
                        return;
                }
                y += 8;
            }
        }
    }
}
