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

#if _WINDOWS

using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace SharpQuake
{
    internal static partial class Mci
    {
        [StructLayout( LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi )]
        public struct OpenParams
        {
            public IntPtr dwCallback;
            public IntPtr wDeviceID;

            [MarshalAs(UnmanagedType.LPStr)]
            public string lpstrDeviceType;

            [MarshalAs(UnmanagedType.LPStr)]
            public string lpstrElementName;

            [MarshalAs(UnmanagedType.LPStr)]
            public string lpstrAlias;
        }

        [StructLayout( LayoutKind.Sequential, Pack = 1 )]
        public struct SetParams
        {
            public IntPtr dwCallback;
            public uint dwTimeFormat;
            public uint dwAudio;
        }

        [StructLayout( LayoutKind.Sequential, Pack = 1 )]
        public struct GenericParams
        {
            public IntPtr dwCallback;
        }

        [StructLayout( LayoutKind.Sequential, Pack = 1 )]
        public struct PlayParams
        {
            public IntPtr dwCallback;
            public uint dwFrom;
            public uint dwTo;
        }

        [StructLayout( LayoutKind.Sequential, Pack = 1 )]
        public struct StatusParams
        {
            public IntPtr dwCallback;
            public uint dwReturn;
            public uint dwItem;
            public uint dwTrack;
        }

        public const int MCI_OPEN = 0x0803;
        public const int MCI_CLOSE = 0x0804;
        public const int MCI_PLAY = 0x0806;
        public const int MCI_STOP = 0x0808;
        public const int MCI_PAUSE = 0x0809;
        public const int MCI_SET = 0x080D;
        public const int MCI_STATUS = 0x0814;
        public const int MCI_RESUME = 0x0855;

        // Flags for MCI Play command
        public const int MCI_OPEN_SHAREABLE = 0x00000100;

        public const int MCI_OPEN_ELEMENT = 0x00000200;
        public const int MCI_OPEN_TYPE_ID = 0x00001000;
        public const int MCI_OPEN_TYPE = 0x00002000;

        // Constants used to specify MCI time formats
        public const int MCI_FORMAT_TMSF = 10;

        // Flags for MCI Set command
        public const int MCI_SET_DOOR_OPEN = 0x00000100;

        public const int MCI_SET_DOOR_CLOSED = 0x00000200;
        public const int MCI_SET_TIME_FORMAT = 0x00000400;

        // Flags for MCI commands
        public const int MCI_NOTIFY = 0x00000001;

        public const int MCI_WAIT = 0x00000002;
        public const int MCI_FROM = 0x00000004;
        public const int MCI_TO = 0x00000008;
        public const int MCI_TRACK = 0x00000010;

        // Flags for MCI Status command
        public const int MCI_STATUS_ITEM = 0x00000100;

        public const int MCI_STATUS_LENGTH = 0x00000001;
        public const int MCI_STATUS_POSITION = 0x00000002;
        public const int MCI_STATUS_NUMBER_OF_TRACKS = 0x00000003;
        public const int MCI_STATUS_MODE = 0x00000004;
        public const int MCI_STATUS_MEDIA_PRESENT = 0x00000005;
        public const int MCI_STATUS_TIME_FORMAT = 0x00000006;
        public const int MCI_STATUS_READY = 0x00000007;
        public const int MCI_STATUS_CURRENT_TRACK = 0x00000008;

        public const int MCI_CD_OFFSET = 1088;

        public const int MCI_CDA_STATUS_TYPE_TRACK = 0x00004001;
        public const int MCI_CDA_TRACK_AUDIO = MCI_CD_OFFSET + 0;
        public const int MCI_CDA_TRACK_OTHER = MCI_CD_OFFSET + 1;

        public const int MCI_NOTIFY_SUCCESSFUL = 1;
        public const int MCI_NOTIFY_SUPERSEDED = 2;
        public const int MCI_NOTIFY_ABORTED = 4;
        public const int MCI_NOTIFY_FAILURE = 8;

        public const int MM_MCINOTIFY = 0x3B9;

        // MCI_OPEN_PARMS

        // MCI_SET_PARMS

        //MCI_GENERIC_PARMS

        //MCI_PLAY_PARMS

        //MCI_STATUS_PARMS

        public static uint MCI_MAKE_TMSF( int t, int m, int s, int f )
        {
            return (uint)( ( (byte)t | ( (uint)m << 8 ) ) | ( ( (uint)(byte)s | ( (uint)f << 8 ) ) << 16 ) );
        }
    }

    internal class CDAudioWinController : ICDAudioController
    {
        public IntPtr DeviceID
        {
            get
            {
                return _DeviceID;
            }
        }

        private IntPtr _DeviceID;
        private bool _IsInitialized;
        private bool _IsEnabled;
        private bool _IsValidDisc;
        private bool _IsPlaying;
        private bool _IsLooping;
        private bool _WasPlaying;
        private byte[] _Remap;
        private NotifyForm _Form;
        private byte _PlayTrack;
        private byte _MaxTrack;
        private float _Volume; // cdvolume

        public void MessageHandler( ref Message m )
        {
            if( m.LParam != _DeviceID )
                return;

            switch( m.WParam.ToInt32() )
            {
                case Mci.MCI_NOTIFY_SUCCESSFUL:
                    if( _IsPlaying )
                    {
                        _IsPlaying = false;
                        if( _IsLooping )
                            Play( _PlayTrack, true );
                    }
                    break;

                case Mci.MCI_NOTIFY_ABORTED:
                case Mci.MCI_NOTIFY_SUPERSEDED:
                    break;

                case Mci.MCI_NOTIFY_FAILURE:
                    Con.DPrint( "MCI_NOTIFY_FAILURE\n" );
                    Stop();
                    _IsValidDisc = false;
                    break;

                default:
                    Con.DPrint( "Unexpected MM_MCINOTIFY type ({0})\n", m.WParam );
                    m.Result = new IntPtr( 1 );
                    break;
            }
        }

        public CDAudioWinController()
        {
            _Remap = new byte[100];
            _Form = new NotifyForm( this );
        }

        #region ICDAudioController Members

        public bool IsInitialized
        {
            get
            {
                return _IsInitialized;
            }
        }

        public bool IsEnabled
        {
            get
            {
                return _IsEnabled;
            }
            set
            {
                _IsEnabled = value;
            }
        }

        public bool IsPlaying
        {
            get
            {
                return _IsPlaying;
            }
        }

        public bool IsPaused
        {
            get
            {
                return _WasPlaying;
            }
        }

        public bool IsValidCD
        {
            get
            {
                return _IsValidDisc;
            }
        }

        public bool IsLooping
        {
            get
            {
                return _IsLooping;
            }
        }

        public byte[] Remap
        {
            get
            {
                return _Remap;
            }
        }

        public byte MaxTrack
        {
            get
            {
                return _MaxTrack;
            }
        }

        public byte CurrentTrack
        {
            get
            {
                return _PlayTrack;
            }
        }

        public float Volume
        {
            get
            {
                return _Volume;
            }
            set
            {
                _Volume = value;
            }
        }

        public void Init()
        {
            Mci.OpenParams parms = default( Mci.OpenParams );
            parms.lpstrDeviceType = "cdaudio";
            int ret = Mci.Open( IntPtr.Zero, Mci.MCI_OPEN, Mci.MCI_OPEN_TYPE | Mci.MCI_OPEN_SHAREABLE, ref parms );
            if( ret != 0 )
            {
                Con.Print( "CDAudio_Init: MCI_OPEN failed ({0})\n", ret );
                return;
            }
            _DeviceID = parms.wDeviceID;

            // Set the time format to track/minute/second/frame (TMSF).
            Mci.SetParams sp = default( Mci.SetParams );
            sp.dwTimeFormat = Mci.MCI_FORMAT_TMSF;
            ret = Mci.Set( _DeviceID, Mci.MCI_SET, Mci.MCI_SET_TIME_FORMAT, ref sp );
            if( ret != 0 )
            {
                Con.Print( "MCI_SET_TIME_FORMAT failed ({0})\n", ret );
                Mci.SendCommand( _DeviceID, Mci.MCI_CLOSE, 0, IntPtr.Zero );
                return;
            }

            for( byte n = 0; n < 100; n++ )
                _Remap[n] = n;

            _IsInitialized = true;
            _IsEnabled = true;

            ReloadDiskInfo();
            if( !_IsValidDisc )
                Con.Print( "CDAudio_Init: No CD in player.\n" );
        }

        public void Play( byte track, bool looping )
        {
            if( !_IsEnabled )
                return;

            if( !_IsValidDisc )
            {
                ReloadDiskInfo();
                if( !_IsValidDisc )
                    return;
            }

            track = _Remap[track];

            if( track < 1 || track > _MaxTrack )
            {
                Con.DPrint( "CDAudio: Bad track number {0}.\n", track );
                return;
            }

            // don't try to play a non-audio track
            Mci.StatusParams sp = default( Mci.StatusParams );
            sp.dwItem = Mci.MCI_CDA_STATUS_TYPE_TRACK;
            sp.dwTrack = track;
            int ret = Mci.Status( _DeviceID, Mci.MCI_STATUS, Mci.MCI_STATUS_ITEM | Mci.MCI_TRACK | Mci.MCI_WAIT, ref sp );
            if( ret != 0 )
            {
                Con.DPrint( "MCI_STATUS failed ({0})\n", ret );
                return;
            }
            if( sp.dwReturn != Mci.MCI_CDA_TRACK_AUDIO )
            {
                Con.Print( "CDAudio: track {0} is not audio\n", track );
                return;
            }

            // get the length of the track to be played
            sp.dwItem = Mci.MCI_STATUS_LENGTH;
            sp.dwTrack = track;
            ret = Mci.Status( _DeviceID, Mci.MCI_STATUS, Mci.MCI_STATUS_ITEM | Mci.MCI_TRACK | Mci.MCI_WAIT, ref sp );
            if( ret != 0 )
            {
                Con.DPrint( "MCI_STATUS failed ({0})\n", ret );
                return;
            }

            if( _IsPlaying )
            {
                if( _PlayTrack == track )
                    return;
                Stop();
            }

            Mci.PlayParams pp;
            pp.dwFrom = Mci.MCI_MAKE_TMSF( track, 0, 0, 0 );
            pp.dwTo = ( sp.dwReturn << 8 ) | track;
            pp.dwCallback = _Form.Handle;
            ret = Mci.Play( _DeviceID, Mci.MCI_PLAY, Mci.MCI_NOTIFY | Mci.MCI_FROM | Mci.MCI_TO, ref pp );
            if( ret != 0 )
            {
                Con.DPrint( "CDAudio: MCI_PLAY failed ({0})\n", ret );
                return;
            }

            _IsLooping = looping;
            _PlayTrack = track;
            _IsPlaying = true;

            if( _Volume == 0 )
                Pause();
        }

        public void Stop()
        {
            if( !_IsEnabled )
                return;

            if( !_IsPlaying )
                return;

            int ret = Mci.SendCommand( _DeviceID, Mci.MCI_STOP, 0, IntPtr.Zero );
            if( ret != 0 )
                Con.DPrint( "MCI_STOP failed ({0})", ret );

            _WasPlaying = false;
            _IsPlaying = false;
        }

        public void Pause()
        {
            if( !_IsEnabled )
                return;

            if( !_IsPlaying )
                return;

            Mci.GenericParams gp = default( Mci.GenericParams );
            int ret = Mci.SendCommand( _DeviceID, Mci.MCI_PAUSE, 0, ref gp );
            if( ret != 0 )
                Con.DPrint( "MCI_PAUSE failed ({0})", ret );

            _WasPlaying = _IsPlaying;
            _IsPlaying = false;
        }

        public void Resume()
        {
            if( !_IsEnabled )
                return;

            if( !_IsValidDisc )
                return;

            if( !_WasPlaying )
                return;

            Mci.PlayParams pp;
            pp.dwFrom = Mci.MCI_MAKE_TMSF( _PlayTrack, 0, 0, 0 );
            pp.dwTo = Mci.MCI_MAKE_TMSF( _PlayTrack + 1, 0, 0, 0 );
            pp.dwCallback = _Form.Handle;// (DWORD)mainwindow;
            int ret = Mci.Play( _DeviceID, Mci.MCI_PLAY, Mci.MCI_TO | Mci.MCI_NOTIFY, ref pp );
            if( ret != 0 )
                Con.DPrint( "CDAudio: MCI_PLAY failed ({0})\n", ret );

            _IsPlaying = ( ret == 0 );
        }

        public void Shutdown()
        {
            if( _Form != null )
            {
                _Form.Dispose();
                _Form = null;
            }

            if( !_IsInitialized )
                return;

            Stop();

            if( Mci.SendCommand( _DeviceID, Mci.MCI_CLOSE, Mci.MCI_WAIT, IntPtr.Zero ) != 0 )
                Con.DPrint( "CDAudio_Shutdown: MCI_CLOSE failed\n" );
        }

        public void Update()
        {
            if( !_IsEnabled )
                return;

            if( Sound.BgmVolume != _Volume )
            {
                if( _Volume != 0 )
                {
                    Cvar.Set( "bgmvolume", 0f );
                    _Volume = Sound.BgmVolume;
                    Pause();
                }
                else
                {
                    Cvar.Set( "bgmvolume", 1f );
                    _Volume = Sound.BgmVolume;
                    Resume();
                }
            }
        }

        /// <summary>
        /// CDAudio_GetAudioDiskInfo
        /// </summary>
        public void ReloadDiskInfo()
        {
            _IsValidDisc = false;

            Mci.StatusParams sp = default( Mci.StatusParams );
            sp.dwItem = Mci.MCI_STATUS_READY;
            int ret = Mci.Status( _DeviceID, Mci.MCI_STATUS, Mci.MCI_STATUS_ITEM | Mci.MCI_WAIT, ref sp );
            if( ret != 0 )
            {
                Con.DPrint( "CDAudio: drive ready test - get status failed\n" );
                return;
            }
            if( sp.dwReturn == 0 )
            {
                Con.DPrint( "CDAudio: drive not ready\n" );
                return;
            }

            sp.dwItem = Mci.MCI_STATUS_NUMBER_OF_TRACKS;
            ret = Mci.Status( _DeviceID, Mci.MCI_STATUS, Mci.MCI_STATUS_ITEM | Mci.MCI_WAIT, ref sp );
            if( ret != 0 )
            {
                Con.DPrint( "CDAudio: get tracks - status failed\n" );
                return;
            }
            if( sp.dwReturn < 1 )
            {
                Con.DPrint( "CDAudio: no music tracks\n" );
                return;
            }

            _IsValidDisc = true;
            _MaxTrack = (byte)sp.dwReturn;
        }

        public void CloseDoor()
        {
            int ret = Mci.SendCommand( _DeviceID, Mci.MCI_SET, Mci.MCI_SET_DOOR_CLOSED, IntPtr.Zero );
            if( ret != 0 )
                Con.DPrint( "MCI_SET_DOOR_CLOSED failed ({0})\n", ret );
        }

        public void Edject()
        {
            int ret = Mci.SendCommand( _DeviceID, Mci.MCI_SET, Mci.MCI_SET_DOOR_OPEN, IntPtr.Zero );
            if( ret != 0 )
                Con.DPrint( "MCI_SET_DOOR_OPEN failed ({0})\n", ret );
        }

        #endregion ICDAudioController Members
    }

    internal class NotifyForm : Form
    {
        private CDAudioWinController _Controller;

        protected override void WndProc( ref Message m )
        {
            if( m.Msg == Mci.MM_MCINOTIFY )
            {
                _Controller.MessageHandler( ref m );
                return;
            }

            base.WndProc( ref m );
        }

        public NotifyForm( CDAudioWinController ctrl )
        {
            _Controller = ctrl;
            Visible = false;
        }
    }
}

#endif
