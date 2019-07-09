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
using System.Text;
using SharpQuake.Framework;
using SharpQuake.Renderer.Textures;

// sbar.h

// the status bar is only redrawn if something has changed, but if anything
// does, the entire thing will be redrawn for the next vid.numpages frames.

namespace SharpQuake
{
    /// <summary>
    /// Sbar_functions
    /// </summary>
    public class sbar
    {
        public Int32 Lines
        {
            get; set;
        }

        public const Int32 SBAR_HEIGHT = 24;

        private const Int32 STAT_MINUS = 10;
        private Int32 _Updates; // sb_updates		// if >= vid.numpages, no update needed
        private Boolean _ShowScores; // sb_showscores

        // num frame for '-' stats digit

        private BasePicture[,] Numbers = new BasePicture[2, 11];

        private BasePicture Colon;
        private BasePicture Slash;
        private BasePicture IBar;
        private BasePicture SBar;
        private BasePicture ScoreBar;

        private BasePicture[,] Weapons = new BasePicture[7, 8];   // 0 is active, 1 is owned, 2-5 are flashes
        private BasePicture[] Ammo = new BasePicture[4];
        private BasePicture[] Sigil = new BasePicture[4];
        private BasePicture[] Armour = new BasePicture[3];
        private BasePicture[] Items = new BasePicture[32];

        private BasePicture[,] Faces = new BasePicture[7, 2];        // 0 is gibbed, 1 is dead, 2-6 are alive

        // 0 is static, 1 is temporary animation

        private BasePicture FaceInvis;

        private BasePicture FaceQuad;
        private BasePicture FaceInvuln;
        private BasePicture FaceInvisInvuln;

        private BasePicture[] RInvBar = new BasePicture[2];
        private BasePicture[] RWeapons = new BasePicture[5];
        private BasePicture[] RItems = new BasePicture[2];
        private BasePicture[] RAmmo = new BasePicture[3];
        private BasePicture RTeamBord;      // PGM 01/19/97 - team color border

        //MED 01/04/97 added two more weapons + 3 alternates for grenade launcher
        private BasePicture[,] HWeapons = new BasePicture[7, 5];   // 0 is active, 1 is owned, 2-5 are flashes

        //MED 01/04/97 added array to simplify weapon parsing
        private Int32[] _HipWeapons = new Int32[]
        {
            QItemsDef.HIT_LASER_CANNON_BIT, QItemsDef.HIT_MJOLNIR_BIT, 4, QItemsDef.HIT_PROXIMITY_GUN_BIT
        };

        //MED 01/04/97 added hipnotic items array
        private BasePicture[] HItems = new BasePicture[2];

        private Int32[] _FragSort = new Int32[QDef.MAX_SCOREBOARD];
        private String[] _ScoreBoardText = new String[QDef.MAX_SCOREBOARD];
        private Int32[] _ScoreBoardTop = new Int32[QDef.MAX_SCOREBOARD];
        private Int32[] _ScoreBoardBottom = new Int32[QDef.MAX_SCOREBOARD];
        private Int32[] _ScoreBoardCount = new Int32[QDef.MAX_SCOREBOARD];
        private Int32 _ScoreBoardLines;

        // CHANGE
        private Host Host
        {
            get;
            set;
        }
        // sb_lines scan lines to draw

        public sbar( Host host )
        {
            Host = host;
        }

        // Sbar_Init
        public void Initialise( )
        {
            for ( var i = 0; i < 10; i++ )
            {
                var str = i.ToString( );

                Numbers[0, i] = BasePicture.FromWad( Host.Video.Device, Host.GfxWad, "num_" + str, "GL_NEAREST" );
                Numbers[1, i] = BasePicture.FromWad( Host.Video.Device, Host.GfxWad, "anum_" + str, "GL_NEAREST" );
            }

            Numbers[0, 10] = BasePicture.FromWad( Host.Video.Device, Host.GfxWad, "num_minus", "GL_NEAREST" );
            Numbers[1, 10] = BasePicture.FromWad( Host.Video.Device, Host.GfxWad, "anum_minus", "GL_NEAREST" );

            Colon = BasePicture.FromWad( Host.Video.Device, Host.GfxWad, "num_colon", "GL_NEAREST" );
            Slash = BasePicture.FromWad( Host.Video.Device, Host.GfxWad, "num_slash", "GL_NEAREST" );

            Weapons[0, 0] = BasePicture.FromWad( Host.Video.Device, Host.GfxWad, "inv_shotgun", "GL_LINEAR" );
            Weapons[0, 1] = BasePicture.FromWad( Host.Video.Device, Host.GfxWad, "inv_sshotgun", "GL_LINEAR" );
            Weapons[0, 2] = BasePicture.FromWad( Host.Video.Device, Host.GfxWad, "inv_nailgun", "GL_LINEAR" );
            Weapons[0, 3] = BasePicture.FromWad( Host.Video.Device, Host.GfxWad, "inv_snailgun", "GL_LINEAR" );
            Weapons[0, 4] = BasePicture.FromWad( Host.Video.Device, Host.GfxWad, "inv_rlaunch", "GL_LINEAR" );
            Weapons[0, 5] = BasePicture.FromWad( Host.Video.Device, Host.GfxWad, "inv_srlaunch", "GL_LINEAR" );
            Weapons[0, 6] = BasePicture.FromWad( Host.Video.Device, Host.GfxWad, "inv_lightng", "GL_LINEAR" );

            Weapons[1, 0] = BasePicture.FromWad( Host.Video.Device, Host.GfxWad, "inv2_shotgun", "GL_LINEAR" );
            Weapons[1, 1] = BasePicture.FromWad( Host.Video.Device, Host.GfxWad, "inv2_sshotgun", "GL_LINEAR" );
            Weapons[1, 2] = BasePicture.FromWad( Host.Video.Device, Host.GfxWad, "inv2_nailgun", "GL_LINEAR" );
            Weapons[1, 3] = BasePicture.FromWad( Host.Video.Device, Host.GfxWad, "inv2_snailgun", "GL_LINEAR" );
            Weapons[1, 4] = BasePicture.FromWad( Host.Video.Device, Host.GfxWad, "inv2_rlaunch", "GL_LINEAR" );
            Weapons[1, 5] = BasePicture.FromWad( Host.Video.Device, Host.GfxWad, "inv2_srlaunch", "GL_LINEAR" );
            Weapons[1, 6] = BasePicture.FromWad( Host.Video.Device, Host.GfxWad, "inv2_lightng", "GL_LINEAR" );

            for ( var i = 0; i < 5; i++ )
            {
                var s = "inva" + ( i + 1 ).ToString( );

                Weapons[2 + i, 0] = BasePicture.FromWad( Host.Video.Device, Host.GfxWad, s + "_shotgun", "GL_LINEAR" );
                Weapons[2 + i, 1] = BasePicture.FromWad( Host.Video.Device, Host.GfxWad, s + "_sshotgun", "GL_LINEAR" );
                Weapons[2 + i, 2] = BasePicture.FromWad( Host.Video.Device, Host.GfxWad, s + "_nailgun", "GL_LINEAR" );
                Weapons[2 + i, 3] = BasePicture.FromWad( Host.Video.Device, Host.GfxWad, s + "_snailgun", "GL_LINEAR" );
                Weapons[2 + i, 4] = BasePicture.FromWad( Host.Video.Device, Host.GfxWad, s + "_rlaunch", "GL_LINEAR" );
                Weapons[2 + i, 5] = BasePicture.FromWad( Host.Video.Device, Host.GfxWad, s + "_srlaunch", "GL_LINEAR" );
                Weapons[2 + i, 6] = BasePicture.FromWad( Host.Video.Device, Host.GfxWad, s + "_lightng", "GL_LINEAR" );
            }

            Ammo[0] = BasePicture.FromWad( Host.Video.Device, Host.GfxWad, "sb_shells", "GL_LINEAR" );
            Ammo[1] = BasePicture.FromWad( Host.Video.Device, Host.GfxWad, "sb_nails", "GL_LINEAR" );
            Ammo[2] = BasePicture.FromWad( Host.Video.Device, Host.GfxWad, "sb_rocket", "GL_LINEAR" );
            Ammo[3] = BasePicture.FromWad( Host.Video.Device, Host.GfxWad, "sb_cells", "GL_LINEAR" );

            Armour[0] = BasePicture.FromWad( Host.Video.Device, Host.GfxWad, "sb_armor1", "GL_LINEAR" );
            Armour[1] = BasePicture.FromWad( Host.Video.Device, Host.GfxWad, "sb_armor2", "GL_LINEAR" );
            Armour[2] = BasePicture.FromWad( Host.Video.Device, Host.GfxWad, "sb_armor3", "GL_LINEAR" );

            Items[0] = BasePicture.FromWad( Host.Video.Device, Host.GfxWad, "sb_key1", "GL_LINEAR" );
            Items[1] = BasePicture.FromWad( Host.Video.Device, Host.GfxWad, "sb_key2", "GL_LINEAR" );
            Items[2] = BasePicture.FromWad( Host.Video.Device, Host.GfxWad, "sb_invis", "GL_LINEAR" );
            Items[3] = BasePicture.FromWad( Host.Video.Device, Host.GfxWad, "sb_invuln", "GL_LINEAR" );
            Items[4] = BasePicture.FromWad( Host.Video.Device, Host.GfxWad, "sb_suit", "GL_LINEAR" );
            Items[5] = BasePicture.FromWad( Host.Video.Device, Host.GfxWad, "sb_quad", "GL_LINEAR" );

            Sigil[0] = BasePicture.FromWad( Host.Video.Device, Host.GfxWad, "sb_sigil1", "GL_LINEAR" );
            Sigil[1] = BasePicture.FromWad( Host.Video.Device, Host.GfxWad, "sb_sigil2", "GL_LINEAR" );
            Sigil[2] = BasePicture.FromWad( Host.Video.Device, Host.GfxWad, "sb_sigil3", "GL_LINEAR" );
            Sigil[3] = BasePicture.FromWad( Host.Video.Device, Host.GfxWad, "sb_sigil4", "GL_LINEAR" );

            Faces[4, 0] = BasePicture.FromWad( Host.Video.Device, Host.GfxWad, "face1", "GL_NEAREST" );
            Faces[4, 1] = BasePicture.FromWad( Host.Video.Device, Host.GfxWad, "face_p1", "GL_NEAREST" );
            Faces[3, 0] = BasePicture.FromWad( Host.Video.Device, Host.GfxWad, "face2", "GL_NEAREST" );
            Faces[3, 1] = BasePicture.FromWad( Host.Video.Device, Host.GfxWad, "face_p2", "GL_NEAREST" );
            Faces[2, 0] = BasePicture.FromWad( Host.Video.Device, Host.GfxWad, "face3", "GL_NEAREST" );
            Faces[2, 1] = BasePicture.FromWad( Host.Video.Device, Host.GfxWad, "face_p3", "GL_NEAREST" );
            Faces[1, 0] = BasePicture.FromWad( Host.Video.Device, Host.GfxWad, "face4", "GL_NEAREST" );
            Faces[1, 1] = BasePicture.FromWad( Host.Video.Device, Host.GfxWad, "face_p4", "GL_NEAREST" );
            Faces[0, 0] = BasePicture.FromWad( Host.Video.Device, Host.GfxWad, "face5", "GL_NEAREST" );
            Faces[0, 1] = BasePicture.FromWad( Host.Video.Device, Host.GfxWad, "face_p5", "GL_NEAREST" );

            FaceInvis = BasePicture.FromWad( Host.Video.Device, Host.GfxWad, "face_invis", "GL_NEAREST" );
            FaceInvuln = BasePicture.FromWad( Host.Video.Device, Host.GfxWad, "face_invul2", "GL_NEAREST" );
            FaceInvisInvuln = BasePicture.FromWad( Host.Video.Device, Host.GfxWad, "face_inv2", "GL_NEAREST" );
            FaceQuad = BasePicture.FromWad( Host.Video.Device, Host.GfxWad, "face_quad", "GL_NEAREST" );

            Host.Command.Add( "+showscores", ShowScores );
            Host.Command.Add( "-showscores", DontShowScores );

            SBar = BasePicture.FromWad( Host.Video.Device, Host.GfxWad, "sbar", "GL_NEAREST" );
            IBar = BasePicture.FromWad( Host.Video.Device, Host.GfxWad, "ibar", "GL_NEAREST" );
            ScoreBar = BasePicture.FromWad( Host.Video.Device, Host.GfxWad, "scorebar", "GL_LINEAR" );

            //MED 01/04/97 added new hipnotic weapons
            if ( MainWindow.Common.GameKind == GameKind.Hipnotic )
            {
                HWeapons[0, 0] = BasePicture.FromWad( Host.Video.Device, Host.GfxWad, "inv_laser", "GL_LINEAR" );
                HWeapons[0, 1] = BasePicture.FromWad( Host.Video.Device, Host.GfxWad, "inv_mjolnir", "GL_LINEAR" );
                HWeapons[0, 2] = BasePicture.FromWad( Host.Video.Device, Host.GfxWad, "inv_gren_prox", "GL_LINEAR" );
                HWeapons[0, 3] = BasePicture.FromWad( Host.Video.Device, Host.GfxWad, "inv_prox_gren", "GL_LINEAR" );
                HWeapons[0, 4] = BasePicture.FromWad( Host.Video.Device, Host.GfxWad, "inv_prox", "GL_LINEAR" );

                HWeapons[1, 0] = BasePicture.FromWad( Host.Video.Device, Host.GfxWad, "inv2_laser", "GL_LINEAR" );
                HWeapons[1, 1] = BasePicture.FromWad( Host.Video.Device, Host.GfxWad, "inv2_mjolnir", "GL_LINEAR" );
                HWeapons[1, 2] = BasePicture.FromWad( Host.Video.Device, Host.GfxWad, "inv2_gren_prox", "GL_LINEAR" );
                HWeapons[1, 3] = BasePicture.FromWad( Host.Video.Device, Host.GfxWad, "inv2_prox_gren", "GL_LINEAR" );
                HWeapons[1, 4] = BasePicture.FromWad( Host.Video.Device, Host.GfxWad, "inv2_prox", "GL_LINEAR" );

                for ( var i = 0; i < 5; i++ )
                {
                    var s = "inva" + ( i + 1 ).ToString( );
                    HWeapons[2 + i, 0] = BasePicture.FromWad( Host.Video.Device, Host.GfxWad, s + "_laser", "GL_LINEAR" );
                    HWeapons[2 + i, 1] = BasePicture.FromWad( Host.Video.Device, Host.GfxWad, s + "_mjolnir", "GL_LINEAR" );
                    HWeapons[2 + i, 2] = BasePicture.FromWad( Host.Video.Device, Host.GfxWad, s + "_gren_prox", "GL_LINEAR" );
                    HWeapons[2 + i, 3] = BasePicture.FromWad( Host.Video.Device, Host.GfxWad, s + "_prox_gren", "GL_LINEAR" );
                    HWeapons[2 + i, 4] = BasePicture.FromWad( Host.Video.Device, Host.GfxWad, s + "_prox", "GL_LINEAR" );
                }

                HItems[0] = BasePicture.FromWad( Host.Video.Device, Host.GfxWad, "sb_wsuit", "GL_LINEAR" );
                HItems[1] = BasePicture.FromWad( Host.Video.Device, Host.GfxWad, "sb_eshld", "GL_LINEAR" );
            }

            if ( MainWindow.Common.GameKind == GameKind.Rogue )
            {
                RInvBar[0] = BasePicture.FromWad( Host.Video.Device, Host.GfxWad, "r_invbar1", "GL_LINEAR" );
                RInvBar[1] = BasePicture.FromWad( Host.Video.Device, Host.GfxWad, "r_invbar2", "GL_LINEAR" );

                RWeapons[0] = BasePicture.FromWad( Host.Video.Device, Host.GfxWad, "r_lava", "GL_LINEAR" );
                RWeapons[1] = BasePicture.FromWad( Host.Video.Device, Host.GfxWad, "r_superlava", "GL_LINEAR" );
                RWeapons[2] = BasePicture.FromWad( Host.Video.Device, Host.GfxWad, "r_gren", "GL_LINEAR" );
                RWeapons[3] = BasePicture.FromWad( Host.Video.Device, Host.GfxWad, "r_multirock", "GL_LINEAR" );
                RWeapons[4] = BasePicture.FromWad( Host.Video.Device, Host.GfxWad, "r_plasma", "GL_LINEAR" );

                RItems[0] = BasePicture.FromWad( Host.Video.Device, Host.GfxWad, "r_shield1", "GL_LINEAR" );
                RItems[1] = BasePicture.FromWad( Host.Video.Device, Host.GfxWad, "r_agrav1", "GL_LINEAR" );

                // PGM 01/19/97 - team color border
                RTeamBord = BasePicture.FromWad( Host.Video.Device, Host.GfxWad, "r_teambord", "GL_LINEAR" );
                // PGM 01/19/97 - team color border

                RAmmo[0] = BasePicture.FromWad( Host.Video.Device, Host.GfxWad, "r_ammolava", "GL_LINEAR" );
                RAmmo[1] = BasePicture.FromWad( Host.Video.Device, Host.GfxWad, "r_ammomulti", "GL_LINEAR" );
                RAmmo[2] = BasePicture.FromWad( Host.Video.Device, Host.GfxWad, "r_ammoplasma", "GL_LINEAR" );
            }
        }

        // Sbar_Changed
        // call whenever any of the client stats represented on the sbar changes
        public void Changed( )
        {
            _Updates = 0;	// update next frame
        }

        // Sbar_Draw
        // called every frame by screen
        public void Draw( )
        {
            if ( Host == null )
                return;

            var vid = Host.Screen.vid;
            if ( Host.Screen.ConCurrent == vid.height )
                return;		// console is full screen

            if ( _Updates >= vid.numpages )
                return;

            Host.Screen.CopyEverithing = true;

            _Updates++;

            if ( Lines > 0 && vid.width > 320 )
                Host.DrawingContext.TileClear( 0, vid.height - Lines, vid.width, Lines );

            if ( Lines > 24 )
            {
                DrawInventory( );
                if ( Host.Client.cl.maxclients != 1 )
                    DrawFrags( );
            }

            var cl = Host.Client.cl;
            if ( _ShowScores || cl.stats[QStatsDef.STAT_HEALTH] <= 0 )
            {
                DrawPic( 0, 0, ScoreBar );
                DrawScoreboard( );
                _Updates = 0;
            }
            else if ( Lines > 0 )
            {
                DrawPic( 0, 0, SBar );

                // keys (hipnotic only)
                //MED 01/04/97 moved keys here so they would not be overwritten
                if ( MainWindow.Common.GameKind == GameKind.Hipnotic )
                {
                    if ( cl.HasItems( QItemsDef.IT_KEY1 ) )
                        DrawPic( 209, 3, Items[0] );
                    if ( cl.HasItems( QItemsDef.IT_KEY2 ) )
                        DrawPic( 209, 12, Items[1] );
                }
                // armor
                if ( cl.HasItems( QItemsDef.IT_INVULNERABILITY ) )
                {
                    DrawNum( 24, 0, 666, 3, 1 );
                    Host.Video.Device.Graphics.DrawPicture( Host.DrawingContext.Disc, 0, 0 );
                }
                else
                {
                    if ( MainWindow.Common.GameKind == GameKind.Rogue )
                    {
                        DrawNum( 24, 0, cl.stats[QStatsDef.STAT_ARMOR], 3, cl.stats[QStatsDef.STAT_ARMOR] <= 25 ? 1 : 0 ); // uze: corrected color param
                        if ( cl.HasItems( QItemsDef.RIT_ARMOR3 ) )
                            DrawPic( 0, 0, Armour[2] );
                        else if ( cl.HasItems( QItemsDef.RIT_ARMOR2 ) )
                            DrawPic( 0, 0, Armour[1] );
                        else if ( cl.HasItems( QItemsDef.RIT_ARMOR1 ) )
                            DrawPic( 0, 0, Armour[0] );
                    }
                    else
                    {
                        DrawNum( 24, 0, cl.stats[QStatsDef.STAT_ARMOR], 3, cl.stats[QStatsDef.STAT_ARMOR] <= 25 ? 1 : 0 );
                        if ( cl.HasItems( QItemsDef.IT_ARMOR3 ) )
                            DrawPic( 0, 0, Armour[2] );
                        else if ( cl.HasItems( QItemsDef.IT_ARMOR2 ) )
                            DrawPic( 0, 0, Armour[1] );
                        else if ( cl.HasItems( QItemsDef.IT_ARMOR1 ) )
                            DrawPic( 0, 0, Armour[0] );
                    }
                }

                // face
                DrawFace( );

                // health
                DrawNum( 136, 0, cl.stats[QStatsDef.STAT_HEALTH], 3, cl.stats[QStatsDef.STAT_HEALTH] <= 25 ? 1 : 0 );

                // ammo icon
                if ( MainWindow.Common.GameKind == GameKind.Rogue )
                {
                    if ( cl.HasItems( QItemsDef.RIT_SHELLS ) )
                        DrawPic( 224, 0, Ammo[0] );
                    else if ( cl.HasItems( QItemsDef.RIT_NAILS ) )
                        DrawPic( 224, 0, Ammo[1] );
                    else if ( cl.HasItems( QItemsDef.RIT_ROCKETS ) )
                        DrawPic( 224, 0, Ammo[2] );
                    else if ( cl.HasItems( QItemsDef.RIT_CELLS ) )
                        DrawPic( 224, 0, Ammo[3] );
                    else if ( cl.HasItems( QItemsDef.RIT_LAVA_NAILS ) )
                        DrawPic( 224, 0, RAmmo[0] );
                    else if ( cl.HasItems( QItemsDef.RIT_PLASMA_AMMO ) )
                        DrawPic( 224, 0, RAmmo[1] );
                    else if ( cl.HasItems( QItemsDef.RIT_MULTI_ROCKETS ) )
                        DrawPic( 224, 0, RAmmo[2] );
                }
                else
                {
                    if ( cl.HasItems( QItemsDef.IT_SHELLS ) )
                        DrawPic( 224, 0, Ammo[0] );
                    else if ( cl.HasItems( QItemsDef.IT_NAILS ) )
                        DrawPic( 224, 0, Ammo[1] );
                    else if ( cl.HasItems( QItemsDef.IT_ROCKETS ) )
                        DrawPic( 224, 0, Ammo[2] );
                    else if ( cl.HasItems( QItemsDef.IT_CELLS ) )
                        DrawPic( 224, 0, Ammo[3] );
                }

                DrawNum( 248, 0, cl.stats[QStatsDef.STAT_AMMO], 3, cl.stats[QStatsDef.STAT_AMMO] <= 10 ? 1 : 0 );
            }

            if ( vid.width > 320 )
            {
                if ( Host.Client.cl.gametype == ProtocolDef.GAME_DEATHMATCH )
                    MiniDeathmatchOverlay( );
            }
        }

        /// <summary>
        /// Sbar_IntermissionOverlay
        /// called each frame after the level has been completed
        /// </summary>
        public void IntermissionOverlay( )
        {
            Host.Screen.CopyEverithing = true;
            Host.Screen.FullUpdate = 0;

            if ( Host.Client.cl.gametype == ProtocolDef.GAME_DEATHMATCH )
            {
                DeathmatchOverlay( );
                return;
            }

            var pic = Host.DrawingContext.CachePic( "gfx/complete.lmp", "GL_LINEAR" );
            Host.Video.Device.Graphics.DrawPicture( pic, 64, 24 );

            pic = Host.DrawingContext.CachePic( "gfx/inter.lmp", "GL_LINEAR" );
            Host.Video.Device.Graphics.DrawPicture( pic, 0, 56, hasAlpha: true );

            // time
            var dig = Host.Client.cl.completed_time / 60;
            IntermissionNumber( 160, 64, dig, 3, 0 );
            var num = Host.Client.cl.completed_time - dig * 60;

            Host.Video.Device.Graphics.DrawPicture( Colon, 234, 64, hasAlpha: true );

            Host.Video.Device.Graphics.DrawPicture( Numbers[0, num / 10], 246, 64, hasAlpha: true );
            Host.Video.Device.Graphics.DrawPicture( Numbers[0, num % 10], 266, 64, hasAlpha: true );

            IntermissionNumber( 160, 104, Host.Client.cl.stats[QStatsDef.STAT_SECRETS], 3, 0 );
            Host.Video.Device.Graphics.DrawPicture( Slash, 232, 104, hasAlpha: true );
            IntermissionNumber( 240, 104, Host.Client.cl.stats[QStatsDef.STAT_TOTALSECRETS], 3, 0 );

            IntermissionNumber( 160, 144, Host.Client.cl.stats[QStatsDef.STAT_MONSTERS], 3, 0 );
            Host.Video.Device.Graphics.DrawPicture( Slash, 232, 144, hasAlpha: true );
            IntermissionNumber( 240, 144, Host.Client.cl.stats[QStatsDef.STAT_TOTALMONSTERS], 3, 0 );
        }

        /// <summary>
        /// Sbar_FinaleOverlay
        /// </summary>
        public void FinaleOverlay( )
        {
            Host.Screen.CopyEverithing = true;

            var pic = Host.DrawingContext.CachePic( "gfx/finale.lmp", "GL_LINEAR" );
            Host.Video.Device.Graphics.DrawPicture( pic, ( Host.Screen.vid.width - pic.Width ) / 2, 16, hasAlpha: true );
        }

        /// <summary>
        /// Sbar_IntermissionNumber
        /// </summary>
        private void IntermissionNumber( Int32 x, Int32 y, Int32 num, Int32 digits, Int32 color )
        {
            var str = num.ToString( );
            if ( str.Length > digits )
            {
                str = str.Remove( 0, str.Length - digits );
            }

            if ( str.Length < digits )
                x += ( digits - str.Length ) * 24;

            for ( var i = 0; i < str.Length; i++ )
            {
                var frame = ( str[i] == '-' ? STAT_MINUS : str[i] - '0' );

                Host.Video.Device.Graphics.DrawPicture( Numbers[color, frame], x, y, hasAlpha: true );

                //Host.DrawingContext.DrawTransPic( x, y, _Nums[color, frame] );
                x += 24;
            }
        }

        // Sbar_DrawInventory
        private void DrawInventory( )
        {
            Int32 flashon;

            var cl = Host.Client.cl;
            if ( MainWindow.Common.GameKind == GameKind.Rogue )
            {
                if ( cl.stats[QStatsDef.STAT_ACTIVEWEAPON] >= QItemsDef.RIT_LAVA_NAILGUN )
                    DrawPic( 0, -24, RInvBar[0] );
                else
                    DrawPic( 0, -24, RInvBar[1] );
            }
            else
                DrawPic( 0, -24, IBar );

            // weapons
            for ( var i = 0; i < 7; i++ )
            {
                if ( cl.HasItems( QItemsDef.IT_SHOTGUN << i ) )
                {
                    var time = cl.item_gettime[i];
                    flashon = ( Int32 ) ( ( cl.time - time ) * 10 );
                    if ( flashon >= 10 )
                    {
                        if ( cl.stats[QStatsDef.STAT_ACTIVEWEAPON] == ( QItemsDef.IT_SHOTGUN << i ) )
                            flashon = 1;
                        else
                            flashon = 0;
                    }
                    else
                        flashon = ( flashon % 5 ) + 2;

                    DrawPic( i * 24, -16, Weapons[flashon, i] );

                    if ( flashon > 1 )
                        _Updates = 0; // force update to remove flash
                }
            }

            // MED 01/04/97
            // hipnotic weapons
            if ( MainWindow.Common.GameKind == GameKind.Hipnotic )
            {
                var grenadeflashing = 0;
                for ( var i = 0; i < 4; i++ )
                {
                    if ( cl.HasItems( 1 << _HipWeapons[i] ) )
                    {
                        var time = cl.item_gettime[_HipWeapons[i]];
                        flashon = ( Int32 ) ( ( cl.time - time ) * 10 );
                        if ( flashon >= 10 )
                        {
                            if ( cl.stats[QStatsDef.STAT_ACTIVEWEAPON] == ( 1 << _HipWeapons[i] ) )
                                flashon = 1;
                            else
                                flashon = 0;
                        }
                        else
                            flashon = ( flashon % 5 ) + 2;

                        // check grenade launcher
                        if ( i == 2 )
                        {
                            if ( cl.HasItems( QItemsDef.HIT_PROXIMITY_GUN ) )
                            {
                                if ( flashon > 0 )
                                {
                                    grenadeflashing = 1;
                                    DrawPic( 96, -16, HWeapons[flashon, 2] );
                                }
                            }
                        }
                        else if ( i == 3 )
                        {
                            if ( cl.HasItems( QItemsDef.IT_SHOTGUN << 4 ) )
                            {
                                if ( flashon > 0 && grenadeflashing == 0 )
                                {
                                    DrawPic( 96, -16, HWeapons[flashon, 3] );
                                }
                                else if ( grenadeflashing == 0 )
                                {
                                    DrawPic( 96, -16, HWeapons[0, 3] );
                                }
                            }
                            else
                                DrawPic( 96, -16, HWeapons[flashon, 4] );
                        }
                        else
                            DrawPic( 176 + ( i * 24 ), -16, HWeapons[flashon, i] );
                        if ( flashon > 1 )
                            _Updates = 0; // force update to remove flash
                    }
                }
            }

            if ( MainWindow.Common.GameKind == GameKind.Rogue )
            {
                // check for powered up weapon.
                if ( cl.stats[QStatsDef.STAT_ACTIVEWEAPON] >= QItemsDef.RIT_LAVA_NAILGUN )
                    for ( var i = 0; i < 5; i++ )
                        if ( cl.stats[QStatsDef.STAT_ACTIVEWEAPON] == ( QItemsDef.RIT_LAVA_NAILGUN << i ) )
                            DrawPic( ( i + 2 ) * 24, -16, RWeapons[i] );
            }

            // ammo counts
            for ( var i = 0; i < 4; i++ )
            {
                var num = cl.stats[QStatsDef.STAT_SHELLS + i].ToString( ).PadLeft( 3 );
                //sprintf(num, "%3i", cl.stats[QStats.STAT_SHELLS + i]);
                if ( num[0] != ' ' )
                    DrawCharacter( ( 6 * i + 1 ) * 8 - 2, -24, 18 + num[0] - '0' );
                if ( num[1] != ' ' )
                    DrawCharacter( ( 6 * i + 2 ) * 8 - 2, -24, 18 + num[1] - '0' );
                if ( num[2] != ' ' )
                    DrawCharacter( ( 6 * i + 3 ) * 8 - 2, -24, 18 + num[2] - '0' );
            }

            flashon = 0;
            // items
            for ( var i = 0; i < 6; i++ )
            {
                if ( cl.HasItems( 1 << ( 17 + i ) ) )
                {
                    var time = cl.item_gettime[17 + i];
                    if ( time > 0 && time > cl.time - 2 && flashon > 0 )
                    {  // flash frame
                        _Updates = 0;
                    }
                    else
                    {
                        //MED 01/04/97 changed keys
                        if ( MainWindow.Common.GameKind != GameKind.Hipnotic || ( i > 1 ) )
                        {
                            DrawPic( 192 + i * 16, -16, Items[i] );
                        }
                    }
                    if ( time > 0 && time > cl.time - 2 )
                        _Updates = 0;
                }
            }

            //MED 01/04/97 added hipnotic items
            // hipnotic items
            if ( MainWindow.Common.GameKind == GameKind.Hipnotic )
            {
                for ( var i = 0; i < 2; i++ )
                {
                    if ( cl.HasItems( 1 << ( 24 + i ) ) )
                    {
                        var time = cl.item_gettime[24 + i];
                        if ( time > 0 && time > cl.time - 2 && flashon > 0 )
                        {  // flash frame
                            _Updates = 0;
                        }
                        else
                        {
                            DrawPic( 288 + i * 16, -16, HItems[i] );
                        }
                        if ( time > 0 && time > cl.time - 2 )
                            _Updates = 0;
                    }
                }
            }

            if ( MainWindow.Common.GameKind == GameKind.Rogue )
            {
                // new rogue items
                for ( var i = 0; i < 2; i++ )
                {
                    if ( cl.HasItems( 1 << ( 29 + i ) ) )
                    {
                        var time = cl.item_gettime[29 + i];

                        if ( time > 0 && time > cl.time - 2 && flashon > 0 )
                        {	// flash frame
                            _Updates = 0;
                        }
                        else
                        {
                            DrawPic( 288 + i * 16, -16, RItems[i] );
                        }

                        if ( time > 0 && time > cl.time - 2 )
                            _Updates = 0;
                    }
                }
            }
            else
            {
                // sigils
                for ( var i = 0; i < 4; i++ )
                {
                    if ( cl.HasItems( 1 << ( 28 + i ) ) )
                    {
                        var time = cl.item_gettime[28 + i];
                        if ( time > 0 && time > cl.time - 2 && flashon > 0 )
                        {	// flash frame
                            _Updates = 0;
                        }
                        else
                            DrawPic( 320 - 32 + i * 8, -16, Sigil[i] );
                        if ( time > 0 && time > cl.time - 2 )
                            _Updates = 0;
                    }
                }
            }
        }

        // Sbar_DrawFrags
        private void DrawFrags( )
        {
            SortFrags( );

            // draw the text
            var l = _ScoreBoardLines <= 4 ? _ScoreBoardLines : 4;
            Int32 xofs, x = 23;
            var cl = Host.Client.cl;

            if ( cl.gametype == ProtocolDef.GAME_DEATHMATCH )
                xofs = 0;
            else
                xofs = ( Host.Screen.vid.width - 320 ) >> 1;

            var y = Host.Screen.vid.height - SBAR_HEIGHT - 23;

            for ( var i = 0; i < l; i++ )
            {
                var k = _FragSort[i];
                var s = cl.scores[k];
                if ( String.IsNullOrEmpty( s.name ) )
                    continue;

                // draw background
                var top = s.colors & 0xf0;
                var bottom = ( s.colors & 15 ) << 4;
                top = ColorForMap( top );
                bottom = ColorForMap( bottom );

                Host.Video.Device.Graphics.FillUsingPalette( xofs + x * 8 + 10, y, 28, 4, top );
                Host.Video.Device.Graphics.FillUsingPalette( xofs + x * 8 + 10, y + 4, 28, 3, bottom );

                // draw number
                var f = s.frags;
                var num = f.ToString( ).PadLeft( 3 );
                //sprintf(num, "%3i", f);

                DrawCharacter( ( x + 1 ) * 8, -24, num[0] );
                DrawCharacter( ( x + 2 ) * 8, -24, num[1] );
                DrawCharacter( ( x + 3 ) * 8, -24, num[2] );

                if ( k == cl.viewentity - 1 )
                {
                    DrawCharacter( x * 8 + 2, -24, 16 );
                    DrawCharacter( ( x + 4 ) * 8 - 4, -24, 17 );
                }
                x += 4;
            }
        }

        // Sbar_DrawPic
        private void DrawPic( Int32 x, Int32 y, BasePicture pic )
        {
            if ( Host.Client.cl.gametype == ProtocolDef.GAME_DEATHMATCH )
                Host.Video.Device.Graphics.DrawPicture( pic, x, y + ( Host.Screen.vid.height - SBAR_HEIGHT ) );
            else
                Host.Video.Device.Graphics.DrawPicture( pic, x + ( ( Host.Screen.vid.width - 320 ) >> 1 ), y + ( Host.Screen.vid.height - SBAR_HEIGHT ) );
        }

        // Sbar_DrawScoreboard
        private void DrawScoreboard( )
        {
            SoloScoreboard( );
            if ( Host.Client.cl.gametype == ProtocolDef.GAME_DEATHMATCH )
                DeathmatchOverlay( );
        }

        // Sbar_DrawNum
        private void DrawNum( Int32 x, Int32 y, Int32 num, Int32 digits, Int32 color )
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

        // Sbar_DrawFace
        private void DrawFace( )
        {
            var cl = Host.Client.cl;

            // PGM 01/19/97 - team color drawing
            // PGM 03/02/97 - fixed so color swatch only appears in CTF modes
            if ( MainWindow.Common.GameKind == GameKind.Rogue &&
                ( Host.Client.cl.maxclients != 1 ) &&
                ( Host.TeamPlay > 3 ) &&
                ( Host.TeamPlay < 7 ) )
            {
                var s = cl.scores[cl.viewentity - 1];

                // draw background
                var top = s.colors & 0xf0;
                var bottom = ( s.colors & 15 ) << 4;
                top = ColorForMap( top );
                bottom = ColorForMap( bottom );

                Int32 xofs;
                if ( cl.gametype == ProtocolDef.GAME_DEATHMATCH )
                    xofs = 113;
                else
                    xofs = ( ( Host.Screen.vid.width - 320 ) >> 1 ) + 113;

                DrawPic( 112, 0, RTeamBord );
                Host.Video.Device.Graphics.FillUsingPalette( xofs, Host.Screen.vid.height - SBAR_HEIGHT + 3, 22, 9, top );
                Host.Video.Device.Graphics.FillUsingPalette( xofs, Host.Screen.vid.height - SBAR_HEIGHT + 12, 22, 9, bottom );

                // draw number
                var num = s.frags.ToString( ).PadLeft( 3 );
                if ( top == 8 )
                {
                    if ( num[0] != ' ' )
                        DrawCharacter( 109, 3, 18 + num[0] - '0' );
                    if ( num[1] != ' ' )
                        DrawCharacter( 116, 3, 18 + num[1] - '0' );
                    if ( num[2] != ' ' )
                        DrawCharacter( 123, 3, 18 + num[2] - '0' );
                }
                else
                {
                    DrawCharacter( 109, 3, num[0] );
                    DrawCharacter( 116, 3, num[1] );
                    DrawCharacter( 123, 3, num[2] );
                }

                return;
            }
            // PGM 01/19/97 - team color drawing

            Int32 f, anim;

            if ( cl.HasItems( QItemsDef.IT_INVISIBILITY | QItemsDef.IT_INVULNERABILITY ) )
            {
                DrawPic( 112, 0, FaceInvisInvuln );
                return;
            }
            if ( cl.HasItems( QItemsDef.IT_QUAD ) )
            {
                DrawPic( 112, 0, FaceQuad );
                return;
            }
            if ( cl.HasItems( QItemsDef.IT_INVISIBILITY ) )
            {
                DrawPic( 112, 0, FaceInvis );
                return;
            }
            if ( cl.HasItems( QItemsDef.IT_INVULNERABILITY ) )
            {
                DrawPic( 112, 0, FaceInvuln );
                return;
            }

            if ( cl.stats[QStatsDef.STAT_HEALTH] >= 100 )
                f = 4;
            else
                f = cl.stats[QStatsDef.STAT_HEALTH] / 20;

            if ( cl.time <= cl.faceanimtime )
            {
                anim = 1;
                _Updates = 0; // make sure the anim gets drawn over
            }
            else
                anim = 0;

            DrawPic( 112, 0, Faces[f, anim] );
        }

        // Sbar_DeathmatchOverlay
        private void MiniDeathmatchOverlay( )
        {
            if ( Host.Screen.vid.width < 512 || Lines == 0 )
                return;

            Host.Screen.CopyEverithing = true;
            Host.Screen.FullUpdate = 0;

            // scores
            SortFrags( );

            // draw the text
            var l = _ScoreBoardLines;
            var y = Host.Screen.vid.height - Lines;
            var numlines = Lines / 8;
            if ( numlines < 3 )
                return;

            //find us
            Int32 i;
            for ( i = 0; i < _ScoreBoardLines; i++ )
                if ( _FragSort[i] == Host.Client.cl.viewentity - 1 )
                    break;

            if ( i == _ScoreBoardLines ) // we're not there
                i = 0;
            else // figure out start
                i = i - numlines / 2;

            if ( i > _ScoreBoardLines - numlines )
                i = _ScoreBoardLines - numlines;
            if ( i < 0 )
                i = 0;

            var x = 324;
            for ( ; i < _ScoreBoardLines && y < Host.Screen.vid.height - 8; i++ )
            {
                var k = _FragSort[i];
                var s = Host.Client.cl.scores[k];
                if ( String.IsNullOrEmpty( s.name ) )
                    continue;

                // draw background
                var top = s.colors & 0xf0;
                var bottom = ( s.colors & 15 ) << 4;
                top = ColorForMap( top );
                bottom = ColorForMap( bottom );

                Host.Video.Device.Graphics.FillUsingPalette( x, y + 1, 40, 3, top );
                Host.Video.Device.Graphics.FillUsingPalette( x, y + 4, 40, 4, bottom );

                // draw number
                var num = s.frags.ToString( ).PadLeft( 3 );
                Host.DrawingContext.DrawCharacter( x + 8, y, num[0] );
                Host.DrawingContext.DrawCharacter( x + 16, y, num[1] );
                Host.DrawingContext.DrawCharacter( x + 24, y, num[2] );

                if ( k == Host.Client.cl.viewentity - 1 )
                {
                    Host.DrawingContext.DrawCharacter( x, y, 16 );
                    Host.DrawingContext.DrawCharacter( x + 32, y, 17 );
                }

                // draw name
                Host.DrawingContext.DrawString( x + 48, y, s.name );

                y += 8;
            }
        }

        // Sbar_SortFrags
        private void SortFrags( )
        {
            var cl = Host.Client.cl;

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

        // Sbar_DrawCharacter
        //
        // Draws one solid graphics character
        private void DrawCharacter( Int32 x, Int32 y, Int32 num )
        {
            if ( Host.Client.cl.gametype == ProtocolDef.GAME_DEATHMATCH )
                Host.DrawingContext.DrawCharacter( x + 4, y + Host.Screen.vid.height - SBAR_HEIGHT, num );
            else
                Host.DrawingContext.DrawCharacter( x + ( ( Host.Screen.vid.width - 320 ) >> 1 ) + 4, y + Host.Screen.vid.height - SBAR_HEIGHT, num );
        }

        // Sbar_ColorForMap
        private Int32 ColorForMap( Int32 m )
        {
            return m < 128 ? m + 8 : m + 8;
        }

        // Sbar_SoloScoreboard
        private void SoloScoreboard( )
        {
            var sb = new StringBuilder( 80 );
            var cl = Host.Client.cl;

            sb.AppendFormat( "Monsters:{0,3:d} /{1,3:d}", cl.stats[QStatsDef.STAT_MONSTERS], Host.Client.cl.stats[QStatsDef.STAT_TOTALMONSTERS] );
            DrawString( 8, 4, sb.ToString( ) );

            sb.Length = 0;
            sb.AppendFormat( "Secrets :{0,3:d} /{1,3:d}", cl.stats[QStatsDef.STAT_SECRETS], cl.stats[QStatsDef.STAT_TOTALSECRETS] );
            DrawString( 8, 12, sb.ToString( ) );

            // time
            var minutes = ( Int32 ) ( cl.time / 60.0 );
            var seconds = ( Int32 ) ( cl.time - 60 * minutes );
            var tens = seconds / 10;
            var units = seconds - 10 * tens;
            sb.Length = 0;
            sb.AppendFormat( "Time :{0,3}:{1}{2}", minutes, tens, units );
            DrawString( 184, 4, sb.ToString( ) );

            // draw level name
            var l = cl.levelname.Length;
            DrawString( 232 - l * 4, 12, cl.levelname );
        }

        // Sbar_DeathmatchOverlay
        private void DeathmatchOverlay( )
        {
            Host.Screen.CopyEverithing = true;
            Host.Screen.FullUpdate = 0;

            var pic = Host.DrawingContext.CachePic( "gfx/ranking.lmp", "GL_LINEAR" );
            Host.Video.Device.Graphics.DrawPicture( pic, ( 320 - pic.Width ) / 2, 8 );

            // scores
            SortFrags( );

            // draw the text
            var l = _ScoreBoardLines;

            var x = 80 + ( ( Host.Screen.vid.width - 320 ) >> 1 );
            var y = 40;
            for ( var i = 0; i < l; i++ )
            {
                var k = _FragSort[i];
                var s = Host.Client.cl.scores[k];
                if ( String.IsNullOrEmpty( s.name ) )
                    continue;

                // draw background
                var top = s.colors & 0xf0;
                var bottom = ( s.colors & 15 ) << 4;
                top = ColorForMap( top );
                bottom = ColorForMap( bottom );

                Host.Video.Device.Graphics.FillUsingPalette( x, y, 40, 4, top );
                Host.Video.Device.Graphics.FillUsingPalette( x, y + 4, 40, 4, bottom );

                // draw number
                var num = s.frags.ToString( ).PadLeft( 3 );

                Host.DrawingContext.DrawCharacter( x + 8, y, num[0] );
                Host.DrawingContext.DrawCharacter( x + 16, y, num[1] );
                Host.DrawingContext.DrawCharacter( x + 24, y, num[2] );

                if ( k == Host.Client.cl.viewentity - 1 )
                    Host.DrawingContext.DrawCharacter( x - 8, y, 12 );

                // draw name
                Host.DrawingContext.DrawString( x + 64, y, s.name );

                y += 10;
            }
        }

        // Sbar_DrawTransPic
        private void DrawTransPic( Int32 x, Int32 y, BasePicture picture )
        {
            if ( Host.Client.cl.gametype == ProtocolDef.GAME_DEATHMATCH )
                Host.Video.Device.Graphics.DrawPicture( picture, x, y + ( Host.Screen.vid.height - SBAR_HEIGHT ), hasAlpha: true );
            else
                Host.Video.Device.Graphics.DrawPicture( picture, x + ( ( Host.Screen.vid.width - 320 ) >> 1 ), y + ( Host.Screen.vid.height - SBAR_HEIGHT ), hasAlpha: true );
        }

        // Sbar_DrawString
        private void DrawString( Int32 x, Int32 y, String str )
        {
            if ( Host.Client.cl.gametype == ProtocolDef.GAME_DEATHMATCH )
                Host.DrawingContext.DrawString( x, y + Host.Screen.vid.height - SBAR_HEIGHT, str );
            else
                Host.DrawingContext.DrawString( x + ( ( Host.Screen.vid.width - 320 ) >> 1 ), y + Host.Screen.vid.height - SBAR_HEIGHT, str );
        }

        // Sbar_ShowScores
        //
        // Tab key down
        private void ShowScores( )
        {
            if ( _ShowScores )
                return;
            _ShowScores = true;
            _Updates = 0;
        }

        // Sbar_DontShowScores
        //
        // Tab key up
        private void DontShowScores( )
        {
            _ShowScores = false;
            _Updates = 0;
        }
    }
}
