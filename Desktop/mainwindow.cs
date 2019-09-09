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
using System.Diagnostics;
using System.Drawing;
using System.IO;
using SDL2;
using SharpQuake.Framework;
using SharpQuake.Renderer;
using SharpQuake.Renderer.Desktop;
using SharpQuake.Renderer.OpenGL.Desktop;

namespace SharpQuake
{
    public class MainWindow : GLWindow//GameWindow
    {
        public static MainWindow Instance
        {
            get
            {
                return ( MainWindow ) _Instance.Target;
            }
        }

        public static Boolean IsFullscreen
        {
            get
            {
                return Instance.IsFullScreen;
            }
        }

        public Boolean ConfirmExit = true;

        private static String DumpFilePath
        {
            get
            {
                return Path.Combine( AppDomain.CurrentDomain.BaseDirectory, "error.txt" );
            }
        }

        // This is where we start porting stuff over to proper instanced classes - TODO
        public Host Host
        {
            get;
            private set;
        }

        public static Input Input
        {
            get;
            private set;
        }

        public static Common Common
        {
            get;
            private set;
        }

        private static WeakReference _Instance;

        private Int32 _MouseBtnState;
        private Stopwatch _Swatch;

        public Boolean IsDisposing
        {
            get;
            private set;
        }

        protected override void OnFocusedChanged( )
        {
            base.OnFocusedChanged( );

            if ( Focused )
                Host.Sound.UnblockSound( );
            else
                Host.Sound.BlockSound( );
        }

        protected override void OnClosing(  )
        {
            // Turned this of as I hate this prompt so much 
            /*if (this.ConfirmExit)
            {
                int button_id;
                SDL.SDL_MessageBoxButtonData[] buttons = new SDL.SDL_MessageBoxButtonData[2];

                buttons[0].flags = SDL.SDL_MessageBoxButtonFlags.SDL_MESSAGEBOX_BUTTON_ESCAPEKEY_DEFAULT;
                buttons[0].buttonid = 0;
                buttons[0].text = "cancel";

                buttons[1].flags = SDL.SDL_MessageBoxButtonFlags.SDL_MESSAGEBOX_BUTTON_RETURNKEY_DEFAULT;
                buttons[1].buttonid = 1;
                buttons[1].text = "yes";

                SDL.SDL_MessageBoxData messageBoxData = new SDL.SDL_MessageBoxData();
                messageBoxData.flags = SDL.SDL_MessageBoxFlags.SDL_MESSAGEBOX_INFORMATION;
                messageBoxData.window = IntPtr.Zero;
                messageBoxData.title = "test";
                messageBoxData.message = "test";
                messageBoxData.numbuttons = 2;
                messageBoxData.buttons = buttons;
                SDL.SDL_ShowMessageBox(ref messageBoxData, out button_id);

                if (button_id == -1)
                {
                    "error displaying message box"
                }
                else
                {
                   "selection was %s"
                }

                // e.Cancel = (MessageBox.Show("Are you sure you want to quit?",
                //"Confirm Exit", MessageBoxButtons.YesNo) != DialogResult.Yes);
            }
            */
            base.OnClosing(  );
        }

        protected override void OnUpdateFrame( Double time )
        {
            try
            {
                if ( IsMinimised || Host.Screen.BlockDrawing || Host.IsDisposing )
                    Host.Screen.SkipUpdate = true;	// no point in bothering to draw

                _Swatch.Stop( );
                var ts = _Swatch.Elapsed.TotalSeconds;
                _Swatch.Reset( );
                _Swatch.Start( );
                Host.Frame( ts );
            }
            catch ( EndGameException )
            {
                // nothing to do
            }
        }

        private static MainWindow CreateInstance( Size size, Boolean fullScreen )
        {
            if ( _Instance != null )
            {
                throw new Exception( "Game instance is already created!" );
            }
            return new MainWindow( size, fullScreen );
        }

        private static void DumpError( Exception ex )
        {
            try
            {
                var fs = new FileStream( DumpFilePath, FileMode.Append, FileAccess.Write, FileShare.Read );
                using ( var writer = new StreamWriter( fs ) )
                {
                    writer.WriteLine( );

                    var ex1 = ex;
                    while ( ex1 != null )
                    {
                        writer.WriteLine( "[" + DateTime.Now.ToString( ) + "] Unhandled exception:" );
                        writer.WriteLine( ex1.Message );
                        writer.WriteLine( );
                        writer.WriteLine( "Stack trace:" );
                        writer.WriteLine( ex1.StackTrace );
                        writer.WriteLine( );

                        ex1 = ex1.InnerException;
                    }
                }
            }
            catch ( Exception )
            {
            }
        }

        private static void SafeShutdown( )
        {
            try
            {
                Instance.Dispose( );
            }
            catch ( Exception ex )
            {
                DumpError( ex );

                if ( Debugger.IsAttached )
                    throw new Exception( "Exception in SafeShutdown()!", ex );
            }
        }

        [STAThread]
        private static Int32 Main( String[] args )
        {
            if ( File.Exists( DumpFilePath ) )
                File.Delete( DumpFilePath );

            var parms = new QuakeParameters( );

            parms.basedir = AppDomain.CurrentDomain.BaseDirectory; //Application.StartupPath;

            var args2 = new String[args.Length + 1];
            args2[0] = String.Empty;
            args.CopyTo( args2, 1 );

            Common = new Common( );
            Common.InitArgv( args2 );

            Input = new Input( );

            parms.argv = new String[CommandLine.Argc];
            CommandLine.Args.CopyTo( parms.argv, 0 );

            if ( CommandLine.HasParam( "-dedicated" ) )
                throw new QuakeException( "Dedicated server mode not supported!" );

            var size = new Size( 1280, 720 );

            using ( var form = CreateInstance( size, false ) )
            {
                form.Host.Console.DPrint( "Host.Init\n" );
                form.Host.Initialise( parms );
                Instance.CursorVisible = false; //Hides mouse cursor during main menu on start up
                form.Run( );
            }
            // host.Shutdown();
#if !DEBUG
            }
            catch (QuakeSystemError se)
            {
                HandleException(se);
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
#endif
            return 0; // all Ok
        }

        private void Mouse_WheelChanged( Object sender, MouseWheelEventArgs e )
        {
            if ( e.Delta > 0 )
            {
                Instance.Host.Keyboard.Event( KeysDef.K_MWHEELUP, true );
                Instance.Host.Keyboard.Event( KeysDef.K_MWHEELUP, false );
            }
            else
            {
                Instance.Host.Keyboard.Event( KeysDef.K_MWHEELDOWN, true );
                Instance.Host.Keyboard.Event( KeysDef.K_MWHEELDOWN, false );
            }
        }

        private void Mouse_ButtonEvent( Object sender, MouseButtonEventArgs e )
        {
            _MouseBtnState = 0;

            if ( e.Button == MouseButton.Left && e.IsPressed )
                _MouseBtnState |= 1;

            if ( e.Button == MouseButton.Right && e.IsPressed )
                _MouseBtnState |= 2;

            if ( e.Button == MouseButton.Middle && e.IsPressed )
                _MouseBtnState |= 4;

            Input.MouseEvent( _MouseBtnState );
        }

        private void Mouse_Move( Object sender, EventArgs e )
        {
            Input.MouseEvent( _MouseBtnState );
        }

        private Int32 MapKey( Key srcKey )
        {
            var key = ( Int32 ) srcKey;
            key &= 255;

            if ( key >= KeysDef.KeyTable.Length )
                return 0;

            if ( KeysDef.KeyTable[key] == 0 )
                Host.Console.DPrint( "key 0x{0:X} has no translation\n", key );

            return KeysDef.KeyTable[key];
        }

        private void Keyboard_KeyUp( Object sender, KeyboardKeyEventArgs e )
        {
            Host.Keyboard.Event( MapKey( e.Key ), false );
        }

        private void Keyboard_KeyDown( Object sender, KeyboardKeyEventArgs e )
        {
            Host.Keyboard.Event( MapKey( e.Key ), true );
        }

        private MainWindow( Size size, Boolean isFullScreen )
        : base( "SharpQuakeEvolved", size, isFullScreen )
        {
            _Instance = new WeakReference( this );
            _Swatch = new Stopwatch( );
            VSync = VSyncMode.One;
            Icon = Icon.ExtractAssociatedIcon( AppDomain.CurrentDomain.FriendlyName ); //Application.ExecutablePath

            KeyDown += new EventHandler<KeyboardKeyEventArgs>( Keyboard_KeyDown );
            KeyUp += new EventHandler<KeyboardKeyEventArgs>( Keyboard_KeyUp );

            MouseMove += new EventHandler<EventArgs>( Mouse_Move );
            MouseDown += new EventHandler<MouseButtonEventArgs>( Mouse_ButtonEvent );
            MouseUp += new EventHandler<MouseButtonEventArgs>( Mouse_ButtonEvent );
            MouseWheel += new EventHandler<MouseWheelEventArgs>( Mouse_WheelChanged );

            Host = new Host( this );
        }

        public override void Dispose( )
        {
            IsDisposing = true;

            Host.Dispose( );

            base.Dispose( );
        }
    }
}
