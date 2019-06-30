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
using System.Runtime.InteropServices;
using OpenTK;
using SharpQuake.Framework;

// sound.h -- client sound i/o functions

namespace SharpQuake
{
    // snd_started == Sound._Controller.IsInitialized
    // snd_initialized == Sound._IsInitialized

    // !!! if this is changed, it much be changed in asm_i386.h too !!!
    [StructLayout( LayoutKind.Sequential, Pack = 1 )]
    internal struct portable_samplepair_t
    {
        public Int32 left;
        public Int32 right;

        public override String ToString()
        {
            return String.Format( "{{{0}, {1}}}", this.left, this.right );
        }
    }

    /// <summary>
    /// S_functions
    /// </summary>
    static partial class snd
    {
        public static Boolean IsInitialized
        {
            get
            {
                return _Controller.IsInitialized;
            }
        }

        public static dma_t shm
        {
            get
            {
                return _shm;
            }
        }

        public static Single BgmVolume
        {
            get
            {
                return _BgmVolume.Value;
            }
        }

        public static Single Volume
        {
            get
            {
                return _Volume.Value;
            }
        }

        public const Int32 DEFAULT_SOUND_PACKET_VOLUME = 255;
        public const Single DEFAULT_SOUND_PACKET_ATTENUATION = 1.0f;
        public const Int32 MAX_CHANNELS = 128;
        public const Int32 MAX_DYNAMIC_CHANNELS = 8;

        private const Int32 MAX_SFX = 512;

        private static CVar _BgmVolume = new CVar( "bgmvolume", "1", true );// = { "bgmvolume", "1", true };
        private static CVar _Volume = new CVar( "volume", "0.7", true );// = { "volume", "0.7", true };
        private static CVar _NoSound = new CVar( "nosound", "0" );// = { "nosound", "0" };
        private static CVar _Precache = new CVar( "precache", "1" );// = { "precache", "1" };
        private static CVar _LoadAs8bit = new CVar( "loadas8bit", "0" );// = { "loadas8bit", "0" };
        private static CVar _BgmBuffer = new CVar( "bgmbuffer", "4096" );// = { "bgmbuffer", "4096" };
        private static CVar _AmbientLevel = new CVar( "ambient_level", "0.3" );// = { "ambient_level", "0.3" };
        private static CVar _AmbientFade = new CVar( "ambient_fade", "100" );// = { "ambient_fade", "100" };
        private static CVar _NoExtraUpdate = new CVar( "snd_noextraupdate", "0" );// = { "snd_noextraupdate", "0" };
        private static CVar _Show = new CVar( "snd_show", "0" );// = { "snd_show", "0" };
        private static CVar _MixAhead = new CVar( "_snd_mixahead", "0.1", true );// = { "_snd_mixahead", "0.1", true };

        private static ISoundController _Controller = new OpenALController();// NullSoundController();
        private static Boolean _IsInitialized; // snd_initialized

        private static sfx_t[] _KnownSfx = new sfx_t[MAX_SFX]; // hunk allocated [MAX_SFX]
        private static Int32 _NumSfx; // num_sfx
        private static sfx_t[] _AmbientSfx = new sfx_t[AmbientDef.NUM_AMBIENTS]; // *ambient_sfx[NUM_AMBIENTS]
        private static Boolean _Ambient = true; // snd_ambient
        private static dma_t _shm = new dma_t(); // shm

        // 0 to MAX_DYNAMIC_CHANNELS-1	= normal entity sounds
        // MAX_DYNAMIC_CHANNELS to MAX_DYNAMIC_CHANNELS + NUM_AMBIENTS -1 = water, etc
        // MAX_DYNAMIC_CHANNELS + NUM_AMBIENTS to total_channels = static sounds
        private static channel_t[] _Channels = new channel_t[MAX_CHANNELS]; // channels[MAX_CHANNELS]

        private static Int32 _TotalChannels; // total_channels

        private static Single _SoundNominalClipDist = 1000.0f; // sound_nominal_clip_dist
        private static Vector3 _ListenerOrigin; // listener_origin
        private static Vector3 _ListenerForward; // listener_forward
        private static Vector3 _ListenerRight; // listener_right
        private static Vector3 _ListenerUp; // listener_up

        private static Int32 _SoundTime; // soundtime		// sample PAIRS
        private static Int32 _PaintedTime; // paintedtime 	// sample PAIRS
        private static Boolean _SoundStarted; // sound_started
        private static Int32 _SoundBlocked = 0; // snd_blocked
        private static Int32 _OldSamplePos; // oldsamplepos from GetSoundTime()
        private static Int32 _Buffers; // buffers from GetSoundTime()
        private static Int32 _PlayHash = 345; // hash from S_Play()
        private static Int32 _PlayVolHash = 543; // hash S_PlayVol

        // CHANGE
        private static Host Host
        {
            get;
            set;
        }

        // S_Init (void)
        public static void Init( Host host )
        {
            Host = host;

            Con.Print( "\nSound Initialization\n" );

            if( CommandLine.HasParam( "-nosound" ) )
                return;

            for( var i = 0; i < _Channels.Length; i++ )
                _Channels[i] = new channel_t();

            Host.Command.Add( "play", Play );
            Host.Command.Add( "playvol", PlayVol );
            Host.Command.Add( "stopsound", StopAllSoundsCmd );
            Host.Command.Add( "soundlist", SoundList );
            Host.Command.Add( "soundinfo", SoundInfo_f );

            _IsInitialized = true;

            Startup();

            InitScaletable();

            _NumSfx = 0;

            Con.Print( "Sound sampling rate: {0}\n", _shm.speed );

            // provides a tick sound until washed clean
            _AmbientSfx[AmbientDef.AMBIENT_WATER] = PrecacheSound( "ambience/water1.wav" );
            _AmbientSfx[AmbientDef.AMBIENT_SKY] = PrecacheSound( "ambience/wind2.wav" );

            StopAllSounds( true );
        }

        // S_AmbientOff (void)
        public static void AmbientOff()
        {
            _Ambient = false;
        }

        // S_AmbientOn (void)
        public static void AmbientOn()
        {
            _Ambient = true;
        }

        // S_Shutdown (void)
        public static void Shutdown()
        {
            if( !_Controller.IsInitialized )
                return;

            if( _shm != null )
                _shm.gamealive = false;

            _Controller.Shutdown();
            _shm = null;
        }

        // S_TouchSound (char *sample)
        public static void TouchSound( String sample )
        {
            if( !_Controller.IsInitialized )
                return;

            sfx_t sfx = FindName( sample );
            Host.Cache.Check( sfx.cache );
        }

        // S_ClearBuffer (void)
        public static void ClearBuffer()
        {
            if( !_Controller.IsInitialized || _shm == null || _shm.buffer == null )
                return;

            _Controller.ClearBuffer();
        }

        // S_StaticSound (sfx_t *sfx, vec3_t origin, float vol, float attenuation)
        public static void StaticSound( sfx_t sfx, ref Vector3 origin, Single vol, Single attenuation )
        {
            if( sfx == null )
                return;

            if( _TotalChannels == MAX_CHANNELS )
            {
                Con.Print( "total_channels == MAX_CHANNELS\n" );
                return;
            }

            channel_t ss = _Channels[_TotalChannels];
            _TotalChannels++;

            sfxcache_t sc = LoadSound( sfx );
            if( sc == null )
                return;

            if( sc.loopstart == -1 )
            {
                Con.Print( "Sound {0} not looped\n", sfx.name );
                return;
            }

            ss.sfx = sfx;
            ss.origin = origin;
            ss.master_vol = ( Int32 ) vol;
            ss.dist_mult = ( attenuation / 64 ) / _SoundNominalClipDist;
            ss.end = _PaintedTime + sc.length;

            Spatialize( ss );
        }

        // S_StartSound (int entnum, int entchannel, sfx_t *sfx, vec3_t origin, float fvol,  float attenuation)
        public static void StartSound( Int32 entnum, Int32 entchannel, sfx_t sfx, ref Vector3 origin, Single fvol, Single attenuation )
        {
            if( !_SoundStarted || sfx == null )
                return;

            if( _NoSound.Value != 0 )
                return;

            var vol = ( Int32 ) ( fvol * 255 );

            // pick a channel to play on
            channel_t target_chan = PickChannel( entnum, entchannel );
            if( target_chan == null )
                return;

            // spatialize
            //memset (target_chan, 0, sizeof(*target_chan));
            target_chan.origin = origin;
            target_chan.dist_mult = attenuation / _SoundNominalClipDist;
            target_chan.master_vol = vol;
            target_chan.entnum = entnum;
            target_chan.entchannel = entchannel;
            Spatialize( target_chan );

            if( target_chan.leftvol == 0 && target_chan.rightvol == 0 )
                return;		// not audible at all

            // new channel
            sfxcache_t sc = LoadSound( sfx );
            if( sc == null )
            {
                target_chan.sfx = null;
                return;		// couldn't load the sound's data
            }

            target_chan.sfx = sfx;
            target_chan.pos = 0;
            target_chan.end = _PaintedTime + sc.length;

            // if an identical sound has also been started this frame, offset the pos
            // a bit to keep it from just making the first one louder
            for( var i = AmbientDef.NUM_AMBIENTS; i < AmbientDef.NUM_AMBIENTS + MAX_DYNAMIC_CHANNELS; i++ )
            {
                channel_t check = _Channels[i];
                if( check == target_chan )
                    continue;

                if( check.sfx == sfx && check.pos == 0 )
                {
                    var skip = MathLib.Random( ( Int32 ) ( 0.1 * _shm.speed ) );// rand() % (int)(0.1 * shm->speed);
                    if( skip >= target_chan.end )
                        skip = target_chan.end - 1;
                    target_chan.pos += skip;
                    target_chan.end -= skip;
                    break;
                }
            }
        }

        // S_StopSound (int entnum, int entchannel)
        public static void StopSound( Int32 entnum, Int32 entchannel )
        {
            for( var i = 0; i < MAX_DYNAMIC_CHANNELS; i++ )
            {
                if( _Channels[i].entnum == entnum &&
                    _Channels[i].entchannel == entchannel )
                {
                    _Channels[i].end = 0;
                    _Channels[i].sfx = null;
                    return;
                }
            }
        }

        // sfx_t *S_PrecacheSound (char *sample)
        public static sfx_t PrecacheSound( String sample )
        {
            if( !_IsInitialized || _NoSound.Value != 0 )
                return null;

            sfx_t sfx = FindName( sample );

            // cache it in
            if( _Precache.Value != 0 )
                LoadSound( sfx );

            return sfx;
        }

        // void S_ClearPrecache (void)
        public static void ClearPrecache()
        {
            // nothing to do
        }

        // void S_Update (vec3_t origin, vec3_t v_forward, vec3_t v_right, vec3_t v_up)
        //
        // Called once each time through the main loop
        public static void Update( ref Vector3 origin, ref Vector3 forward, ref Vector3 right, ref Vector3 up )
        {
            if( !_IsInitialized || ( _SoundBlocked > 0 ) )
                return;

            _ListenerOrigin = origin;
            _ListenerForward = forward;
            _ListenerRight = right;
            _ListenerUp = up;

            // update general area ambient sound sources
            UpdateAmbientSounds();

            channel_t combine = null;

            // update spatialization for static and dynamic sounds
            //channel_t ch = channels + NUM_AMBIENTS;
            for( var i = AmbientDef.NUM_AMBIENTS; i < _TotalChannels; i++ )
            {
                channel_t ch = _Channels[i];// channels + NUM_AMBIENTS;
                if( ch.sfx == null )
                    continue;

                Spatialize( ch );  // respatialize channel
                if( ch.leftvol == 0 && ch.rightvol == 0 )
                    continue;

                // try to combine static sounds with a previous channel of the same
                // sound effect so we don't mix five torches every frame
                if( i >= MAX_DYNAMIC_CHANNELS + AmbientDef.NUM_AMBIENTS )
                {
                    // see if it can just use the last one
                    if( combine != null && combine.sfx == ch.sfx )
                    {
                        combine.leftvol += ch.leftvol;
                        combine.rightvol += ch.rightvol;
                        ch.leftvol = ch.rightvol = 0;
                        continue;
                    }
                    // search for one
                    combine = _Channels[MAX_DYNAMIC_CHANNELS + AmbientDef.NUM_AMBIENTS];// channels + MAX_DYNAMIC_CHANNELS + NUM_AMBIENTS;
                    Int32 j;
                    for( j = MAX_DYNAMIC_CHANNELS + AmbientDef.NUM_AMBIENTS; j < i; j++ )
                    {
                        combine = _Channels[j];
                        if( combine.sfx == ch.sfx )
                            break;
                    }

                    if( j == _TotalChannels )
                    {
                        combine = null;
                    }
                    else
                    {
                        if( combine != ch )
                        {
                            combine.leftvol += ch.leftvol;
                            combine.rightvol += ch.rightvol;
                            ch.leftvol = ch.rightvol = 0;
                        }
                        continue;
                    }
                }
            }

            //
            // debugging output
            //
            if( _Show.Value != 0 )
            {
                var total = 0;
                for( var i = 0; i < _TotalChannels; i++ )
                {
                    channel_t ch = _Channels[i];
                    if( ch.sfx != null && ( ch.leftvol > 0 || ch.rightvol > 0 ) )
                    {
                        total++;
                    }
                }
                Con.Print( "----({0})----\n", total );
            }

            // mix some sound
            Update();
        }

        // S_StopAllSounds (qboolean clear)
        public static void StopAllSounds( Boolean clear )
        {
            if( !_Controller.IsInitialized )
                return;

            _TotalChannels = MAX_DYNAMIC_CHANNELS + AmbientDef.NUM_AMBIENTS;	// no statics

            for( var i = 0; i < MAX_CHANNELS; i++ )
                if( _Channels[i].sfx != null )
                    _Channels[i].Clear();

            if( clear )
                ClearBuffer();
        }

        // void S_BeginPrecaching (void)
        public static void BeginPrecaching()
        {
            // nothing to do
        }

        // void S_EndPrecaching (void)
        public static void EndPrecaching()
        {
            // nothing to do
        }

        // void S_ExtraUpdate (void)
        public static void ExtraUpdate()
        {
            if( !_IsInitialized )
                return;
#if _WIN32
	        IN_Accumulate ();
#endif

            if( _NoExtraUpdate.Value != 0 )
                return;		// don't pollute timings

            Update();
        }

        // void S_LocalSound (char *s)
        public static void LocalSound( String sound )
        {
            if( _NoSound.Value != 0 )
                return;

            if( !_Controller.IsInitialized )
                return;

            sfx_t sfx = PrecacheSound( sound );
            if( sfx == null )
            {
                Con.Print( "S_LocalSound: can't cache {0}\n", sound );
                return;
            }
            StartSound( client.cl.viewentity, -1, sfx, ref Utilities.ZeroVector, 1, 1 );
        }

        // S_Startup
        public static void Startup()
        {
            if( _IsInitialized && !_Controller.IsInitialized )
            {
                _Controller.Init();
                _SoundStarted = _Controller.IsInitialized;
            }
        }

        /// <summary>
        /// S_BlockSound
        /// </summary>
        public static void BlockSound()
        {
            _SoundBlocked++;

            if( _SoundBlocked == 1 )
            {
                _Controller.ClearBuffer();  //waveOutReset (hWaveOut);
            }
        }

        /// <summary>
        /// S_UnblockSound
        /// </summary>
        public static void UnblockSound()
        {
            _SoundBlocked--;
        }

        // S_Play
        private static void Play()
        {
            for( var i = 1; i < Host.Command.Argc; i++ )
            {
                var name = Host.Command.Argv( i );
                var k = name.IndexOf( '.' );
                if( k == -1 )
                    name += ".wav";

                sfx_t sfx = PrecacheSound( name );
                StartSound( _PlayHash++, 0, sfx, ref _ListenerOrigin, 1.0f, 1.0f );
            }
        }

        // S_PlayVol
        private static void PlayVol()
        {
            for( var i = 1; i < Host.Command.Argc; i += 2 )
            {
                var name = Host.Command.Argv( i );
                var k = name.IndexOf( '.' );
                if( k == -1 )
                    name += ".wav";

                sfx_t sfx = PrecacheSound( name );
                var vol = Single.Parse( Host.Command.Argv( i + 1 ) );
                StartSound( _PlayVolHash++, 0, sfx, ref _ListenerOrigin, vol, 1.0f );
            }
        }

        // S_SoundList
        private static void SoundList()
        {
            var total = 0;
            for( var i = 0; i < _NumSfx; i++ )
            {
                sfx_t sfx = _KnownSfx[i];
                sfxcache_t sc = (sfxcache_t) Host.Cache.Check( sfx.cache );
                if( sc == null )
                    continue;

                var size = sc.length * sc.width * ( sc.stereo + 1 );
                total += size;
                if( sc.loopstart >= 0 )
                    Con.Print( "L" );
                else
                    Con.Print( " " );
                Con.Print( "({0:d2}b) {1:g6} : {2}\n", sc.width * 8, size, sfx.name );
            }
            Con.Print( "Total resident: {0}\n", total );
        }

        // S_SoundInfo_f
        private static void SoundInfo_f()
        {
            if( !_Controller.IsInitialized || _shm == null )
            {
                Con.Print( "sound system not started\n" );
                return;
            }

            Con.Print( "{0:d5} stereo\n", _shm.channels - 1 );
            Con.Print( "{0:d5} samples\n", _shm.samples );
            Con.Print( "{0:d5} samplepos\n", _shm.samplepos );
            Con.Print( "{0:d5} samplebits\n", _shm.samplebits );
            Con.Print( "{0:d5} submission_chunk\n", _shm.submission_chunk );
            Con.Print( "{0:d5} speed\n", _shm.speed );
            //Con.Print("0x%x dma buffer\n", _shm.buffer);
            Con.Print( "{0:d5} total_channels\n", _TotalChannels );
        }

        // S_StopAllSoundsC
        private static void StopAllSoundsCmd()
        {
            StopAllSounds( true );
        }

        // S_FindName
        private static sfx_t FindName( String name )
        {
            if( String.IsNullOrEmpty( name ) )
                Utilities.Error( "S_FindName: NULL or empty\n" );

            if( name.Length >= QDef.MAX_QPATH )
                Utilities.Error( "Sound name too long: {0}", name );

            // see if already loaded
            for( var i = 0; i < _NumSfx; i++ )
            {
                if( _KnownSfx[i].name == name )// !Q_strcmp(known_sfx[i].name, name))
                    return _KnownSfx[i];
            }

            if( _NumSfx == MAX_SFX )
                Utilities.Error( "S_FindName: out of sfx_t" );

            sfx_t sfx = _KnownSfx[_NumSfx];
            sfx.name = name;

            _NumSfx++;
            return sfx;
        }

        // SND_Spatialize
        private static void Spatialize( channel_t ch )
        {
            // anything coming from the view entity will allways be full volume
            if( ch.entnum == client.cl.viewentity )
            {
                ch.leftvol = ch.master_vol;
                ch.rightvol = ch.master_vol;
                return;
            }

            // calculate stereo seperation and distance attenuation
            sfx_t snd = ch.sfx;
            Vector3 source_vec = ch.origin - _ListenerOrigin;

            var dist = MathLib.Normalize( ref source_vec ) * ch.dist_mult;
            var dot = Vector3.Dot( _ListenerRight, source_vec );

            Single rscale, lscale;
            if( _shm.channels == 1 )
            {
                rscale = 1.0f;
                lscale = 1.0f;
            }
            else
            {
                rscale = 1.0f + dot;
                lscale = 1.0f - dot;
            }

            // add in distance effect
            var scale = ( 1.0f - dist ) * rscale;
            ch.rightvol = ( Int32 ) ( ch.master_vol * scale );
            if( ch.rightvol < 0 )
                ch.rightvol = 0;

            scale = ( 1.0f - dist ) * lscale;
            ch.leftvol = ( Int32 ) ( ch.master_vol * scale );
            if( ch.leftvol < 0 )
                ch.leftvol = 0;
        }

        // S_LoadSound
        private static sfxcache_t LoadSound( sfx_t s )
        {
            // see if still in memory
            sfxcache_t sc = (sfxcache_t) Host.Cache.Check( s.cache );
            if( sc != null )
                return sc;

            // load it in
            var namebuffer = "sound/" + s.name;

            Byte[] data = FileSystem.LoadFile( namebuffer );
            if( data == null )
            {
                Con.Print( "Couldn't load {0}\n", namebuffer );
                return null;
            }

            wavinfo_t info = GetWavInfo( s.name, data );
            if( info.channels != 1 )
            {
                Con.Print( "{0} is a stereo sample\n", s.name );
                return null;
            }

            var stepscale = info.rate / ( Single ) _shm.speed;
            var len = ( Int32 ) ( info.samples / stepscale );

            len *= info.width * info.channels;

            s.cache = Host.Cache.Alloc( len, s.name );
            if( s.cache == null )
                return null;

            sc = new sfxcache_t();
            sc.length = info.samples;
            sc.loopstart = info.loopstart;
            sc.speed = info.rate;
            sc.width = info.width;
            sc.stereo = info.channels;
            s.cache.data = sc;

            ResampleSfx( s, sc.speed, sc.width, new ByteArraySegment( data, info.dataofs ) );

            return sc;
        }

        // SND_PickChannel
        private static channel_t PickChannel( Int32 entnum, Int32 entchannel )
        {
            // Check for replacement sound, or find the best one to replace
            var first_to_die = -1;
            var life_left = 0x7fffffff;
            for( var ch_idx = AmbientDef.NUM_AMBIENTS; ch_idx < AmbientDef.NUM_AMBIENTS + MAX_DYNAMIC_CHANNELS; ch_idx++ )
            {
                if( entchannel != 0		// channel 0 never overrides
                    && _Channels[ch_idx].entnum == entnum
                    && ( _Channels[ch_idx].entchannel == entchannel || entchannel == -1 ) )
                {
                    // allways override sound from same entity
                    first_to_die = ch_idx;
                    break;
                }

                // don't let monster sounds override player sounds
                if( _Channels[ch_idx].entnum == client.cl.viewentity && entnum != client.cl.viewentity && _Channels[ch_idx].sfx != null )
                    continue;

                if( _Channels[ch_idx].end - _PaintedTime < life_left )
                {
                    life_left = _Channels[ch_idx].end - _PaintedTime;
                    first_to_die = ch_idx;
                }
            }

            if( first_to_die == -1 )
                return null;

            if( _Channels[first_to_die].sfx != null )
                _Channels[first_to_die].sfx = null;

            return _Channels[first_to_die];
        }

        // S_UpdateAmbientSounds
        private static void UpdateAmbientSounds()
        {
            if( !_Ambient )
                return;

            // calc ambient sound levels
            if( client.cl.worldmodel == null )
                return;

            MemoryLeaf l = Mod.PointInLeaf( ref _ListenerOrigin, client.cl.worldmodel );
            if( l == null || _AmbientLevel.Value == 0 )
            {
                for( var i = 0; i < AmbientDef.NUM_AMBIENTS; i++ )
                    _Channels[i].sfx = null;
                return;
            }

            for( var i = 0; i < AmbientDef.NUM_AMBIENTS; i++ )
            {
                channel_t chan = _Channels[i];
                chan.sfx = _AmbientSfx[i];

                var vol = _AmbientLevel.Value * l.ambient_sound_level[i];
                if( vol < 8 )
                    vol = 0;

                // don't adjust volume too fast
                if( chan.master_vol < vol )
                {
                    chan.master_vol += ( Int32 ) ( Host.FrameTime * _AmbientFade.Value );
                    if( chan.master_vol > vol )
                        chan.master_vol = ( Int32 ) vol;
                }
                else if( chan.master_vol > vol )
                {
                    chan.master_vol -= ( Int32 ) ( Host.FrameTime * _AmbientFade.Value );
                    if( chan.master_vol < vol )
                        chan.master_vol = ( Int32 ) vol;
                }

                chan.leftvol = chan.rightvol = chan.master_vol;
            }
        }

        // S_Update_
        private static void Update()
        {
            if( !_SoundStarted || ( _SoundBlocked > 0 ) )
                return;

            // Updates DMA time
            GetSoundTime();

            // check to make sure that we haven't overshot
            if( _PaintedTime < _SoundTime )
                _PaintedTime = _SoundTime;

            // mix ahead of current position
            var endtime = ( Int32 ) ( _SoundTime + _MixAhead.Value * _shm.speed );
            var samps = _shm.samples >> ( _shm.channels - 1 );
            if( endtime - _SoundTime > samps )
                endtime = _SoundTime + samps;

            PaintChannels( endtime );
        }

        // GetSoundtime
        private static void GetSoundTime()
        {
            var fullsamples = _shm.samples / _shm.channels;
            var samplepos = _Controller.GetPosition();
            if( samplepos < _OldSamplePos )
            {
                _Buffers++; // buffer wrapped

                if( _PaintedTime > 0x40000000 )
                {
                    // time to chop things off to avoid 32 bit limits
                    _Buffers = 0;
                    _PaintedTime = fullsamples;
                    StopAllSounds( true );
                }
            }
            _OldSamplePos = samplepos;
            _SoundTime = _Buffers * fullsamples + samplepos / _shm.channels;
        }

        static snd()
        {
            for( var i = 0; i < _KnownSfx.Length; i++ )
                _KnownSfx[i] = new sfx_t();
        }
    }

    // portable_samplepair_t;

    internal class sfx_t
    {
        public String name; // char[MAX_QPATH];
        public CacheUser cache; // cache_user_t

        public void Clear()
        {
            this.name = null;
            cache = null;
        }
    } // sfx_t;

    // !!! if this is changed, it much be changed in asm_i386.h too !!!
    internal class sfxcache_t
    {
        public Int32 length;
        public Int32 loopstart;
        public Int32 speed;
        public Int32 width;
        public Int32 stereo;
        public Byte[] data; // [1];		// variable sized
    } // sfxcache_t;

    internal class dma_t
    {
        public Boolean gamealive;
        public Boolean soundalive;
        public Boolean splitbuffer;
        public Int32 channels;
        public Int32 samples;             // mono samples in buffer
        public Int32 submission_chunk;        // don't mix less than this #
        public Int32 samplepos;               // in mono samples
        public Int32 samplebits;
        public Int32 speed;
        public Byte[] buffer;
    } // dma_t;

    // !!! if this is changed, it much be changed in asm_i386.h too !!!
    [StructLayout( LayoutKind.Sequential )]
    internal class channel_t
    {
        public sfx_t sfx;			// sfx number
        public Int32 leftvol;		// 0-255 volume
        public Int32 rightvol;		// 0-255 volume
        public Int32 end;			// end time in global paintsamples
        public Int32 pos;			// sample position in sfx
        public Int32 looping;		// where to loop, -1 = no looping
        public Int32 entnum;			// to allow overriding a specific sound
        public Int32 entchannel;		//
        public Vector3 origin;			// origin of sound effect
        public Single dist_mult;		// distance multiplier (attenuation/clipK)
        public Int32 master_vol;		// 0-255 master volume

        public void Clear()
        {
            sfx = null;
            leftvol = 0;
            rightvol = 0;
            end = 0;
            pos = 0;
            looping = 0;
            entnum = 0;
            entchannel = 0;
            origin = Vector3.Zero;
            dist_mult = 0;
            master_vol = 0;
        }
    } // channel_t;

    //[StructLayout(LayoutKind.Sequential)]
    internal class wavinfo_t
    {
        public Int32 rate;
        public Int32 width;
        public Int32 channels;
        public Int32 loopstart;
        public Int32 samples;
        public Int32 dataofs;		// chunk starts this many bytes from file start
    } // wavinfo_t;

    internal interface ISoundController
    {
        Boolean IsInitialized
        {
            get;
        }

        void Init();

        void Shutdown();

        void ClearBuffer();

        Byte[] LockBuffer();

        void UnlockBuffer( Int32 count );

        Int32 GetPosition();

        //void Submit();
    }
}
