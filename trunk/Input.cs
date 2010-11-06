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
using System.Drawing;
using System.Windows.Forms;
using OpenTK;

// input.h -- external (non-keyboard) input devices

namespace SharpQuake
{
    /// <summary>
    /// In_functions
    /// </summary>
    static class Input
    {
        static Cvar _MouseFilter;// = { "m_filter", "0" };
        static Vector2 _OldMouse; // old_mouse_x, old_mouse_y
        static Vector2 _Mouse; // mouse_x, mouse_y
        static Vector2 _MouseAccum; // mx_accum, my_accum
        static bool _IsMouseActive; // mouseactive
        static int _MouseButtons; // mouse_buttons
        static int _MouseOldButtonState; // mouse_oldbuttonstate
        static bool _MouseActivateToggle; // mouseactivatetoggle
        static bool _MouseShowToggle = true; // mouseshowtoggle

        public static bool IsMouseActive
        {
            get { return _IsMouseActive; }
        }
        public static Point WindowCenter
        {
            get
            {
                Rectangle bounds = MainForm.Instance.Bounds;
                Point p = bounds.Location;
                p.Offset(bounds.Width / 2, bounds.Height / 2);
                return p;
            }
        }

        // IN_Init
        public static void Init()
        {
            if (_MouseFilter == null)
            {
                _MouseFilter = new Cvar("m_filter", "0");
            }

            _IsMouseActive = (MainForm.Instance.Mouse != null);
            if (_IsMouseActive)
            {
                _MouseButtons = MainForm.Instance.Mouse.NumberOfButtons;
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

            if (MainForm.Instance.Mouse != null)
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
            //ReleaseCapture ();

            _IsMouseActive = false;
        }

        /// <summary>
        /// IN_HideMouse
        /// </summary>
        public static void HideMouse()
        {
	        if (_MouseShowToggle)
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
            if (!_MouseShowToggle)
            {
                if (!MainForm.IsFullscreen)
                {
                    Cursor.Show();
                }
                _MouseShowToggle = true;
            }
        }

        // IN_Move
        // add additional movement on top of the keyboard move cmd
        public static void Move(usercmd_t cmd)
        {
            if (!MainForm.Instance.Focused)
                return;

            if (MainForm.Instance.WindowState == WindowState.Minimized)
                return;

            MouseMove(cmd);
        }
        
        // IN_ClearStates
        // restores all button and position states to defaults
        public static void ClearStates()
        {
            if (_IsMouseActive)
            {
                _MouseAccum = Vector2.Zero;
                _MouseOldButtonState = 0;
            }

        }

        /// <summary>
        /// IN_MouseEvent
        /// </summary>
        public static void MouseEvent(int mstate)
        {
            if (_IsMouseActive)
            {
                // perform button actions
                for (int i = 0; i < _MouseButtons; i++)
                {
                    if ((mstate & (1 << i)) != 0 && (_MouseOldButtonState & (1 << i)) == 0)
                    {
                        Key.Event(Key.K_MOUSE1 + i, true);
                    }

                    if ((mstate & (1 << i)) == 0 && (_MouseOldButtonState & (1 << i)) != 0)
                    {
                        Key.Event(Key.K_MOUSE1 + i, false);
                    }
                }

                _MouseOldButtonState = mstate;
            }
        }

        /// <summary>
        /// IN_MouseMove
        /// </summary>
        static void MouseMove(usercmd_t cmd)
        {
            if (!_IsMouseActive)
                return;

            Rectangle bounds = MainForm.Instance.Bounds;
            Point current_pos = Cursor.Position;
            Point window_center = Input.WindowCenter;

            int mx = (int)(current_pos.X - window_center.X + _MouseAccum.X);
            int my = (int)(current_pos.Y - window_center.Y + _MouseAccum.Y);
            _MouseAccum.X = 0;
            _MouseAccum.Y = 0;


            if (_MouseFilter.Value != 0)
            {
                _Mouse.X = (mx + _OldMouse.X) * 0.5f;
                _Mouse.Y = (my + _OldMouse.Y) * 0.5f;
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
            if (ClientInput.StrafeBtn.IsDown || (Client.LookStrafe && ClientInput.MLookBtn.IsDown))
                cmd.sidemove += Client.MSide * _Mouse.X;
            else
                Client.cl.viewangles.Y -= Client.MYaw * _Mouse.X;

            if (ClientInput.MLookBtn.IsDown)
                View.StopPitchDrift();

            if (ClientInput.MLookBtn.IsDown && !ClientInput.StrafeBtn.IsDown)
            {
                Client.cl.viewangles.X += Client.MPitch * _Mouse.Y;
                if (Client.cl.viewangles.X > 80)
                    Client.cl.viewangles.X = 80;
                if (Client.cl.viewangles.X < -70)
                    Client.cl.viewangles.X = -70;
            }
            else
            {
                if (ClientInput.StrafeBtn.IsDown && Host.NoClipAngleHack)
                    cmd.upmove -= Client.MForward * _Mouse.Y;
                else
                    cmd.forwardmove -= Client.MForward * _Mouse.Y;
            }

            // if the mouse has moved, force it to the center, so there's room to move
            if (mx != 0 || my != 0)
            {
                Cursor.Position = window_center;
            }
        }
    }
}
