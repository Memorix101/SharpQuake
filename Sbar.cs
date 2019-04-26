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
using System.Text;

// sbar.h

// the status bar is only redrawn if something has changed, but if anything
// does, the entire thing will be redrawn for the next vid.numpages frames.

namespace SharpQuake
{
    /// <summary>
    /// Sbar_functions
    /// </summary>
    internal static class Sbar
    {
        public static int Lines
        {
            get; set;
        }

        public const int SBAR_HEIGHT = 24;

        private const int STAT_MINUS = 10;
        private static int _Updates; // sb_updates		// if >= vid.numpages, no update needed
        private static bool _ShowScores; // sb_showscores

        // num frame for '-' stats digit

        private static glpic_t[,] _Nums = new glpic_t[2, 11];
        private static glpic_t _Colon;
        private static glpic_t _Slash;
        private static glpic_t _IBar;
        private static glpic_t _SBar;
        private static glpic_t _ScoreBar;

        private static glpic_t[,] _Weapons = new glpic_t[7, 8];   // 0 is active, 1 is owned, 2-5 are flashes
        private static glpic_t[] _Ammo = new glpic_t[4];
        private static glpic_t[] _Sigil = new glpic_t[4];
        private static glpic_t[] _Armor = new glpic_t[3];
        private static glpic_t[] _Items = new glpic_t[32];

        private static glpic_t[,] _Faces = new glpic_t[7, 2];        // 0 is gibbed, 1 is dead, 2-6 are alive

        // 0 is static, 1 is temporary animation
        private static glpic_t _FaceInvis;

        private static glpic_t _FaceQuad;
        private static glpic_t _FaceInvuln;
        private static glpic_t _FaceInvisInvuln;

        private static glpic_t[] _RInvBar = new glpic_t[2];
        private static glpic_t[] _RWeapons = new glpic_t[5];
        private static glpic_t[] _RItems = new glpic_t[2];
        private static glpic_t[] _RAmmo = new glpic_t[3];
        private static glpic_t _RTeamBord;		// PGM 01/19/97 - team color border

        //MED 01/04/97 added two more weapons + 3 alternates for grenade launcher
        private static glpic_t[,] _HWeapons = new glpic_t[7, 5];   // 0 is active, 1 is owned, 2-5 are flashes

        //MED 01/04/97 added array to simplify weapon parsing
        private static int[] _HipWeapons = new int[]
        {
            QItems.HIT_LASER_CANNON_BIT, QItems.HIT_MJOLNIR_BIT, 4, QItems.HIT_PROXIMITY_GUN_BIT
        };

        //MED 01/04/97 added hipnotic items array
        private static glpic_t[] _HItems = new glpic_t[2];

        private static int[] _FragSort = new int[QDef.MAX_SCOREBOARD];
        private static string[] _ScoreBoardText = new string[QDef.MAX_SCOREBOARD];
        private static int[] _ScoreBoardTop = new int[QDef.MAX_SCOREBOARD];
        private static int[] _ScoreBoardBottom = new int[QDef.MAX_SCOREBOARD];
        private static int[] _ScoreBoardCount = new int[QDef.MAX_SCOREBOARD];
        private static int _ScoreBoardLines;

        // sb_lines scan lines to draw

        // Sbar_Init
        public static void Init()
        {
            for( int i = 0; i < 10; i++ )
            {
                string str = i.ToString();
                _Nums[0, i] = Drawer.PicFromWad( "num_" + str );
                _Nums[1, i] = Drawer.PicFromWad( "anum_" + str );
            }

            _Nums[0, 10] = Drawer.PicFromWad( "num_minus" );
            _Nums[1, 10] = Drawer.PicFromWad( "anum_minus" );

            _Colon = Drawer.PicFromWad( "num_colon" );
            _Slash = Drawer.PicFromWad( "num_slash" );

            _Weapons[0, 0] = Drawer.PicFromWad( "inv_shotgun" );
            _Weapons[0, 1] = Drawer.PicFromWad( "inv_sshotgun" );
            _Weapons[0, 2] = Drawer.PicFromWad( "inv_nailgun" );
            _Weapons[0, 3] = Drawer.PicFromWad( "inv_snailgun" );
            _Weapons[0, 4] = Drawer.PicFromWad( "inv_rlaunch" );
            _Weapons[0, 5] = Drawer.PicFromWad( "inv_srlaunch" );
            _Weapons[0, 6] = Drawer.PicFromWad( "inv_lightng" );

            _Weapons[1, 0] = Drawer.PicFromWad( "inv2_shotgun" );
            _Weapons[1, 1] = Drawer.PicFromWad( "inv2_sshotgun" );
            _Weapons[1, 2] = Drawer.PicFromWad( "inv2_nailgun" );
            _Weapons[1, 3] = Drawer.PicFromWad( "inv2_snailgun" );
            _Weapons[1, 4] = Drawer.PicFromWad( "inv2_rlaunch" );
            _Weapons[1, 5] = Drawer.PicFromWad( "inv2_srlaunch" );
            _Weapons[1, 6] = Drawer.PicFromWad( "inv2_lightng" );

            for( int i = 0; i < 5; i++ )
            {
                string s = "inva" + ( i + 1 ).ToString();
                _Weapons[2 + i, 0] = Drawer.PicFromWad( s + "_shotgun" );
                _Weapons[2 + i, 1] = Drawer.PicFromWad( s + "_sshotgun" );
                _Weapons[2 + i, 2] = Drawer.PicFromWad( s + "_nailgun" );
                _Weapons[2 + i, 3] = Drawer.PicFromWad( s + "_snailgun" );
                _Weapons[2 + i, 4] = Drawer.PicFromWad( s + "_rlaunch" );
                _Weapons[2 + i, 5] = Drawer.PicFromWad( s + "_srlaunch" );
                _Weapons[2 + i, 6] = Drawer.PicFromWad( s + "_lightng" );
            }

            _Ammo[0] = Drawer.PicFromWad( "sb_shells" );
            _Ammo[1] = Drawer.PicFromWad( "sb_nails" );
            _Ammo[2] = Drawer.PicFromWad( "sb_rocket" );
            _Ammo[3] = Drawer.PicFromWad( "sb_cells" );

            _Armor[0] = Drawer.PicFromWad( "sb_armor1" );
            _Armor[1] = Drawer.PicFromWad( "sb_armor2" );
            _Armor[2] = Drawer.PicFromWad( "sb_armor3" );

            _Items[0] = Drawer.PicFromWad( "sb_key1" );
            _Items[1] = Drawer.PicFromWad( "sb_key2" );
            _Items[2] = Drawer.PicFromWad( "sb_invis" );
            _Items[3] = Drawer.PicFromWad( "sb_invuln" );
            _Items[4] = Drawer.PicFromWad( "sb_suit" );
            _Items[5] = Drawer.PicFromWad( "sb_quad" );

            _Sigil[0] = Drawer.PicFromWad( "sb_sigil1" );
            _Sigil[1] = Drawer.PicFromWad( "sb_sigil2" );
            _Sigil[2] = Drawer.PicFromWad( "sb_sigil3" );
            _Sigil[3] = Drawer.PicFromWad( "sb_sigil4" );

            _Faces[4, 0] = Drawer.PicFromWad( "face1" );
            _Faces[4, 1] = Drawer.PicFromWad( "face_p1" );
            _Faces[3, 0] = Drawer.PicFromWad( "face2" );
            _Faces[3, 1] = Drawer.PicFromWad( "face_p2" );
            _Faces[2, 0] = Drawer.PicFromWad( "face3" );
            _Faces[2, 1] = Drawer.PicFromWad( "face_p3" );
            _Faces[1, 0] = Drawer.PicFromWad( "face4" );
            _Faces[1, 1] = Drawer.PicFromWad( "face_p4" );
            _Faces[0, 0] = Drawer.PicFromWad( "face5" );
            _Faces[0, 1] = Drawer.PicFromWad( "face_p5" );

            _FaceInvis = Drawer.PicFromWad( "face_invis" );
            _FaceInvuln = Drawer.PicFromWad( "face_invul2" );
            _FaceInvisInvuln = Drawer.PicFromWad( "face_inv2" );
            _FaceQuad = Drawer.PicFromWad( "face_quad" );

            Cmd.Add( "+showscores", ShowScores );
            Cmd.Add( "-showscores", DontShowScores );

            _SBar = Drawer.PicFromWad( "sbar" );
            _IBar = Drawer.PicFromWad( "ibar" );
            _ScoreBar = Drawer.PicFromWad( "scorebar" );

            //MED 01/04/97 added new hipnotic weapons
            if( Common.GameKind == GameKind.Hipnotic )
            {
                _HWeapons[0, 0] = Drawer.PicFromWad( "inv_laser" );
                _HWeapons[0, 1] = Drawer.PicFromWad( "inv_mjolnir" );
                _HWeapons[0, 2] = Drawer.PicFromWad( "inv_gren_prox" );
                _HWeapons[0, 3] = Drawer.PicFromWad( "inv_prox_gren" );
                _HWeapons[0, 4] = Drawer.PicFromWad( "inv_prox" );

                _HWeapons[1, 0] = Drawer.PicFromWad( "inv2_laser" );
                _HWeapons[1, 1] = Drawer.PicFromWad( "inv2_mjolnir" );
                _HWeapons[1, 2] = Drawer.PicFromWad( "inv2_gren_prox" );
                _HWeapons[1, 3] = Drawer.PicFromWad( "inv2_prox_gren" );
                _HWeapons[1, 4] = Drawer.PicFromWad( "inv2_prox" );

                for( int i = 0; i < 5; i++ )
                {
                    string s = "inva" + ( i + 1 ).ToString();
                    _HWeapons[2 + i, 0] = Drawer.PicFromWad( s + "_laser" );
                    _HWeapons[2 + i, 1] = Drawer.PicFromWad( s + "_mjolnir" );
                    _HWeapons[2 + i, 2] = Drawer.PicFromWad( s + "_gren_prox" );
                    _HWeapons[2 + i, 3] = Drawer.PicFromWad( s + "_prox_gren" );
                    _HWeapons[2 + i, 4] = Drawer.PicFromWad( s + "_prox" );
                }

                _HItems[0] = Drawer.PicFromWad( "sb_wsuit" );
                _HItems[1] = Drawer.PicFromWad( "sb_eshld" );
            }

            if( Common.GameKind == GameKind.Rogue )
            {
                _RInvBar[0] = Drawer.PicFromWad( "r_invbar1" );
                _RInvBar[1] = Drawer.PicFromWad( "r_invbar2" );

                _RWeapons[0] = Drawer.PicFromWad( "r_lava" );
                _RWeapons[1] = Drawer.PicFromWad( "r_superlava" );
                _RWeapons[2] = Drawer.PicFromWad( "r_gren" );
                _RWeapons[3] = Drawer.PicFromWad( "r_multirock" );
                _RWeapons[4] = Drawer.PicFromWad( "r_plasma" );

                _RItems[0] = Drawer.PicFromWad( "r_shield1" );
                _RItems[1] = Drawer.PicFromWad( "r_agrav1" );

                // PGM 01/19/97 - team color border
                _RTeamBord = Drawer.PicFromWad( "r_teambord" );
                // PGM 01/19/97 - team color border

                _RAmmo[0] = Drawer.PicFromWad( "r_ammolava" );
                _RAmmo[1] = Drawer.PicFromWad( "r_ammomulti" );
                _RAmmo[2] = Drawer.PicFromWad( "r_ammoplasma" );
            }
        }

        // Sbar_Changed
        // call whenever any of the client stats represented on the sbar changes
        public static void Changed()
        {
            _Updates = 0;	// update next frame
        }

        // Sbar_Draw
        // called every frame by screen
        public static void Draw()
        {
            viddef_t vid = Scr.vid;
            if( Scr.ConCurrent == vid.height )
                return;		// console is full screen

            if( _Updates >= vid.numpages )
                return;

            Scr.CopyEverithing = true;

            _Updates++;

            if( Sbar.Lines > 0 && vid.width > 320 )
                Drawer.TileClear( 0, vid.height - Sbar.Lines, vid.width, Sbar.Lines );

            if( Sbar.Lines > 24 )
            {
                DrawInventory();
                if( Client.cl.maxclients != 1 )
                    DrawFrags();
            }

            client_state_t cl = Client.cl;
            if( _ShowScores || cl.stats[QStats.STAT_HEALTH] <= 0 )
            {
                DrawPic( 0, 0, _ScoreBar );
                DrawScoreboard();
                _Updates = 0;
            }
            else if( Sbar.Lines > 0 )
            {
                DrawPic( 0, 0, _SBar );

                // keys (hipnotic only)
                //MED 01/04/97 moved keys here so they would not be overwritten
                if( Common.GameKind == GameKind.Hipnotic )
                {
                    if( cl.HasItems( QItems.IT_KEY1 ) )
                        DrawPic( 209, 3, _Items[0] );
                    if( cl.HasItems( QItems.IT_KEY2 ) )
                        DrawPic( 209, 12, _Items[1] );
                }
                // armor
                if( cl.HasItems( QItems.IT_INVULNERABILITY ) )
                {
                    DrawNum( 24, 0, 666, 3, 1 );
                    DrawPic( 0, 0, Drawer.Disc );
                }
                else
                {
                    if( Common.GameKind == GameKind.Rogue )
                    {
                        DrawNum( 24, 0, cl.stats[QStats.STAT_ARMOR], 3, cl.stats[QStats.STAT_ARMOR] <= 25 ? 1 : 0 ); // uze: corrected color param
                        if( cl.HasItems( QItems.RIT_ARMOR3 ) )
                            DrawPic( 0, 0, _Armor[2] );
                        else if( cl.HasItems( QItems.RIT_ARMOR2 ) )
                            DrawPic( 0, 0, _Armor[1] );
                        else if( cl.HasItems( QItems.RIT_ARMOR1 ) )
                            DrawPic( 0, 0, _Armor[0] );
                    }
                    else
                    {
                        DrawNum( 24, 0, cl.stats[QStats.STAT_ARMOR], 3, cl.stats[QStats.STAT_ARMOR] <= 25 ? 1 : 0 );
                        if( cl.HasItems( QItems.IT_ARMOR3 ) )
                            DrawPic( 0, 0, _Armor[2] );
                        else if( cl.HasItems( QItems.IT_ARMOR2 ) )
                            DrawPic( 0, 0, _Armor[1] );
                        else if( cl.HasItems( QItems.IT_ARMOR1 ) )
                            DrawPic( 0, 0, _Armor[0] );
                    }
                }

                // face
                DrawFace();

                // health
                DrawNum( 136, 0, cl.stats[QStats.STAT_HEALTH], 3, cl.stats[QStats.STAT_HEALTH] <= 25 ? 1 : 0 );

                // ammo icon
                if( Common.GameKind == GameKind.Rogue )
                {
                    if( cl.HasItems( QItems.RIT_SHELLS ) )
                        DrawPic( 224, 0, _Ammo[0] );
                    else if( cl.HasItems( QItems.RIT_NAILS ) )
                        DrawPic( 224, 0, _Ammo[1] );
                    else if( cl.HasItems( QItems.RIT_ROCKETS ) )
                        DrawPic( 224, 0, _Ammo[2] );
                    else if( cl.HasItems( QItems.RIT_CELLS ) )
                        DrawPic( 224, 0, _Ammo[3] );
                    else if( cl.HasItems( QItems.RIT_LAVA_NAILS ) )
                        DrawPic( 224, 0, _RAmmo[0] );
                    else if( cl.HasItems( QItems.RIT_PLASMA_AMMO ) )
                        DrawPic( 224, 0, _RAmmo[1] );
                    else if( cl.HasItems( QItems.RIT_MULTI_ROCKETS ) )
                        DrawPic( 224, 0, _RAmmo[2] );
                }
                else
                {
                    if( cl.HasItems( QItems.IT_SHELLS ) )
                        DrawPic( 224, 0, _Ammo[0] );
                    else if( cl.HasItems( QItems.IT_NAILS ) )
                        DrawPic( 224, 0, _Ammo[1] );
                    else if( cl.HasItems( QItems.IT_ROCKETS ) )
                        DrawPic( 224, 0, _Ammo[2] );
                    else if( cl.HasItems( QItems.IT_CELLS ) )
                        DrawPic( 224, 0, _Ammo[3] );
                }

                DrawNum( 248, 0, cl.stats[QStats.STAT_AMMO], 3, cl.stats[QStats.STAT_AMMO] <= 10 ? 1 : 0 );
            }

            if( vid.width > 320 )
            {
                if( Client.cl.gametype == Protocol.GAME_DEATHMATCH )
                    MiniDeathmatchOverlay();
            }
        }

        /// <summary>
        /// Sbar_IntermissionOverlay
        /// called each frame after the level has been completed
        /// </summary>
        public static void IntermissionOverlay()
        {
            Scr.CopyEverithing = true;
            Scr.FullUpdate = 0;

            if( Client.cl.gametype == Protocol.GAME_DEATHMATCH )
            {
                Sbar.DeathmatchOverlay();
                return;
            }

            glpic_t pic = Drawer.CachePic( "gfx/complete.lmp" );
            Drawer.DrawPic( 64, 24, pic );

            pic = Drawer.CachePic( "gfx/inter.lmp" );
            Drawer.DrawTransPic( 0, 56, pic );

            // time
            int dig = Client.cl.completed_time / 60;
            IntermissionNumber( 160, 64, dig, 3, 0 );
            int num = Client.cl.completed_time - dig * 60;
            Drawer.DrawTransPic( 234, 64, _Colon );
            Drawer.DrawTransPic( 246, 64, _Nums[0, num / 10] );
            Drawer.DrawTransPic( 266, 64, _Nums[0, num % 10] );

            IntermissionNumber( 160, 104, Client.cl.stats[QStats.STAT_SECRETS], 3, 0 );
            Drawer.DrawTransPic( 232, 104, _Slash );
            IntermissionNumber( 240, 104, Client.cl.stats[QStats.STAT_TOTALSECRETS], 3, 0 );

            IntermissionNumber( 160, 144, Client.cl.stats[QStats.STAT_MONSTERS], 3, 0 );
            Drawer.DrawTransPic( 232, 144, _Slash );
            IntermissionNumber( 240, 144, Client.cl.stats[QStats.STAT_TOTALMONSTERS], 3, 0 );
        }

        /// <summary>
        /// Sbar_FinaleOverlay
        /// </summary>
        public static void FinaleOverlay()
        {
            Scr.CopyEverithing = true;

            glpic_t pic = Drawer.CachePic( "gfx/finale.lmp" );
            Drawer.DrawTransPic( ( Scr.vid.width - pic.width ) / 2, 16, pic );
        }

        /// <summary>
        /// Sbar_IntermissionNumber
        /// </summary>
        private static void IntermissionNumber( int x, int y, int num, int digits, int color )
        {
            string str = num.ToString();
            if( str.Length > digits )
            {
                str = str.Remove( 0, str.Length - digits );
            }

            if( str.Length < digits )
                x += ( digits - str.Length ) * 24;

            for( int i = 0; i < str.Length; i++ )
            {
                int frame = ( str[i] == '-' ? STAT_MINUS : str[i] - '0' );
                Drawer.DrawTransPic( x, y, _Nums[color, frame] );
                x += 24;
            }
        }

        // Sbar_DrawInventory
        private static void DrawInventory()
        {
            int flashon;

            client_state_t cl = Client.cl;
            if( Common.GameKind == GameKind.Rogue )
            {
                if( cl.stats[QStats.STAT_ACTIVEWEAPON] >= QItems.RIT_LAVA_NAILGUN )
                    DrawPic( 0, -24, _RInvBar[0] );
                else
                    DrawPic( 0, -24, _RInvBar[1] );
            }
            else
                DrawPic( 0, -24, _IBar );

            // weapons
            for( int i = 0; i < 7; i++ )
            {
                if( cl.HasItems( QItems.IT_SHOTGUN << i ) )
                {
                    float time = cl.item_gettime[i];
                    flashon = (int)( ( cl.time - time ) * 10 );
                    if( flashon >= 10 )
                    {
                        if( cl.stats[QStats.STAT_ACTIVEWEAPON] == ( QItems.IT_SHOTGUN << i ) )
                            flashon = 1;
                        else
                            flashon = 0;
                    }
                    else
                        flashon = ( flashon % 5 ) + 2;

                    DrawPic( i * 24, -16, _Weapons[flashon, i] );

                    if( flashon > 1 )
                        _Updates = 0; // force update to remove flash
                }
            }

            // MED 01/04/97
            // hipnotic weapons
            if( Common.GameKind == GameKind.Hipnotic )
            {
                int grenadeflashing = 0;
                for( int i = 0; i < 4; i++ )
                {
                    if( cl.HasItems( 1 << _HipWeapons[i] ) )
                    {
                        float time = cl.item_gettime[_HipWeapons[i]];
                        flashon = (int)( ( cl.time - time ) * 10 );
                        if( flashon >= 10 )
                        {
                            if( cl.stats[QStats.STAT_ACTIVEWEAPON] == ( 1 << _HipWeapons[i] ) )
                                flashon = 1;
                            else
                                flashon = 0;
                        }
                        else
                            flashon = ( flashon % 5 ) + 2;

                        // check grenade launcher
                        if( i == 2 )
                        {
                            if( cl.HasItems( QItems.HIT_PROXIMITY_GUN ) )
                            {
                                if( flashon > 0 )
                                {
                                    grenadeflashing = 1;
                                    DrawPic( 96, -16, _HWeapons[flashon, 2] );
                                }
                            }
                        }
                        else if( i == 3 )
                        {
                            if( cl.HasItems( QItems.IT_SHOTGUN << 4 ) )
                            {
                                if( flashon > 0 && grenadeflashing == 0 )
                                {
                                    DrawPic( 96, -16, _HWeapons[flashon, 3] );
                                }
                                else if( grenadeflashing == 0 )
                                {
                                    DrawPic( 96, -16, _HWeapons[0, 3] );
                                }
                            }
                            else
                                DrawPic( 96, -16, _HWeapons[flashon, 4] );
                        }
                        else
                            DrawPic( 176 + ( i * 24 ), -16, _HWeapons[flashon, i] );
                        if( flashon > 1 )
                            _Updates = 0; // force update to remove flash
                    }
                }
            }

            if( Common.GameKind == GameKind.Rogue )
            {
                // check for powered up weapon.
                if( cl.stats[QStats.STAT_ACTIVEWEAPON] >= QItems.RIT_LAVA_NAILGUN )
                    for( int i = 0; i < 5; i++ )
                        if( cl.stats[QStats.STAT_ACTIVEWEAPON] == ( QItems.RIT_LAVA_NAILGUN << i ) )
                            DrawPic( ( i + 2 ) * 24, -16, _RWeapons[i] );
            }

            // ammo counts
            for( int i = 0; i < 4; i++ )
            {
                string num = cl.stats[QStats.STAT_SHELLS + i].ToString().PadLeft( 3 );
                //sprintf(num, "%3i", cl.stats[QStats.STAT_SHELLS + i]);
                if( num[0] != ' ' )
                    DrawCharacter( ( 6 * i + 1 ) * 8 - 2, -24, 18 + num[0] - '0' );
                if( num[1] != ' ' )
                    DrawCharacter( ( 6 * i + 2 ) * 8 - 2, -24, 18 + num[1] - '0' );
                if( num[2] != ' ' )
                    DrawCharacter( ( 6 * i + 3 ) * 8 - 2, -24, 18 + num[2] - '0' );
            }

            flashon = 0;
            // items
            for( int i = 0; i < 6; i++ )
            {
                if( cl.HasItems( 1 << ( 17 + i ) ) )
                {
                    float time = cl.item_gettime[17 + i];
                    if( time > 0 && time > cl.time - 2 && flashon > 0 )
                    {  // flash frame
                        _Updates = 0;
                    }
                    else
                    {
                        //MED 01/04/97 changed keys
                        if( Common.GameKind != GameKind.Hipnotic || ( i > 1 ) )
                        {
                            DrawPic( 192 + i * 16, -16, _Items[i] );
                        }
                    }
                    if( time > 0 && time > cl.time - 2 )
                        _Updates = 0;
                }
            }

            //MED 01/04/97 added hipnotic items
            // hipnotic items
            if( Common.GameKind == GameKind.Hipnotic )
            {
                for( int i = 0; i < 2; i++ )
                {
                    if( cl.HasItems( 1 << ( 24 + i ) ) )
                    {
                        float time = cl.item_gettime[24 + i];
                        if( time > 0 && time > cl.time - 2 && flashon > 0 )
                        {  // flash frame
                            _Updates = 0;
                        }
                        else
                        {
                            DrawPic( 288 + i * 16, -16, _HItems[i] );
                        }
                        if( time > 0 && time > cl.time - 2 )
                            _Updates = 0;
                    }
                }
            }

            if( Common.GameKind == GameKind.Rogue )
            {
                // new rogue items
                for( int i = 0; i < 2; i++ )
                {
                    if( cl.HasItems( 1 << ( 29 + i ) ) )
                    {
                        float time = cl.item_gettime[29 + i];

                        if( time > 0 && time > cl.time - 2 && flashon > 0 )
                        {	// flash frame
                            _Updates = 0;
                        }
                        else
                        {
                            DrawPic( 288 + i * 16, -16, _RItems[i] );
                        }

                        if( time > 0 && time > cl.time - 2 )
                            _Updates = 0;
                    }
                }
            }
            else
            {
                // sigils
                for( int i = 0; i < 4; i++ )
                {
                    if( cl.HasItems( 1 << ( 28 + i ) ) )
                    {
                        float time = cl.item_gettime[28 + i];
                        if( time > 0 && time > cl.time - 2 && flashon > 0 )
                        {	// flash frame
                            _Updates = 0;
                        }
                        else
                            DrawPic( 320 - 32 + i * 8, -16, _Sigil[i] );
                        if( time > 0 && time > cl.time - 2 )
                            _Updates = 0;
                    }
                }
            }
        }

        // Sbar_DrawFrags
        private static void DrawFrags()
        {
            SortFrags();

            // draw the text
            int l = _ScoreBoardLines <= 4 ? _ScoreBoardLines : 4;
            int xofs, x = 23;
            client_state_t cl = Client.cl;

            if( cl.gametype == Protocol.GAME_DEATHMATCH )
                xofs = 0;
            else
                xofs = ( Scr.vid.width - 320 ) >> 1;

            int y = Scr.vid.height - SBAR_HEIGHT - 23;

            for( int i = 0; i < l; i++ )
            {
                int k = _FragSort[i];
                scoreboard_t s = cl.scores[k];
                if( String.IsNullOrEmpty( s.name ) )
                    continue;

                // draw background
                int top = s.colors & 0xf0;
                int bottom = ( s.colors & 15 ) << 4;
                top = ColorForMap( top );
                bottom = ColorForMap( bottom );

                Drawer.Fill( xofs + x * 8 + 10, y, 28, 4, top );
                Drawer.Fill( xofs + x * 8 + 10, y + 4, 28, 3, bottom );

                // draw number
                int f = s.frags;
                string num = f.ToString().PadLeft( 3 );
                //sprintf(num, "%3i", f);

                DrawCharacter( ( x + 1 ) * 8, -24, num[0] );
                DrawCharacter( ( x + 2 ) * 8, -24, num[1] );
                DrawCharacter( ( x + 3 ) * 8, -24, num[2] );

                if( k == cl.viewentity - 1 )
                {
                    DrawCharacter( x * 8 + 2, -24, 16 );
                    DrawCharacter( ( x + 4 ) * 8 - 4, -24, 17 );
                }
                x += 4;
            }
        }

        // Sbar_DrawPic
        private static void DrawPic( int x, int y, glpic_t pic )
        {
            if( Client.cl.gametype == Protocol.GAME_DEATHMATCH )
                Drawer.DrawPic( x, y + ( Scr.vid.height - SBAR_HEIGHT ), pic );
            else
                Drawer.DrawPic( x + ( ( Scr.vid.width - 320 ) >> 1 ), y + ( Scr.vid.height - SBAR_HEIGHT ), pic );
        }

        // Sbar_DrawScoreboard
        private static void DrawScoreboard()
        {
            SoloScoreboard();
            if( Client.cl.gametype == Protocol.GAME_DEATHMATCH )
                DeathmatchOverlay();
        }

        // Sbar_DrawNum
        private static void DrawNum( int x, int y, int num, int digits, int color )
        {
            string str = num.ToString();// int l = Sbar_itoa(num, str);

            if( str.Length > digits )
                str = str.Remove( str.Length - digits );
            if( str.Length < digits )
                x += ( digits - str.Length ) * 24;

            for( int i = 0, frame; i < str.Length; i++ )
            {
                if( str[i] == '-' )
                    frame = STAT_MINUS;
                else
                    frame = str[i] - '0';

                DrawTransPic( x, y, _Nums[color, frame] );
                x += 24;
            }
        }

        // Sbar_DrawFace
        private static void DrawFace()
        {
            client_state_t cl = Client.cl;

            // PGM 01/19/97 - team color drawing
            // PGM 03/02/97 - fixed so color swatch only appears in CTF modes
            if( Common.GameKind == GameKind.Rogue &&
                ( Client.cl.maxclients != 1 ) &&
                ( Host.TeamPlay > 3 ) &&
                ( Host.TeamPlay < 7 ) )
            {
                scoreboard_t s = cl.scores[cl.viewentity - 1];

                // draw background
                int top = s.colors & 0xf0;
                int bottom = ( s.colors & 15 ) << 4;
                top = ColorForMap( top );
                bottom = ColorForMap( bottom );

                int xofs;
                if( cl.gametype == Protocol.GAME_DEATHMATCH )
                    xofs = 113;
                else
                    xofs = ( ( Scr.vid.width - 320 ) >> 1 ) + 113;

                DrawPic( 112, 0, _RTeamBord );
                Drawer.Fill( xofs, Scr.vid.height - SBAR_HEIGHT + 3, 22, 9, top );
                Drawer.Fill( xofs, Scr.vid.height - SBAR_HEIGHT + 12, 22, 9, bottom );

                // draw number
                string num = s.frags.ToString().PadLeft( 3 );
                if( top == 8 )
                {
                    if( num[0] != ' ' )
                        DrawCharacter( 109, 3, 18 + num[0] - '0' );
                    if( num[1] != ' ' )
                        DrawCharacter( 116, 3, 18 + num[1] - '0' );
                    if( num[2] != ' ' )
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

            int f, anim;

            if( cl.HasItems( QItems.IT_INVISIBILITY | QItems.IT_INVULNERABILITY ) )
            {
                DrawPic( 112, 0, _FaceInvisInvuln );
                return;
            }
            if( cl.HasItems( QItems.IT_QUAD ) )
            {
                DrawPic( 112, 0, _FaceQuad );
                return;
            }
            if( cl.HasItems( QItems.IT_INVISIBILITY ) )
            {
                DrawPic( 112, 0, _FaceInvis );
                return;
            }
            if( cl.HasItems( QItems.IT_INVULNERABILITY ) )
            {
                DrawPic( 112, 0, _FaceInvuln );
                return;
            }

            if( cl.stats[QStats.STAT_HEALTH] >= 100 )
                f = 4;
            else
                f = cl.stats[QStats.STAT_HEALTH] / 20;

            if( cl.time <= cl.faceanimtime )
            {
                anim = 1;
                _Updates = 0; // make sure the anim gets drawn over
            }
            else
                anim = 0;

            DrawPic( 112, 0, _Faces[f, anim] );
        }

        // Sbar_DeathmatchOverlay
        private static void MiniDeathmatchOverlay()
        {
            if( Scr.vid.width < 512 || Sbar.Lines == 0 )
                return;

            Scr.CopyEverithing = true;
            Scr.FullUpdate = 0;

            // scores
            SortFrags();

            // draw the text
            int l = _ScoreBoardLines;
            int y = Scr.vid.height - Sbar.Lines;
            int numlines = Sbar.Lines / 8;
            if( numlines < 3 )
                return;

            //find us
            int i;
            for( i = 0; i < _ScoreBoardLines; i++ )
                if( _FragSort[i] == Client.cl.viewentity - 1 )
                    break;

            if( i == _ScoreBoardLines ) // we're not there
                i = 0;
            else // figure out start
                i = i - numlines / 2;

            if( i > _ScoreBoardLines - numlines )
                i = _ScoreBoardLines - numlines;
            if( i < 0 )
                i = 0;

            int x = 324;
            for( ; i < _ScoreBoardLines && y < Scr.vid.height - 8; i++ )
            {
                int k = _FragSort[i];
                scoreboard_t s = Client.cl.scores[k];
                if( String.IsNullOrEmpty( s.name ) )
                    continue;

                // draw background
                int top = s.colors & 0xf0;
                int bottom = ( s.colors & 15 ) << 4;
                top = ColorForMap( top );
                bottom = ColorForMap( bottom );

                Drawer.Fill( x, y + 1, 40, 3, top );
                Drawer.Fill( x, y + 4, 40, 4, bottom );

                // draw number
                string num = s.frags.ToString().PadLeft( 3 );
                Drawer.DrawCharacter( x + 8, y, num[0] );
                Drawer.DrawCharacter( x + 16, y, num[1] );
                Drawer.DrawCharacter( x + 24, y, num[2] );

                if( k == Client.cl.viewentity - 1 )
                {
                    Drawer.DrawCharacter( x, y, 16 );
                    Drawer.DrawCharacter( x + 32, y, 17 );
                }

                // draw name
                Drawer.DrawString( x + 48, y, s.name );

                y += 8;
            }
        }

        // Sbar_SortFrags
        private static void SortFrags()
        {
            client_state_t cl = Client.cl;

            // sort by frags
            _ScoreBoardLines = 0;
            for( int i = 0; i < cl.maxclients; i++ )
            {
                if( !String.IsNullOrEmpty( cl.scores[i].name ) )
                {
                    _FragSort[_ScoreBoardLines] = i;
                    _ScoreBoardLines++;
                }
            }

            for( int i = 0; i < _ScoreBoardLines; i++ )
            {
                for( int j = 0; j < _ScoreBoardLines - 1 - i; j++ )
                    if( cl.scores[_FragSort[j]].frags < cl.scores[_FragSort[j + 1]].frags )
                    {
                        int k = _FragSort[j];
                        _FragSort[j] = _FragSort[j + 1];
                        _FragSort[j + 1] = k;
                    }
            }
        }

        // Sbar_DrawCharacter
        //
        // Draws one solid graphics character
        private static void DrawCharacter( int x, int y, int num )
        {
            if( Client.cl.gametype == Protocol.GAME_DEATHMATCH )
                Drawer.DrawCharacter( x + 4, y + Scr.vid.height - SBAR_HEIGHT, num );
            else
                Drawer.DrawCharacter( x + ( ( Scr.vid.width - 320 ) >> 1 ) + 4, y + Scr.vid.height - SBAR_HEIGHT, num );
        }

        // Sbar_ColorForMap
        private static int ColorForMap( int m )
        {
            return m < 128 ? m + 8 : m + 8;
        }

        // Sbar_SoloScoreboard
        private static void SoloScoreboard()
        {
            StringBuilder sb = new StringBuilder( 80 );
            client_state_t cl = Client.cl;

            sb.AppendFormat( "Monsters:{0,3:d} /{1,3:d}", cl.stats[QStats.STAT_MONSTERS], Client.cl.stats[QStats.STAT_TOTALMONSTERS] );
            DrawString( 8, 4, sb.ToString() );

            sb.Length = 0;
            sb.AppendFormat( "Secrets :{0,3:d} /{1,3:d}", cl.stats[QStats.STAT_SECRETS], cl.stats[QStats.STAT_TOTALSECRETS] );
            DrawString( 8, 12, sb.ToString() );

            // time
            int minutes = (int)( cl.time / 60.0 );
            int seconds = (int)( cl.time - 60 * minutes );
            int tens = seconds / 10;
            int units = seconds - 10 * tens;
            sb.Length = 0;
            sb.AppendFormat( "Time :{0,3}:{1}{2}", minutes, tens, units );
            DrawString( 184, 4, sb.ToString() );

            // draw level name
            int l = cl.levelname.Length;
            DrawString( 232 - l * 4, 12, cl.levelname );
        }

        // Sbar_DeathmatchOverlay
        private static void DeathmatchOverlay()
        {
            Scr.CopyEverithing = true;
            Scr.FullUpdate = 0;

            glpic_t pic = Drawer.CachePic( "gfx/ranking.lmp" );
            Menu.DrawPic( ( 320 - pic.width ) / 2, 8, pic );

            // scores
            SortFrags();

            // draw the text
            int l = _ScoreBoardLines;

            int x = 80 + ( ( Scr.vid.width - 320 ) >> 1 );
            int y = 40;
            for( int i = 0; i < l; i++ )
            {
                int k = _FragSort[i];
                scoreboard_t s = Client.cl.scores[k];
                if( String.IsNullOrEmpty( s.name ) )
                    continue;

                // draw background
                int top = s.colors & 0xf0;
                int bottom = ( s.colors & 15 ) << 4;
                top = ColorForMap( top );
                bottom = ColorForMap( bottom );

                Drawer.Fill( x, y, 40, 4, top );
                Drawer.Fill( x, y + 4, 40, 4, bottom );

                // draw number
                string num = s.frags.ToString().PadLeft( 3 );

                Drawer.DrawCharacter( x + 8, y, num[0] );
                Drawer.DrawCharacter( x + 16, y, num[1] );
                Drawer.DrawCharacter( x + 24, y, num[2] );

                if( k == Client.cl.viewentity - 1 )
                    Drawer.DrawCharacter( x - 8, y, 12 );

                // draw name
                Drawer.DrawString( x + 64, y, s.name );

                y += 10;
            }
        }

        // Sbar_DrawTransPic
        private static void DrawTransPic( int x, int y, glpic_t pic )
        {
            if( Client.cl.gametype == Protocol.GAME_DEATHMATCH )
                Drawer.DrawTransPic( x, y + ( Scr.vid.height - SBAR_HEIGHT ), pic );
            else
                Drawer.DrawTransPic( x + ( ( Scr.vid.width - 320 ) >> 1 ), y + ( Scr.vid.height - SBAR_HEIGHT ), pic );
        }

        // Sbar_DrawString
        private static void DrawString( int x, int y, string str )
        {
            if( Client.cl.gametype == Protocol.GAME_DEATHMATCH )
                Drawer.DrawString( x, y + Scr.vid.height - SBAR_HEIGHT, str );
            else
                Drawer.DrawString( x + ( ( Scr.vid.width - 320 ) >> 1 ), y + Scr.vid.height - SBAR_HEIGHT, str );
        }

        // Sbar_ShowScores
        //
        // Tab key down
        private static void ShowScores()
        {
            if( _ShowScores )
                return;
            _ShowScores = true;
            _Updates = 0;
        }

        // Sbar_DontShowScores
        //
        // Tab key up
        private static void DontShowScores()
        {
            _ShowScores = false;
            _Updates = 0;
        }
    }
}
