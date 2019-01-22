/// <remarks>
/// Copyright (C) 2010 Yury Kiselev.
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
/// </remarks>

using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Input;

namespace SharpQuake
{
    public class MainForm : GameWindow
    {
        public static MainForm Instance
        {
            get
            {
                return (MainForm)_Instance.Target;
            }
        }

        public static DisplayDevice DisplayDevice
        {
            get
            {
                return _DisplayDevice;
            }
            set
            {
                _DisplayDevice = value;
            }
        }

        public static bool IsFullscreen
        {
            get
            {
                return ( Instance.WindowState == WindowState.Fullscreen );
            }
        }

        public bool ConfirmExit = true;

        private static string DumpFilePath
        {
            get
            {
                return Path.Combine( Application.LocalUserAppDataPath, "error.txt" );
            }
        }

        private static byte[] _KeyTable = new byte[130]
        {
            0, Key.K_SHIFT, Key.K_SHIFT, Key.K_CTRL, Key.K_CTRL, Key.K_ALT, Key.K_ALT, 0, // 0 - 7
            0, 0, Key.K_F1, Key.K_F2, Key.K_F3, Key.K_F4, Key.K_F5, Key.K_F6, // 8 - 15
            Key.K_F7, Key.K_F8, Key.K_F9, Key.K_F10, Key.K_F11, Key.K_F12, 0, 0, // 16 - 23
            0, 0, 0, 0, 0, 0, 0, 0, // 24 - 31
            0, 0, 0, 0, 0, 0, 0, 0, // 32 - 39
            0, 0, 0, 0, 0, Key.K_UPARROW, Key.K_DOWNARROW, Key.K_LEFTARROW, // 40 - 47
            Key.K_RIGHTARROW, Key.K_ENTER, Key.K_ESCAPE, Key.K_SPACE, Key.K_TAB, Key.K_BACKSPACE, Key.K_INS, Key.K_DEL, // 48 - 55
            Key.K_PGUP, Key.K_PGDN, Key.K_HOME, Key.K_END, 0, 0, 0, Key.K_PAUSE, // 56 - 63
            0, 0, 0, Key.K_INS, Key.K_END, Key.K_DOWNARROW, Key.K_PGDN, Key.K_LEFTARROW, // 64 - 71
            0, Key.K_RIGHTARROW, Key.K_HOME, Key.K_UPARROW, Key.K_PGUP, (byte)'/', (byte)'*', (byte)'-', // 72 - 79
            (byte)'+', (byte)'.', Key.K_ENTER, (byte)'a', (byte)'b', (byte)'c', (byte)'d', (byte)'e', // 80 - 87
            (byte)'f', (byte)'g', (byte)'h', (byte)'i', (byte)'j', (byte)'k', (byte)'l', (byte)'m', // 88 - 95
            (byte)'n', (byte)'o', (byte)'p', (byte)'q', (byte)'r', (byte)'s', (byte)'t', (byte)'u', // 96 - 103
            (byte)'v', (byte)'w', (byte)'x', (byte)'y', (byte)'z', (byte)'0', (byte)'1', (byte)'2', // 104 - 111
            (byte)'3', (byte)'4', (byte)'5', (byte)'6', (byte)'7', (byte)'8', (byte)'9', (byte)'`', // 112 - 119
            (byte)'-', (byte)'+', (byte)'[', (byte)']', (byte)';', (byte)'\'', (byte)',', (byte)'.', // 120 - 127
            (byte)'/', (byte)'\\' // 128 - 129
        };

        private static WeakReference _Instance;
        private static DisplayDevice _DisplayDevice;

        private int _MouseBtnState;
        private Stopwatch _Swatch;

        protected override void OnFocusedChanged( EventArgs e )
        {
            base.OnFocusedChanged( e );

            if( this.Focused )
                Sound.UnblockSound();
            else
                Sound.BlockSound();
        }

        protected override void OnClosing( System.ComponentModel.CancelEventArgs e )
        {
            if( this.ConfirmExit )
            {
                e.Cancel = ( MessageBox.Show( "Are you sure you want to quit?",
                    "Confirm Exit", MessageBoxButtons.YesNo ) != DialogResult.Yes );
            }
            base.OnClosing( e );
        }

        protected override void OnUpdateFrame( FrameEventArgs e )
        {
            try
            {
                if( this.WindowState == OpenTK.WindowState.Minimized || Scr.BlockDrawing )
                    Scr.SkipUpdate = true;	// no point in bothering to draw

                _Swatch.Stop();
                double ts = _Swatch.Elapsed.TotalSeconds;
                _Swatch.Reset();
                _Swatch.Start();
                Host.Frame( ts );
            }
            catch( EndGameException )
            {
                // nothing to do
            }
        }

        private static MainForm CreateInstance( Size size, GraphicsMode mode, bool fullScreen )
        {
            if( _Instance != null )
            {
                throw new Exception( "MainForm instance is already created!" );
            }
            return new MainForm( size, mode, fullScreen );
        }

        private static void DumpError( Exception ex )
        {
            try
            {
                FileStream fs = new FileStream( DumpFilePath, FileMode.Append, FileAccess.Write, FileShare.Read );
                using( StreamWriter writer = new StreamWriter( fs ) )
                {
                    writer.WriteLine();

                    Exception ex1 = ex;
                    while( ex1 != null )
                    {
                        writer.WriteLine( "[" + DateTime.Now.ToString() + "] Unhandled exception:" );
                        writer.WriteLine( ex1.Message );
                        writer.WriteLine();
                        writer.WriteLine( "Stack trace:" );
                        writer.WriteLine( ex1.StackTrace );
                        writer.WriteLine();

                        ex1 = ex1.InnerException;
                    }
                }
            }
            catch( Exception )
            {
            }
        }

        private static void SafeShutdown()
        {
            try
            {
                Host.Shutdown();
            }
            catch( Exception ex )
            {
                DumpError( ex );

                if( Debugger.IsAttached )
                    throw new Exception( "Exception in SafeShutdown()!", ex );
            }
        }

        private static void HandleException( Exception ex )
        {
            DumpError( ex );

            if( Debugger.IsAttached )
                throw new Exception( "Fatal error!", ex );

            Instance.CursorVisible = true;
            MessageBox.Show( ex.Message );
            SafeShutdown();
        }

        [STAThread]
        private static int Main( string[] args )
        {
#if !DEBUG
            try
            {
#endif
                // select display device
                _DisplayDevice = DisplayDevice.Default;

                if( File.Exists( DumpFilePath ) )
                    File.Delete( DumpFilePath );

                quakeparms_t parms = new quakeparms_t();

                parms.basedir = Application.StartupPath;

                string[] args2 = new string[args.Length + 1];
                args2[0] = String.Empty;
                args.CopyTo( args2, 1 );

                Common.InitArgv( args2 );

                parms.argv = new string[Common.Argc];
                Common.Args.CopyTo( parms.argv, 0 );

                if( Common.HasParam( "-dedicated" ) )
                    throw new QuakeException( "Dedicated server mode not supported!" );

                Size size = new Size( 640, 480 );
                GraphicsMode mode = new GraphicsMode();
                bool fullScreen = false;
                using( MainForm form = MainForm.CreateInstance( size, mode, fullScreen ) )
                {
                    Con.DPrint( "Host.Init\n" );
                    Host.Init( parms );

                    form.Run();
                }
                Host.Shutdown();
#if !DEBUG
            }
            catch( QuakeSystemError se )
            {
                HandleException( se );
            }
            catch( Exception ex )
            {
                HandleException( ex );
            }
#endif
            return 0; // all Ok
        }

        private void Mouse_WheelChanged( object sender, OpenTK.Input.MouseWheelEventArgs e )
        {
            if( e.Delta > 0 )
            {
                Key.Event( Key.K_MWHEELUP, true );
                Key.Event( Key.K_MWHEELUP, false );
            }
            else
            {
                Key.Event( Key.K_MWHEELDOWN, true );
                Key.Event( Key.K_MWHEELDOWN, false );
            }
        }

        private void Mouse_ButtonEvent( object sender, OpenTK.Input.MouseButtonEventArgs e )
        {
            _MouseBtnState = 0;

            if( e.Button == MouseButton.Left && e.IsPressed )
                _MouseBtnState |= 1;

            if( e.Button == MouseButton.Right && e.IsPressed )
                _MouseBtnState |= 2;

            if( e.Button == MouseButton.Middle && e.IsPressed )
                _MouseBtnState |= 4;

            Input.MouseEvent( _MouseBtnState );
        }

        private void Mouse_Move( object sender, OpenTK.Input.MouseMoveEventArgs e )
        {
            Input.MouseEvent( _MouseBtnState );
        }

        private int MapKey( OpenTK.Input.Key srcKey )
        {
            int key = (int)srcKey;
            key &= 255;

            if( key >= _KeyTable.Length )
                return 0;

            if( _KeyTable[key] == 0 )
                Con.DPrint( "key 0x{0:X} has no translation\n", key );

            return _KeyTable[key];
        }

        private void Keyboard_KeyUp( object sender, OpenTK.Input.KeyboardKeyEventArgs e )
        {
            Key.Event( MapKey( e.Key ), false );
        }

        private void Keyboard_KeyDown( object sender, OpenTK.Input.KeyboardKeyEventArgs e )
        {
            Key.Event( MapKey( e.Key ), true );
        }

        private MainForm( Size size, GraphicsMode mode, bool fullScreen )
                                                                                                                            : base( size.Width, size.Height, mode, "SharpQuake", fullScreen ? GameWindowFlags.Fullscreen : GameWindowFlags.Default )
        {
            _Instance = new WeakReference( this );
            _Swatch = new Stopwatch();
            this.VSync = VSyncMode.On;
            this.Icon = Icon.ExtractAssociatedIcon( Application.ExecutablePath );
            if( this.Keyboard != null )
            {
                this.Keyboard.KeyRepeat = true;
                this.Keyboard.KeyDown += new EventHandler<OpenTK.Input.KeyboardKeyEventArgs>( Keyboard_KeyDown );
                this.Keyboard.KeyUp += new EventHandler<OpenTK.Input.KeyboardKeyEventArgs>( Keyboard_KeyUp );
            }
            if( this.Mouse != null )
            {
                this.Mouse.Move += new EventHandler<OpenTK.Input.MouseMoveEventArgs>( Mouse_Move );
                this.Mouse.ButtonDown += new EventHandler<OpenTK.Input.MouseButtonEventArgs>( Mouse_ButtonEvent );
                this.Mouse.ButtonUp += new EventHandler<OpenTK.Input.MouseButtonEventArgs>( Mouse_ButtonEvent );
                this.Mouse.WheelChanged += new EventHandler<OpenTK.Input.MouseWheelEventArgs>( Mouse_WheelChanged );
            }
        }
    }
}
