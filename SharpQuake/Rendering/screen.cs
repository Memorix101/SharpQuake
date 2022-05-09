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
using SharpQuake.Factories.Rendering.UI;
using SharpQuake.Framework;
using SharpQuake.Framework.IO;
using SharpQuake.Framework.IO.Input;
using SharpQuake.Game.Client;
using SharpQuake.Renderer;
using SharpQuake.Rendering.UI;
using SharpQuake.Rendering.UI.Elements;

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
        public VRect VRect
        {
            get
            {
                return _VRect;
            }
        }

        public ClientVariable ViewSize
        {
            get
            {
                return Host.Cvars.ViewSize;
            }
        }


        public Boolean CopyEverithing
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

        public Boolean IsDisabledForLoading;
        public Boolean BlockDrawing
        {
            get
            {
                return Host.Video.Device.BlockDrawing;
            }
            set
            {
                Host.Video.Device.BlockDrawing = value;
            }
        }

        public Boolean SkipUpdate
        {
            get
            {
                return Host.Video.Device.SkipUpdate;
            }
            set
            {
                Host.Video.Device.SkipUpdate = value;
            }
        }

        // scr_skipupdate
        public Boolean FullSbarDraw;

        // fullsbardraw = false
        public Boolean IsPermedia;

        // only the refresh window will be updated unless these variables are flagged
        public Boolean CopyTop;

        public Int32 ClearNotify;
        public Int32 glX;
        public Int32 glY;
        public Int32 glWidth;
        public Int32 glHeight;
        public Int32 FullUpdate;
        private VidDef _VidDef = new VidDef( );	// viddef_t vid (global video state)
        private VRect _VRect; // scr_vrect

        private Double _DisabledTime; // float scr_disabled_time

        // isPermedia
        private Boolean _IsInitialized;

        private Boolean _InUpdate;
        private Boolean _CopyEverything;

        private Single _OldScreenSize; // float oldscreensize
        private Single _OldFov; // float oldfov

        private Boolean _IsMouseWindowed; // windowed_mouse (don't confuse with _windowed_mouse cvar)
                                          // scr_fullupdate    set to 0 to force full redraw
                                          // CHANGE
        private Host Host
        {
            get;
            set;
        }

        public ElementFactory Elements
        {
            get;
            private set;
        }

        public HudResources HudResources
        {
            get;
            private set;
        }

        public Scr( Host host )
        {
            Host = host;
            HudResources = new HudResources( host );
            Elements = new ElementFactory( host );
        }

        // SCR_Init
        public void Initialise( )
        {
            if ( Host.Cvars.ViewSize == null )
            {
                Host.Cvars.ViewSize = Host.CVars.Add( "viewsize", 100f, ClientVariableFlags.Archive );
                Host.Cvars.Fov = Host.CVars.Add( "fov", 90f, ClientVariableFlags.Archive );	// 10 - 170
                Host.Cvars.ConSpeed = Host.CVars.Add( "scr_conspeed", 3000 );
                Host.Cvars.CenterTime = Host.CVars.Add( "scr_centertime", 2 );
                Host.Cvars.ShowRam = Host.CVars.Add( "showram", true );
                Host.Cvars.ShowTurtle = Host.CVars.Add( "showturtle", false );
                Host.Cvars.ShowPause = Host.CVars.Add( "showpause", true );
                Host.Cvars.PrintSpeed = Host.CVars.Add( "scr_printspeed", 8 );
                Host.Cvars.glTripleBuffer = Host.CVars.Add( "gl_triplebuffer", 1, ClientVariableFlags.Archive );
            }

            //
            // register our commands
            //
            Host.Commands.Add( "screenshot", ScreenShot_f );
            Host.Commands.Add( "sizeup", SizeUp_f );
            Host.Commands.Add( "sizedown", SizeDown_f );

            HudResources.Initialise( );
            Elements.Initialise( );

            if ( CommandLine.HasParam( "-fullsbar" ) )
                FullSbarDraw = true;

            _IsInitialized = true;
        }

        public void InitialiseHUD( )
        {
            Elements.Initialise( ElementFactory.HUD );
            Elements.Initialise( ElementFactory.INTERMISSION );
            Elements.Initialise( ElementFactory.FINALE );
            Elements.Initialise( ElementFactory.SP_SCOREBOARD );
            Elements.Initialise( ElementFactory.MP_SCOREBOARD );
            Elements.Initialise( ElementFactory.MP_MINI_SCOREBOARD );
            Elements.Initialise( ElementFactory.FRAGS );
        }

        // void SCR_UpdateScreen (void);
        // This is called every frame, and can also be called explicitly to flush
        // text to the screen.
        //
        // WARNING: be very careful calling this from elsewhere, because the refresh
        // needs almost the entire 256k of stack space!
        public void UpdateScreen( )
        {
            if ( BlockDrawing || !_IsInitialized || _InUpdate )
                return;

            _InUpdate = true;
            try
            {
                if ( MainWindow.Instance != null && !MainWindow.Instance.IsDisposing )
                {
                    if ( ( MainWindow.Instance.VSync == VSyncMode.One ) != Host.Video.Wait )
                        MainWindow.Instance.VSync = ( Host.Video.Wait ? VSyncMode.One : VSyncMode.None );
                }

                _VidDef.numpages = 2 + ( Int32 ) Host.Cvars.glTripleBuffer.Get<Int32>( );

                CopyTop = false;
                _CopyEverything = false;

                if ( IsDisabledForLoading )
                {
                    if ( ( Host.RealTime - _DisabledTime ) > 60 )
                    {
                        IsDisabledForLoading = false;
                        Host.Console.Print( "Load failed.\n" );
                    }
                    else
                        return;
                }

                if ( !Host.Console.IsInitialized )
                    return;	// not initialized yet

                BeginRendering( );

                //
                // determine size of refresh window
                //
                if ( _OldFov != Host.Cvars.Fov.Get<Single>( ) )
                {
                    _OldFov = Host.Cvars.Fov.Get<Single>( );
                    _VidDef.recalc_refdef = true;
                }

                if ( _OldScreenSize != Host.Cvars.ViewSize.Get<Single>( ) )
                {
                    _OldScreenSize = Host.Cvars.ViewSize.Get<Single>( );
                    _VidDef.recalc_refdef = true;
                }

                if ( _VidDef.recalc_refdef )
                    CalcRefdef( );

                //
                // do 3D refresh drawing, and then update the screen
                //
                Elements.Get<VisualConsole>( ElementFactory.CONSOLE )?.Configure( );

                Host.View.RenderView( );

                Host.Video.Device.Begin2DScene( );
                //Set2D();

                //
                // draw any areas not covered by the refresh
                //
                Host.Screen.TileClear( );

                DrawElements( );

                Host.Video.Device.End2DScene( );

                Host.View.UpdatePalette( );
                EndRendering( );
            }
            finally
            {
                _InUpdate = false;
            }
        }

        /// <summary>
        /// Logic for drawing elements
        /// </summary>
        private void DrawElements( )
        {
            if ( Elements.IsVisible( ElementFactory.MODAL ) )
            {
                Elements.Draw( ElementFactory.HUD );
                Host.DrawingContext.FadeScreen( );
                Elements.Draw( ElementFactory.MODAL );
                _CopyEverything = true;
            }
            else if ( Elements.IsVisible( ElementFactory.LOADING ) )
            {
                Elements.Draw( ElementFactory.LOADING );
                Elements.Draw( ElementFactory.HUD );
            }
            else if ( Host.Client.cl.intermission == 1 && Host.Keyboard.Destination == KeyDestination.key_game )
            {
                if ( Host.Client.cl.gametype == ProtocolDef.GAME_DEATHMATCH )
                    Elements.Draw( ElementFactory.MP_SCOREBOARD );
                else
                    Elements.Draw( ElementFactory.INTERMISSION );
            }
            else if ( Host.Client.cl.intermission == 2 && Host.Keyboard.Destination == KeyDestination.key_game )
            {
                Elements.Draw( ElementFactory.FINALE );
                Elements.Draw( ElementFactory.CENTRE_PRINT );
            }
            else
            {
                if ( Host.View.Crosshair > 0 )
                    Host.DrawingContext.DrawCharacter( _VRect.x + _VRect.width / 2, _VRect.y + _VRect.height / 2, '+' );

                Elements.Draw( ElementFactory.RAM );
                Elements.Draw( ElementFactory.NET );
                Elements.Draw( ElementFactory.TURTLE );
                Elements.Draw( ElementFactory.PAUSE );
                Elements.Draw( ElementFactory.CENTRE_PRINT );
                Elements.Draw( ElementFactory.HUD );
                Elements.Draw( ElementFactory.CONSOLE );
                Host.Menus.Draw( );
            }

            if ( Host.ShowFPS )
                Elements.Draw( ElementFactory.FPS );
        }

        /// <summary>
        /// GL_EndRendering
        /// </summary>
        public void EndRendering( )
        {
            if ( MainWindow.Instance == null || MainWindow.Instance.IsDisposing )
                return;

            var form = MainWindow.Instance;
            if ( form == null )
                return;

            Host.Video?.Device?.EndScene( );

            //if( !SkipUpdate || BlockDrawing )
            //    form.SwapBuffers();

            // handle the mouse state
            if ( !Host.Video.WindowedMouse )
            {
                if ( _IsMouseWindowed )
                {
                    MainWindow.Input.DeactivateMouse( );
                    MainWindow.Input.ShowMouse( );
                    _IsMouseWindowed = false;
                }
            }
            else
            {
                _IsMouseWindowed = true;
                if ( Host.Keyboard.Destination == KeyDestination.key_game && !MainWindow.Input.IsMouseActive &&
                    Host.Client.cls.state != cactive_t.ca_disconnected )// && ActiveApp)
                {
                    MainWindow.Input.ActivateMouse( );
                    MainWindow.Input.HideMouse( );
                }
                else if ( MainWindow.Input.IsMouseActive && Host.Keyboard.Destination != KeyDestination.key_game )
                {
                    MainWindow.Input.DeactivateMouse( );
                    MainWindow.Input.ShowMouse( );
                }
            }

            if ( FullSbarDraw )
                Elements.SetDirty( ElementFactory.HUD );
        }

        /// <summary>
        /// SCR_EndLoadingPlaque
        /// </summary>
        public void EndLoadingPlaque( )
        {
            Host.Screen.IsDisabledForLoading = false;
            Host.Screen.FullUpdate = 0;
            Host.Console.ClearNotify( );
        }

        /// <summary>
        /// SCR_BeginLoadingPlaque
        /// </summary>
        public void BeginLoadingPlaque( )
        {
            Host.Sound.StopAllSounds( true );

            if ( Host.Client.cls.state != cactive_t.ca_connected ||
                Host.Client.cls.signon != ClientDef.SIGNONS )
                return;

            // redraw with no console and the loading plaque
            Host.Console.ClearNotify( );
            Host.Screen.Elements.Reset( ElementFactory.CENTRE_PRINT );
            Host.Screen.Elements.Reset( ElementFactory.CONSOLE );

            Elements.Show( ElementFactory.LOADING );
            Host.Screen.FullUpdate = 0;
            Elements.SetDirty( ElementFactory.HUD );
            UpdateScreen( );
            Elements.Hide( ElementFactory.LOADING );

            Host.Screen.IsDisabledForLoading = true;
            _DisabledTime = Host.RealTime;
            Host.Screen.FullUpdate = 0;
        }

        /// <summary>
        /// SCR_ModalMessage
        /// Displays a text string in the center of the screen and waits for a Y or N keypress.
        /// </summary>
        public Boolean ModalMessage( String text )
        {
            if ( Host.Client.cls.state == cactive_t.ca_dedicated )
                return true;

            Elements.Enqueue( ElementFactory.MODAL, text );

            // draw a fresh screen
            Host.Screen.FullUpdate = 0;

            Elements.Show( ElementFactory.MODAL );
            UpdateScreen( );
            Elements.Hide( ElementFactory.MODAL );

            Host.Sound.ClearBuffer( );		// so dma doesn't loop current sound

            do
            {
                Host.Keyboard.KeyCount = -1;        // wait for a key down and up
				Host.MainWindow.SendKeyEvents( );
            } while ( Host.Keyboard.LastPress != 'y' && Host.Keyboard.LastPress != 'n' && Host.Keyboard.LastPress != KeysDef.K_ESCAPE );

            Host.Screen.FullUpdate = 0;
            UpdateScreen( );

            return ( Host.Keyboard.LastPress == 'y' );
        }

        // SCR_SizeUp_f
        //
        // Keybinding command
        private void SizeUp_f( CommandMessage msg )
        {
            Host.CVars.Set( "viewsize", Host.Cvars.ViewSize.Get<Single>( ) + 10 );
            _VidDef.recalc_refdef = true;
        }

        // SCR_SizeDown_f
        //
        // Keybinding command
        private void SizeDown_f( CommandMessage msg )
        {
            Host.CVars.Set( "viewsize", Host.Cvars.ViewSize.Get<Single>( ) - 10 );
            _VidDef.recalc_refdef = true;
        }

        /// <summary>
        /// SCR_ScreenShot_f
        /// </summary>
        /// <param name="msg"></param>
        private void ScreenShot_f( CommandMessage msg )
        {
            Host.Video.Device.ScreenShot( out var path );
            Host.Console.Print( $"Screenshot saved '{path}'.\n" );
        }

        /// <summary>
        /// GL_BeginRendering
        /// </summary>
        private void BeginRendering( )
        {
            if ( MainWindow.Instance == null || MainWindow.Instance.IsDisposing )
                return;

            glX = 0;
            glY = 0;
            glWidth = 0;
            glHeight = 0;

            var window = MainWindow.Instance;
            if ( window != null )
            {
                var size = window.ClientSize;
                glWidth = size.Width;
                glHeight = size.Height;
            }

            Host.Video?.Device?.BeginScene( );
        }

        // SCR_CalcRefdef
        //
        // Must be called whenever vid changes
        // Internal use only
        private void CalcRefdef( )
        {
            Host.Screen.FullUpdate = 0; // force a background redraw
            _VidDef.recalc_refdef = false;

            // force the status bar to redraw
            Elements.SetDirty( ElementFactory.HUD );

            // bound viewsize
            if ( Host.Cvars.ViewSize.Get<Single>( ) < 30 )
                Host.CVars.Set( "viewsize", 30f );
            if ( Host.Cvars.ViewSize.Get<Single>( ) > 120 )
                Host.CVars.Set( "viewsize", 120f );

            // bound field of view
            if ( Host.Cvars.Fov.Get<Single>( ) < 10 )
                Host.CVars.Set( "fov", 10f );
            if ( Host.Cvars.Fov.Get<Single>( ) > 170 )
                Host.CVars.Set( "fov", 170f );

            // intermission is always full screen
            Single size;
            if ( Host.Client.cl.intermission > 0 )
                size = 120;
            else
                size = Host.Cvars.ViewSize.Get<Single>( );

            if ( size >= 120 )
                HudResources.Lines = 0; // no status bar at all
            else if ( size >= 110 )
                HudResources.Lines = 24; // no inventory
            else
                HudResources.Lines = 24 + 16 + 8;

            var full = false;
            if ( Host.Cvars.ViewSize.Get<Single>( ) >= 100.0 )
            {
                full = true;
                size = 100.0f;
            }
            else
                size = Host.Cvars.ViewSize.Get<Single>( );

            if ( Host.Client.cl.intermission > 0 )
            {
                full = true;
                size = 100;
                HudResources.Lines = 0;
            }
            size /= 100.0f;

            var h = _VidDef.height - HudResources.Lines;

            var rdef = Host.RenderContext.RefDef;
            rdef.vrect.width = ( Int32 ) ( _VidDef.width * size );
            if ( rdef.vrect.width < 96 )
            {
                size = 96.0f / rdef.vrect.width;
                rdef.vrect.width = 96;  // min for icons
            }

            rdef.vrect.height = ( Int32 ) ( _VidDef.height * size );
            if ( rdef.vrect.height > _VidDef.height - HudResources.Lines )
                rdef.vrect.height = _VidDef.height - HudResources.Lines;
            if ( rdef.vrect.height > _VidDef.height )
                rdef.vrect.height = _VidDef.height;
            rdef.vrect.x = ( _VidDef.width - rdef.vrect.width ) / 2;
            if ( full )
                rdef.vrect.y = 0;
            else
                rdef.vrect.y = ( h - rdef.vrect.height ) / 2;

            rdef.fov_x = Host.Cvars.Fov.Get<Single>( );
            rdef.fov_y = Utilities.CalculateFOV( rdef.fov_x, rdef.vrect.width, rdef.vrect.height );

            _VRect = rdef.vrect;
        }


        // SCR_TileClear
        private void TileClear( )
        {
            var rdef = Host.RenderContext.RefDef;
            if ( rdef.vrect.x > 0 )
            {
                // left
                Host.DrawingContext.TileClear( 0, 0, rdef.vrect.x, _VidDef.height - HudResources.Lines );
                // right
                Host.DrawingContext.TileClear( rdef.vrect.x + rdef.vrect.width, 0,
                    _VidDef.width - rdef.vrect.x + rdef.vrect.width,
                    _VidDef.height - HudResources.Lines );
            }
            if ( rdef.vrect.y > 0 )
            {
                // top
                Host.DrawingContext.TileClear( rdef.vrect.x, 0, rdef.vrect.x + rdef.vrect.width, rdef.vrect.y );
                // bottom
                Host.DrawingContext.TileClear( rdef.vrect.x, rdef.vrect.y + rdef.vrect.height,
                    rdef.vrect.width, _VidDef.height - HudResources.Lines - ( rdef.vrect.height + rdef.vrect.y ) );
            }
        }
    }
}
