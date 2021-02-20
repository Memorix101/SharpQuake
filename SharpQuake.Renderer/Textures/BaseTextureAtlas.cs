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

namespace SharpQuake.Renderer.Textures
{
    public class BaseTextureAtlas : IDisposable
    {
        public BaseDevice Device
        {
            get;
            private set;
        }

        public Boolean IsDirty
        {
            get;
            private set;
        }

        public Int32 UploadCount
        {
            get;
            private set;
        }

        private Int32[][] Allocated
        {
            get;
            set;
        }

        private Byte[][] Texels
        {
            get;
            set;
        }

        private Int32 MaxTextures
        {
            get;
            set;
        }

        private Int32 Width
        {
            get;
            set;
        }

        private Int32 Height
        {
            get;
            set;
        }

        public BaseTexture[] Textures
        {
            get;
            private set;
        }

        public BaseTextureAtlas( BaseDevice device, Int32 maxTextures, Int32 width, Int32 height )
        {
            Device = device;
            MaxTextures = maxTextures;
            Width = width;
            Height = height;
            Textures = new BaseTexture[MaxTextures];

            Allocated = new Int32[MaxTextures][]; //[MAX_SCRAPS][BLOCK_WIDTH];
            for ( var i = 0; i < Allocated.GetLength( 0 ); i++ )
            {
                Allocated[i] = new Int32[Width];
            }

            Texels = new Byte[MaxTextures][]; // [MAX_SCRAPS][BLOCK_WIDTH*BLOCK_HEIGHT*4];
            for ( var i = 0; i < Texels.GetLength( 0 ); i++ )
            {
                Texels[i] = new Byte[Width * Height * 4];
            }
        }

        public virtual void Initialise( )
        {
        }       

        public virtual void Upload( Boolean resample )
        {
            UploadCount++;

            for ( var i = 0; i < MaxTextures; i++ )
            {
                var texture = Textures[i];

                if ( texture == null )
                {
                    texture = BaseTexture.FromBuffer( Device, Guid.NewGuid( ).ToString( ),
                        new ByteArraySegment( Texels[i] ), Width, Height, false, true, filter: "GL_NEAREST" );
                }
                else
                {
                    texture.Initialise( new ByteArraySegment( Texels[i] ) );
                    texture.Bind( );
                    texture.Upload8( resample );
                }

                Textures[i] = texture;
            }

            IsDirty = false;
        }

        public virtual void Dispose( )
        {
        }

        public virtual BaseTexture Add( ByteArraySegment buffer, BasePicture picture )
        {
            var textureNumber = Allocate( picture.Width, picture.Height, out var x, out var y );

            var source = new System.Drawing.RectangleF( );
            source.X = ( Single ) ( ( x + 0.01 ) / ( Single ) Height );
            source.Width = ( picture.Width ) / ( Single ) Width;
            source.Y = ( Single ) ( ( y + 0.01 ) / ( Single  )Height );
            source.Height = ( picture.Height ) / ( Single ) Height;

            picture.Source = source;

            IsDirty = true;

            var k = 0;

            for ( var i = 0; i < picture.Height; i++ )
            {
                for ( var j = 0; j < picture.Width; j++, k++ )
                    Texels[textureNumber][( y + i ) * Width + x + j] = buffer.Data[buffer.StartIndex + k];// p->data[k];
            }

            Upload( true );

            return Textures[textureNumber];
        }

        // Scrap_AllocBlock
        // returns a texture number and the position inside it
        protected virtual Int32 Allocate( Int32 width, Int32 height, out Int32 x, out Int32 y )
        {
            x = -1;
            y = -1;

            for ( var texnum = 0; texnum < MaxTextures; texnum++ )
            {
                var best = Height;

                for ( var i = 0; i < Width - width; i++ )
                {
                    Int32 best2 = 0, j;

                    for ( j = 0; j < width; j++ )
                    {
                        if ( Allocated[texnum][i + j] >= best )
                            break;
                        if ( Allocated[texnum][i + j] > best2 )
                            best2 = Allocated[texnum][i + j];
                    }
                    if ( j == width )
                    {
                        // this is a valid spot
                        x = i;
                        y = best = best2;
                    }
                }

                if ( best + height > Height )
                    continue;

                for ( var i = 0; i < width; i++ )
                    Allocated[texnum][x + i] = best + height;

                return texnum;
            }

            Utilities.Error( "Scrap_AllocBlock: full" );
            return -1;
        }
    }
}
