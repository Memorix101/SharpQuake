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
using System.Drawing;
using SharpQuake.Framework;

namespace SharpQuake.Renderer
{
    public class Palette : IDisposable
    {
        private UInt16[] _8to16table = new UInt16[256]; // d_8to16table[256]
        private UInt32[] _8to24table = new UInt32[256]; // d_8to24table[256]
        private Byte[] _15to8table = new Byte[65536]; // d_15to8table[65536]

        public UInt16[] Table8to16
        {
            get
            {
                return _8to16table;
            }
        }

        public UInt32[] Table8to24
        {
            get
            {
                return _8to24table;
            }
        }

        public Byte[] Table15to8
        {
            get
            {
                return _15to8table;
            }
        }

        private BaseDevice Device
        {
            get;
            set;
        }

        public Byte[] Data
        {
            get;
            private set;
        }

        public Palette( BaseDevice device )
        {
            Device = device;
        }

        /// <summary>
        /// VID_SetPalette
        /// called at startup and after any gamma correction
        /// </summary>
        public void Initialise( Byte[] palette )
        {
            Data = palette;

            //
            // 8 8 8 encoding
            //
            var offset = 0;
            var pal = palette;
            var table = _8to24table;
            for ( var i = 0; i < table.Length; i++ )
            {
                UInt32 r = pal[offset + 0];
                UInt32 g = pal[offset + 1];
                UInt32 b = pal[offset + 2];

                table[i] = ( ( UInt32 ) 0xff << 24 ) + ( r << 0 ) + ( g << 8 ) + ( b << 16 );
                offset += 3;
            }

            table[255] &= 0xffffff;	// 255 is transparent

            // JACK: 3D distance calcs - k is last closest, l is the distance.
            // FIXME: Precalculate this and cache to disk.
            var val = Union4b.Empty;
            for ( UInt32 i = 0; i < ( 1 << 15 ); i++ )
            {
                // Maps
                // 000000000000000
                // 000000000011111 = Red  = 0x1F
                // 000001111100000 = Blue = 0x03E0
                // 111110000000000 = Grn  = 0x7C00
                var r = ( ( ( i & 0x1F ) << 3 ) + 4 );
                var g = ( ( ( i & 0x03E0 ) >> 2 ) + 4 );
                var b = ( ( ( i & 0x7C00 ) >> 7 ) + 4 );
                UInt32 k = 0;
                UInt32 l = 10000 * 10000;
                for ( UInt32 v = 0; v < 256; v++ )
                {
                    val.ui0 = _8to24table[v];
                    var r1 = r - val.b0;
                    var g1 = g - val.b1;
                    var b1 = b - val.b2;
                    var j = ( r1 * r1 ) + ( g1 * g1 ) + ( b1 * b1 );
                    if ( j < l )
                    {
                        k = v;
                        l = j;
                    }
                }
                _15to8table[i] = ( Byte ) k;
            }
        }

        public void Dispose( )
        {
        }

        public Color ToColour( Int32 colour )
        {
            return Color.FromArgb( 255,
                 Data[colour * 3],
                 Data[colour * 3 + 1],
                 Data[colour * 3 + 2] );
        }

        // Check_Gamma
        public void CorrectGamma( Byte[] palette )
        {
            var i = CommandLine.CheckParm( "-gamma" );

            if ( i == 0 )
            {
                if ( Device.Desc.Renderer.Contains( "Voodoo" ) 
                    || Device.Desc.Vendor.Contains( "3Dfx" ) )
                    Device.Desc.Gamma = 1;
                else
                    Device.Desc.Gamma = 0.7f; // default to 0.7 on non-3dfx hardware
            }
            else
                Device.Desc.Gamma = Single.Parse( CommandLine.Argv( i + 1 ) );

            for ( i = 0; i < palette.Length; i++ )
            {
                var f = Math.Pow( ( palette[i] + 1 ) / 256.0, Device.Desc.Gamma );
                var inf = f * 255 + 0.5;

                if ( inf < 0 )
                    inf = 0;

                if ( inf > 255 )
                    inf = 255;

                palette[i] = ( Byte ) inf;
            }
        }
    }
}
