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

    internal class external
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
