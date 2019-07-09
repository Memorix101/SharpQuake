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
using System.IO;
using SharpQuake.Framework;
using SharpQuake.Framework.IO;
using SharpQuake.Renderer;
using SharpQuake.Renderer.OpenGL;

// vid.h -- video driver defs

namespace SharpQuake
{
    /// <summary>
    /// Vid_functions
    /// </summary>
    public class Vid
    {
        public UInt16[] Table8to16
        {
            get
            {
                return Device.Palette.Table8to16;//_8to16table;
            }
        }

        public UInt32[] Table8to24
        {
            get
            {
                return Device.Palette.Table8to24;//_8to24table;
            }
        }

        public Byte[] Table15to8
        {
            get
            {
                return Device.Palette.Table15to8;//_15to8table;
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

        public Int32 ModeNum
        {
            get
            {
                return Device.ChosenMode;//_ModeNum;
            }
        }

        public const Int32 VID_CBITS = 6;
        public const Int32 VID_GRADES = (1 << VID_CBITS);
        public const Int32 VID_ROW_SIZE = 3;
        private const Int32 WARP_WIDTH = 320;
        private const Int32 WARP_HEIGHT = 200;
        
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

        private System.Boolean _Windowed
        {
            get
            {
                return !Device.Desc.IsFullScreen;
            }
        }

        // Instances
        private Host Host
        {
            get;
            set;
        }

        public BaseDevice Device
        {
            get;
            private set;
        }

        public Vid( Host host )
        {
            Host = host;

            Device = new GLDevice( Host.MainWindow, MainWindow.DisplayDevice );
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

            Device.Initialise( palette );

            UpdateConsole( );
            UpdateScreen( );

            // Moved from SetMode

            // so Con_Printfs don't mess us up by forcing vid and snd updates
            var temp = Host.Screen.IsDisabledForLoading;
            Host.Screen.IsDisabledForLoading = true;
            Host.CDAudio.Pause( );

            Device.SetMode( Device.ChosenMode, palette );

            var vid = Host.Screen.vid;

            UpdateConsole( false );

            vid.width = Device.Desc.Width; // vid.conwidth
            vid.height = Device.Desc.Height;
            vid.numpages = 2;

            Host.CDAudio.Resume( );
            Host.Screen.IsDisabledForLoading = temp;

            CVar.Set( "vid_mode", ( Single ) Device.ChosenMode );

            // fix the leftover Alt from any Alt-Tab or the like that switched us away
            ClearAllStates( );

            Host.Console.SafePrint( "Video mode {0} initialized.\n", Device.GetModeDescription( Device.ChosenMode ) );

            vid.recalc_refdef = true;

            if ( Device.Desc.Renderer.StartsWith( "PowerVR", StringComparison.InvariantCultureIgnoreCase ) )
                Host.Screen.FullSbarDraw = true;

            if ( Device.Desc.Renderer.StartsWith( "Permedia", StringComparison.InvariantCultureIgnoreCase ) )
                Host.Screen.IsPermedia = true;

            CheckTextureExtensions( );

            Directory.CreateDirectory( Path.Combine( FileSystem.GameDir, "glquake" ) );
        }

        private void UpdateScreen()
        {
            Host.Screen.vid.maxwarpwidth = WARP_WIDTH;
            Host.Screen.vid.maxwarpheight = WARP_HEIGHT;
            Host.Screen.vid.colormap = Host.ColorMap;
            var v = BitConverter.ToInt32( Host.ColorMap, 2048 );
            Host.Screen.vid.fullbright = 256 - EndianHelper.LittleLong( v );
        }

        private void UpdateConsole( System.Boolean isInitialStage = true )
        {
            var vid = Host.Screen.vid;

            if ( isInitialStage )
            {
                var i2 = CommandLine.CheckParm( "-conwidth" );

                if ( i2 > 0 )
                    vid.conwidth = MathLib.atoi( CommandLine.Argv( i2 + 1 ) );
                else
                    vid.conwidth = 640;

                vid.conwidth &= 0xfff8; // make it a multiple of eight

                if ( vid.conwidth < 320 )
                    vid.conwidth = 320;

                // pick a conheight that matches with correct aspect
                vid.conheight = vid.conwidth * 3 / 4;

                i2 = CommandLine.CheckParm( "-conheight" );

                if ( i2 > 0 )
                    vid.conheight = MathLib.atoi( CommandLine.Argv( i2 + 1 ) );

                if ( vid.conheight < 200 )
                    vid.conheight = 200;
            }
            else
            {
                if ( vid.conheight > Device.Desc.Height )
                    vid.conheight = Device.Desc.Height;
                if ( vid.conwidth > Device.Desc.Width )
                    vid.conwidth = Device.Desc.Width;
            }
        }

        /// <summary>
        /// VID_Shutdown
        /// Called at shutdown
        /// </summary>
        public void Shutdown()
        {
            Device.Dispose( );
            //_IsInitialized = false;
        }


        /// <summary>
        /// VID_GetModeDescription
        /// </summary>
        public String GetModeDescription( Int32 mode )
        {
            return Device.GetModeDescription( mode );
        }

        // VID_NumModes_f
        private void NumModes_f()
        {
            var nummodes = Device.AvailableModes.Length;

            if( nummodes == 1 )
                Host.Console.Print( "{0} video mode is available\n", nummodes );
            else
                Host.Console.Print( "{0} video modes are available\n", nummodes );
        }

        // VID_DescribeCurrentMode_f
        private void DescribeCurrentMode_f()
        {
            Host.Console.Print( "{0}\n", GetModeDescription( Device.ChosenMode ) );
        }

        // VID_DescribeMode_f
        private void DescribeMode_f()
        {
            var modenum = MathLib.atoi( Host.Command.Argv( 1 ) );

            Host.Console.Print( "{0}\n", GetModeDescription( modenum ) );
        }

        // VID_DescribeModes_f
        private void DescribeModes_f()
        {
            for( var i = 0; i < Device.AvailableModes.Length; i++ )
            {
                Host.Console.Print( "{0}:{1}\n", i, GetModeDescription( i ) );
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
            var texture_ext = Device.Desc.Extensions.Contains( TEXTURE_EXT_STRING );
        }
    }
}
