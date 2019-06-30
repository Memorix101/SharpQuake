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

// cdaudio.h

using System;
using System.IO;
using OpenTK.Audio;
using NVorbis.OpenTKSupport;
using SharpQuake.Framework;

namespace SharpQuake
{
    /// <summary>
    /// CDAudio_functions
    /// </summary>

    internal static class cd_audio
    {
#if _WINDOWS
        private static ICDAudioController _Controller = new CDAudioWinController();
#else
        static NullCDAudioController _Controller = new NullCDAudioController();
#endif

        /// <summary>
        /// CDAudio_Init
        /// </summary>
        public static Boolean Init()
        {
            if (client.cls.state == cactive_t.ca_dedicated)
                return false;

            if (CommandLine.HasParam("-nocdaudio"))
                return false;

            _Controller.Init();

            if (_Controller.IsInitialized)
            {
                Command.Add("cd", CD_f);
                Con.Print("CD Audio (Fallback) Initialized\n");
            }

            return _Controller.IsInitialized;
        }

        // CDAudio_Play(byte track, qboolean looping)
        public static void Play( Byte track, Boolean looping )
        {
            _Controller.Play(track, looping);
#if DEBUG
            Console.WriteLine("DEBUG: track byte:{0} - loop byte: {1}", track, looping);
#endif
        }

        // CDAudio_Stop
        public static void Stop()
        {
            _Controller.Stop();
        }

        // CDAudio_Pause
        public static void Pause()
        {
            _Controller.Pause();
        }

        // CDAudio_Resume
        public static void Resume()
        {
            _Controller.Resume();
        }

        // CDAudio_Shutdown
        public static void Shutdown()
        {
            _Controller.Shutdown();
        }

        // CDAudio_Update
        public static void Update()
        {
            _Controller.Update();
        }

        private static void CD_f()
        {
            if (Command.Argc < 2)
                return;

            var command = Command.Argv(1);

            if (Utilities.SameText(command, "on"))
            {
                _Controller.IsEnabled = true;
                return;
            }

            if (Utilities.SameText(command, "off"))
            {
                if (_Controller.IsPlaying)
                    _Controller.Stop();
                _Controller.IsEnabled = false;
                return;
            }

            if (Utilities.SameText(command, "reset"))
            {
                _Controller.IsEnabled = true;
                if (_Controller.IsPlaying)
                    _Controller.Stop();

                _Controller.ReloadDiskInfo();
                return;
            }

            if (Utilities.SameText(command, "remap"))
            {
                var ret = Command.Argc - 2;
                Byte[] remap = _Controller.Remap;
                if (ret <= 0)
                {
                    for ( var n = 1; n < 100; n++)
                        if (remap[n] != n)
                            Con.Print("  {0} -> {1}\n", n, remap[n]);
                    return;
                }
                for ( var n = 1; n <= ret; n++)
                    remap[n] = ( Byte ) MathLib.atoi(Command.Argv(n + 1));
                return;
            }

            if (Utilities.SameText(command, "close"))
            {
                _Controller.CloseDoor();
                return;
            }

            if (!_Controller.IsValidCD)
            {
                _Controller.ReloadDiskInfo();
                if (!_Controller.IsValidCD)
                {
                    Con.Print("No CD in player.\n");
                    return;
                }
            }

            if (Utilities.SameText(command, "play"))
            {
                _Controller.Play(( Byte ) MathLib.atoi(Command.Argv(2)), false);
                return;
            }

            if (Utilities.SameText(command, "loop"))
            {
                _Controller.Play(( Byte ) MathLib.atoi(Command.Argv(2)), true);
                return;
            }

            if (Utilities.SameText(command, "stop"))
            {
                _Controller.Stop();
                return;
            }

            if (Utilities.SameText(command, "pause"))
            {
                _Controller.Pause();
                return;
            }

            if (Utilities.SameText(command, "resume"))
            {
                _Controller.Resume();
                return;
            }

            if (Utilities.SameText(command, "eject"))
            {
                if (_Controller.IsPlaying)
                    _Controller.Stop();
                _Controller.Eject();
                return;
            }

            if (Utilities.SameText(command, "info"))
            {
                Con.Print("%u tracks\n", _Controller.MaxTrack);
                if (_Controller.IsPlaying)
                    Con.Print("Currently {0} track {1}\n", _Controller.IsLooping ? "looping" : "playing", _Controller.CurrentTrack);
                else if (_Controller.IsPaused)
                    Con.Print("Paused {0} track {1}\n", _Controller.IsLooping ? "looping" : "playing", _Controller.CurrentTrack);
                Con.Print("Volume is {0}\n", _Controller.Volume);
                return;
            }
        }
    }

    internal class NullCDAudioController
    {
        private Byte[] _Remap;
        private OggStream oggStream;
        private OggStreamer streamer;
        //private WaveOutEvent waveOut; // or WaveOutEvent()
        private Boolean _isLooping;
        String trackid;
        String trackpath;
        private Boolean _noAudio = false;
        private Boolean _noPlayback = false;
        private Single _Volume;
        private Boolean _isPlaying;
        private Boolean _isPaused;


        //OGG file
        Int32 channels;
        Int32 sampleRate;
        TimeSpan totalTime;

        public NullCDAudioController()
        {
            _Remap = new Byte[100];
        }

        #region ICDAudioController Members

        public Boolean IsInitialized
        {
            get
            {
                return true;
            }
        }

        public Boolean IsEnabled
        {
            get
            {
                return true;
            }
            set
            {

            }
        }

        public Boolean IsPlaying
        {
            get
            {
                return _isPlaying;
            }
        }

        public Boolean IsPaused
        {
            get
            {
                return _isPaused;
            }
        }

        public Boolean IsValidCD
        {
            get
            {
                return false;
            }
        }

        public Boolean IsLooping
        {
            get
            {
                return _isLooping;
            }
        }

        public Byte[] Remap
        {
            get
            {
                return _Remap;
            }
        }

        public Byte MaxTrack
        {
            get
            {
                return 0;
            }
        }

        public Byte CurrentTrack
        {
            get
            {
                return 0;
            }
        }

        public Single Volume
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
            streamer = new OggStreamer(441000);
            _Volume = snd.BgmVolume;

            if (Directory.Exists( String.Format("{0}/{1}/music/", qparam.globalbasedir, qparam.globalgameid)) == false)
            {
                _noAudio = true;
            }
        }

        public void Play( Byte track, Boolean looping )
        {
            if (_noAudio == false)
            {
                trackid = track.ToString("00");
                trackpath = String.Format("{0}/{1}/music/track{2}.ogg", qparam.globalbasedir, qparam.globalgameid, trackid);
#if DEBUG
                Console.WriteLine("DEBUG: track path:{0} ", trackpath);
#endif
                try
                {
                    _isLooping = looping;
                    if(oggStream != null)
                        oggStream.Stop();
                    oggStream = new OggStream(trackpath, 3);
                    oggStream.IsLooped = looping;
                    oggStream.Play();
                    oggStream.Volume = _Volume;
                    _noPlayback = false;
                }
                catch (Exception e)
                {
                    Console.WriteLine("Could not find or play {0}", trackpath);
                    _noPlayback = true;
                    //throw;
                }
            }
        }

        public void Stop()
        {
            if (streamer == null)
                return;

            if (_noAudio == true)
                return;

            oggStream.Stop();
        }

        public void Pause()
        {
            if (streamer == null)
                return;

            if (_noAudio == true)
                return;

            oggStream.Pause();
        }

        public void Resume()
        {
            if (streamer == null)
                return;

            oggStream.Resume();
        }

        public void Shutdown()
        {
            if (streamer == null)
                return;

            if (_noAudio == true)
                return;

            //oggStream.Dispose();
            streamer.Dispose();
        }

        public void Update()
        {
            if (streamer == null)
                return;

            if (_noAudio == true)
                return;

            if (_noPlayback == true)
                return;

            /*if (waveOut.PlaybackState == PlaybackState.Paused)
            {
                _isPaused = true;
            }
            else if (waveOut.PlaybackState == PlaybackState.Playing)
            {
                _isPaused = false;
            }

            if (waveOut.PlaybackState == PlaybackState.Paused || waveOut.PlaybackState == PlaybackState.Stopped)
            {
                _isPlaying = false;
            }
            else if (waveOut.PlaybackState == PlaybackState.Playing)
            {
                _isPlaying = true;
            }*/

            _Volume = snd.BgmVolume;
            oggStream.Volume = _Volume;
        }

        public void ReloadDiskInfo()
        {
        }

        public void CloseDoor()
        {
        }

        public void Eject()
        {
        }

        #endregion ICDAudioController Members
    }
}
