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
using OpenTK.Audio;
using OpenTK.Audio.OpenAL;

namespace SharpQuake
{
    internal class OpenALController : ISoundController
    {
        private const Int32 AL_BUFFER_COUNT = 24;
        private const Int32 BUFFER_SIZE = 0x10000;

        private Boolean _IsInitialized;
        private AudioContext _Context;
        private Int32 _Source;
        private Int32[] _Buffers;
        private Int32[] _BufferBytes;
        private ALFormat _BufferFormat;
        private Int32 _SamplesSent;
        private Queue<Int32> _FreeBuffers;

        private void FreeContext()
        {
            if( _Source != 0 )
            {
                AL.SourceStop( _Source );
                AL.DeleteSource( _Source );
                _Source = 0;
            }
            if( _Buffers != null )
            {
                AL.DeleteBuffers( _Buffers );
                _Buffers = null;
            }
            if( _Context != null )
            {
                _Context.Dispose();
                _Context = null;
            }
        }

        #region ISoundController Members

        public Boolean IsInitialized
        {
            get
            {
                return _IsInitialized;
            }
        }

        public void Init()
        {
            FreeContext();

            _Context = new AudioContext();
            _Source = AL.GenSource();
            _Buffers = new Int32[AL_BUFFER_COUNT];
            _BufferBytes = new Int32[AL_BUFFER_COUNT];
            _FreeBuffers = new Queue<Int32>( AL_BUFFER_COUNT );

            for( var i = 0; i < _Buffers.Length; i++ )
            {
                _Buffers[i] = AL.GenBuffer();
                _FreeBuffers.Enqueue( _Buffers[i] );
            }

            AL.SourcePlay( _Source );
            AL.Source( _Source, ALSourceb.Looping, false );

            snd.shm.channels = 2;
            snd.shm.samplebits = 16;
            snd.shm.speed = 11025;
            snd.shm.buffer = new Byte[BUFFER_SIZE];
            snd.shm.soundalive = true;
            snd.shm.splitbuffer = false;
            snd.shm.samples = snd.shm.buffer.Length / ( snd.shm.samplebits / 8 );
            snd.shm.samplepos = 0;
            snd.shm.submission_chunk = 1;

            if( snd.shm.samplebits == 8 )
            {
                if( snd.shm.channels == 2 )
                    _BufferFormat = ALFormat.Stereo8;
                else
                    _BufferFormat = ALFormat.Mono8;
            }
            else
            {
                if( snd.shm.channels == 2 )
                    _BufferFormat = ALFormat.Stereo16;
                else
                    _BufferFormat = ALFormat.Mono16;
            }

            _IsInitialized = true;
        }

        public void Shutdown()
        {
            FreeContext();
            _IsInitialized = false;
        }

        public void ClearBuffer()
        {
            AL.SourceStop( _Source );
        }

        public Byte[] LockBuffer()
        {
            return snd.shm.buffer;
        }

        public void UnlockBuffer( Int32 bytes )
        {
            Int32 processed;
            AL.GetSource( _Source, ALGetSourcei.BuffersProcessed, out processed );
            if( processed > 0 )
            {
                Int32[] bufs = AL.SourceUnqueueBuffers( _Source, processed );
                foreach( var buffer in bufs )
                {
                    if( buffer == 0 )
                        continue;

                    var idx = Array.IndexOf( _Buffers, buffer );
                    if( idx != -1 )
                    {
                        _SamplesSent += _BufferBytes[idx] >> ( ( snd.shm.samplebits / 8 ) - 1 );
                        _SamplesSent &= ( snd.shm.samples - 1 );
                        _BufferBytes[idx] = 0;
                    }
                    if( !_FreeBuffers.Contains( buffer ) )
                        _FreeBuffers.Enqueue( buffer );
                }
            }

            if( _FreeBuffers.Count == 0 )
            {
                Con.DPrint( "UnlockBuffer: No free buffers!\n" );
                return;
            }

            var buf = _FreeBuffers.Dequeue();
            if( buf != 0 )
            {
                AL.BufferData( buf, _BufferFormat, snd.shm.buffer, bytes, snd.shm.speed );
                AL.SourceQueueBuffer( _Source, buf );

                var idx = Array.IndexOf( _Buffers, buf );
                if( idx != -1 )
                {
                    _BufferBytes[idx] = bytes;
                }

                Int32 state;
                AL.GetSource( _Source, ALGetSourcei.SourceState, out state );
                if( (ALSourceState)state != ALSourceState.Playing )
                {
                    AL.SourcePlay( _Source );
                    Con.DPrint( "Sound resumed from {0}, free {1} of {2} buffers\n",
                        ( (ALSourceState)state ).ToString( "F" ), _FreeBuffers.Count, _Buffers.Length );
                }
            }
        }

        public Int32 GetPosition()
        {
            Int32 state, offset = 0;
            AL.GetSource( _Source, ALGetSourcei.SourceState, out state );
            if( (ALSourceState)state != ALSourceState.Playing )
            {
                for( var i = 0; i < _BufferBytes.Length; i++ )
                {
                    _SamplesSent += _BufferBytes[i] >> ( ( snd.shm.samplebits / 8 ) - 1 );
                    _BufferBytes[i] = 0;
                }
                _SamplesSent &= ( snd.shm.samples - 1 );
            }
            else
            {
                AL.GetSource( _Source, ALGetSourcei.SampleOffset, out offset );
            }
            return ( _SamplesSent + offset ) & ( snd.shm.samples - 1 );
        }

        #endregion ISoundController Members
    }
}
