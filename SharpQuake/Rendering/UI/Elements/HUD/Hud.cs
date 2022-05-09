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
using SharpQuake.Factories.Rendering.UI;
using SharpQuake.Framework.Rendering.UI;
using SharpQuake.Framework;
using SharpQuake.Framework.IO;
using SharpQuake.Renderer.Textures;

// sbar.h

// the status bar is only redrawn if something has changed, but if anything
// does, the entire thing will be redrawn for the next vid.numpages frames.

namespace SharpQuake.Rendering.UI.Elements.HUD
{
    /// <summary>
    /// Sbar_functions
    /// </summary>
    public class Hud : BaseUIElement
    {
        private Int32 _Updates; // sb_updates		// if >= vid.numpages, no update needed
        private Boolean _ShowScores; // sb_showscores

        //MED 01/04/97 added array to simplify weapon parsing
        private Int32[] _HipWeapons = new Int32[]
        {
            QItemsDef.HIT_LASER_CANNON_BIT, QItemsDef.HIT_MJOLNIR_BIT, 4, QItemsDef.HIT_PROXIMITY_GUN_BIT
        };

        public override Boolean ManualInitialisation
        {
            get
            {
                return true;
            }
        }

        private HudResources _resources;

        public Hud( Host host ) : base( host )
        {
        }

        // Sbar_Init
        public override void Initialise( )
        {
            _resources = _host.Screen.HudResources;
            base.Initialise( );
            _host.Commands.Add( "+showscores", ShowScores );
            _host.Commands.Add( "-showscores", DontShowScores );

            HasInitialised = true;
        }

        // Sbar_Changed
        // call whenever any of the client stats represented on the sbar changes
        protected override void OnDirty( )
        {
            _Updates = 0;	// update next frame
        }

        // Sbar_Draw
        // called every frame by screen
        public override void Draw( )
        {
            base.Draw( );

            if ( _host == null || !HasInitialised )
                return;

            var vid = _host.Screen.vid;
            if ( _host.Screen.Elements.Get<VisualConsole>( ElementFactory.CONSOLE )?.ConCurrent == vid.height )
                return;		// console is full screen

            if ( _Updates >= vid.numpages )
                return;

            _host.Screen.CopyEverithing = true;

            _Updates++;

            if ( _resources.Lines > 0 && vid.width > 320 )
                _host.DrawingContext.TileClear( 0, vid.height - _resources.Lines, vid.width, _resources.Lines );

            if ( _resources.Lines > 24 )
            {
                DrawInventory( );
                if ( _host.Client.cl.maxclients != 1 )
                    _host.Screen.Elements.Draw( ElementFactory.FRAGS );
            }

            var cl = _host.Client.cl;
            if ( _ShowScores || cl.stats[QStatsDef.STAT_HEALTH] <= 0 )
            {
                DrawScoreboard( );
                _Updates = 0;
            }
            else if ( _resources.Lines > 0 )
            {
                _resources.DrawPic( 0, 0, _resources.SBar );

                // keys (hipnotic only)
                //MED 01/04/97 moved keys here so they would not be overwritten
                if ( MainWindow.Common.GameKind == GameKind.Hipnotic )
                {
                    if ( cl.HasItems( QItemsDef.IT_KEY1 ) )
                        _resources.DrawPic( 209, 3, _resources.Items[0] );
                    if ( cl.HasItems( QItemsDef.IT_KEY2 ) )
                        _resources.DrawPic( 209, 12, _resources.Items[1] );
                }
                // armor
                if ( cl.HasItems( QItemsDef.IT_INVULNERABILITY ) )
                {
                    _resources.DrawNum( 24, 0, 666, 3, 1 );
                    _host.Video.Device.Graphics.DrawPicture( _host.DrawingContext.Disc, 0, 0 );
                }
                else
                {
                    if ( MainWindow.Common.GameKind == GameKind.Rogue )
                    {
                        _resources.DrawNum( 24, 0, cl.stats[QStatsDef.STAT_ARMOR], 3, cl.stats[QStatsDef.STAT_ARMOR] <= 25 ? 1 : 0 ); // uze: corrected color param
                        if ( cl.HasItems( QItemsDef.RIT_ARMOR3 ) )
                            _resources.DrawPic( 0, 0, _resources.Armour[2] );
                        else if ( cl.HasItems( QItemsDef.RIT_ARMOR2 ) )
                            _resources.DrawPic( 0, 0, _resources.Armour[1] );
                        else if ( cl.HasItems( QItemsDef.RIT_ARMOR1 ) )
                            _resources.DrawPic( 0, 0, _resources.Armour[0] );
                    }
                    else
                    {
                        _resources.DrawNum( 24, 0, cl.stats[QStatsDef.STAT_ARMOR], 3, cl.stats[QStatsDef.STAT_ARMOR] <= 25 ? 1 : 0 );
                        if ( cl.HasItems( QItemsDef.IT_ARMOR3 ) )
                            _resources.DrawPic( 0, 0, _resources.Armour[2] );
                        else if ( cl.HasItems( QItemsDef.IT_ARMOR2 ) )
                            _resources.DrawPic( 0, 0, _resources.Armour[1] );
                        else if ( cl.HasItems( QItemsDef.IT_ARMOR1 ) )
                            _resources.DrawPic( 0, 0, _resources.Armour[0] );
                    }
                }

                // face
                DrawFace( );

                // health
                _resources.DrawNum( 136, 0, cl.stats[QStatsDef.STAT_HEALTH], 3, cl.stats[QStatsDef.STAT_HEALTH] <= 25 ? 1 : 0 );

                // ammo icon
                if ( MainWindow.Common.GameKind == GameKind.Rogue )
                {
                    if ( cl.HasItems( QItemsDef.RIT_SHELLS ) )
                        _resources.DrawPic( 224, 0, _resources.Ammo[0] );
                    else if ( cl.HasItems( QItemsDef.RIT_NAILS ) )
                        _resources.DrawPic( 224, 0, _resources.Ammo[1] );
                    else if ( cl.HasItems( QItemsDef.RIT_ROCKETS ) )
                        _resources.DrawPic( 224, 0, _resources.Ammo[2] );
                    else if ( cl.HasItems( QItemsDef.RIT_CELLS ) )
                        _resources.DrawPic( 224, 0, _resources.Ammo[3] );
                    else if ( cl.HasItems( QItemsDef.RIT_LAVA_NAILS ) )
                        _resources.DrawPic( 224, 0, _resources.RAmmo[0] );
                    else if ( cl.HasItems( QItemsDef.RIT_PLASMA_AMMO ) )
                        _resources.DrawPic( 224, 0, _resources.RAmmo[1] );
                    else if ( cl.HasItems( QItemsDef.RIT_MULTI_ROCKETS ) )
                        _resources.DrawPic( 224, 0, _resources.RAmmo[2] );
                }
                else
                {
                    if ( cl.HasItems( QItemsDef.IT_SHELLS ) )
                        _resources.DrawPic( 224, 0, _resources.Ammo[0] );
                    else if ( cl.HasItems( QItemsDef.IT_NAILS ) )
                        _resources.DrawPic( 224, 0, _resources.Ammo[1] );
                    else if ( cl.HasItems( QItemsDef.IT_ROCKETS ) )
                        _resources.DrawPic( 224, 0, _resources.Ammo[2] );
                    else if ( cl.HasItems( QItemsDef.IT_CELLS ) )
                        _resources.DrawPic( 224, 0, _resources.Ammo[3] );
                }

                _resources.DrawNum( 248, 0, cl.stats[QStatsDef.STAT_AMMO], 3, cl.stats[QStatsDef.STAT_AMMO] <= 10 ? 1 : 0 );
            }

            if ( vid.width > 320 )
            {
                if ( _host.Client.cl.gametype == ProtocolDef.GAME_DEATHMATCH )
                    MiniDeathmatchOverlay( );
            }
        }

        private void DrawInventoryWeapons( ref Int32 flashon )
        {
            var cl = _host.Client.cl;

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

                    _resources.DrawPic( i * 24, -16, _resources.Weapons[flashon, i] );

                    if ( flashon > 1 )
                        _Updates = 0; // force update to remove flash
                }
            }
        }

        private void DrawInventoryHipnoticWeapons( ref Int32 flashon )
        {
            var cl = _host.Client.cl;
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
                                _resources.DrawPic( 96, -16, _resources.HWeapons[flashon, 2] );
                            }
                        }
                    }
                    else if ( i == 3 )
                    {
                        if ( cl.HasItems( QItemsDef.IT_SHOTGUN << 4 ) )
                        {
                            if ( flashon > 0 && grenadeflashing == 0 )
                            {
                                _resources.DrawPic( 96, -16, _resources.HWeapons[flashon, 3] );
                            }
                            else if ( grenadeflashing == 0 )
                            {
                                _resources.DrawPic( 96, -16, _resources.HWeapons[0, 3] );
                            }
                        }
                        else
                            _resources.DrawPic( 96, -16, _resources.HWeapons[flashon, 4] );
                    }
                    else
                        _resources.DrawPic( 176 + ( i * 24 ), -16, _resources.HWeapons[flashon, i] );
                    if ( flashon > 1 )
                        _Updates = 0; // force update to remove flash
                }
            }
        }

        private void DrawInventoryRogueWeapons( )
        {
            var cl = _host.Client.cl;

            // check for powered up weapon.
            if ( cl.stats[QStatsDef.STAT_ACTIVEWEAPON] >= QItemsDef.RIT_LAVA_NAILGUN )
                for ( var i = 0; i < 5; i++ )
                    if ( cl.stats[QStatsDef.STAT_ACTIVEWEAPON] == ( QItemsDef.RIT_LAVA_NAILGUN << i ) )
                        _resources.DrawPic( ( i + 2 ) * 24, -16, _resources.RWeapons[i] );
        }

        private void DrawInventoryAmmoCounts( )
        {
            var cl = _host.Client.cl;

            for ( var i = 0; i < 4; i++ )
            {
                var num = cl.stats[QStatsDef.STAT_SHELLS + i].ToString().PadLeft( 3 );
                //sprintf(num, "%3i", cl.stats[QStats.STAT_SHELLS + i]);
                if ( num[0] != ' ' )
                    _resources.DrawCharacter( ( 6 * i + 1 ) * 8 - 2, -24, 18 + num[0] - '0' );
                if ( num[1] != ' ' )
                    _resources.DrawCharacter( ( 6 * i + 2 ) * 8 - 2, -24, 18 + num[1] - '0' );
                if ( num[2] != ' ' )
                    _resources.DrawCharacter( ( 6 * i + 3 ) * 8 - 2, -24, 18 + num[2] - '0' );
            }
        }

        private void DrawInventoryItems( Int32 flashon )
        {
            var cl = _host.Client.cl;

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
                            _resources.DrawPic( 192 + i * 16, -16, _resources.Items[i] );
                        }
                    }
                    if ( time > 0 && time > cl.time - 2 )
                        _Updates = 0;
                }
            }
        }

        private void DrawInventoryHipnoticItems( Int32 flashon )
        {
            var cl = _host.Client.cl;

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
                        _resources.DrawPic( 288 + i * 16, -16, _resources.HItems[i] );
                    }
                    if ( time > 0 && time > cl.time - 2 )
                        _Updates = 0;
                }
            }
        }

        private void DrawInventoryRogueItems( Int32 flashon )
        {
            var cl = _host.Client.cl;

            for ( var i = 0; i < 2; i++ )
            {
                if ( cl.HasItems( 1 << ( 29 + i ) ) )
                {
                    var time = cl.item_gettime[29 + i];

                    if ( time > 0 && time > cl.time - 2 && flashon > 0 )
                    {   // flash frame
                        _Updates = 0;
                    }
                    else
                    {
                        _resources.DrawPic( 288 + i * 16, -16, _resources.RItems[i] );
                    }

                    if ( time > 0 && time > cl.time - 2 )
                        _Updates = 0;
                }
            }
        }

        private void DrawInventorySigils( Int32 flashon )
        {
            var cl = _host.Client.cl;

            for ( var i = 0; i < 4; i++ )
            {
                if ( cl.HasItems( 1 << ( 28 + i ) ) )
                {
                    var time = cl.item_gettime[28 + i];
                    if ( time > 0 && time > cl.time - 2 && flashon > 0 )
                    {   // flash frame
                        _Updates = 0;
                    }
                    else
                        _resources.DrawPic( 320 - 32 + i * 8, -16, _resources.Sigil[i] );
                    if ( time > 0 && time > cl.time - 2 )
                        _Updates = 0;
                }
            }
        }

        private void DrawInventoryIBar()
        {
            _resources.DrawPic( 0, -24, _resources.IBar );
        }

        private void DrawInventoryRogueIBar()
        {
            var cl = _host.Client.cl;

            if ( cl.stats[QStatsDef.STAT_ACTIVEWEAPON] >= QItemsDef.RIT_LAVA_NAILGUN )
                _resources.DrawPic( 0, -24, _resources.RInvBar[0] );
            else
                _resources.DrawPic( 0, -24, _resources.RInvBar[1] );

        }
        // Sbar_DrawInventory
        private void DrawInventory( )
        {
            var flashon = 0;

            if ( MainWindow.Common.GameKind == GameKind.Rogue )
                DrawInventoryRogueIBar();
            else
                DrawInventoryIBar();

            // weapons
            DrawInventoryWeapons( ref flashon );

            // MED 01/04/97
            // hipnotic weapons
            if ( MainWindow.Common.GameKind == GameKind.Hipnotic )
                DrawInventoryHipnoticWeapons( ref flashon );

            if ( MainWindow.Common.GameKind == GameKind.Rogue )
                DrawInventoryRogueWeapons();

            // ammo counts
            DrawInventoryAmmoCounts();

            // DAN - Is this pointless? it resets flashon and then passes it into the next function, which has logic that might be not be needed
            flashon = 0;
            // items
            DrawInventoryItems( flashon );

            //MED 01/04/97 added hipnotic items
            // hipnotic items
            if ( MainWindow.Common.GameKind == GameKind.Hipnotic )
                DrawInventoryHipnoticItems( flashon );

            if ( MainWindow.Common.GameKind == GameKind.Rogue ) // new rogue items
                DrawInventoryRogueItems( flashon );
            else // sigils
                DrawInventorySigils( flashon );
        }

        // Sbar_DrawScoreboard
        private void DrawScoreboard( )
        {
            SoloScoreboard( );
            if ( _host.Client.cl.gametype == ProtocolDef.GAME_DEATHMATCH )
                DeathmatchOverlay( );
        }

        // Sbar_DrawFace
        private void DrawFace( )
        {
            var cl = _host.Client.cl;

            // PGM 01/19/97 - team color drawing
            // PGM 03/02/97 - fixed so color swatch only appears in CTF modes
            if ( MainWindow.Common.GameKind == GameKind.Rogue &&
                ( _host.Client.cl.maxclients != 1 ) &&
                ( _host.Cvars.TeamPlay.Get<Int32>( ) > 3 ) &&
                ( _host.Cvars.TeamPlay.Get<Int32>( ) < 7 ) )
            {
                var s = cl.scores[cl.viewentity - 1];

                // draw background
                var top = s.colors & 0xf0;
                var bottom = ( s.colors & 15 ) << 4;
                top = _resources.ColorForMap( top );
                bottom = _resources.ColorForMap( bottom );

                Int32 xofs;
                if ( cl.gametype == ProtocolDef.GAME_DEATHMATCH )
                    xofs = 113;
                else
                    xofs = ( ( _host.Screen.vid.width - 320 ) >> 1 ) + 113;

                _resources.DrawPic( 112, 0, _resources.RTeamBord );
                _host.Video.Device.Graphics.FillUsingPalette( xofs, _host.Screen.vid.height - HudResources.SBAR_HEIGHT + 3, 22, 9, top );
                _host.Video.Device.Graphics.FillUsingPalette( xofs, _host.Screen.vid.height - HudResources.SBAR_HEIGHT + 12, 22, 9, bottom );

                // draw number
                var num = s.frags.ToString( ).PadLeft( 3 );
                if ( top == 8 )
                {
                    if ( num[0] != ' ' )
                        _resources.DrawCharacter( 109, 3, 18 + num[0] - '0' );
                    if ( num[1] != ' ' )
                        _resources.DrawCharacter( 116, 3, 18 + num[1] - '0' );
                    if ( num[2] != ' ' )
                        _resources.DrawCharacter( 123, 3, 18 + num[2] - '0' );
                }
                else
                {
                    _resources.DrawCharacter( 109, 3, num[0] );
                    _resources.DrawCharacter( 116, 3, num[1] );
                    _resources.DrawCharacter( 123, 3, num[2] );
                }

                return;
            }
            // PGM 01/19/97 - team color drawing

            Int32 f, anim;

            if ( cl.HasItems( QItemsDef.IT_INVISIBILITY | QItemsDef.IT_INVULNERABILITY ) )
            {
                _resources.DrawPic( 112, 0, _resources.FaceInvisInvuln );
                return;
            }
            if ( cl.HasItems( QItemsDef.IT_QUAD ) )
            {
                _resources.DrawPic( 112, 0, _resources.FaceQuad );
                return;
            }
            if ( cl.HasItems( QItemsDef.IT_INVISIBILITY ) )
            {
                _resources.DrawPic( 112, 0, _resources.FaceInvis );
                return;
            }
            if ( cl.HasItems( QItemsDef.IT_INVULNERABILITY ) )
            {
                _resources.DrawPic( 112, 0, _resources.FaceInvuln );
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

            _resources.DrawPic( 112, 0, _resources.Faces[f, anim] );
        }

        // Sbar_DeathmatchOverlay
        private void MiniDeathmatchOverlay( )
        {
            _host.Screen.Elements.Draw( ElementFactory.MP_MINI_SCOREBOARD );
        }

        // Sbar_SoloScoreboard
        private void SoloScoreboard( )
        {
            _host.Screen.Elements.Draw( ElementFactory.SP_SCOREBOARD );            
        }

        // Sbar_DeathmatchOverlay
        private void DeathmatchOverlay( )
        {
            _host.Screen.Elements.Draw( ElementFactory.MP_SCOREBOARD );            
        }

        // Sbar_ShowScores
        //
        // Tab key down
        private void ShowScores( CommandMessage msg )
        {
            if ( _ShowScores )
                return;
            _ShowScores = true;
            _Updates = 0;
        }

        // Sbar_DontShowScores
        //
        // Tab key up
        private void DontShowScores( CommandMessage msg )
        {
            _ShowScores = false;
            _Updates = 0;
        }
    }
}
