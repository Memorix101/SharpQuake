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
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using SharpQuake.Framework;
using SharpQuake.Framework.IO;
using SharpQuake.Renderer.Textures;

// gl_draw.c

namespace SharpQuake
{
    /// <summary>
    /// Draw_functions, GL_functions
    /// </summary>
    public class Drawer
    {
        public Single glMaxSize
        {
            get
            {
                return Host.Cvars.glMaxSize.Get<Int32>( );
            }
        }

        public Int32 CurrentTexture = -1;

        public String LightMapFormat = "GL_RGBA";

        private readonly GLTexture_t[] _glTextures = new GLTexture_t[DrawDef.MAX_GLTEXTURES];

        private readonly Dictionary<String, BasePicture> _MenuCachePics = new Dictionary<String, BasePicture>( );

        public Byte[] _MenuPlayerPixels = new Byte[4096];
        public Int32 _MenuPlayerPixelWidth;
        public Int32 _MenuPlayerPixelHeight;

        public BasePicture Disc
        {
            get;
            private set;
        }

        public BasePicture ConsoleBackground
        {
            get;
            private set;
        }

        public BasePicture BackgroundTile
        {
            get;
            private set;
        }

        private Renderer.Font CharSetFont
        {
            get;
            set;
        }

        private BaseTexture TranslateTexture
        {
            get;
            set;
        }

        // texture_extension_number = 1;
        // currenttexture = -1		// to avoid unnecessary texture sets
        private MTexTarget _OldTarget = MTexTarget.TEXTURE0_SGIS;

        // oldtarget
        private Int32[] _CntTextures = new Int32[2] { -1, -1 };

        // cnttextures
        private String CurrentFilter = "GL_LINEAR_MIPMAP_NEAREST";

        // menu_cachepics
        private Int32 _MenuNumCachePics;

        public System.Boolean IsInitialised
        {
            get;
            private set;
        }

        // CHANGE
        private Host Host
        {
            get;
            set;
        }

        // Draw_Init
        public void Initialise( )
        {
            if ( Host.Cvars.glNoBind == null )
            {
                Host.Cvars.glNoBind = Host.CVars.Add( "gl_nobind", false );
                Host.Cvars.glMaxSize = Host.CVars.Add( "gl_max_size", 8192 );
                Host.Cvars.glPicMip = Host.CVars.Add( "gl_picmip", 0f );
            }

            // 3dfx can only handle 256 wide textures
            var renderer = Host.Video.Device.Desc.Renderer;

            if ( renderer.Contains( "3dfx" ) || renderer.Contains( "Glide" ) )
                Host.CVars.Set( "gl_max_size", 256 );

            Host.Commands.Add( "gl_texturemode", TextureMode_f );
            Host.Commands.Add( "imagelist", Imagelist_f );

            // load the console background and the charset
            // by hand, because we need to write the version
            // string into the background before turning
            // it into a texture
            var offset = Host.GfxWad.GetLumpNameOffset( "conchars" );
            var draw_chars = Host.GfxWad.Data; // draw_chars
            for ( var i = 0; i < 256 * 64; i++ )
            {
                if ( draw_chars[offset + i] == 0 )
                    draw_chars[offset + i] = 255;	// proper transparent color
            }

            // Temporarily set here
            BaseTexture.PicMip = Host.Cvars.glPicMip.Get<Single>( );
            BaseTexture.MaxSize = glMaxSize;

            CharSetFont = new Renderer.Font( Host.Video.Device, "charset" );
            CharSetFont.Initialise( new ByteArraySegment( draw_chars, offset ) );

            var buf = FileSystem.LoadFile( "gfx/conback.lmp" );
            if ( buf == null )
                Utilities.Error( "Couldn't load gfx/conback.lmp" );

            var cbHeader = Utilities.BytesToStructure<WadPicHeader>( buf, 0 );
            EndianHelper.SwapPic( cbHeader );

            // hack the version number directly into the pic
            var ver = String.Format( $"(c# {QDef.CSQUAKE_VERSION,7:F2}) {QDef.VERSION,7:F2}" );
            var offset2 = Marshal.SizeOf( typeof( WadPicHeader ) ) + 320 * 186 + 320 - 11 - 8 * ver.Length;
            var y = ver.Length;
            for ( var x = 0; x < y; x++ )
                CharToConback( ver[x], new ByteArraySegment( buf, offset2 + ( x << 3 ) ), new ByteArraySegment( draw_chars, offset ) );

            var ncdataIndex = Marshal.SizeOf( typeof( WadPicHeader ) ); // cb->data;

            ConsoleBackground = BasePicture.FromBuffer( Host.Video.Device, new ByteArraySegment( buf, ncdataIndex ), ( Int32 ) cbHeader.width, ( Int32 ) cbHeader.height, "conback", "GL_LINEAR" );
            
            TranslateTexture = BaseTexture.FromDynamicBuffer( Host.Video.Device, "_TranslateTexture", new ByteArraySegment( _MenuPlayerPixels ), _MenuPlayerPixelWidth, _MenuPlayerPixelHeight, false, true, "GL_LINEAR" );

            //
            // get the other pics we need
            //
            Disc = BasePicture.FromWad( Host.Video.Device, Host.GfxWad, "disc", "GL_NEAREST" );

            BackgroundTile = BasePicture.FromWad( Host.Video.Device, Host.GfxWad, "backtile", "GL_NEAREST" );

            IsInitialised = true;
        }

        // Draw_BeginDisc
        //
        // Draws the little blue disc in the corner of the screen.
        // Call before beginning any disc IO.
        public void BeginDisc( )
        {
            if ( Disc != null )
            {
                Host.Video.Device.SetDrawBuffer( true );
                Host.Video.Device.Graphics.DrawPicture( Disc, Host.Screen.vid.width - 24, 0 );
                Host.Video.Device.SetDrawBuffer( false );
            }
        }

        // Draw_EndDisc
        // Erases the disc iHost.Console.
        // Call after completing any disc IO
        public void EndDisc( )
        {
            // nothing to do?
        }

        // Draw_TileClear
        //
        // This repeats a 64*64 tile graphic to fill the screen around a sized down
        // refresh window.
        public void TileClear( Int32 x, Int32 y, Int32 w, Int32 h )
        {
            BackgroundTile.Source = new RectangleF( x / 64.0f, y / 64.0f, w / 64f, h / 64f );

            Host.Video.Device.Graphics.DrawPicture( BackgroundTile, x, y, w, h );
        }
        
        // Draw_FadeScreen
        public void FadeScreen( )
        {
            Host.Video.Device.Graphics.FadeScreen( );
            Host.Hud.Changed( );
        }

        // Draw_Character
        //
        // Draws one 8*8 graphics character with 0 being transparent.
        // It can be clipped to the top of the screen to allow the console to be
        // smoothly scrolled off.
        // Vertex color modification has no effect currently
        public void DrawCharacter( Int32 x, Int32 y, Int32 num, System.Drawing.Color? color = null )
        {
            CharSetFont.DrawCharacter( x, y, num, color );
        }

        // Draw_String
        public void DrawString( Int32 x, Int32 y, String str, System.Drawing.Color? color = null )
        {
            CharSetFont.Draw( x, y, str, color );
        }

        // Draw_CachePic
        public BasePicture CachePic( String path, String filter = "GL_LINEAR_MIPMAP_NEAREST", System.Boolean ignoreAtlas = false )
        {
            if ( _MenuCachePics.ContainsKey( path ) )
                return _MenuCachePics[path];

            if ( _MenuNumCachePics == DrawDef.MAX_CACHED_PICS )
                Utilities.Error( "menu_numcachepics == MAX_CACHED_PICS" );

            var picture = BasePicture.FromFile( Host.Video.Device, path, filter, ignoreAtlas );

            if ( picture != null )
            {
                _MenuNumCachePics++;

                _MenuCachePics.Add( path, picture );
            }

            return picture;
        }

        /// <summary>
        /// Draw_TransPicTranslate
        /// Only used for the player color selection menu
        /// </summary>
        public void TransPicTranslate( Int32 x, Int32 y, BasePicture pic, Byte[] translation )
        {
            Host.Video.Device.Graphics.DrawTransTranslate( TranslateTexture, x, y, pic.Width, pic.Height, translation );
        }

        // Draw_ConsoleBackground
        public void DrawConsoleBackground( Int32 lines )
        {
            var y = ( Host.Screen.vid.height * 3 ) >> 2;

            if ( lines > y )
            {
                Host.Video.Device.Graphics.DrawPicture( ConsoleBackground, 0, lines - Host.Screen.vid.height, Host.Screen.vid.width, Host.Screen.vid.height );
            }
            else
            {
                var alpha = ( Int32 ) Math.Min( ( 255 * ( ( 1.2f * lines ) / y ) ), 255 );

                Host.Video.Device.Graphics.DrawPicture( ConsoleBackground, 0, lines - Host.Screen.vid.height, Host.Screen.vid.width, Host.Screen.vid.height, Color.FromArgb( alpha, Color.White ) );
            }
        }

        /// <summary>
        /// GL_SelectTexture
        /// </summary>
        public void SelectTexture( MTexTarget target )
        {
            if ( !Host.Video.Device.Desc.SupportsMultiTexture )
                return;

            Host.Video.Device.SelectTexture( target );

            if ( target == _OldTarget )
                return;

            _CntTextures[_OldTarget - MTexTarget.TEXTURE0_SGIS] = Host.DrawingContext.CurrentTexture;
            Host.DrawingContext.CurrentTexture = _CntTextures[target - MTexTarget.TEXTURE0_SGIS];
            _OldTarget = target;
        }

        /// <summary>
        /// Draw_TextureMode_f
        /// </summary>
        private void TextureMode_f( CommandMessage msg )
        {
            if ( msg.Parameters == null || msg.Parameters.Length == 0 )
            {
                foreach ( var textureFilter in Host.Video.Device.TextureFilters )
                {
                    if ( CurrentFilter == textureFilter.Name )
                    {
                        Host.Console.Print( $"{textureFilter.Name}\n" );
                        return;
                    }
                }

                Host.Console.Print( "current filter is unknown???\n" );
                return;
            }

            BaseTextureFilter newFilter = null;

            foreach ( var textureFilter in Host.Video.Device.TextureFilters )
            {
                if ( Utilities.SameText( textureFilter.Name, msg.Parameters[0] ) )
                {
                    newFilter = textureFilter;
                    break;
                }
            }

            if ( newFilter == null )
            {
                Host.Console.Print( "bad filter name!\n" );
                return;
            }

            var count = 0;

            // change all the existing mipmap texture objects
            foreach ( var texture in BaseTexture.TexturePool )
            {
                var t = texture.Value;

                if ( t.Desc.HasMipMap )
                {
                    t.Desc.Filter = newFilter.Name;
                    t.Bind( );

                    Host.Video.Device.SetTextureFilters( newFilter.Name );

                    count++;
                }
            }

            Host.Console.Print( $"Set {count} textures to {newFilter.Name}\n" );
            CurrentFilter = newFilter.Name;
        }

        private void Imagelist_f( CommandMessage msg )
        {
            Int16 textureCount = 0;

            foreach ( var glTexture in _glTextures )
            {
                if ( glTexture != null )
                {
                    Host.Console.Print( "{0} x {1}   {2}:{3}\n", glTexture.width, glTexture.height,
                    glTexture.owner, glTexture.identifier );
                    textureCount++;
                }
            }

            Host.Console.Print( "{0} textures currently loaded.\n", textureCount );
        }

        private void CharToConback( Int32 num, ByteArraySegment dest, ByteArraySegment drawChars )
        {
            var row = num >> 4;
            var col = num & 15;
            var destOffset = dest.StartIndex;
            var srcOffset = drawChars.StartIndex + ( row << 10 ) + ( col << 3 );
            //source = draw_chars + (row<<10) + (col<<3);
            var drawline = 8;

            while ( drawline-- > 0 )
            {
                for ( var x = 0; x < 8; x++ )
                    if ( drawChars.Data[srcOffset + x] != 255 )
                        dest.Data[destOffset + x] = ( Byte ) ( 0x60 + drawChars.Data[srcOffset + x] ); // source[x];
                srcOffset += 128; // source += 128;
                destOffset += 320; // dest += 320;
            }
        }

        public Drawer( Host host )
        {
            Host = host;
        }
    }
}
