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

using SharpQuake.Framework;
using SharpQuake.Renderer.Textures;
using SharpQuake.Rendering.UI.Elements;
using System;

namespace SharpQuake.Rendering.UI
{
    /// <summary>
    /// Helper class to wrap core HUD resources and functions
    /// </summary>
    public class HudResources
    {
        public const Int32 SBAR_HEIGHT = 24;
        public const Int32 STAT_MINUS = 10;

        public BasePicture[,] Numbers = new BasePicture[2, 11];

        public BasePicture Colon;
        public BasePicture Slash;
        public BasePicture IBar;
        public BasePicture SBar;
        public BasePicture ScoreBar;

        public BasePicture[,] Weapons = new BasePicture[7, 8];   // 0 is active, 1 is owned, 2-5 are flashes
        public BasePicture[] Ammo = new BasePicture[4];
        public BasePicture[] Sigil = new BasePicture[4];
        public BasePicture[] Armour = new BasePicture[3];
        public BasePicture[] Items = new BasePicture[32];

        public BasePicture[,] Faces = new BasePicture[7, 2];        // 0 is gibbed, 1 is dead, 2-6 are alive

        // 0 is static, 1 is temporary animation

        public BasePicture FaceInvis;

        public BasePicture FaceQuad;
        public BasePicture FaceInvuln;
        public BasePicture FaceInvisInvuln;

        public BasePicture[] RInvBar = new BasePicture[2];
        public BasePicture[] RWeapons = new BasePicture[5];
        public BasePicture[] RItems = new BasePicture[2];
        public BasePicture[] RAmmo = new BasePicture[3];
        public BasePicture RTeamBord;      // PGM 01/19/97 - team color border

        //MED 01/04/97 added two more weapons + 3 alternates for grenade launcher
        public BasePicture[,] HWeapons = new BasePicture[7, 5];   // 0 is active, 1 is owned, 2-5 are flashes


        //MED 01/04/97 added hipnotic items array
        public BasePicture[] HItems = new BasePicture[2];

        public Int32[] _FragSort = new Int32[QDef.MAX_SCOREBOARD];
        public String[] _ScoreBoardText = new String[QDef.MAX_SCOREBOARD];
        public Int32[] _ScoreBoardTop = new Int32[QDef.MAX_SCOREBOARD];
        public Int32[] _ScoreBoardBottom = new Int32[QDef.MAX_SCOREBOARD];
        public Int32[] _ScoreBoardCount = new Int32[QDef.MAX_SCOREBOARD];
        public Int32 _ScoreBoardLines;

        public Int32 Lines
        {
            get; 
            set;
        }

        private readonly Host _host;

        public HudResources( Host host )
        {
            _host = host;
        }

        private void LoadNumbers( )
        {
            for ( var i = 0; i < 10; i++ )
            {
                var str = i.ToString( );

                Numbers[0, i] = BasePicture.FromWad( _host.Video.Device, _host.Wads.FromTexture( "num_" + str ), "num_" + str, "GL_NEAREST" );
                Numbers[1, i] = BasePicture.FromWad( _host.Video.Device, _host.Wads.FromTexture( "anum_" + str ), "anum_" + str, "GL_NEAREST" );
            }

            Numbers[0, 10] = BasePicture.FromWad( _host.Video.Device, _host.Wads.FromTexture( "num_minus" ), "num_minus", "GL_NEAREST" );
            Numbers[1, 10] = BasePicture.FromWad( _host.Video.Device, _host.Wads.FromTexture( "anum_minus" ), "anum_minus", "GL_NEAREST" );
        }

        private void LoadSymbols( )
        {
            Colon = BasePicture.FromWad( _host.Video.Device, _host.Wads.FromTexture( "num_colon" ), "num_colon", "GL_NEAREST" );
            Slash = BasePicture.FromWad( _host.Video.Device, _host.Wads.FromTexture( "num_slash" ), "num_slash", "GL_NEAREST" );
        }

        private void LoadWeapons( )
        {
            Weapons[0, 0] = BasePicture.FromWad( _host.Video.Device, _host.Wads.FromTexture( "inv_shotgun" ), "inv_shotgun", "GL_LINEAR" );
            Weapons[0, 1] = BasePicture.FromWad( _host.Video.Device, _host.Wads.FromTexture( "inv_sshotgun" ), "inv_sshotgun", "GL_LINEAR" );
            Weapons[0, 2] = BasePicture.FromWad( _host.Video.Device, _host.Wads.FromTexture( "inv_nailgun" ), "inv_nailgun", "GL_LINEAR" );
            Weapons[0, 3] = BasePicture.FromWad( _host.Video.Device, _host.Wads.FromTexture( "inv_snailgun" ), "inv_snailgun", "GL_LINEAR" );
            Weapons[0, 4] = BasePicture.FromWad( _host.Video.Device, _host.Wads.FromTexture( "inv_rlaunch" ), "inv_rlaunch", "GL_LINEAR" );
            Weapons[0, 5] = BasePicture.FromWad( _host.Video.Device, _host.Wads.FromTexture( "inv_srlaunch" ), "inv_srlaunch", "GL_LINEAR" );
            Weapons[0, 6] = BasePicture.FromWad( _host.Video.Device, _host.Wads.FromTexture( "inv_lightng" ), "inv_lightng", "GL_LINEAR" );

            Weapons[1, 0] = BasePicture.FromWad( _host.Video.Device, _host.Wads.FromTexture( "inv2_shotgun" ), "inv2_shotgun", "GL_LINEAR" );
            Weapons[1, 1] = BasePicture.FromWad( _host.Video.Device, _host.Wads.FromTexture( "inv2_sshotgun" ), "inv2_sshotgun", "GL_LINEAR" );
            Weapons[1, 2] = BasePicture.FromWad( _host.Video.Device, _host.Wads.FromTexture( "inv2_nailgun" ), "inv2_nailgun", "GL_LINEAR" );
            Weapons[1, 3] = BasePicture.FromWad( _host.Video.Device, _host.Wads.FromTexture( "inv2_snailgun" ), "inv2_snailgun", "GL_LINEAR" );
            Weapons[1, 4] = BasePicture.FromWad( _host.Video.Device, _host.Wads.FromTexture( "inv2_rlaunch" ), "inv2_rlaunch", "GL_LINEAR" );
            Weapons[1, 5] = BasePicture.FromWad( _host.Video.Device, _host.Wads.FromTexture( "inv2_srlaunch" ), "inv2_srlaunch", "GL_LINEAR" );
            Weapons[1, 6] = BasePicture.FromWad( _host.Video.Device, _host.Wads.FromTexture( "inv2_lightng" ), "inv2_lightng", "GL_LINEAR" );

            for ( var i = 0; i < 5; i++ )
            {
                var s = "inva" + ( i + 1 ).ToString( );

                Weapons[2 + i, 0] = BasePicture.FromWad( _host.Video.Device, _host.Wads.FromTexture( s + "_shotgun" ), s + "_shotgun", "GL_LINEAR" );
                Weapons[2 + i, 1] = BasePicture.FromWad( _host.Video.Device, _host.Wads.FromTexture( s + "_sshotgun" ), s + "_sshotgun", "GL_LINEAR" );
                Weapons[2 + i, 2] = BasePicture.FromWad( _host.Video.Device, _host.Wads.FromTexture( s + "_nailgun" ), s + "_nailgun", "GL_LINEAR" );
                Weapons[2 + i, 3] = BasePicture.FromWad( _host.Video.Device, _host.Wads.FromTexture( s + "_snailgun" ), s + "_snailgun", "GL_LINEAR" );
                Weapons[2 + i, 4] = BasePicture.FromWad( _host.Video.Device, _host.Wads.FromTexture( s + "_rlaunch" ), s + "_rlaunch", "GL_LINEAR" );
                Weapons[2 + i, 5] = BasePicture.FromWad( _host.Video.Device, _host.Wads.FromTexture( s + "_srlaunch" ), s + "_srlaunch", "GL_LINEAR" );
                Weapons[2 + i, 6] = BasePicture.FromWad( _host.Video.Device, _host.Wads.FromTexture( s + "_lightng" ), s + "_lightng", "GL_LINEAR" );
            }
        }

        private void LoadAmmo( )
        {
            Ammo[0] = BasePicture.FromWad( _host.Video.Device, _host.Wads.FromTexture( "anum_minus" ), "sb_shells", "GL_LINEAR" );
            Ammo[1] = BasePicture.FromWad( _host.Video.Device, _host.Wads.FromTexture( "anum_minus" ), "sb_nails", "GL_LINEAR" );
            Ammo[2] = BasePicture.FromWad( _host.Video.Device, _host.Wads.FromTexture( "anum_minus" ), "sb_rocket", "GL_LINEAR" );
            Ammo[3] = BasePicture.FromWad( _host.Video.Device, _host.Wads.FromTexture( "anum_minus" ), "sb_cells", "GL_LINEAR" );
        }

        private void LoadArmour( )
        {
            Armour[0] = BasePicture.FromWad( _host.Video.Device, _host.Wads.FromTexture( "anum_minus" ), "sb_armor1", "GL_LINEAR" );
            Armour[1] = BasePicture.FromWad( _host.Video.Device, _host.Wads.FromTexture( "anum_minus" ), "sb_armor2", "GL_LINEAR" );
            Armour[2] = BasePicture.FromWad( _host.Video.Device, _host.Wads.FromTexture( "anum_minus" ), "sb_armor3", "GL_LINEAR" );
        }

        private void LoadItems( )
        {
            Items[0] = BasePicture.FromWad( _host.Video.Device, _host.Wads.FromTexture( "anum_minus" ), "sb_key1", "GL_LINEAR" );
            Items[1] = BasePicture.FromWad( _host.Video.Device, _host.Wads.FromTexture( "anum_minus" ), "sb_key2", "GL_LINEAR" );
            Items[2] = BasePicture.FromWad( _host.Video.Device, _host.Wads.FromTexture( "anum_minus" ), "sb_invis", "GL_LINEAR" );
            Items[3] = BasePicture.FromWad( _host.Video.Device, _host.Wads.FromTexture( "anum_minus" ), "sb_invuln", "GL_LINEAR" );
            Items[4] = BasePicture.FromWad( _host.Video.Device, _host.Wads.FromTexture( "anum_minus" ), "sb_suit", "GL_LINEAR" );
            Items[5] = BasePicture.FromWad( _host.Video.Device, _host.Wads.FromTexture( "anum_minus" ), "sb_quad", "GL_LINEAR" );
        }

        private void LoadSigil( )
        {
            Sigil[0] = BasePicture.FromWad( _host.Video.Device, _host.Wads.FromTexture( "anum_minus" ), "sb_sigil1", "GL_LINEAR" );
            Sigil[1] = BasePicture.FromWad( _host.Video.Device, _host.Wads.FromTexture( "anum_minus" ), "sb_sigil2", "GL_LINEAR" );
            Sigil[2] = BasePicture.FromWad( _host.Video.Device, _host.Wads.FromTexture( "anum_minus" ), "sb_sigil3", "GL_LINEAR" );
            Sigil[3] = BasePicture.FromWad( _host.Video.Device, _host.Wads.FromTexture( "anum_minus" ), "sb_sigil4", "GL_LINEAR" );
        }

        private void LoadFaces( )
        {
            Faces[4, 0] = BasePicture.FromWad( _host.Video.Device, _host.Wads.FromTexture( "anum_minus" ), "face1", "GL_NEAREST" );
            Faces[4, 1] = BasePicture.FromWad( _host.Video.Device, _host.Wads.FromTexture( "anum_minus" ), "face_p1", "GL_NEAREST" );
            Faces[3, 0] = BasePicture.FromWad( _host.Video.Device, _host.Wads.FromTexture( "anum_minus" ), "face2", "GL_NEAREST" );
            Faces[3, 1] = BasePicture.FromWad( _host.Video.Device, _host.Wads.FromTexture( "anum_minus" ), "face_p2", "GL_NEAREST" );
            Faces[2, 0] = BasePicture.FromWad( _host.Video.Device, _host.Wads.FromTexture( "anum_minus" ), "face3", "GL_NEAREST" );
            Faces[2, 1] = BasePicture.FromWad( _host.Video.Device, _host.Wads.FromTexture( "anum_minus" ), "face_p3", "GL_NEAREST" );
            Faces[1, 0] = BasePicture.FromWad( _host.Video.Device, _host.Wads.FromTexture( "anum_minus" ), "face4", "GL_NEAREST" );
            Faces[1, 1] = BasePicture.FromWad( _host.Video.Device, _host.Wads.FromTexture( "anum_minus" ), "face_p4", "GL_NEAREST" );
            Faces[0, 0] = BasePicture.FromWad( _host.Video.Device, _host.Wads.FromTexture( "anum_minus" ), "face5", "GL_NEAREST" );
            Faces[0, 1] = BasePicture.FromWad( _host.Video.Device, _host.Wads.FromTexture( "anum_minus" ), "face_p5", "GL_NEAREST" );

            FaceInvis = BasePicture.FromWad( _host.Video.Device, _host.Wads.FromTexture( "anum_minus" ), "face_invis", "GL_NEAREST" );
            FaceInvuln = BasePicture.FromWad( _host.Video.Device, _host.Wads.FromTexture( "anum_minus" ), "face_invul2", "GL_NEAREST" );
            FaceInvisInvuln = BasePicture.FromWad( _host.Video.Device, _host.Wads.FromTexture( "anum_minus" ), "face_inv2", "GL_NEAREST" );
            FaceQuad = BasePicture.FromWad( _host.Video.Device, _host.Wads.FromTexture( "anum_minus" ), "face_quad", "GL_NEAREST" );
        }

        private void LoadBars( )
        {
            SBar = BasePicture.FromWad( _host.Video.Device, _host.Wads.FromTexture( "anum_minus" ), "sbar", "GL_NEAREST" );
            IBar = BasePicture.FromWad( _host.Video.Device, _host.Wads.FromTexture( "anum_minus" ), "ibar", "GL_NEAREST" );
            ScoreBar = BasePicture.FromWad( _host.Video.Device, _host.Wads.FromTexture( "anum_minus" ), "scorebar", "GL_LINEAR" );
        }

        private void LoadHipnotic( )
        {
            HWeapons[0, 0] = BasePicture.FromWad( _host.Video.Device, _host.Wads.FromTexture( "anum_minus" ), "inv_laser", "GL_LINEAR" );
            HWeapons[0, 1] = BasePicture.FromWad( _host.Video.Device, _host.Wads.FromTexture( "anum_minus" ), "inv_mjolnir", "GL_LINEAR" );
            HWeapons[0, 2] = BasePicture.FromWad( _host.Video.Device, _host.Wads.FromTexture( "anum_minus" ), "inv_gren_prox", "GL_LINEAR" );
            HWeapons[0, 3] = BasePicture.FromWad( _host.Video.Device, _host.Wads.FromTexture( "anum_minus" ), "inv_prox_gren", "GL_LINEAR" );
            HWeapons[0, 4] = BasePicture.FromWad( _host.Video.Device, _host.Wads.FromTexture( "anum_minus" ), "inv_prox", "GL_LINEAR" );

            HWeapons[1, 0] = BasePicture.FromWad( _host.Video.Device, _host.Wads.FromTexture( "anum_minus" ), "inv2_laser", "GL_LINEAR" );
            HWeapons[1, 1] = BasePicture.FromWad( _host.Video.Device, _host.Wads.FromTexture( "anum_minus" ), "inv2_mjolnir", "GL_LINEAR" );
            HWeapons[1, 2] = BasePicture.FromWad( _host.Video.Device, _host.Wads.FromTexture( "anum_minus" ), "inv2_gren_prox", "GL_LINEAR" );
            HWeapons[1, 3] = BasePicture.FromWad( _host.Video.Device, _host.Wads.FromTexture( "anum_minus" ), "inv2_prox_gren", "GL_LINEAR" );
            HWeapons[1, 4] = BasePicture.FromWad( _host.Video.Device, _host.Wads.FromTexture( "anum_minus" ), "inv2_prox", "GL_LINEAR" );

            for ( var i = 0; i < 5; i++ )
            {
                var s = "inva" + ( i + 1 ).ToString( );
                HWeapons[2 + i, 0] = BasePicture.FromWad( _host.Video.Device, _host.Wads.FromTexture( "anum_minus" ), s + "_laser", "GL_LINEAR" );
                HWeapons[2 + i, 1] = BasePicture.FromWad( _host.Video.Device, _host.Wads.FromTexture( "anum_minus" ), s + "_mjolnir", "GL_LINEAR" );
                HWeapons[2 + i, 2] = BasePicture.FromWad( _host.Video.Device, _host.Wads.FromTexture( "anum_minus" ), s + "_gren_prox", "GL_LINEAR" );
                HWeapons[2 + i, 3] = BasePicture.FromWad( _host.Video.Device, _host.Wads.FromTexture( "anum_minus" ), s + "_prox_gren", "GL_LINEAR" );
                HWeapons[2 + i, 4] = BasePicture.FromWad( _host.Video.Device, _host.Wads.FromTexture( "anum_minus" ), s + "_prox", "GL_LINEAR" );
            }

            HItems[0] = BasePicture.FromWad( _host.Video.Device, _host.Wads.FromTexture( "anum_minus" ), "sb_wsuit", "GL_LINEAR" );
            HItems[1] = BasePicture.FromWad( _host.Video.Device, _host.Wads.FromTexture( "anum_minus" ), "sb_eshld", "GL_LINEAR" );
        }

        private void LoadRogue( )
        {
            RInvBar[0] = BasePicture.FromWad( _host.Video.Device, _host.Wads.FromTexture( "anum_minus" ), "r_invbar1", "GL_LINEAR" );
            RInvBar[1] = BasePicture.FromWad( _host.Video.Device, _host.Wads.FromTexture( "anum_minus" ), "r_invbar2", "GL_LINEAR" );

            RWeapons[0] = BasePicture.FromWad( _host.Video.Device, _host.Wads.FromTexture( "anum_minus" ), "r_lava", "GL_LINEAR" );
            RWeapons[1] = BasePicture.FromWad( _host.Video.Device, _host.Wads.FromTexture( "anum_minus" ), "r_superlava", "GL_LINEAR" );
            RWeapons[2] = BasePicture.FromWad( _host.Video.Device, _host.Wads.FromTexture( "anum_minus" ), "r_gren", "GL_LINEAR" );
            RWeapons[3] = BasePicture.FromWad( _host.Video.Device, _host.Wads.FromTexture( "anum_minus" ), "r_multirock", "GL_LINEAR" );
            RWeapons[4] = BasePicture.FromWad( _host.Video.Device, _host.Wads.FromTexture( "anum_minus" ), "r_plasma", "GL_LINEAR" );

            RItems[0] = BasePicture.FromWad( _host.Video.Device, _host.Wads.FromTexture( "anum_minus" ), "r_shield1", "GL_LINEAR" );
            RItems[1] = BasePicture.FromWad( _host.Video.Device, _host.Wads.FromTexture( "anum_minus" ), "r_agrav1", "GL_LINEAR" );

            // PGM 01/19/97 - team color border
            RTeamBord = BasePicture.FromWad( _host.Video.Device, _host.Wads.FromTexture( "anum_minus" ), "r_teambord", "GL_LINEAR" );
            // PGM 01/19/97 - team color border

            RAmmo[0] = BasePicture.FromWad( _host.Video.Device, _host.Wads.FromTexture( "anum_minus" ), "r_ammolava", "GL_LINEAR" );
            RAmmo[1] = BasePicture.FromWad( _host.Video.Device, _host.Wads.FromTexture( "anum_minus" ), "r_ammomulti", "GL_LINEAR" );
            RAmmo[2] = BasePicture.FromWad( _host.Video.Device, _host.Wads.FromTexture( "anum_minus" ), "r_ammoplasma", "GL_LINEAR" );
        }

        public void Initialise( )
        {
            LoadNumbers( );
            LoadSymbols( );
            LoadWeapons( );
            LoadAmmo( );
            LoadArmour( );
            LoadItems( );
            LoadSigil( );
            LoadFaces( );
            LoadBars( );

            //MED 01/04/97 added new hipnotic weapons
            if ( MainWindow.Common.GameKind == GameKind.Hipnotic )
                LoadHipnotic( );

            if ( MainWindow.Common.GameKind == GameKind.Rogue )
                LoadRogue( );
        }

        /// <summary>
        /// Sbar_DrawPic
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="pic"></param>
        public void DrawPic( Int32 x, Int32 y, BasePicture pic )
        {
            if ( _host.Client.cl.gametype == ProtocolDef.GAME_DEATHMATCH )
                _host.Video.Device.Graphics.DrawPicture( pic, x, y + ( _host.Screen.vid.height - SBAR_HEIGHT ) );
            else
                _host.Video.Device.Graphics.DrawPicture( pic, x + ( ( _host.Screen.vid.width - 320 ) >> 1 ), y + ( _host.Screen.vid.height - SBAR_HEIGHT ) );
        }

        /// <summary>
        /// Sbar_DrawString
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="str"></param>
        public void DrawString( Int32 x, Int32 y, String str )
        {
            if ( _host.Client.cl.gametype == ProtocolDef.GAME_DEATHMATCH )
                _host.DrawingContext.DrawString( x, y + _host.Screen.vid.height - SBAR_HEIGHT, str );
            else
                _host.DrawingContext.DrawString( x + ( ( _host.Screen.vid.width - 320 ) >> 1 ), y + _host.Screen.vid.height - SBAR_HEIGHT, str );
        }

        /// <summary>
        /// Sbar_DrawCharacter
        /// </summary>
        /// <remarks>
        /// Draws one solid graphics character
        /// </remarks>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="num"></param>
        public void DrawCharacter( Int32 x, Int32 y, Int32 num )
        {
            if ( _host.Client.cl.gametype == ProtocolDef.GAME_DEATHMATCH )
                _host.DrawingContext.DrawCharacter( x + 4, y + _host.Screen.vid.height - SBAR_HEIGHT, num );
            else
                _host.DrawingContext.DrawCharacter( x + ( ( _host.Screen.vid.width - 320 ) >> 1 ) + 4, y + _host.Screen.vid.height - SBAR_HEIGHT, num );
        }

        /// <summary>
        /// Sbar_DrawTransPic
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="picture"></param>
        public void DrawTransPic( Int32 x, Int32 y, BasePicture picture )
        {
            if ( _host.Client.cl.gametype == ProtocolDef.GAME_DEATHMATCH )
                _host.Video.Device.Graphics.DrawPicture( picture, x, y + ( _host.Screen.vid.height - SBAR_HEIGHT ), hasAlpha: true );
            else
                _host.Video.Device.Graphics.DrawPicture( picture, x + ( ( _host.Screen.vid.width - 320 ) >> 1 ), y + ( _host.Screen.vid.height - SBAR_HEIGHT ), hasAlpha: true );
        }


        // Sbar_DrawNum
        public void DrawNum( Int32 x, Int32 y, Int32 num, Int32 digits, Int32 color )
        {
            var str = num.ToString( );// int l = Sbar_itoa(num, str);

            if ( str.Length > digits )
                str = str.Remove( str.Length - digits );
            if ( str.Length < digits )
                x += ( digits - str.Length ) * 24;

            for ( Int32 i = 0, frame; i < str.Length; i++ )
            {
                if ( str[i] == '-' )
                    frame = STAT_MINUS;
                else
                    frame = str[i] - '0';

                DrawTransPic( x, y, Numbers[color, frame] );
                x += 24;
            }
        }

        /// <summary>
        /// Sbar_ColorForMap
        /// </summary>
        /// <param name="m"></param>
        /// <returns></returns>
        public Int32 ColorForMap( Int32 m )
        {
            return m < 128 ? m + 8 : m + 8;
        }

        // Sbar_SortFrags
        public void SortFrags( )
        {
            var cl = _host.Client.cl;

            // sort by frags
            _ScoreBoardLines = 0;
            for ( var i = 0; i < cl.maxclients; i++ )
            {
                if ( !String.IsNullOrEmpty( cl.scores[i].name ) )
                {
                    _FragSort[_ScoreBoardLines] = i;
                    _ScoreBoardLines++;
                }
            }

            for ( var i = 0; i < _ScoreBoardLines; i++ )
            {
                for ( var j = 0; j < _ScoreBoardLines - 1 - i; j++ )
                    if ( cl.scores[_FragSort[j]].frags < cl.scores[_FragSort[j + 1]].frags )
                    {
                        var k = _FragSort[j];
                        _FragSort[j] = _FragSort[j + 1];
                        _FragSort[j + 1] = k;
                    }
            }
        }
    }
}
