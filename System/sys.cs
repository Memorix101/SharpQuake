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
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace SharpQuake
{
    internal static class sys
    {
        /// <summary>
        /// Sys_SendKeyEvents
        /// </summary>
        public static void SendKeyEvents()
        {
            Scr.SkipUpdate = false;
            MainWindow.Instance.ProcessEvents();
        }

        /// <summary>
        /// Sys_ConsoleInput
        /// </summary>
        public static String ConsoleInput()
        {
            return null; // this is needed only for dedicated servers
        }

        /// <summary>
        /// Sys_Quit
        /// </summary>
        public static void Quit()
        {
            if( MainWindow.Instance != null )
            {
                MainWindow.Instance.ConfirmExit = false;
                MainWindow.Instance.Exit();
                MainWindow.Instance.Dispose();
            }
        }
    }
}