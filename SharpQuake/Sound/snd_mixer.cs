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
using SharpQuake.Framework;
using SharpQuake.Framework.IO.Sound;

namespace SharpQuake
{
    public partial class snd
    {
        private const Int32 PAINTBUFFER_SIZE = 512;
        private const Int16 C8000 = -32768;

        private Int32[,] _ScaleTable = new Int32[32, 256];
        private PortableSamplePair_t[] _PaintBuffer = new PortableSamplePair_t[PAINTBUFFER_SIZE]; // paintbuffer[PAINTBUFFER_SIZE]

        // SND_InitScaletable
        private void InitScaletable()
        {
            for( var i = 0; i < 32; i++ )
                for( var j = 0; j < 256; j++ )
                    _ScaleTable[i, j] = ( ( SByte ) j ) * i * 8;
        }

        // S_PaintChannels
        private void PaintChannels( Int32 endtime )
        {
            while( _PaintedTime < endtime )
            {
                // if paintbuffer is smaller than DMA buffer
                var end = endtime;
                if( endtime - _PaintedTime > PAINTBUFFER_SIZE )
                    end = _PaintedTime + PAINTBUFFER_SIZE;

                // clear the paint buffer
                Array.Clear( _PaintBuffer, 0, end - _PaintedTime );

                // paint in the channels.
                for( var i = 0; i < _TotalChannels; i++ )
                {
                    var ch = _Channels[i];

                    if( ch.sfx == null )
                        continue;
                    if( ch.leftvol == 0 && ch.rightvol == 0 )
                        continue;

                    var sc = LoadSound( ch.sfx );
                    if( sc == null )
                        continue;

                    Int32 count, ltime = _PaintedTime;

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
        private void PaintChannelFrom8( Channel_t ch, SoundEffectCache_t sc, Int32 count )
        {
            if( ch.leftvol > 255 )
                ch.leftvol = 255;
            if( ch.rightvol > 255 )
                ch.rightvol = 255;

            var lscale = ch.leftvol >> 3;
            var rscale = ch.rightvol >> 3;
            var sfx = sc.data;
            var offset = ch.pos;

            for( var i = 0; i < count; i++ )
            {
                Int32 data = sfx[offset + i];
                _PaintBuffer[i].left += _ScaleTable[lscale, data];
                _PaintBuffer[i].right += _ScaleTable[rscale, data];
            }
            ch.pos += count;
        }

        // SND_PaintChannelFrom16
        private void PaintChannelFrom16( Channel_t ch, SoundEffectCache_t sc, Int32 count )
        {
            var leftvol = ch.leftvol;
            var rightvol = ch.rightvol;
            var sfx = sc.data;
            var offset = ch.pos * 2; // sfx = (signed short *)sc->data + ch->pos;

            for( var i = 0; i < count; i++ )
            {
                Int32 data = ( Int16 ) ( ( UInt16 ) sfx[offset] + ( ( UInt16 ) sfx[offset + 1] << 8 ) ); // Uze: check is this is right!!!
                var left = ( data * leftvol ) >> 8;
                var right = ( data * rightvol ) >> 8;
                _PaintBuffer[i].left += left;
                _PaintBuffer[i].right += right;
                offset += 2;
            }

            ch.pos += count;
        }

        // S_TransferPaintBuffer
        private void TransferPaintBuffer( Int32 endtime )
        {
            if( _shm.samplebits == 16 && _shm.channels == 2 )
            {
                TransferStereo16( endtime );
                return;
            }

            var count = ( endtime - _PaintedTime ) * _shm.channels;
            var out_mask = _shm.samples - 1;
            var out_idx = 0; //_PaintedTime * _shm.channels & out_mask;
            var step = 3 - _shm.channels;
            var snd_vol = ( Int32 ) ( Host.Cvars.Volume.Get<Single>( ) * 256 );
            var buffer = _Controller.LockBuffer();
            var uval = Union4b.Empty;
            Int32 val, srcIndex = 0;
            var useLeft = true;
            var destCount = ( count * ( _shm.samplebits >> 3 ) ) & out_mask;

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

                    buffer[out_idx] = ( Byte ) ( ( val >> 8 ) + 128 );
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
        private void TransferStereo16( Int32 endtime )
        {
            var snd_vol = ( Int32 ) ( Host.Cvars.Volume.Get<Single>( ) * 256 );
            var lpaintedtime = _PaintedTime;
            var buffer = _Controller.LockBuffer();
            var srcOffset = 0;
            var destCount = 0;//uze
            var destOffset = 0;
            var uval = Union4b.Empty;

            while( lpaintedtime < endtime )
            {
                // handle recirculating buffer issues
                var lpos = lpaintedtime & ( ( _shm.samples >> 1 ) - 1 );
                //int destOffset = (lpos << 2); // in bytes!!!
                var snd_linear_count = ( _shm.samples >> 1 ) - lpos; // in portable_samplepair_t's!!!
                if( lpaintedtime + snd_linear_count > endtime )
                    snd_linear_count = endtime - lpaintedtime;

                // beginning of Snd_WriteLinearBlastStereo16
                // write a linear blast of samples
                for( var i = 0; i < snd_linear_count; i++ )
                {
                    var val1 = ( _PaintBuffer[srcOffset + i].left * snd_vol ) >> 8;
                    var val2 = ( _PaintBuffer[srcOffset + i].right * snd_vol ) >> 8;

                    if( val1 > 0x7fff )
                        val1 = 0x7fff;
                    else if( val1 < C8000 )
                        val1 = C8000;

                    if( val2 > 0x7fff )
                        val2 = 0x7fff;
                    else if( val2 < C8000 )
                        val2 = C8000;

                    uval.s0 = ( Int16 ) val1;
                    uval.s1 = ( Int16 ) val2;
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
