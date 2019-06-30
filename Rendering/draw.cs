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
using OpenTK.Graphics.OpenGL;
using SharpQuake.Framework;
using Buffer = System.Buffer;

// gl_draw.c

namespace SharpQuake
{
    internal enum MTexTarget
    {
        TEXTURE0_SGIS = 0x835E,
        TEXTURE1_SGIS = 0x835F
    }

    /// <summary>
    /// Draw_functions, GL_functions
    /// </summary>
    internal static class Drawer
    {
        public static PixelInternalFormat AlphaFormat
        {
            get
            {
                return _AlphaFormat;
            }
        }

        public static PixelInternalFormat SolidFormat
        {
            get
            {
                return _SolidFormat;
            }
        }

        public static glpic_t Disc
        {
            get
            {
                return _Disc;
            }
        }

        public static Single glMaxSize
        {
            get
            {
                return _glMaxSize.Value;
            }
        }

        public static Int32 CurrentTexture = -1;

        public static PixelFormat LightMapFormat = PixelFormat.Rgba;

        private const Int32 MAX_GLTEXTURES = 1024;

        private const Int32 MAX_CACHED_PICS = 128;

        //
        //  scrap allocation
        //
        //  Allocate all the little status bar obejcts into a single texture
        //  to crutch up stupid hardware / drivers
        //
        private const Int32 MAX_SCRAPS = 2;

        private const Int32 BLOCK_WIDTH = 256;

        private const Int32 BLOCK_HEIGHT = 256;

        private static readonly glmode_t[] _Modes = new glmode_t[]
        {
            new glmode_t("GL_NEAREST", TextureMinFilter.Nearest, TextureMagFilter.Nearest),
            new glmode_t("GL_LINEAR", TextureMinFilter.Linear, TextureMagFilter.Linear),
            new glmode_t("GL_NEAREST_MIPMAP_NEAREST", TextureMinFilter.NearestMipmapNearest, TextureMagFilter.Nearest),
            new glmode_t("GL_LINEAR_MIPMAP_NEAREST", TextureMinFilter.LinearMipmapNearest, TextureMagFilter.Linear),
            new glmode_t("GL_NEAREST_MIPMAP_LINEAR", TextureMinFilter.NearestMipmapLinear, TextureMagFilter.Nearest),
            new glmode_t("GL_LINEAR_MIPMAP_LINEAR", TextureMinFilter.LinearMipmapLinear, TextureMagFilter.Linear)
        };

        private static readonly gltexture_t[] _glTextures = new gltexture_t[MAX_GLTEXTURES];

        private static readonly cachepic_t[] _MenuCachePics = new cachepic_t[MAX_CACHED_PICS];

        private static readonly Byte[] _MenuPlayerPixels = new Byte[4096];

        private static Int32[][] _ScrapAllocated;

        //[MAX_SCRAPS][BLOCK_WIDTH];
        private static Byte[][] _ScrapTexels;

        // [MAX_SCRAPS][BLOCK_WIDTH*BLOCK_HEIGHT*4];
        private static System.Boolean _ScrapDirty;

        // scrap_dirty
        private static Int32 _ScrapTexNum;

        // scrap_texnum
        private static Int32 _ScrapUploads;

        // scrap_uploads;
        private static Int32 _NumTextures;

        // numgltextures; // how many slots are used
        private static Int32 _Texels;

        // texels
        private static Int32 _PicTexels;

        // pic_texels
        private static Int32 _PicCount;

        private static CVar _glNoBind;

        // = {"gl_nobind", "0"};
        private static CVar _glMaxSize;

        // = {"gl_max_size", "1024"};
        private static CVar _glPicMip;

        private static glpic_t _Disc;

        // draw_disc
        private static glpic_t _BackTile;

        // draw_backtile
        private static glpic_t _ConBack;

        private static Int32 _CharTexture;

        // char_texture
        private static Int32 _TranslateTexture;

        // translate_texture
        private static Int32 _TextureExtensionNumber = 1;

        // texture_extension_number = 1;
        // currenttexture = -1		// to avoid unnecessary texture sets
        private static MTexTarget _OldTarget = MTexTarget.TEXTURE0_SGIS;

        // oldtarget
        private static Int32[] _CntTextures = new Int32[2] { -1, -1 };

        // cnttextures
        private static TextureMinFilter _MinFilter = TextureMinFilter.LinearMipmapNearest;

        // gl_filter_min = GL_LINEAR_MIPMAP_NEAREST
        private static TextureMagFilter _MagFilter = TextureMagFilter.Linear;

        // gl_lightmap_format = 4
        private static PixelInternalFormat _SolidFormat = PixelInternalFormat.Three;

        // gl_solid_format = 3
        private static PixelInternalFormat _AlphaFormat = PixelInternalFormat.Four;

        // menu_cachepics
        private static Int32 _MenuNumCachePics;

        // Draw_Init
        public static void Init()
        {
            for( var i = 0; i < _MenuCachePics.Length; i++ )
                _MenuCachePics[i] = new cachepic_t();

            if( _glNoBind == null )
            {
                _glNoBind = new CVar( "gl_nobind", "0" );
                _glMaxSize = new CVar( "gl_max_size", "1024" );
                _glPicMip = new CVar( "gl_picmip", "0" );
            }

            // 3dfx can only handle 256 wide textures
            var renderer = GL.GetString( StringName.Renderer );
            if( renderer.Contains( "3dfx" ) || renderer.Contains( "Glide" ) )
                CVar.Set( "gl_max_size", "256" );

            Command.Add( "gl_texturemode", TextureMode_f );
            Command.Add( "imagelist", Imagelist_f );

            // load the console background and the charset
            // by hand, because we need to write the version
            // string into the background before turning
            // it into a texture
            var offset = Wad.GetLumpNameOffset( "conchars" );
            Byte[] draw_chars = Wad.Data; // draw_chars
            for( var i = 0; i < 256 * 64; i++ )
            {
                if( draw_chars[offset + i] == 0 )
                    draw_chars[offset + i] = 255;	// proper transparent color
            }

            // now turn them into textures
            _CharTexture = LoadTexture( "charset", 128, 128, new ByteArraySegment( draw_chars, offset ), false, true );

            Byte[] buf = FileSystem.LoadFile( "gfx/conback.lmp" );
            if( buf == null )
                Utilities.Error( "Couldn't load gfx/conback.lmp" );

            dqpicheader_t cbHeader = Utilities.BytesToStructure<dqpicheader_t>( buf, 0 );
            Wad.SwapPic( cbHeader );

            // hack the version number directly into the pic
            var ver = String.Format( "(c# {0,7:F2}) {1,7:F2}", ( Single ) QDef.CSQUAKE_VERSION, ( Single ) QDef.VERSION );
            var offset2 = Marshal.SizeOf( typeof( dqpicheader_t ) ) + 320 * 186 + 320 - 11 - 8 * ver.Length;
            var y = ver.Length;
            for( var x = 0; x < y; x++ )
                CharToConback( ver[x], new ByteArraySegment( buf, offset2 + ( x << 3 ) ), new ByteArraySegment( draw_chars, offset ) );

            _ConBack = new glpic_t();
            _ConBack.width = cbHeader.width;
            _ConBack.height = cbHeader.height;
            var ncdataIndex = Marshal.SizeOf( typeof( dqpicheader_t ) ); // cb->data;

            SetTextureFilters( TextureMinFilter.Nearest, TextureMagFilter.Nearest );

            _ConBack.texnum = LoadTexture( "conback", _ConBack.width, _ConBack.height, new ByteArraySegment( buf, ncdataIndex ), false, false );
            _ConBack.width = Scr.vid.width;
            _ConBack.height = Scr.vid.height;

            // save a texture slot for translated picture
            _TranslateTexture = _TextureExtensionNumber++;

            // save slots for scraps
            _ScrapTexNum = _TextureExtensionNumber;
            _TextureExtensionNumber += MAX_SCRAPS;

            //
            // get the other pics we need
            //
            _Disc = PicFromWad( "disc" );
            _BackTile = PicFromWad( "backtile" );
        }

        public static void SetTextureFilters( TextureMinFilter min, TextureMagFilter mag )
        {
            GL.TexParameter( TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, ( Int32 ) min );
            GL.TexParameter( TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, ( Int32 ) mag );
        }

        /// <summary>
        /// gets texture_extension_number++
        /// </summary>
        public static Int32 GenerateTextureNumber()
        {
            return _TextureExtensionNumber++;
        }

        /// <summary>
        /// gets texture_extension_number++
        /// </summary>
        public static Int32 GenerateTextureNumberRange( Int32 count )
        {
            var result = _TextureExtensionNumber;
            _TextureExtensionNumber += count;
            return result;
        }

        // Draw_Pic(int x, int y, qpic_t* pic)
        public static void DrawPic( Int32 x, Int32 y, glpic_t pic )
        {
            if( _ScrapDirty )
                UploadScrap();

            GL.Color4( 1f, 1f, 1f, 1f );
            Bind( pic.texnum );
            GL.Begin( PrimitiveType.Quads );
            GL.TexCoord2( pic.sl, pic.tl );
            GL.Vertex2( x, y );
            GL.TexCoord2( pic.sh, pic.tl );
            GL.Vertex2( x + pic.width, y );
            GL.TexCoord2( pic.sh, pic.th );
            GL.Vertex2( x + pic.width, y + pic.height );
            GL.TexCoord2( pic.sl, pic.th );
            GL.Vertex2( x, y + pic.height );
            GL.End();
        }

        // Draw_BeginDisc
        //
        // Draws the little blue disc in the corner of the screen.
        // Call before beginning any disc IO.
        public static void BeginDisc()
        {
            if( _Disc != null )
            {
                GL.DrawBuffer( DrawBufferMode.Front );
                DrawPic( Scr.vid.width - 24, 0, _Disc );
                GL.DrawBuffer( DrawBufferMode.Back );
            }
        }

        // Draw_EndDisc
        // Erases the disc icon.
        // Call after completing any disc IO
        public static void EndDisc()
        {
            // nothing to do?
        }

        // Draw_TileClear
        //
        // This repeats a 64*64 tile graphic to fill the screen around a sized down
        // refresh window.
        public static void TileClear( Int32 x, Int32 y, Int32 w, Int32 h )
        {
            GL.Color3( 1.0f, 1.0f, 1.0f );
            Bind( _BackTile.texnum ); //GL_Bind (*(int *)draw_backtile->data);
            GL.Begin( PrimitiveType.Quads );
            GL.TexCoord2( x / 64.0f, y / 64.0f );
            GL.Vertex2( x, y );
            GL.TexCoord2( ( x + w ) / 64.0f, y / 64.0f );
            GL.Vertex2( x + w, y );
            GL.TexCoord2( ( x + w ) / 64.0f, ( y + h ) / 64.0f );
            GL.Vertex2( x + w, y + h );
            GL.TexCoord2( x / 64.0f, ( y + h ) / 64.0f );
            GL.Vertex2( x, y + h );
            GL.End();
        }

        //qpic_t *Draw_PicFromWad (char *name);
        public static glpic_t PicFromWad( String name )
        {
            var offset = Wad.GetLumpNameOffset( name );
            IntPtr ptr = new IntPtr( Wad.DataPointer.ToInt64() + offset );
            dqpicheader_t header = (dqpicheader_t)Marshal.PtrToStructure( ptr, typeof( dqpicheader_t ) );
            glpic_t gl = new glpic_t(); // (glpic_t)Marshal.PtrToStructure(ptr, typeof(glpic_t));
            gl.width = header.width;
            gl.height = header.height;
            offset += Marshal.SizeOf( typeof( dqpicheader_t ) );

            // load little ones into the scrap
            if( gl.width < 64 && gl.height < 64 )
            {
                Int32 x, y;
                var texnum = AllocScrapBlock( gl.width, gl.height, out x, out y );
                _ScrapDirty = true;
                var k = 0;
                for( var i = 0; i < gl.height; i++ )
                    for( var j = 0; j < gl.width; j++, k++ )
                        _ScrapTexels[texnum][( y + i ) * BLOCK_WIDTH + x + j] = Wad.Data[offset + k];// p->data[k];
                texnum += _ScrapTexNum;
                gl.texnum = texnum;
                gl.sl = ( Single ) ( ( x + 0.01 ) / ( Single ) BLOCK_WIDTH );
                gl.sh = ( Single ) ( ( x + gl.width - 0.01 ) / ( Single ) BLOCK_WIDTH );
                gl.tl = ( Single ) ( ( y + 0.01 ) / ( Single ) BLOCK_WIDTH );
                gl.th = ( Single ) ( ( y + gl.height - 0.01 ) / ( Single ) BLOCK_WIDTH );

                _PicCount++;
                _PicTexels += gl.width * gl.height;
            }
            else
            {
                gl.texnum = LoadTexture( gl, new ByteArraySegment( Wad.Data, offset ) );
            }
            return gl;
        }

        // GL_Bind (int texnum)
        public static void Bind( Int32 texnum )
        {
            //if (_glNoBind.Value != 0)
            //    texnum = _CharTexture;
            if( CurrentTexture == texnum )
                return;
            CurrentTexture = texnum;
            GL.BindTexture( TextureTarget.Texture2D, texnum );
        }

        // Draw_FadeScreen
        public static void FadeScreen()
        {
            GL.Enable( EnableCap.Blend );
            GL.Disable( EnableCap.Texture2D );
            GL.Color4( 0, 0, 0, 0.8f );
            GL.Begin( PrimitiveType.Quads );

            GL.Vertex2( 0f, 0f );
            GL.Vertex2( Scr.vid.width, 0f );
            GL.Vertex2( ( Single ) Scr.vid.width, ( Single ) Scr.vid.height );
            GL.Vertex2( 0f, Scr.vid.height );

            GL.End();
            GL.Color4( 1f, 1f, 1f, 1f );
            GL.Enable( EnableCap.Texture2D );
            GL.Disable( EnableCap.Blend );

            sbar.Changed();
        }

        /// <summary>
        /// GL_LoadTexture
        /// </summary>
        public static Int32 LoadTexture( String identifier, Int32 width, Int32 height, ByteArraySegment data, System.Boolean mipmap, System.Boolean alpha, String owner = "" )
        {
            // see if the texture is allready present
            if( !String.IsNullOrEmpty( identifier ) )
            {
                for( var i = 0; i < _NumTextures; i++ )
                {
                    gltexture_t glt = _glTextures[i];
                    if( glt.identifier == identifier && glt.owner == owner )
                    {
                        if( width != glt.width || height != glt.height )
                            Utilities.Error( "GL_LoadTexture: cache mismatch!" );
                        return glt.texnum;
                    }
                }
            }
            if( _NumTextures == _glTextures.Length )
                Utilities.Error( "GL_LoadTexture: no more texture slots available!" );

            gltexture_t tex = new gltexture_t();
            _glTextures[_NumTextures] = tex;
            _NumTextures++;

            tex.identifier = identifier;
            tex.owner = owner;
            tex.texnum = _TextureExtensionNumber;
            tex.width = width;
            tex.height = height;
            tex.mipmap = mipmap;

            Bind( tex.texnum );

            Upload8( data, width, height, mipmap, alpha );

            _TextureExtensionNumber++;

            return tex.texnum;
        }


        /// <summary>
        /// GL_LoadTexture32
        /// </summary>
        public static Int32 LoadTexture( String identifier, Int32 width, Int32 height, System.Drawing.Bitmap bitmap, System.Boolean mipmap, System.Boolean alpha, String owner = "" )
        {
            // see if the texture is allready present
            if ( !String.IsNullOrEmpty( identifier ) )
            {
                for ( var i = 0; i < _NumTextures; i++ )
                {
                    gltexture_t glt = _glTextures[i];
                    if ( glt.identifier == identifier && glt.owner == owner )
                    {
                        if ( width != glt.width || height != glt.height )
                            Utilities.Error( "GL_LoadTexture: cache mismatch!" );
                        return glt.texnum;
                    }
                }
            }
            if ( _NumTextures == _glTextures.Length )
                Utilities.Error( "GL_LoadTexture: no more texture slots available!" );

            gltexture_t tex = new gltexture_t( );
            _glTextures[_NumTextures] = tex;
            _NumTextures++;

            tex.identifier = identifier;
            tex.owner = owner;
            tex.texnum = _TextureExtensionNumber;
            tex.width = width;
            tex.height = height;
            tex.mipmap = mipmap;

            Bind( tex.texnum );

            UploadBitmap( bitmap, width, height, mipmap, alpha );

            _TextureExtensionNumber++;

            return tex.texnum;
        }

        // Draw_Character
        //
        // Draws one 8*8 graphics character with 0 being transparent.
        // It can be clipped to the top of the screen to allow the console to be
        // smoothly scrolled off.
        public static void DrawCharacter( Int32 x, Int32 y, Int32 num )
        {
            if( num == 32 )
                return;		// space

            num &= 255;

            if( y <= -8 )
                return;			// totally off screen

            var row = num >> 4;
            var col = num & 15;

            var frow = row * 0.0625f;
            var fcol = col * 0.0625f;
            var size = 0.0625f;

            Bind( _CharTexture );

            GL.Begin( PrimitiveType.Quads );
            GL.TexCoord2( fcol, frow );
            GL.Vertex2( x, y );
            GL.TexCoord2( fcol + size, frow );
            GL.Vertex2( x + 8, y );
            GL.TexCoord2( fcol + size, frow + size );
            GL.Vertex2( x + 8, y + 8 );
            GL.TexCoord2( fcol, frow + size );
            GL.Vertex2( x, y + 8 );
            GL.End();
        }

        // Draw_String
        public static void DrawString( Int32 x, Int32 y, String str )
        {
            for( var i = 0; i < str.Length; i++, x += 8 )
                DrawCharacter( x, y, str[i] );
        }

        // Draw_CachePic
        public static glpic_t CachePic( String path )
        {
            for( var i = 0; i < _MenuNumCachePics; i++ )
            {
                cachepic_t p = _MenuCachePics[i];
                if( p.name == path )// !strcmp(path, pic->name))
                    return p.pic;
            }

            if( _MenuNumCachePics == MAX_CACHED_PICS )
                Utilities.Error( "menu_numcachepics == MAX_CACHED_PICS" );

            cachepic_t pic = _MenuCachePics[_MenuNumCachePics];
            _MenuNumCachePics++;
            pic.name = path;

            //
            // load the pic from disk
            //
            Byte[] data = FileSystem.LoadFile( path );
            if( data == null )
                Utilities.Error( "Draw_CachePic: failed to load {0}", path );
            dqpicheader_t header = Utilities.BytesToStructure<dqpicheader_t>( data, 0 );
            Wad.SwapPic( header );

            var headerSize = Marshal.SizeOf( typeof( dqpicheader_t ) );

            // HACK HACK HACK --- we need to keep the bytes for
            // the translatable player picture just for the menu
            // configuration dialog
            if( path == "gfx/menuplyr.lmp" )
            {
                Buffer.BlockCopy( data, headerSize, _MenuPlayerPixels, 0, header.width * header.height );
                //memcpy (menuplyr_pixels, dat->data, dat->width*dat->height);
            }

            glpic_t gl = new glpic_t();
            gl.width = header.width;
            gl.height = header.height;

            //gl = (glpic_t *)pic->pic.data;
            gl.texnum = LoadTexture( gl, new ByteArraySegment( data, headerSize ) );
            gl.sl = 0;
            gl.sh = 1;
            gl.tl = 0;
            gl.th = 1;
            pic.pic = gl;

            return gl;
        }

        // Draw_Fill
        //
        // Fills a box of pixels with a single color
        public static void Fill( Int32 x, Int32 y, Int32 w, Int32 h, Int32 c )
        {
            GL.Disable( EnableCap.Texture2D );

            Byte[] pal = host.BasePal;

            GL.Color3( pal[c * 3] / 255.0f, pal[c * 3 + 1] / 255.0f, pal[c * 3 + 2] / 255.0f );
            GL.Begin( PrimitiveType.Quads );
            GL.Vertex2( x, y );
            GL.Vertex2( x + w, y );
            GL.Vertex2( x + w, y + h );
            GL.Vertex2( x, y + h );
            GL.End();
            GL.Color3( 1f, 1f, 1f );
            GL.Enable( EnableCap.Texture2D );
        }

        // Draw_TransPic
        public static void DrawTransPic( Int32 x, Int32 y, glpic_t pic )
        {
            if( x < 0 || ( UInt32 ) ( x + pic.width ) > Scr.vid.width ||
                y < 0 || ( UInt32 ) ( y + pic.height ) > Scr.vid.height )
            {
                Utilities.Error( "Draw_TransPic: bad coordinates" );
            }

            DrawPic( x, y, pic );
        }

        /// <summary>
        /// Draw_TransPicTranslate
        /// Only used for the player color selection menu
        /// </summary>
        public static void TransPicTranslate( Int32 x, Int32 y, glpic_t pic, Byte[] translation )
        {
            Bind( _TranslateTexture );

            var c = pic.width * pic.height;
            var destOffset = 0;
            UInt32[] trans = new UInt32[64 * 64];

            for( var v = 0; v < 64; v++, destOffset += 64 )
            {
                var srcOffset = ( ( v * pic.height ) >> 6 ) * pic.width;
                for( var u = 0; u < 64; u++ )
                {
                    UInt32 p = _MenuPlayerPixels[srcOffset + ( ( u * pic.width ) >> 6 )];
                    if( p == 255 )
                        trans[destOffset + u] = p;
                    else
                        trans[destOffset + u] = vid.Table8to24[translation[p]];
                }
            }

            GCHandle handle = GCHandle.Alloc( trans, GCHandleType.Pinned );
            try
            {
                GL.TexImage2D( TextureTarget.Texture2D, 0, Drawer.AlphaFormat, 64, 64, 0,
                    PixelFormat.Rgba, PixelType.UnsignedByte, handle.AddrOfPinnedObject() );
            }
            finally
            {
                handle.Free();
            }

            SetTextureFilters( TextureMinFilter.Linear, TextureMagFilter.Linear );

            GL.Color3( 1f, 1, 1 );
            GL.Begin( PrimitiveType.Quads );
            GL.TexCoord2( 0f, 0 );
            GL.Vertex2( ( Single ) x, y );
            GL.TexCoord2( 1f, 0 );
            GL.Vertex2( ( Single ) x + pic.width, y );
            GL.TexCoord2( 1f, 1 );
            GL.Vertex2( ( Single ) x + pic.width, y + pic.height );
            GL.TexCoord2( 0f, 1 );
            GL.Vertex2( ( Single ) x, y + pic.height );
            GL.End();
        }

        // Draw_ConsoleBackground
        public static void DrawConsoleBackground( Int32 lines )
        {
            var y = ( Scr.vid.height * 3 ) >> 2;

            if( lines > y )
                DrawPic( 0, lines - Scr.vid.height, _ConBack );
            else
                DrawAlphaPic( 0, lines - Scr.vid.height, _ConBack, ( Single ) ( 1.2 * lines ) / y );
        }

        // Draw_AlphaPic
        public static void DrawAlphaPic( Int32 x, Int32 y, glpic_t pic, Single alpha )
        {
            if( _ScrapDirty )
                UploadScrap();

            GL.Disable( EnableCap.AlphaTest );
            GL.Enable( EnableCap.Blend );
            GL.Color4( 1f, 1f, 1f, alpha );
            Bind( pic.texnum );
            GL.Begin( PrimitiveType.Quads );
            GL.TexCoord2( pic.sl, pic.tl );
            GL.Vertex2( x, y );
            GL.TexCoord2( pic.sh, pic.tl );
            GL.Vertex2( x + pic.width, y );
            GL.TexCoord2( pic.sh, pic.th );
            GL.Vertex2( x + pic.width, y + pic.height );
            GL.TexCoord2( pic.sl, pic.th );
            GL.Vertex2( x, y + pic.height );
            GL.End();
            GL.Color4( 1f, 1f, 1f, 1f );
            GL.Enable( EnableCap.AlphaTest );
            GL.Disable( EnableCap.Blend );
        }

        /// <summary>
        /// GL_SelectTexture
        /// </summary>
        public static void SelectTexture( MTexTarget target )
        {
            if( !vid.glMTexable )
                return;

            switch( target )
            {
                case MTexTarget.TEXTURE0_SGIS:
                    GL.Arb.ActiveTexture( TextureUnit.Texture0 );
                    break;

                case MTexTarget.TEXTURE1_SGIS:
                    GL.Arb.ActiveTexture( TextureUnit.Texture1 );
                    break;

                default:
                    Utilities.Error( "GL_SelectTexture: Unknown target\n" );
                    break;
            }

            if( target == _OldTarget )
                return;

            _CntTextures[_OldTarget - MTexTarget.TEXTURE0_SGIS] = Drawer.CurrentTexture;
            Drawer.CurrentTexture = _CntTextures[target - MTexTarget.TEXTURE0_SGIS];
            _OldTarget = target;
        }

        /// <summary>
        /// Draw_TextureMode_f
        /// </summary>
        private static void TextureMode_f()
        {
            Int32 i;
            if( Command.Argc == 1 )
            {
                for( i = 0; i < 6; i++ )
                    if( _MinFilter == _Modes[i].minimize )
                    {
                        Con.Print( "{0}\n", _Modes[i].name );
                        return;
                    }
                Con.Print( "current filter is unknown???\n" );
                return;
            }

            for( i = 0; i < _Modes.Length; i++ )
            {
                if( Common.SameText( _Modes[i].name, Command.Argv( 1 ) ) )
                    break;
            }
            if( i == _Modes.Length )
            {
                Con.Print( "bad filter name!\n" );
                return;
            }

            _MinFilter = _Modes[i].minimize;
            _MagFilter = _Modes[i].maximize;

            // change all the existing mipmap texture objects
            for( i = 0; i < _NumTextures; i++ )
            {
                gltexture_t glt = _glTextures[i];
                if( glt.mipmap )
                {
                    Bind( glt.texnum );
                    SetTextureFilters( _MinFilter, _MagFilter );
                }
            }
        }

        private static void Imagelist_f()
        {
            Int16 textureCount = 0;

            foreach (gltexture_t glTexture in _glTextures)
            {
                if (glTexture != null)
                {
                    Con.Print( "{0} x {1}   {2}:{3}\n", glTexture.width, glTexture.height,
                    glTexture.owner, glTexture.identifier );
                    textureCount++;
                }
            }

            Con.Print( "{0} textures currently loaded.\n", textureCount );
        }

        /// <summary>
        /// GL_LoadPicTexture
        /// </summary>
        private static Int32 LoadTexture( glpic_t pic, ByteArraySegment data )
        {
            return LoadTexture( String.Empty, pic.width, pic.height, data, false, true );
        }

        private static void CharToConback( Int32 num, ByteArraySegment dest, ByteArraySegment drawChars )
        {
            var row = num >> 4;
            var col = num & 15;
            var destOffset = dest.StartIndex;
            var srcOffset = drawChars.StartIndex + ( row << 10 ) + ( col << 3 );
            //source = draw_chars + (row<<10) + (col<<3);
            var drawline = 8;

            while( drawline-- > 0 )
            {
                for( var x = 0; x < 8; x++ )
                    if( drawChars.Data[srcOffset + x] != 255 )
                        dest.Data[destOffset + x] = ( Byte ) ( 0x60 + drawChars.Data[srcOffset + x] ); // source[x];
                srcOffset += 128; // source += 128;
                destOffset += 320; // dest += 320;
            }
        }

        /// <summary>
        /// GL_Upload8
        /// </summary>
        private static void Upload8( ByteArraySegment data, Int32 width, Int32 height, System.Boolean mipmap, System.Boolean alpha )
        {
            var s = width * height;
            UInt32[] trans = new UInt32[s];
            UInt32[] table = vid.Table8to24;
            Byte[] data1 = data.Data;
            var offset = data.StartIndex;

            // if there are no transparent pixels, make it a 3 component
            // texture even if it was specified as otherwise
            if( alpha )
            {
                var noalpha = true;
                for( var i = 0; i < s; i++, offset++ )
                {
                    var p = data1[offset];
                    if( p == 255 )
                        noalpha = false;
                    trans[i] = table[p];
                }

                if( alpha && noalpha )
                    alpha = false;
            }
            else
            {
                if( ( s & 3 ) != 0 )
                    Utilities.Error( "GL_Upload8: s&3" );

                for( var i = 0; i < s; i += 4, offset += 4 )
                {
                    trans[i] = table[data1[offset]];
                    trans[i + 1] = table[data1[offset + 1]];
                    trans[i + 2] = table[data1[offset + 2]];
                    trans[i + 3] = table[data1[offset + 3]];
                }
            }

            Upload32( trans, width, height, mipmap, alpha );
        }

        // GL_Upload32
        private static void Upload32( UInt32[] data, Int32 width, Int32 height, System.Boolean mipmap, System.Boolean alpha )
        {
            Int32 scaled_width, scaled_height;

            for( scaled_width = 1; scaled_width < width; scaled_width <<= 1 )
                ;
            for( scaled_height = 1; scaled_height < height; scaled_height <<= 1 )
                ;

            scaled_width >>= ( Int32 ) _glPicMip.Value;
            scaled_height >>= ( Int32 ) _glPicMip.Value;

            if( scaled_width > _glMaxSize.Value )
                scaled_width = ( Int32 ) _glMaxSize.Value;
            if( scaled_height > _glMaxSize.Value )
                scaled_height = ( Int32 ) _glMaxSize.Value;

            PixelInternalFormat samples = alpha ? _AlphaFormat : _SolidFormat;
            UInt32[] scaled;

            _Texels += scaled_width * scaled_height;

            if( scaled_width == width && scaled_height == height )
            {
                if( !mipmap )
                {
                    GCHandle h2 = GCHandle.Alloc( data, GCHandleType.Pinned );
                    try
                    {
                        GL.TexImage2D( TextureTarget.Texture2D, 0, samples, scaled_width, scaled_height, 0,
                            PixelFormat.Rgba, PixelType.UnsignedByte, h2.AddrOfPinnedObject() );
                    }
                    finally
                    {
                        h2.Free();
                    }
                    goto Done;
                }
                scaled = new UInt32[scaled_width * scaled_height]; // uint[1024 * 512];
                data.CopyTo( scaled, 0 );
            }
            else
                ResampleTexture( data, width, height, out scaled, scaled_width, scaled_height );

            GCHandle h = GCHandle.Alloc( scaled, GCHandleType.Pinned );
            try
            {
                IntPtr ptr = h.AddrOfPinnedObject();
                GL.TexImage2D( TextureTarget.Texture2D, 0, samples, scaled_width, scaled_height, 0,
                    PixelFormat.Rgba, PixelType.UnsignedByte, ptr );
                ErrorCode err = GL.GetError(); // debug
                if( mipmap )
                {
                    var miplevel = 0;
                    while( scaled_width > 1 || scaled_height > 1 )
                    {
                        MipMap( scaled, scaled_width, scaled_height );
                        scaled_width >>= 1;
                        scaled_height >>= 1;
                        if( scaled_width < 1 )
                            scaled_width = 1;
                        if( scaled_height < 1 )
                            scaled_height = 1;
                        miplevel++;
                        GL.TexImage2D( TextureTarget.Texture2D, miplevel, samples, scaled_width, scaled_height, 0,
                            PixelFormat.Rgba, PixelType.UnsignedByte, ptr );
                    }
                }
            }
            finally
            {
                h.Free();
            }

Done:
            ;

            if( mipmap )
                SetTextureFilters( _MinFilter, _MagFilter );
            else
                SetTextureFilters( (TextureMinFilter)_MagFilter, _MagFilter );
        }

        // GL_UploadBitmap
        private static void UploadBitmap( System.Drawing.Bitmap bitmap, Int32 width, Int32 height, System.Boolean mipmap, System.Boolean alpha )
        {
            //bitmap.Save( "F:\\Test.png" );

            //bitmap.Save( ms, System.Drawing.Imaging.ImageFormat.MemoryBmp );

            var data = bitmap.LockBits( new System.Drawing.Rectangle( 0, 0, bitmap.Width, bitmap.Height ),
       System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb );

            GL.TexImage2D( TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, data.Width, data.Height, 0,
                PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0 );

            bitmap.UnlockBits( data );

            GL.GenerateMipmap( GenerateMipmapTarget.Texture2D );
        //int scaled_width, scaled_height;

        //for ( scaled_width = 1; scaled_width < width; scaled_width <<= 1 )
        //    ;
        //for ( scaled_height = 1; scaled_height < height; scaled_height <<= 1 )
        //    ;

        //scaled_width >>= ( int ) _glPicMip.Value;
        //scaled_height >>= ( int ) _glPicMip.Value;

        //if ( scaled_width > _glMaxSize.Value )
        //    scaled_width = ( int ) _glMaxSize.Value;
        //if ( scaled_height > _glMaxSize.Value )
        //    scaled_height = ( int ) _glMaxSize.Value;

        //PixelInternalFormat samples = alpha ? _AlphaFormat : _SolidFormat;
        //uint[] scaled;

        //_Texels += scaled_width * scaled_height;

        //if ( scaled_width == width && scaled_height == height )
        //{
        //    if ( !mipmap )
        //    {
        //        GCHandle h2 = GCHandle.Alloc( data, GCHandleType.Pinned );
        //        try
        //        {
        //            GL.TexImage2D( TextureTarget.Texture2D, 0, samples, scaled_width, scaled_height, 0,
        //                PixelFormat.Rgba, PixelType.UnsignedByte, h2.AddrOfPinnedObject( ) );
        //        }
        //        finally
        //        {
        //            h2.Free( );
        //        }
        //        goto Done;
        //    }
        //    scaled = new uint[scaled_width * scaled_height]; // uint[1024 * 512];
        //    data.CopyTo( scaled, 0 );
        //}
        //else
        //    ResampleTexture( data, width, height, out scaled, scaled_width, scaled_height );

        //GCHandle h = GCHandle.Alloc( scaled, GCHandleType.Pinned );
        //try
        //{
        //    IntPtr ptr = h.AddrOfPinnedObject( );
        //    GL.TexImage2D( TextureTarget.Texture2D, 0, samples, scaled_width, scaled_height, 0,
        //        PixelFormat.Rgba, PixelType.UnsignedByte, ptr );
        //    ErrorCode err = GL.GetError( ); // debug
        //    if ( mipmap )
        //    {
        //        int miplevel = 0;
        //        while ( scaled_width > 1 || scaled_height > 1 )
        //        {
        //            MipMap( scaled, scaled_width, scaled_height );
        //            scaled_width >>= 1;
        //            scaled_height >>= 1;
        //            if ( scaled_width < 1 )
        //                scaled_width = 1;
        //            if ( scaled_height < 1 )
        //                scaled_height = 1;
        //            miplevel++;
        //            GL.TexImage2D( TextureTarget.Texture2D, miplevel, samples, scaled_width, scaled_height, 0,
        //                PixelFormat.Rgba, PixelType.UnsignedByte, ptr );
        //        }
        //    }
        //}
        //finally
        //{
        //    h.Free( );
        //}

        Done:
            ;

            if ( mipmap )
                SetTextureFilters( _MinFilter, _MagFilter );
            else
                SetTextureFilters( ( TextureMinFilter ) _MagFilter, _MagFilter );
        }

        // GL_ResampleTexture
        private static void ResampleTexture( UInt32[] src, Int32 srcWidth, Int32 srcHeight, out UInt32[] dest, Int32 destWidth, Int32 destHeight )
        {
            dest = new UInt32[destWidth * destHeight];
            var fracstep = srcWidth * 0x10000 / destWidth;
            var destOffset = 0;
            for( var i = 0; i < destHeight; i++ )
            {
                var srcOffset = srcWidth * ( i * srcHeight / destHeight );
                var frac = fracstep >> 1;
                for( var j = 0; j < destWidth; j += 4 )
                {
                    dest[destOffset + j] = src[srcOffset + ( frac >> 16 )];
                    frac += fracstep;
                    dest[destOffset + j + 1] = src[srcOffset + ( frac >> 16 )];
                    frac += fracstep;
                    dest[destOffset + j + 2] = src[srcOffset + ( frac >> 16 )];
                    frac += fracstep;
                    dest[destOffset + j + 3] = src[srcOffset + ( frac >> 16 )];
                    frac += fracstep;
                }
                destOffset += destWidth;
            }
        }

        // GL_MipMap
        //
        // Operates in place, quartering the size of the texture
        private static void MipMap( UInt32[] src, Int32 width, Int32 height )
        {
            Union4b p1 = Union4b.Empty, p2 = Union4b.Empty, p3 = Union4b.Empty, p4 = Union4b.Empty;

            width >>= 1;
            height >>= 1;

            UInt32[] dest = src;
            var srcOffset = 0;
            var destOffset = 0;
            for( var i = 0; i < height; i++ )
            {
                for( var j = 0; j < width; j++ )
                {
                    p1.ui0 = src[srcOffset];
                    var offset = srcOffset + 1;
                    p2.ui0 = offset < src.Length ? src[offset] : p1.ui0;
                    offset = srcOffset + ( width << 1 );
                    p3.ui0 = offset < src.Length ? src[offset] : p1.ui0;
                    offset = srcOffset + ( width << 1 ) + 1;
                    p4.ui0 = offset < src.Length ? src[offset] : p1.ui0;

                    p1.b0 = ( Byte ) ( ( p1.b0 + p2.b0 + p3.b0 + p4.b0 ) >> 2 );
                    p1.b1 = ( Byte ) ( ( p1.b1 + p2.b1 + p3.b1 + p4.b1 ) >> 2 );
                    p1.b2 = ( Byte ) ( ( p1.b2 + p2.b2 + p3.b2 + p4.b2 ) >> 2 );
                    p1.b3 = ( Byte ) ( ( p1.b3 + p2.b3 + p3.b3 + p4.b3 ) >> 2 );

                    dest[destOffset] = p1.ui0;
                    destOffset++;
                    srcOffset += 2;
                }
                srcOffset += width << 1;
            }
        }

        // Scrap_AllocBlock
        // returns a texture number and the position inside it
        private static Int32 AllocScrapBlock( Int32 w, Int32 h, out Int32 x, out Int32 y )
        {
            x = -1;
            y = -1;
            for( var texnum = 0; texnum < MAX_SCRAPS; texnum++ )
            {
                var best = BLOCK_HEIGHT;

                for( var i = 0; i < BLOCK_WIDTH - w; i++ )
                {
                    Int32 best2 = 0, j;

                    for( j = 0; j < w; j++ )
                    {
                        if( _ScrapAllocated[texnum][i + j] >= best )
                            break;
                        if( _ScrapAllocated[texnum][i + j] > best2 )
                            best2 = _ScrapAllocated[texnum][i + j];
                    }
                    if( j == w )
                    {
                        // this is a valid spot
                        x = i;
                        y = best = best2;
                    }
                }

                if( best + h > BLOCK_HEIGHT )
                    continue;

                for( var i = 0; i < w; i++ )
                    _ScrapAllocated[texnum][x + i] = best + h;

                return texnum;
            }

            Utilities.Error( "Scrap_AllocBlock: full" );
            return -1;
        }

        private static void UploadScrap()
        {
            _ScrapUploads++;
            for( var i = 0; i < MAX_SCRAPS; i++ )
            {
                Bind( _ScrapTexNum + i );
                Upload8( new ByteArraySegment( _ScrapTexels[i] ), BLOCK_WIDTH, BLOCK_HEIGHT, false, true );
            }
            _ScrapDirty = false;
        }


        // pic_count

        // = {"gl_picmip", "0"};

        // conback

        // gl_filter_max = GL_LINEAR

        // gl_alpha_format = 4

        // menu_numcachepics
        // menuplyr_pixels
        static Drawer()
        {
            _ScrapAllocated = new Int32[MAX_SCRAPS][]; //[MAX_SCRAPS][BLOCK_WIDTH];
            for( var i = 0; i < _ScrapAllocated.GetLength( 0 ); i++ )
            {
                _ScrapAllocated[i] = new Int32[BLOCK_WIDTH];
            }
            _ScrapTexels = new Byte[MAX_SCRAPS][]; // [MAX_SCRAPS][BLOCK_WIDTH*BLOCK_HEIGHT*4];
            for( var i = 0; i < _ScrapTexels.GetLength( 0 ); i++ )
            {
                _ScrapTexels[i] = new Byte[BLOCK_WIDTH * BLOCK_HEIGHT * 4];
            }
        }
    }
}
