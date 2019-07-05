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

using System;
using System.Drawing;
using System.IO;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using SharpQuake.Framework;

// screen.h
// gl_screen.c

namespace SharpQuake
{
    /// <summary>
    /// SCR_functions
    /// </summary>
    public partial class Scr
    {
        public VidDef vid
        {
            get
            {
                return _VidDef;
            }
        }

        public CVar ViewSize
        {
            get
            {
                return _ViewSize;
            }
        }

        public Single ConCurrent
        {
            get
            {
                return _ConCurrent;
            }
        }

        public System.Boolean CopyEverithing
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

        public System.Boolean IsDisabledForLoading;
        public System.Boolean BlockDrawing = false;
        public System.Boolean SkipUpdate;

        // scr_skipupdate
        public System.Boolean FullSbarDraw;

        // fullsbardraw = false
        public System.Boolean IsPermedia;

        // only the refresh window will be updated unless these variables are flagged
        public System.Boolean CopyTop;

        public Int32 ClearNotify;
        public Int32 glX;
        public Int32 glY;
        public Int32 glWidth;
        public Int32 glHeight;
        public Single CenterTimeOff;
        public Int32 FullUpdate;
        private VidDef _VidDef = new VidDef();	// viddef_t vid (global video state)
        private VRect _VRect; // scr_vrect

        // scr_disabled_for_loading
        private System.Boolean _DrawLoading; // scr_drawloading

        private Double _DisabledTime; // float scr_disabled_time

        // qboolean block_drawing
        private System.Boolean _DrawDialog; // scr_drawdialog

        // isPermedia
        private System.Boolean _IsInitialized;

        private System.Boolean _InUpdate;
        private GLPic _Ram;
        private GLPic _Net;
        private GLPic _Turtle;
        private Int32 _TurtleCount; // count from SCR_DrawTurtle()
        private System.Boolean _CopyEverything;

        private Single _ConCurrent; // scr_con_current
        private Single _ConLines;		// lines of console to display
        private Int32 _ClearConsole; // clearconsole
                                          // clearnotify

        private Single _OldScreenSize; // float oldscreensize
        private Single _OldFov; // float oldfov
        private Int32 _CenterLines; // scr_center_lines
        private Int32 _EraseLines; // scr_erase_lines

        //int _EraseCenter; // scr_erase_center
        private Single _CenterTimeStart; // scr_centertime_start	// for slow victory printing

        // scr_centertime_off
        private String _CenterString; // char	scr_centerstring[1024]

        private CVar _ViewSize; // = { "viewsize", "100", true };
        private CVar _Fov;// = { "fov", "90" };	// 10 - 170
        private CVar _ConSpeed;// = { "scr_conspeed", "300" };
        private CVar _CenterTime;// = { "scr_centertime", "2" };
        private CVar _ShowRam;// = { "showram", "1" };
        private CVar _ShowTurtle;// = { "showturtle", "0" };
        private CVar _ShowPause;// = { "showpause", "1" };
        private CVar _PrintSpeed;// = { "scr_printspeed", "8" };
        private CVar _glTripleBuffer;// = { "gl_triplebuffer", "1", true };

        private String _NotifyString; // scr_notifystring
        private System.Boolean _IsMouseWindowed; // windowed_mouse (don't confuse with _windowed_mouse cvar)
                                              // scr_fullupdate    set to 0 to force full redraw
        // CHANGE
        private Host Host
        {
            get;
            set;
        }

        public Scr( Host host )
        {
            Host = host;
        }

        // SCR_Init
        public void Initialise( )
        {
            if( _ViewSize == null )
            {
                _ViewSize = new CVar( "viewsize", "100", true );
                _Fov = new CVar( "fov", "90" );	// 10 - 170
                _ConSpeed = new CVar( "scr_conspeed", "3000" );
                _CenterTime = new CVar( "scr_centertime", "2" );
                _ShowRam = new CVar( "showram", "1" );
                _ShowTurtle = new CVar( "showturtle", "0" );
                _ShowPause = new CVar( "showpause", "1" );
                _PrintSpeed = new CVar( "scr_printspeed", "8" );
                _glTripleBuffer = new CVar( "gl_triplebuffer", "1", true );
            }

            //
            // register our commands
            //
            Host.Command.Add( "screenshot", ScreenShot_f );
            Host.Command.Add( "sizeup", SizeUp_f );
            Host.Command.Add( "sizedown", SizeDown_f );

            _Ram = Host.DrawingContext.PicFromWad( "ram" );
            _Net = Host.DrawingContext.PicFromWad( "net" );
            _Turtle = Host.DrawingContext.PicFromWad( "turtle" );

            if( CommandLine.HasParam( "-fullsbar" ) )
                FullSbarDraw = true;

            _IsInitialized = true;
        }

        // void SCR_UpdateScreen (void);
        // This is called every frame, and can also be called explicitly to flush
        // text to the screen.
        //
        // WARNING: be very careful calling this from elsewhere, because the refresh
        // needs almost the entire 256k of stack space!
        public void UpdateScreen()
        {
            if( BlockDrawing || !_IsInitialized || _InUpdate )
                return;

            _InUpdate = true;
            try
            {
                if( MainWindow.Instance != null && !MainWindow.Instance.IsDisposing)
                {
                    if( (MainWindow.Instance.VSync == VSyncMode.On ) != Host.Video.Wait )
                        MainWindow.Instance.VSync = (Host.Video.Wait ? VSyncMode.On : VSyncMode.Off );
                }

                _VidDef.numpages = 2 + ( Int32 ) _glTripleBuffer.Value;

                CopyTop = false;
                _CopyEverything = false;

                if( IsDisabledForLoading )
                {
                    if( ( Host.RealTime - _DisabledTime ) > 60 )
                    {
                        IsDisabledForLoading = false;
                        Host.Console.Print( "Load failed.\n" );
                    }
                    else
                        return;
                }

                if( !Host.Console.IsInitialized )
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

                Host.View.RenderView();

                Set2D();

                //
                // draw any areas not covered by the refresh
                //
                Host.Screen.TileClear();

                if( _DrawDialog )
                {
                    Host.StatusBar.Draw();
                    Host.DrawingContext.FadeScreen();
                    DrawNotifyString();
                    _CopyEverything = true;
                }
                else if( _DrawLoading )
                {
                    DrawLoading();
                    Host.StatusBar.Draw();
                }
                else if( Host.Client.cl.intermission == 1 && Host.Keyboard.Destination == KeyDestination.key_game )
                {
                    Host.StatusBar.IntermissionOverlay();
                }
                else if( Host.Client.cl.intermission == 2 && Host.Keyboard.Destination == KeyDestination.key_game )
                {
                    Host.StatusBar.FinaleOverlay();
                    CheckDrawCenterString();
                }
                else
                {
                    if( Host.View.Crosshair > 0 )
                        Host.DrawingContext.DrawCharacter( _VRect.x + _VRect.width / 2, _VRect.y + _VRect.height / 2, '+' );

                    DrawRam();
                    DrawNet();
                    DrawTurtle();
                    DrawPause();
                    CheckDrawCenterString();
                    Host.StatusBar.Draw();
                    DrawConsole();
                    Host.Menu.Draw();
                }

                if ( Host.ShowFPS )
                {
                    if ( DateTime.Now.Subtract( Host.LastFPSUpdate ).TotalSeconds >= 1 )
                    {
                        Host.FPS = Host.FPSCounter;
                        Host.FPSCounter = 0;
                        Host.LastFPSUpdate = DateTime.Now;
                    }

                    Host.FPSCounter++;

                    Host.DrawingContext.DrawString( 640 - 16 - 10, 10, $"{Host.FPS}", System.Drawing.Color.Yellow );
                }
                Host.View.UpdatePalette();
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
        public void EndRendering()
        {
            if ( MainWindow.Instance == null || MainWindow.Instance.IsDisposing )
                return;

            var form = MainWindow.Instance;
            if( form == null )
                return;

            if( !SkipUpdate || BlockDrawing )
                form.SwapBuffers();

            // handle the mouse state
            if( !Host.Video.WindowedMouse )
            {
                if(_IsMouseWindowed)
                {
                    MainWindow.Input.DeactivateMouse();
                    MainWindow.Input.ShowMouse();
                    _IsMouseWindowed = false;
                }
            }
            else
            {
                _IsMouseWindowed = true;
                if( Host.Keyboard.Destination == KeyDestination.key_game && !MainWindow.Input.IsMouseActive &&
                    Host.Client.cls.state != cactive_t.ca_disconnected )// && ActiveApp)
                {
                    MainWindow.Input.ActivateMouse();
                    MainWindow.Input.HideMouse();
                }
                else if( MainWindow.Input.IsMouseActive && Host.Keyboard.Destination != KeyDestination.key_game )
                {
                    MainWindow.Input.DeactivateMouse();
                    MainWindow.Input.ShowMouse();
                }
            }

            if( FullSbarDraw )
                Host.StatusBar.Changed();
        }

        // SCR_CenterPrint
        //
        // Called for important messages that should stay in the center of the screen
        // for a few moments
        public void CenterPrint( String str )
        {
            _CenterString = str;
            CenterTimeOff = _CenterTime.Value;
            _CenterTimeStart = ( Single ) Host.Client.cl.time;

            // count the number of lines for centering
            _CenterLines = 1;
            foreach( var c in _CenterString )
            {
                if( c == '\n' )
                    _CenterLines++;
            }
        }

        /// <summary>
        /// SCR_EndLoadingPlaque
        /// </summary>
        public void EndLoadingPlaque()
        {
            Host.Screen.IsDisabledForLoading = false;
            Host.Screen.FullUpdate = 0;
            Host.Console.ClearNotify();
        }

        /// <summary>
        /// SCR_BeginLoadingPlaque
        /// </summary>
        public void BeginLoadingPlaque()
        {
            Host.Sound.StopAllSounds( true );

            if( Host.Client.cls.state != cactive_t.ca_connected )
                return;
            if( Host.Client.cls.signon != ClientDef.SIGNONS )
                return;

            // redraw with no console and the loading plaque
            Host.Console.ClearNotify();
            CenterTimeOff = 0;
            _ConCurrent = 0;

            _DrawLoading = true;
            Host.Screen.FullUpdate = 0;
            Host.StatusBar.Changed();
            UpdateScreen();
            _DrawLoading = false;

            Host.Screen.IsDisabledForLoading = true;
            _DisabledTime = Host.RealTime;
            Host.Screen.FullUpdate = 0;
        }

        /// <summary>
        /// SCR_ModalMessage
        /// Displays a text string in the center of the screen and waits for a Y or N keypress.
        /// </summary>
        public System.Boolean ModalMessage( String text )
        {
            if( Host.Client.cls.state == cactive_t.ca_dedicated )
                return true;

            _NotifyString = text;

            // draw a fresh screen
            Host.Screen.FullUpdate = 0;
            _DrawDialog = true;
            UpdateScreen();
            _DrawDialog = false;

            Host.Sound.ClearBuffer();		// so dma doesn't loop current sound

            do
            {
                Host.Keyboard.KeyCount = -1;		// wait for a key down and up
                sys.SendKeyEvents();
            } while( Host.Keyboard.LastPress != 'y' && Host.Keyboard.LastPress != 'n' && Host.Keyboard.LastPress != KeysDef.K_ESCAPE );

            Host.Screen.FullUpdate = 0;
            UpdateScreen();

            return ( Host.Keyboard.LastPress == 'y' );
        }

        // SCR_SizeUp_f
        //
        // Keybinding command
        private void SizeUp_f()
        {
            CVar.Set( "viewsize", _ViewSize.Value + 10 );
            _VidDef.recalc_refdef = true;
        }

        // SCR_SizeDown_f
        //
        // Keybinding command
        private void SizeDown_f()
        {
            CVar.Set( "viewsize", _ViewSize.Value - 10 );
            _VidDef.recalc_refdef = true;
        }

        // SCR_ScreenShot_f
        private void ScreenShot_f()
        {
            //
            // find a file name to save it to
            //
            String path = null;
            Int32 i;
            for( i = 0; i <= 999; i++ )
            {
                path = Path.Combine( FileSystem.GameDir, String.Format( "quake{0:D3}.tga", i ) );
                if( FileSystem.GetFileTime( path ) == DateTime.MinValue )
                    break;	// file doesn't exist
            }
            if( i == 100 )
            {
                Host.Console.Print( "SCR_ScreenShot_f: Couldn't create a file\n" );
                return;
            }

            var fs = FileSystem.OpenWrite( path, true );
            if( fs == null )
            {
                Host.Console.Print( "SCR_ScreenShot_f: Couldn't create a file\n" );
                return;
            }
            using( var writer = new BinaryWriter( fs ) )
            {
                // Write tga header (18 bytes)
                writer.Write( ( UInt16 ) 0 );
                writer.Write( ( Byte ) 2 ); //buffer[2] = 2; uncompressed type
                writer.Write( ( Byte ) 0 );
                writer.Write( ( UInt32 ) 0 );
                writer.Write( ( UInt32 ) 0 );
                writer.Write( ( Byte ) ( glWidth & 0xff ) );
                writer.Write( ( Byte ) ( glWidth >> 8 ) );
                writer.Write( ( Byte ) ( glHeight & 0xff ) );
                writer.Write( ( Byte ) ( glHeight >> 8 ) );
                writer.Write( ( Byte ) 24 ); // pixel size
                writer.Write( ( UInt16 ) 0 );

                var buffer = new Byte[glWidth * glHeight * 3];
                GL.ReadPixels( glX, glY, glWidth, glHeight, PixelFormat.Rgb, PixelType.UnsignedByte, buffer );

                // swap 012 to 102
                var c = glWidth * glHeight * 3;
                for( i = 0; i < c; i += 3 )
                {
                    var temp = buffer[i + 0];
                    buffer[i + 0] = buffer[i + 1];
                    buffer[i + 1] = temp;
                }
                writer.Write( buffer, 0, buffer.Length );
            }
            Host.Console.Print( "Wrote {0}\n", Path.GetFileName( path ) );
        }

        /// <summary>
        /// GL_BeginRendering
        /// </summary>
        private void BeginRendering()
        {
            if ( MainWindow.Instance == null || MainWindow.Instance.IsDisposing )
                return;

            glX = 0;
            glY = 0;
            glWidth = 0;
            glHeight = 0;

            INativeWindow window = MainWindow.Instance;
            if( window != null )
            {
                var size = window.ClientSize;
                glWidth = size.Width;
                glHeight = size.Height;
            }
        }

        // SCR_CalcRefdef
        //
        // Must be called whenever vid changes
        // Internal use only
        private void CalcRefdef()
        {
            Host.Screen.FullUpdate = 0; // force a background redraw
            _VidDef.recalc_refdef = false;

            // force the status bar to redraw
            Host.StatusBar.Changed();

            // bound viewsize
            if( _ViewSize.Value < 30 )
                CVar.Set( "viewsize", "30" );
            if( _ViewSize.Value > 120 )
                CVar.Set( "viewsize", "120" );

            // bound field of view
            if( _Fov.Value < 10 )
                CVar.Set( "fov", "10" );
            if( _Fov.Value > 170 )
                CVar.Set( "fov", "170" );

            // intermission is always full screen
            Single size;
            if( Host.Client.cl.intermission > 0 )
                size = 120;
            else
                size = _ViewSize.Value;

            if( size >= 120 )
                Host.StatusBar.Lines = 0; // no status bar at all
            else if( size >= 110 )
                Host.StatusBar.Lines = 24; // no inventory
            else
                Host.StatusBar.Lines = 24 + 16 + 8;

            var full = false;
            if( _ViewSize.Value >= 100.0 )
            {
                full = true;
                size = 100.0f;
            }
            else
                size = _ViewSize.Value;

            if( Host.Client.cl.intermission > 0 )
            {
                full = true;
                size = 100;
                Host.StatusBar.Lines = 0;
            }
            size /= 100.0f;

            var h = _VidDef.height - Host.StatusBar.Lines;

            var rdef = Host.RenderContext.RefDef;
            rdef.vrect.width = ( Int32 ) ( _VidDef.width * size );
            if( rdef.vrect.width < 96 )
            {
                size = 96.0f / rdef.vrect.width;
                rdef.vrect.width = 96;  // min for icons
            }

            rdef.vrect.height = ( Int32 ) ( _VidDef.height * size );
            if( rdef.vrect.height > _VidDef.height - Host.StatusBar.Lines )
                rdef.vrect.height = _VidDef.height - Host.StatusBar.Lines;
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
        private Single CalcFov( Single fov_x, Single width, Single height )
        {
            if( fov_x < 1 || fov_x > 179 )
                Utilities.Error( "Bad fov: {0}", fov_x );

            var x = width / Math.Tan( fov_x / 360.0 * Math.PI );
            var a = Math.Atan( height / x );
            a = a * 360.0 / Math.PI;
            return ( Single ) a;
        }

        /// <summary>
        /// SCR_SetUpToDrawConsole
        /// </summary>
        private void SetUpToDrawConsole()
        {
            Host.Console.CheckResize();

            if( _DrawLoading )
                return;     // never a console with loading plaque

            // decide on the height of the console
            Host.Console.ForcedUp = ( Host.Client.cl.worldmodel == null ) || ( Host.Client.cls.signon != ClientDef.SIGNONS );

            if( Host.Console.ForcedUp )
            {
                _ConLines = _VidDef.height; // full screen
                _ConCurrent = _ConLines;
            }
            else if( Host.Keyboard.Destination == KeyDestination.key_console )
                _ConLines = _VidDef.height / 2; // half screen
            else
                _ConLines = 0; // none visible

            if( _ConLines < _ConCurrent )
            {
                _ConCurrent -= ( Int32 ) ( _ConSpeed.Value * Host.FrameTime );
                if( _ConLines > _ConCurrent )
                    _ConCurrent = _ConLines;
            }
            else if( _ConLines > _ConCurrent )
            {
                _ConCurrent += ( Int32 ) ( _ConSpeed.Value * Host.FrameTime );
                if( _ConLines < _ConCurrent )
                    _ConCurrent = _ConLines;
            }

            if( _ClearConsole++ < _VidDef.numpages )
            {
                Host.StatusBar.Changed();
            }
            else if( ClearNotify++ < _VidDef.numpages )
            {
                //????????????
            }
            else
                Host.Console.NotifyLines = 0;
        }

        // SCR_TileClear
        private void TileClear()
        {
            var rdef = Host.RenderContext.RefDef;
            if( rdef.vrect.x > 0 )
            {
                // left
                Host.DrawingContext.TileClear( 0, 0, rdef.vrect.x, _VidDef.height - Host.StatusBar.Lines );
                // right
                Host.DrawingContext.TileClear( rdef.vrect.x + rdef.vrect.width, 0,
                    _VidDef.width - rdef.vrect.x + rdef.vrect.width,
                    _VidDef.height - Host.StatusBar.Lines );
            }
            if( rdef.vrect.y > 0 )
            {
                // top
                Host.DrawingContext.TileClear( rdef.vrect.x, 0, rdef.vrect.x + rdef.vrect.width, rdef.vrect.y );
                // bottom
                Host.DrawingContext.TileClear( rdef.vrect.x, rdef.vrect.y + rdef.vrect.height,
                    rdef.vrect.width, _VidDef.height - Host.StatusBar.Lines - ( rdef.vrect.height + rdef.vrect.y ) );
            }
        }

        /// <summary>
        /// SCR_DrawNotifyString
        /// </summary>
        private void DrawNotifyString()
        {
            var offset = 0;
            var y = ( Int32 ) ( Host.Screen.vid.height * 0.35 );

            do
            {
                var end = _NotifyString.IndexOf( '\n', offset );
                if( end == -1 )
                    end = _NotifyString.Length;
                if( end - offset > 40 )
                    end = offset + 40;

                var length = end - offset;
                if( length > 0 )
                {
                    var x = ( vid.width - length * 8 ) / 2;
                    for( var j = 0; j < length; j++, x += 8 )
                        Host.DrawingContext.DrawCharacter( x, y, _NotifyString[offset + j] );

                    y += 8;
                }
                offset = end + 1;
            } while( offset < _NotifyString.Length );
        }

        /// <summary>
        /// SCR_DrawLoading
        /// </summary>
        private void DrawLoading()
        {
            if( !_DrawLoading )
                return;

            var pic = Host.DrawingContext.CachePic( "gfx/loading.lmp" );
            Host.DrawingContext.DrawPic( ( vid.width - pic.width ) / 2, ( vid.height - 48 - pic.height ) / 2, pic );
        }

        // SCR_CheckDrawCenterString
        private void CheckDrawCenterString()
        {
            CopyTop = true;
            if( _CenterLines > _EraseLines )
                _EraseLines = _CenterLines;

            CenterTimeOff -= ( Single ) Host.FrameTime;

            if( CenterTimeOff <= 0 && Host.Client.cl.intermission == 0 )
                return;
            if( Host.Keyboard.Destination != KeyDestination.key_game )
                return;

            DrawCenterString();
        }

        // SCR_DrawRam
        private void DrawRam()
        {
            if( _ShowRam.Value == 0 )
                return;

            if( !Host.RenderContext.CacheTrash )
                return;

            Host.DrawingContext.DrawPic( _VRect.x + 32, _VRect.y, _Ram );
        }

        // SCR_DrawTurtle
        private void DrawTurtle()
        {
            //int	count;

            if( _ShowTurtle.Value == 0 )
                return;

            if( Host.FrameTime < 0.1 )
            {
                _TurtleCount = 0;
                return;
            }

            _TurtleCount++;
            if( _TurtleCount < 3 )
                return;

            Host.DrawingContext.DrawPic( _VRect.x, _VRect.y, _Turtle );
        }

        // SCR_DrawNet
        private void DrawNet()
        {
            if( Host.RealTime - Host.Client.cl.last_received_message < 0.3 )
                return;
            if( Host.Client.cls.demoplayback )
                return;

            Host.DrawingContext.DrawPic( _VRect.x + 64, _VRect.y, _Net );
        }

        // DrawPause
        private void DrawPause()
        {
            if( _ShowPause.Value == 0 )	// turn off for screenshots
                return;

            if( !Host.Client.cl.paused )
                return;

            var pic = Host.DrawingContext.CachePic( "gfx/pause.lmp" );
            Host.DrawingContext.DrawPic( ( vid.width - pic.width ) / 2, ( vid.height - 48 - pic.height ) / 2, pic );
        }

        // SCR_DrawConsole
        private void DrawConsole()
        {
            if( _ConCurrent > 0 )
            {
                _CopyEverything = true;
                Host.Console.Draw( ( Int32 ) _ConCurrent, true );
                _ClearConsole = 0;
            }
            else if( Host.Keyboard.Destination == KeyDestination.key_game ||
                Host.Keyboard.Destination == KeyDestination.key_message )
            {
                Host.Console.DrawNotify();	// only draw notify in game
            }
        }

        // SCR_DrawCenterString
        private void DrawCenterString()
        {
            Int32 remaining;

            // the finale prints the characters one at a time
            if( Host.Client.cl.intermission > 0 )
                remaining = ( Int32 ) ( _PrintSpeed.Value * ( Host.Client.cl.time - _CenterTimeStart ) );
            else
                remaining = 9999;

            var y = 48;
            if( _CenterLines <= 4 )
                y = ( Int32 ) ( _VidDef.height * 0.35 );

            var lines = _CenterString.Split( '\n' );
            for( var i = 0; i < lines.Length; i++ )
            {
                var line = lines[i].TrimEnd( '\r' );
                var x = ( vid.width - line.Length * 8 ) / 2;

                for( var j = 0; j < line.Length; j++, x += 8 )
                {
                    Host.DrawingContext.DrawCharacter( x, y, line[j] );
                    if( remaining-- <= 0 )
                        return;
                }
                y += 8;
            }
        }
    }
}
