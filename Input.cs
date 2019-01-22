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

using System.Drawing;
using System.Windows.Forms;
using OpenTK;
using OpenTK.Input;

// input.h -- external (non-keyboard) input devices

namespace SharpQuake
{
    /// <summary>
    /// In_functions
    /// </summary>
    internal static class Input
    {
        public static bool IsMouseActive
        {
            get
            {
                return _IsMouseActive;
            }
        }

        public static Point WindowCenter
        {
            get
            {
                Rectangle bounds = MainForm.Instance.Bounds;
                Point p = bounds.Location;
                p.Offset( bounds.Width / 2, bounds.Height / 2 );
                return p;
            }
        }

        private static Cvar _MouseFilter;// = { "m_filter", "0" };
        private static Vector2 _OldMouse; // old_mouse_x, old_mouse_y
        private static Vector2 _Mouse; // mouse_x, mouse_y
        private static Vector2 _MouseAccum; // mx_accum, my_accum
        private static bool _IsMouseActive; // mouseactive
        private static int _MouseButtons; // mouse_buttons
        private static int _MouseOldButtonState; // mouse_oldbuttonstate
        private static bool _MouseActivateToggle; // mouseactivatetoggle
        private static bool _MouseShowToggle = true; // mouseshowtoggle

        // IN_Init
        public static void Init()
        {
            if( _MouseFilter == null )
            {
                _MouseFilter = new Cvar( "m_filter", "0" );
            }

            _IsMouseActive = ( Mouse.GetState( 0 ).IsConnected != false );
            if( _IsMouseActive )
            {
                _MouseButtons = 3; //??? TODO: properly upgrade this to 3.0.1
            }
        }

        /// <summary>
        /// IN_Shutdown
        /// </summary>
        public static void Shutdown()
        {
            DeactivateMouse();
            ShowMouse();
        }

        // IN_Commands
        // oportunity for devices to stick commands on the script buffer
        public static void Commands()
        {
            // joystick not supported
        }

        /// <summary>
        /// IN_ActivateMouse
        /// </summary>
        public static void ActivateMouse()
        {
            _MouseActivateToggle = true;

            if( Mouse.GetState( 0 ).IsConnected != false )
            {
                //if (mouseparmsvalid)
                //    restore_spi = SystemParametersInfo (SPI_SETMOUSE, 0, newmouseparms, 0);

                Cursor.Position = Input.WindowCenter;

                //SetCapture(mainwindow);

                Cursor.Clip = MainForm.Instance.Bounds;

                _IsMouseActive = true;
            }
        }

        /// <summary>
        /// IN_DeactivateMouse
        /// </summary>
        public static void DeactivateMouse()
        {
            _MouseActivateToggle = false;

            Cursor.Clip = Screen.PrimaryScreen.Bounds;

            _IsMouseActive = false;
        }

        /// <summary>
        /// IN_HideMouse
        /// </summary>
        public static void HideMouse()
        {
            if( _MouseShowToggle )
            {
                Cursor.Hide();
                _MouseShowToggle = false;
            }
        }

        /// <summary>
        /// IN_ShowMouse
        /// </summary>
        public static void ShowMouse()
        {
            if( !_MouseShowToggle )
            {
                if( !MainForm.IsFullscreen )
                {
                    Cursor.Show();
                }
                _MouseShowToggle = true;
            }
        }

        // IN_Move
        // add additional movement on top of the keyboard move cmd
        public static void Move( usercmd_t cmd )
        {
            if( !MainForm.Instance.Focused )
                return;

            if( MainForm.Instance.WindowState == WindowState.Minimized )
                return;

            MouseMove( cmd );
        }

        // IN_ClearStates
        // restores all button and position states to defaults
        public static void ClearStates()
        {
            if( _IsMouseActive )
            {
                _MouseAccum = Vector2.Zero;
                _MouseOldButtonState = 0;
            }
        }

        /// <summary>
        /// IN_MouseEvent
        /// </summary>
        public static void MouseEvent( int mstate )
        {
            if( _IsMouseActive )
            {
                // perform button actions
                for( int i = 0; i < _MouseButtons; i++ )
                {
                    if( ( mstate & ( 1 << i ) ) != 0 && ( _MouseOldButtonState & ( 1 << i ) ) == 0 )
                    {
                        Key.Event( Key.K_MOUSE1 + i, true );
                    }

                    if( ( mstate & ( 1 << i ) ) == 0 && ( _MouseOldButtonState & ( 1 << i ) ) != 0 )
                    {
                        Key.Event( Key.K_MOUSE1 + i, false );
                    }
                }

                _MouseOldButtonState = mstate;
            }
        }

        /// <summary>
        /// IN_MouseMove
        /// </summary>
        private static void MouseMove( usercmd_t cmd )
        {
            if( !_IsMouseActive )
                return;

            Point current_pos = Cursor.Position;
            Point window_center = Input.WindowCenter;

            int mx = (int)( current_pos.X - window_center.X + _MouseAccum.X );
            int my = (int)( current_pos.Y - window_center.Y + _MouseAccum.Y );
            _MouseAccum.X = 0;
            _MouseAccum.Y = 0;

            if( _MouseFilter.Value != 0 )
            {
                _Mouse.X = ( mx + _OldMouse.X ) * 0.5f;
                _Mouse.Y = ( my + _OldMouse.Y ) * 0.5f;
            }
            else
            {
                _Mouse.X = mx;
                _Mouse.Y = my;
            }

            _OldMouse.X = mx;
            _OldMouse.Y = my;

            _Mouse *= Client.Sensitivity;

            // add mouse X/Y movement to cmd
            if( ClientInput.StrafeBtn.IsDown || ( Client.LookStrafe && ClientInput.MLookBtn.IsDown ) )
                cmd.sidemove += Client.MSide * _Mouse.X;
            else
                Client.cl.viewangles.Y -= Client.MYaw * _Mouse.X;

            View.StopPitchDrift();

            Client.cl.viewangles.X += Client.MPitch * _Mouse.Y;

            // modernized to always use mouse look
            Client.cl.viewangles.X = MathHelper.Clamp( Client.cl.viewangles.X, -70, 80 );

            // if the mouse has moved, force it to the center, so there's room to move
            if( mx != 0 || my != 0 )
            {
                Cursor.Position = window_center;
            }
        }
    }
}
