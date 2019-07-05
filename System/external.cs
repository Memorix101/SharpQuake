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
using System.Runtime.InteropServices;

namespace SharpQuake
{
#if _WINDOWS

    internal static partial class Mci
    {
        [DllImport( "winmm.dll", EntryPoint = "mciSendCommandA", ExactSpelling = true )]
        public static extern int Open( IntPtr device, int cmd, int flags, ref OpenParams p );

        [DllImport( "winmm.dll", EntryPoint = "mciSendCommandA", ExactSpelling = true )]
        public static extern int Set( IntPtr device, int cmd, int flags, ref SetParams p );

        [DllImport( "winmm.dll", EntryPoint = "mciSendCommandA", ExactSpelling = true )]
        public static extern int Play( IntPtr device, int cmd, int flags, ref PlayParams p );

        [DllImport( "winmm.dll", EntryPoint = "mciSendCommandA", ExactSpelling = true )]
        public static extern int Status( IntPtr device, int cmd, int flags, ref StatusParams p );

        [DllImport( "winmm.dll", EntryPoint = "mciSendCommandA", ExactSpelling = true )]
        public static extern int SendCommand( IntPtr device, int cmd, int flags, IntPtr p );

        [DllImport( "winmm.dll", EntryPoint = "mciSendCommandA", ExactSpelling = true )]
        public static extern int SendCommand( IntPtr device, int cmd, int flags, ref GenericParams p );
    }

#endif

    internal class External
    {
#if _WINDOWS

        internal class WindowsShell
        {
            /// <summary>
            /// <para>
            /// Notifies the system of an event that an application has performed.
            /// </para>
            /// An application should use this function if it performs an action that may affect the Shell.
            /// <para>
            /// </para>
            /// </summary>
            /// <param name="eventId"></param>
            /// <param name="flags"></param>
            /// <param name="item1"></param>
            /// <param name="item2"></param>
            /// <returns></returns>
            [DllImport( "Shell32.dll" )]
            public static extern int SHChangeNotify( int eventId, int flags, IntPtr item1, IntPtr item2 );
        }

#endif
    }
}
