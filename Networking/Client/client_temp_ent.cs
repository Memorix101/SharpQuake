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
using OpenTK;
using SharpQuake.Framework;

// cl_tent.c

namespace SharpQuake
{
    partial class client
    {
        private static Int32 _NumTempEntities; // num_temp_entities
        private static entity_t[] _TempEntities = new entity_t[MAX_TEMP_ENTITIES]; // cl_temp_entities[MAX_TEMP_ENTITIES]
        private static beam_t[] _Beams = new beam_t[MAX_BEAMS]; // cl_beams[MAX_BEAMS]

        private static sfx_t _SfxWizHit; // cl_sfx_wizhit
        private static sfx_t _SfxKnigtHit; // cl_sfx_knighthit
        private static sfx_t _SfxTink1; // cl_sfx_tink1
        private static sfx_t _SfxRic1; // cl_sfx_ric1
        private static sfx_t _SfxRic2; // cl_sfx_ric2
        private static sfx_t _SfxRic3; // cl_sfx_ric3
        private static sfx_t _SfxRExp3; // cl_sfx_r_exp3

        // CL_InitTEnts
        private static void InitTempEntities()
        {
            _SfxWizHit = snd.PrecacheSound( "wizard/hit.wav" );
            _SfxKnigtHit = snd.PrecacheSound( "hknight/hit.wav" );
            _SfxTink1 = snd.PrecacheSound( "weapons/tink1.wav" );
            _SfxRic1 = snd.PrecacheSound( "weapons/ric1.wav" );
            _SfxRic2 = snd.PrecacheSound( "weapons/ric2.wav" );
            _SfxRic3 = snd.PrecacheSound( "weapons/ric3.wav" );
            _SfxRExp3 = snd.PrecacheSound( "weapons/r_exp3.wav" );

            for( var i = 0; i < _TempEntities.Length; i++ )
                _TempEntities[i] = new entity_t();

            for( var i = 0; i < _Beams.Length; i++ )
                _Beams[i] = new beam_t();
        }

        // CL_UpdateTEnts
        private static void UpdateTempEntities()
        {
            _NumTempEntities = 0;

            // update lightning
            for( var i = 0; i < MAX_BEAMS; i++ )
            {
                beam_t b = _Beams[i];
                if( b.model == null || b.endtime < cl.time )
                    continue;

                // if coming from the player, update the start position
                if( b.entity == cl.viewentity )
                {
                    b.start = _Entities[cl.viewentity].origin;
                }

                // calculate pitch and yaw
                Vector3 dist = b.end - b.start;
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
                Vector3 org = b.start;
                var d = MathLib.Normalize( ref dist );
                while( d > 0 )
                {
                    entity_t ent = NewTempEntity();
                    if( ent == null )
                        return;

                    ent.origin = org;
                    ent.model = b.model;
                    ent.angles.X = pitch;
                    ent.angles.Y = yaw;
                    ent.angles.Z = sys.Random() % 360;

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
        private static entity_t NewTempEntity()
        {
            if( NumVisEdicts == MAX_VISEDICTS )
                return null;
            if( _NumTempEntities == MAX_TEMP_ENTITIES )
                return null;

            entity_t ent = _TempEntities[_NumTempEntities];
            _NumTempEntities++;
            _VisEdicts[NumVisEdicts] = ent;
            NumVisEdicts++;

            ent.colormap = Scr.vid.colormap;

            return ent;
        }

        /// <summary>
        /// CL_ParseTEnt
        /// </summary>
        private static void ParseTempEntity()
        {
            Vector3 pos;
            dlight_t dl;
            var type = net.Reader.ReadByte();
            switch( type )
            {
                case protocol.TE_WIZSPIKE:			// spike hitting wall
                    pos = net.Reader.ReadCoords();
                    render.RunParticleEffect( ref pos, ref Common.ZeroVector, 20, 30 );
                    snd.StartSound( -1, 0, _SfxWizHit, ref pos, 1, 1 );
                    break;

                case protocol.TE_KNIGHTSPIKE:			// spike hitting wall
                    pos = net.Reader.ReadCoords();
                    render.RunParticleEffect( ref pos, ref Common.ZeroVector, 226, 20 );
                    snd.StartSound( -1, 0, _SfxKnigtHit, ref pos, 1, 1 );
                    break;

                case protocol.TE_SPIKE:			// spike hitting wall
                    pos = net.Reader.ReadCoords();
#if GLTEST
                    Test_Spawn (pos);
#else
                    render.RunParticleEffect( ref pos, ref Common.ZeroVector, 0, 10 );
#endif
                    if( ( sys.Random() % 5 ) != 0 )
                        snd.StartSound( -1, 0, _SfxTink1, ref pos, 1, 1 );
                    else
                    {
                        var rnd = sys.Random() & 3;
                        if( rnd == 1 )
                            snd.StartSound( -1, 0, _SfxRic1, ref pos, 1, 1 );
                        else if( rnd == 2 )
                            snd.StartSound( -1, 0, _SfxRic2, ref pos, 1, 1 );
                        else
                            snd.StartSound( -1, 0, _SfxRic3, ref pos, 1, 1 );
                    }
                    break;

                case protocol.TE_SUPERSPIKE:			// super spike hitting wall
                    pos = net.Reader.ReadCoords();
                    render.RunParticleEffect( ref pos, ref Common.ZeroVector, 0, 20 );

                    if( ( sys.Random() % 5 ) != 0 )
                        snd.StartSound( -1, 0, _SfxTink1, ref pos, 1, 1 );
                    else
                    {
                        var rnd = sys.Random() & 3;
                        if( rnd == 1 )
                            snd.StartSound( -1, 0, _SfxRic1, ref pos, 1, 1 );
                        else if( rnd == 2 )
                            snd.StartSound( -1, 0, _SfxRic2, ref pos, 1, 1 );
                        else
                            snd.StartSound( -1, 0, _SfxRic3, ref pos, 1, 1 );
                    }
                    break;

                case protocol.TE_GUNSHOT:			// bullet hitting wall
                    pos = net.Reader.ReadCoords();
                    render.RunParticleEffect( ref pos, ref Common.ZeroVector, 0, 20 );
                    break;

                case protocol.TE_EXPLOSION:			// rocket explosion
                    pos = net.Reader.ReadCoords();
                    render.ParticleExplosion( ref pos );
                    dl = AllocDlight( 0 );
                    dl.origin = pos;
                    dl.radius = 350;
                    dl.die = ( Single ) client.cl.time + 0.5f;
                    dl.decay = 300;
                    snd.StartSound( -1, 0, _SfxRExp3, ref pos, 1, 1 );
                    break;

                case protocol.TE_TAREXPLOSION:			// tarbaby explosion
                    pos = net.Reader.ReadCoords();
                    render.BlobExplosion( ref pos );
                    snd.StartSound( -1, 0, _SfxRExp3, ref pos, 1, 1 );
                    break;

                case protocol.TE_LIGHTNING1:				// lightning bolts
                    ParseBeam( Mod.ForName( "progs/bolt.mdl", true ) );
                    break;

                case protocol.TE_LIGHTNING2:				// lightning bolts
                    ParseBeam( Mod.ForName( "progs/bolt2.mdl", true ) );
                    break;

                case protocol.TE_LIGHTNING3:				// lightning bolts
                    ParseBeam( Mod.ForName( "progs/bolt3.mdl", true ) );
                    break;

                // PGM 01/21/97
                case protocol.TE_BEAM:				// grappling hook beam
                    ParseBeam( Mod.ForName( "progs/beam.mdl", true ) );
                    break;
                // PGM 01/21/97

                case protocol.TE_LAVASPLASH:
                    pos = net.Reader.ReadCoords();
                    render.LavaSplash( ref pos );
                    break;

                case protocol.TE_TELEPORT:
                    pos = net.Reader.ReadCoords();
                    render.TeleportSplash( ref pos );
                    break;

                case protocol.TE_EXPLOSION2:				// color mapped explosion
                    pos = net.Reader.ReadCoords();
                    var colorStart = net.Reader.ReadByte();
                    var colorLength = net.Reader.ReadByte();
                    render.ParticleExplosion( ref pos, colorStart, colorLength );
                    dl = AllocDlight( 0 );
                    dl.origin = pos;
                    dl.radius = 350;
                    dl.die = ( Single ) cl.time + 0.5f;
                    dl.decay = 300;
                    snd.StartSound( -1, 0, _SfxRExp3, ref pos, 1, 1 );
                    break;

                default:
                    Utilities.Error( "CL_ParseTEnt: bad type" );
                    break;
            }
        }

        /// <summary>
        /// CL_ParseBeam
        /// </summary>
        private static void ParseBeam( model_t m )
        {
            var ent = net.Reader.ReadShort();

            Vector3 start = net.Reader.ReadCoords();
            Vector3 end = net.Reader.ReadCoords();

            // override any beam with the same entity
            for( var i = 0; i < MAX_BEAMS; i++ )
            {
                beam_t b = _Beams[i];
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
            for( var i = 0; i < MAX_BEAMS; i++ )
            {
                beam_t b = _Beams[i];
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
            Con.Print( "beam list overflow!\n" );
        }
    }
}
