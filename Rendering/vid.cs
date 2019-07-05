/// <copyright>
///
/// SharpQuakeEvolved changes by optimus-code, 2019
/// 
/// Based on SharpQuake Rewritten in C# by Yury Kiselev, 2010.
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
using System.IO;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using SharpQuake.Framework;

// vid.h -- video driver defs

namespace SharpQuake
{
    /// <summary>
    /// Vid_functions
    /// </summary>
    public class vid
    {
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

        public System.Boolean glMTexable
        {
            get
            {
                return _glMTexable;
            }
        }

        public System.Boolean glZTrick
        {
            get
            {
                return ( _glZTrick.Value != 0 );
            }
        }

        public System.Boolean WindowedMouse
        {
            get
            {
                return _WindowedMouse.Value != 0;
            }
        }

        public System.Boolean Wait
        {
            get
            {
                return _Wait.Value != 0;
            }
        }

        public VidMode[] Modes
        {
            get
            {
                return _Modes;
            }
        }

        public Int32 ModeNum
        {
            get
            {
                return _ModeNum;
            }
        }

        public const Int32 VID_CBITS = 6;
        public const Int32 VID_GRADES = (1 << VID_CBITS);
        public const Int32 VID_ROW_SIZE = 3;
        private const Int32 WARP_WIDTH = 320;
        private const Int32 WARP_HEIGHT = 200;
        private UInt16[] _8to16table = new UInt16[256]; // d_8to16table[256]
        private UInt32[] _8to24table = new UInt32[256]; // d_8to24table[256]
        private Byte[] _15to8table = new Byte[65536]; // d_15to8table[65536]

        private VidMode[] _Modes;
        private Int32 _ModeNum; // vid_modenum

        private CVar _glZTrick;// = { "gl_ztrick", "1" };
        private CVar _Mode;// = { "vid_mode", "0", false };

        // Note that 0 is MODE_WINDOWED
        private CVar _DefaultMode;// = { "_vid_default_mode", "0", true };

        // Note that 3 is MODE_FULLSCREEN_DEFAULT
        private CVar _DefaultModeWin;// = { "_vid_default_mode_win", "3", true };

        private CVar _Wait;// = { "vid_wait", "0" };
        private CVar _NoPageFlip;// = { "vid_nopageflip", "0", true };
        private CVar _WaitOverride;// = { "_vid_wait_override", "0", true };
        private CVar _ConfigX;// = { "vid_config_x", "800", true };
        private CVar _ConfigY;// = { "vid_config_y", "600", true };
        private CVar _StretchBy2;// = { "vid_stretch_by_2", "1", true };
        private CVar _WindowedMouse;// = { "_windowed_mouse", "1", true };

        private System.Boolean _Windowed; // windowed

        //private bool _IsInitialized; // vid_initialized
        private Single _Gamma = 1.0f; // vid_gamma

        private Int32 _DefModeNum;
        private System.Boolean _glMTexable = false; // gl_mtexable

        private String _glVendor; // gl_vendor
        private String _glRenderer; // gl_renderer
        private String _glVersion; // gl_version
        private String _glExtensions; // gl_extensions

        // CHANGE
        private Host Host
        {
            get;
            set;
        }

        public vid( Host host )
        {
            Host = host;
        }

        // VID_Init (unsigned char *palette)
        // Called at startup to set up translation tables, takes 256 8 bit RGB values
        // the palette data will go away after the call, so it must be copied off if
        // the video driver will need it again
        public void Initialise( Byte[] palette )
        {
            if ( _glZTrick == null )
            {
                _glZTrick = new CVar( "gl_ztrick", "1" );
                _Mode = new CVar( "vid_mode", "0", false );
                _DefaultMode = new CVar( "_vid_default_mode", "0", true );
                _DefaultModeWin = new CVar( "_vid_default_mode_win", "3", true );
                _Wait = new CVar( "vid_wait", "0" );
                _NoPageFlip = new CVar( "vid_nopageflip", "0", true );
                _WaitOverride = new CVar( "_vid_wait_override", "0", true );
                _ConfigX = new CVar( "vid_config_x", "800", true );
                _ConfigY = new CVar( "vid_config_y", "600", true );
                _StretchBy2 = new CVar( "vid_stretch_by_2", "1", true );
                _WindowedMouse = new CVar( "_windowed_mouse", "1", true );
            }

            Host.Command.Add( "vid_nummodes", NumModes_f );
            Host.Command.Add( "vid_describecurrentmode", DescribeCurrentMode_f );
            Host.Command.Add( "vid_describemode", DescribeMode_f );
            Host.Command.Add( "vid_describemodes", DescribeModes_f );

            var dev = MainWindow.DisplayDevice;

            // Enumerate available modes, skip 8 bpp modes, and group by refresh rates
            var tmp = new List<VidMode>( dev.AvailableResolutions.Count );
            foreach( var res in dev.AvailableResolutions )
            {
                if( res.BitsPerPixel <= 8 )
                    continue;

                Predicate<VidMode> SameMode = delegate ( VidMode m )
                {
                    return ( m.width == res.Width && m.height == res.Height && m.bpp == res.BitsPerPixel );
                };
                if( tmp.Exists( SameMode ) )
                    continue;

                var mode = new VidMode();
                mode.width = res.Width;
                mode.height = res.Height;
                mode.bpp = res.BitsPerPixel;
                mode.refreshRate = res.RefreshRate;
                tmp.Add( mode );
            }
            _Modes = tmp.ToArray();

            var mode1 = new VidMode();
            mode1.width = dev.Width;
            mode1.height = dev.Height;
            mode1.bpp = dev.BitsPerPixel;
            mode1.refreshRate = dev.RefreshRate;
            mode1.fullScreen = true;

            Int32 width = dev.Width, height = dev.Height;
            var i = CommandLine.CheckParm( "-width" );
            if( i > 0 && i < CommandLine.Argc - 1 )
            {
                width = MathLib.atoi( CommandLine.Argv( i + 1 ) );

                foreach( var res in dev.AvailableResolutions )
                {
                    if( res.Width == width )
                    {
                        height = res.Height;
                        break;
                    }
                }
            }

            i = CommandLine.CheckParm( "-height" );
            if( i > 0 && i < CommandLine.Argc - 1 )
                height = MathLib.atoi( CommandLine.Argv( i + 1 ) );

            mode1.width = width;
            mode1.height = height;

            if( CommandLine.HasParam( "-window" ) )
            {
                _Windowed = true;
            }
            else
            {
                _Windowed = false;

                if( CommandLine.HasParam( "-current" ) )
                {
                    mode1.width = dev.Width;
                    mode1.height = dev.Height;
                }
                else
                {
                    var bpp = mode1.bpp;
                    i = CommandLine.CheckParm( "-bpp" );
                    if( i > 0 && i < CommandLine.Argc - 1 )
                    {
                        bpp = MathLib.atoi( CommandLine.Argv( i + 1 ) );
                    }
                    mode1.bpp = bpp;
                }
            }

            //_IsInitialized = true;

            var i2 = CommandLine.CheckParm( "-conwidth" );
            if( i2 > 0 )
                Host.Screen.vid.conwidth = MathLib.atoi( CommandLine.Argv( i2 + 1 ) );
            else
                Host.Screen.vid.conwidth = 640;

            Host.Screen.vid.conwidth &= 0xfff8; // make it a multiple of eight

            if( Host.Screen.vid.conwidth < 320 )
                Host.Screen.vid.conwidth = 320;

            // pick a conheight that matches with correct aspect
            Host.Screen.vid.conheight = Host.Screen.vid.conwidth * 3 / 4;

            i2 = CommandLine.CheckParm( "-conheight" );
            if( i2 > 0 )
                Host.Screen.vid.conheight = MathLib.atoi( CommandLine.Argv( i2 + 1 ) );
            if( Host.Screen.vid.conheight < 200 )
                Host.Screen.vid.conheight = 200;

            Host.Screen.vid.maxwarpwidth = WARP_WIDTH;
            Host.Screen.vid.maxwarpheight = WARP_HEIGHT;
            Host.Screen.vid.colormap = Host.ColorMap;
            var v = BitConverter.ToInt32( Host.ColorMap, 2048 );
            Host.Screen.vid.fullbright = 256 - EndianHelper.LittleLong( v );

            CheckGamma( palette );
            SetPalette( palette );

            mode1.fullScreen = !_Windowed;

            _DefModeNum = -1;
            for( i = 0; i < _Modes.Length; i++ )
            {
                var m = _Modes[i];
                if( m.width != mode1.width || m.height != mode1.height )
                    continue;

                _DefModeNum = i;

                if( m.bpp == mode1.bpp && m.refreshRate == mode1.refreshRate )
                    break;
            }
            if( _DefModeNum == -1 )
                _DefModeNum = 0;

            SetMode( _DefModeNum, palette );

            InitOpenGL();

            Directory.CreateDirectory( Path.Combine( FileSystem.GameDir, "glquake" ) );
        }

        /// <summary>
        /// VID_Shutdown
        /// Called at shutdown
        /// </summary>
        public void Shutdown()
        {
            //_IsInitialized = false;
        }

        // VID_SetMode (int modenum, unsigned char *palette)
        // sets the mode; only used by the Quake engine for resetting to mode 0 (the
        // base mode) on memory allocation failures
        public void SetMode( Int32 modenum, Byte[] palette )
        {
            if( modenum < 0 || modenum >= _Modes.Length )
            {
                Utilities.Error( "Bad video mode\n" );
            }

            var mode = _Modes[modenum];

            // so Con_Printfs don't mess us up by forcing vid and snd updates
            var temp = Host.Screen.IsDisabledForLoading;
            Host.Screen.IsDisabledForLoading = true;

            Host.CDAudio.Pause();

            // Set either the fullscreen or windowed mode
            var dev = MainWindow.DisplayDevice;
            var form = MainWindow.Instance;
            if( _Windowed )
            {
                try
                {
                    dev.ChangeResolution(mode.width, mode.height, mode.bpp, mode.refreshRate);
                }
                catch (Exception ex)
                {
                    Utilities.Error("Couldn't set video mode: " + ex.Message);
                }
                form.WindowState = WindowState.Normal;
                form.WindowBorder = WindowBorder.Fixed;

                /*form.WindowState = WindowState.Normal;
                form.WindowBorder = WindowBorder.Fixed;
                form.Location = new Point( ( mode.width - form.Width ) / 2, ( mode.height - form.Height ) / 2 );
                if( _WindowedMouse.Value != 0 && Key.Destination == keydest_t.key_game )
                {
                    Input.ActivateMouse();
                    Input.HideMouse();
                }
                else
                {
                    Input.DeactivateMouse();
                    Input.ShowMouse();
                }*/
            }
            else
            {
                try
                {
                    dev.ChangeResolution( mode.width, mode.height, mode.bpp, mode.refreshRate );
                }
                catch( Exception ex )
                {
                    Utilities.Error( "Couldn't set video mode: " + ex.Message );
                }
                form.WindowState = WindowState.Fullscreen;
                form.WindowBorder = WindowBorder.Hidden;
            }

            var vid = Host.Screen.vid;
            if( vid.conheight > dev.Height )
                vid.conheight = dev.Height;
            if( vid.conwidth > dev.Width )
                vid.conwidth = dev.Width;

            // Support any aspect ratio by converting the virtual coordinate system
            var aspectRatio = Host.MainWindow.ClientSize.Width / ( Double ) Host.MainWindow.ClientSize.Height;
            var width = ( Int32 ) ( vid.conheight * aspectRatio );

            vid.width = width; // vid.conwidth
            vid.height = vid.conheight;

            vid.numpages = 2;

            Host.CDAudio.Resume();
            Host.Screen.IsDisabledForLoading = temp;

            _ModeNum = modenum;
            CVar.Set( "vid_mode", ( Single ) _ModeNum );

            // fix the leftover Alt from any Alt-Tab or the like that switched us away
            ClearAllStates();

            Host.Console.SafePrint( "Video mode {0} initialized.\n", GetModeDescription( _ModeNum ) );

            SetPalette( palette );

            vid.recalc_refdef = true;
        }

        /// <summary>
        /// VID_GetModeDescription
        /// </summary>
        public String GetModeDescription( Int32 mode )
        {
            if( mode < 0 || mode >= _Modes.Length )
                return String.Empty;

            var m = _Modes[mode];
            return String.Format( "{0}x{1}x{2} {3}", m.width, m.height, m.bpp, _Windowed ? "windowed" : "fullscreen" );
        }

        /// <summary>
        /// VID_SetPalette
        /// called at startup and after any gamma correction
        /// </summary>
        public void SetPalette( Byte[] palette )
        {
            //
            // 8 8 8 encoding
            //
            var offset = 0;
            var pal = palette;
            var table = _8to24table;
            for( var i = 0; i < table.Length; i++ )
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
            for( UInt32 i = 0; i < ( 1 << 15 ); i++ )
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
                for( UInt32 v = 0; v < 256; v++ )
                {
                    val.ui0 = _8to24table[v];
                    var r1 = r - val.b0;
                    var g1 = g - val.b1;
                    var b1 = b - val.b2;
                    var j = ( r1 * r1 ) + ( g1 * g1 ) + ( b1 * b1 );
                    if( j < l )
                    {
                        k = v;
                        l = j;
                    }
                }
                _15to8table[i] = ( Byte ) k;
            }
        }

        /// <summary>
        /// GL_Init
        /// </summary>
        private void InitOpenGL()
        {
            _glVendor = GL.GetString( StringName.Vendor );
            Host.Console.Print( "GL_VENDOR: {0}\n", _glVendor );
            _glRenderer = GL.GetString( StringName.Renderer );
            Host.Console.Print( "GL_RENDERER: {0}\n", _glRenderer );

            _glVersion = GL.GetString( StringName.Version );
            Host.Console.Print( "GL_VERSION: {0}\n", _glVersion );
            _glExtensions = GL.GetString( StringName.Extensions );
            Host.Console.Print( "GL_EXTENSIONS: {0}\n", _glExtensions );

            if( _glRenderer.StartsWith( "PowerVR", StringComparison.InvariantCultureIgnoreCase ) )
                Host.Screen.FullSbarDraw = true;

            if( _glRenderer.StartsWith( "Permedia", StringComparison.InvariantCultureIgnoreCase ) )
                Host.Screen.IsPermedia = true;

            CheckTextureExtensions();
            CheckMultiTextureExtensions();

            GL.ClearColor( 1, 0, 0, 0 );
            GL.CullFace( CullFaceMode.Front );
            GL.Enable( EnableCap.Texture2D );

            GL.Enable( EnableCap.AlphaTest );
            GL.AlphaFunc( AlphaFunction.Greater, 0.666f );

            GL.PolygonMode( MaterialFace.FrontAndBack, PolygonMode.Fill );
            GL.ShadeModel( ShadingModel.Flat );

            Host.DrawingContext.SetTextureFilters( TextureMinFilter.Nearest, TextureMagFilter.Nearest );
            GL.TexParameter( TextureTarget.Texture2D, TextureParameterName.TextureWrapS, ( Int32 ) TextureWrapMode.Repeat );
            GL.TexParameter( TextureTarget.Texture2D, TextureParameterName.TextureWrapT, ( Int32 ) TextureWrapMode.Repeat );
            GL.BlendFunc( BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha );
            GL.TexEnv( TextureEnvTarget.TextureEnv, TextureEnvParameter.TextureEnvMode, ( Int32 ) TextureEnvMode.Replace );
        }

        // VID_NumModes_f
        private void NumModes_f()
        {
            var nummodes = _Modes.Length;
            if( nummodes == 1 )
                Host.Console.Print( "{0} video mode is available\n", nummodes );
            else
                Host.Console.Print( "{0} video modes are available\n", nummodes );
        }

        // VID_DescribeCurrentMode_f
        private void DescribeCurrentMode_f()
        {
            Host.Console.Print( "{0}\n", GetExtModeDescription( _ModeNum ) );
        }

        // VID_DescribeMode_f
        private void DescribeMode_f()
        {
            var modenum = MathLib.atoi( Host.Command.Argv( 1 ) );

            Host.Console.Print( "{0}\n", GetExtModeDescription( modenum ) );
        }

        // VID_DescribeModes_f
        private void DescribeModes_f()
        {
            for( var i = 0; i < _Modes.Length; i++ )
            {
                Host.Console.Print( "{0}:{1}\n", i, GetExtModeDescription( i ) );
            }
        }

        private String GetExtModeDescription( Int32 mode )
        {
            return GetModeDescription( mode );
        }

        // Check_Gamma
        private void CheckGamma( Byte[] pal )
        {
            var i = CommandLine.CheckParm( "-gamma" );
            if( i == 0 )
            {
                var renderer = GL.GetString( StringName.Renderer );
                var vendor = GL.GetString( StringName.Vendor );
                if( renderer.Contains( "Voodoo" ) || vendor.Contains( "3Dfx" ) )
                    _Gamma = 1;
                else
                    _Gamma = 0.7f; // default to 0.7 on non-3dfx hardware
            }
            else
                _Gamma = Single.Parse( CommandLine.Argv( i + 1 ) );

            for( i = 0; i < pal.Length; i++ )
            {
                var f = Math.Pow( ( pal[i] + 1 ) / 256.0, _Gamma );
                var inf = f * 255 + 0.5;
                if( inf < 0 )
                    inf = 0;
                if( inf > 255 )
                    inf = 255;
                pal[i] = ( Byte ) inf;
            }
        }

        // ClearAllStates
        private void ClearAllStates()
        {
            // send an up event for each key, to make sure the server clears them all
            for( var i = 0; i < 256; i++ )
            {
                Host.Keyboard.Event( i, false );
            }

            Host.Keyboard.ClearStates();
            MainWindow.Input.ClearStates();
        }

        /// <summary>
        /// CheckTextureExtensions
        /// </summary>
        private void CheckTextureExtensions()
        {
            const String TEXTURE_EXT_STRING = "GL_EXT_texture_object";

            // check for texture extension
            var texture_ext = _glExtensions.Contains( TEXTURE_EXT_STRING );
        }

        /// <summary>
        /// CheckMultiTextureExtensions
        /// </summary>
        private void CheckMultiTextureExtensions()
        {
            if( _glExtensions.Contains( "GL_SGIS_multitexture " ) && !CommandLine.HasParam( "-nomtex" ) )
            {
                Host.Console.Print( "Multitexture extensions found.\n" );
                _glMTexable = true;
            }
        }
    }
}
