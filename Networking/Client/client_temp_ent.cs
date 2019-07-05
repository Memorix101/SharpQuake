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
using OpenTK;
using SharpQuake.Framework;

// cl_tent.c

namespace SharpQuake
{
    partial class client
    {
        private Int32 _NumTempEntities; // num_temp_entities
        private Entity[] _TempEntities = new Entity[ClientDef.MAX_TEMP_ENTITIES]; // cl_temp_entities[MAX_TEMP_ENTITIES]
        private beam_t[] _Beams = new beam_t[ClientDef.MAX_BEAMS]; // cl_beams[MAX_BEAMS]

        private sfx_t _SfxWizHit; // cl_sfx_wizhit
        private sfx_t _SfxKnigtHit; // cl_sfx_knighthit
        private sfx_t _SfxTink1; // cl_sfx_tink1
        private sfx_t _SfxRic1; // cl_sfx_ric1
        private sfx_t _SfxRic2; // cl_sfx_ric2
        private sfx_t _SfxRic3; // cl_sfx_ric3
        private sfx_t _SfxRExp3; // cl_sfx_r_exp3

        // CL_InitTEnts
        private void InitTempEntities()
        {
            _SfxWizHit = Host.Sound.PrecacheSound( "wizard/hit.wav" );
            _SfxKnigtHit = Host.Sound.PrecacheSound( "hknight/hit.wav" );
            _SfxTink1 = Host.Sound.PrecacheSound( "weapons/tink1.wav" );
            _SfxRic1 = Host.Sound.PrecacheSound( "weapons/ric1.wav" );
            _SfxRic2 = Host.Sound.PrecacheSound( "weapons/ric2.wav" );
            _SfxRic3 = Host.Sound.PrecacheSound( "weapons/ric3.wav" );
            _SfxRExp3 = Host.Sound.PrecacheSound( "weapons/r_exp3.wav" );

            for( var i = 0; i < _TempEntities.Length; i++ )
                _TempEntities[i] = new Entity();

            for( var i = 0; i < _Beams.Length; i++ )
                _Beams[i] = new beam_t();
        }

        // CL_UpdateTEnts
        private void UpdateTempEntities()
        {
            _NumTempEntities = 0;

            // update lightning
            for( var i = 0; i < ClientDef.MAX_BEAMS; i++ )
            {
                var b = _Beams[i];
                if( b.model == null || b.endtime < cl.time )
                    continue;

                // if coming from the player, update the start position
                if( b.entity == cl.viewentity )
                {
                    b.start = _Entities[cl.viewentity].origin;
                }

                // calculate pitch and yaw
                var dist = b.end - b.start;
                Single yaw, pitch, forward;

                if( dist.Y == 0 && dist.X == 0 )
                {
                    yaw = 0;
                    if( dist.Z > 0 )
                        pitch = 90;
                    else
                        pitch = 270;
                }
                else
                {
                    yaw = ( Int32 ) ( Math.Atan2( dist.Y, dist.X ) * 180 / Math.PI );
                    if( yaw < 0 )
                        yaw += 360;

                    forward = ( Single ) Math.Sqrt( dist.X * dist.X + dist.Y * dist.Y );
                    pitch = ( Int32 ) ( Math.Atan2( dist.Z, forward ) * 180 / Math.PI );
                    if( pitch < 0 )
                        pitch += 360;
                }

                // add new entities for the lightning
                var org = b.start;
                var d = MathLib.Normalize( ref dist );
                while( d > 0 )
                {
                    var ent = NewTempEntity();
                    if( ent == null )
                        return;

                    ent.origin = org;
                    ent.model = b.model;
                    ent.angles.X = pitch;
                    ent.angles.Y = yaw;
                    ent.angles.Z = MathLib.Random() % 360;

                    org += dist * 30;
                    // Uze: is this code bug (i is outer loop variable!!!) or what??????????????
                    //for (i=0 ; i<3 ; i++)
                    //    org[i] += dist[i]*30;
                    d -= 30;
                }
            }
        }

        /// <summary>
        /// CL_NewTempEntity
        /// </summary>
        private Entity NewTempEntity()
        {
            if( NumVisEdicts == ClientDef.MAX_VISEDICTS )
                return null;
            if( _NumTempEntities == ClientDef.MAX_TEMP_ENTITIES )
                return null;

            var ent = _TempEntities[_NumTempEntities];
            _NumTempEntities++;
            _VisEdicts[NumVisEdicts] = ent;
            NumVisEdicts++;

            ent.colormap = Host.Screen.vid.colormap;

            return ent;
        }

        /// <summary>
        /// CL_ParseTEnt
        /// </summary>
        private void ParseTempEntity()
        {
            Vector3 pos;
            dlight_t dl;
            var type = Host.Network.Reader.ReadByte();
            switch( type )
            {
                case ProtocolDef.TE_WIZSPIKE:			// spike hitting wall
                    pos = Host.Network.Reader.ReadCoords();
                    Host.RenderContext.RunParticleEffect( ref pos, ref Utilities.ZeroVector, 20, 30 );
                    Host.Sound.StartSound( -1, 0, _SfxWizHit, ref pos, 1, 1 );
                    break;

                case ProtocolDef.TE_KNIGHTSPIKE:			// spike hitting wall
                    pos = Host.Network.Reader.ReadCoords();
                    Host.RenderContext.RunParticleEffect( ref pos, ref Utilities.ZeroVector, 226, 20 );
                    Host.Sound.StartSound( -1, 0, _SfxKnigtHit, ref pos, 1, 1 );
                    break;

                case ProtocolDef.TE_SPIKE:			// spike hitting wall
                    pos = Host.Network.Reader.ReadCoords();
#if GLTEST
                    Test_Spawn (pos);
#else
                    Host.RenderContext.RunParticleEffect( ref pos, ref Utilities.ZeroVector, 0, 10 );
#endif
                    if( ( MathLib.Random() % 5 ) != 0 )
                        Host.Sound.StartSound( -1, 0, _SfxTink1, ref pos, 1, 1 );
                    else
                    {
                        var rnd = MathLib.Random() & 3;
                        if( rnd == 1 )
                            Host.Sound.StartSound( -1, 0, _SfxRic1, ref pos, 1, 1 );
                        else if( rnd == 2 )
                            Host.Sound.StartSound( -1, 0, _SfxRic2, ref pos, 1, 1 );
                        else
                            Host.Sound.StartSound( -1, 0, _SfxRic3, ref pos, 1, 1 );
                    }
                    break;

                case ProtocolDef.TE_SUPERSPIKE:			// super spike hitting wall
                    pos = Host.Network.Reader.ReadCoords();
                    Host.RenderContext.RunParticleEffect( ref pos, ref Utilities.ZeroVector, 0, 20 );

                    if( ( MathLib.Random() % 5 ) != 0 )
                        Host.Sound.StartSound( -1, 0, _SfxTink1, ref pos, 1, 1 );
                    else
                    {
                        var rnd = MathLib.Random() & 3;
                        if( rnd == 1 )
                            Host.Sound.StartSound( -1, 0, _SfxRic1, ref pos, 1, 1 );
                        else if( rnd == 2 )
                            Host.Sound.StartSound( -1, 0, _SfxRic2, ref pos, 1, 1 );
                        else
                            Host.Sound.StartSound( -1, 0, _SfxRic3, ref pos, 1, 1 );
                    }
                    break;

                case ProtocolDef.TE_GUNSHOT:			// bullet hitting wall
                    pos = Host.Network.Reader.ReadCoords();
                    Host.RenderContext.RunParticleEffect( ref pos, ref Utilities.ZeroVector, 0, 20 );
                    break;

                case ProtocolDef.TE_EXPLOSION:			// rocket explosion
                    pos = Host.Network.Reader.ReadCoords();
                    Host.RenderContext.ParticleExplosion( ref pos );
                    dl = AllocDlight( 0 );
                    dl.origin = pos;
                    dl.radius = 350;
                    dl.die = ( Single ) cl.time + 0.5f;
                    dl.decay = 300;
                    Host.Sound.StartSound( -1, 0, _SfxRExp3, ref pos, 1, 1 );
                    break;

                case ProtocolDef.TE_TAREXPLOSION:			// tarbaby explosion
                    pos = Host.Network.Reader.ReadCoords();
                    Host.RenderContext.BlobExplosion( ref pos );
                    Host.Sound.StartSound( -1, 0, _SfxRExp3, ref pos, 1, 1 );
                    break;

                case ProtocolDef.TE_LIGHTNING1:				// lightning bolts
                    ParseBeam( Host.Model.ForName( "progs/bolt.mdl", true ) );
                    break;

                case ProtocolDef.TE_LIGHTNING2:				// lightning bolts
                    ParseBeam( Host.Model.ForName( "progs/bolt2.mdl", true ) );
                    break;

                case ProtocolDef.TE_LIGHTNING3:				// lightning bolts
                    ParseBeam( Host.Model.ForName( "progs/bolt3.mdl", true ) );
                    break;

                // PGM 01/21/97
                case ProtocolDef.TE_BEAM:				// grappling hook beam
                    ParseBeam( Host.Model.ForName( "progs/beam.mdl", true ) );
                    break;
                // PGM 01/21/97

                case ProtocolDef.TE_LAVASPLASH:
                    pos = Host.Network.Reader.ReadCoords();
                    Host.RenderContext.LavaSplash( ref pos );
                    break;

                case ProtocolDef.TE_TELEPORT:
                    pos = Host.Network.Reader.ReadCoords();
                    Host.RenderContext.TeleportSplash( ref pos );
                    break;

                case ProtocolDef.TE_EXPLOSION2:				// color mapped explosion
                    pos = Host.Network.Reader.ReadCoords();
                    var colorStart = Host.Network.Reader.ReadByte();
                    var colorLength = Host.Network.Reader.ReadByte();
                    Host.RenderContext.ParticleExplosion( ref pos, colorStart, colorLength );
                    dl = AllocDlight( 0 );
                    dl.origin = pos;
                    dl.radius = 350;
                    dl.die = ( Single ) cl.time + 0.5f;
                    dl.decay = 300;
                    Host.Sound.StartSound( -1, 0, _SfxRExp3, ref pos, 1, 1 );
                    break;

                default:
                    Utilities.Error( "CL_ParseTEnt: bad type" );
                    break;
            }
        }

        /// <summary>
        /// CL_ParseBeam
        /// </summary>
        private void ParseBeam( Model m )
        {
            var ent = Host.Network.Reader.ReadShort();

            var start = Host.Network.Reader.ReadCoords();
            var end = Host.Network.Reader.ReadCoords();

            // override any beam with the same entity
            for( var i = 0; i < ClientDef.MAX_BEAMS; i++ )
            {
                var b = _Beams[i];
                if( b.entity == ent )
                {
                    b.entity = ent;
                    b.model = m;
                    b.endtime = ( Single ) ( cl.time + 0.2 );
                    b.start = start;
                    b.end = end;
                    return;
                }
            }

            // find a free beam
            for( var i = 0; i < ClientDef.MAX_BEAMS; i++ )
            {
                var b = _Beams[i];
                if( b.model == null || b.endtime < cl.time )
                {
                    b.entity = ent;
                    b.model = m;
                    b.endtime = ( Single ) ( cl.time + 0.2 );
                    b.start = start;
                    b.end = end;
                    return;
                }
            }
            Host.Console.Print( "beam list overflow!\n" );
        }
    }
}
