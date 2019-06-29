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

namespace SharpQuake
{
    partial class snd
    {
        private const int PAINTBUFFER_SIZE = 512;
        private const short C8000 = -32768;

        private static int[,] _ScaleTable = new int[32, 256];
        private static portable_samplepair_t[] _PaintBuffer = new portable_samplepair_t[PAINTBUFFER_SIZE]; // paintbuffer[PAINTBUFFER_SIZE]

        // SND_InitScaletable
        private static void InitScaletable()
        {
            for( int i = 0; i < 32; i++ )
                for( int j = 0; j < 256; j++ )
                    _ScaleTable[i, j] = ( (sbyte)j ) * i * 8;
        }

        // S_PaintChannels
        private static void PaintChannels( int endtime )
        {
            while( _PaintedTime < endtime )
            {
                // if paintbuffer is smaller than DMA buffer
                int end = endtime;
                if( endtime - _PaintedTime > PAINTBUFFER_SIZE )
                    end = _PaintedTime + PAINTBUFFER_SIZE;

                // clear the paint buffer
                Array.Clear( _PaintBuffer, 0, end - _PaintedTime );

                // paint in the channels.
                for( int i = 0; i < _TotalChannels; i++ )
                {
                    channel_t ch = _Channels[i];

                    if( ch.sfx == null )
                        continue;
                    if( ch.leftvol == 0 && ch.rightvol == 0 )
                        continue;

                    sfxcache_t sc = LoadSound( ch.sfx );
                    if( sc == null )
                        continue;

                    int count, ltime = _PaintedTime;

                    while( ltime < end )
                    {
                        // paint up to end
                        if( ch.end < end )
                            count = ch.end - ltime;
                        else
                            count = end - ltime;

                        if( count > 0 )
                        {
                            if( sc.width == 1 )
                                PaintChannelFrom8( ch, sc, count );
                            else
                                PaintChannelFrom16( ch, sc, count );

                            ltime += count;
                        }

                        // if at end of loop, restart
                        if( ltime >= ch.end )
                        {
                            if( sc.loopstart >= 0 )
                            {
                                ch.pos = sc.loopstart;
                                ch.end = ltime + sc.length - ch.pos;
                            }
                            else
                            {	// channel just stopped
                                ch.sfx = null;
                                break;
                            }
                        }
                    }
                }

                // transfer out according to DMA format
                TransferPaintBuffer( end );
                _PaintedTime = end;
            }
        }

        // SND_PaintChannelFrom8
        private static void PaintChannelFrom8( channel_t ch, sfxcache_t sc, int count )
        {
            if( ch.leftvol > 255 )
                ch.leftvol = 255;
            if( ch.rightvol > 255 )
                ch.rightvol = 255;

            int lscale = ch.leftvol >> 3;
            int rscale = ch.rightvol >> 3;
            byte[] sfx = sc.data;
            int offset = ch.pos;

            for( int i = 0; i < count; i++ )
            {
                int data = sfx[offset + i];
                _PaintBuffer[i].left += _ScaleTable[lscale, data];
                _PaintBuffer[i].right += _ScaleTable[rscale, data];
            }
            ch.pos += count;
        }

        // SND_PaintChannelFrom16
        private static void PaintChannelFrom16( channel_t ch, sfxcache_t sc, int count )
        {
            int leftvol = ch.leftvol;
            int rightvol = ch.rightvol;
            byte[] sfx = sc.data;
            int offset = ch.pos * 2; // sfx = (signed short *)sc->data + ch->pos;

            for( int i = 0; i < count; i++ )
            {
                int data = (short)( (ushort)sfx[offset] + ( (ushort)sfx[offset + 1] << 8 ) ); // Uze: check is this is right!!!
                int left = ( data * leftvol ) >> 8;
                int right = ( data * rightvol ) >> 8;
                _PaintBuffer[i].left += left;
                _PaintBuffer[i].right += right;
                offset += 2;
            }

            ch.pos += count;
        }

        // S_TransferPaintBuffer
        private static void TransferPaintBuffer( int endtime )
        {
            if( _shm.samplebits == 16 && _shm.channels == 2 )
            {
                TransferStereo16( endtime );
                return;
            }

            int count = ( endtime - _PaintedTime ) * _shm.channels;
            int out_mask = _shm.samples - 1;
            int out_idx = 0; //_PaintedTime * _shm.channels & out_mask;
            int step = 3 - _shm.channels;
            int snd_vol = (int)( _Volume.Value * 256 );
            byte[] buffer = _Controller.LockBuffer();
            Union4b uval = Union4b.Empty;
            int val, srcIndex = 0;
            bool useLeft = true;
            int destCount = ( count * ( _shm.samplebits >> 3 ) ) & out_mask;

            if( _shm.samplebits == 16 )
            {
                while( count-- > 0 )
                {
                    if( useLeft )
                        val = ( _PaintBuffer[srcIndex].left * snd_vol ) >> 8;
                    else
                        val = ( _PaintBuffer[srcIndex].right * snd_vol ) >> 8;
                    if( val > 0x7fff )
                        val = 0x7fff;
                    else if( val < C8000 )// (short)0x8000)
                        val = C8000;// (short)0x8000;

                    uval.i0 = val;
                    buffer[out_idx * 2] = uval.b0;
                    buffer[out_idx * 2 + 1] = uval.b1;

                    if( _shm.channels == 2 && useLeft )
                    {
                        useLeft = false;
                        out_idx += 2;
                    }
                    else
                    {
                        useLeft = true;
                        srcIndex++;
                        out_idx = ( out_idx + 1 ) & out_mask;
                    }
                }
            }
            else if( _shm.samplebits == 8 )
            {
                while( count-- > 0 )
                {
                    if( useLeft )
                        val = ( _PaintBuffer[srcIndex].left * snd_vol ) >> 8;
                    else
                        val = ( _PaintBuffer[srcIndex].right * snd_vol ) >> 8;
                    if( val > 0x7fff )
                        val = 0x7fff;
                    else if( val < C8000 )//(short)0x8000)
                        val = C8000;//(short)0x8000;

                    buffer[out_idx] = (byte)( ( val >> 8 ) + 128 );
                    out_idx = ( out_idx + 1 ) & out_mask;

                    if( _shm.channels == 2 && useLeft )
                        useLeft = false;
                    else
                    {
                        useLeft = true;
                        srcIndex++;
                    }
                }
            }

            _Controller.UnlockBuffer( destCount );
        }

        // S_TransferStereo16
        private static void TransferStereo16( int endtime )
        {
            int snd_vol = (int)( _Volume.Value * 256 );
            int lpaintedtime = _PaintedTime;
            byte[] buffer = _Controller.LockBuffer();
            int srcOffset = 0;
            int destCount = 0;//uze
            int destOffset = 0;
            Union4b uval = Union4b.Empty;

            while( lpaintedtime < endtime )
            {
                // handle recirculating buffer issues
                int lpos = lpaintedtime & ( ( _shm.samples >> 1 ) - 1 );
                //int destOffset = (lpos << 2); // in bytes!!!
                int snd_linear_count = ( _shm.samples >> 1 ) - lpos; // in portable_samplepair_t's!!!
                if( lpaintedtime + snd_linear_count > endtime )
                    snd_linear_count = endtime - lpaintedtime;

                // beginning of Snd_WriteLinearBlastStereo16
                // write a linear blast of samples
                for( int i = 0; i < snd_linear_count; i++ )
                {
                    int val1 = ( _PaintBuffer[srcOffset + i].left * snd_vol ) >> 8;
                    int val2 = ( _PaintBuffer[srcOffset + i].right * snd_vol ) >> 8;

                    if( val1 > 0x7fff )
                        val1 = 0x7fff;
                    else if( val1 < C8000 )
                        val1 = C8000;

                    if( val2 > 0x7fff )
                        val2 = 0x7fff;
                    else if( val2 < C8000 )
                        val2 = C8000;

                    uval.s0 = (short)val1;
                    uval.s1 = (short)val2;
                    buffer[destOffset + 0] = uval.b0;
                    buffer[destOffset + 1] = uval.b1;
                    buffer[destOffset + 2] = uval.b2;
                    buffer[destOffset + 3] = uval.b3;

                    destOffset += 4;
                }
                // end of Snd_WriteLinearBlastStereo16 ();

                // Uze
                destCount += snd_linear_count * 4;

                srcOffset += snd_linear_count; // snd_p += snd_linear_count;
                lpaintedtime += ( snd_linear_count );// >> 1);
            }

            _Controller.UnlockBuffer( destCount );
        }
    }
}
