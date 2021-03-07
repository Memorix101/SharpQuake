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
using System.Runtime.InteropServices;
using SharpQuake.Framework;
using SharpQuake.Framework.IO;

namespace SharpQuake.Rendering.UI
{
    public class SetupMenu : MenuBase
    {
        private const Int32 NUM_SETUP_CMDS = 5;

        private readonly Int32[] _CursorTable = new Int32[]
        {
            40, 56, 80, 104, 140
        }; // setup_cursor_table

        private String _HostName; // setup_hostname[16]
        private String _MyName; // setup_myname[16]
        private Int32 _OldTop; // setup_oldtop
        private Int32 _OldBottom; // setup_oldbottom
        private Int32 _Top; // setup_top
        private Int32 _Bottom; // setup_bottom
        private Boolean hasPlayPixels;

        /// <summary>
        /// M_Menu_Setup_f
        /// </summary>
        public override void Show( Host host )
        {
            _MyName = host.Client.Name;
            _HostName = host.Network.HostName;
            _Top = _OldTop = ( ( Int32 ) host.Client.Color ) >> 4;
            _Bottom = _OldBottom = ( ( Int32 ) host.Client.Color ) & 15;

            base.Show( host );
        }

        public override void KeyEvent( Int32 key )
        {
            switch ( key )
            {
                case KeysDef.K_ESCAPE:
                    MultiPlayerMenu.Show( Host );
                    break;

                case KeysDef.K_UPARROW:
                    Host.Sound.LocalSound( "misc/menu1.wav" );
                    _Cursor--;
                    if ( _Cursor < 0 )
                        _Cursor = NUM_SETUP_CMDS - 1;
                    break;

                case KeysDef.K_DOWNARROW:
                    Host.Sound.LocalSound( "misc/menu1.wav" );
                    _Cursor++;
                    if ( _Cursor >= NUM_SETUP_CMDS )
                        _Cursor = 0;
                    break;

                case KeysDef.K_LEFTARROW:
                    if ( _Cursor < 2 )
                        return;
                    Host.Sound.LocalSound( "misc/menu3.wav" );
                    if ( _Cursor == 2 )
                        _Top = _Top - 1;
                    if ( _Cursor == 3 )
                        _Bottom = _Bottom - 1;
                    break;

                case KeysDef.K_RIGHTARROW:
                    if ( _Cursor < 2 )
                        return;
                    forward:
                    Host.Sound.LocalSound( "misc/menu3.wav" );
                    if ( _Cursor == 2 )
                        _Top = _Top + 1;
                    if ( _Cursor == 3 )
                        _Bottom = _Bottom + 1;
                    break;

                case KeysDef.K_ENTER:
                    if ( _Cursor == 0 || _Cursor == 1 )
                        return;

                    if ( _Cursor == 2 || _Cursor == 3 )
                        goto forward;

                    // _Cursor == 4 (OK)
                    if ( _MyName != Host.Client.Name )
                        Host.Commands.Buffer.Append( String.Format( "name \"{0}\"\n", _MyName ) );
                    if ( Host.Network.HostName != _HostName )
                        Host.CVars.Set( "hostname", _HostName );
                    if ( _Top != _OldTop || _Bottom != _OldBottom )
                        Host.Commands.Buffer.Append( String.Format( "color {0} {1}\n", _Top, _Bottom ) );
                    Host.Menu.EnterSound = true;
                    MultiPlayerMenu.Show( Host );
                    break;

                case KeysDef.K_BACKSPACE:
                    if ( _Cursor == 0 )
                    {
                        if ( !String.IsNullOrEmpty( _HostName ) )
                            _HostName = _HostName.Substring( 0, _HostName.Length - 1 );// setup_hostname[strlen(setup_hostname) - 1] = 0;
                    }

                    if ( _Cursor == 1 )
                    {
                        if ( !String.IsNullOrEmpty( _MyName ) )
                            _MyName = _MyName.Substring( 0, _MyName.Length - 1 );
                    }
                    break;

                default:
                    if ( key < 32 || key > 127 )
                        break;
                    if ( _Cursor == 0 )
                    {
                        var l = _HostName.Length;
                        if ( l < 15 )
                        {
                            _HostName = _HostName + ( Char ) key;
                        }
                    }
                    if ( _Cursor == 1 )
                    {
                        var l = _MyName.Length;
                        if ( l < 15 )
                        {
                            _MyName = _MyName + ( Char ) key;
                        }
                    }
                    break;
            }

            if ( _Top > 13 )
                _Top = 0;
            if ( _Top < 0 )
                _Top = 13;
            if ( _Bottom > 13 )
                _Bottom = 0;
            if ( _Bottom < 0 )
                _Bottom = 13;
        }

        private void DrawPlaque()
		{
            Host.Menu.DrawTransPic( 16, 4, Host.DrawingContext.CachePic( "gfx/qplaque.lmp", "GL_NEAREST" ) );
            var p = Host.DrawingContext.CachePic( "gfx/p_multi.lmp", "GL_NEAREST" );
            Host.Menu.DrawPic( ( 320 - p.Width ) / 2, 4, p );
        }

        private void DrawHostName()
        {
            Host.Menu.Print( 64, 40, "Hostname" );
            Host.Menu.DrawTextBox( 160, 32, 16, 1 );
            Host.Menu.Print( 168, 40, _HostName );
        }

        private void DrawName()
		{
            Host.Menu.Print( 64, 56, "Your name" );
            Host.Menu.DrawTextBox( 160, 48, 16, 1 );
            Host.Menu.Print( 168, 56, _MyName );
        }

        private void DrawClothesColours()
		{
            Host.Menu.Print( 64, 80, "Shirt color" );
            Host.Menu.Print( 64, 104, "Pants color" );
        }

        private void DrawAcceptButton()
		{
            Host.Menu.DrawTextBox( 64, 140 - 8, 14, 1 );
            Host.Menu.Print( 72, 140, "Accept Changes" );
        }

        private void DrawBigBox()
        {
            var p = Host.DrawingContext.CachePic( "gfx/bigbox.lmp", "GL_NEAREST" );
            Host.Menu.DrawTransPic( 160, 64, p );
        }

        private void DrawPlayer()
		{
            var p = Host.DrawingContext.CachePic( "gfx/menuplyr.lmp", "GL_NEAREST", true );

            if ( !hasPlayPixels && p != null )
            {
                // HACK HACK HACK --- we need to keep the bytes for
                // the translatable player picture just for the menu
                // configuration dialog

                var headerSize = Marshal.SizeOf( typeof( WadPicHeader ) );
                var data = FileSystem.LoadFile( p.Identifier );
                Host.DrawingContext._MenuPlayerPixelWidth = p.Texture.Desc.Width;
                Host.DrawingContext._MenuPlayerPixelHeight = p.Texture.Desc.Height;
                Buffer.BlockCopy( data, headerSize, Host.DrawingContext._MenuPlayerPixels, 0, p.Texture.Desc.Width * p.Texture.Desc.Height );
                //memcpy (menuplyr_pixels, dat->data, dat->width*dat->height);

                hasPlayPixels = true;
            }

            Host.Menu.BuildTranslationTable( _Top * 16, _Bottom * 16 );
            Host.Menu.DrawTransPicTranslate( 172, 72, p );
        }

        private void DrawText()
		{
            Host.Menu.DrawCharacter( 56, _CursorTable[_Cursor], 12 + ( ( Int32 ) ( Host.RealTime * 4 ) & 1 ) );

            if ( _Cursor == 0 )
                Host.Menu.DrawCharacter( 168 + 8 * _HostName.Length, _CursorTable[_Cursor], 10 + ( ( Int32 ) ( Host.RealTime * 4 ) & 1 ) );

            if ( _Cursor == 1 )
                Host.Menu.DrawCharacter( 168 + 8 * _MyName.Length, _CursorTable[_Cursor], 10 + ( ( Int32 ) ( Host.RealTime * 4 ) & 1 ) );
        }

        public override void Draw( )
        {
            DrawPlaque();
            DrawHostName();
            DrawName();
            DrawClothesColours();
            DrawAcceptButton();
            DrawBigBox();
            DrawPlayer();
            DrawText();
        }
    }
}
