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
using SharpQuake.Factories.Rendering.UI;
using SharpQuake.Framework;

namespace SharpQuake.Rendering.UI
{
    public class OptionsMenu : BaseMenu
    {
        private const Int32 OPTIONS_ITEMS = 13;

        //private float _BgmVolumeCoeff = 0.1f;

        public OptionsMenu( MenuFactory menuFactory ) : base( "menu_options", menuFactory )
        {
        }

        public override void Show( Host host )
        {
            /*if( sys.IsWindows )  fix cd audio first
             {
                 _BgmVolumeCoeff = 1.0f;
             }*/

            if ( Cursor > OPTIONS_ITEMS - 1 )
                Cursor = 0;

            base.Show( host );
        }

        public override void KeyEvent( Int32 key )
        {
            switch ( key )
            {
                case KeysDef.K_ESCAPE:
                    MenuFactory.Show( "menu_main" );
                    break;

                case KeysDef.K_ENTER:
                    Host.Menus.EnterSound = true;
                    switch ( Cursor )
                    {
                        case 0:
                            MenuFactory.Show( "menu_keys" );
                            break;

                        case 1:
                            MenuFactory.CurrentMenu.Hide( );
                            Host.Console.ToggleConsole_f( null );
                            break;

                        case 2:
                            Host.Commands.Buffer.Append( "exec default.cfg\n" );
                            break;

                        case 12:
                            MenuFactory.Show( "menu_video" );
                            break;

                        default:
                            AdjustSliders( 1 );
                            break;
                    }
                    return;

                case KeysDef.K_UPARROW:
                    Host.Sound.LocalSound( "misc/menu1.wav" );
                    Cursor--;
                    if ( Cursor < 0 )
                        Cursor = OPTIONS_ITEMS - 1;
                    break;

                case KeysDef.K_DOWNARROW:
                    Host.Sound.LocalSound( "misc/menu1.wav" );
                    Cursor++;
                    if ( Cursor >= OPTIONS_ITEMS )
                        Cursor = 0;
                    break;

                case KeysDef.K_LEFTARROW:
                    AdjustSliders( -1 );
                    break;

                case KeysDef.K_RIGHTARROW:
                    AdjustSliders( 1 );
                    break;
            }

            /*if( Cursor == 12 && VideoMenu == null )
            {
                if( key == KeysDef.K_UPARROW )
                    Cursor = 11;
                else
                    Cursor = 0;
            }*/

            if ( Cursor == 12 )
            {
                if ( key == KeysDef.K_UPARROW )
                    Cursor = 11;
                else
                    Cursor = 0;
            }

            /*#if _WIN32
                        if ((optionsCursor == 13) && (modestate != MS_WINDOWED))
                        {
                            if (k == K_UPARROW)
                                optionsCursor = 12;
                            else
                                optionsCursor = 0;
                        }
            #endif*/
        }

        private void DrawPlaque()
        {
            Host.Menus.DrawTransPic( 16, 4, Host.Pictures.Cache( "gfx/qplaque.lmp", "GL_NEAREST" ) );
            var p = Host.Pictures.Cache( "gfx/p_option.lmp", "GL_NEAREST" );
            Host.Menus.DrawPic( ( 320 - p.Width ) / 2, 4, p );
        }

        private void DrawSound()
		{
            Host.Menus.Print( 16, 80, "       CD Music Volume" );
            var r = Host.Sound.BgmVolume;
            Host.Menus.DrawSlider( 220, 80, r );

            Host.Menus.Print( 16, 88, "          Sound Volume" );
            r = Host.Sound.Volume;
            Host.Menus.DrawSlider( 220, 88, r );
        }

        private void DrawMovementControls()
        {
            Host.Menus.Print( 16, 72, "           Mouse Speed" );
            var r = ( Host.Client.Sensitivity - 1 ) / 10;
            Host.Menus.DrawSlider( 220, 72, r );

            Host.Menus.Print( 16, 96, "            Always Run" );
            Host.Menus.DrawCheckbox( 220, 96, Host.Client.ForwardSpeed > 200 );

            Host.Menus.Print( 16, 104, "          Invert Mouse" );
            Host.Menus.DrawCheckbox( 220, 104, Host.Client.MPitch < 0 );

            Host.Menus.Print( 16, 112, "            Lookspring" );
            Host.Menus.DrawCheckbox( 220, 112, Host.Client.LookSpring );

            Host.Menus.Print( 16, 120, "            Lookstrafe" );
            Host.Menus.DrawCheckbox( 220, 120, Host.Client.LookStrafe );
        }

        private void DrawScreenSettings()
		{
            Host.Menus.Print( 16, 56, "           Screen size" );
            var r = ( Host.Screen.ViewSize.Get<Single>() - 30 ) / ( 120 - 30 );
            Host.Menus.DrawSlider( 220, 56, r );

            Host.Menus.Print( 16, 64, "            Brightness" );
            r = ( 1.0f - Host.View.Gamma ) / 0.5f;
            Host.Menus.DrawSlider( 220, 64, r );
        }

        public override void Draw( )
        {
            DrawPlaque();

            Host.Menus.Print( 16, 32, "    Customize controls" );
            Host.Menus.Print( 16, 40, "         Go to console" );
            Host.Menus.Print( 16, 48, "     Reset to defaults" );

            DrawScreenSettings();
            DrawSound();
            DrawMovementControls();

            /*if( VideoMenu != null )
                Host.Menu.Print( 16, 128, "         Video Options" );*/

#if _WIN32
	if (modestate == MS_WINDOWED)
	{
		Host.Menu.Print (16, 136, "             Use Mouse");
		Host.Menu.DrawCheckbox (220, 136, _windowed_mouse.value);
	}
#endif

            // cursor
            Host.Menus.DrawCharacter( 200, 32 + Cursor * 8, 12 + ( ( Int32 ) ( Host.RealTime * 4 ) & 1 ) );
        }

        /// <summary>
        /// M_AdjustSliders
        /// </summary>
        private void AdjustSliders( Int32 dir )
        {
            Host.Sound.LocalSound( "misc/menu3.wav" );
            Single value;

            switch ( Cursor )
            {
                case 3:	// screen size
                    value = Host.Screen.ViewSize.Get<Single>( ) + dir * 10;
                    if ( value < 30 )
                        value = 30;
                    if ( value > 120 )
                        value = 120;
                    Host.CVars.Set( "viewsize", value );
                    break;

                case 4:	// gamma
                    value = Host.View.Gamma - dir * 0.05f;
                    if ( value < 0.5 )
                        value = 0.5f;
                    if ( value > 1 )
                        value = 1;
                    Host.CVars.Set( "gamma", value );
                    break;

                case 5:	// mouse speed
                    value = Host.Client.Sensitivity + dir * 0.5f;
                    if ( value < 1 )
                        value = 1;
                    if ( value > 11 )
                        value = 11;
                    Host.CVars.Set( "sensitivity", value );
                    break;

                case 6:	// music volume
                    value = Host.Sound.BgmVolume + dir * 0.1f; ///_BgmVolumeCoeff;
                    if ( value < 0 )
                        value = 0;
                    if ( value > 1 )
                        value = 1;
                    Host.CVars.Set( "bgmvolume", value );
                    break;

                case 7:	// sfx volume
                    value = Host.Sound.Volume + dir * 0.1f;
                    if ( value < 0 )
                        value = 0;
                    if ( value > 1 )
                        value = 1;
                    Host.CVars.Set( "volume", value );
                    break;

                case 8:	// allways run
                    if ( Host.Client.ForwardSpeed > 200 )
                    {
                        Host.CVars.Set( "cl_forwardspeed", 200f );
                        Host.CVars.Set( "cl_backspeed", 200f );
                    }
                    else
                    {
                        Host.CVars.Set( "cl_forwardspeed", 400f );
                        Host.CVars.Set( "cl_backspeed", 400f );
                    }
                    break;

                case 9:	// invert mouse
                    Host.CVars.Set( "m_pitch", -Host.Client.MPitch );
                    break;

                case 10:	// lookspring
                    Host.CVars.Set( "lookspring", !Host.Client.LookSpring );
                    break;

                case 11:	// lookstrafe
                    Host.CVars.Set( "lookstrafe", !Host.Client.LookStrafe );
                    break;

#if _WIN32
	        case 13:	// _windowed_mouse
		        Cvar_SetValue ("_windowed_mouse", !_windowed_mouse.value);
		        break;
#endif
            }
        }
    }
}
